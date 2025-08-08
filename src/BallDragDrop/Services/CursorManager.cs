using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BallDragDrop.Contracts;
using BallDragDrop.Models;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Manages cursor loading, caching, and application
    /// </summary>
    public class CursorManager : ICursorService
    {
        #region Fields

        /// <summary>
        /// Configuration service for cursor settings
        /// </summary>
        private readonly IConfigurationService _configurationService;

        /// <summary>
        /// Log service for error reporting
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Image loader for PNG cursor files
        /// </summary>
        private readonly CursorImageLoader _imageLoader;

        /// <summary>
        /// Cache of loaded cursors by hand state
        /// </summary>
        private readonly Dictionary<HandState, Cursor> _cursorCache;

        /// <summary>
        /// Lock object for thread-safe cache operations
        /// </summary>
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Current cursor configuration
        /// </summary>
        private CursorConfiguration? _currentConfiguration;

        /// <summary>
        /// Current hand state for debugging
        /// </summary>
        private HandState _currentHandState = HandState.Default;

        /// <summary>
        /// Event throttler for cursor updates to prevent flickering
        /// </summary>
        private EventThrottler? _cursorUpdateThrottler;

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the CursorManager class
        /// </summary>
        /// <param name="configurationService">Configuration service</param>
        /// <param name="logService">Log service</param>
        /// <param name="imageLoader">Image loader for PNG files</param>
        /// <param name="cursorConfiguration">Cursor configuration</param>
        public CursorManager(
            IConfigurationService configurationService,
            ILogService logService,
            CursorImageLoader imageLoader,
            CursorConfiguration cursorConfiguration)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _imageLoader = imageLoader ?? throw new ArgumentNullException(nameof(imageLoader));

            _cursorCache = new Dictionary<HandState, Cursor>();
            
            // Use injected configuration
            _currentConfiguration = cursorConfiguration;
            
            // Initialize cursor update throttler with configured debounce time
            InitializeCursorUpdateThrottler();
            
            _logService.LogDebug("CursorManager initialized");
        }

        #endregion Construction

        #region SetCursorForHandState

        /// <summary>
        /// Sets the cursor for the specified hand state
        /// </summary>
        /// <param name="handState">The hand state</param>
        public void SetCursorForHandState(HandState handState)
        {
            try
            {
                _currentHandState = handState;
                
                if (_currentConfiguration?.EnableCustomCursors != true)
                {
                    _logService.LogDebug("Custom cursors disabled, using system cursor for hand state {HandState}", handState);
                    SetSystemCursorThrottled();
                    return;
                }

                // Use throttled cursor update to prevent flickering
                _cursorUpdateThrottler?.Execute();
                
                _logService.LogDebug("Queued cursor update for hand state {HandState}", handState);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error setting cursor for hand state {HandState}", handState);
                SetSystemCursor();
            }
        }

        #endregion SetCursorForHandState

        #region ReloadConfigurationAsync

        /// <summary>
        /// Reloads cursor configuration from settings with comprehensive error handling and recovery
        /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            var previousConfiguration = _currentConfiguration;
            
            try
            {
                _logService.LogInformation("Starting cursor configuration reload");
                
                // Clear cache to force reload of cursors
                try
                {
                    ClearCache();
                    _logService.LogDebug("Cursor cache cleared for configuration reload");
                }
                catch (Exception cacheEx)
                {
                    _logService.LogError(cacheEx, "Error clearing cursor cache during reload, continuing with reload");
                }
                
                // Reload configuration with validation
                try
                {
                    LoadConfiguration();
                    
                    if (_currentConfiguration == null)
                    {
                        _logService.LogError("Configuration reload resulted in null configuration, reverting to previous configuration");
                        _currentConfiguration = previousConfiguration ?? CreateFallbackConfiguration();
                        return;
                    }
                    
                    _logService.LogDebug("Configuration reloaded successfully");
                }
                catch (Exception configEx)
                {
                    _logService.LogError(configEx, "Error loading configuration during reload, reverting to previous configuration");
                    _currentConfiguration = previousConfiguration ?? CreateFallbackConfiguration();
                    return;
                }
                
                // Reinitialize throttler with new configuration
                try
                {
                    InitializeCursorUpdateThrottler();
                    _logService.LogDebug("Cursor update throttler reinitialized with new configuration");
                }
                catch (Exception throttlerEx)
                {
                    _logService.LogError(throttlerEx, "Error reinitializing cursor update throttler, using previous configuration");
                    _currentConfiguration = previousConfiguration ?? CreateFallbackConfiguration();
                    
                    // Try to reinitialize with previous configuration
                    try
                    {
                        InitializeCursorUpdateThrottler();
                    }
                    catch (Exception fallbackThrottlerEx)
                    {
                        _logService.LogError(fallbackThrottlerEx, "Error reinitializing throttler with previous configuration");
                    }
                    return;
                }
                
                // Reapply current cursor with new configuration
                try
                {
                    SetCursorForHandState(_currentHandState);
                    _logService.LogDebug("Current cursor reapplied with new configuration");
                }
                catch (Exception cursorEx)
                {
                    _logService.LogError(cursorEx, "Error reapplying cursor with new configuration, cursor may not update until next state change");
                }
                
                _logService.LogInformation("Cursor configuration reloaded successfully. EnableCustomCursors: {EnableCustomCursors}, DebounceTimeMs: {DebounceTimeMs}", 
                    _currentConfiguration.EnableCustomCursors, _currentConfiguration.DebounceTimeMs);
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory during configuration reload, reverting to previous configuration");
                _currentConfiguration = previousConfiguration ?? CreateFallbackConfiguration();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error during cursor configuration reload, reverting to previous configuration");
                _currentConfiguration = previousConfiguration ?? CreateFallbackConfiguration();
            }
            
            await Task.CompletedTask;
        }

        #endregion ReloadConfigurationAsync

        #region GetCurrentCursorState

        /// <summary>
        /// Gets the current cursor state for debugging
        /// </summary>
        /// <returns>Current cursor state description</returns>
        public string GetCurrentCursorState()
        {
            try
            {
                var enabled = _currentConfiguration?.EnableCustomCursors == true ? "Enabled" : "Disabled";
                var cacheCount = 0;
                
                lock (_cacheLock)
                {
                    cacheCount = _cursorCache.Count;
                }
                
                return $"HandState: {_currentHandState}, CustomCursors: {enabled}, CachedCursors: {cacheCount}";
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error getting current cursor state");
                return "Error retrieving cursor state";
            }
        }

        #endregion GetCurrentCursorState

        #region GetCursorForHandState

        /// <summary>
        /// Gets the cursor for the specified hand state, loading from cache or file with comprehensive error handling
        /// </summary>
        /// <param name="handState">Hand state</param>
        /// <returns>Cursor for the hand state</returns>
        private Cursor GetCursorForHandState(HandState handState)
        {
            try
            {
                lock (_cacheLock)
                {
                    // Check cache first
                    if (_cursorCache.TryGetValue(handState, out var cachedCursor))
                    {
                        if (cachedCursor != null)
                        {
                            _logService.LogDebug("Using cached cursor for hand state {HandState}", handState);
                            return cachedCursor;
                        }
                        else
                        {
                            _logService.LogWarning("Cached cursor is null for hand state {HandState}, removing from cache", handState);
                            _cursorCache.Remove(handState);
                        }
                    }

                    // Load cursor from configuration
                    var cursor = LoadCursorFromConfiguration(handState);
                    
                    if (cursor == null)
                    {
                        _logService.LogWarning("LoadCursorFromConfiguration returned null for hand state {HandState}, using system cursor", handState);
                        cursor = Cursors.Arrow;
                    }

                    // Cache the cursor (even if it's the fallback)
                    try
                    {
                        _cursorCache[handState] = cursor;
                        _logService.LogDebug("Loaded and cached cursor for hand state '{HandState}'", handState);
                    }
                    catch (Exception cacheEx)
                    {
                        _logService.LogWarning("Failed to cache cursor for hand state '{HandState}', continuing without caching", handState);
                    }
                    
                    return cursor;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error getting cursor for hand state '{HandState}', using system cursor", handState);
                return Cursors.Arrow;
            }
        }

        #endregion GetCursorForHandState

        #region LoadCursorFromConfiguration

        /// <summary>
        /// Loads a cursor from configuration for the specified hand state
        /// </summary>
        /// <param name="handState">Hand state</param>
        /// <returns>Loaded cursor</returns>
        private Cursor LoadCursorFromConfiguration(HandState handState)
        {
            if (_currentConfiguration == null)
            {
                _logService.LogWarning("No cursor configuration available, using system cursor");
                return Cursors.Arrow;
            }

            var cursorPath = GetCursorPathForHandState(handState);
            
            if (string.IsNullOrEmpty(cursorPath))
            {
                _logService.LogWarning("No cursor path configured for hand state {HandState}, using system cursor", handState);
                return Cursors.Arrow;
            }

            return LoadCursorWithFallback(cursorPath, Cursors.Arrow);
        }

        #endregion LoadCursorFromConfiguration

        #region LoadCursorWithFallback

        /// <summary>
        /// Loads a cursor with comprehensive error handling and fallback mechanisms
        /// </summary>
        /// <param name="pngPath">Path to the PNG cursor file</param>
        /// <param name="fallbackCursor">Cursor to use if loading fails</param>
        /// <returns>Loaded cursor or fallback cursor</returns>
        private Cursor LoadCursorWithFallback(string pngPath, Cursor fallbackCursor)
        {
            try
            {
                if (string.IsNullOrEmpty(pngPath))
                {
                    _logService.LogWarning("Cursor PNG path is null or empty, using fallback cursor");
                    return fallbackCursor;
                }

                _logService.LogDebug("Attempting to load cursor from path: {PngPath}", pngPath);

                // Validate configuration state
                if (_currentConfiguration?.EnableCustomCursors != true)
                {
                    _logService.LogDebug("Custom cursors disabled in configuration, using fallback cursor");
                    return fallbackCursor;
                }

                // Use the image loader with its own error handling
                var cursor = _imageLoader.LoadPngAsCursorWithFallback(pngPath, fallbackCursor);
                
                if (cursor == fallbackCursor)
                {
                    _logService.LogWarning("Image loader returned fallback cursor for path: {PngPath}", pngPath);
                }
                else
                {
                    _logService.LogDebug("Successfully loaded cursor from path: {PngPath}", pngPath);
                }

                return cursor;
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory while loading cursor from {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logService.LogError(ex, "Access denied loading cursor from {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
            catch (System.IO.IOException ex)
            {
                _logService.LogError(ex, "IO error loading cursor from {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error loading cursor from {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
        }

        #endregion LoadCursorWithFallback

        #region GetCursorPathForHandState

        /// <summary>
        /// Gets the cursor path for the specified hand state from configuration
        /// </summary>
        /// <param name="handState">Hand state</param>
        /// <returns>Cursor path or empty string if not configured</returns>
        private string GetCursorPathForHandState(HandState handState)
        {
            if (_currentConfiguration == null)
                return "";

            return handState switch
            {
                HandState.Default => _currentConfiguration.DefaultCursorPath,
                HandState.Hover => _currentConfiguration.HoverCursorPath,
                HandState.Grabbing => _currentConfiguration.GrabbingCursorPath,
                HandState.Releasing => _currentConfiguration.ReleasingCursorPath,
                _ => _currentConfiguration.DefaultCursorPath
            };
        }

        #endregion GetCursorPathForHandState

        #region ApplyCursor

        /// <summary>
        /// Applies the cursor to the main window with comprehensive error handling
        /// </summary>
        /// <param name="cursor">Cursor to apply</param>
        private void ApplyCursor(Cursor cursor)
        {
            try
            {
                if (cursor == null)
                {
                    _logService.LogWarning("Attempted to apply null cursor, using default system cursor");
                    cursor = Cursors.Arrow;
                }

                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            Application.Current.MainWindow.Cursor = cursor;
                            _logService.LogDebug("Successfully applied cursor to main window");
                        }
                        catch (Exception ex)
                        {
                            HandleCursorApplicationError(ex, _currentHandState);
                        }
                    });
                }
                else
                {
                    _logService.LogWarning("Main window not available for cursor application");
                    // Try to apply to current window if available
                    TryApplyToCurrentWindow(cursor);
                }
            }
            catch (InvalidOperationException ex)
            {
                HandleCursorApplicationError(ex, _currentHandState);
            }
            catch (Exception ex)
            {
                HandleCursorApplicationError(ex, _currentHandState);
            }
        }

        #endregion ApplyCursor

        #region HandleCursorApplicationError

        /// <summary>
        /// Handles errors that occur during cursor application with recovery mechanisms
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="handState">The hand state that was being applied</param>
        private void HandleCursorApplicationError(Exception ex, HandState handState)
        {
            try
            {
                _logService.LogError(ex, "Failed to apply cursor for hand state {HandState}, attempting recovery", handState);

                // Attempt recovery strategies in order of preference
                if (TryApplySystemCursorRecovery())
                {
                    _logService.LogInformation("Successfully recovered from cursor application error using system cursor");
                    return;
                }

                if (TryApplyToCurrentWindow(Cursors.Arrow))
                {
                    _logService.LogInformation("Successfully recovered from cursor application error using current window");
                    return;
                }

                // If all recovery attempts fail, log the failure but don't throw
                _logService.LogError("All cursor application recovery attempts failed for hand state {HandState}", handState);
            }
            catch (Exception recoveryEx)
            {
                _logService.LogError(recoveryEx, "Error during cursor application recovery for hand state {HandState}", handState);
            }
        }

        #endregion HandleCursorApplicationError

        #region TryApplySystemCursorRecovery

        /// <summary>
        /// Attempts to recover by applying the default system cursor
        /// </summary>
        /// <returns>True if recovery was successful</returns>
        private bool TryApplySystemCursorRecovery()
        {
            try
            {
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.MainWindow.Cursor = Cursors.Arrow;
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logService.LogDebug("System cursor recovery attempt failed. Exception: {Exception}", ex.Message);
            }
            return false;
        }

        #endregion TryApplySystemCursorRecovery

        #region TryApplyToCurrentWindow

        /// <summary>
        /// Attempts to apply cursor to the current active window
        /// </summary>
        /// <param name="cursor">Cursor to apply</param>
        /// <returns>True if application was successful</returns>
        private bool TryApplyToCurrentWindow(Cursor cursor)
        {
            try
            {
                // Try to find any available window
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsVisible && window.IsEnabled)
                    {
                        window.Cursor = cursor;
                        _logService.LogDebug("Applied cursor to window: {WindowTitle}", window.Title);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogDebug("Failed to apply cursor to current window. Exception: {Exception}", ex.Message);
            }
            return false;
        }

        #endregion TryApplyToCurrentWindow

        #region SetSystemCursor

        /// <summary>
        /// Sets the system default cursor
        /// </summary>
        private void SetSystemCursor()
        {
            try
            {
                ApplyCursor(Cursors.Arrow);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error setting system cursor");
            }
        }

        #endregion SetSystemCursor

        #region SetSystemCursorSafe

        /// <summary>
        /// Sets the system default cursor with maximum safety and minimal error handling
        /// </summary>
        private void SetSystemCursorSafe()
        {
            try
            {
                // Use the most basic cursor application possible
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.Cursor = Cursors.Arrow;
                }
            }
            catch
            {
                // Silently fail - this is the last resort recovery method
                // We don't want to cause additional exceptions during error recovery
            }
        }

        #endregion SetSystemCursorSafe

        #region LoadConfiguration

        /// <summary>
        /// Loads cursor configuration from the configuration service with comprehensive validation and error recovery
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                _logService.LogDebug("Loading cursor configuration from configuration service");
                
                // Get configuration from service
                var configFromService = _configurationService.GetCursorConfiguration();
                
                if (configFromService == null)
                {
                    _logService.LogError("Configuration service returned null cursor configuration, using fallback");
                    _currentConfiguration = CreateFallbackConfiguration();
                    return;
                }

                // Validate the configuration
                if (_configurationService.ValidateCursorConfiguration(configFromService))
                {
                    _currentConfiguration = configFromService;
                    _logService.LogDebug("Cursor configuration loaded and validated successfully");
                }
                else
                {
                    _logService.LogWarning("Cursor configuration validation failed, attempting to use configuration with system cursor fallback");
                    
                    // Use the configuration but disable custom cursors for safety
                    _currentConfiguration = new CursorConfiguration
                    {
                        EnableCustomCursors = false,
                        DefaultCursorPath = configFromService.DefaultCursorPath,
                        HoverCursorPath = configFromService.HoverCursorPath,
                        GrabbingCursorPath = configFromService.GrabbingCursorPath,
                        ReleasingCursorPath = configFromService.ReleasingCursorPath,
                        DebounceTimeMs = Math.Max(1, Math.Min(1000, configFromService.DebounceTimeMs)),
                        ReleasingDurationMs = Math.Max(50, Math.Min(5000, configFromService.ReleasingDurationMs))
                    };
                    
                    _logService.LogInformation("Using cursor configuration with custom cursors disabled due to validation failures");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Critical error loading cursor configuration, using fallback configuration");
                _currentConfiguration = CreateFallbackConfiguration();
            }
        }

        #endregion LoadConfiguration

        #region CreateFallbackConfiguration

        /// <summary>
        /// Creates a safe fallback cursor configuration when all else fails
        /// </summary>
        /// <returns>Fallback cursor configuration</returns>
        private CursorConfiguration CreateFallbackConfiguration()
        {
            try
            {
                _logService.LogDebug("Creating fallback cursor configuration");
                
                return new CursorConfiguration
                {
                    EnableCustomCursors = false, // Always disable custom cursors in fallback
                    DefaultCursorPath = "",
                    HoverCursorPath = "",
                    GrabbingCursorPath = "",
                    ReleasingCursorPath = "",
                    DebounceTimeMs = 16, // Safe default
                    ReleasingDurationMs = 200 // Safe default
                };
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error creating fallback configuration, using minimal configuration");
                
                // Last resort - create minimal configuration without any dependencies
                return new CursorConfiguration();
            }
        }

        #endregion CreateFallbackConfiguration

        #region ClearCache

        /// <summary>
        /// Clears the cursor cache with comprehensive error handling
        /// </summary>
        private void ClearCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    var cacheCount = _cursorCache.Count;
                    _logService.LogDebug("Clearing cursor cache with {CacheCount} entries", cacheCount);

                    // Dispose cached cursors if needed
                    var disposalErrors = 0;
                    foreach (var kvp in _cursorCache)
                    {
                        try
                        {
                            // WPF cursors don't typically need explicit disposal
                            // but we validate the references and clear them safely
                            if (kvp.Value == null)
                            {
                                _logService.LogDebug("Found null cursor in cache for hand state {HandState}", kvp.Key);
                            }
                        }
                        catch (Exception cursorEx)
                        {
                            disposalErrors++;
                            _logService.LogWarning("Error processing cached cursor for hand state '{HandState}'", kvp.Key);
                        }
                    }
                    
                    _cursorCache.Clear();
                    
                    if (disposalErrors > 0)
                    {
                        _logService.LogWarning("Cursor cache cleared with {DisposalErrors} disposal errors out of {CacheCount} entries", 
                            disposalErrors, cacheCount);
                    }
                    else
                    {
                        _logService.LogDebug("Cursor cache cleared successfully, {CacheCount} entries removed", cacheCount);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logService.LogError(ex, "Invalid operation while clearing cursor cache, forcing clear");
                    try
                    {
                        _cursorCache.Clear();
                    }
                    catch (Exception clearEx)
                    {
                        _logService.LogError(clearEx, "Failed to force clear cursor cache");
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Unexpected error clearing cursor cache");
                }
            }
        }

        #endregion ClearCache

        #region InitializeCursorUpdateThrottler

        /// <summary>
        /// Initializes the cursor update throttler for performance
        /// </summary>
        private void InitializeCursorUpdateThrottler()
        {
            try
            {
                var debounceTimeMs = _currentConfiguration?.DebounceTimeMs ?? 16;
                _cursorUpdateThrottler = new EventThrottler(UpdateCursorNow, debounceTimeMs);
                _logService.LogDebug("Cursor update throttler initialized with {DebounceTime}ms debounce", debounceTimeMs);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error initializing cursor update throttler");
            }
        }

        #endregion InitializeCursorUpdateThrottler

        #region UpdateCursorNow

        /// <summary>
        /// Updates the cursor immediately based on current hand state with comprehensive error handling
        /// </summary>
        private void UpdateCursorNow()
        {
            try
            {
                _logService.LogDebug("Updating cursor for hand state {HandState}", _currentHandState);

                if (_currentConfiguration?.EnableCustomCursors != true)
                {
                    _logService.LogDebug("Custom cursors disabled, applying system cursor");
                    SetSystemCursor();
                    return;
                }

                var cursor = GetCursorForHandState(_currentHandState);
                
                if (cursor == null)
                {
                    _logService.LogWarning("Retrieved null cursor for hand state {HandState}, using system cursor", _currentHandState);
                    SetSystemCursor();
                    return;
                }

                ApplyCursor(cursor);
                _logService.LogDebug("Successfully applied cursor for hand state {HandState}", _currentHandState);
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory while updating cursor for hand state {HandState}, using system cursor", _currentHandState);
                SetSystemCursorSafe();
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation while updating cursor for hand state {HandState}, using system cursor", _currentHandState);
                SetSystemCursorSafe();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error updating cursor for hand state {HandState}, using system cursor", _currentHandState);
                SetSystemCursorSafe();
            }
        }

        #endregion UpdateCursorNow

        #region SetSystemCursorThrottled

        /// <summary>
        /// Sets the system default cursor using throttling
        /// </summary>
        private void SetSystemCursorThrottled()
        {
            try
            {
                // For system cursor, we can apply immediately since it's lightweight
                SetSystemCursor();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error setting system cursor");
            }
        }

        #endregion SetSystemCursorThrottled
    }
}
