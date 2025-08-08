using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.Services;

namespace BallDragDrop.ViewModels
{
    /// <summary>
    /// View model for the status bar, implementing INotifyPropertyChanged for UI binding
    /// and IBallStateObserver for ball state change notifications
    /// </summary>
    public class StatusBarViewModel : INotifyPropertyChanged, IBallStateObserver, IDisposable
    {
        #region Fields

        /// <summary>
        /// Logging service for tracking status bar operations
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Performance monitor service for FPS data
        /// </summary>
        private readonly PerformanceMonitor _performanceMonitor;

        /// <summary>
        /// Ball state machine for tracking ball state changes
        /// </summary>
        private readonly IBallStateMachine _stateMachine;

        /// <summary>
        /// Ball view model for asset information
        /// </summary>
        private BallViewModel _ballViewModel;

        /// <summary>
        /// FPS calculator for 10-second rolling average
        /// </summary>
        private readonly FpsCalculator _fpsCalculator;

        /// <summary>
        /// Current frames per second value
        /// </summary>
        private double _currentFps;

        /// <summary>
        /// Average frames per second over the last 10 seconds
        /// </summary>
        private double _averageFps;

        /// <summary>
        /// Name of the currently loaded asset
        /// </summary>
        private string _assetName;

        /// <summary>
        /// Status text field
        /// </summary>
        private string _statusText;

        /// <summary>
        /// Status field for displaying ball state information
        /// </summary>
        private string _status;

        /// <summary>
        /// Flag to track if the object has been disposed
        /// </summary>
        private bool _disposed = false;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the current frames per second
        /// </summary>
        public double CurrentFps
        {
            get => _currentFps;
            set
            {
                // Validate and sanitize the FPS value
                var sanitizedValue = SanitizeFpsValue(value);
                if (Math.Abs(_currentFps - sanitizedValue) > 0.01)
                {
                    _currentFps = sanitizedValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentFpsDisplay));
                }
            }
        }

        /// <summary>
        /// Gets or sets the average frames per second over the last 10 seconds
        /// </summary>
        public double AverageFps
        {
            get => _averageFps;
            set
            {
                // Validate and sanitize the FPS value
                var sanitizedValue = SanitizeFpsValue(value);
                if (Math.Abs(_averageFps - sanitizedValue) > 0.01)
                {
                    _averageFps = sanitizedValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AverageFpsDisplay));
                }
            }
        }

        /// <summary>
        /// Gets the formatted current FPS display string
        /// </summary>
        public string CurrentFpsDisplay
        {
            get
            {
                if (_currentFps <= 0 || double.IsNaN(_currentFps) || double.IsInfinity(_currentFps))
                {
                    return "FPS: --";
                }
                try
                {
                    return $"FPS: {_currentFps:F1}";
                }
                catch (FormatException)
                {
                    return "FPS: --";
                }
            }
        }

        /// <summary>
        /// Gets the formatted average FPS display string
        /// </summary>
        public string AverageFpsDisplay
        {
            get
            {
                if (_averageFps <= 0 || double.IsNaN(_averageFps) || double.IsInfinity(_averageFps))
                {
                    return "Avg: --";
                }
                try
                {
                    return $"Avg: {_averageFps:F1}";
                }
                catch (FormatException)
                {
                    return "Avg: --";
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the currently loaded asset
        /// </summary>
        public string AssetName
        {
            get => _assetName;
            set
            {
                if (_assetName != value)
                {
                    _assetName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the status field that displays ball state information
        /// </summary>
        public string Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current hand state for display
        /// </summary>
        public string HandState
        {
            get => _ballViewModel?.CurrentHandState.ToString() ?? "Default";
        }

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the StatusBarViewModel class
        /// </summary>
        /// <param name="logService">Logging service for tracking status bar operations</param>
        /// <param name="stateMachine">Ball state machine for tracking ball state changes</param>
        /// <param name="performanceMonitor">Performance monitor service for FPS data</param>
        public StatusBarViewModel(ILogService logService, IBallStateMachine stateMachine, PerformanceMonitor performanceMonitor = null)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _performanceMonitor = performanceMonitor;
            _fpsCalculator = new FpsCalculator();
            
            // Initialize with default values
            _currentFps = 0.0;
            _averageFps = 0.0;
            _assetName = "No Asset";
            _statusText = "Status";
            _status = FormatBallStateForDisplay(_stateMachine.CurrentState);

            // Subscribe to PerformanceMonitor events if available
            if (_performanceMonitor != null)
            {
                _performanceMonitor.FpsUpdated += OnFpsUpdated;
                _logService.LogDebug("StatusBarViewModel subscribed to PerformanceMonitor FpsUpdated events");
            }
            else
            {
                _logService.LogWarning("PerformanceMonitor not provided, FPS updates will not be available");
            }

            // Subscribe to state machine notifications
            _stateMachine.Subscribe(this);
            _logService.LogDebug("StatusBarViewModel subscribed to ball state machine notifications");
            
            _logService.LogDebug("StatusBarViewModel created with dependency injection");
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
        /// Handles FPS updates from the PerformanceMonitor
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data containing FPS information</param>
        private void OnFpsUpdated(object sender, FpsUpdatedEventArgs e)
        {
            // Ensure UI thread marshaling for property change notifications
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() => OnFpsUpdated(sender, e)));
                return;
            }

            try
            {
                // Update current FPS
                CurrentFps = e.CurrentFps;

                // Add FPS reading to calculator for 10-second average
                _fpsCalculator.AddFpsReading(e.CurrentFps);

                // Update average FPS
                AverageFps = _fpsCalculator.AverageFps;

                _logService?.LogDebug("FPS updated - Current: {CurrentFps:F1}, Average: {AverageFps:F1}", 
                    CurrentFps, AverageFps);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error processing FPS update");
            }
        }

        /// <summary>
        /// Handles property changes from the BallViewModel
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data containing property change information</param>
        private void OnBallViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Ensure UI thread marshaling for property change notifications
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() => OnBallViewModelPropertyChanged(sender, e)));
                return;
            }

            try
            {
                // Handle AssetName property changes
                if (e.PropertyName == nameof(BallViewModel.AssetName) && _ballViewModel != null)
                {
                    AssetName = ProcessAssetName(_ballViewModel.AssetName);
                    _logService?.LogDebug("Asset name updated: {AssetName}", AssetName);
                }
                
                // Handle hand state changes
                if (e.PropertyName == nameof(BallViewModel.CurrentHandState) && _ballViewModel != null)
                {
                    OnPropertyChanged(nameof(HandState));
                    _logService?.LogDebug("Hand state updated: {HandState}", HandState);
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error processing BallViewModel property change: {PropertyName}", e.PropertyName);
            }
        }

        /// <summary>
        /// Connects the StatusBarViewModel to a BallViewModel for asset information
        /// </summary>
        /// <param name="ballViewModel">The BallViewModel to connect to</param>
        public void ConnectToBallViewModel(BallViewModel ballViewModel)
        {
            if (ballViewModel == null)
            {
                _logService?.LogWarning("Cannot connect to null BallViewModel");
                return;
            }

            // Disconnect from previous BallViewModel if any
            if (_ballViewModel != null)
            {
                _ballViewModel.PropertyChanged -= OnBallViewModelPropertyChanged;
            }

            // Store reference and subscribe to events
            _ballViewModel = ballViewModel;
            _ballViewModel.PropertyChanged += OnBallViewModelPropertyChanged;

            // Initialize asset name from current ball view model state
            AssetName = ProcessAssetName(_ballViewModel.AssetName);

            // Initialize hand state display
            OnPropertyChanged(nameof(HandState));

            _logService?.LogDebug("StatusBarViewModel connected to BallViewModel");
        }

        /// <summary>
        /// Processes the asset name with truncation and default handling
        /// </summary>
        /// <param name="rawAssetName">The raw asset name from BallViewModel</param>
        /// <returns>The processed asset name suitable for display</returns>
        private string ProcessAssetName(string rawAssetName)
        {
            try
            {
                // Handle null or empty asset names with appropriate defaults
                if (string.IsNullOrEmpty(rawAssetName))
                {
                    return "No Asset";
                }

                // The BallViewModel already handles truncation, but we can add additional processing here if needed
                return rawAssetName;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error processing asset name: {RawAssetName}", rawAssetName);
                return "No Asset";
            }
        }

        /// <summary>
        /// Sanitizes FPS values to ensure they are valid for display
        /// </summary>
        /// <param name="fps">The FPS value to sanitize</param>
        /// <returns>A sanitized FPS value</returns>
        private double SanitizeFpsValue(double fps)
        {
            // Filter out invalid values
            if (double.IsNaN(fps) || double.IsInfinity(fps) || fps < 0 || fps > 1000)
            {
                return 0.0;
            }
            
            return fps;
        }

        /// <summary>
        /// Formats the ball state for display in the status bar
        /// </summary>
        /// <param name="state">The ball state to format</param>
        /// <returns>A formatted string representation of the ball state</returns>
        private string FormatBallStateForDisplay(BallState state)
        {
            return state switch
            {
                BallState.Idle => "Ball: Idle",
                BallState.Held => "Ball: Held",
                BallState.Thrown => "Ball: Thrown",
                _ => "Ball: Unknown"
            };
        }

        /// <summary>
        /// Handles ball state changes from the state machine
        /// </summary>
        /// <param name="previousState">The previous ball state</param>
        /// <param name="newState">The new ball state</param>
        /// <param name="trigger">The trigger that caused the state change</param>
        public void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger)
        {
            // Ensure UI thread marshaling for property change notifications
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() => OnStateChanged(previousState, newState, trigger)));
                return;
            }

            try
            {
                // Update the status display with the new ball state
                Status = FormatBallStateForDisplay(newState);
                
                _logService?.LogDebug("Ball state changed from {PreviousState} to {NewState} via {Trigger}, status updated to: {Status}", 
                    previousState, newState, trigger, Status);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error processing ball state change from {PreviousState} to {NewState}", 
                    previousState, newState);
            }
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
                // Unsubscribe from PerformanceMonitor events
                if (_performanceMonitor != null)
                {
                    _performanceMonitor.FpsUpdated -= OnFpsUpdated;
                }

                // Unsubscribe from BallViewModel events
                if (_ballViewModel != null)
                {
                    _ballViewModel.PropertyChanged -= OnBallViewModelPropertyChanged;
                }

                // Unsubscribe from state machine notifications
                if (_stateMachine != null)
                {
                    _stateMachine.Unsubscribe(this);
                }

                // Clear FPS calculator
                _fpsCalculator?.Clear();

                _disposed = true;
                _logService?.LogDebug("StatusBarViewModel disposed");
            }
        }

        #endregion Methods
    }
}