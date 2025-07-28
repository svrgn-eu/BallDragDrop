using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Data;
using System.Globalization;
using BallDragDrop.Bootstrapper;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.ViewModels;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace BallDragDrop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region Properties

    /// <summary>
    /// Event that will be raised when the ball position changes due to window resize
    /// </summary>
    public event EventHandler<Point>? BallPositionChanged;

    #endregion Properties

    #region Fields

    /// <summary>
    /// Logging service for the MainWindow
    /// </summary>
    private readonly ILogService _logService;

    #endregion Fields

    #region Construction

    /// <summary>
    /// Initializes a new instance of the MainWindow class
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Get logging service from dependency injection
        _logService = ServiceBootstrapper.GetService<ILogService>();
        
        // Initialize physics engine
        _physicsEngine = new Models.PhysicsEngine();
        _lastPhysicsUpdate = DateTime.Now;
        _isPhysicsRunning = false;
        
        // Get performance monitor from dependency injection
        _performanceMonitor = ServiceBootstrapper.GetService<Services.PerformanceMonitor>();
        
        // Set up event handlers
        this.SizeChanged += Window_SizeChanged;
        this.Loaded += Window_Loaded;
        this.Closed += MainWindow_Closed;
        
        // Initialize optimized dual timer system
        InitializeOptimizedTimers();
        
        // Initialize DataContext immediately in constructor
        InitializeDataContext();
        
        _logService.LogDebug("MainWindow initialized");
    }
    
    /// <summary>
    /// Initializes the DataContext with MainWindowViewModel
    /// </summary>
    private void InitializeDataContext()
    {
        try
        {
            _logService.LogDebug("InitializeDataContext started");
            Console.WriteLine("InitializeDataContext started");
            
            // Create a new MainWindowViewModel using dependency injection
            MainWindowViewModel mainViewModel = ServiceBootstrapper.GetService<MainWindowViewModel>();
            
            _logService.LogDebug("MainWindowViewModel created successfully");
            Console.WriteLine("MainWindowViewModel created successfully");
            
            // Set the DataContext for the window
            this.DataContext = mainViewModel;
            
            _logService.LogDebug("DataContext set successfully");
            
            // Initialize the ball position (will be updated in Window_Loaded)
            mainViewModel.BallViewModel.Initialize(400, 300, 25);
            
            _logService.LogDebug("InitializeDataContext completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, "Error in InitializeDataContext");
            
            // Show error message
            MessageBox.Show($"Error initializing DataContext: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion Construction

    #region Fields

    /// <summary>
    /// Physics engine for ball movement
    /// </summary>
    private Models.PhysicsEngine _physicsEngine;
    
    /// <summary>
    /// Last time the physics was updated
    /// </summary>
    private DateTime _lastPhysicsUpdate;
    
    /// <summary>
    /// Flag to track if physics simulation is running
    /// </summary>
    private bool _isPhysicsRunning;
    
    /// <summary>
    /// Performance monitoring and optimization
    /// </summary>
    private Services.PerformanceMonitor _performanceMonitor;
    
    /// <summary>
    /// Debug counter for physics updates
    /// </summary>
    private int _physicsUpdateCounter = 0;
    
    /// <summary>
    /// High-precision timer for physics updates (60 FPS)
    /// </summary>
    private DispatcherTimer _physicsTimer;
    
    /// <summary>
    /// Target physics update interval (60 FPS = ~16.67ms)
    /// </summary>
    private readonly TimeSpan _physicsUpdateInterval = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    
    /// <summary>
    /// Flag to track if we're using optimized dual timer system
    /// </summary>
    private bool _useOptimizedTimers = true;

    /// <summary>
    /// Array storing mouse position history for velocity calculation
    /// </summary>
    private Point[] _mousePositionHistory = new Point[10];

    /// <summary>
    /// Array storing mouse timestamp history for velocity calculation
    /// </summary>
    private DateTime[] _mouseTimestampHistory = new DateTime[10];

    /// <summary>
    /// Number of valid entries in the mouse history arrays
    /// </summary>
    private int _mouseHistoryCount = 0;

    /// <summary>
    /// Last recorded mouse position for drag operations
    /// </summary>
    private Point _lastMousePosition;

    /// <summary>
    /// Timestamp of the last mouse update
    /// </summary>
    private DateTime _lastMouseUpdateTime;

    #endregion Fields
    
    #region Event Handlers

    /// <summary>
    /// Event handler for window closed event
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void MainWindow_Closed(object sender, EventArgs e)
    {
        // Clean up timers and event handlers
        CleanupOptimizedTimers();
    }
    
    /// <summary>
    /// Event handler for window loaded event
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogDebug("Window_Loaded started");
            
            // Initialize canvas dimensions to match the window's client area minus UI chrome
            MainCanvas.Width = this.ActualWidth;
            MainCanvas.Height = this.ActualHeight - Constants.UI_CHROME_HEIGHT_OFFSET;
            
            _logService.LogDebug("Canvas dimensions: {Width} x {Height}", MainCanvas.Width, MainCanvas.Height);
            
            // Initialize the BallViewModel with the center position of the canvas
            double centerX = MainCanvas.Width / 2;
            double centerY = MainCanvas.Height / 2;
            double ballRadius = 25; // Default radius
            
            _logService.LogDebug("Creating MainWindowViewModel...");
            
            // Create a new MainWindowViewModel using dependency injection
            MainWindowViewModel mainViewModel = ServiceBootstrapper.GetService<MainWindowViewModel>();
            
            _logService.LogDebug("MainWindowViewModel created successfully");
            Console.WriteLine("MainWindowViewModel created successfully");
        
        // Initialize the ball position
        mainViewModel.BallViewModel.Initialize(centerX, centerY, ballRadius);
        
        // Load the ball image using the configuration service
        _ = Task.Run(async () =>
        {
            try
            {
                var logService = ServiceBootstrapper.GetService<ILogService>();
                
                // Debug: Log current working directory and base directory
                logService?.LogDebug("Current Directory: {CurrentDir}", Environment.CurrentDirectory);
                logService?.LogDebug("Base Directory: {BaseDir}", AppDomain.CurrentDomain.BaseDirectory);
                
                // Use the BallViewModel's LoadDefaultBallImageAsync method which properly uses configuration
                bool success = await mainViewModel.BallViewModel.LoadDefaultBallImageAsync();
                
                if (!success)
                {
                    logService?.LogWarning("Failed to load default ball image from configuration, trying fallback paths");
                    
                    // Try multiple fallback paths
                    string[] fallbackPaths = {
                        "Resources/Ball/Ball01.png",
                        "../../Resources/Ball/Ball01.png",
                        "../../../Resources/Ball/Ball01.png",
                        "../../../../Resources/Ball/Ball01.png",
                        "../../../../../Resources/Ball/Ball01.png",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "Ball", "Ball01.png"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Ball", "Ball01.png")
                    };
                    
                    foreach (var fallbackPath in fallbackPaths)
                    {
                        var fullPath = Path.GetFullPath(fallbackPath);
                        logService?.LogDebug("Trying fallback path: {FallbackPath} -> {FullPath}, Exists: {Exists}", 
                            fallbackPath, fullPath, File.Exists(fullPath));
                        
                        if (File.Exists(fullPath))
                        {
                            success = await mainViewModel.BallViewModel.LoadBallVisualAsync(fullPath);
                            if (success)
                            {
                                logService?.LogInformation("Successfully loaded ball image from fallback path: {Path}", fullPath);
                                break;
                            }
                        }
                    }
                    
                    if (!success)
                    {
                        logService?.LogWarning("Failed to load ball visual from all paths, fallback image should be displayed");
                    }
                }
                else
                {
                    logService?.LogInformation("Successfully loaded ball image from configuration");
                }
            }
            catch (Exception ex)
            {
                // Get logging service from dependency injection
                var logService = ServiceBootstrapper.GetService<ILogService>();
                logService?.LogError(ex, "Error during ball visual loading");
                
                // The ImageService should have already provided a fallback image
            }
        });
        
        // Set the DataContext for the window
        _logService.LogDebug("Setting DataContext...");
        
        this.DataContext = mainViewModel;
        
        _logService.LogDebug("DataContext set successfully");
        
        // Enable hardware rendering if supported
        if (RenderCapability.Tier > 0)
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            
            // Check if hardware acceleration is available
            bool isHardwareAccelerated = RenderCapability.IsPixelShaderVersionSupported(2, 0);
            _logService.LogDebug("Hardware acceleration available: {IsHardwareAccelerated}", isHardwareAccelerated);
            
            // Set rendering tier information for debugging
            _logService.LogDebug("Rendering Tier: {RenderingTier}", RenderCapability.Tier);
        }
        
        _logService.LogDebug("Window loaded");
        _logService.LogDebug("Window_Loaded completed successfully");
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, "Error in Window_Loaded");
            
            // Show error message
            MessageBox.Show($"Error initializing window: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for window resize events
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update canvas dimensions minus UI chrome heights
        MainCanvas.Width = e.NewSize.Width;
        MainCanvas.Height = e.NewSize.Height - Constants.UI_CHROME_HEIGHT_OFFSET;
        
        // Get the MainWindowViewModel from the DataContext
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Store the ball's relative position (as a percentage of the window size)
            // before resizing to maintain its relative position
            double relativeX = ballViewModel.X / e.PreviousSize.Width;
            double relativeY = ballViewModel.Y / e.PreviousSize.Height;
            
            // Only apply relative positioning if we have valid previous dimensions
            // and the ball was not at the edge of the window
            if (!double.IsNaN(relativeX) && !double.IsInfinity(relativeX) && 
                !double.IsNaN(relativeY) && !double.IsInfinity(relativeY) &&
                e.PreviousSize.Width > 0 && e.PreviousSize.Height > 0)
            {
                // Calculate new position based on relative position
                double newX = relativeX * e.NewSize.Width;
                double newY = relativeY * e.NewSize.Height;
                
                // Update the ball position
                ballViewModel.X = newX;
                ballViewModel.Y = newY;
            }
            
            // Constrain the ball position to the new window boundaries
            // This ensures the ball stays within the window even after applying relative positioning
            ballViewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
            
            // Raise the BallPositionChanged event with the new position
            BallPositionChanged?.Invoke(this, new Point(ballViewModel.X, ballViewModel.Y));
        }
    }
    
    /// <summary>
    /// Event handler for mouse down on the ball image
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void BallImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Get the position of the mouse click relative to the canvas (not the window)
            var position = e.GetPosition(MainCanvas);
            
            // Check if the click is inside the ball
            if (ballViewModel._ballModel.ContainsPoint(position.X, position.Y))
            {
                // Stop the physics simulation first
                _isPhysicsRunning = false;
                ballViewModel._ballModel.Stop();
                
                // Start dragging
                ballViewModel.IsDragging = true;
                
                // Initialize mouse tracking
                _lastMousePosition = position;
                _lastMouseUpdateTime = DateTime.Now;
                _mouseHistoryCount = 0;
                
                // Store initial position in history
                StoreMousePosition(position, _lastMouseUpdateTime);
                
                // Capture the mouse
                Mouse.Capture((IInputElement)sender);
                
                // Immediately update the ball position to the mouse position
                // This ensures the ball appears under the cursor right away
                UpdateBallPositionToMouse(ballViewModel, position);
                
                // Update cursor
                UpdateCursorForPosition(ballViewModel, position);
                
                _logService.LogDebug("Ball grabbed at position ({X:F2}, {Y:F2})", position.X, position.Y);
            }
        }
    }
    
    /// <summary>
    /// Event handler for mouse move on the ball image
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void BallImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Get canvas-relative position
            var canvasPosition = e.GetPosition(MainCanvas);
            var currentTime = DateTime.Now;
            
            // Update cursor based on canvas-relative position
            UpdateCursorForPosition(ballViewModel, canvasPosition);
            
            // If the ball is being dragged, ensure it follows the mouse cursor immediately
            if (ballViewModel.IsDragging)
            {
                // Store mouse position in history for velocity calculation
                StoreMousePosition(canvasPosition, currentTime);
                
                // Update ball position
                UpdateBallPositionToMouse(ballViewModel, canvasPosition);
                
                // Update tracking variables
                _lastMousePosition = canvasPosition;
                _lastMouseUpdateTime = currentTime;
            }
        }
    }
    
    /// <summary>
    /// Event handler for mouse up on the ball image
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void BallImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            if (ballViewModel.IsDragging)
            {
                // Stop dragging
                ballViewModel.IsDragging = false;
                
                // Release mouse capture
                Mouse.Capture(null);
                
                // Get canvas-relative position
                var position = e.GetPosition(MainCanvas);
                var currentTime = DateTime.Now;
                
                // Store final position in history
                StoreMousePosition(position, currentTime);
                
                // Calculate velocity based on movement history
                var (velocityX, velocityY) = CalculateVelocityFromHistory();
                
                // Check if the movement is fast enough to be considered a throw
                double throwThreshold = 100.0;
                if (Math.Abs(velocityX) > throwThreshold || Math.Abs(velocityY) > throwThreshold)
                {
                    // Apply the velocity to the ball model
                    ballViewModel._ballModel.SetVelocity(velocityX, velocityY);
                    
                    // Start the physics simulation
                    _isPhysicsRunning = true;
                    _physicsUpdateCounter = 0;
                    
                    if (_useOptimizedTimers)
                    {
                        StartPhysicsTimer();
                    }
                    else
                    {
                        _lastPhysicsUpdate = DateTime.Now;
                    }
                    
                    _logService.LogDebug("Ball thrown with velocity: ({VelocityX:F2}, {VelocityY:F2})", velocityX, velocityY);
                }
                else
                {
                    // Not a throw, stop the ball
                    ballViewModel._ballModel.Stop();
                    _logService.LogDebug("Ball dropped (velocity too low for throw): ({VelocityX:F2}, {VelocityY:F2})", velocityX, velocityY);
                }
                
                // Update cursor
                UpdateCursorForPosition(ballViewModel, position);
                
                _logService.LogDebug("Ball released at position ({X:F2}, {Y:F2})", position.X, position.Y);
            }
        }
    }

    /// <summary>
    /// Event handler for composition target rendering
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        // Start measuring frame time
        _performanceMonitor.BeginFrameTime();
        
        // Get current time for physics calculations
        DateTime currentTime = DateTime.Now;
        
        // Get the MainWindowViewModel from the DataContext
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // If the ball is being dragged, ensure it follows the mouse cursor
            if (ballViewModel.IsDragging && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = Mouse.GetPosition(MainCanvas);
                UpdateBallPositionToMouse(ballViewModel, mousePosition);
                
                // Coordinate animation timing with physics updates during drag
                ballViewModel.CoordinateAnimationWithPhysics();
            }
            // Otherwise, check if the ball is moving (has velocity) and not being dragged
            else if (!ballViewModel.IsDragging && _isPhysicsRunning)
            {
                // Calculate time step
                double timeStep = (currentTime - _lastPhysicsUpdate).TotalSeconds;
                
                // Clamp time step to avoid large jumps if the application was paused
                // This ensures smooth animation even if the frame rate drops temporarily
                timeStep = Math.Min(timeStep, 1.0 / 30.0);
                
                // Start measuring physics update time
                _performanceMonitor.BeginPhysicsTime();
                
                // Store the old position for comparison
                double oldX = ballViewModel._ballModel.X;
                double oldY = ballViewModel._ballModel.Y;
                
                // Update the ball's position and velocity using physics
                var result = _physicsEngine.UpdateBall(
                    ballViewModel._ballModel,
                    timeStep,
                    0, 0,
                    MainCanvas.Width,
                    MainCanvas.Height);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Check if the position has changed
                if (Math.Abs(oldX - ballViewModel._ballModel.X) > 0.01 || Math.Abs(oldY - ballViewModel._ballModel.Y) > 0.01)
                {
                    // Update the view model's position to match the model
                    // This will trigger property change notifications for X and Y
                    ballViewModel.X = ballViewModel._ballModel.X;
                    ballViewModel.Y = ballViewModel._ballModel.Y;
                    
                    // Force UI update by updating the Canvas.Left and Canvas.Top properties directly
                    // This ensures the ball's visual position is updated even if the binding doesn't update
                    Dispatcher.InvokeAsync(() => {
                        Canvas.SetLeft(BallImage, ballViewModel.Left);
                        Canvas.SetTop(BallImage, ballViewModel.Top);
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
                
                // Increment the physics update counter
                _physicsUpdateCounter++;
                
                // Debug output to verify physics is running (limit to every 60 updates to reduce spam)
                if (_physicsUpdateCounter % 60 == 0)
                {
                    // Safely format values without using culture-specific formatting
                    var xStr = FormatDoubleForDebug(ballViewModel.X);
                    var yStr = FormatDoubleForDebug(ballViewModel.Y);
                    var vxStr = FormatDoubleForDebug(ballViewModel._ballModel.VelocityX);
                    var vyStr = FormatDoubleForDebug(ballViewModel._ballModel.VelocityY);
                    
                    _logService.LogDebug("Physics update #{PhysicsUpdateCounter}: Position=({XStr}, {YStr}), Velocity=({VxStr}, {VyStr})", _physicsUpdateCounter, xStr, yStr, vxStr, vyStr);
                }
                
                // If the ball has stopped moving, stop physics updates
                if (!result.IsMoving)
                {
                    _isPhysicsRunning = false;
                    
                    // Ensure the ball is completely stopped
                    ballViewModel._ballModel.Stop();
                    
                    _logService.LogDebug("Physics stopped: Ball no longer moving");
                }
                
                // If the ball hit any boundaries, we could add visual or audio feedback here
                if (result.HitLeft || result.HitRight || result.HitTop || result.HitBottom)
                {
                    // Optional: Add bounce effect or sound here
                    // For example, you could trigger an animation or play a sound
                    _logService.LogDebug("Ball hit boundary");
                }
                
                // Coordinate animation timing with physics updates
                ballViewModel.CoordinateAnimationWithPhysics();
                
                // Update the last physics update time
                _lastPhysicsUpdate = currentTime;
            }
            else if (_isPhysicsRunning && ballViewModel.IsDragging)
            {
                // Ball is being dragged, stop physics simulation
                _isPhysicsRunning = false;
                ballViewModel._ballModel.Stop();
                _logService.LogDebug("Physics stopped: Ball is being dragged");
                
                _lastPhysicsUpdate = currentTime;
            }
        }
        
        // End measuring frame time
        _performanceMonitor.EndFrameTime();
    }

    #endregion Event Handlers

    #region Helper Methods

    /// <summary>
    /// Safely formats a double value for debug output, handling NaN, Infinity, and culture issues
    /// </summary>
    /// <param name="value">The double value to format</param>
    /// <returns>A safe string representation of the value</returns>
    private static string FormatDoubleForDebug(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsPositiveInfinity(value))
            return "+Inf";
        if (double.IsNegativeInfinity(value))
            return "-Inf";
        
        // Use invariant culture to avoid locale-specific formatting issues
        return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion Helper Methods

    #region Optimized Timer System

    /// <summary>
    /// Initializes the optimized dual timer system
    /// Separates physics updates (60 FPS) from animation frame updates
    /// </summary>
    private void InitializeOptimizedTimers()
    {
        if (_useOptimizedTimers)
        {
            // Initialize dedicated physics timer for 60 FPS updates
            _physicsTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = _physicsUpdateInterval
            };
            _physicsTimer.Tick += PhysicsTimer_Tick;
            
            _logService.LogDebug("Optimized dual timer system initialized - Physics: {PhysicsInterval:F2}ms", _physicsUpdateInterval.TotalMilliseconds);
        }
        else
        {
            // Fallback to original CompositionTarget.Rendering approach
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            _logService.LogDebug("Using legacy CompositionTarget.Rendering for physics updates");
        }
    }

    /// <summary>
    /// Cleans up the optimized timer system
    /// </summary>
    private void CleanupOptimizedTimers()
    {
        if (_useOptimizedTimers)
        {
            _physicsTimer?.Stop();
            if (_physicsTimer != null)
            {
                _physicsTimer.Tick -= PhysicsTimer_Tick;
                _physicsTimer = null;
            }
        }
        else
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }
        
        _logService.LogDebug("Timer system cleaned up");
    }

    /// <summary>
    /// Starts the physics timer when physics simulation begins
    /// </summary>
    private void StartPhysicsTimer()
    {
        if (_useOptimizedTimers && _physicsTimer != null && !_physicsTimer.IsEnabled)
        {
            _lastPhysicsUpdate = DateTime.Now;
            _physicsTimer.Start();
            _logService.LogDebug("Physics timer started");
        }
    }

    /// <summary>
    /// Stops the physics timer when physics simulation ends
    /// </summary>
    private void StopPhysicsTimer()
    {
        if (_useOptimizedTimers && _physicsTimer != null && _physicsTimer.IsEnabled)
        {
            _physicsTimer.Stop();
            _logService.LogDebug("Physics timer stopped");
        }
    }

    /// <summary>
    /// Dedicated physics timer tick handler for 60 FPS physics updates
    /// Separated from animation frame updates for optimal performance
    /// </summary>
    /// <param name="sender">Timer sender</param>
    /// <param name="e">Event arguments</param>
    private void PhysicsTimer_Tick(object sender, EventArgs e)
    {
        // Start measuring frame time
        _performanceMonitor.BeginFrameTime();
        
        // Get current time for physics calculations
        DateTime currentTime = DateTime.Now;
        
        // Get the MainWindowViewModel from the DataContext
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Handle drag operations with immediate responsiveness
            if (ballViewModel.IsDragging && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = Mouse.GetPosition(MainCanvas);
                UpdateBallPositionToMouse(ballViewModel, mousePosition);
                
                // Stop physics simulation during drag but keep timer running for responsiveness
                if (_isPhysicsRunning)
                {
                    _isPhysicsRunning = false;
                    ballViewModel._ballModel.Stop();
                    _logService.LogDebug("Physics paused: Ball is being dragged");
                }
                
                // Animation updates are handled separately by BallViewModel's animation timer
                // This ensures drag responsiveness is not affected by animation frame rates
                // Coordinate animation timing to ensure smooth operation during drag
                ballViewModel.CoordinateAnimationWithPhysics();
            }
            // Handle physics-based movement when not dragging
            else if (!ballViewModel.IsDragging && _isPhysicsRunning)
            {
                // Calculate time step for consistent 60 FPS physics
                double timeStep = (currentTime - _lastPhysicsUpdate).TotalSeconds;
                
                // Clamp time step to prevent large jumps (maintain 60 FPS consistency)
                timeStep = Math.Min(timeStep, _physicsUpdateInterval.TotalSeconds * 2);
                
                // Start measuring physics update time
                _performanceMonitor.BeginPhysicsTime();
                
                // Store the old position for comparison
                double oldX = ballViewModel._ballModel.X;
                double oldY = ballViewModel._ballModel.Y;
                
                // Update the ball's position and velocity using physics
                var result = _physicsEngine.UpdateBall(
                    ballViewModel._ballModel,
                    timeStep,
                    0, 0,
                    MainCanvas.Width,
                    MainCanvas.Height);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Check if the position has changed
                if (Math.Abs(oldX - ballViewModel._ballModel.X) > 0.01 || Math.Abs(oldY - ballViewModel._ballModel.Y) > 0.01)
                {
                    // Update the view model's position to match the model
                    ballViewModel.X = ballViewModel._ballModel.X;
                    ballViewModel.Y = ballViewModel._ballModel.Y;
                    
                    // Force UI update with high priority for smooth physics
                    Dispatcher.InvokeAsync(() => {
                        Canvas.SetLeft(BallImage, ballViewModel.Left);
                        Canvas.SetTop(BallImage, ballViewModel.Top);
                    }, DispatcherPriority.Render);
                }
                
                // Increment the physics update counter
                _physicsUpdateCounter++;
                
                // Debug output (limit to reduce spam)
                if (_physicsUpdateCounter % 60 == 0) // Every second at 60 FPS
                {
                    // Safely format values without using culture-specific formatting
                    var xStr = FormatDoubleForDebug(ballViewModel.X);
                    var yStr = FormatDoubleForDebug(ballViewModel.Y);
                    var vxStr = FormatDoubleForDebug(ballViewModel._ballModel.VelocityX);
                    var vyStr = FormatDoubleForDebug(ballViewModel._ballModel.VelocityY);
                    
                    _logService.LogDebug("Physics update #{PhysicsUpdateCounter}: Position=({XStr}, {YStr}), Velocity=({VxStr}, {VyStr})", _physicsUpdateCounter, xStr, yStr, vxStr, vyStr);
                }
                
                // If the ball has stopped moving, stop physics updates
                if (!result.IsMoving)
                {
                    _isPhysicsRunning = false;
                    StopPhysicsTimer();
                    
                    // Ensure the ball is completely stopped
                    ballViewModel._ballModel.Stop();
                    
                    _logService.LogDebug("Physics stopped: Ball no longer moving");
                }
                
                // Update the last physics update time
                _lastPhysicsUpdate = currentTime;
            }
            else if (_isPhysicsRunning && !_physicsTimer.IsEnabled)
            {
                // Physics should be running but timer is stopped - restart it
                StartPhysicsTimer();
            }
        }
        
        // End measuring frame time
        _performanceMonitor.EndFrameTime();
    }

    /// <summary>
    /// Enables or disables the optimized dual timer system
    /// </summary>
    /// <param name="useOptimized">True to use optimized timers, false for legacy approach</param>
    public void SetOptimizedTimerMode(bool useOptimized)
    {
        if (_useOptimizedTimers != useOptimized)
        {
            // Clean up current system
            CleanupOptimizedTimers();
            
            // Switch mode
            _useOptimizedTimers = useOptimized;
            
            // Initialize new system
            InitializeOptimizedTimers();
            
            _logService.LogDebug("Timer system switched to: {TimerSystem}", useOptimized ? "Optimized" : "Legacy");
        }
    }

    /// <summary>
    /// Gets performance metrics for the dual timer system
    /// </summary>
    /// <returns>Performance metrics including physics FPS and animation coordination</returns>
    public TimerPerformanceMetrics GetTimerPerformanceMetrics()
    {
        return new TimerPerformanceMetrics
        {
            PhysicsTimerEnabled = _physicsTimer?.IsEnabled ?? false,
            PhysicsUpdateInterval = _physicsUpdateInterval,
            IsPhysicsRunning = _isPhysicsRunning,
            PhysicsUpdateCount = _physicsUpdateCounter,
            UseOptimizedTimers = _useOptimizedTimers,
            AverageFrameTime = _performanceMonitor.AverageFrameTime,
            AveragePhysicsTime = _performanceMonitor.AveragePhysicsTime
        };
    }

    /// <summary>
    /// Optimizes the dual timer system coordination
    /// Ensures physics updates (60 FPS) are separated from animation frame updates
    /// </summary>
    public void OptimizeDualTimerCoordination()
    {
        // Ensure the entire method runs on the UI thread
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OptimizeDualTimerCoordination());
            return;
        }

        if (_useOptimizedTimers && DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Enable optimized dual timer system in BallViewModel
            ballViewModel.OptimizeDualTimerSystem();
            
            // Optimize animation timing to respect source frame rates
            ballViewModel.OptimizeAnimationTiming();
            
            // Ensure animation doesn't impact drag responsiveness
            ballViewModel.EnsureAnimationDoesNotImpactDragResponsiveness();
            
            // Adjust physics timer priority if needed
            if (_physicsTimer != null)
            {
                // Ensure physics timer uses normal priority for consistent 60 FPS
                var currentPriority = _physicsTimer.Dispatcher.Thread.Priority;
                if (currentPriority != System.Threading.ThreadPriority.Normal)
                {
                    _physicsTimer.Dispatcher.Thread.Priority = System.Threading.ThreadPriority.Normal;
                }
            }
            
            _logService.LogDebug("Dual timer system coordination optimized");
        }
    }

    /// <summary>
    /// Gets comprehensive timing metrics for both physics and animation systems
    /// </summary>
    /// <returns>Combined timing metrics</returns>
    public DualTimerCoordinationMetrics GetDualTimerCoordinationMetrics()
    {
        var physicsMetrics = GetTimerPerformanceMetrics();
        var animationMetrics = DataContext is MainWindowViewModel mainViewModel ? 
            mainViewModel.BallViewModel.GetAnimationTimingMetrics() : 
            new AnimationTimingMetrics();

        return new DualTimerCoordinationMetrics
        {
            PhysicsMetrics = physicsMetrics,
            AnimationMetrics = animationMetrics,
            IsCoordinationOptimal = IsTimerCoordinationOptimal(physicsMetrics, animationMetrics),
            CoordinationEfficiency = CalculateCoordinationEfficiency(physicsMetrics, animationMetrics)
        };
    }

    /// <summary>
    /// Determines if timer coordination is optimal
    /// </summary>
    /// <param name="physicsMetrics">Physics timer metrics</param>
    /// <param name="animationMetrics">Animation timer metrics</param>
    /// <returns>True if coordination is optimal</returns>
    private bool IsTimerCoordinationOptimal(TimerPerformanceMetrics physicsMetrics, AnimationTimingMetrics animationMetrics)
    {
        // Physics should maintain 60 FPS when running
        var physicsOptimal = !physicsMetrics.IsPhysicsRunning || physicsMetrics.IsPhysicsTimingOptimal;
        
        // Animation should respect source frame rate and be optimized for drag
        var animationOptimal = !animationMetrics.IsAnimated || 
            (animationMetrics.IsRespectingSourceFrameRate && animationMetrics.IsOptimizedForDrag);
        
        return physicsOptimal && animationOptimal;
    }

    /// <summary>
    /// Calculates the coordination efficiency between physics and animation timers
    /// </summary>
    /// <param name="physicsMetrics">Physics timer metrics</param>
    /// <param name="animationMetrics">Animation timer metrics</param>
    /// <returns>Coordination efficiency percentage</returns>
    private double CalculateCoordinationEfficiency(TimerPerformanceMetrics physicsMetrics, AnimationTimingMetrics animationMetrics)
    {
        var physicsEfficiency = physicsMetrics.PhysicsTimingEfficiency;
        var animationEfficiency = animationMetrics.IsPerformanceAcceptable ? 100.0 : 50.0;
        
        // Weight physics efficiency higher since it's more critical for responsiveness
        return (physicsEfficiency * 0.7) + (animationEfficiency * 0.3);
    }

    #endregion Optimized Timer System

    #region Methods

    /// <summary>
    /// Updates the ball position to follow the mouse cursor
    /// </summary>
    /// <param name="viewModel">The ball view model</param>
    /// <param name="mousePosition">The current mouse position</param>
    private void UpdateBallPositionToMouse(BallViewModel viewModel, Point mousePosition)
    {
        // Update the ball position to the mouse position
        viewModel.X = mousePosition.X;
        viewModel.Y = mousePosition.Y;
        
        // Constrain the ball position to stay within the window boundaries
        viewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
        
        // Force UI update by updating the Canvas.Left and Canvas.Top properties directly
        Canvas.SetLeft(BallImage, viewModel.Left);
        Canvas.SetTop(BallImage, viewModel.Top);
    }

    /// <summary>
    /// Updates the cursor based on the mouse position relative to the ball
    /// </summary>
    /// <param name="viewModel">The ball view model</param>
    /// <param name="canvasPosition">The mouse position relative to the canvas</param>
    private void UpdateCursorForPosition(BallViewModel viewModel, Point canvasPosition)
    {
        if (viewModel.IsDragging)
        {
            // When dragging the ball, show the "SizeAll" cursor to indicate movement
            viewModel.CurrentCursor = Cursors.SizeAll;
        }
        else if (viewModel._ballModel.ContainsPoint(canvasPosition.X, canvasPosition.Y))
        {
            // When hovering over the ball, show the "Hand" cursor to indicate it can be grabbed
            viewModel.CurrentCursor = Cursors.Hand;
        }
        else
        {
            // Default cursor when not interacting with the ball
            viewModel.CurrentCursor = Cursors.Arrow;
        }
    }

    /// <summary>
    /// Stores the current mouse position and timestamp in the history arrays
    /// </summary>
    /// <param name="position">Current mouse position</param>
    /// <param name="timestamp">Current timestamp</param>
    private void StoreMousePosition(Point position, DateTime timestamp)
    {
        // Shift all elements one position to make room for the new one
        if (_mouseHistoryCount >= _mousePositionHistory.Length)
        {
            for (int i = 0; i < _mousePositionHistory.Length - 1; i++)
            {
                _mousePositionHistory[i] = _mousePositionHistory[i + 1];
                _mouseTimestampHistory[i] = _mouseTimestampHistory[i + 1];
            }
            
            // Add the new position and timestamp at the end
            _mousePositionHistory[_mousePositionHistory.Length - 1] = position;
            _mouseTimestampHistory[_mouseTimestampHistory.Length - 1] = timestamp;
        }
        else
        {
            // Add the new position and timestamp at the current count
            _mousePositionHistory[_mouseHistoryCount] = position;
            _mouseTimestampHistory[_mouseHistoryCount] = timestamp;
            _mouseHistoryCount++;
        }
    }

    /// <summary>
    /// Calculates velocity from the mouse movement history
    /// </summary>
    /// <returns>Velocity in X and Y directions</returns>
    private (double velocityX, double velocityY) CalculateVelocityFromHistory()
    {
        if (_mouseHistoryCount < 2)
        {
            return (0, 0);
        }

        // Use the last few positions to calculate velocity
        int samplesToUse = Math.Min(5, _mouseHistoryCount);
        Point startPos = _mousePositionHistory[_mouseHistoryCount - samplesToUse];
        Point endPos = _mousePositionHistory[_mouseHistoryCount - 1];
        DateTime startTime = _mouseTimestampHistory[_mouseHistoryCount - samplesToUse];
        DateTime endTime = _mouseTimestampHistory[_mouseHistoryCount - 1];

        double timeElapsed = (endTime - startTime).TotalSeconds;
        
        if (timeElapsed <= 0.001) // Avoid division by very small numbers
        {
            return (0, 0);
        }

        double deltaX = endPos.X - startPos.X;
        double deltaY = endPos.Y - startPos.Y;

        double velocityX = deltaX / timeElapsed;
        double velocityY = deltaY / timeElapsed;

        return (velocityX, velocityY);
    }

    /// <summary>
    /// Ensures the ball stays within the window boundaries
    /// </summary>
    /// <param name="x">The x-coordinate to constrain</param>
    /// <param name="y">The y-coordinate to constrain</param>
    /// <param name="ballRadius">The radius of the ball (default is 0, treating the ball as a point)</param>
    /// <returns>A Point with coordinates constrained to the window boundaries</returns>
    public Point ConstrainToWindowBoundaries(double x, double y, double ballRadius = 0)
    {
        // Get the current canvas dimensions
        double canvasWidth = MainCanvas.Width;
        double canvasHeight = MainCanvas.Height;
        
        // Constrain x and y to be within the canvas boundaries, accounting for the ball's size
        double constrainedX = Math.Max(ballRadius, Math.Min(x, canvasWidth - ballRadius));
        double constrainedY = Math.Max(ballRadius, Math.Min(y, canvasHeight - ballRadius));
        
        return new Point(constrainedX, constrainedY);
    }
    
    /// <summary>
    /// Helper method for testing to simulate a window resize
    /// </summary>
    /// <param name="newWidth">The new width of the window</param>
    /// <param name="newHeight">The new height of the window</param>
    public void SimulateResize(double newWidth, double newHeight)
    {
        // Store the old dimensions for calculating relative position
        double oldWidth = MainCanvas.Width;
        double oldHeight = MainCanvas.Height;
        
        // Update the canvas dimensions to the new size
        MainCanvas.Width = newWidth;
        MainCanvas.Height = newHeight;
        
        // Get the MainWindowViewModel from the DataContext
        if (DataContext is MainWindowViewModel mainViewModel)
        {
            var ballViewModel = mainViewModel.BallViewModel;
            
            // Store the ball's relative position (as a percentage of the window size)
            // before resizing to maintain its relative position
            double relativeX = ballViewModel.X / oldWidth;
            double relativeY = ballViewModel.Y / oldHeight;
            
            // Only apply relative positioning if we have valid previous dimensions
            // and the ball was not at the edge of the window
            if (!double.IsNaN(relativeX) && !double.IsInfinity(relativeX) && 
                !double.IsNaN(relativeY) && !double.IsInfinity(relativeY) &&
                oldWidth > 0 && oldHeight > 0)
            {
                // Calculate new position based on relative position
                double newX = relativeX * newWidth;
                double newY = relativeY * newHeight;
                
                // Update the ball position
                ballViewModel.X = newX;
                ballViewModel.Y = newY;
            }
            
            // Constrain the ball position to the new window boundaries
            // This ensures the ball stays within the window even after applying relative positioning
            ballViewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
            
            // Raise the BallPositionChanged event with the new position
            BallPositionChanged?.Invoke(this, new Point(ballViewModel.X, ballViewModel.Y));
        }
    }

    #endregion Methods

    #region Visual Content Switching Event Handlers

    /// <summary>
    /// Event handler for switching ball visual content
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private async void SwitchVisual_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Ball Visual Content",
                Filter = "All Supported|*.png;*.jpg;*.jpeg;*.bmp;*.gif|" +
                        "Static Images|*.png;*.jpg;*.jpeg;*.bmp|" +
                        "GIF Animations|*.gif|" +
                        "PNG Files (Aseprite)|*.png|" +
                        "All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested visual content switch to: {FilePath}", openFileDialog.FileName);

                    bool success = await ballViewModel.SwitchBallVisualAsync(openFileDialog.FileName);
                    
                    if (success)
                    {
                        logService?.LogInformation("Visual content switched successfully");
                        // Optional: Show success message
                        // MessageBox.Show("Visual content switched successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        logService?.LogWarning("Failed to switch visual content");
                        MessageBox.Show("Failed to switch visual content. Please check the file format and try again.", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logService = ServiceBootstrapper.GetService<ILogService>();
            logService?.LogError(ex, "Error in visual content switching dialog");
            MessageBox.Show($"An error occurred while switching visual content: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for loading static image content
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private async void LoadStaticImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Static Image",
                Filter = "Static Images|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested static image load: {FilePath}", openFileDialog.FileName);

                    bool success = await ballViewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
                    if (!success)
                    {
                        MessageBox.Show("Failed to load static image. Please check the file format and try again.", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logService = ServiceBootstrapper.GetService<ILogService>();
            logService?.LogError(ex, "Error loading static image");
            MessageBox.Show($"An error occurred while loading the static image: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for loading GIF animation content
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private async void LoadGifAnimation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select GIF Animation",
                Filter = "GIF Animations|*.gif|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested GIF animation load: {FilePath}", openFileDialog.FileName);

                    bool success = await ballViewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
                    if (!success)
                    {
                        MessageBox.Show("Failed to load GIF animation. Please check the file format and try again.", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logService = ServiceBootstrapper.GetService<ILogService>();
            logService?.LogError(ex, "Error loading GIF animation");
            MessageBox.Show($"An error occurred while loading the GIF animation: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for loading Aseprite animation content
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private async void LoadAsepriteAnimation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Aseprite PNG File (JSON metadata should be in same folder)",
                Filter = "PNG Files (Aseprite)|*.png|All Files|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested Aseprite animation load: {FilePath}", openFileDialog.FileName);

                    // Check if JSON metadata file exists
                    var directory = Path.GetDirectoryName(openFileDialog.FileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    var jsonPath = Path.Combine(directory, fileNameWithoutExtension + ".json");

                    if (!File.Exists(jsonPath))
                    {
                        var result = MessageBox.Show(
                            $"JSON metadata file not found: {Path.GetFileName(jsonPath)}\n\n" +
                            "Aseprite animations require both PNG and JSON files in the same folder.\n" +
                            "The PNG file will be loaded as a static image instead.\n\n" +
                            "Do you want to continue?",
                            "JSON Metadata Missing", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    bool success = await ballViewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
                    if (!success)
                    {
                        MessageBox.Show("Failed to load Aseprite animation. Please check the file format and try again.", 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logService = ServiceBootstrapper.GetService<ILogService>();
            logService?.LogError(ex, "Error loading Aseprite animation");
            MessageBox.Show($"An error occurred while loading the Aseprite animation: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for toggling bounding box display
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void ToggleBoundingBox_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                var configService = ServiceBootstrapper.GetService<IConfigurationService>();
                if (configService != null)
                {
                    // Toggle the setting
                    bool newValue = !configService.GetShowBoundingBox();
                    configService.SetShowBoundingBox(newValue);
                    
                    // Update the view model
                    mainViewModel.BallViewModel.ShowBoundingBox = newValue;
                    
                    _logService.LogDebug("Bounding box display toggled to: {ShowBoundingBox}", newValue);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, "Error toggling bounding box display");
            MessageBox.Show($"An error occurred while toggling bounding box display: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion Visual Content Switching Event Handlers
}

/// <summary>
/// Simple converter to offset a value by a specified amount
/// </summary>
public class OffsetConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance of the OffsetConverter
    /// </summary>
    public static readonly OffsetConverter Instance = new OffsetConverter();

    /// <summary>
    /// Converts a value by adding the specified offset
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="targetType">The target type</param>
    /// <param name="parameter">The offset amount as a string</param>
    /// <param name="culture">The culture info</param>
    /// <returns>The value plus the offset, or the original value if conversion fails</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string parameterString)
        {
            // Use invariant culture to avoid locale-specific parsing issues
            if (double.TryParse(parameterString, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
            {
                return doubleValue + offset;
            }
        }
        return value;
    }

    /// <summary>
    /// Converts back (not implemented)
    /// </summary>
    /// <param name="value">The value to convert back</param>
    /// <param name="targetType">The target type</param>
    /// <param name="parameter">The parameter</param>
    /// <param name="culture">The culture info</param>
    /// <returns>Not implemented</returns>
    /// <exception cref="NotImplementedException">This method is not implemented</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
