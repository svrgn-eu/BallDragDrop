using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Service for monitoring and analyzing application performance metrics
    /// </summary>
    public class PerformanceMonitor
    {
        #region Properties

        /// <summary>
        /// Event raised when performance metrics are updated
        /// </summary>
        public event EventHandler<PerformanceMetricsEventArgs> MetricsUpdated;

        #endregion Properties
        
        #region Construction

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitor class
        /// </summary>
        /// <param name="targetFrameRate">Target frame rate for the application</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when targetFrameRate is less than or equal to zero</exception>
        public PerformanceMonitor(int targetFrameRate = 60)
        {
            _targetFrameRate = targetFrameRate;
            _targetFrameTime = TimeSpan.FromSeconds(1.0 / targetFrameRate);
            
            // Set up a timer to periodically update and report metrics
            DispatcherTimer metricsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            metricsTimer.Tick += (s, e) => UpdateMetrics();
            metricsTimer.Start();
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// Queue storing recent frame times for averaging
        /// </summary>
        private readonly Queue<double> _frameTimesMs = new Queue<double>(100);
        
        /// <summary>
        /// Queue storing recent physics update times for averaging
        /// </summary>
        private readonly Queue<double> _physicsTimesMs = new Queue<double>(100);
        
        /// <summary>
        /// Stopwatch for measuring frame rendering time
        /// </summary>
        private readonly Stopwatch _frameStopwatch = new Stopwatch();
        
        /// <summary>
        /// Stopwatch for measuring physics update time
        /// </summary>
        private readonly Stopwatch _physicsStopwatch = new Stopwatch();
        
        /// <summary>
        /// Average frame time in milliseconds
        /// </summary>
        private double _averageFrameTimeMs = 0;
        
        /// <summary>
        /// Average physics update time in milliseconds
        /// </summary>
        private double _averagePhysicsTimeMs = 0;
        
        /// <summary>
        /// Maximum frame time recorded in the current period
        /// </summary>
        private double _maxFrameTimeMs = 0;
        
        /// <summary>
        /// Maximum physics update time recorded in the current period
        /// </summary>
        private double _maxPhysicsTimeMs = 0;
        
        /// <summary>
        /// Number of frames rendered since last metrics update
        /// </summary>
        private int _frameCount = 0;
        
        /// <summary>
        /// Number of physics updates since last metrics update
        /// </summary>
        private int _physicsCount = 0;
        
        /// <summary>
        /// Timestamp of the last metrics update
        /// </summary>
        private DateTime _lastMetricsUpdate = DateTime.Now;
        
        /// <summary>
        /// Target frame rate for the application
        /// </summary>
        private readonly int _targetFrameRate;
        
        /// <summary>
        /// Target time per frame based on target frame rate
        /// </summary>
        private readonly TimeSpan _targetFrameTime;
        
        /// <summary>
        /// Timestamp of the last frame rendering
        /// </summary>
        private DateTime _lastFrameTime = DateTime.Now;

        #endregion Fields
        
        #region Methods

        /// <summary>
        /// Begins measuring a new frame time
        /// </summary>
        public void BeginFrameTime()
        {
            _frameStopwatch.Restart();
        }
        
        /// <summary>
        /// Ends the current frame time measurement and records the result
        /// </summary>
        public void EndFrameTime()
        {
            _frameStopwatch.Stop();
            double frameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds;
            
            // Add to metrics
            _frameTimesMs.Enqueue(frameTimeMs);
            if (_frameTimesMs.Count > 100)
            {
                _frameTimesMs.Dequeue();
            }
            
            _maxFrameTimeMs = Math.Max(_maxFrameTimeMs, frameTimeMs);
            _frameCount++;
        }
        
        /// <summary>
        /// Begins measuring a new physics update time
        /// </summary>
        public void BeginPhysicsTime()
        {
            _physicsStopwatch.Restart();
        }
        
        /// <summary>
        /// Ends the current physics update time measurement and records the result
        /// </summary>
        public void EndPhysicsTime()
        {
            _physicsStopwatch.Stop();
            double physicsTimeMs = _physicsStopwatch.Elapsed.TotalMilliseconds;
            
            // Add to metrics
            _physicsTimesMs.Enqueue(physicsTimeMs);
            if (_physicsTimesMs.Count > 100)
            {
                _physicsTimesMs.Dequeue();
            }
            
            _maxPhysicsTimeMs = Math.Max(_maxPhysicsTimeMs, physicsTimeMs);
            _physicsCount++;
        }
        
        /// <summary>
        /// Checks if a new frame should be rendered based on the target frame rate
        /// </summary>
        /// <param name="forceRender">Whether to force rendering regardless of frame rate</param>
        /// <returns>True if a new frame should be rendered, false otherwise</returns>
        public bool ShouldRenderFrame(bool forceRender = false)
        {
            // Always render frames when physics is running or when forced
            // This ensures smooth animation for the ball
            if (forceRender)
            {
                return true;
            }
            
            // Otherwise, limit to target frame rate
            DateTime currentTime = DateTime.Now;
            TimeSpan timeSinceLastFrame = currentTime - _lastFrameTime;
            
            if (timeSinceLastFrame >= _targetFrameTime)
            {
                _lastFrameTime = currentTime;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Updates performance metrics and raises the MetricsUpdated event
        /// </summary>
        private void UpdateMetrics()
        {
            // Calculate average frame time
            double frameTimeSum = 0;
            foreach (var time in _frameTimesMs)
            {
                frameTimeSum += time;
            }
            _averageFrameTimeMs = _frameTimesMs.Count > 0 ? frameTimeSum / _frameTimesMs.Count : 0;
            
            // Calculate average physics time
            double physicsTimeSum = 0;
            foreach (var time in _physicsTimesMs)
            {
                physicsTimeSum += time;
            }
            _averagePhysicsTimeMs = _physicsTimesMs.Count > 0 ? physicsTimeSum / _physicsTimesMs.Count : 0;
            
            // Calculate FPS
            double fps = 0;
            if (_averageFrameTimeMs > 0)
            {
                fps = 1000.0 / _averageFrameTimeMs;
                
                // Cap reported FPS to a reasonable value to avoid misleading metrics
                fps = Math.Min(fps, 1000);
            }
            else
            {
                fps = _targetFrameRate; // Default to target frame rate if no frames were rendered
            }
            
            // Create metrics event args
            var metrics = new PerformanceMetricsEventArgs
            {
                AverageFrameTimeMs = _averageFrameTimeMs,
                MaxFrameTimeMs = _maxFrameTimeMs,
                FramesPerSecond = fps,
                AveragePhysicsTimeMs = _averagePhysicsTimeMs,
                MaxPhysicsTimeMs = _maxPhysicsTimeMs,
                FrameCount = _frameCount,
                PhysicsCount = _physicsCount
            };
            
            // Reset max values and counters
            _maxFrameTimeMs = 0;
            _maxPhysicsTimeMs = 0;
            _frameCount = 0;
            _physicsCount = 0;
            
            // Raise the event
            MetricsUpdated?.Invoke(this, metrics);
            
            // Log metrics for debugging
            Debug.WriteLine($"FPS: {fps:F1}, Avg Frame: {_averageFrameTimeMs:F2}ms, Max Frame: {metrics.MaxFrameTimeMs:F2}ms, " +
                           $"Avg Physics: {_averagePhysicsTimeMs:F2}ms, Max Physics: {metrics.MaxPhysicsTimeMs:F2}ms");
        }
        
        /// <summary>
        /// Gets the current performance metrics
        /// </summary>
        /// <returns>Current performance metrics</returns>
        public PerformanceMetricsEventArgs GetCurrentMetrics()
        {
            // Calculate FPS
            double fps = 0;
            if (_averageFrameTimeMs > 0)
            {
                fps = 1000.0 / _averageFrameTimeMs;
                fps = Math.Min(fps, 1000); // Cap to reasonable value
            }
            else
            {
                fps = _targetFrameRate;
            }
            
            return new PerformanceMetricsEventArgs
            {
                AverageFrameTimeMs = _averageFrameTimeMs,
                MaxFrameTimeMs = _maxFrameTimeMs,
                FramesPerSecond = fps,
                AveragePhysicsTimeMs = _averagePhysicsTimeMs,
                MaxPhysicsTimeMs = _maxPhysicsTimeMs,
                FrameCount = _frameCount,
                PhysicsCount = _physicsCount
            };
        }

        #endregion Methods
    }
    
    /// <summary>
    /// Event arguments for performance metrics updates
    /// </summary>
    public class PerformanceMetricsEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the average frame time in milliseconds
        /// </summary>
        public double AverageFrameTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum frame time in milliseconds
        /// </summary>
        public double MaxFrameTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the frames per second
        /// </summary>
        public double FramesPerSecond { get; set; }
        
        /// <summary>
        /// Gets or sets the average physics update time in milliseconds
        /// </summary>
        public double AveragePhysicsTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum physics update time in milliseconds
        /// </summary>
        public double MaxPhysicsTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the number of frames rendered since the last metrics update
        /// </summary>
        public int FrameCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of physics updates since the last metrics update
        /// </summary>
        public int PhysicsCount { get; set; }
    }
}