using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using Config.Net;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Service for managing application configuration using Config.Net
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        #region Fields


        
        private readonly string _configFilePath;
        private readonly ILogService _logService;
        private IAppConfiguration _configuration;

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ConfigurationService class
        /// </summary>
        /// <param name="logService">The logging service</param>
        public ConfigurationService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            using var scope = _logService?.BeginScope("ConfigurationService.Constructor");
            
            // Set up the configuration file path in the application directory
            var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
                              ?? Environment.CurrentDirectory;
            _configFilePath = Path.Combine(appDirectory, Constants.CONFIG_FILE_NAME);
            
            _logService?.LogDebug("ConfigurationService initialized with config file path: {ConfigFilePath}", _configFilePath);
        }

        /// <summary>
        /// Initializes a new instance of the ConfigurationService class with a custom config file path (for testing)
        /// </summary>
        /// <param name="logService">The logging service</param>
        /// <param name="configFilePath">The custom configuration file path</param>
        public ConfigurationService(ILogService logService, string configFilePath)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            
            using var scope = _logService?.BeginScope("ConfigurationService.Constructor", configFilePath);
            
            _logService?.LogDebug("ConfigurationService initialized with custom config file path: {ConfigFilePath}", _configFilePath);
        }

        #endregion Construction

        #region Properties

        /// <summary>
        /// Gets the application configuration
        /// </summary>
        public IAppConfiguration Configuration => _configuration;

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Initializes the configuration service using Config.Net
        /// </summary>
        public void Initialize()
        {
            _logService?.LogMethodEntry(nameof(Initialize));
            
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logService?.LogDebug("Created configuration directory: {Directory}", directory);
                }

                // Build configuration using Config.Net
                _configuration = new ConfigurationBuilder<IAppConfiguration>()
                    .UseJsonFile(_configFilePath)
                    .Build();
                
                _logService?.LogInformation("Configuration initialized successfully using Config.Net with file: {ConfigFilePath}", _configFilePath);
                _logService?.LogDebug("Default ball image path: {DefaultBallImagePath}", _configuration.DefaultBallImagePath);
                _logService?.LogDebug("Animations enabled: {EnableAnimations}", _configuration.EnableAnimations);
                _logService?.LogDebug("Default ball size: {DefaultBallSize}", _configuration.DefaultBallSize);
                
                _logService?.LogMethodExit(nameof(Initialize));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error initializing configuration from file: {ConfigFilePath}", _configFilePath);
                
                // Fallback: create a minimal configuration
                _configuration = new ConfigurationBuilder<IAppConfiguration>()
                    .Build();
                
                _logService?.LogWarning("Using fallback in-memory configuration due to initialization error");
                _logService?.LogMethodExit(nameof(Initialize));
            }
        }

        /// <summary>
        /// Gets the default ball image path from configuration
        /// </summary>
        /// <returns>The default ball image path</returns>
        public string GetDefaultBallImagePath()
        {
            var path = _configuration?.DefaultBallImagePath ?? Constants.DEFAULT_BALL_IMAGE_PATH;
            _logService?.LogTrace("Getting default ball image path: {Path}", path);
            return path;
        }

        /// <summary>
        /// Sets the default ball image path in configuration
        /// </summary>
        /// <param name="path">The path to set as default</param>
        public void SetDefaultBallImagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }
            
            if (_configuration == null)
            {
                _logService?.LogWarning("Configuration not initialized, cannot set default ball image path");
                return;
            }
            
            var oldPath = _configuration.DefaultBallImagePath;
            _configuration.DefaultBallImagePath = path;
            
            _logService?.LogDebug("Default ball image path changed from {OldPath} to {NewPath}", oldPath, path);
        }

        /// <summary>
        /// Validates if the specified image path exists and is accessible
        /// </summary>
        /// <param name="path">The image path to validate</param>
        /// <returns>True if the path is valid, false otherwise</returns>
        public bool ValidateImagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logService?.LogTrace("Image path validation failed: path is null or empty");
                return false;
            }
            
            try
            {
                // Convert relative paths to absolute paths
                var fullPath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
                
                // Check if file exists
                var exists = File.Exists(fullPath);
                _logService?.LogTrace("Image path validation for {Path}: {IsValid}", path, exists);
                
                if (exists)
                {
                    // Check if it's a supported image format
                    var extension = Path.GetExtension(fullPath).ToLowerInvariant();
                    var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                    var isSupported = Array.Exists(supportedExtensions, ext => ext == extension);
                    
                    _logService?.LogTrace("Image format validation for {Path} (extension: {Extension}): {IsSupported}", 
                        path, extension, isSupported);
                    
                    return isSupported;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error validating image path: {Path}", path);
                return false;
            }
        }

        /// <summary>
        /// Gets whether to show the ball's bounding box for debugging
        /// </summary>
        /// <returns>True if bounding box should be shown, false otherwise</returns>
        public bool GetShowBoundingBox()
        {
            var show = _configuration?.ShowBoundingBox ?? false;
            _logService?.LogTrace("Getting show bounding box setting: {Show}", show);
            return show;
        }

        /// <summary>
        /// Sets whether to show the ball's bounding box for debugging
        /// </summary>
        /// <param name="show">True to show bounding box, false to hide</param>
        public void SetShowBoundingBox(bool show)
        {
            if (_configuration == null)
            {
                _logService?.LogWarning("Configuration not initialized, cannot set show bounding box setting");
                return;
            }
            
            var oldValue = _configuration.ShowBoundingBox;
            _configuration.ShowBoundingBox = show;
            
            _logService?.LogDebug("Show bounding box setting changed from {OldValue} to {NewValue}", oldValue, show);
        }

        /// <summary>
        /// Gets the cursor configuration from settings
        /// </summary>
        /// <returns>The cursor configuration</returns>
        public CursorConfiguration GetCursorConfiguration()
        {
            _logService?.LogMethodEntry(nameof(GetCursorConfiguration));
            
            try
            {
                if (_configuration == null)
                {
                    _logService?.LogWarning("Configuration not initialized, returning default cursor configuration");
                    return GetDefaultCursorConfiguration();
                }

                var cursorConfig = new CursorConfiguration
                {
                    EnableCustomCursors = _configuration.CursorConfiguration_EnableCustomCursors,
                    DefaultCursorPath = _configuration.CursorConfiguration_DefaultCursorPath ?? "",
                    HoverCursorPath = _configuration.CursorConfiguration_HoverCursorPath ?? "",
                    GrabbingCursorPath = _configuration.CursorConfiguration_GrabbingCursorPath ?? "",
                    ReleasingCursorPath = _configuration.CursorConfiguration_ReleasingCursorPath ?? "",
                    DebounceTimeMs = _configuration.CursorConfiguration_DebounceTimeMs,
                    ReleasingDurationMs = _configuration.CursorConfiguration_ReleasingDurationMs
                };

                _logService?.LogDebug("Retrieved cursor configuration: EnableCustomCursors={EnableCustomCursors}, DebounceTimeMs={DebounceTimeMs}", 
                    cursorConfig.EnableCustomCursors, cursorConfig.DebounceTimeMs);
                
                _logService?.LogMethodExit(nameof(GetCursorConfiguration));
                return cursorConfig;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error retrieving cursor configuration");
                _logService?.LogMethodExit(nameof(GetCursorConfiguration));
                return GetDefaultCursorConfiguration();
            }
        }

        /// <summary>
        /// Validates the cursor configuration and returns validation results
        /// </summary>
        /// <param name="configuration">The cursor configuration to validate</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool ValidateCursorConfiguration(CursorConfiguration configuration)
        {
            _logService?.LogMethodEntry(nameof(ValidateCursorConfiguration));
            
            if (configuration == null)
            {
                _logService?.LogWarning("Cursor configuration is null");
                _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
                return false;
            }

            var isValid = true;

            // Validate debounce time
            if (configuration.DebounceTimeMs < 1 || configuration.DebounceTimeMs > 1000)
            {
                _logService?.LogWarning("Invalid debounce time: {DebounceTimeMs}ms. Must be between 1 and 1000ms", 
                    configuration.DebounceTimeMs);
                isValid = false;
            }

            // Validate releasing duration
            if (configuration.ReleasingDurationMs < 50 || configuration.ReleasingDurationMs > 5000)
            {
                _logService?.LogWarning("Invalid releasing duration: {ReleasingDurationMs}ms. Must be between 50 and 5000ms", 
                    configuration.ReleasingDurationMs);
                isValid = false;
            }

            // Validate cursor paths if custom cursors are enabled
            if (configuration.EnableCustomCursors)
            {
                var cursorPaths = new[]
                {
                    ("DefaultCursorPath", configuration.DefaultCursorPath),
                    ("HoverCursorPath", configuration.HoverCursorPath),
                    ("GrabbingCursorPath", configuration.GrabbingCursorPath),
                    ("ReleasingCursorPath", configuration.ReleasingCursorPath)
                };

                foreach (var (pathName, pathValue) in cursorPaths)
                {
                    if (string.IsNullOrWhiteSpace(pathValue))
                    {
                        _logService?.LogWarning("Cursor path {PathName} is null or empty", pathName);
                        isValid = false;
                        continue;
                    }

                    // Validate that the path has a PNG extension
                    if (!pathValue.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        _logService?.LogWarning("Cursor path {PathName} does not have .png extension: {PathValue}", 
                            pathName, pathValue);
                        isValid = false;
                    }

                    // Check if file exists (relative to application directory)
                    try
                    {
                        var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
                                          ?? Environment.CurrentDirectory;
                        var fullPath = Path.Combine(appDirectory, pathValue);
                        
                        if (!File.Exists(fullPath))
                        {
                            _logService?.LogWarning("Cursor file not found for {PathName}: {FullPath}", pathName, fullPath);
                            // Don't mark as invalid - file might be created later, just log warning
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService?.LogError(ex, "Error validating cursor path {PathName}: {PathValue}", pathName, pathValue);
                        isValid = false;
                    }
                }
            }

            _logService?.LogDebug("Cursor configuration validation result: {IsValid}", isValid);
            _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
            return isValid;
        }

        /// <summary>
        /// Gets the default cursor configuration with fallback values
        /// </summary>
        /// <returns>Default cursor configuration</returns>
        public CursorConfiguration GetDefaultCursorConfiguration()
        {
            _logService?.LogMethodEntry(nameof(GetDefaultCursorConfiguration));
            
            var defaultConfig = new CursorConfiguration
            {
                EnableCustomCursors = true,
                DefaultCursorPath = Constants.DEFAULT_CURSOR_PATH,
                HoverCursorPath = Constants.DEFAULT_HOVER_CURSOR_PATH,
                GrabbingCursorPath = Constants.DEFAULT_GRABBING_CURSOR_PATH,
                ReleasingCursorPath = Constants.DEFAULT_RELEASING_CURSOR_PATH,
                DebounceTimeMs = Constants.DEFAULT_CURSOR_DEBOUNCE_TIME_MS,
                ReleasingDurationMs = Constants.DEFAULT_CURSOR_RELEASING_DURATION_MS
            };

            _logService?.LogDebug("Created default cursor configuration");
            _logService?.LogMethodExit(nameof(GetDefaultCursorConfiguration));
            return defaultConfig;
        }

        #endregion Public Methods
    }
}
