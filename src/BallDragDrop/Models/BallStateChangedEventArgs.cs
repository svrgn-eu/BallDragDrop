using System;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Event arguments for ball state change notifications.
    /// Provides information about the state transition including the previous state,
    /// new state, trigger that caused the change, and timestamp of the transition.
    /// </summary>
    public class BallStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the state the ball was in before the transition.
        /// </summary>
        /// <value>The previous ball state.</value>
        public BallState PreviousState { get; }

        /// <summary>
        /// Gets the state the ball transitioned to.
        /// </summary>
        /// <value>The new ball state.</value>
        public BallState NewState { get; }

        /// <summary>
        /// Gets the trigger that caused the state transition.
        /// </summary>
        /// <value>The trigger that initiated the state change.</value>
        public BallTrigger Trigger { get; }

        /// <summary>
        /// Gets the timestamp when the state transition occurred.
        /// </summary>
        /// <value>The date and time of the state transition.</value>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BallStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The state the ball was in before the transition.</param>
        /// <param name="newState">The state the ball transitioned to.</param>
        /// <param name="trigger">The trigger that caused the state transition.</param>
        public BallStateChangedEventArgs(BallState previousState, BallState newState, BallTrigger trigger)
        {
            PreviousState = previousState;
            NewState = newState;
            Trigger = trigger;
            Timestamp = DateTime.Now;
        }
    }
}