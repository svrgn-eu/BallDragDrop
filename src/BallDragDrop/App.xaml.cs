using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BallDragDrop.Services;
using BallDragDrop.Views;

namespace BallDragDrop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Application-wide logger for errors and diagnostics
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BallDragDrop",
        "logs",
        $"app_{DateTime.Now:yyyyMMdd}.log");
        
    // Main window reference
    private MainWindow _mainWindow;
    
    // Settings manager
    private SettingsManager _settingsManager;
    
    /// <summary>
    /// Application startup event handler from XAML
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        // Ensure log directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
        
        // Log application startup
        LogMessage("Application starting");
        
        // Process command line arguments if any
        ProcessCommandLineArguments(e.Args);
        
        // Show splash screen
        ShowSplashScreenAndInitialize();
    }
    
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
                    
                    // Update splash screen status
                    splashScreen.UpdateStatus("Initialization complete");
                }
                catch (Exception ex)
                {
                    // Log any errors during initialization
                    LogException("Failed to initialize application", ex);
                    
                    // Update splash screen status
                    splashScreen.UpdateStatus("Initialization failed");
                }
            });
        }
        catch (Exception ex)
        {
            // Log any errors during splash screen creation
            LogException("Failed to show splash screen", ex);
            
            // Fall back to direct initialization and main window creation
            InitializeSettings();
            ShowMainWindow();
        }
    }
    
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
    
    /// <summary>
    /// Shows the main window
    /// </summary>
    private void ShowMainWindow()
    {
        try
        {
            // Create and show the main window
            _mainWindow = new MainWindow();
            _mainWindow.Show();
            
            // Log successful startup
            LogMessage("Application started successfully");
        }
        catch (Exception ex)
        {
            // Log any errors during main window creation
            LogException("Failed to create main window", ex);
            
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
    
    /// <summary>
    /// Application exit event handler from XAML
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // Save any application settings
        SaveSettings();
        
        // Clean up resources
        CleanupResources();
        
        // Log application exit
        LogMessage($"Application exiting with code: {e.ApplicationExitCode}");
    }
    
    /// <summary>
    /// Processes command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    private void ProcessCommandLineArguments(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            // Log the arguments
            LogMessage($"Command line arguments: {string.Join(", ", args)}");
            
            // Process specific arguments if needed
            // For example: debug mode, specific file to open, etc.
            foreach (string arg in args)
            {
                if (arg.Equals("--debug", StringComparison.OrdinalIgnoreCase))
                {
                    // Enable debug mode
                    LogMessage("Debug mode enabled");
                }
            }
        }
    }
    
    /// <summary>
    /// Cleans up application resources before exit
    /// </summary>
    private void CleanupResources()
    {
        try
        {
            // Dispose of any resources that need explicit cleanup
            // For example: close file handles, release COM objects, etc.
            
            LogMessage("Resources cleaned up successfully");
        }
        catch (Exception ex)
        {
            LogException("Error during resource cleanup", ex);
        }
    }
    
    /// <summary>
    /// Handles unhandled exceptions in the UI thread
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log the exception
        LogException("Unhandled UI exception", e.Exception);
        
        // Show error message to the user
        MessageBox.Show(
            $"An unexpected error occurred: {e.Exception.Message}\n\nThe application will continue running, but some features may not work correctly.",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        // Prevent the application from crashing
        e.Handled = true;
    }
    
    /// <summary>
    /// Handles unhandled exceptions in non-UI threads
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log the exception
        LogException("Unhandled application exception", e.ExceptionObject as Exception);
        
        // For terminal exceptions, we can't prevent the application from crashing
        if (e.IsTerminating)
        {
            MessageBox.Show(
                "A fatal error has occurred and the application needs to close.",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handles unobserved task exceptions
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        // Log the exception
        LogException("Unobserved task exception", e.Exception);
        
        // Mark the exception as observed to prevent it from crashing the application
        e.SetObserved();
    }
    
    /// <summary>
    /// Logs an exception to the application log file
    /// </summary>
    /// <param name="message">Message describing the context of the exception</param>
    /// <param name="ex">The exception to log</param>
    private void LogException(string message, Exception ex)
    {
        try
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}: {ex?.Message}";
            if (ex != null)
            {
                logMessage += $"\n{ex.GetType().Name}: {ex.Message}";
                logMessage += $"\n{ex.StackTrace}";
                
                // Log inner exception if present
                if (ex.InnerException != null)
                {
                    logMessage += $"\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                    logMessage += $"\n{ex.InnerException.StackTrace}";
                }
            }
            
            // Write to log file
            File.AppendAllText(LogFilePath, logMessage + "\n\n");
            
            // Also output to debug console
            Debug.WriteLine(logMessage);
        }
        catch
        {
            // If logging fails, at least try to output to debug console
            Debug.WriteLine($"Failed to log exception: {ex?.Message}");
        }
    }
    
    /// <summary>
    /// Logs a message to the application log file
    /// </summary>
    /// <param name="message">The message to log</param>
    private void LogMessage(string message)
    {
        try
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}";
            
            // Write to log file
            File.AppendAllText(LogFilePath, logMessage + "\n");
            
            // Also output to debug console
            Debug.WriteLine(logMessage);
        }
        catch
        {
            // If logging fails, at least try to output to debug console
            Debug.WriteLine($"Failed to log message: {message}");
        }
    }
    
    /// <summary>
    /// Initializes application settings
    /// </summary>
    private void InitializeSettings()
    {
        try
        {
            // Create settings manager
            _settingsManager = new SettingsManager();
            
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
                
                LogMessage("Default settings created");
            }
            else
            {
                // Update last run date
                _settingsManager.SetSetting("LastRunDate", DateTime.Now);
                
                // Increment run count
                int runCount = _settingsManager.GetSetting<int>("RunCount", 0);
                _settingsManager.SetSetting("RunCount", runCount + 1);
                
                LogMessage($"Settings loaded. Run count: {runCount + 1}");
            }
            
            // Log successful initialization
            LogMessage("Application settings initialized");
        }
        catch (Exception ex)
        {
            // Log any errors during initialization
            LogException("Failed to initialize application settings", ex);
        }
    }
    
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
                LogMessage("Settings manager not initialized, skipping settings save");
                return;
            }
            
            // Save window size if main window exists
            if (_mainWindow != null)
            {
                _settingsManager.SetSetting("WindowWidth", _mainWindow.Width);
                _settingsManager.SetSetting("WindowHeight", _mainWindow.Height);
                
                // Save ball position if available
                if (_mainWindow.DataContext is BallDragDrop.ViewModels.BallViewModel viewModel)
                {
                    _settingsManager.SetSetting("BallX", viewModel.X);
                    _settingsManager.SetSetting("BallY", viewModel.Y);
                    _settingsManager.SetSetting("BallRadius", viewModel.Radius);
                }
            }
            
            // Save the settings
            bool saved = _settingsManager.SaveSettings();
            
            // Log result
            if (saved)
            {
                LogMessage("Application settings saved successfully");
            }
            else
            {
                LogMessage("Failed to save application settings");
            }
        }
        catch (Exception ex)
        {
            // Log any errors during save
            LogException("Failed to save application settings", ex);
        }
    }
    
    /// <summary>
    /// Gets the settings manager instance
    /// </summary>
    /// <returns>The settings manager instance</returns>
    public SettingsManager GetSettingsManager()
    {
        return _settingsManager;
    }
}
