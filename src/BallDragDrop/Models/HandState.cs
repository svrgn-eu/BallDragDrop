namespace BallDragDrop.Models
{
    /// <summary>
    /// Hand states for cursor management
    /// </summary>
    public enum HandState
    {
        /// <summary>
        /// Normal cursor, ready for interaction
        /// </summary>
        Default,

        /// <summary>
        /// Mouse over interactable element
        /// </summary>
        Hover,

        /// <summary>
        /// Actively dragging/holding
        /// </summary>
        Grabbing,

        /// <summary>
        /// Brief state during release
        /// </summary>
        Releasing
    }
}