namespace BallDragDrop
{
    /// <summary>
    /// Centralized constants for the BallDragDrop application
    /// </summary>
    public static class Constants
    {
        #region UI Layout Constants

        /// <summary>
        /// Height offset to account for menu bar, status bar, borders, and other UI chrome
        /// This value ensures the canvas doesn't overlap with the status bar
        /// Adjust this value if the layout changes or on different DPI settings
        /// </summary>
        public const int UI_CHROME_HEIGHT_OFFSET = 92;

        #endregion UI Layout Constants

        #region Ball Physics Constants

        /// <summary>
        /// Size of the mouse position history buffer for velocity calculation
        /// </summary>
        public const int MOUSE_HISTORY_SIZE = 10;

        /// <summary>
        /// Default ball size when configuration is not available
        /// </summary>
        public const double DEFAULT_BALL_SIZE = 25.0;

        /// <summary>
        /// Maximum length for asset names displayed in the status bar
        /// </summary>
        public const int MAX_ASSET_NAME_LENGTH = 30;

        #endregion Ball Physics Constants

        #region Physics Engine Constants

        /// <summary>
        /// Default friction coefficient for ball movement
        /// </summary>
        public const double DEFAULT_FRICTION_COEFFICIENT = 0.995;

        /// <summary>
        /// Default gravity acceleration
        /// </summary>
        public const double DEFAULT_GRAVITY = 300.0;

        /// <summary>
        /// Default bounce factor for collisions
        /// </summary>
        public const double DEFAULT_BOUNCE_FACTOR = 0.8;

        /// <summary>
        /// Velocity threshold below which the ball is considered stopped
        /// </summary>
        public const double VELOCITY_THRESHOLD = 0.1;

        #endregion Physics Engine Constants

        #region Configuration Constants

        /// <summary>
        /// Default path to the ball image resource
        /// </summary>
        public const string DEFAULT_BALL_IMAGE_PATH = "../../Resources/Ball/Ball01.png";

        /// <summary>
        /// Configuration file name
        /// </summary>
        public const string CONFIG_FILE_NAME = "appsettings.json";

        /// <summary>
        /// Default path to the default cursor PNG file
        /// </summary>
        public const string DEFAULT_CURSOR_PATH = "Resources/Cursors/default.png";
        
        /// <summary>
        /// Default path to the hover cursor PNG file
        /// </summary>
        public const string DEFAULT_HOVER_CURSOR_PATH = "Resources/Cursors/hover.png";
        
        /// <summary>
        /// Default path to the grabbing cursor PNG file
        /// </summary>
        public const string DEFAULT_GRABBING_CURSOR_PATH = "Resources/Cursors/grabbing.png";
        
        /// <summary>
        /// Default path to the releasing cursor PNG file
        /// </summary>
        public const string DEFAULT_RELEASING_CURSOR_PATH = "Resources/Cursors/releasing.png";
        
        /// <summary>
        /// Default cursor update debounce time in milliseconds
        /// </summary>
        public const int DEFAULT_CURSOR_DEBOUNCE_TIME_MS = 16;
        
        /// <summary>
        /// Default releasing cursor duration in milliseconds
        /// </summary>
        public const int DEFAULT_CURSOR_RELEASING_DURATION_MS = 200;

        #endregion Configuration Constants

        #region Performance Constants

        /// <summary>
        /// Maximum acceptable frame time in milliseconds (~60 FPS)
        /// </summary>
        public const double MAX_ACCEPTABLE_FRAME_TIME_MS = 16.67;

        /// <summary>
        /// Maximum acceptable physics update time in milliseconds
        /// </summary>
        public const double MAX_ACCEPTABLE_PHYSICS_UPDATE_TIME_MS = 5.0;

        /// <summary>
        /// Maximum acceptable logging overhead in milliseconds per operation
        /// </summary>
        public const double MAX_ACCEPTABLE_LOGGING_OVERHEAD_MS = 0.1;

        /// <summary>
        /// Maximum acceptable memory overhead in megabytes
        /// </summary>
        public const double MAX_ACCEPTABLE_MEMORY_OVERHEAD_MB = 10.0;

        #endregion Performance Constants
    }
}