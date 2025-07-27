using System;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents a single FPS reading with timestamp
    /// </summary>
    public struct FpsReading
    {
        /// <summary>
        /// Gets or sets the FPS value
        /// </summary>
        public double Fps { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this reading was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of the FpsReading struct
        /// </summary>
        /// <param name="fps">The FPS value</param>
        /// <param name="timestamp">The timestamp when this reading was taken</param>
        public FpsReading(double fps, DateTime timestamp)
        {
            Fps = fps;
            Timestamp = timestamp;
        }
    }
}