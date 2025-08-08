using System.Threading.Tasks;
using BallDragDrop.Models;
using Config.Net;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Interface for application configuration management using Config.Net
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the application configuration
        /// </summary>
        IAppConfiguration Configuration { get; }

        /// <summary>
        /// Initializes the configuration service
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the default ball image path from configuration
        /// </summary>
        /// <returns>The default ball image path</returns>
        string GetDefaultBallImagePath();

        /// <summary>
        /// Sets the default ball image path in configuration
        /// </summary>
        /// <param name="path">The path to set as default</param>
        void SetDefaultBallImagePath(string path);

        /// <summary>
        /// Validates if the specified image path exists and is accessible
        /// </summary>
        /// <param name="path">The image path to validate</param>
        /// <returns>True if the path is valid, false otherwise</returns>
        bool ValidateImagePath(string path);

        /// <summary>
        /// Gets whether to show the ball's bounding box for debugging
        /// </summary>
        /// <returns>True if bounding box should be shown, false otherwise</returns>
        bool GetShowBoundingBox();

        /// <summary>
        /// Sets whether to show the ball's bounding box for debugging
        /// </summary>
        /// <param name="show">True to show bounding box, false to hide</param>
        void SetShowBoundingBox(bool show);

        /// <summary>
        /// Gets the cursor configuration from settings
        /// </summary>
        /// <returns>The cursor configuration</returns>
        CursorConfiguration GetCursorConfiguration();

        /// <summary>
        /// Validates the cursor configuration and returns validation results
        /// </summary>
        /// <param name="configuration">The cursor configuration to validate</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        bool ValidateCursorConfiguration(CursorConfiguration configuration);

        /// <summary>
        /// Gets the default cursor configuration with fallback values
        /// </summary>
        /// <returns>Default cursor configuration</returns>
        CursorConfiguration GetDefaultCursorConfiguration();
    }

    /// <summary>
    /// Application configuration interface using Config.Net
    /// </summary>
    public interface IAppConfiguration
    {
        /// <summary>
        /// Gets or sets the default ball image path
        /// </summary>
        [Option(DefaultValue = "../../Resources/Ball/Ball01.png")]
        string DefaultBallImagePath { get; set; }

        /// <summary>
        /// Gets or sets whether animations are enabled
        /// </summary>
        [Option(DefaultValue = true)]
        bool EnableAnimations { get; set; }

        /// <summary>
        /// Gets or sets the default ball size
        /// </summary>
        [Option(DefaultValue = 50.0)]
        double DefaultBallSize { get; set; }

        /// <summary>
        /// Gets or sets whether to show the ball's bounding box for debugging
        /// </summary>
        [Option(DefaultValue = false)]
        bool ShowBoundingBox { get; set; }

        /// <summary>
        /// Gets or sets whether custom cursors are enabled
        /// </summary>
        [Option(DefaultValue = true)]
        bool CursorConfiguration_EnableCustomCursors { get; set; }

        /// <summary>
        /// Gets or sets the default cursor PNG path
        /// </summary>
        [Option(DefaultValue = "Resources/Cursors/default.png")]
        string CursorConfiguration_DefaultCursorPath { get; set; }

        /// <summary>
        /// Gets or sets the hover cursor PNG path
        /// </summary>
        [Option(DefaultValue = "Resources/Cursors/hover.png")]
        string CursorConfiguration_HoverCursorPath { get; set; }

        /// <summary>
        /// Gets or sets the grabbing cursor PNG path
        /// </summary>
        [Option(DefaultValue = "Resources/Cursors/grabbing.png")]
        string CursorConfiguration_GrabbingCursorPath { get; set; }

        /// <summary>
        /// Gets or sets the releasing cursor PNG path
        /// </summary>
        [Option(DefaultValue = "Resources/Cursors/releasing.png")]
        string CursorConfiguration_ReleasingCursorPath { get; set; }

        /// <summary>
        /// Gets or sets the cursor update debounce time in milliseconds
        /// </summary>
        [Option(DefaultValue = 16)]
        int CursorConfiguration_DebounceTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the releasing cursor duration in milliseconds
        /// </summary>
        [Option(DefaultValue = 200)]
        int CursorConfiguration_ReleasingDurationMs { get; set; }
    }
}