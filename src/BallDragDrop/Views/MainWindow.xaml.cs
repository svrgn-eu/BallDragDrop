using System;
using System.IO;
using System.Threading.Tasks;
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
public partial class MainWindow : Window, IBallStateObserver
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

    /// <summary>
    /// Ball state configuration for physics engine
    /// </summary>
    private readonly BallStateConfiguration _ballStateConfiguration;

    #endregion Fields

    #region Construction

    /// <summary>
    /// Initializes a new instance of the MainWindow class
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        
        // Get logging service from dependency injection
        this._logService = ServiceBootstrapper.GetService<ILogService>();
        
        // Get ball state configuration from dependency injection
        this._ballStateConfiguration = ServiceBootstrapper.GetService<BallStateConfiguration>();
        
        // Initialize physics engine
        this._physicsEngine = new Models.PhysicsEngine();
        this._lastPhysicsUpdate = DateTime.Now;
        this._isPhysicsRunning = false;
        
        // Get performance monitor from dependency injection
        this._performanceMonitor = ServiceBootstrapper.GetService<Services.PerformanceMonitor>();
        
        // Set up event handlers
        this.SizeChanged += this.Window_SizeChanged;
        this.Loaded += this.Window_Loaded;
        this.Closed += this.MainWindow_Closed;
        
        // Subscribe to CompositionTarget.Rendering for physics updates
        CompositionTarget.Rendering += this.CompositionTarget_Rendering;
        
        // Initialize DataContext immediately in constructor
        this.InitializeDataContext();
        
        this._logService.LogDebug("MainWindow initialized");
    }
    
    /// <summary>
    /// Initializes the DataContext with MainWindowViewModel
    /// </summary>
    private void InitializeDataContext()
    {
        try
        {
            this._logService.LogDebug("InitializeDataContext started");
            Console.WriteLine("InitializeDataContext started");
            
            // Create a new MainWindowViewModel using dependency injection
            MainWindowViewModel mainViewModel = ServiceBootstrapper.GetService<MainWindowViewModel>();
            
            this._logService.LogDebug("MainWindowViewModel created successfully");
            Console.WriteLine("MainWindowViewModel created successfully");
            
            // Set the DataContext for the window
            this.DataContext = mainViewModel;
            
            this._logService.LogDebug("DataContext set successfully");
            
            // Initialize the ball position (will be updated in Window_Loaded)
            mainViewModel.BallViewModel.Initialize(400, 300, 25);
            
            // Subscribe to state machine changes to manage physics simulation
            var stateMachine = ServiceBootstrapper.GetService<IBallStateMachine>();
            if (stateMachine != null)
            {
                stateMachine.Subscribe(this);
                this._logService.LogDebug("MainWindow subscribed to state machine notifications");
            }
            else
            {
                this._logService.LogWarning("State machine not available for subscription");
            }
            
            this._logService.LogDebug("InitializeDataContext completed successfully");
        }
        catch (Exception ex)
        {
            this._logService.LogError(ex, "Error in InitializeDataContext");
            
            // Show error message
            MessageBox.Show($"Error initializing DataContext: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion Construction

    #region IBallStateObserver Implementation

    /// <summary>
    /// Handles state change notifications from the ball state machine
    /// </summary>
    /// <param name="previousState">The previous state</param>
    /// <param name="newState">The new state</param>
    /// <param name="trigger">The trigger that caused the state change</param>
    public void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger)
    {
        _logService?.LogDebug("MainWindow received state change: {PreviousState} -> {NewState} via {Trigger}", 
            previousState, newState, trigger);

        // Handle physics simulation based on state changes
        switch (newState)
        {
            case BallState.Thrown:
                // Start physics simulation when ball is thrown
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
                
                // Get the ball's current velocity for debugging
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    var ballViewModel = mainViewModel.BallViewModel;
                    _logService?.LogInformation($"PHYSICS STARTED - Ball thrown with velocity: ({ballViewModel._ballModel.VelocityX:F2}, {ballViewModel._ballModel.VelocityY:F2}), Position: ({ballViewModel._ballModel.X:F2}, {ballViewModel._ballModel.Y:F2})");
                }
                else
                {
                    _logService?.LogInformation("PHYSICS STARTED - Ball thrown");
                }
                break;

            case BallState.Idle:
                // Stop physics simulation when ball comes to rest
                if (previousState == BallState.Thrown)
                {
                    _isPhysicsRunning = false;
                    
                    if (_useOptimizedTimers)
                    {
                        StopPhysicsTimer();
                    }
                    
                    _logService?.LogDebug("Physics simulation stopped - ball idle");
                }
                break;

            case BallState.Held:
                // Stop physics simulation when ball is held
                _isPhysicsRunning = false;
                
                if (_useOptimizedTimers)
                {
                    StopPhysicsTimer();
                }
                
                _logService?.LogDebug("Physics simulation stopped - ball held");
                break;
        }
    }

    #endregion IBallStateObserver Implementation

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
        // Unsubscribe from CompositionTarget.Rendering
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
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
                
                // Use the BallViewModel's mouse command to properly trigger state machine
                ballViewModel.MouseDownCommand.Execute(e);
                
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
            
            // Use the BallViewModel's mouse command to properly handle mouse move
            ballViewModel.MouseMoveCommand.Execute(e);
            
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
                // Get canvas-relative position
                var position = e.GetPosition(MainCanvas);
                var currentTime = DateTime.Now;
                
                // Store final position in history for MainWindow's velocity calculation
                StoreMousePosition(position, currentTime);
                
                // Calculate velocity using MainWindow's mouse history (canvas coordinates)
                var (velocityX, velocityY) = CalculateVelocityFromHistory();
                
                // Set the velocity on the ball model before triggering state transition
                ballViewModel._ballModel.SetVelocity(velocityX, velocityY);
                
                // Debug: Log velocity calculation
                _logService.LogInformation($"MOUSE UP - Ball released at position ({position.X:F2}, {position.Y:F2}) with velocity ({velocityX:F2}, {velocityY:F2})");
                
                // Use the BallViewModel's mouse command to trigger state machine transition
                // The BallViewModel will handle state transitions, MainWindow handles physics
                ballViewModel.MouseUpCommand.Execute(e);
                
                // Release mouse capture
                Mouse.Capture(null);
                
                // Update cursor
                UpdateCursorForPosition(ballViewModel, position);
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
                
                // Debug: Log physics update attempt
                if (_physicsUpdateCounter % 30 == 0)
                {
                    _logService.LogInformation($"PHYSICS UPDATE #{_physicsUpdateCounter}: State={ballViewModel.CurrentState}, Velocity=({ballViewModel._ballModel.VelocityX:F2}, {ballViewModel._ballModel.VelocityY:F2}), Position=({ballViewModel._ballModel.X:F2}, {ballViewModel._ballModel.Y:F2})");
                }
                
                // Update the ball's position and velocity using physics
                var result = _physicsEngine.UpdateBall(
                    ballViewModel._ballModel,
                    timeStep,
                    0, 0,
                    MainCanvas.Width,
                    MainCanvas.Height,
                    ballViewModel.CurrentState,
                    _ballStateConfiguration);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Store old ViewModel position for comparison
                double oldViewModelX = ballViewModel.X;
                double oldViewModelY = ballViewModel.Y;
                
                // Force PropertyChanged events for position properties
                // Since the getter returns _ballModel.X, setting it to the same value won't trigger PropertyChanged
                ballViewModel.ForcePositionUpdate();
                
                // Debug: Compare physics model vs ViewModel positions
                if (_physicsUpdateCounter % 30 == 0)
                {
                    _logService.LogInformation($"PHYSICS vs VISUAL: Model=({ballViewModel._ballModel.X:F2}, {ballViewModel._ballModel.Y:F2}), ViewModel=({ballViewModel.X:F2}, {ballViewModel.Y:F2}), Left={ballViewModel.Left:F2}, Top={ballViewModel.Top:F2}, Changed=({Math.Abs(oldViewModelX - ballViewModel.X) > 0.01}, {Math.Abs(oldViewModelY - ballViewModel.Y) > 0.01})");
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
                
                // Check if velocity dropped below threshold for state transition
                if (result.VelocityBelowThreshold && ballViewModel.CurrentState == BallState.Thrown)
                {
                    // Trigger state machine transition from Thrown to Idle
                    try
                    {
                        var stateMachine = ServiceBootstrapper.GetService<IBallStateMachine>();
                        if (stateMachine != null && stateMachine.CanFire(BallTrigger.VelocityBelowThreshold))
                        {
                            stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                            _logService.LogDebug("State transition triggered: Thrown -> Idle (velocity below threshold)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Error triggering velocity threshold state transition");
                    }
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
            _logService.LogDebug("Physics timer started - timer enabled: {TimerEnabled}", _physicsTimer.IsEnabled);
        }
        else
        {
            _logService.LogDebug("Physics timer start skipped - useOptimized: {UseOptimized}, timer null: {TimerNull}, already enabled: {AlreadyEnabled}", 
                _useOptimizedTimers, _physicsTimer == null, _physicsTimer?.IsEnabled ?? false);
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
                    MainCanvas.Height,
                    ballViewModel.CurrentState,
                    _ballStateConfiguration);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Force PropertyChanged events for position properties
                // Since the getter returns _ballModel.X, setting it to the same value won't trigger PropertyChanged
                ballViewModel.ForcePositionUpdate();
                
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
                
                // Check if velocity dropped below threshold for state transition
                if (result.VelocityBelowThreshold && ballViewModel.CurrentState == BallState.Thrown)
                {
                    // Trigger state machine transition from Thrown to Idle
                    try
                    {
                        var stateMachine = ServiceBootstrapper.GetService<IBallStateMachine>();
                        if (stateMachine != null && stateMachine.CanFire(BallTrigger.VelocityBelowThreshold))
                        {
                            stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                            _logService.LogDebug("State transition triggered: Thrown -> Idle (velocity below threshold)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError(ex, "Error triggering velocity threshold state transition");
                    }
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

    /// <summary>
    /// Event handler for the Reset menu item
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainWindowViewModel mainViewModel)
            {
                var ballViewModel = mainViewModel.BallViewModel;
                
                // Stop any physics simulation
                _isPhysicsRunning = false;
                
                // Stop dragging if in progress and release mouse capture
                if (ballViewModel.IsDragging)
                {
                    Mouse.Capture(null);
                }
                
                // Calculate center position of canvas
                double centerX = MainCanvas.Width / 2;
                double centerY = MainCanvas.Height / 2;
                
                // Use BallViewModel's ResetBall method which properly handles state machine reset
                ballViewModel.ResetBall(centerX, centerY);
                
                // Reset mouse history
                _mouseHistoryCount = 0;
                
                _logService.LogDebug("Application reset - ball reset to center ({X:F2}, {Y:F2}), state: {State}", 
                    centerX, centerY, ballViewModel.CurrentState);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, "Error during application reset");
            MessageBox.Show($"An error occurred while resetting the application: {ex.Message}", 
                "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for the Quit menu item
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logService.LogDebug("Application quit requested via menu");
            
            // Close the application
            this.Close();
        }
        catch (Exception ex)
        {
            _logService.LogError(ex, "Error during application quit");
            MessageBox.Show($"An error occurred while quitting the application: {ex.Message}", 
                "Quit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion Visual Content Switching Event Handlers

    #region Mouse Position Tracking

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
    /// Calculates velocity from the mouse position history
    /// </summary>
    /// <returns>Velocity as (X, Y) tuple</returns>
    private (double velocityX, double velocityY) CalculateVelocityFromHistory()
    {
        if (_mouseHistoryCount < 2)
        {
            return (0, 0);
        }

        // Use the physics engine to calculate velocity from history
        var physicsEngine = new Models.PhysicsEngine();
        return physicsEngine.CalculateVelocityFromHistory(
            _mousePositionHistory, 
            _mouseTimestampHistory, 
            _mouseHistoryCount);
    }

    /// <summary>
    /// Updates the ball position to match the mouse position
    /// </summary>
    /// <param name="ballViewModel">The ball view model</param>
    /// <param name="mousePosition">The mouse position</param>
    private void UpdateBallPositionToMouse(BallViewModel ballViewModel, Point mousePosition)
    {
        ballViewModel.X = mousePosition.X;
        ballViewModel.Y = mousePosition.Y;
        
        // Constrain the ball position to stay within the canvas boundaries
        ballViewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
    }

    /// <summary>
    /// Updates the cursor based on the ball position and state
    /// </summary>
    /// <param name="ballViewModel">The ball view model</param>
    /// <param name="position">The current position</param>
    private void UpdateCursorForPosition(BallViewModel ballViewModel, Point position)
    {
        if (ballViewModel.IsDragging)
        {
            this.Cursor = Cursors.Hand;
        }
        else if (ballViewModel._ballModel.ContainsPoint(position.X, position.Y))
        {
            this.Cursor = Cursors.Hand;
        }
        else
        {
            this.Cursor = Cursors.Arrow;
        }
    }

    #endregion Mouse Position Tracking
}


