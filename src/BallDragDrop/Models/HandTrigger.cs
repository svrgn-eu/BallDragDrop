namespace BallDragDrop.Models
{
    /// <summary>
    /// Triggers for hand state transitions
    /// </summary>
    public enum HandTrigger
    {
        /// <summary>
        /// Mouse enters ball area
        /// </summary>
        MouseOverBall,

        /// <summary>
        /// Mouse leaves ball area
        /// </summary>
        MouseLeaveBall,

        /// <summary>
        /// Begin drag operation
        /// </summary>
        StartGrabbing,

        /// <summary>
        /// End drag operation
        /// </summary>
        StopGrabbing,

        /// <summary>
        /// Release animation complete
        /// </summary>
        ReleaseComplete,

        /// <summary>
        /// Reset to default state
        /// </summary>
        Reset
    }
}