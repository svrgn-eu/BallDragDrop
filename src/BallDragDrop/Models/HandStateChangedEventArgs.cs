using System;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Event arguments for hand state changes
    /// </summary>
    public class HandStateChangedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Previous hand state
        /// </summary>
        public HandState PreviousState { get; }

        /// <summary>
        /// New hand state
        /// </summary>
        public HandState NewState { get; }

        /// <summary>
        /// Trigger that caused the state change
        /// </summary>
        public HandTrigger Trigger { get; }

        /// <summary>
        /// Timestamp of the state change
        /// </summary>
        public DateTime Timestamp { get; }

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the HandStateChangedEventArgs class
        /// </summary>
        /// <param name="previousState">Previous hand state</param>
        /// <param name="newState">New hand state</param>
        /// <param name="trigger">Trigger that caused the state change</param>
        public HandStateChangedEventArgs(HandState previousState, HandState newState, HandTrigger trigger)
        {
            PreviousState = previousState;
            NewState = newState;
            Trigger = trigger;
            Timestamp = DateTime.Now;
        }

        #endregion Construction
    }
}