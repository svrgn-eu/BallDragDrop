using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using Config.Net;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Service for managing application configuration using Config.Net
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        #region Fields

        private const string DefaultBallImagePath = "./Resources/Images/Ball01.png";
        private const string ConfigFileName = "appsettings.json";
        
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
            _configFilePath = Path.Combine(appDirectory, ConfigFileName);
            
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
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            _logService?.LogMethodEntry(nameof(InitializeAsync));
            
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
                
                _logService?.LogMethodExit(nameof(InitializeAsync));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error initializing configuration from file: {ConfigFilePath}", _configFilePath);
                
                // Fallback: create a minimal configuration
                _configuration = new ConfigurationBuilder<IAppConfiguration>()
                    .Build();
                
                _logService?.LogWarning("Using fallback in-memory configuration due to initialization error");
                _logService?.LogMethodExit(nameof(InitializeAsync));
            }
        }

        /// <summary>
        /// Gets the default ball image path from configuration
        /// </summary>
        /// <returns>The default ball image path</returns>
        public string GetDefaultBallImagePath()
        {
            var path = _configuration?.DefaultBallImagePath ?? DefaultBallImagePath;
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

        #endregion Public Methods
    }
}
