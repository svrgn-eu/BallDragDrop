using System;
using System.Windows.Threading;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Utility class for throttling events to limit their frequency
    /// </summary>
    public class EventThrottler
    {
        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the EventThrottler class
        /// </summary>
        /// <param name="action">The action to execute when throttled</param>
        /// <param name="intervalMs">The minimum interval between executions in milliseconds</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null</exception>
        public EventThrottler(Action action, int intervalMs)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _interval = TimeSpan.FromMilliseconds(intervalMs);
            _isQueued = false;
            _lastExecutionTime = DateTime.MinValue;
            
            // Create a timer for delayed execution
            _timer = new DispatcherTimer
            {
                Interval = _interval
            };
            _timer.Tick += Timer_Tick;
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// Timer for delayed execution
        /// </summary>
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Action to execute when throttled
        /// </summary>
        private readonly Action _action;

        /// <summary>
        /// Minimum interval between executions
        /// </summary>
        private readonly TimeSpan _interval;

        /// <summary>
        /// Flag indicating if an execution is queued
        /// </summary>
        private bool _isQueued;

        /// <summary>
        /// Timestamp of the last execution
        /// </summary>
        private DateTime _lastExecutionTime;

        #endregion Fields

        #region Methods
        
        /// <summary>
        /// Executes the action, throttling if called too frequently
        /// </summary>
        public void Execute()
        {
            // If we're already queued, just return
            if (_isQueued)
            {
                return;
            }
            
            // Check if enough time has passed since the last execution
            TimeSpan timeSinceLastExecution = DateTime.Now - _lastExecutionTime;
            if (timeSinceLastExecution >= _interval)
            {
                // Execute immediately
                ExecuteNow();
            }
            else
            {
                // Queue for later execution
                _isQueued = true;
                _timer.Start();
            }
        }
        
        /// <summary>
        /// Executes the action immediately, bypassing throttling
        /// </summary>
        public void ExecuteNow()
        {
            // Stop the timer if it's running
            _timer.Stop();
            
            // Execute the action
            _action();
            
            // Update the last execution time
            _lastExecutionTime = DateTime.Now;
            
            // Reset the queued flag
            _isQueued = false;
        }
        
        /// <summary>
        /// Event handler for the timer tick
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            _timer.Stop();
            
            // Execute the action
            _action();
            
            // Update the last execution time
            _lastExecutionTime = DateTime.Now;
            
            // Reset the queued flag
            _isQueued = false;
        }

        #endregion Methods
    }
}