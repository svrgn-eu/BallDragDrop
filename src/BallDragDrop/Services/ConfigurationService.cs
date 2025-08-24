using System;
using System.Collections.Generic;
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
        /// Gets the cursor configuration from settings with comprehensive error handling and validation
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

                CursorConfiguration cursorConfig;
                
                try
                {
                    cursorConfig = new CursorConfiguration
                    {
                        EnableCustomCursors = _configuration.CursorConfiguration_EnableCustomCursors,
                        DefaultCursorPath = _configuration.CursorConfiguration_DefaultCursorPath ?? "",
                        HoverCursorPath = _configuration.CursorConfiguration_HoverCursorPath ?? "",
                        GrabbingCursorPath = _configuration.CursorConfiguration_GrabbingCursorPath ?? "",
                        ReleasingCursorPath = _configuration.CursorConfiguration_ReleasingCursorPath ?? "",
                        DebounceTimeMs = _configuration.CursorConfiguration_DebounceTimeMs,
                        ReleasingDurationMs = _configuration.CursorConfiguration_ReleasingDurationMs
                    };
                }
                catch (Exception configEx)
                {
                    _logService?.LogError(configEx, "Error reading cursor configuration properties, using default configuration");
                    return GetDefaultCursorConfiguration();
                }

                // Validate the configuration and apply corrections if needed
                var validatedConfig = ValidateAndCorrectCursorConfiguration(cursorConfig);

                _logService?.LogDebug("Retrieved cursor configuration: EnableCustomCursors={EnableCustomCursors}, DebounceTimeMs={DebounceTimeMs}", 
                    validatedConfig.EnableCustomCursors, validatedConfig.DebounceTimeMs);
                
                _logService?.LogMethodExit(nameof(GetCursorConfiguration));
                return validatedConfig;
            }
            catch (OutOfMemoryException ex)
            {
                _logService?.LogError(ex, "Out of memory retrieving cursor configuration, using default configuration");
                _logService?.LogMethodExit(nameof(GetCursorConfiguration));
                return GetDefaultCursorConfiguration();
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Unexpected error retrieving cursor configuration, using default configuration");
                _logService?.LogMethodExit(nameof(GetCursorConfiguration));
                return GetDefaultCursorConfiguration();
            }
        }

        /// <summary>
        /// Validates the cursor configuration with comprehensive error handling and detailed validation results
        /// </summary>
        /// <param name="configuration">The cursor configuration to validate</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool ValidateCursorConfiguration(CursorConfiguration configuration)
        {
            _logService?.LogMethodEntry(nameof(ValidateCursorConfiguration));
            
            try
            {
                if (configuration == null)
                {
                    _logService?.LogError("Cursor configuration is null - this indicates a critical configuration error");
                    _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
                    return false;
                }

                var isValid = true;
                var validationErrors = new List<string>();

                // Validate debounce time with detailed error reporting
                try
                {
                    if (configuration.DebounceTimeMs < 1)
                    {
                        var error = $"Debounce time too low: {configuration.DebounceTimeMs}ms. Must be at least 1ms";
                        validationErrors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }
                    else if (configuration.DebounceTimeMs > 1000)
                    {
                        var error = $"Debounce time too high: {configuration.DebounceTimeMs}ms. Must be at most 1000ms";
                        validationErrors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }
                    else if (configuration.DebounceTimeMs > 100)
                    {
                        _logService?.LogWarning("High debounce time ({DebounceTimeMs}ms) may cause sluggish cursor response", 
                            configuration.DebounceTimeMs);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error validating debounce time: {ex.Message}";
                    validationErrors.Add(error);
                    _logService?.LogError(ex, "Error validating debounce time");
                    isValid = false;
                }

                // Validate releasing duration with detailed error reporting
                try
                {
                    if (configuration.ReleasingDurationMs < 50)
                    {
                        var error = $"Releasing duration too short: {configuration.ReleasingDurationMs}ms. Must be at least 50ms";
                        validationErrors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }
                    else if (configuration.ReleasingDurationMs > 5000)
                    {
                        var error = $"Releasing duration too long: {configuration.ReleasingDurationMs}ms. Must be at most 5000ms";
                        validationErrors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }
                    else if (configuration.ReleasingDurationMs > 1000)
                    {
                        _logService?.LogWarning("Long releasing duration ({ReleasingDurationMs}ms) may feel unresponsive", 
                            configuration.ReleasingDurationMs);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Error validating releasing duration: {ex.Message}";
                    validationErrors.Add(error);
                    _logService?.LogError(ex, "Error validating releasing duration");
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
                        try
                        {
                            var pathValidationResult = ValidateCursorPath(pathName, pathValue);
                            if (!pathValidationResult.IsValid)
                            {
                                validationErrors.AddRange(pathValidationResult.Errors);
                                isValid = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = $"Error validating cursor path {pathName}: {ex.Message}";
                            validationErrors.Add(error);
                            _logService?.LogError(ex, "Error validating cursor path {PathName}: {PathValue}", pathName, pathValue);
                            isValid = false;
                        }
                    }
                }
                else
                {
                    _logService?.LogDebug("Custom cursors disabled, skipping cursor path validation");
                }

                // Log validation summary
                if (validationErrors.Count > 0)
                {
                    _logService?.LogWarning("Cursor configuration validation failed with {ErrorCount} errors: {Errors}", 
                        validationErrors.Count, string.Join("; ", validationErrors));
                }

                _logService?.LogDebug("Cursor configuration validation result: {IsValid} (Errors: {ErrorCount})", 
                    isValid, validationErrors.Count);
                _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
                return isValid;
            }
            catch (OutOfMemoryException ex)
            {
                _logService?.LogError(ex, "Out of memory during cursor configuration validation");
                _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
                return false;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Unexpected error during cursor configuration validation");
                _logService?.LogMethodExit(nameof(ValidateCursorConfiguration));
                return false;
            }
        }

        /// <summary>
        /// Gets the default cursor configuration with fallback values
        /// </summary>
        /// <returns>Default cursor configuration</returns>
        public CursorConfiguration GetDefaultCursorConfiguration()
        {
            _logService?.LogMethodEntry(nameof(GetDefaultCursorConfiguration));
            
            try
            {
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

                _logService?.LogDebug("Created default cursor configuration with EnableCustomCursors={EnableCustomCursors}, DebounceTimeMs={DebounceTimeMs}", 
                    defaultConfig.EnableCustomCursors, defaultConfig.DebounceTimeMs);
                _logService?.LogMethodExit(nameof(GetDefaultCursorConfiguration));
                return defaultConfig;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error creating default cursor configuration, using minimal fallback");
                _logService?.LogMethodExit(nameof(GetDefaultCursorConfiguration));
                
                // Return minimal fallback configuration
                return new CursorConfiguration
                {
                    EnableCustomCursors = false, // Disable custom cursors if we can't create proper defaults
                    DefaultCursorPath = "",
                    HoverCursorPath = "",
                    GrabbingCursorPath = "",
                    ReleasingCursorPath = "",
                    DebounceTimeMs = 16,
                    ReleasingDurationMs = 200
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates and corrects cursor configuration, applying fixes where possible
        /// </summary>
        /// <param name="configuration">Configuration to validate and correct</param>
        /// <returns>Corrected configuration</returns>
        private CursorConfiguration ValidateAndCorrectCursorConfiguration(CursorConfiguration configuration)
        {
            try
            {
                _logService?.LogDebug("Validating and correcting cursor configuration");
                
                if (configuration == null)
                {
                    _logService?.LogError("Configuration is null, returning default configuration");
                    return GetDefaultCursorConfiguration();
                }

                var correctedConfig = new CursorConfiguration
                {
                    EnableCustomCursors = configuration.EnableCustomCursors,
                    DefaultCursorPath = configuration.DefaultCursorPath ?? "",
                    HoverCursorPath = configuration.HoverCursorPath ?? "",
                    GrabbingCursorPath = configuration.GrabbingCursorPath ?? "",
                    ReleasingCursorPath = configuration.ReleasingCursorPath ?? "",
                    DebounceTimeMs = configuration.DebounceTimeMs,
                    ReleasingDurationMs = configuration.ReleasingDurationMs
                };

                var correctionsMade = 0;

                // Correct debounce time
                if (correctedConfig.DebounceTimeMs < 1)
                {
                    _logService?.LogWarning("Correcting debounce time from {OldValue}ms to {NewValue}ms", 
                        correctedConfig.DebounceTimeMs, Constants.DEFAULT_CURSOR_DEBOUNCE_TIME_MS);
                    correctedConfig.DebounceTimeMs = Constants.DEFAULT_CURSOR_DEBOUNCE_TIME_MS;
                    correctionsMade++;
                }
                else if (correctedConfig.DebounceTimeMs > 1000)
                {
                    _logService?.LogWarning("Correcting debounce time from {OldValue}ms to {NewValue}ms", 
                        correctedConfig.DebounceTimeMs, 100);
                    correctedConfig.DebounceTimeMs = 100;
                    correctionsMade++;
                }

                // Correct releasing duration
                if (correctedConfig.ReleasingDurationMs < 50)
                {
                    _logService?.LogWarning("Correcting releasing duration from {OldValue}ms to {NewValue}ms", 
                        correctedConfig.ReleasingDurationMs, Constants.DEFAULT_CURSOR_RELEASING_DURATION_MS);
                    correctedConfig.ReleasingDurationMs = Constants.DEFAULT_CURSOR_RELEASING_DURATION_MS;
                    correctionsMade++;
                }
                else if (correctedConfig.ReleasingDurationMs > 5000)
                {
                    _logService?.LogWarning("Correcting releasing duration from {OldValue}ms to {NewValue}ms", 
                        correctedConfig.ReleasingDurationMs, 1000);
                    correctedConfig.ReleasingDurationMs = 1000;
                    correctionsMade++;
                }

                // Correct cursor paths if custom cursors are enabled
                if (correctedConfig.EnableCustomCursors)
                {
                    var pathCorrections = CorrectCursorPaths(correctedConfig);
                    correctionsMade += pathCorrections;
                }

                if (correctionsMade > 0)
                {
                    _logService?.LogInformation("Applied {CorrectionCount} corrections to cursor configuration", correctionsMade);
                }

                return correctedConfig;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error validating and correcting cursor configuration, returning default");
                return GetDefaultCursorConfiguration();
            }
        }

        /// <summary>
        /// Validates a single cursor path
        /// </summary>
        /// <param name="pathName">Name of the path for logging</param>
        /// <param name="pathValue">Path value to validate</param>
        /// <returns>Validation result</returns>
        private (bool IsValid, List<string> Errors) ValidateCursorPath(string pathName, string pathValue)
        {
            var errors = new List<string>();
            var isValid = true;

            try
            {
                if (string.IsNullOrWhiteSpace(pathValue))
                {
                    var error = $"Cursor path {pathName} is null or empty";
                    errors.Add(error);
                    _logService?.LogWarning(error);
                    return (false, errors);
                }

                // Validate path format
                try
                {
                    if (pathValue.Length > 260) // Windows MAX_PATH
                    {
                        var error = $"Cursor path {pathName} too long ({pathValue.Length} characters): {pathValue}";
                        errors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }

                    // Check for invalid path characters
                    var invalidChars = Path.GetInvalidPathChars();
                    if (pathValue.IndexOfAny(invalidChars) >= 0)
                    {
                        var error = $"Cursor path {pathName} contains invalid characters: {pathValue}";
                        errors.Add(error);
                        _logService?.LogWarning(error);
                        isValid = false;
                    }
                }
                catch (Exception pathEx)
                {
                    var error = $"Error validating path format for {pathName}: {pathEx.Message}";
                    errors.Add(error);
                    _logService?.LogError(pathEx, "Error validating path format for {PathName}: {PathValue}", pathName, pathValue);
                    isValid = false;
                }

                // Validate PNG extension
                if (!pathValue.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var error = $"Cursor path {pathName} does not have .png extension: {pathValue}";
                    errors.Add(error);
                    _logService?.LogWarning(error);
                    isValid = false;
                }

                // Check file existence and accessibility
                try
                {
                    var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
                                      ?? Environment.CurrentDirectory;
                    var fullPath = Path.Combine(appDirectory, pathValue);
                    
                    if (!File.Exists(fullPath))
                    {
                        var warning = $"Cursor file not found for {pathName}: {fullPath}";
                        _logService?.LogWarning(warning);
                        // Don't mark as invalid - file might be created later
                    }
                    else
                    {
                        // Check file accessibility
                        try
                        {
                            using (var testStream = File.OpenRead(fullPath))
                            {
                                // File is accessible
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            var error = $"Access denied to cursor file {pathName}: {fullPath}";
                            errors.Add(error);
                            _logService?.LogWarning(error);
                            isValid = false;
                        }
                        catch (IOException ioEx)
                        {
                            var error = $"IO error accessing cursor file {pathName}: {ioEx.Message}";
                            errors.Add(error);
                            _logService?.LogWarning(error);
                            isValid = false;
                        }
                    }
                }
                catch (Exception fileEx)
                {
                    var error = $"Error checking cursor file {pathName}: {fileEx.Message}";
                    errors.Add(error);
                    _logService?.LogError(fileEx, "Error checking cursor file {PathName}: {PathValue}", pathName, pathValue);
                    isValid = false;
                }

                return (isValid, errors);
            }
            catch (Exception ex)
            {
                var error = $"Unexpected error validating cursor path {pathName}: {ex.Message}";
                errors.Add(error);
                _logService?.LogError(ex, "Unexpected error validating cursor path {PathName}: {PathValue}", pathName, pathValue);
                return (false, errors);
            }
        }

        /// <summary>
        /// Corrects cursor paths in the configuration
        /// </summary>
        /// <param name="configuration">Configuration to correct</param>
        /// <returns>Number of corrections made</returns>
        private int CorrectCursorPaths(CursorConfiguration configuration)
        {
            var corrections = 0;

            try
            {
                // Correct empty or null paths
                if (string.IsNullOrWhiteSpace(configuration.DefaultCursorPath))
                {
                    _logService?.LogWarning("Correcting empty DefaultCursorPath to default value");
                    configuration.DefaultCursorPath = Constants.DEFAULT_CURSOR_PATH;
                    corrections++;
                }

                if (string.IsNullOrWhiteSpace(configuration.HoverCursorPath))
                {
                    _logService?.LogWarning("Correcting empty HoverCursorPath to default value");
                    configuration.HoverCursorPath = Constants.DEFAULT_HOVER_CURSOR_PATH;
                    corrections++;
                }

                if (string.IsNullOrWhiteSpace(configuration.GrabbingCursorPath))
                {
                    _logService?.LogWarning("Correcting empty GrabbingCursorPath to default value");
                    configuration.GrabbingCursorPath = Constants.DEFAULT_GRABBING_CURSOR_PATH;
                    corrections++;
                }

                if (string.IsNullOrWhiteSpace(configuration.ReleasingCursorPath))
                {
                    _logService?.LogWarning("Correcting empty ReleasingCursorPath to default value");
                    configuration.ReleasingCursorPath = Constants.DEFAULT_RELEASING_CURSOR_PATH;
                    corrections++;
                }

                return corrections;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error correcting cursor paths");
                return corrections;
            }
        }

        #endregion Private Helper Methods

        #endregion Public Methods
    }
}
