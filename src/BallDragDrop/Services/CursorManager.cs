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
        /// Reloads cursor configuration from settings
        /// </summary>
        public async Task ReloadConfigurationAsync()
        {
            try
            {
                _logService.LogDebug("Reloading cursor configuration");
                
                // Clear cache to force reload of cursors
                ClearCache();
                
                // Reload configuration
                LoadConfiguration();
                
                // Reinitialize throttler with new configuration
                InitializeCursorUpdateThrottler();
                
                // Reapply current cursor
                SetCursorForHandState(_currentHandState);
                
                _logService.LogInformation("Cursor configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error reloading cursor configuration");
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
        /// Gets the cursor for the specified hand state, loading from cache or file
        /// </summary>
        /// <param name="handState">Hand state</param>
        /// <returns>Cursor for the hand state</returns>
        private Cursor GetCursorForHandState(HandState handState)
        {
            lock (_cacheLock)
            {
                // Check cache first
                if (_cursorCache.TryGetValue(handState, out var cachedCursor))
                {
                    _logService.LogDebug("Using cached cursor for hand state {HandState}", handState);
                    return cachedCursor;
                }

                // Load cursor from configuration
                var cursor = LoadCursorFromConfiguration(handState);
                
                // Cache the cursor
                _cursorCache[handState] = cursor;
                
                _logService.LogDebug("Loaded and cached cursor for hand state {HandState}", handState);
                return cursor;
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

            return _imageLoader.LoadPngAsCursorWithFallback(cursorPath, Cursors.Arrow);
        }

        #endregion LoadCursorFromConfiguration

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
        /// Applies the cursor to the main window
        /// </summary>
        /// <param name="cursor">Cursor to apply</param>
        private void ApplyCursor(Cursor cursor)
        {
            try
            {
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            Application.Current.MainWindow.Cursor = cursor;
                        }
                        catch (Exception ex)
                        {
                            _logService.LogError(ex, "Error applying cursor to main window");
                        }
                    });
                }
                else
                {
                    _logService.LogWarning("Main window not available for cursor application");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error in cursor application dispatcher");
            }
        }

        #endregion ApplyCursor

        #region SetSystemCursor

        /// <summary>
        /// Sets the system default cursor
        /// </summary>
        private void SetSystemCursor()
        {
            ApplyCursor(Cursors.Arrow);
        }

        #endregion SetSystemCursor

        #region LoadConfiguration

        /// <summary>
        /// Loads cursor configuration from the configuration service
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Configuration is now injected, so this method is mainly for future reload scenarios
                _logService.LogDebug("Cursor configuration loaded");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error loading cursor configuration");
                _currentConfiguration = new CursorConfiguration { EnableCustomCursors = false };
            }
        }

        #endregion LoadConfiguration

        #region ClearCache

        /// <summary>
        /// Clears the cursor cache
        /// </summary>
        private void ClearCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    // Dispose cached cursors if needed
                    foreach (var cursor in _cursorCache.Values)
                    {
                        // WPF cursors don't typically need explicit disposal
                        // but we clear the references
                    }
                    
                    _cursorCache.Clear();
                    _logService.LogDebug("Cursor cache cleared");
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Error clearing cursor cache");
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
        /// Updates the cursor immediately based on current hand state
        /// </summary>
        private void UpdateCursorNow()
        {
            try
            {
                if (_currentConfiguration?.EnableCustomCursors != true)
                {
                    SetSystemCursor();
                    return;
                }

                var cursor = GetCursorForHandState(_currentHandState);
                ApplyCursor(cursor);
                
                _logService.LogDebug("Applied cursor for hand state {HandState}", _currentHandState);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error updating cursor for hand state {HandState}", _currentHandState);
                SetSystemCursor();
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