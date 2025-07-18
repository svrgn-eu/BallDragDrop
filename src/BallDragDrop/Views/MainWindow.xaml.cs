using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using BallDragDrop.ViewModels;
using Path = System.IO.Path;

namespace BallDragDrop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Event that will be raised when the ball position changes due to window resize
    public event EventHandler<Point>? BallPositionChanged;
    
    // Physics engine for ball movement
    private Models.PhysicsEngine _physicsEngine;
    
    // Last time the physics was updated
    private DateTime _lastPhysicsUpdate;
    
    // Flag to track if physics simulation is running
    private bool _isPhysicsRunning;
    
    // Performance monitoring and optimization
    private Services.PerformanceMonitor _performanceMonitor;
    
    // Debug counter for physics updates
    private int _physicsUpdateCounter = 0;
    
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
        
        // Set up rendering event for physics updates
        CompositionTarget.Rendering += CompositionTarget_Rendering;
        
        Debug.WriteLine("MainWindow initialized");
    }
    
    /// <summary>
    /// Event handler for window closed event
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event data</param>
    private void MainWindow_Closed(object sender, EventArgs e)
    {
        // Clean up event handlers
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
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
        
        // Create a new BallViewModel with the center position
        BallViewModel viewModel = new BallViewModel(centerX, centerY, ballRadius);
        
        // Load the ball image with optimized settings
        try
        {
            // Try to load the image from the Resources folder
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "Ball", "Ball01.png");
            string fullPath = Path.GetFullPath(imagePath);
            
            if (File.Exists(fullPath))
            {
                // Performance optimization: Load image with optimized settings
                BitmapImage ballImage = new BitmapImage();
                ballImage.BeginInit();
                ballImage.UriSource = new Uri(fullPath);
                ballImage.CacheOption = BitmapCacheOption.OnLoad; // Load image into memory immediately
                ballImage.CreateOptions = BitmapCreateOptions.None; // Don't delay creation
                ballImage.DecodePixelWidth = (int)(ballRadius * 2); // Only decode to the size we need
                ballImage.DecodePixelHeight = (int)(ballRadius * 2);
                ballImage.EndInit();
                
                // Freeze the image to make it immutable and improve performance
                if (ballImage.CanFreeze)
                {
                    ballImage.Freeze();
                }
                
                // Set the image source in the view model
                viewModel.BallImage = ballImage;
            }
            else
            {
                throw new FileNotFoundException($"Ball image not found at: {fullPath}");
            }
        }
        catch (Exception ex)
        {
            // If the image can't be loaded, create a fallback image (a simple circle)
            // Performance optimization: Create a more efficient fallback image
            int size = (int)(ballRadius * 2);
            
            // Create a DrawingVisual for the fallback image
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Draw a simple circle
                drawingContext.DrawEllipse(
                    Brushes.Red,
                    new Pen(Brushes.DarkRed, 2),
                    new Point(ballRadius, ballRadius),
                    ballRadius,
                    ballRadius);
            }
            
            // Render the drawing to a bitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                size, size,
                96, 96,
                PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            
            // Freeze the bitmap to make it immutable and improve performance
            if (renderBitmap.CanFreeze)
            {
                renderBitmap.Freeze();
            }
            
            // Set the fallback image in the view model
            viewModel.BallImage = renderBitmap;
            
            // Log the error
            Debug.WriteLine($"Error loading ball image: {ex.Message}");
        }
        
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
                // Execute the mouse down command
                if (viewModel.MouseDownCommand.CanExecute(e))
                {
                    viewModel.MouseDownCommand.Execute(e);
                }
                
                // Stop the physics simulation
                _isPhysicsRunning = false;
                viewModel._ballModel.Stop();
                
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
                // Start the physics simulation
                _isPhysicsRunning = true;
                _lastPhysicsUpdate = DateTime.Now;
                _physicsUpdateCounter = 0;
                
                Debug.WriteLine($"Ball thrown with velocity: ({viewModel._ballModel.VelocityX:F2}, {viewModel._ballModel.VelocityY:F2})");
            }
        }
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
    
    #region Test Helpers
    
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
    
    #endregion
    
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
            // Check if the ball is moving (has velocity) and not being dragged
            if (!viewModel.IsDragging && _isPhysicsRunning)
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
}