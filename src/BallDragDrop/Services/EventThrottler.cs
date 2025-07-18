using System;
using System.Windows.Threading;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Utility class for throttling events to limit their frequency
    /// </summary>
    public class EventThrottler
    {
        private readonly DispatcherTimer _timer;
        private readonly Action _action;
        private readonly TimeSpan _interval;
        private bool _isQueued;
        private DateTime _lastExecutionTime;
        
        /// <summary>
        /// Initializes a new instance of the EventThrottler class
        /// </summary>
        /// <param name="action">The action to execute when throttled</param>
        /// <param name="intervalMs">The minimum interval between executions in milliseconds</param>
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
    }
}