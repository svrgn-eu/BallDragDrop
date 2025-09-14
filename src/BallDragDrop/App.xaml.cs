using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Bootstrapper;
using BallDragDrop.Views;

namespace BallDragDrop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    #region Fields

    /// <summary>
    /// Logging service for application-wide logging
    /// </summary>
    private ILogService _logService;
        
    /// <summary>
    /// Main window reference for the application
    /// </summary>
    private MainWindow _mainWindow;
    
    /// <summary>
    /// Settings manager for application configuration
    /// </summary>
    private SettingsManager _settingsManager;
    
    /// <summary>
    /// Configuration service for application configuration
    /// </summary>
    private IConfigurationService _configurationService;

    #endregion Fields
    
    #region Event Handlers

    /// <summary>
    /// Application startup event handler from XAML
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Initialize ServiceBootstrapper during application startup
        ServiceBootstrapper.Initialize();
        
        // Get logging service from dependency injection
        _logService = ServiceBootstrapper.GetService<ILogService>();
        
        // Log comprehensive startup information
        LogStartupInformation();
        
        // Set up global exception handling with injected services
        var exceptionHandlingService = ServiceBootstrapper.GetService<IExceptionHandlingService>();
        AppDomain.CurrentDomain.UnhandledException += (s, ex) => CurrentDomain_UnhandledException(s, ex, exceptionHandlingService);
        DispatcherUnhandledException += (s, ex) => App_DispatcherUnhandledException(s, ex, exceptionHandlingService);
        TaskScheduler.UnobservedTaskException += (s, ex) => TaskScheduler_UnobservedTaskException(s, ex, exceptionHandlingService);
        
        _logService.LogInformation("Global exception handlers configured with dependency injection");
        
        // Process command line arguments if any
        ProcessCommandLineArguments(e.Args);
        
        // Show splash screen and initialize application
        ShowSplashScreenAndInitialize();
    }
    
    #endregion Event Handlers

    #region Methods

    #region ShowSplashScreenAndInitialize
    /// <summary>
    /// Shows the splash screen and initializes the application
    /// </summary>
    private void ShowSplashScreenAndInitialize()
    {
        try
        {
            // Create and show the splash screen
            var splashScreen = new Views.SplashScreen();
            splashScreen.InitializationComplete += SplashScreen_InitializationComplete;
            splashScreen.Show();
            
            // Start initialization in the background
            Task.Run(() => 
            {
                try
                {
                    // Initialize application settings
                    InitializeSettings();
                    
                    // Initialize default ball image from configuration
                    InitializeDefaultBallImage();
                    
                    // Update splash screen status
                    splashScreen.UpdateStatus("Initialization complete");
                }
                catch (Exception ex)
                {
                    // Log any errors during initialization
                    _logService?.LogError(ex, "Failed to initialize application");
                    
                    // Update splash screen status
                    splashScreen.UpdateStatus("Initialization failed");
                }
            });
        }
        catch (Exception ex)
        {
            // Log any errors during splash screen creation
            _logService?.LogError(ex, "Failed to show splash screen");
            
            // Fall back to direct initialization and main window creation
            InitializeSettings();
            ShowMainWindow();
        }
    }
    #endregion ShowSplashScreenAndInitialize

    #region SplashScreen_InitializationComplete
    /// <summary>
    /// Event handler for splash screen initialization complete
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void SplashScreen_InitializationComplete(object sender, EventArgs e)
    {
        // Show the main window
        ShowMainWindow();
    }
    #endregion SplashScreen_InitializationComplete

    #region ShowMainWindow
    /// <summary>
    /// Shows the main window
    /// </summary>
    private void ShowMainWindow()
    {
        try
        {
            // Create and show the main window
            _mainWindow = new MainWindow();
            
            // Add debug output
            _logService?.LogDebug("MainWindow created successfully");
            
            _mainWindow.Show();
            
            // Add debug output
            _logService?.LogDebug("MainWindow.Show() called successfully");
            
            // Force the window to be visible and on top
            _mainWindow.Activate();
            _mainWindow.Focus();
            
            // Log successful startup
            _logService?.LogInformation("Application started successfully");
            
            // Add debug output
            _logService?.LogDebug("Application startup completed");
        }
        catch (Exception ex)
        {
            // Log any errors during main window creation
            _logService?.LogError(ex, "Failed to create main window");
            
            // Add debug output
            _logService?.LogError(ex, "Error creating main window");
            
            // Show error message to the user
            MessageBox.Show(
                $"Failed to start the application: {ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                
            // Shutdown the application
            Shutdown(1);
        }
    }
    #endregion ShowMainWindow

    #region Application_Exit
    /// <summary>
    /// Application exit event handler from XAML
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _logService?.LogInformation("Application shutdown initiated");
        
        // Save any application settings
        SaveSettings();
        
        // Clean up resources
        CleanupResources();
        
        // Log comprehensive shutdown information
        LogShutdownInformation(e.ApplicationExitCode);
        
        // Flush any pending log entries
        FlushLogs();
        
        // Ensure proper service disposal during application shutdown
        ServiceBootstrapper.Dispose();
    }
    #endregion Application_Exit

    #region ProcessCommandLineArguments
    /// <summary>
    /// Processes command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    private void ProcessCommandLineArguments(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            // Log the arguments
            _logService.LogInformation("Command line arguments received: {Args}", string.Join(", ", args));
            
            // Process specific arguments if needed
            // For example: debug mode, specific file to open, etc.
            foreach (string arg in args)
            {
                if (arg.Equals("--debug", StringComparison.OrdinalIgnoreCase))
                {
                    // Enable debug mode
                    _logService.LogInformation("Debug mode enabled via command line");
                }
            }
        }
        else
        {
            _logService.LogDebug("No command line arguments provided");
        }
    }
    #endregion ProcessCommandLineArguments

    #region CleanupResources
    /// <summary>
    /// Cleans up application resources before exit
    /// </summary>
    private void CleanupResources()
    {
        try
        {
            // Dispose of any resources that need explicit cleanup
            // For example: close file handles, release COM objects, etc.
            
            _logService?.LogInformation("Resources cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex, "Error during resource cleanup");
        }
    }
    #endregion CleanupResources

    #region App_DispatcherUnhandledException
    /// <summary>
    /// Handles unhandled exceptions in the UI thread
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    /// <param name="exceptionHandlingService">Injected exception handling service</param>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e, IExceptionHandlingService exceptionHandlingService)
    {
        // Use injected exception handling service
        exceptionHandlingService.HandleException(e.Exception, "UI thread exception");
        
        // Generate user-friendly error message
        var userMessage = exceptionHandlingService.GenerateUserFriendlyMessage(e.Exception);
        
        // Show error message to the user
        MessageBox.Show(
            $"{userMessage}\n\nThe application will continue running, but some features may not work correctly.",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        // Attempt recovery
        bool recovered = exceptionHandlingService.AttemptRecovery(e.Exception);
        _logService?.LogInformation("Recovery attempt result: {Recovered}", recovered);
        
        // Prevent the application from crashing
        e.Handled = true;
    }
    #endregion App_DispatcherUnhandledException

    #region CurrentDomain_UnhandledException
    /// <summary>
    /// Handles unhandled exceptions in non-UI threads
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    /// <param name="exceptionHandlingService">Injected exception handling service</param>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e, IExceptionHandlingService exceptionHandlingService)
    {
        var exception = e.ExceptionObject as Exception;
        
        if (e.IsTerminating)
        {
            // Capture application state for critical error reporting
            var applicationState = exceptionHandlingService.CaptureApplicationContext();
            exceptionHandlingService.ReportCriticalError(exception, applicationState);
            
            MessageBox.Show(
                "A fatal error has occurred and the application needs to close.",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else
        {
            // Handle non-terminal exceptions
            exceptionHandlingService.HandleException(exception, "Background thread exception");
        }
    }
    #endregion CurrentDomain_UnhandledException

    #region TaskScheduler_UnobservedTaskException
    /// <summary>
    /// Handles unobserved task exceptions
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    /// <param name="exceptionHandlingService">Injected exception handling service</param>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e, IExceptionHandlingService exceptionHandlingService)
    {
        // Use injected exception handling service
        exceptionHandlingService.HandleException(e.Exception, "Unobserved task exception");
        
        // Mark the exception as observed to prevent it from crashing the application
        e.SetObserved();
    }
    #endregion TaskScheduler_UnobservedTaskException

    #region LogStartupInformation
    /// <summary>
    /// Logs comprehensive startup information including version and configuration
    /// </summary>
    private void LogStartupInformation()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var location = assembly.Location;
            
            _logService.LogInformation("=== Application Starting ===");
            _logService.LogInformation("Application Version: {Version}", version);
            _logService.LogInformation("Assembly Location: {Location}", location);
            _logService.LogInformation("OS Version: {OSVersion}", Environment.OSVersion.ToString());
            _logService.LogInformation(".NET Version: {DotNetVersion}", Environment.Version.ToString());
            _logService.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            _logService.LogInformation("User Name: {UserName}", Environment.UserName);
            _logService.LogInformation("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
            _logService.LogInformation("Available Memory: {MemoryMB} MB", GC.GetTotalMemory(false) / (1024 * 1024));
            _logService.LogInformation("Startup Time: {StartupTime}", DateTime.Now);
            _logService.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex, "Failed to log startup information");
        }
    }
    #endregion LogStartupInformation

    #region LogShutdownInformation
    /// <summary>
    /// Logs comprehensive shutdown information
    /// </summary>
    /// <param name="exitCode">Application exit code</param>
    private void LogShutdownInformation(int exitCode)
    {
        try
        {
            _logService?.LogInformation("=== Application Shutting Down ===");
            _logService?.LogInformation("Exit Code: {ExitCode}", exitCode);
            _logService?.LogInformation("Shutdown Time: {ShutdownTime}", DateTime.Now);
            _logService?.LogInformation("Final Memory Usage: {MemoryMB} MB", GC.GetTotalMemory(false) / (1024 * 1024));
            
            // Force garbage collection to get accurate memory usage
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            _logService?.LogInformation("Memory After GC: {MemoryMB} MB", GC.GetTotalMemory(false) / (1024 * 1024));
            _logService?.LogInformation("=== Application Shutdown Complete ===");
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex, "Failed to log shutdown information");
        }
    }
    #endregion LogShutdownInformation

    #region FlushLogs
    /// <summary>
    /// Flushes any pending log entries
    /// </summary>
    private void FlushLogs()
    {
        try
        {
            // For the simple implementation, there's no explicit flush needed
            // This method is a placeholder for when Log4NET is implemented
            _logService?.LogDebug("Log flush completed");
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex, "Failed to flush logs");
        }
    }
    #endregion FlushLogs

    #region InitializeSettings
    /// <summary>
    /// Initializes application settings
    /// </summary>
    private void InitializeSettings()
    {
        try
        {
            // Get settings manager from dependency injection
            _settingsManager = ServiceBootstrapper.GetService<SettingsManager>();
            
            // Get configuration service from dependency injection
            _configurationService = ServiceBootstrapper.GetService<IConfigurationService>();
            
            // Initialize configuration first
            Task.Run(() =>
            {
                try
                {
                    _configurationService.Initialize();
                    _logService?.LogInformation("Configuration initialized successfully");
                }
                catch (Exception ex)
                {
                    _logService?.LogError(ex, "Failed to initialize configuration, using defaults");
                }
            });
            
            // Load existing settings
            bool settingsLoaded = _settingsManager.LoadSettings();
            
            // If settings were not loaded (first run or settings file deleted),
            // initialize with default settings
            if (!settingsLoaded)
            {
                // Set default settings
                _settingsManager.SetSetting("FirstRun", false);
                _settingsManager.SetSetting("LastRunDate", DateTime.Now);
                _settingsManager.SetSetting("WindowWidth", 800);
                _settingsManager.SetSetting("WindowHeight", 600);
                _settingsManager.SetSetting("BallRadius", 25);
                
                // Save the default settings
                _settingsManager.SaveSettings();
                
                _logService?.LogInformation("Default settings created");
            }
            else
            {
                // Update last run date
                _settingsManager.SetSetting("LastRunDate", DateTime.Now);
                
                // Increment run count
                int runCount = _settingsManager.GetSetting<int>("RunCount", 0);
                _settingsManager.SetSetting("RunCount", runCount + 1);
                
                _logService?.LogInformation("Settings loaded. Run count: {RunCount}", runCount + 1);
            }
            
            // Log successful initialization
            _logService?.LogInformation("Application settings and configuration initialized");
        }
        catch (Exception ex)
        {
            // Log any errors during initialization
            _logService?.LogError(ex, "Failed to initialize application settings");
        }
    }
    #endregion InitializeSettings

    #region SaveSettings
    /// <summary>
    /// Saves application settings
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            // Check if settings manager is initialized
            if (_settingsManager == null)
            {
                _logService?.LogWarning("Settings manager not initialized, skipping settings save");
                return;
            }
            
            // Save window size if main window exists
            if (_mainWindow != null)
            {
                _settingsManager.SetSetting("WindowWidth", _mainWindow.Width);
                _settingsManager.SetSetting("WindowHeight", _mainWindow.Height);
                
                // Save ball position if available
                if (_mainWindow.DataContext is BallDragDrop.ViewModels.MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    _settingsManager.SetSetting("BallX", ballViewModel.X);
                    _settingsManager.SetSetting("BallY", ballViewModel.Y);
                    _settingsManager.SetSetting("BallRadius", ballViewModel.Radius);
                }
            }
            
            // Save the settings
            bool saved = _settingsManager.SaveSettings();
            
            // Configuration is automatically saved by Config.Net when values change
            _logService?.LogInformation("Configuration will be automatically persisted by Config.Net");
            
            // Log result
            if (saved)
            {
                _logService?.LogInformation("Application settings saved successfully");
            }
            else
            {
                _logService?.LogWarning("Failed to save application settings");
            }
        }
        catch (Exception ex)
        {
            // Log any errors during save
            _logService?.LogError(ex, "Failed to save application settings");
        }
    }
    #endregion SaveSettings

    #region GetSettingsManager
    /// <summary>
    /// Gets the settings manager instance
    /// </summary>
    /// <returns>The settings manager instance</returns>
    public SettingsManager GetSettingsManager()
    {
        return _settingsManager;
    }
    #endregion GetSettingsManager

    #region GetLogService
    /// <summary>
    /// Gets the logging service instance
    /// </summary>
    /// <returns>The logging service instance</returns>
    public ILogService GetLogService()
    {
        return _logService;
    }
    #endregion GetLogService

    #region GetConfigurationService
    /// <summary>
    /// Gets the configuration service instance
    /// </summary>
    /// <returns>The configuration service instance</returns>
    public IConfigurationService GetConfigurationService()
    {
        return _configurationService;
    }
    #endregion GetConfigurationService

    #region InitializeDefaultBallImage
    /// <summary>
    /// Initializes the default ball image from configuration
    /// </summary>
    private void InitializeDefaultBallImage()
    {
        try
        {
            if (_configurationService == null)
            {
                _logService?.LogWarning("Configuration service not initialized, skipping default ball image setup");
                return;
            }
            
            var defaultImagePath = _configurationService.GetDefaultBallImagePath();
            _logService?.LogInformation("Default ball image path from configuration: {ImagePath}", defaultImagePath);
            
            // Validate the image path
            if (!_configurationService.ValidateImagePath(defaultImagePath))
            {
                _logService?.LogWarning("Default ball image path is invalid: {ImagePath}", defaultImagePath);
                
                // Try to set a fallback path
                var fallbackPath = "./Resources/Images/Ball01.png";
                if (_configurationService.ValidateImagePath(fallbackPath))
                {
                    _configurationService.SetDefaultBallImagePath(fallbackPath);
                    _logService?.LogInformation("Updated default ball image path to fallback: {FallbackPath}", fallbackPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logService?.LogError(ex, "Failed to initialize default ball image from configuration");
        }
    }
    #endregion InitializeDefaultBallImage

    #endregion Methods
}
