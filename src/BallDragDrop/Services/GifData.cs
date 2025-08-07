using System;
using System.Collections.Generic;
using BallDragDrop.Models;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Data structure for holding GIF animation data
    /// </summary>
    public class GifData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of animation frames
        /// </summary>
        public List<AnimationFrame> Frames { get; set; } = new List<AnimationFrame>();

        /// <summary>
        /// Gets or sets the loop count (0 = infinite, -1 = no looping)
        /// </summary>
        public int LoopCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total duration of the animation
        /// </summary>
        public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero;

        #endregion Properties
    }
}