namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents the various states that a ball can be in during its lifecycle.
    /// The state machine manages transitions between these states to ensure predictable ball behavior.
    /// </summary>
    public enum BallState
    {
        /// <summary>
        /// The ball is at rest and not being interacted with.
        /// In this state, the ball is stationary and waiting for user input.
        /// Physics calculations are minimal, and the ball displays its default visual appearance.
        /// </summary>
        Idle,

        /// <summary>
        /// The ball is being held by the user (clicked and dragged).
        /// In this state, the ball follows the mouse cursor and physics calculations are paused.
        /// Visual feedback indicates the ball is being actively manipulated.
        /// </summary>
        Held,

        /// <summary>
        /// The ball has been released and is in motion under physics control.
        /// In this state, physics calculations are active, controlling the ball's movement.
        /// The ball will remain in this state until its velocity drops below the threshold.
        /// </summary>
        Thrown
    }
}