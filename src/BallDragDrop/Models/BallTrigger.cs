namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents the triggers that can cause state transitions in the ball state machine.
    /// These triggers correspond to user actions and system events that change the ball's state.
    /// </summary>
    public enum BallTrigger
    {
        /// <summary>
        /// Triggered when the user presses the mouse button down on the ball.
        /// This trigger causes the transition from Idle state to Held state,
        /// indicating that the user has started interacting with the ball.
        /// </summary>
        MouseDown,

        /// <summary>
        /// Triggered when the user releases the mouse button after holding the ball.
        /// This trigger causes the transition from Held state to Thrown state,
        /// indicating that the ball has been released and should be subject to physics.
        /// </summary>
        Release,

        /// <summary>
        /// Triggered automatically when the ball's velocity drops below the configured threshold.
        /// This trigger causes the transition from Thrown state back to Idle state,
        /// indicating that the ball has come to rest and is no longer in motion.
        /// </summary>
        VelocityBelowThreshold,

        /// <summary>
        /// Triggered when the user activates the reset function (e.g., through menu or keyboard shortcut).
        /// This trigger causes the transition from any state back to Idle state,
        /// resetting the ball to its initial position and clearing all velocity.
        /// </summary>
        Reset
    }
}