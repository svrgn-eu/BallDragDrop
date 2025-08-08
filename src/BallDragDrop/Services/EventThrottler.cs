using System;
using System.Threading;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Throttles event execution to prevent rapid successive calls
    /// </summary>
    public class EventThrottler
    {
        #region Fields

        /// <summary>
        /// The action to execute when throttle allows
        /// </summary>
        private readonly Action _action;

        /// <summary>
        /// The throttle interval in milliseconds
        /// </summary>
        private readonly int _throttleIntervalMs;

        /// <summary>
        /// Last execution timestamp
        /// </summary>
        private DateTime _lastExecution = DateTime.MinValue;

        /// <summary>
        /// Lock object for thread safety
        /// </summary>
        private readonly object _lock = new object();

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the EventThrottler class
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="throttleIntervalMs">Throttle interval in milliseconds</param>
        public EventThrottler(Action action, int throttleIntervalMs)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _throttleIntervalMs = throttleIntervalMs;
        }

        #endregion Construction

        #region Execute

        /// <summary>
        /// Executes the action if throttle interval has passed
        /// </summary>
        public void Execute()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                var timeSinceLastExecution = now - _lastExecution;
                
                if (timeSinceLastExecution.TotalMilliseconds >= _throttleIntervalMs)
                {
                    _action();
                    _lastExecution = now;
                }
            }
        }

        #endregion Execute

        #region ExecuteNow

        /// <summary>
        /// Executes the action immediately, bypassing throttle interval
        /// </summary>
        public void ExecuteNow()
        {
            lock (_lock)
            {
                _action();
                _lastExecution = DateTime.Now;
            }
        }

        #endregion ExecuteNow
    }
}