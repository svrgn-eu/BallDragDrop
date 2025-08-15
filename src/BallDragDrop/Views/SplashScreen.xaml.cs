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
        
        #region Construction

        /// <summary>
        /// Initializes a new instance of the SplashScreen class
        /// </summary>
        public SplashScreen()
        {
            this.InitializeComponent();
            
            // Initialize flags
            this._isInitializationComplete = false;
            this._isMinimumTimeElapsed = false;
            
            // Set up timer for minimum display time (1.5 seconds)
            this._minimumDisplayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            this._minimumDisplayTimer.Tick += this.MinimumDisplayTimer_Tick;
        }

        #endregion Construction
        
        #region Event Handlers

        /// <summary>
        /// Event handler for window loaded event
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start the minimum display timer
            this._minimumDisplayTimer.Start();
            
            // Start the initialization process
            Task.Run(() => this.InitializeApplication());
        }
        
        /// <summary>
        /// Event handler for minimum display timer tick
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data</param>
        private void MinimumDisplayTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            this._minimumDisplayTimer.Stop();
            
            // Mark minimum time as elapsed
            this._isMinimumTimeElapsed = true;
            
            // Check if we can close the splash screen
            this.CheckIfReadyToClose();
        }

        #endregion Event Handlers

        #region Methods

        #region InitializeApplication
        /// <summary>
        /// Initializes the application
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation</returns>
        private async Task InitializeApplication()
        {
            try
            {
                // Update status
                await this.UpdateStatus("Initializing...");
                
                // Simulate initialization tasks
                await Task.Delay(500);
                await this.UpdateStatus("Loading resources...");
                
                await Task.Delay(500);
                await this.UpdateStatus("Preparing application...");
                
                await Task.Delay(500);
                
                // Mark initialization as complete
                this._isInitializationComplete = true;
                
                // Check if we can close the splash screen
                await this.Dispatcher.InvokeAsync(this.CheckIfReadyToClose);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error during initialization: {ex.Message}");
                await this.UpdateStatus("Error during initialization");
                // Update status to show error
                await this.Dispatcher.InvokeAsync(() => 
                {
                    this.LoadingProgress.IsIndeterminate = false;
                    this.LoadingProgress.Value = 0;
                });
            }
        }
        #endregion InitializeApplication

        #region CheckIfReadyToClose
        /// <summary>
        /// Checks if the splash screen is ready to close
        /// </summary>
        private void CheckIfReadyToClose()
        {
            // If both initialization is complete and minimum time has elapsed, close the splash screen
            if (this._isInitializationComplete && this._isMinimumTimeElapsed)
            {
                // Raise the initialization complete event
                this.InitializationComplete?.Invoke(this, EventArgs.Empty);
                
                // Close the splash screen
                this.Close();
            }
        }
        #endregion CheckIfReadyToClose

        #region UpdateStatus
        /// <summary>
        /// Updates the status text on the splash screen
        /// </summary>
        /// <param name="status">The new status text</param>
        /// <exception cref="ArgumentNullException">Thrown when status is null</exception>
        public async Task UpdateStatus(string status)
        {
            await this.Dispatcher.InvokeAsync(() => this.StatusText.Text = status);
        }
        #endregion UpdateStatus

        #endregion Methods
    }
}
