using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BallDragDrop.Views
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        #region Properties

        /// <summary>
        /// Event that is raised when initialization is complete
        /// </summary>
        public event EventHandler InitializationComplete;

        #endregion Properties
        
        #region Construction

        /// <summary>
        /// Initializes a new instance of the SplashScreen class
        /// </summary>
        public SplashScreen()
        {
            InitializeComponent();
            
            // Initialize flags
            _isInitializationComplete = false;
            _isMinimumTimeElapsed = false;
            
            // Set up timer for minimum display time (1.5 seconds)
            _minimumDisplayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _minimumDisplayTimer.Tick += MinimumDisplayTimer_Tick;
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// Timer for minimum display time
        /// </summary>
        private readonly DispatcherTimer _minimumDisplayTimer;
        
        /// <summary>
        /// Flag to track if initialization is complete
        /// </summary>
        private bool _isInitializationComplete;
        
        /// <summary>
        /// Flag to track if minimum display time has elapsed
        /// </summary>
        private bool _isMinimumTimeElapsed;

        #endregion Fields
        
        #region Event Handlers

        /// <summary>
        /// Event handler for window loaded event
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start the minimum display timer
            _minimumDisplayTimer.Start();
            
            // Start the initialization process
            Task.Run(() => InitializeApplication());
        }
        
        #region Methods

        /// <summary>
        /// Initializes the application
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation</returns>
        private async Task InitializeApplication()
        {
            try
            {
                // Update status
                await Dispatcher.InvokeAsync(() => StatusText.Text = "Initializing...");
                
                // Simulate initialization tasks
                await Task.Delay(500);
                await Dispatcher.InvokeAsync(() => StatusText.Text = "Loading resources...");
                
                await Task.Delay(500);
                await Dispatcher.InvokeAsync(() => StatusText.Text = "Preparing application...");
                
                await Task.Delay(500);
                
                // Mark initialization as complete
                _isInitializationComplete = true;
                
                // Check if we can close the splash screen
                await Dispatcher.InvokeAsync(CheckIfReadyToClose);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error during initialization: {ex.Message}");
                
                // Update status to show error
                await Dispatcher.InvokeAsync(() => 
                {
                    StatusText.Text = "Error during initialization";
                    LoadingProgress.IsIndeterminate = false;
                    LoadingProgress.Value = 0;
                });
            }
        }
        
        /// <summary>
        /// Event handler for minimum display timer tick
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void MinimumDisplayTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            _minimumDisplayTimer.Stop();
            
            // Mark minimum time as elapsed
            _isMinimumTimeElapsed = true;
            
            // Check if we can close the splash screen
            CheckIfReadyToClose();
        }

        #endregion Event Handlers
        
        /// <summary>
        /// Checks if the splash screen is ready to close
        /// </summary>
        private void CheckIfReadyToClose()
        {
            // If both initialization is complete and minimum time has elapsed, close the splash screen
            if (_isInitializationComplete && _isMinimumTimeElapsed)
            {
                // Raise the initialization complete event
                InitializationComplete?.Invoke(this, EventArgs.Empty);
                
                // Close the splash screen
                Close();
            }
        }
        
        /// <summary>
        /// Updates the status text on the splash screen
        /// </summary>
        /// <param name="status">The new status text</param>
        /// <exception cref="ArgumentNullException">Thrown when status is null</exception>
        public void UpdateStatus(string status)
        {
            Dispatcher.InvokeAsync(() => StatusText.Text = status);
        }

        #endregion Methods
    }
}