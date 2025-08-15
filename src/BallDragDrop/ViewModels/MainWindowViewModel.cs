using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BallDragDrop.Contracts;

namespace BallDragDrop.ViewModels
{
    /// <summary>
    /// Main window view model that aggregates all child view models
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields

        /// <summary>
        /// Logging service for tracking operations
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Ball view model instance
        /// </summary>
        private readonly BallViewModel _ballViewModel;

        /// <summary>
        /// Status bar view model instance
        /// </summary>
        private readonly StatusBarViewModel _statusBarViewModel;

        /// <summary>
        /// Flag to track if the object has been disposed
        /// </summary>
        private bool _disposed = false;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the ball view model
        /// </summary>
        public BallViewModel BallViewModel => _ballViewModel;

        /// <summary>
        /// Gets the status bar view model
        /// </summary>
        public StatusBarViewModel StatusBarViewModel => _statusBarViewModel;

        // Expose BallViewModel properties for backward compatibility with existing XAML bindings
        public double X => _ballViewModel.X;
        public double Y => _ballViewModel.Y;
        public double Radius => _ballViewModel.Radius;
        public System.Windows.Media.ImageSource BallImage => _ballViewModel.BallImage;
        public bool IsAnimated => _ballViewModel.IsAnimated;
        public bool IsDragging => _ballViewModel.IsDragging;
        public System.Windows.Input.Cursor CurrentCursor => _ballViewModel.CurrentCursor;
        public double Left => _ballViewModel.Left;
        public double Top => _ballViewModel.Top;
        public double Width => _ballViewModel.Width;
        public double Height => _ballViewModel.Height;
        public string AssetName => _ballViewModel.AssetName;
        public System.Windows.Input.ICommand MouseDownCommand => _ballViewModel.MouseDownCommand;
        public System.Windows.Input.ICommand MouseMoveCommand => _ballViewModel.MouseMoveCommand;
        public System.Windows.Input.ICommand MouseUpCommand => _ballViewModel.MouseUpCommand;
        public bool ShowBoundingBox 
        { 
            get => _ballViewModel.ShowBoundingBox;
            set => _ballViewModel.ShowBoundingBox = value;
        }

        // Expose BallViewModel visual state properties for state-dependent visual feedback
        public double StateOpacity => _ballViewModel.StateOpacity;
        public double StateScale => _ballViewModel.StateScale;
        public double StateGlowRadius => _ballViewModel.StateGlowRadius;
        public System.Windows.Media.Color StateGlowColor => _ballViewModel.StateGlowColor;
        public double StateBorderThickness => _ballViewModel.StateBorderThickness;
        public System.Windows.Media.Color StateBorderColor => _ballViewModel.StateBorderColor;

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        /// <param name="ballViewModel">Ball view model instance</param>
        /// <param name="statusBarViewModel">Status bar view model instance</param>
        /// <param name="logService">Logging service for tracking operations</param>
        public MainWindowViewModel(BallViewModel ballViewModel, StatusBarViewModel statusBarViewModel, ILogService logService)
        {
            _ballViewModel = ballViewModel ?? throw new ArgumentNullException(nameof(ballViewModel));
            _statusBarViewModel = statusBarViewModel ?? throw new ArgumentNullException(nameof(statusBarViewModel));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            // Subscribe to BallViewModel property changes to forward them
            _ballViewModel.PropertyChanged += OnBallViewModelPropertyChanged;

            // Connect StatusBarViewModel to BallViewModel for asset information
            _statusBarViewModel.ConnectToBallViewModel(_ballViewModel);

            _logService.LogDebug("MainWindowViewModel created with dependency injection");
        }

        #endregion Construction

        #region Events

        /// <summary>
        /// Event that is raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Methods

        /// <summary>
        /// Handles property changes from the BallViewModel and forwards them
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void OnBallViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Forward property change notifications for exposed properties
            OnPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Disposes the view model and cleans up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the view model and cleans up resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Unsubscribe from events
                if (_ballViewModel != null)
                {
                    _ballViewModel.PropertyChanged -= OnBallViewModelPropertyChanged;
                }

                _disposed = true;
                _logService?.LogDebug("MainWindowViewModel disposed");
            }
        }

        #endregion Methods
    }
}