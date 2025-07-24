using System;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Comprehensive metrics for dual timer system coordination
    /// Combines physics and animation timing metrics for performance analysis
    /// </summary>
    public class DualTimerCoordinationMetrics
    {
        /// <summary>
        /// Gets or sets the physics timer performance metrics
        /// </summary>
        public TimerPerformanceMetrics PhysicsMetrics { get; set; } = new TimerPerformanceMetrics();

        /// <summary>
        /// Gets or sets the animation timing metrics
        /// </summary>
        public AnimationTimingMetrics AnimationMetrics { get; set; } = new AnimationTimingMetrics();

        /// <summary>
        /// Gets or sets whether the timer coordination is optimal
        /// </summary>
        public bool IsCoordinationOptimal { get; set; }

        /// <summary>
        /// Gets or sets the coordination efficiency as a percentage
        /// </summary>
        public double CoordinationEfficiency { get; set; }

        /// <summary>
        /// Gets whether both timers are performing within acceptable bounds
        /// </summary>
        public bool IsPerformanceAcceptable => 
            PhysicsMetrics.IsPhysicsTimingOptimal && AnimationMetrics.IsPerformanceAcceptable;

        /// <summary>
        /// Gets whether the system is properly separating physics and animation updates
        /// </summary>
        public bool IsProperlySeparated => 
            PhysicsMetrics.UseOptimizedTimers && 
            (!AnimationMetrics.IsAnimated || AnimationMetrics.AnimationTimerEnabled);

        /// <summary>
        /// Gets whether drag responsiveness is being maintained
        /// </summary>
        public bool IsDragResponsive => 
            !AnimationMetrics.IsDragging || AnimationMetrics.IsOptimizedForDrag;

        /// <summary>
        /// Gets whether source animation frame rates are being respected
        /// </summary>
        public bool IsRespectingSourceFrameRates => 
            !AnimationMetrics.IsAnimated || AnimationMetrics.IsRespectingSourceFrameRate;

        /// <summary>
        /// Gets the overall system health score (0-100)
        /// </summary>
        public double SystemHealthScore
        {
            get
            {
                var score = 0.0;
                
                // Physics performance (40% weight)
                if (PhysicsMetrics.IsPhysicsTimingOptimal)
                    score += 40.0;
                else if (PhysicsMetrics.PhysicsFPS >= 45.0)
                    score += 30.0;
                else if (PhysicsMetrics.PhysicsFPS >= 30.0)
                    score += 20.0;
                
                // Animation performance (30% weight)
                if (AnimationMetrics.IsPerformanceAcceptable)
                    score += 30.0;
                else if (AnimationMetrics.EffectiveAnimationFPS > 0 && AnimationMetrics.EffectiveAnimationFPS <= 60.0)
                    score += 20.0;
                
                // Coordination efficiency (20% weight)
                score += (CoordinationEfficiency / 100.0) * 20.0;
                
                // Drag responsiveness (10% weight)
                if (IsDragResponsive)
                    score += 10.0;
                
                return Math.Min(100.0, score);
            }
        }

        /// <summary>
        /// Returns a string representation of the dual timer coordination metrics
        /// </summary>
        /// <returns>Formatted coordination metrics string</returns>
        public override string ToString()
        {
            return $"Dual Timer Coordination: Health={SystemHealthScore:F1}%, " +
                   $"Efficiency={CoordinationEfficiency:F1}%, " +
                   $"Optimal={IsCoordinationOptimal}, " +
                   $"Separated={IsProperlySeparated}, " +
                   $"Drag Responsive={IsDragResponsive}, " +
                   $"Respects Source Rates={IsRespectingSourceFrameRates}";
        }
    }
}