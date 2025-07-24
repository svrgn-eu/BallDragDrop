using System;
using BallDragDrop.Contracts;
using BallDragDrop.Services;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Metrics for animation timing performance in the dual timer system
    /// Used to monitor animation frame rate coordination with physics updates
    /// </summary>
    public class AnimationTimingMetrics
    {
        /// <summary>
        /// Gets or sets whether the ball visual is currently animated
        /// </summary>
        public bool IsAnimated { get; set; }

        /// <summary>
        /// Gets or sets whether the animation timer is currently enabled
        /// </summary>
        public bool AnimationTimerEnabled { get; set; }

        /// <summary>
        /// Gets or sets the current animation timer interval
        /// </summary>
        public TimeSpan AnimationTimerInterval { get; set; }

        /// <summary>
        /// Gets or sets the source frame duration from the original animation
        /// </summary>
        public TimeSpan SourceFrameDuration { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last animation frame update
        /// </summary>
        public DateTime LastAnimationUpdate { get; set; }

        /// <summary>
        /// Gets or sets whether the ball is currently being dragged
        /// </summary>
        public bool IsDragging { get; set; }

        /// <summary>
        /// Gets or sets the type of visual content currently loaded
        /// </summary>
        public VisualContentType ContentType { get; set; }

        /// <summary>
        /// Gets the effective animation FPS based on current timer interval
        /// </summary>
        public double EffectiveAnimationFPS { get; set; }

        /// <summary>
        /// Gets the source animation FPS based on original frame duration
        /// </summary>
        public double SourceAnimationFPS { get; set; }

        /// <summary>
        /// Gets whether the animation timing is respecting the source frame rate
        /// </summary>
        public bool IsRespectingSourceFrameRate => 
            SourceAnimationFPS > 0 && Math.Abs(EffectiveAnimationFPS - SourceAnimationFPS) < 2.0;

        /// <summary>
        /// Gets whether the animation timing is optimized for drag operations
        /// </summary>
        public bool IsOptimizedForDrag => 
            !IsDragging || (IsDragging && EffectiveAnimationFPS <= 20.0);

        /// <summary>
        /// Gets the time since the last animation update
        /// </summary>
        public TimeSpan TimeSinceLastUpdate => DateTime.Now - LastAnimationUpdate;

        /// <summary>
        /// Gets whether the animation timing is within acceptable performance bounds
        /// </summary>
        public bool IsPerformanceAcceptable => 
            !IsAnimated || (EffectiveAnimationFPS > 0 && EffectiveAnimationFPS <= 60.0);

        /// <summary>
        /// Returns a string representation of the animation timing metrics
        /// </summary>
        /// <returns>Formatted animation timing metrics string</returns>
        public override string ToString()
        {
            return $"Animation Timing: Effective={EffectiveAnimationFPS:F1} FPS, " +
                   $"Source={SourceAnimationFPS:F1} FPS, " +
                   $"Respecting Source={IsRespectingSourceFrameRate}, " +
                   $"Drag Optimized={IsOptimizedForDrag}, " +
                   $"Performance OK={IsPerformanceAcceptable}";
        }
    }
}