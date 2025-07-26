using System.Threading.Tasks;
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
    }

    /// <summary>
    /// Application configuration interface using Config.Net
    /// </summary>
    public interface IAppConfiguration
    {
        /// <summary>
        /// Gets or sets the default ball image path
        /// </summary>
        [Option(DefaultValue = "./Resources/Images/Ball01.png")]
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
    }
}