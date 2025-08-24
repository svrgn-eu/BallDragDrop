namespace BallDragDrop.Models
{
    /// <summary>
    /// Configuration model for cursor settings
    /// </summary>
    public class CursorConfiguration
    {
        #region Properties

        /// <summary>
        /// Path to default cursor PNG file
        /// </summary>
        public string DefaultCursorPath { get; set; } = "";

        /// <summary>
        /// Path to hover cursor PNG file
        /// </summary>
        public string HoverCursorPath { get; set; } = "";

        /// <summary>
        /// Path to grabbing cursor PNG file
        /// </summary>
        public string GrabbingCursorPath { get; set; } = "";

        /// <summary>
        /// Path to releasing cursor PNG file
        /// </summary>
        public string ReleasingCursorPath { get; set; } = "";

        /// <summary>
        /// Whether to enable custom cursors
        /// </summary>
        public bool EnableCustomCursors { get; set; } = true;

        /// <summary>
        /// Cursor update debounce time in milliseconds
        /// </summary>
        public int DebounceTimeMs { get; set; } = 16;

        /// <summary>
        /// Duration to show releasing cursor in milliseconds
        /// </summary>
        public int ReleasingDurationMs { get; set; } = 200;

        #endregion Properties
    }
}