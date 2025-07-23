using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BallDragDrop.Models;
using BallDragDrop.Services;
using BallDragDrop.Contracts;

namespace BallDragDrop.ViewModels
{
    /// <summary>
    /// View model for the ball, implementing INotifyPropertyChanged for UI binding
    /// </summary>
    public class BallViewModel : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Command for handling mouse down events
        /// </summary>
        public ICommand MouseDownCommand { get; }

        /// <summary>
        /// Command for handling mouse move events
        /// </summary>
        public ICommand MouseMoveCommand { get; }

        /// <summary>
        /// Command for handling mouse up events
        /// </summary>
        public ICommand MouseUpCommand { get; }

        /// <summary>
        /// Gets or sets the X position of the ball
        /// </summary>
        public double X
        {
            get => _ballModel.X;
            set
            {
                if (_ballModel.X != value)
                {
                    _ballModel.X = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Y position of the ball
        /// </summary>
        public double Y
        {
            get => _ballModel.Y;
            set
            {
                if (_ballModel.Y != value)
                {
                    _ballModel.Y = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the radius of the ball
        /// </summary>
        public double Radius
        {
            get => _ballModel.Radius;
            set
            {
                if (_ballModel.Radius != value)
                {
                    _ballModel.Radius = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the image source for the ball
        /// </summary>
        public ImageSource BallImage
        {
            get => _ballImage;
            set
            {
                if (_ballImage != value)
                {
                    _ballImage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ball visual is animated
        /// </summary>
        public bool IsAnimated
        {
            get => _isAnimated;
            private set
            {
                if (_isAnimated != value)
                {
                    _isAnimated = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the type of visual content currently loaded
        /// </summary>
        public VisualContentType ContentType
        {
            get => _contentType;
            private set
            {
                if (_contentType != value)
                {
                    _contentType = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the ball is currently being dragged
        /// </summary>
        public bool IsDragging
        {
            get => _isDragging;
            set
            {
                if (_isDragging != value)
                {
                    _isDragging = value;
                    OnPropertyChanged();
                    // Update cursor when dragging state changes
                    UpdateCursor();
                }
            }
        }

        /// <summary>
        /// Gets or sets the cursor to display
        /// </summary>
        public Cursor CurrentCursor
        {
            get => _currentCursor;
            set
            {
                if (_currentCursor != value)
                {
                    _currentCursor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the left position for binding (X - Radius)
        /// </summary>
        public double Left => X - Radius;

        /// <summary>
        /// Gets the top position for binding (Y - Radius)
        /// </summary>
        public double Top => Y - Radius;

        /// <summary>
        /// Gets the width for binding (Diameter)
        /// </summary>
        public double Width => Radius * 2;

        /// <summary>
        /// Gets the height for binding (Diameter)
        /// </summary>
        public double Height => Radius * 2;

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the BallViewModel class
        /// </summary>
        /// <param name="logService">Logging service for tracking user interactions</param>
        /// <param name="imageService">Image service for loading and managing visual content</param>
        public BallViewModel(ILogService logService, ImageService imageService = null)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _imageService = imageService ?? new ImageService(logService);
            
            // Initialize with default values - will be set via Initialize method
            _ballModel = new BallModel(0, 0, 25);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            _isAnimated = false;
            _contentType = VisualContentType.Unknown;
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[MouseHistorySize];
            _mouseTimestampHistory = new DateTime[MouseHistorySize];
            _mouseHistoryCount = 0;
            
            // Initialize event throttler for mouse move events
            // Throttle to 60 updates per second (approximately 16ms)
            _mouseMoveThrottler = new EventThrottler(ProcessMouseMove, 16);
            
            // Initialize animation timer for updating frames
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(100); // Default 10 FPS, will be adjusted based on content
            _animationTimer.Tick += OnAnimationTimerTick;
            
            // Initialize commands
            MouseDownCommand = new RelayCommand<MouseEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(OnMouseMove);
            MouseUpCommand = new RelayCommand<MouseEventArgs>(OnMouseUp);
            
            _logService.LogDebug("BallViewModel created with dependency injection");
        }

        /// <summary>
        /// Initializes a new instance of the BallViewModel class for testing
        /// </summary>
        /// <param name="x">Initial X position</param>
        /// <param name="y">Initial Y position</param>
        /// <param name="radius">Ball radius</param>
        /// <param name="imageService">Optional image service for testing</param>
        public BallViewModel(double x, double y, double radius, ImageService imageService = null)
        {
            // For testing, use a null log service or get from app
            _logService = GetLogServiceFromApp();
            _imageService = imageService ?? new ImageService(_logService);
            
            // Initialize with provided values
            _ballModel = new BallModel(x, y, radius);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            _isAnimated = false;
            _contentType = VisualContentType.Unknown;
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[MouseHistorySize];
            _mouseTimestampHistory = new DateTime[MouseHistorySize];
            _mouseHistoryCount = 0;
            
            // Initialize event throttler for mouse move events
            // Throttle to 60 updates per second (approximately 16ms)
            _mouseMoveThrottler = new EventThrottler(ProcessMouseMove, 16);
            
            // Initialize animation timer for updating frames
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(100); // Default 10 FPS, will be adjusted based on content
            _animationTimer.Tick += OnAnimationTimerTick;
            
            // Initialize commands
            MouseDownCommand = new RelayCommand<MouseEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommand<MouseEventArgs>(OnMouseMove);
            MouseUpCommand = new RelayCommand<MouseEventArgs>(OnMouseUp);
            
            _logService?.LogDebug("BallViewModel created for testing at position ({X}, {Y}) with radius {Radius}", x, y, radius);
        }

        #endregion Construction

        #region Constants

        /// <summary>
        /// Size of the mouse position history buffer for velocity calculation
        /// </summary>
        private const int MouseHistorySize = 10;

        #endregion Constants

        #region Fields

        /// <summary>
        /// The underlying ball model (internal for MainWindow access)
        /// </summary>
        internal readonly BallModel _ballModel;

        /// <summary>
        /// Logging service for tracking user interactions
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Image service for loading and managing visual content
        /// </summary>
        private readonly ImageService _imageService;

        /// <summary>
        /// Image source for the ball
        /// </summary>
        private ImageSource _ballImage;

        /// <summary>
        /// Flag indicating whether the ball visual is animated
        /// </summary>
        private bool _isAnimated;

        /// <summary>
        /// Type of visual content currently loaded
        /// </summary>
        private VisualContentType _contentType;

        /// <summary>
        /// Timer for updating animation frames
        /// </summary>
        private readonly DispatcherTimer _animationTimer;

        /// <summary>
        /// Flag indicating whether the ball is currently being dragged
        /// </summary>
        private bool _isDragging;

        /// <summary>
        /// Last recorded mouse position
        /// </summary>
        private Point _lastMousePosition;

        /// <summary>
        /// Position where dragging started
        /// </summary>
        private Point _dragStartPosition;

        /// <summary>
        /// Timestamp of the last update
        /// </summary>
        private DateTime _lastUpdateTime;

        /// <summary>
        /// Current cursor to display
        /// </summary>
        private Cursor _currentCursor;
        
        /// <summary>
        /// Array storing mouse position history for velocity calculation
        /// </summary>
        private Point[] _mousePositionHistory;

        /// <summary>
        /// Array storing mouse timestamp history for velocity calculation
        /// </summary>
        private DateTime[] _mouseTimestampHistory;

        /// <summary>
        /// Number of valid entries in the mouse history arrays
        /// </summary>
        private int _mouseHistoryCount;
        
        /// <summary>
        /// Event throttler for mouse move events
        /// </summary>
        private readonly EventThrottler _mouseMoveThrottler;

        /// <summary>
        /// Last mouse move event arguments for throttled processing
        /// </summary>
        private MouseEventArgs _lastMouseMoveArgs;

        #endregion Fields

        #region Events

        /// <summary>
        /// Event that is raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Methods

        /// <summary>
        /// Initializes the ball position and properties
        /// </summary>
        /// <param name="initialX">Initial X position</param>
        /// <param name="initialY">Initial Y position</param>
        /// <param name="radius">Ball radius</param>
        public void Initialize(double initialX, double initialY, double radius = 25)
        {
            _ballModel.X = initialX;
            _ballModel.Y = initialY;
            _ballModel.Radius = radius;
            
            // Notify property changes
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(Radius));
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            
            _logService.LogDebug("BallViewModel initialized at position ({X}, {Y}) with radius {Radius}", 
                initialX, initialY, radius);
        }

        /// <summary>
        /// Loads ball visual content from the specified file path
        /// </summary>
        /// <param name="filePath">Path to the visual content file</param>
        /// <returns>True if loading was successful, false otherwise</returns>
        public async Task<bool> LoadBallVisualAsync(string filePath)
        {
            _logService?.LogMethodEntry(nameof(LoadBallVisualAsync), filePath);

            try
            {
                // Stop any current animation
                StopAnimation();

                // Load the visual content using ImageService
                bool success = await _imageService.LoadBallVisualAsync(filePath);

                if (success)
                {
                    // Update properties from ImageService
                    BallImage = _imageService.CurrentFrame;
                    IsAnimated = _imageService.IsAnimated;
                    ContentType = _imageService.ContentType;

                    // Start animation if content is animated
                    if (IsAnimated)
                    {
                        StartAnimation();
                    }

                    _logService?.LogInformation("Ball visual loaded successfully: {FilePath} (Animated: {IsAnimated})", 
                        filePath, IsAnimated);
                }
                else
                {
                    _logService?.LogWarning("Failed to load ball visual: {FilePath}", filePath);
                }

                _logService?.LogMethodExit(nameof(LoadBallVisualAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading ball visual: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(LoadBallVisualAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Switches the ball visual content to a new file without restarting the application
        /// Handles transitions between static and animated content while maintaining drag functionality
        /// </summary>
        /// <param name="filePath">Path to the new visual content file</param>
        /// <returns>True if switching was successful, false otherwise</returns>
        public async Task<bool> SwitchBallVisualAsync(string filePath)
        {
            _logService?.LogMethodEntry(nameof(SwitchBallVisualAsync), filePath);

            try
            {
                // Store current state to preserve during transition
                var wasDragging = IsDragging;
                var currentPosition = new Point(X, Y);
                var wasAnimationRunning = IsAnimated && _animationTimer.IsEnabled;

                _logService?.LogDebug("Switching visual content from {CurrentType} to new content. Current state - Dragging: {IsDragging}, Position: ({X}, {Y}), Animation running: {AnimationRunning}", 
                    ContentType, wasDragging, X, Y, wasAnimationRunning);

                // Temporarily stop animation to prevent conflicts during transition
                if (wasAnimationRunning)
                {
                    StopAnimation();
                }

                // Load the new visual content
                bool success = await _imageService.LoadBallVisualAsync(filePath);

                if (success)
                {
                    // Update visual properties from ImageService
                    var newImage = _imageService.CurrentFrame;
                    var newIsAnimated = _imageService.IsAnimated;
                    var newContentType = _imageService.ContentType;

                    // Perform smooth transition by updating properties in the correct order
                    // Update content type first to trigger any necessary UI changes
                    ContentType = newContentType;
                    
                    // Update animation state
                    IsAnimated = newIsAnimated;
                    
                    // Update the image last to ensure smooth visual transition
                    BallImage = newImage;

                    // Maintain ball position during visual change
                    X = currentPosition.X;
                    Y = currentPosition.Y;

                    // Restore animation state if the new content is animated
                    if (IsAnimated)
                    {
                        StartAnimation();
                    }

                    // Maintain drag state if ball was being dragged
                    if (wasDragging)
                    {
                        IsDragging = true;
                        // Ensure animation continues during drag if applicable
                        EnsureAnimationContinuesDuringDrag();
                    }

                    _logService?.LogInformation("Ball visual switched successfully: {FilePath} (Type: {ContentType}, Animated: {IsAnimated}). Drag state maintained: {IsDragging}", 
                        filePath, ContentType, IsAnimated, IsDragging);
                }
                else
                {
                    _logService?.LogWarning("Failed to switch ball visual: {FilePath}. Keeping current visual.", filePath);
                    
                    // Restore animation if it was running before the failed switch
                    if (wasAnimationRunning && IsAnimated)
                    {
                        StartAnimation();
                    }
                }

                _logService?.LogMethodExit(nameof(SwitchBallVisualAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error switching ball visual: {FilePath}", filePath);
                
                // Try to restore previous state on error
                try
                {
                    if (IsAnimated && !_animationTimer.IsEnabled)
                    {
                        StartAnimation();
                    }
                }
                catch (Exception restoreEx)
                {
                    _logService?.LogError(restoreEx, "Error restoring animation state after failed visual switch");
                }
                
                _logService?.LogMethodExit(nameof(SwitchBallVisualAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Switches between different visual content types (static to animated or vice versa)
        /// while maintaining all ball functionality
        /// </summary>
        /// <param name="filePath">Path to the new visual content file</param>
        /// <param name="preserveAnimationState">Whether to preserve animation playback state during transition</param>
        /// <returns>True if switching was successful, false otherwise</returns>
        public async Task<bool> SwitchVisualContentTypeAsync(string filePath, bool preserveAnimationState = true)
        {
            _logService?.LogMethodEntry(nameof(SwitchVisualContentTypeAsync), filePath, preserveAnimationState);

            try
            {
                var previousContentType = ContentType;
                var previousAnimationState = IsAnimated && _animationTimer.IsEnabled;
                
                // Perform the visual switch
                bool success = await SwitchBallVisualAsync(filePath);
                
                if (success)
                {
                    // Log the transition type
                    string transitionType = GetTransitionType(previousContentType, ContentType);
                    _logService?.LogInformation("Visual content type transition completed: {TransitionType}. Animation state preserved: {PreserveState}", 
                        transitionType, preserveAnimationState && previousAnimationState && IsAnimated);
                    
                    // Handle specific transition scenarios
                    if (previousContentType == VisualContentType.StaticImage && IsAnimated)
                    {
                        _logService?.LogDebug("Transitioned from static image to animation - starting animation playback");
                    }
                    else if (previousContentType != VisualContentType.StaticImage && !IsAnimated)
                    {
                        _logService?.LogDebug("Transitioned from animation to static image - animation stopped");
                    }
                }
                
                _logService?.LogMethodExit(nameof(SwitchVisualContentTypeAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error switching visual content type: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(SwitchVisualContentTypeAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Gets a description of the transition type between two content types
        /// </summary>
        /// <param name="fromType">The previous content type</param>
        /// <param name="toType">The new content type</param>
        /// <returns>A string describing the transition</returns>
        private string GetTransitionType(VisualContentType fromType, VisualContentType toType)
        {
            if (fromType == toType)
            {
                return $"Same type ({fromType})";
            }
            
            if (fromType == VisualContentType.StaticImage && toType != VisualContentType.StaticImage)
            {
                return $"Static to Animated ({toType})";
            }
            
            if (fromType != VisualContentType.StaticImage && toType == VisualContentType.StaticImage)
            {
                return $"Animated ({fromType}) to Static";
            }
            
            return $"Animation type change ({fromType} to {toType})";
        }

        /// <summary>
        /// Starts animation playback if the current content is animated
        /// </summary>
        private void StartAnimation()
        {
            _logService?.LogMethodEntry(nameof(StartAnimation));

            if (IsAnimated)
            {
                // Update timer interval based on frame duration
                if (_imageService.FrameDuration > TimeSpan.Zero)
                {
                    _animationTimer.Interval = _imageService.FrameDuration;
                }
                else
                {
                    _animationTimer.Interval = TimeSpan.FromMilliseconds(100); // Default 10 FPS
                }

                _imageService.StartAnimation();
                _animationTimer.Start();

                _logService?.LogDebug("Animation started with interval: {Interval}ms", _animationTimer.Interval.TotalMilliseconds);
            }

            _logService?.LogMethodExit(nameof(StartAnimation));
        }

        /// <summary>
        /// Stops animation playback
        /// </summary>
        private void StopAnimation()
        {
            _logService?.LogMethodEntry(nameof(StopAnimation));

            _animationTimer.Stop();
            _imageService.StopAnimation();

            _logService?.LogDebug("Animation stopped");
            _logService?.LogMethodExit(nameof(StopAnimation));
        }

        /// <summary>
        /// Handles animation timer tick events to update frames with optimized rendering
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAnimationTimerTick(object sender, EventArgs e)
        {
            if (IsAnimated)
            {
                // Update frame in ImageService
                _imageService.UpdateFrame();
                
                // Get the new frame
                var newFrame = _imageService.CurrentFrame;
                
                // Only update BallImage if the frame actually changed to prevent unnecessary redraws
                if (newFrame != null && !ReferenceEquals(BallImage, newFrame))
                {
                    // Use Dispatcher.BeginInvoke to ensure smooth frame updates on UI thread
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BallImage = newFrame;
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }

                // Update timer interval if frame duration changed
                if (_imageService.FrameDuration > TimeSpan.Zero && _animationTimer.Interval != _imageService.FrameDuration)
                {
                    _animationTimer.Interval = _imageService.FrameDuration;
                    _logService?.LogTrace("Animation timer interval updated to {Interval}ms", _animationTimer.Interval.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        /// Ensures animation continues during drag operations by keeping the timer running
        /// This method is called during drag operations to maintain animation playback
        /// </summary>
        public void EnsureAnimationContinuesDuringDrag()
        {
            if (IsAnimated && !_animationTimer.IsEnabled)
            {
                _animationTimer.Start();
                _logService?.LogDebug("Animation timer restarted during drag operation");
            }
        }

        /// <summary>
        /// Coordinates animation timing with physics updates by synchronizing frame updates
        /// This method should be called from the physics update loop to ensure smooth coordination
        /// </summary>
        public void CoordinateAnimationWithPhysics()
        {
            if (IsAnimated)
            {
                // Force an animation frame update to coordinate with physics timing
                // This ensures animation frames are updated in sync with physics calculations
                OnAnimationTimerTick(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Optimizes animation rendering by pre-loading frames and setting up efficient rendering
        /// </summary>
        public void OptimizeAnimationRendering()
        {
            if (IsAnimated)
            {
                // Ensure the animation timer uses optimal priority for smooth playback
                _animationTimer.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Force a frame update to ensure current frame is properly loaded
                    _imageService.UpdateFrame();
                    var currentFrame = _imageService.CurrentFrame;
                    if (currentFrame != null)
                    {
                        // Freeze the image source for better performance
                        if (currentFrame.CanFreeze && !currentFrame.IsFrozen)
                        {
                            currentFrame.Freeze();
                        }
                        BallImage = currentFrame;
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);

                _logService?.LogDebug("Animation rendering optimized");
            }
        }

        /// <summary>
        /// Ensures visual quality is maintained during animation playback
        /// </summary>
        public void EnsureAnimationVisualQuality()
        {
            if (IsAnimated)
            {
                // Verify that the current frame is properly loaded and rendered
                var currentFrame = _imageService.CurrentFrame;
                if (currentFrame != null)
                {
                    // Ensure the frame is frozen for optimal rendering performance
                    if (currentFrame.CanFreeze && !currentFrame.IsFrozen)
                    {
                        currentFrame.Freeze();
                    }
                    
                    // Update the bound image if needed
                    if (!ReferenceEquals(BallImage, currentFrame))
                    {
                        BallImage = currentFrame;
                    }
                }

                _logService?.LogTrace("Animation visual quality ensured");
            }
        }

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        /// <param name="e">Mouse event arguments</param>
        private void OnMouseDown(MouseEventArgs e)
        {
            if (e == null) return;

            // Get the position of the mouse click
            var position = e.GetPosition(null);

            // Check if the click is inside the ball
            if (_ballModel.ContainsPoint(position.X, position.Y))
            {
                // Log user interaction
                _logService?.LogInformation("User started dragging ball at position ({X}, {Y})", position.X, position.Y);
                
                // Start dragging
                IsDragging = true;
                _lastMousePosition = position;
                _dragStartPosition = new Point(X, Y);
                _lastUpdateTime = DateTime.Now;

                // Stop any current movement
                _ballModel.Stop();

                // Ensure animation continues during drag operations
                EnsureAnimationContinuesDuringDrag();

                // Capture the mouse
                Mouse.Capture((IInputElement)e.Source);
                
                // Reset mouse history when starting a new drag
                _mouseHistoryCount = 0;
                
                _logService?.LogDebug("Ball drag initiated - mouse captured, movement stopped, animation maintained");
            }
            else
            {
                _logService?.LogTrace("Mouse click outside ball bounds at ({X}, {Y})", position.X, position.Y);
            }
        }

        /// <summary>
        /// Handles mouse move events with throttling
        /// </summary>
        /// <param name="e">Mouse event arguments</param>
        private void OnMouseMove(MouseEventArgs e)
        {
            if (e == null) return;
            
            // Store the event args for processing in the throttled method
            _lastMouseMoveArgs = e;
            
            // If we're dragging, process immediately for responsive feel
            if (IsDragging)
            {
                _mouseMoveThrottler.ExecuteNow();
            }
            else
            {
                // Otherwise, throttle the processing to reduce CPU usage
                _mouseMoveThrottler.Execute();
            }
        }
        
        /// <summary>
        /// Processes mouse move events at a throttled rate
        /// </summary>
        private void ProcessMouseMove()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Use the last stored mouse event args
            var e = _lastMouseMoveArgs;
            if (e == null) return;

            var position = e.GetPosition(null);
            var currentTime = DateTime.Now;

            // If dragging, update the ball position and track movement history
            if (IsDragging)
            {
                // Calculate the movement delta
                double deltaX = position.X - _lastMousePosition.X;
                double deltaY = position.Y - _lastMousePosition.Y;

                // Log significant movements at debug level
                if (Math.Abs(deltaX) > 5 || Math.Abs(deltaY) > 5)
                {
                    _logService?.LogTrace("Ball dragged by delta ({DeltaX:F1}, {DeltaY:F1}) to position ({X:F1}, {Y:F1})", 
                        deltaX, deltaY, X + deltaX, Y + deltaY);
                }

                // Update the ball position
                X += deltaX;
                Y += deltaY;
                
                // Get the current window size from the Application's main window
                double windowWidth = 0;
                double windowHeight = 0;
                
                if (Application.Current != null && Application.Current.MainWindow != null)
                {
                    windowWidth = Application.Current.MainWindow.ActualWidth;
                    windowHeight = Application.Current.MainWindow.ActualHeight;
                }
                
                // If we can't get the window size, use reasonable defaults
                if (windowWidth <= 0) windowWidth = 800;
                if (windowHeight <= 0) windowHeight = 600;
                
                // Constrain the ball position to stay within the window boundaries
                ConstrainPosition(0, 0, windowWidth, windowHeight);

                // Store mouse position and timestamp in history arrays
                StoreMousePosition(position, currentTime);
                
                // Update the last update time
                _lastUpdateTime = currentTime;
            }
            
            // Store the current mouse position for next update and cursor feedback
            _lastMousePosition = position;

            // Update cursor based on position and dragging state
            UpdateCursor();
            
            stopwatch.Stop();
            
            // Log performance metrics for mouse processing at debug level
            if (stopwatch.ElapsedMilliseconds > 5) // Only log if processing took more than 5ms
            {
                _logService?.LogDebug("Mouse move processing took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
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
            if (_mouseHistoryCount >= MouseHistorySize)
            {
                for (int i = 0; i < MouseHistorySize - 1; i++)
                {
                    _mousePositionHistory[i] = _mousePositionHistory[i + 1];
                    _mouseTimestampHistory[i] = _mouseTimestampHistory[i + 1];
                }
                
                // Add the new position and timestamp at the end
                _mousePositionHistory[MouseHistorySize - 1] = position;
                _mouseTimestampHistory[MouseHistorySize - 1] = timestamp;
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
        /// Handles mouse up events
        /// </summary>
        /// <param name="e">Mouse event arguments</param>
        private void OnMouseUp(MouseEventArgs e)
        {
            if (IsDragging)
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Log the end of dragging
                _logService?.LogInformation("User released ball at position ({X:F1}, {Y:F1})", X, Y);
                
                // Stop dragging
                IsDragging = false;

                // Release mouse capture
                Mouse.Capture(null);

                // Calculate velocity based on movement for throwing
                if (e != null)
                {
                    var currentPosition = e.GetPosition(null);
                    var currentTime = DateTime.Now;
                    
                    // Store the final position in the history
                    StoreMousePosition(currentPosition, currentTime);
                    
                    // Create a physics engine instance to calculate velocity
                    var physicsEngine = new Models.PhysicsEngine();
                    
                    // Calculate velocity using the mouse movement history for more accuracy
                    var (velocityX, velocityY) = physicsEngine.CalculateVelocityFromHistory(
                        _mousePositionHistory, 
                        _mouseTimestampHistory, 
                        _mouseHistoryCount);
                    
                    // If we don't have enough history or the calculation failed, fall back to simple calculation
                    if (Math.Abs(velocityX) < 0.001 && Math.Abs(velocityY) < 0.001 && _mouseHistoryCount > 1)
                    {
                        // Calculate time elapsed since last update
                        double timeElapsed = (currentTime - _lastUpdateTime).TotalSeconds;
                        
                        // Only calculate velocity if enough time has passed to avoid division by very small numbers
                        if (timeElapsed > 0.001)
                        {
                            // Calculate distance moved
                            double deltaX = currentPosition.X - _lastMousePosition.X;
                            double deltaY = currentPosition.Y - _lastMousePosition.Y;
                            
                            // Calculate velocity using simple method
                            (velocityX, velocityY) = physicsEngine.CalculateVelocity(deltaX, deltaY, timeElapsed);
                        }
                    }
                    
                    // Lower the threshold for throwing to make it easier to throw the ball
                    double throwThreshold = 100.0; // Reduced from default 200.0
                    
                    // Check if the movement is fast enough to be considered a throw
                    if (physicsEngine.IsThrow(velocityX, velocityY, throwThreshold))
                    {
                        // Log the throw action
                        _logService?.LogInformation("Ball thrown with velocity ({VelX:F1}, {VelY:F1})", velocityX, velocityY);
                        
                        // Apply the velocity to the ball model
                        _ballModel.SetVelocity(velocityX, velocityY);
                    }
                    else
                    {
                        // Not a throw, stop the ball
                        _logService?.LogDebug("Ball dropped (velocity too low for throw): ({VelX:F1}, {VelY:F1})", velocityX, velocityY);
                        _ballModel.Stop();
                    }
                    
                    _lastMousePosition = currentPosition;
                    
                    // Reset the mouse history count for the next drag
                    _mouseHistoryCount = 0;
                }
                else
                {
                    // No event data, stop the ball
                    _logService?.LogDebug("Ball released without event data - stopping movement");
                    _ballModel.Stop();
                }
                
                // Update the cursor after releasing the ball
                UpdateCursor();
                
                stopwatch.Stop();
                
                // Log performance metrics for physics calculations at debug level
                _logService?.LogPerformance("Ball release processing", stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Updates the cursor based on the current state
        /// </summary>
        private void UpdateCursor()
        {
            if (IsDragging)
            {
                // When dragging the ball, show the "SizeAll" cursor to indicate movement
                CurrentCursor = Cursors.SizeAll;
            }
            else if (_ballModel.ContainsPoint(_lastMousePosition.X, _lastMousePosition.Y))
            {
                // When hovering over the ball, show the "Hand" cursor to indicate it can be grabbed
                CurrentCursor = Cursors.Hand;
            }
            else
            {
                // Default cursor when not interacting with the ball
                CurrentCursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Constrains the ball position to the specified boundaries
        /// </summary>
        /// <param name="minX">Minimum X coordinate</param>
        /// <param name="minY">Minimum Y coordinate</param>
        /// <param name="maxX">Maximum X coordinate</param>
        /// <param name="maxY">Maximum Y coordinate</param>
        public void ConstrainPosition(double minX, double minY, double maxX, double maxY)
        {
            var originalX = X;
            var originalY = Y;
            
            bool wasConstrained = _ballModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            if (wasConstrained)
            {
                _logService?.LogDebug("Ball position constrained from ({OriginalX:F1}, {OriginalY:F1}) to ({NewX:F1}, {NewY:F1})", 
                    originalX, originalY, X, Y);
                
                // Notify UI that position properties have changed
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(Y));
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Top));
            }
        }
        
        /// <summary>
        /// Gets the log service from the application instance
        /// </summary>
        /// <returns>The log service or null if not available</returns>
        private static ILogService GetLogServiceFromApp()
        {
            try
            {
                if (Application.Current is App app)
                {
                    return app.GetLogService();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get log service from app: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Update dependent properties
            if (propertyName == nameof(X) || propertyName == nameof(Y) || propertyName == nameof(Radius))
            {
                if (propertyName == nameof(X) || propertyName == nameof(Radius))
                {
                    OnPropertyChanged(nameof(Left));
                    OnPropertyChanged(nameof(Width));
                }

                if (propertyName == nameof(Y) || propertyName == nameof(Radius))
                {
                    OnPropertyChanged(nameof(Top));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        #endregion Methods
    }

    /// <summary>
    /// A simple implementation of ICommand for the view model
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        #region Properties

        /// <summary>
        /// Event that is raised when the ability to execute the command changes
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the RelayCommand class
        /// </summary>
        /// <param name="execute">The execution logic</param>
        /// <param name="canExecute">The execution status logic</param>
        /// <exception cref="ArgumentNullException">Thrown when execute is null</exception>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// The execution logic delegate
        /// </summary>
        private readonly Action<T> _execute;

        /// <summary>
        /// The execution status logic delegate
        /// </summary>
        private readonly Predicate<T> _canExecute;

        #endregion Fields

        #region Methods

        /// <summary>
        /// Determines whether this command can execute in its current state
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <returns>True if this command can be executed; otherwise, false</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        #endregion Methods
    }
}