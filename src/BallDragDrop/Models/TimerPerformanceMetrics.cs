using System;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Performance metrics for the dual timer system
    /// Used to monitor and optimize physics and animation timing coordination
    /// </summary>
    public class TimerPerformanceMetrics
    {
        /// <summary>
        /// Gets or sets whether the physics timer is currently enabled
        /// </summary>
        public bool PhysicsTimerEnabled { get; set; }

        /// <summary>
        /// Gets or sets the physics update interval (target: 60 FPS = ~16.67ms)
        /// </summary>
        public TimeSpan PhysicsUpdateInterval { get; set; }

        /// <summary>
        /// Gets or sets whether physics simulation is currently running
        /// </summary>
        public bool IsPhysicsRunning { get; set; }

        /// <summary>
        /// Gets or sets the total number of physics updates performed
        /// </summary>
        public int PhysicsUpdateCount { get; set; }

        /// <summary>
        /// Gets or sets whether the optimized dual timer system is being used
        /// </summary>
        public bool UseOptimizedTimers { get; set; }

        /// <summary>
        /// Gets or sets the average frame time in milliseconds
        /// </summary>
        public double AverageFrameTime { get; set; }

        /// <summary>
        /// Gets or sets the average physics update time in milliseconds
        /// </summary>
        public double AveragePhysicsTime { get; set; }

        /// <summary>
        /// Gets the effective physics FPS based on the update interval
        /// </summary>
        public double PhysicsFPS => PhysicsUpdateInterval.TotalSeconds > 0 ? 1.0 / PhysicsUpdateInterval.TotalSeconds : 0;

        /// <summary>
        /// Gets the effective frame FPS based on average frame time
        /// </summary>
        public double FrameFPS => AverageFrameTime > 0 ? 1000.0 / AverageFrameTime : 0;

        /// <summary>
        /// Gets whether the physics timing is meeting the 60 FPS target
        /// </summary>
        public bool IsPhysicsTimingOptimal => Math.Abs(PhysicsFPS - 60.0) < 5.0; // Within 5 FPS of target

        /// <summary>
        /// Gets the physics timing efficiency as a percentage
        /// </summary>
        public double PhysicsTimingEfficiency => PhysicsFPS > 0 ? Math.Min(100.0, (60.0 / PhysicsFPS) * 100.0) : 0;

        /// <summary>
        /// Returns a string representation of the performance metrics
        /// </summary>
        /// <returns>Formatted performance metrics string</returns>
        public override string ToString()
        {
            return $"Timer Performance: Physics={PhysicsFPS:F1} FPS, Frame={FrameFPS:F1} FPS, " +
                   $"Optimized={UseOptimizedTimers}, Running={IsPhysicsRunning}, " +
                   $"Updates={PhysicsUpdateCount}, Efficiency={PhysicsTimingEfficiency:F1}%";
        }
    }
}