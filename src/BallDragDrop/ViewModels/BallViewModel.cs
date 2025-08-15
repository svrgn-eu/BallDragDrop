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
using BallDragDrop.Commands;

namespace BallDragDrop.ViewModels
{
    /// <summary>
    /// View model for the ball, implementing INotifyPropertyChanged for UI binding and IBallStateObserver for state machine integration
    /// </summary>
    public class BallViewModel : INotifyPropertyChanged, IBallStateObserver, IDisposable
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
        /// Command for handling mouse enter events
        /// </summary>
        public ICommand MouseEnterCommand { get; }

        /// <summary>
        /// Command for handling mouse leave events
        /// </summary>
        public ICommand MouseLeaveCommand { get; }

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
                    double oldValue = _ballModel.X;
                    _ballModel.X = value;
                    OnPropertyChanged();
                    
                    // Debug logging to verify X property updates and PropertyChanged firing
                    if (_logService != null && DateTime.Now.Millisecond % 100 < 20)
                    {
                        _logService.LogInformation($"PROPERTY UPDATE: X changed from {oldValue:F2} to {value:F2}, Left={Left:F2}");
                    }
                }
                else
                {
                    // Debug: Log when X setter is called but value doesn't change
                    if (_logService != null && DateTime.Now.Millisecond % 200 < 20)
                    {
                        _logService.LogWarning($"PROPERTY NO-CHANGE: X setter called with same value {value:F2}");
                    }
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
                    double oldValue = _ballModel.Y;
                    _ballModel.Y = value;
                    OnPropertyChanged();
                    
                    // Debug logging to verify Y property updates and PropertyChanged firing
                    if (_logService != null && DateTime.Now.Millisecond % 100 < 20)
                    {
                        _logService.LogInformation($"PROPERTY UPDATE: Y changed from {oldValue:F2} to {value:F2}, Top={Top:F2}");
                    }
                }
                else
                {
                    // Debug: Log when Y setter is called but value doesn't change
                    if (_logService != null && DateTime.Now.Millisecond % 200 < 20)
                    {
                        _logService.LogWarning($"PROPERTY NO-CHANGE: Y setter called with same value {value:F2}");
                    }
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
        /// Gets the name of the currently loaded asset
        /// </summary>
        public string AssetName
        {
            get => _assetName;
            private set
            {
                if (_assetName != value)
                {
                    _assetName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the ball is currently being dragged
        /// This property reflects the Held state from the state machine
        /// </summary>
        public bool IsDragging
        {
            get => _stateMachine?.CurrentState == BallState.Held || _isDragging;
            set
            {
                if (_isDragging != value)
                {
                    _isDragging = value;
                    OnPropertyChanged();
                    // Update cursor when dragging state changes
                    UpdateCursor();
                    
                    // Use optimized dual timer system if enabled
                    if (_isDualTimerOptimized)
                    {
                        EnsureDualTimerDragResponsiveness();
                    }
                    else
                    {
                        // Fallback to original method
                        EnsureAnimationDoesNotImpactDragResponsiveness();
                    }
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

        /// <summary>
        /// Gets or sets whether to show the bounding box for debugging
        /// </summary>
        public bool ShowBoundingBox
        {
            get => _showBoundingBox;
            set
            {
                if (_showBoundingBox != value)
                {
                    _showBoundingBox = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current state of the ball from the state machine
        /// </summary>
        public BallState CurrentState => _stateMachine?.CurrentState ?? BallState.Idle;

        /// <summary>
        /// Gets the current hand state from the hand state machine
        /// </summary>
        public HandState CurrentHandState => _handStateMachine?.CurrentState ?? HandState.Default;

        /// <summary>
        /// Gets a value indicating whether the ball is in the Idle state
        /// </summary>
        public bool IsInIdleState => CurrentState == BallState.Idle;

        /// <summary>
        /// Gets a value indicating whether the ball is in the Held state
        /// </summary>
        public bool IsInHeldState => CurrentState == BallState.Held;

        /// <summary>
        /// Gets a value indicating whether the ball is in the Thrown state
        /// </summary>
        public bool IsInThrownState => CurrentState == BallState.Thrown;

        /// <summary>
        /// Gets the visual opacity for the ball based on its current state
        /// </summary>
        public double StateOpacity
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => 1.0,      // Full opacity when idle
                    BallState.Held => 0.8,      // Slightly transparent when held
                    BallState.Thrown => 1.0,    // Full opacity when thrown
                    _ => 1.0
                };
            }
        }

        /// <summary>
        /// Gets the visual scale factor for the ball based on its current state
        /// </summary>
        public double StateScale
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => 1.0,      // Normal size when idle
                    BallState.Held => 1.1,      // Slightly larger when held
                    BallState.Thrown => 1.0,    // Normal size when thrown
                    _ => 1.0
                };
            }
        }

        /// <summary>
        /// Gets the visual glow effect intensity for the ball based on its current state
        /// </summary>
        public double StateGlowRadius
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => 0.0,      // No glow when idle
                    BallState.Held => 8.0,      // Glow when held
                    BallState.Thrown => 4.0,    // Subtle glow when thrown
                    _ => 0.0
                };
            }
        }

        /// <summary>
        /// Gets the visual glow color for the ball based on its current state
        /// </summary>
        public Color StateGlowColor
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => Colors.Transparent,     // No glow when idle
                    BallState.Held => Colors.LightBlue,       // Blue glow when held
                    BallState.Thrown => Colors.Orange,        // Orange glow when thrown
                    _ => Colors.Transparent
                };
            }
        }

        /// <summary>
        /// Gets the visual border thickness for the ball based on its current state
        /// </summary>
        public double StateBorderThickness
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => 0.0,      // No border when idle
                    BallState.Held => 2.0,      // Thick border when held
                    BallState.Thrown => 1.0,    // Thin border when thrown
                    _ => 0.0
                };
            }
        }

        /// <summary>
        /// Gets the visual border color for the ball based on its current state
        /// </summary>
        public Color StateBorderColor
        {
            get
            {
                return CurrentState switch
                {
                    BallState.Idle => Colors.Transparent,     // No border when idle
                    BallState.Held => Colors.Blue,            // Blue border when held
                    BallState.Thrown => Colors.Red,           // Red border when thrown
                    _ => Colors.Transparent
                };
            }
        }

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the BallViewModel class
        /// </summary>
        /// <param name="logService">Logging service for tracking user interactions</param>
        /// <param name="stateMachine">State machine for managing ball state transitions</param>
        /// <param name="handStateMachine">Hand state machine for managing cursor states</param>
        /// <param name="imageService">Image service for loading and managing visual content</param>
        /// <param name="configurationService">Configuration service for accessing application settings</param>
        public BallViewModel(ILogService logService, IBallStateMachine stateMachine, IHandStateMachine handStateMachine, ImageService imageService = null, IConfigurationService configurationService = null)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _handStateMachine = handStateMachine ?? throw new ArgumentNullException(nameof(handStateMachine));
            _imageService = imageService ?? new ImageService(logService);
            _configurationService = configurationService;
            
            // Get default ball size from configuration, fallback to 25 if not available
            double defaultBallSize = GetDefaultBallSizeFromConfiguration();
            
            // Initialize with default values - will be set via Initialize method
            _ballModel = new BallModel(0, 0, defaultBallSize);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            _isAnimated = false;
            _contentType = VisualContentType.Unknown;
            _assetName = "No Asset";
            _showBoundingBox = GetShowBoundingBoxFromConfiguration();
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[Constants.MOUSE_HISTORY_SIZE];
            _mouseTimestampHistory = new DateTime[Constants.MOUSE_HISTORY_SIZE];
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
            MouseEnterCommand = new RelayCommand<MouseEventArgs>(OnMouseEnter);
            MouseLeaveCommand = new RelayCommand<MouseEventArgs>(OnMouseLeave);

            // Subscribe to state machine notifications
            _stateMachine.Subscribe(this);
            
            // Subscribe to hand state machine notifications
            if (_handStateMachine != null)
            {
                _handStateMachine.StateChanged += OnHandStateChanged;
            }

            // Load default ball image asynchronously (fire-and-forget with error handling)
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadDefaultBallImageAsync();
                }
                catch (Exception ex)
                {
                    _logService?.LogError(ex, "Error loading default ball image during construction");
                }
            });
            
            _logService.LogDebug("BallViewModel created with dependency injection");
        }

        /// <summary>
        /// Initializes a new instance of the BallViewModel class for testing
        /// </summary>
        /// <param name="x">Initial X position</param>
        /// <param name="y">Initial Y position</param>
        /// <param name="radius">Ball radius</param>
        /// <param name="stateMachine">State machine for managing ball state transitions</param>
        /// <param name="handStateMachine">Hand state machine for managing cursor states</param>
        /// <param name="imageService">Optional image service for testing</param>
        public BallViewModel(double x, double y, double radius, IBallStateMachine stateMachine = null, IHandStateMachine handStateMachine = null, ImageService imageService = null)
        {
            // For testing, use a null log service or get from app
            _logService = GetLogServiceFromApp();
            _stateMachine = stateMachine; // Allow null for testing
            _handStateMachine = handStateMachine; // Allow null for testing
            _imageService = imageService ?? new ImageService(_logService);
            
            // Initialize with provided values
            _ballModel = new BallModel(x, y, radius);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            _isAnimated = false;
            _contentType = VisualContentType.Unknown;
            _assetName = "No Asset";
            _showBoundingBox = false; // Default to false for testing
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[Constants.MOUSE_HISTORY_SIZE];
            _mouseTimestampHistory = new DateTime[Constants.MOUSE_HISTORY_SIZE];
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
            MouseEnterCommand = new RelayCommand<MouseEventArgs>(OnMouseEnter);
            MouseLeaveCommand = new RelayCommand<MouseEventArgs>(OnMouseLeave);

            // Subscribe to state machine notifications if available
            _stateMachine?.Subscribe(this);
            
            // Subscribe to hand state machine notifications if available
            if (_handStateMachine != null)
            {
                _handStateMachine.StateChanged += OnHandStateChanged;
            }
            
            _logService?.LogDebug("BallViewModel created for testing at position ({0}, {1}) with radius {2}", x, y, radius);
        }

        #endregion Construction

        #region Constants



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
        /// Configuration service for accessing application settings
        /// </summary>
        private readonly IConfigurationService _configurationService;

        /// <summary>
        /// State machine for managing ball state transitions
        /// </summary>
        private readonly IBallStateMachine _stateMachine;

        /// <summary>
        /// Hand state machine for managing cursor states
        /// </summary>
        private readonly IHandStateMachine _handStateMachine;

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
        /// Name of the currently loaded asset
        /// </summary>
        private string _assetName;

        /// <summary>
        /// Timer for updating animation frames (optimized for source frame rates)
        /// </summary>
        private DispatcherTimer _animationTimer;

        /// <summary>
        /// Flag to track if dual timer optimization is enabled
        /// </summary>
        private bool _isDualTimerOptimized = false;

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
        /// Flag indicating whether to show the bounding box for debugging
        /// </summary>
        private bool _showBoundingBox;
        
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

        /// <summary>
        /// Timestamp of the last animation frame update for coordination with physics
        /// </summary>
        private DateTime _lastAnimationUpdate = DateTime.Now;

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
        /// <param name="radius">Ball radius (if not provided, uses configuration default)</param>
        public void Initialize(double initialX, double initialY, double? radius = null)
        {
            _ballModel.X = initialX;
            _ballModel.Y = initialY;
            _ballModel.Radius = radius ?? GetDefaultBallSizeFromConfiguration();
            
            // Notify property changes
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(Radius));
            OnPropertyChanged(nameof(Left));
            OnPropertyChanged(nameof(Top));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            
            _logService.LogDebug("BallViewModel initialized at position ({0}, {1}) with radius {2}", 
                initialX, initialY, radius);
        }

        /// <summary>
        /// Resets the ball to its initial state and position
        /// </summary>
        /// <param name="centerX">Center X position to reset to</param>
        /// <param name="centerY">Center Y position to reset to</param>
        public void ResetBall(double centerX, double centerY)
        {
            try
            {
                _logService?.LogDebug("ResetBall called with center position ({CenterX:F2}, {CenterY:F2})", centerX, centerY);

                // Stop any ongoing animation
                if (IsAnimated && _animationTimer.IsEnabled)
                {
                    _animationTimer.Stop();
                    _logService?.LogDebug("Animation stopped during reset");
                }

                // Clear drag state
                if (_isDragging)
                {
                    _isDragging = false;
                    OnPropertyChanged(nameof(IsDragging));
                    _logService?.LogDebug("Drag state cleared during reset");
                }

                // Clear mouse tracking history
                _mouseHistoryCount = 0;
                _logService?.LogDebug("Mouse tracking history cleared during reset");

                // Reset ball position to center
                _ballModel.X = centerX;
                _ballModel.Y = centerY;

                // Clear ball velocity
                _ballModel.Stop();

                // Notify property changes for position
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(Y));
                OnPropertyChanged(nameof(Left));
                OnPropertyChanged(nameof(Top));

                // Trigger state machine reset if available
                if (_stateMachine != null && _stateMachine.CanFire(BallTrigger.Reset))
                {
                    _stateMachine.Fire(BallTrigger.Reset);
                    _logService?.LogDebug("State machine reset triggered");
                }
                else
                {
                    _logService?.LogWarning("State machine not available or cannot fire Reset trigger");
                }

                // Restart animation if the content is animated
                if (IsAnimated)
                {
                    _animationTimer.Start();
                    _logService?.LogDebug("Animation restarted after reset");
                }

                _logService?.LogInformation("Ball reset completed - position: ({CenterX:F2}, {CenterY:F2}), state: {CurrentState}", 
                    centerX, centerY, CurrentState);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error during ball reset operation");
                throw;
            }
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

                    // Extract and set asset name from file path
                    AssetName = ExtractAssetNameFromPath(filePath);

                    // Start animation if content is animated
                    if (IsAnimated)
                    {
                        StartAnimation();
                    }

                    _logService?.LogInformation("Ball visual loaded successfully: {FilePath} (Animated: {IsAnimated}, Asset: {AssetName})", 
                        filePath, IsAnimated, AssetName);
                }
                else
                {
                    // Reset to default asset name on failure
                    AssetName = "No Asset";
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
        /// Loads the default ball image from configuration
        /// </summary>
        /// <returns>True if loading was successful, false otherwise</returns>
        public async Task<bool> LoadDefaultBallImageAsync()
        {
            _logService?.LogMethodEntry(nameof(LoadDefaultBallImageAsync));

            try
            {
                if (_configurationService == null)
                {
                    _logService?.LogWarning("Configuration service not available, cannot load default ball image");
                    _logService?.LogMethodExit(nameof(LoadDefaultBallImageAsync), false);
                    return false;
                }

                var defaultImagePath = _configurationService.GetDefaultBallImagePath();
                _logService?.LogDebug("Loading default ball image from configuration: {ImagePath}", defaultImagePath);

                // Validate the image path
                if (!_configurationService.ValidateImagePath(defaultImagePath))
                {
                    _logService?.LogWarning("Default ball image path is invalid: {ImagePath}", defaultImagePath);
                    
                    // Try to use a fallback image
                    var fallbackPath = "./Resources/Images/Ball01.png";
                    if (_configurationService.ValidateImagePath(fallbackPath))
                    {
                        _logService?.LogInformation("Using fallback image path: {FallbackPath}", fallbackPath);
                        defaultImagePath = fallbackPath;
                        
                        // Update configuration with the working fallback path
                        _configurationService.SetDefaultBallImagePath(fallbackPath);
                    }
                    else
                    {
                        _logService?.LogError("Both default and fallback image paths are invalid");
                        _logService?.LogMethodExit(nameof(LoadDefaultBallImageAsync), false);
                        return false;
                    }
                }

                // Load the ball visual
                bool success = await LoadBallVisualAsync(defaultImagePath);
                
                if (success)
                {
                    _logService?.LogInformation("Default ball image loaded successfully from: {ImagePath}", defaultImagePath);
                }
                else
                {
                    _logService?.LogError("Failed to load default ball image from: {ImagePath}", defaultImagePath);
                }

                _logService?.LogMethodExit(nameof(LoadDefaultBallImageAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading default ball image from configuration");
                _logService?.LogMethodExit(nameof(LoadDefaultBallImageAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Gets the default ball size from configuration
        /// </summary>
        /// <returns>The default ball size from configuration, or 25.0 as fallback</returns>
        private double GetDefaultBallSizeFromConfiguration()
        {
            _logService?.LogMethodEntry(nameof(GetDefaultBallSizeFromConfiguration));

            try
            {
                if (_configurationService?.Configuration != null)
                {
                    var defaultSize = _configurationService.Configuration.DefaultBallSize;
                    _logService?.LogDebug("Using default ball size from configuration: {DefaultSize}", defaultSize);
                    _logService?.LogMethodExit(nameof(GetDefaultBallSizeFromConfiguration), defaultSize);
                    return defaultSize;
                }
                else
                {
                    _logService?.LogDebug("Configuration service not available, using fallback ball size: {FallbackSize}", Constants.DEFAULT_BALL_SIZE);
                    _logService?.LogMethodExit(nameof(GetDefaultBallSizeFromConfiguration), Constants.DEFAULT_BALL_SIZE);
                    return Constants.DEFAULT_BALL_SIZE;
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error getting default ball size from configuration, using fallback: {FallbackSize}", Constants.DEFAULT_BALL_SIZE);
                _logService?.LogMethodExit(nameof(GetDefaultBallSizeFromConfiguration), Constants.DEFAULT_BALL_SIZE);
                return Constants.DEFAULT_BALL_SIZE;
            }
        }

        /// <summary>
        /// Gets the show bounding box setting from configuration
        /// </summary>
        /// <returns>The show bounding box setting from configuration, or false as fallback</returns>
        private bool GetShowBoundingBoxFromConfiguration()
        {
            _logService?.LogMethodEntry(nameof(GetShowBoundingBoxFromConfiguration));

            try
            {
                if (_configurationService != null)
                {
                    var showBoundingBox = _configurationService.GetShowBoundingBox();
                    _logService?.LogDebug("Using show bounding box setting from configuration: {ShowBoundingBox}", showBoundingBox);
                    _logService?.LogMethodExit(nameof(GetShowBoundingBoxFromConfiguration), showBoundingBox);
                    return showBoundingBox;
                }
                else
                {
                    _logService?.LogDebug("Configuration service not available, using fallback show bounding box: false");
                    _logService?.LogMethodExit(nameof(GetShowBoundingBoxFromConfiguration), false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error getting show bounding box setting from configuration, using fallback: false");
                _logService?.LogMethodExit(nameof(GetShowBoundingBoxFromConfiguration), false);
                return false;
            }
        }

        /// <summary>
        /// Toggles the bounding box display and updates the configuration
        /// </summary>
        public void ToggleBoundingBox()
        {
            _logService?.LogMethodEntry(nameof(ToggleBoundingBox));

            try
            {
                bool newValue = !ShowBoundingBox;
                ShowBoundingBox = newValue;
                
                // Update configuration if available
                if (_configurationService != null)
                {
                    _configurationService.SetShowBoundingBox(newValue);
                    _logService?.LogDebug("Bounding box display toggled to: {ShowBoundingBox}", newValue);
                }
                
                _logService?.LogMethodExit(nameof(ToggleBoundingBox));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error toggling bounding box display");
                _logService?.LogMethodExit(nameof(ToggleBoundingBox));
            }
        }

        /// <summary>
        /// Validates the current configuration settings and updates them if necessary
        /// </summary>
        /// <returns>True if configuration is valid or was successfully updated, false otherwise</returns>
        public bool ValidateAndUpdateConfiguration()
        {
            _logService?.LogMethodEntry(nameof(ValidateAndUpdateConfiguration));

            try
            {
                if (_configurationService == null)
                {
                    _logService?.LogWarning("Configuration service not available for validation");
                    _logService?.LogMethodExit(nameof(ValidateAndUpdateConfiguration), false);
                    return false;
                }

                bool configurationUpdated = false;

                // Validate default ball image path
                var currentImagePath = _configurationService.GetDefaultBallImagePath();
                if (!_configurationService.ValidateImagePath(currentImagePath))
                {
                    _logService?.LogWarning("Current default ball image path is invalid: {ImagePath}", currentImagePath);
                    
                    // Try to find a valid fallback
                    var fallbackPaths = new[]
                    {
                        "./Resources/Images/Ball01.png",
                        "./src/BallDragDrop/Resources/Images/Ball01.png",
                        "Resources/Images/Ball01.png"
                    };

                    foreach (var fallbackPath in fallbackPaths)
                    {
                        if (_configurationService.ValidateImagePath(fallbackPath))
                        {
                            _logService?.LogInformation("Updating configuration with valid fallback image path: {FallbackPath}", fallbackPath);
                            _configurationService.SetDefaultBallImagePath(fallbackPath);
                            configurationUpdated = true;
                            break;
                        }
                    }

                    if (!configurationUpdated)
                    {
                        _logService?.LogError("No valid fallback image path found for configuration");
                        _logService?.LogMethodExit(nameof(ValidateAndUpdateConfiguration), false);
                        return false;
                    }
                }

                // Validate default ball size
                if (_configurationService.Configuration != null)
                {
                    var defaultSize = _configurationService.Configuration.DefaultBallSize;
                    if (defaultSize <= 0 || defaultSize > 200) // Reasonable bounds for ball size
                    {
                        _logService?.LogWarning("Default ball size is out of reasonable bounds: {DefaultSize}. Resetting to 50.0", defaultSize);
                        _configurationService.Configuration.DefaultBallSize = 50.0;
                        configurationUpdated = true;
                    }
                }

                if (configurationUpdated)
                {
                    _logService?.LogInformation("Configuration validation completed with updates");
                }
                else
                {
                    _logService?.LogDebug("Configuration validation completed - no updates needed");
                }

                _logService?.LogMethodExit(nameof(ValidateAndUpdateConfiguration), true);
                return true;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error validating and updating configuration");
                _logService?.LogMethodExit(nameof(ValidateAndUpdateConfiguration), false);
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

                _logService?.LogDebug("Switching visual content from {0} to new content. Current state - Dragging: {1}, Position: ({2}, {3}), Animation running: {4}", 
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

                _logService?.LogDebug("Animation started with interval: {0}ms", _animationTimer.Interval.TotalMilliseconds);
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
        /// Optimized animation timer tick handler that respects source frame rates
        /// while ensuring physics updates are not impacted
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnOptimizedAnimationTimerTick(object sender, EventArgs e)
        {
            if (IsAnimated)
            {
                // Check if we should skip this frame update during high physics activity
                if (IsDragging && _imageService.FrameDuration.TotalMilliseconds < 33) // Skip if faster than 30 FPS during drag
                {
                    return;
                }

                // Update frame in ImageService with timing coordination
                var frameUpdateStart = DateTime.Now;
                _imageService.UpdateFrame();
                
                // Get the new frame
                var newFrame = _imageService.CurrentFrame;
                
                // Only update BallImage if the frame actually changed to prevent unnecessary redraws
                if (newFrame != null && !ReferenceEquals(BallImage, newFrame))
                {
                    // Use background priority to not interfere with physics updates
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BallImage = newFrame;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // Dynamically adjust timer interval based on source frame rate and system performance
                var frameUpdateTime = DateTime.Now - frameUpdateStart;
                if (_imageService.FrameDuration > TimeSpan.Zero)
                {
                    var targetInterval = _imageService.FrameDuration;
                    
                    // Ensure minimum interval to prevent overwhelming the system
                    if (targetInterval.TotalMilliseconds < 16) // Don't exceed 60 FPS for animations
                    {
                        targetInterval = TimeSpan.FromMilliseconds(16);
                    }
                    
                    // Adjust for processing time to maintain accurate frame rate
                    var adjustedInterval = targetInterval.Add(frameUpdateTime);
                    
                    if (_animationTimer.Interval != adjustedInterval)
                    {
                        _animationTimer.Interval = adjustedInterval;
                        _logService?.LogTrace("Animation timer interval adjusted to {Interval}ms (source: {SourceInterval}ms, processing: {ProcessingTime}ms)", 
                            adjustedInterval.TotalMilliseconds, targetInterval.TotalMilliseconds, frameUpdateTime.TotalMilliseconds);
                    }
                }

                // Update last animation update timestamp for coordination
                _lastAnimationUpdate = DateTime.Now;
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
            // Use optimized dual timer coordination if enabled
            if (_isDualTimerOptimized)
            {
                CoordinateDualTimerSystem();
                return;
            }

            // Fallback to original coordination method
            if (IsAnimated)
            {
                var now = DateTime.Now;
                var timeSinceLastAnimationUpdate = now - _lastAnimationUpdate;
                
                // Only coordinate if enough time has passed based on source frame rate
                // This prevents animation updates from interfering with 60 FPS physics updates
                if (timeSinceLastAnimationUpdate >= _imageService.FrameDuration)
                {
                    // Use background priority to ensure physics updates take precedence
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (IsAnimated) // Double-check in case animation stopped
                        {
                            _imageService.UpdateFrame();
                            var newFrame = _imageService.CurrentFrame;
                            
                            if (newFrame != null && !ReferenceEquals(BallImage, newFrame))
                            {
                                BallImage = newFrame;
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    
                    _lastAnimationUpdate = now;
                }
            }
        }

        /// <summary>
        /// Optimizes animation timer to respect source frame rates while maintaining physics smoothness
        /// Separates animation frame updates from physics updates for better performance
        /// </summary>
        public void OptimizeAnimationTiming()
        {
            if (IsAnimated && _animationTimer != null)
            {
                // Ensure animation timer uses background priority to not interfere with physics
                if (_animationTimer.Dispatcher != null)
                {
                    // Stop current timer
                    _animationTimer.Stop();
                    
                    // Create new optimized timer with background priority to separate from physics updates
                    var optimizedTimer = new DispatcherTimer(DispatcherPriority.Background, _animationTimer.Dispatcher);
                    
                    // Set interval based on source animation frame rate to respect original timing
                    if (_imageService.FrameDuration > TimeSpan.Zero)
                    {
                        optimizedTimer.Interval = _imageService.FrameDuration;
                    }
                    else
                    {
                        // Default to 10 FPS for unknown frame rates to avoid overwhelming the system
                        optimizedTimer.Interval = TimeSpan.FromMilliseconds(100);
                    }
                    
                    // Transfer event handler
                    optimizedTimer.Tick += OnOptimizedAnimationTimerTick;
                    
                    // Replace the timer
                    _animationTimer.Tick -= OnAnimationTimerTick;
                    _animationTimer = optimizedTimer;
                    
                    // Start the optimized timer
                    _animationTimer.Start();
                    
                    _logService?.LogDebug("Animation timer optimized - Priority: Background, Interval: {0}ms (Source frame rate respected)", 
                        optimizedTimer.Interval.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        /// Ensures animation performance doesn't impact drag responsiveness
        /// Temporarily adjusts animation timing during drag operations
        /// </summary>
        public void EnsureAnimationDoesNotImpactDragResponsiveness()
        {
            if (IsAnimated && IsDragging)
            {
                // During drag operations, reduce animation update frequency to maintain responsiveness
                // Cap animation at 20 FPS during drag to prioritize physics updates
                var maxDragInterval = TimeSpan.FromMilliseconds(50); // 20 FPS max during drag
                
                if (_animationTimer != null && _animationTimer.Interval < maxDragInterval)
                {
                    var originalInterval = _animationTimer.Interval;
                    _animationTimer.Interval = maxDragInterval;
                    
                    // Ensure animation timer uses lowest priority during drag
                    if (_animationTimer is DispatcherTimer dispatcherTimer)
                    {
                        // Stop and recreate with lowest priority if needed
                        var wasRunning = dispatcherTimer.IsEnabled;
                        dispatcherTimer.Stop();
                        
                        var lowPriorityTimer = new DispatcherTimer(DispatcherPriority.SystemIdle, dispatcherTimer.Dispatcher)
                        {
                            Interval = maxDragInterval
                        };
                        lowPriorityTimer.Tick += OnOptimizedAnimationTimerTick;
                        
                        _animationTimer.Tick -= OnOptimizedAnimationTimerTick;
                        _animationTimer.Tick -= OnAnimationTimerTick;
                        _animationTimer = lowPriorityTimer;
                        
                        if (wasRunning)
                        {
                            _animationTimer.Start();
                        }
                    }
                    
                    _logService?.LogTrace("Animation frequency reduced during drag: {OriginalInterval}ms -> {NewInterval}ms (Priority: SystemIdle)", 
                        originalInterval.TotalMilliseconds, _animationTimer.Interval.TotalMilliseconds);
                }
            }
            else if (IsAnimated && !IsDragging)
            {
                // Restore original animation timing and priority when not dragging
                if (_imageService.FrameDuration > TimeSpan.Zero && 
                    _animationTimer != null)
                {
                    var targetInterval = _imageService.FrameDuration;
                    
                    // Ensure minimum interval to prevent overwhelming the system
                    if (targetInterval.TotalMilliseconds < 16)
                    {
                        targetInterval = TimeSpan.FromMilliseconds(16);
                    }
                    
                    if (_animationTimer.Interval != targetInterval)
                    {
                        var wasRunning = _animationTimer.IsEnabled;
                        _animationTimer.Stop();
                        
                        // Restore background priority for normal operation
                        var normalPriorityTimer = new DispatcherTimer(DispatcherPriority.Background, _animationTimer.Dispatcher)
                        {
                            Interval = targetInterval
                        };
                        normalPriorityTimer.Tick += OnOptimizedAnimationTimerTick;
                        
                        _animationTimer.Tick -= OnOptimizedAnimationTimerTick;
                        _animationTimer.Tick -= OnAnimationTimerTick;
                        _animationTimer = normalPriorityTimer;
                        
                        if (wasRunning)
                        {
                            _animationTimer.Start();
                        }
                        
                        _logService?.LogTrace("Animation frequency restored after drag: {Interval}ms (Priority: Background)", 
                            _animationTimer.Interval.TotalMilliseconds);
                    }
                }
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
        /// Gets comprehensive animation timing metrics for performance monitoring
        /// </summary>
        /// <returns>Animation timing metrics including FPS, coordination status, and performance indicators</returns>
        public AnimationTimingMetrics GetAnimationTimingMetrics()
        {
            var metrics = new AnimationTimingMetrics
            {
                IsAnimated = IsAnimated,
                AnimationTimerEnabled = _animationTimer?.IsEnabled ?? false,
                AnimationTimerInterval = _animationTimer?.Interval ?? TimeSpan.Zero,
                SourceFrameDuration = _imageService?.FrameDuration ?? TimeSpan.Zero,
                LastAnimationUpdate = _lastAnimationUpdate,
                IsDragging = IsDragging,
                ContentType = ContentType
            };

            // Calculate effective animation FPS
            if (metrics.AnimationTimerInterval.TotalSeconds > 0)
            {
                metrics.EffectiveAnimationFPS = 1.0 / metrics.AnimationTimerInterval.TotalSeconds;
            }

            // Calculate source animation FPS
            if (metrics.SourceFrameDuration.TotalSeconds > 0)
            {
                metrics.SourceAnimationFPS = 1.0 / metrics.SourceFrameDuration.TotalSeconds;
            }

            return metrics;
        }

        /// <summary>
        /// Optimizes the dual timer system by separating physics and animation updates
        /// Physics runs at 60 FPS while animation respects source frame rates
        /// </summary>
        public void OptimizeDualTimerSystem()
        {
            _logService?.LogMethodEntry(nameof(OptimizeDualTimerSystem));

            if (!_isDualTimerOptimized)
            {
                // Optimize animation timer to use background priority
                // This ensures physics updates (handled by MainWindow) take precedence
                if (_animationTimer != null)
                {
                    var wasRunning = _animationTimer.IsEnabled;
                    var currentInterval = _animationTimer.Interval;
                    
                    // Stop current timer
                    _animationTimer.Stop();
                    _animationTimer.Tick -= OnAnimationTimerTick;
                    _animationTimer.Tick -= OnOptimizedAnimationTimerTick;

                    // Create new optimized timer with background priority
                    _animationTimer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = currentInterval
                    };
                    _animationTimer.Tick += OnOptimizedDualTimerAnimationTick;

                    // Restart if it was running
                    if (wasRunning)
                    {
                        _animationTimer.Start();
                    }
                }

                _isDualTimerOptimized = true;
                _logService?.LogDebug("Dual timer system optimized - Animation timer using background priority");
            }

            _logService?.LogMethodExit(nameof(OptimizeDualTimerSystem));
        }

        /// <summary>
        /// Optimized animation timer tick handler for dual timer system
        /// Respects source frame rates while ensuring physics updates are not impacted
        /// </summary>
        /// <param name="sender">Timer sender</param>
        /// <param name="e">Event arguments</param>
        private void OnOptimizedDualTimerAnimationTick(object sender, EventArgs e)
        {
            if (!IsAnimated) return;

            // During drag operations, reduce animation frequency to maintain responsiveness
            if (IsDragging)
            {
                // Limit animation to 20 FPS during drag to prioritize physics responsiveness
                var maxDragInterval = TimeSpan.FromMilliseconds(50); // 20 FPS
                if (_animationTimer.Interval < maxDragInterval)
                {
                    _animationTimer.Interval = maxDragInterval;
                    _logService?.LogTrace("Animation frequency reduced during drag: {Interval}ms", 
                        _animationTimer.Interval.TotalMilliseconds);
                }
            }
            else
            {
                // Restore source frame rate when not dragging
                var sourceInterval = _imageService?.FrameDuration ?? TimeSpan.FromMilliseconds(100);
                
                // Ensure minimum interval to prevent overwhelming the system (max 60 FPS)
                if (sourceInterval.TotalMilliseconds < 16.67)
                {
                    sourceInterval = TimeSpan.FromMilliseconds(16.67);
                }

                if (_animationTimer.Interval != sourceInterval)
                {
                    _animationTimer.Interval = sourceInterval;
                    _logService?.LogTrace("Animation frequency restored to source rate: {Interval}ms", 
                        _animationTimer.Interval.TotalMilliseconds);
                }
            }

            // Update frame with low priority to not interfere with physics
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsAnimated) // Double-check in case animation stopped
                {
                    _imageService?.UpdateFrame();
                    var newFrame = _imageService?.CurrentFrame;
                    
                    if (newFrame != null && !ReferenceEquals(BallImage, newFrame))
                    {
                        BallImage = newFrame;
                    }
                }
                
                _lastAnimationUpdate = DateTime.Now;
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// Ensures animation performance doesn't impact drag responsiveness in dual timer system
        /// </summary>
        public void EnsureDualTimerDragResponsiveness()
        {
            if (_isDualTimerOptimized && IsAnimated)
            {
                if (IsDragging)
                {
                    // During drag, use system idle priority for animation updates
                    if (_animationTimer != null)
                    {
                        var wasRunning = _animationTimer.IsEnabled;
                        var currentInterval = _animationTimer.Interval;
                        
                        _animationTimer.Stop();
                        _animationTimer.Tick -= OnOptimizedDualTimerAnimationTick;

                        // Create ultra-low priority timer for drag operations
                        _animationTimer = new DispatcherTimer(DispatcherPriority.SystemIdle)
                        {
                            Interval = TimeSpan.FromMilliseconds(50) // 20 FPS max during drag
                        };
                        _animationTimer.Tick += OnOptimizedDualTimerAnimationTick;

                        if (wasRunning)
                        {
                            _animationTimer.Start();
                        }

                        _logService?.LogTrace("Animation timer switched to SystemIdle priority during drag");
                    }
                }
                else
                {
                    // Restore background priority when not dragging
                    if (_animationTimer != null)
                    {
                        var wasRunning = _animationTimer.IsEnabled;
                        var sourceInterval = _imageService?.FrameDuration ?? TimeSpan.FromMilliseconds(100);
                        
                        _animationTimer.Stop();
                        _animationTimer.Tick -= OnOptimizedDualTimerAnimationTick;

                        _animationTimer = new DispatcherTimer(DispatcherPriority.Background)
                        {
                            Interval = sourceInterval
                        };
                        _animationTimer.Tick += OnOptimizedDualTimerAnimationTick;

                        if (wasRunning)
                        {
                            _animationTimer.Start();
                        }

                        _logService?.LogTrace("Animation timer restored to Background priority after drag");
                    }
                }
            }
        }

        /// <summary>
        /// Coordinates animation timing with physics updates in the dual timer system
        /// Ensures smooth operation between 60 FPS physics and variable animation frame rates
        /// </summary>
        public void CoordinateDualTimerSystem()
        {
            if (_isDualTimerOptimized && IsAnimated)
            {
                var now = DateTime.Now;
                var timeSinceLastUpdate = now - _lastAnimationUpdate;
                var sourceFrameDuration = _imageService?.FrameDuration ?? TimeSpan.FromMilliseconds(100);

                // Only update animation frame if enough time has passed based on source frame rate
                // This prevents animation updates from interfering with 60 FPS physics updates
                if (timeSinceLastUpdate >= sourceFrameDuration)
                {
                    // Use background priority to ensure physics updates take precedence
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (IsAnimated) // Double-check in case animation stopped
                        {
                            _imageService?.UpdateFrame();
                            var newFrame = _imageService?.CurrentFrame;
                            
                            if (newFrame != null && !ReferenceEquals(BallImage, newFrame))
                            {
                                BallImage = newFrame;
                            }
                        }
                    }), DispatcherPriority.Background);
                    
                    _lastAnimationUpdate = now;
                }
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
                // Only process mouse down if we can transition to Held state (from Idle or Thrown)
                if (_stateMachine != null && _stateMachine.CanFire(BallTrigger.MouseDown))
                {
                    // Log user interaction
                    _logService?.LogInformation("User started dragging ball at position ({X}, {Y})", position.X, position.Y);
                    
                    // Fire the MouseDown trigger to transition to Held state
                    try
                    {
                        _stateMachine.Fire(BallTrigger.MouseDown);
                        
                        // Also trigger hand state machine to start grabbing
                        if (_handStateMachine != null && _handStateMachine.CanFire(HandTrigger.StartGrabbing))
                        {
                            _handStateMachine.Fire(HandTrigger.StartGrabbing);
                            _logService?.LogDebug("Hand state machine: StartGrabbing triggered");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logService?.LogError(ex, "Failed to transition to Held state on mouse down");
                        return;
                    }

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
                    _logService?.LogDebug("Mouse down ignored - cannot transition to Held state from current state ({CurrentState}) or state machine unavailable", CurrentState);
                }
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

            // Only allow dragging if we're in the Held state
            if (IsDragging && (_stateMachine?.CurrentState == BallState.Held || _stateMachine == null))
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
                _logService?.LogDebug("Mouse move processing took {0}ms", stopwatch.ElapsedMilliseconds);
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
            if (_mouseHistoryCount >= Constants.MOUSE_HISTORY_SIZE)
            {
                for (int i = 0; i < Constants.MOUSE_HISTORY_SIZE - 1; i++)
                {
                    _mousePositionHistory[i] = _mousePositionHistory[i + 1];
                    _mouseTimestampHistory[i] = _mouseTimestampHistory[i + 1];
                }
                
                // Add the new position and timestamp at the end
                _mousePositionHistory[Constants.MOUSE_HISTORY_SIZE - 1] = position;
                _mouseTimestampHistory[Constants.MOUSE_HISTORY_SIZE - 1] = timestamp;
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
                
                // Only process mouse up if we're in Held state and can transition to Thrown
                if (_stateMachine != null && _stateMachine.CanFire(BallTrigger.Release))
                {
                    // Log the end of dragging
                    _logService?.LogInformation("User released ball at position ({X:F1}, {Y:F1})", X, Y);
                    
                    // Check if velocity has already been set by MainWindow (preferred)
                    // If not, calculate it here as fallback
                    if (Math.Abs(_ballModel.VelocityX) < 0.001 && Math.Abs(_ballModel.VelocityY) < 0.001)
                    {
                        // Calculate velocity based on movement for throwing (fallback)
                        double velocityX = 0, velocityY = 0;
                        if (e != null)
                        {
                            var currentPosition = e.GetPosition(null);
                            var currentTime = DateTime.Now;
                            
                            // Store the final position in the history
                            StoreMousePosition(currentPosition, currentTime);
                            
                            // Create a physics engine instance to calculate velocity
                            var physicsEngine = new Models.PhysicsEngine();
                            
                            // Calculate velocity using the mouse movement history for more accuracy
                            (velocityX, velocityY) = physicsEngine.CalculateVelocityFromHistory(
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
                            
                            _lastMousePosition = currentPosition;
                        }

                        // Apply the calculated velocity to the ball model
                        _ballModel.SetVelocity(velocityX, velocityY);
                    }
                    
                    // Debug logging for velocity (show the actual velocity on the ball model)
                    _logService?.LogInformation("BALLVIEWMODEL - Final velocity before state transition: ({VelX:F2}, {VelY:F2})", _ballModel.VelocityX, _ballModel.VelocityY);

                    // Fire the Release trigger to transition to Thrown state
                    try
                    {
                        _stateMachine.Fire(BallTrigger.Release);
                        _logService?.LogInformation("BALLVIEWMODEL - State transition to Thrown successful");
                        
                        // Also trigger hand state machine to stop grabbing
                        if (_handStateMachine != null && _handStateMachine.CanFire(HandTrigger.StopGrabbing))
                        {
                            _handStateMachine.Fire(HandTrigger.StopGrabbing);
                            _logService?.LogDebug("Hand state machine: StopGrabbing triggered");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logService?.LogError(ex, "Failed to transition to Thrown state on mouse up");
                        // Fall back to stopping the ball if state transition fails
                        _ballModel.Stop();
                    }

                    // Stop dragging
                    IsDragging = false;

                    // Release mouse capture
                    Mouse.Capture(null);
                    
                    // Reset the mouse history count for the next drag
                    _mouseHistoryCount = 0;

                    // Lower the threshold for throwing to make it easier to throw the ball
                    double throwThreshold = 100.0; // Reduced from default 200.0
                    
                    // Check if the movement is fast enough to be considered a throw
                    var physicsEngine2 = new Models.PhysicsEngine();
                    if (physicsEngine2.IsThrow(_ballModel.VelocityX, _ballModel.VelocityY, throwThreshold))
                    {
                        // Log the throw action
                        _logService?.LogInformation("Ball thrown with velocity ({VelX:F1}, {VelY:F1})", _ballModel.VelocityX, _ballModel.VelocityY);
                    }
                    else
                    {
                        // Not a throw, but still transition to thrown state briefly before settling
                        _logService?.LogDebug("Ball dropped (velocity too low for throw): ({0:F1}, {1:F1})", _ballModel.VelocityX, _ballModel.VelocityY);
                    }
                }
                else
                {
                    _logService?.LogDebug("Mouse up ignored - ball not in Held state or state machine unavailable");
                    
                    // Fallback behavior - stop dragging and stop the ball
                    IsDragging = false;
                    Mouse.Capture(null);
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
            if (_stateMachine != null)
            {
                // State-aware cursor logic
                switch (_stateMachine.CurrentState)
                {
                    case BallState.Held:
                        // When ball is held, show the "SizeAll" cursor to indicate movement
                        CurrentCursor = Cursors.SizeAll;
                        break;
                    
                    case BallState.Idle:
                        if (_ballModel.ContainsPoint(_lastMousePosition.X, _lastMousePosition.Y))
                        {
                            // When hovering over idle ball, show the "Hand" cursor to indicate it can be grabbed
                            CurrentCursor = Cursors.Hand;
                        }
                        else
                        {
                            // Default cursor when not interacting with idle ball
                            CurrentCursor = Cursors.Arrow;
                        }
                        break;
                    
                    case BallState.Thrown:
                        // When ball is thrown, show default cursor (can't interact)
                        CurrentCursor = Cursors.Arrow;
                        break;
                    
                    default:
                        CurrentCursor = Cursors.Arrow;
                        break;
                }
            }
            else
            {
                // Fallback to original logic if state machine is not available
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
        }

        /// <summary>
        /// Handles mouse enter events for ball-specific hover detection
        /// </summary>
        /// <param name="e">Mouse event arguments</param>
        private void OnMouseEnter(MouseEventArgs e)
        {
            if (e == null) return;

            try
            {
                // Allow hover state when ball is idle or thrown (mid-air)
                if (_stateMachine?.CurrentState == BallState.Idle || _stateMachine?.CurrentState == BallState.Thrown)
                {
                    // Fire the MouseOverBall trigger to transition to hover state
                    if (_handStateMachine != null && _handStateMachine.CanFire(HandTrigger.MouseOverBall))
                    {
                        _handStateMachine.Fire(HandTrigger.MouseOverBall);
                        _logService?.LogDebug("Hand state machine: MouseOverBall triggered for ball state {BallState}", _stateMachine?.CurrentState);
                    }
                }
                else
                {
                    _logService?.LogDebug("Mouse enter ignored - ball is in {BallState} state", _stateMachine?.CurrentState);
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error handling mouse enter event");
            }
        }

        /// <summary>
        /// Handles mouse leave events for ball-specific hover detection
        /// </summary>
        /// <param name="e">Mouse event arguments</param>
        private void OnMouseLeave(MouseEventArgs e)
        {
            if (e == null) return;

            try
            {
                // Fire the MouseLeaveBall trigger to exit hover state
                if (_handStateMachine != null && _handStateMachine.CanFire(HandTrigger.MouseLeaveBall))
                {
                    _handStateMachine.Fire(HandTrigger.MouseLeaveBall);
                    _logService?.LogDebug("Hand state machine: MouseLeaveBall triggered");
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error handling mouse leave event");
            }
        }

        /// <summary>
        /// Handles hand state changes from the hand state machine
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data containing hand state change information</param>
        private void OnHandStateChanged(object sender, HandStateChangedEventArgs e)
        {
            try
            {
                // Notify UI that hand state has changed
                OnPropertyChanged(nameof(CurrentHandState));
                _logService?.LogDebug("Hand state changed from {PreviousState} to {NewState}", e.PreviousState, e.NewState);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error handling hand state change");
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
                _logService?.LogDebug("Ball position constrained from ({0:F1}, {1:F1}) to ({2:F1}, {3:F1})", 
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
                // If we can't get the log service, fall back to Debug.WriteLine
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
            // Ensure PropertyChanged events are raised on the UI thread
            if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(propertyName));
                return;
            }

            // Debug: Log PropertyChanged events for position properties
            if ((propertyName == nameof(X) || propertyName == nameof(Y)) && _logService != null && DateTime.Now.Millisecond % 150 < 20)
            {
                _logService.LogInformation("PROPERTYCHANGED: {PropertyName} event fired, HasSubscribers={HasSubscribers}", 
                    propertyName, PropertyChanged != null);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Update dependent properties (avoid infinite recursion by only notifying for primary properties)
            if (propertyName == nameof(X) || propertyName == nameof(Y) || propertyName == nameof(Radius))
            {
                if (propertyName == nameof(X) || propertyName == nameof(Radius))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Left)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
                    
                    // Debug: Log dependent property notifications
                    if (propertyName == nameof(X) && _logService != null && DateTime.Now.Millisecond % 150 < 20)
                    {
                        _logService.LogInformation("DEPENDENT PROPERTIES: Left and Width events fired for X change");
                    }
                }

                if (propertyName == nameof(Y) || propertyName == nameof(Radius))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Top)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
                    
                    // Debug: Log dependent property notifications
                    if (propertyName == nameof(Y) && _logService != null && DateTime.Now.Millisecond % 150 < 20)
                    {
                        _logService.LogInformation("DEPENDENT PROPERTIES: Top and Height events fired for Y change");
                    }
                }
            }
        }

        /// <summary>
        /// Forces PropertyChanged events for position properties even if values haven't changed
        /// This is needed when the physics engine updates the model directly
        /// </summary>
        public void ForcePositionUpdate()
        {
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
        }

        /// <summary>
        /// Extracts the asset name from a file path
        /// </summary>
        /// <param name="filePath">The file path to extract the asset name from</param>
        /// <returns>The extracted asset name, or "No Asset" if extraction fails</returns>
        private string ExtractAssetNameFromPath(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return "No Asset";
                }

                // Get the file name without extension
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                
                if (string.IsNullOrEmpty(fileName))
                {
                    return "No Asset";
                }

                // Truncate long names with ellipsis to fit the available space
                if (fileName.Length > Constants.MAX_ASSET_NAME_LENGTH)
                {
                    return fileName.Substring(0, Constants.MAX_ASSET_NAME_LENGTH - 3) + "...";
                }

                return fileName;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error extracting asset name from path: {FilePath}", filePath);
                return "No Asset";
            }
        }

        /// <summary>
        /// Handles state change notifications from the ball state machine.
        /// Updates UI properties and coordinates state-dependent behavior.
        /// </summary>
        /// <param name="previousState">The state the ball was in before the transition</param>
        /// <param name="newState">The state the ball transitioned to</param>
        /// <param name="trigger">The trigger that caused the state transition</param>
        public void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger)
        {
            _logService?.LogDebug("Ball state changed from {PreviousState} to {NewState} via {Trigger}", 
                previousState, newState, trigger);

            // Notify UI of state property changes
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(IsInIdleState));
            OnPropertyChanged(nameof(IsInHeldState));
            OnPropertyChanged(nameof(IsInThrownState));
            OnPropertyChanged(nameof(IsDragging)); // Update IsDragging since it depends on state

            // Notify UI of visual state property changes
            OnPropertyChanged(nameof(StateOpacity));
            OnPropertyChanged(nameof(StateScale));
            OnPropertyChanged(nameof(StateGlowRadius));
            OnPropertyChanged(nameof(StateGlowColor));
            OnPropertyChanged(nameof(StateBorderThickness));
            OnPropertyChanged(nameof(StateBorderColor));

            // Handle state-specific behavior
            switch (newState)
            {
                case BallState.Idle:
                    // Ball has come to rest - ensure dragging is disabled
                    if (_isDragging)
                    {
                        _isDragging = false;
                        Mouse.Capture(null);
                        _logService?.LogDebug("Dragging disabled - ball transitioned to Idle state");
                    }
                    break;

                case BallState.Held:
                    // Ball is being held - dragging behavior is now controlled by state
                    _logService?.LogDebug("Ball is now in Held state - drag behavior enabled");
                    break;

                case BallState.Thrown:
                    // Ball has been released and is in motion
                    // Ensure dragging is disabled
                    if (_isDragging)
                    {
                        _isDragging = false;
                        Mouse.Capture(null);
                        _logService?.LogDebug("Dragging disabled - ball transitioned to Thrown state");
                    }
                    break;
            }

            // Update cursor based on new state
            UpdateCursor();
        }

        /// <summary>
        /// Disposes of resources and unsubscribes from the state machine
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Unsubscribe from state machine notifications
                _stateMachine?.Unsubscribe(this);

                // Stop animation timer
                _animationTimer?.Stop();

                // Note: EventThrottler doesn't implement IDisposable

                _logService?.LogDebug("BallViewModel disposed");
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error disposing BallViewModel");
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
