using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        public BallViewModel(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Initialize with default values - will be set via Initialize method
            _ballModel = new BallModel(0, 0, 25);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[MouseHistorySize];
            _mouseTimestampHistory = new DateTime[MouseHistorySize];
            _mouseHistoryCount = 0;
            
            // Initialize event throttler for mouse move events
            // Throttle to 60 updates per second (approximately 16ms)
            _mouseMoveThrottler = new EventThrottler(ProcessMouseMove, 16);
            
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
        public BallViewModel(double x, double y, double radius)
        {
            // For testing, use a null log service or get from app
            _logService = GetLogServiceFromApp();
            
            // Initialize with provided values
            _ballModel = new BallModel(x, y, radius);
            _isDragging = false;
            _currentCursor = Cursors.Arrow;
            _ballImage = null!; // Initialize to null! to satisfy non-nullable field requirement
            
            // Initialize mouse history arrays for velocity calculation
            _mousePositionHistory = new Point[MouseHistorySize];
            _mouseTimestampHistory = new DateTime[MouseHistorySize];
            _mouseHistoryCount = 0;
            
            // Initialize event throttler for mouse move events
            // Throttle to 60 updates per second (approximately 16ms)
            _mouseMoveThrottler = new EventThrottler(ProcessMouseMove, 16);
            
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
        /// Image source for the ball
        /// </summary>
        private ImageSource _ballImage;

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

                // Capture the mouse
                Mouse.Capture((IInputElement)e.Source);
                
                // Reset mouse history when starting a new drag
                _mouseHistoryCount = 0;
                
                _logService?.LogDebug("Ball drag initiated - mouse captured, movement stopped");
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