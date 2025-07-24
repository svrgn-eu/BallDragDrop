using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

    #region Construction

    /// <summary>
    /// Initializes a new instance of the MainWindow class
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize physics engine
        _physicsEngine = new Models.PhysicsEngine();
        _lastPhysicsUpdate = DateTime.Now;
        _isPhysicsRunning = false;
        
        // Initialize performance monitor with target frame rate of 60 FPS
        _performanceMonitor = new Services.PerformanceMonitor(60);
        
        // Set up event handlers
        this.SizeChanged += Window_SizeChanged;
        this.Loaded += Window_Loaded;
        this.Closed += MainWindow_Closed;
        
        // Initialize optimized dual timer system
        InitializeOptimizedTimers();
        
        Debug.WriteLine("MainWindow initialized");
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
        // Initialize canvas dimensions to match the window's client area
        MainCanvas.Width = this.ActualWidth;
        MainCanvas.Height = this.ActualHeight;
        
        // Initialize the BallViewModel with the center position of the canvas
        double centerX = MainCanvas.Width / 2;
        double centerY = MainCanvas.Height / 2;
        double ballRadius = 25; // Default radius
        
        // Create a new BallViewModel using dependency injection
        BallViewModel viewModel = ServiceBootstrapper.GetService<BallViewModel>();
        
        // Initialize the ball position
        viewModel.Initialize(centerX, centerY, ballRadius);
        
        // Load the ball image using the new LoadBallVisualAsync method
        _ = Task.Run(async () =>
        {
            try
            {
                // Try to load the image from the Resources folder
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "Ball", "Ball01.png");
                string fullPath = Path.GetFullPath(imagePath);
                
                bool success = await viewModel.LoadBallVisualAsync(fullPath);
                
                if (!success)
                {
                    // If loading failed, the ImageService will have already set a fallback image
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogWarning("Failed to load ball visual, fallback image should be displayed");
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
        this.DataContext = viewModel;
        
        // Enable hardware rendering if supported
        if (RenderCapability.Tier > 0)
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            
            // Check if hardware acceleration is available
            bool isHardwareAccelerated = RenderCapability.IsPixelShaderVersionSupported(2, 0);
            Debug.WriteLine($"Hardware acceleration available: {isHardwareAccelerated}");
            
            // Set rendering tier information for debugging
            Debug.WriteLine($"Rendering Tier: {RenderCapability.Tier}");
        }
        
        Debug.WriteLine("Window loaded");
    }

    /// <summary>
    /// Event handler for window resize events
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update canvas dimensions
        MainCanvas.Width = e.NewSize.Width;
        MainCanvas.Height = e.NewSize.Height;
        
        // Get the BallViewModel from the DataContext
        if (DataContext is BallViewModel viewModel)
        {
            // Store the ball's relative position (as a percentage of the window size)
            // before resizing to maintain its relative position
            double relativeX = viewModel.X / e.PreviousSize.Width;
            double relativeY = viewModel.Y / e.PreviousSize.Height;
            
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
                viewModel.X = newX;
                viewModel.Y = newY;
            }
            
            // Constrain the ball position to the new window boundaries
            // This ensures the ball stays within the window even after applying relative positioning
            viewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
            
            // Raise the BallPositionChanged event with the new position
            BallPositionChanged?.Invoke(this, new Point(viewModel.X, viewModel.Y));
        }
    }
    
    /// <summary>
    /// Event handler for mouse down on the ball image
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void BallImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is BallViewModel viewModel)
        {
            // Get the position of the mouse click
            var position = e.GetPosition(null);
            
            // Check if the click is inside the ball
            if (viewModel._ballModel.ContainsPoint(position.X, position.Y))
            {
                // Stop the physics simulation first
                _isPhysicsRunning = false;
                viewModel._ballModel.Stop();
                
                // Execute the mouse down command
                if (viewModel.MouseDownCommand.CanExecute(e))
                {
                    viewModel.MouseDownCommand.Execute(e);
                }
                
                // Immediately update the ball position to the mouse position
                // This ensures the ball appears under the cursor right away
                UpdateBallPositionToMouse(viewModel, position);
                
                Debug.WriteLine($"Ball grabbed at position ({position.X:F2}, {position.Y:F2})");
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
        if (DataContext is BallViewModel viewModel && viewModel.MouseMoveCommand.CanExecute(e))
        {
            viewModel.MouseMoveCommand.Execute(e);
            
            // If the ball is being dragged, ensure it follows the mouse cursor immediately
            if (viewModel.IsDragging)
            {
                var position = e.GetPosition(null);
                UpdateBallPositionToMouse(viewModel, position);
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
        if (DataContext is BallViewModel viewModel)
        {
            // Execute the mouse up command
            if (viewModel.MouseUpCommand.CanExecute(e))
            {
                viewModel.MouseUpCommand.Execute(e);
            }
            
            // Check if the ball has velocity after release (was thrown)
            if (Math.Abs(viewModel._ballModel.VelocityX) > 0.1 || Math.Abs(viewModel._ballModel.VelocityY) > 0.1)
            {
                // Start the physics simulation using optimized timer system
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
                
                Debug.WriteLine($"Ball thrown with velocity: ({viewModel._ballModel.VelocityX:F2}, {viewModel._ballModel.VelocityY:F2})");
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
        
        // Get the BallViewModel from the DataContext
        if (DataContext is BallViewModel viewModel)
        {
            // If the ball is being dragged, ensure it follows the mouse cursor
            if (viewModel.IsDragging && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = Mouse.GetPosition(MainCanvas);
                UpdateBallPositionToMouse(viewModel, mousePosition);
                
                // Coordinate animation timing with physics updates during drag
                viewModel.CoordinateAnimationWithPhysics();
            }
            // Otherwise, check if the ball is moving (has velocity) and not being dragged
            else if (!viewModel.IsDragging && _isPhysicsRunning)
            {
                // Calculate time step
                double timeStep = (currentTime - _lastPhysicsUpdate).TotalSeconds;
                
                // Clamp time step to avoid large jumps if the application was paused
                // This ensures smooth animation even if the frame rate drops temporarily
                timeStep = Math.Min(timeStep, 1.0 / 30.0);
                
                // Start measuring physics update time
                _performanceMonitor.BeginPhysicsTime();
                
                // Store the old position for comparison
                double oldX = viewModel._ballModel.X;
                double oldY = viewModel._ballModel.Y;
                
                // Update the ball's position and velocity using physics
                var result = _physicsEngine.UpdateBall(
                    viewModel._ballModel,
                    timeStep,
                    0, 0,
                    MainCanvas.Width,
                    MainCanvas.Height);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Check if the position has changed
                if (Math.Abs(oldX - viewModel._ballModel.X) > 0.01 || Math.Abs(oldY - viewModel._ballModel.Y) > 0.01)
                {
                    // Update the view model's position to match the model
                    // This will trigger property change notifications for X and Y
                    viewModel.X = viewModel._ballModel.X;
                    viewModel.Y = viewModel._ballModel.Y;
                    
                    // Force UI update by updating the Canvas.Left and Canvas.Top properties directly
                    // This ensures the ball's visual position is updated even if the binding doesn't update
                    Dispatcher.InvokeAsync(() => {
                        Canvas.SetLeft(BallImage, viewModel.Left);
                        Canvas.SetTop(BallImage, viewModel.Top);
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
                
                // Increment the physics update counter
                _physicsUpdateCounter++;
                
                // Debug output to verify physics is running (limit to every 5 updates to reduce spam)
                if (_physicsUpdateCounter % 5 == 0)
                {
                    Debug.WriteLine($"Physics update #{_physicsUpdateCounter}: Position=({viewModel.X:F2}, {viewModel.Y:F2}), Velocity=({viewModel._ballModel.VelocityX:F2}, {viewModel._ballModel.VelocityY:F2})");
                }
                
                // If the ball has stopped moving, stop physics updates
                if (!result.IsMoving)
                {
                    _isPhysicsRunning = false;
                    
                    // Ensure the ball is completely stopped
                    viewModel._ballModel.Stop();
                    
                    Debug.WriteLine("Physics stopped: Ball no longer moving");
                }
                
                // If the ball hit any boundaries, we could add visual or audio feedback here
                if (result.HitLeft || result.HitRight || result.HitTop || result.HitBottom)
                {
                    // Optional: Add bounce effect or sound here
                    // For example, you could trigger an animation or play a sound
                    Debug.WriteLine("Ball hit boundary");
                }
                
                // Coordinate animation timing with physics updates
                viewModel.CoordinateAnimationWithPhysics();
                
                // Update the last physics update time
                _lastPhysicsUpdate = currentTime;
            }
            else if (_isPhysicsRunning && viewModel.IsDragging)
            {
                // Ball is being dragged, stop physics simulation
                _isPhysicsRunning = false;
                viewModel._ballModel.Stop();
                Debug.WriteLine("Physics stopped: Ball is being dragged");
                
                _lastPhysicsUpdate = currentTime;
            }
        }
        
        // End measuring frame time
        _performanceMonitor.EndFrameTime();
    }

    #endregion Event Handlers

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
            
            Debug.WriteLine($"Optimized dual timer system initialized - Physics: {_physicsUpdateInterval.TotalMilliseconds:F2}ms");
        }
        else
        {
            // Fallback to original CompositionTarget.Rendering approach
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            Debug.WriteLine("Using legacy CompositionTarget.Rendering for physics updates");
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
        
        Debug.WriteLine("Timer system cleaned up");
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
            Debug.WriteLine("Physics timer started");
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
            Debug.WriteLine("Physics timer stopped");
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
        
        // Get the BallViewModel from the DataContext
        if (DataContext is BallViewModel viewModel)
        {
            // Handle drag operations with immediate responsiveness
            if (viewModel.IsDragging && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                var mousePosition = Mouse.GetPosition(MainCanvas);
                UpdateBallPositionToMouse(viewModel, mousePosition);
                
                // Stop physics simulation during drag but keep timer running for responsiveness
                if (_isPhysicsRunning)
                {
                    _isPhysicsRunning = false;
                    viewModel._ballModel.Stop();
                    Debug.WriteLine("Physics paused: Ball is being dragged");
                }
                
                // Animation updates are handled separately by BallViewModel's animation timer
                // This ensures drag responsiveness is not affected by animation frame rates
                // Coordinate animation timing to ensure smooth operation during drag
                viewModel.CoordinateAnimationWithPhysics();
            }
            // Handle physics-based movement when not dragging
            else if (!viewModel.IsDragging && _isPhysicsRunning)
            {
                // Calculate time step for consistent 60 FPS physics
                double timeStep = (currentTime - _lastPhysicsUpdate).TotalSeconds;
                
                // Clamp time step to prevent large jumps (maintain 60 FPS consistency)
                timeStep = Math.Min(timeStep, _physicsUpdateInterval.TotalSeconds * 2);
                
                // Start measuring physics update time
                _performanceMonitor.BeginPhysicsTime();
                
                // Store the old position for comparison
                double oldX = viewModel._ballModel.X;
                double oldY = viewModel._ballModel.Y;
                
                // Update the ball's position and velocity using physics
                var result = _physicsEngine.UpdateBall(
                    viewModel._ballModel,
                    timeStep,
                    0, 0,
                    MainCanvas.Width,
                    MainCanvas.Height);
                
                // End measuring physics update time
                _performanceMonitor.EndPhysicsTime();
                
                // Check if the position has changed
                if (Math.Abs(oldX - viewModel._ballModel.X) > 0.01 || Math.Abs(oldY - viewModel._ballModel.Y) > 0.01)
                {
                    // Update the view model's position to match the model
                    viewModel.X = viewModel._ballModel.X;
                    viewModel.Y = viewModel._ballModel.Y;
                    
                    // Force UI update with high priority for smooth physics
                    Dispatcher.InvokeAsync(() => {
                        Canvas.SetLeft(BallImage, viewModel.Left);
                        Canvas.SetTop(BallImage, viewModel.Top);
                    }, DispatcherPriority.Render);
                }
                
                // Increment the physics update counter
                _physicsUpdateCounter++;
                
                // Debug output (limit to reduce spam)
                if (_physicsUpdateCounter % 60 == 0) // Every second at 60 FPS
                {
                    Debug.WriteLine($"Physics update #{_physicsUpdateCounter}: Position=({viewModel.X:F2}, {viewModel.Y:F2}), Velocity=({viewModel._ballModel.VelocityX:F2}, {viewModel._ballModel.VelocityY:F2})");
                }
                
                // If the ball has stopped moving, stop physics updates
                if (!result.IsMoving)
                {
                    _isPhysicsRunning = false;
                    StopPhysicsTimer();
                    
                    // Ensure the ball is completely stopped
                    viewModel._ballModel.Stop();
                    
                    Debug.WriteLine("Physics stopped: Ball no longer moving");
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
            
            Debug.WriteLine($"Timer system switched to: {(useOptimized ? "Optimized" : "Legacy")}");
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
        if (_useOptimizedTimers && DataContext is BallViewModel viewModel)
        {
            // Enable optimized dual timer system in BallViewModel
            viewModel.OptimizeDualTimerSystem();
            
            // Optimize animation timing to respect source frame rates
            viewModel.OptimizeAnimationTiming();
            
            // Ensure animation doesn't impact drag responsiveness
            viewModel.EnsureAnimationDoesNotImpactDragResponsiveness();
            
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
            
            Debug.WriteLine("Dual timer system coordination optimized");
        }
    }

    /// <summary>
    /// Gets comprehensive timing metrics for both physics and animation systems
    /// </summary>
    /// <returns>Combined timing metrics</returns>
    public DualTimerCoordinationMetrics GetDualTimerCoordinationMetrics()
    {
        var physicsMetrics = GetTimerPerformanceMetrics();
        var animationMetrics = DataContext is BallViewModel viewModel ? 
            viewModel.GetAnimationTimingMetrics() : 
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
        
        // Get the BallViewModel from the DataContext
        if (DataContext is BallViewModel viewModel)
        {
            // Store the ball's relative position (as a percentage of the window size)
            // before resizing to maintain its relative position
            double relativeX = viewModel.X / oldWidth;
            double relativeY = viewModel.Y / oldHeight;
            
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
                viewModel.X = newX;
                viewModel.Y = newY;
            }
            
            // Constrain the ball position to the new window boundaries
            // This ensures the ball stays within the window even after applying relative positioning
            viewModel.ConstrainPosition(0, 0, MainCanvas.Width, MainCanvas.Height);
            
            // Raise the BallPositionChanged event with the new position
            BallPositionChanged?.Invoke(this, new Point(viewModel.X, viewModel.Y));
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
                if (DataContext is BallViewModel viewModel)
                {
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested visual content switch to: {FilePath}", openFileDialog.FileName);

                    bool success = await viewModel.SwitchBallVisualAsync(openFileDialog.FileName);
                    
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
                if (DataContext is BallViewModel viewModel)
                {
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested static image load: {FilePath}", openFileDialog.FileName);

                    bool success = await viewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
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
                if (DataContext is BallViewModel viewModel)
                {
                    var logService = ServiceBootstrapper.GetService<ILogService>();
                    logService?.LogInformation("User requested GIF animation load: {FilePath}", openFileDialog.FileName);

                    bool success = await viewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
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
                if (DataContext is BallViewModel viewModel)
                {
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

                    bool success = await viewModel.SwitchVisualContentTypeAsync(openFileDialog.FileName);
                    
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

    #endregion Visual Content Switching Event Handlers
}
