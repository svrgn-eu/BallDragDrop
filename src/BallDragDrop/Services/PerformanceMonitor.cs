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

        /// <summary>
        /// Event raised when FPS values are updated for real-time notifications
        /// </summary>
        public event EventHandler<FpsUpdatedEventArgs> FpsUpdated;

        /// <summary>
        /// Gets the average frame time in milliseconds
        /// </summary>
        public double AverageFrameTime => _averageFrameTimeMs;

        /// <summary>
        /// Gets the average physics update time in milliseconds
        /// </summary>
        public double AveragePhysicsTime => _averagePhysicsTimeMs;

        /// <summary>
        /// Gets the current frames per second
        /// </summary>
        public double CurrentFps
        {
            get
            {
                lock (_lock)
                {
                    return _currentFps;
                }
            }
        }

        #endregion Properties
        
        #region Construction

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitor class
        /// </summary>
        /// <param name="targetFrameRate">Target frame rate for the application</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when targetFrameRate is less than or equal to zero</exception>
        public PerformanceMonitor(int targetFrameRate = 60)
        {
            if (targetFrameRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetFrameRate), "Target frame rate must be greater than zero");
            }

            _targetFrameRate = targetFrameRate;
            _targetFrameTime = TimeSpan.FromSeconds(1.0 / targetFrameRate);
            
            try
            {
                // Set up a timer to periodically update and report metrics
                DispatcherTimer metricsTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                metricsTimer.Tick += (s, e) => SafeUpdateMetrics();
                metricsTimer.Start();
            }
            catch (Exception)
            {
                // If timer setup fails, continue without metrics updates
                // The service will still function for manual metric queries
            }
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

        /// <summary>
        /// Current frames per second value
        /// </summary>
        private double _currentFps = 0.0;

        /// <summary>
        /// Lock object for thread-safe access to FPS data
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Timestamp of the last FPS update
        /// </summary>
        private DateTime _lastFpsUpdate = DateTime.Now;

        /// <summary>
        /// Interval for FPS update notifications (100ms = 10 Hz)
        /// </summary>
        private readonly TimeSpan _fpsUpdateInterval = TimeSpan.FromMilliseconds(100);

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
        /// Safely updates performance metrics with exception handling
        /// </summary>
        private void SafeUpdateMetrics()
        {
            try
            {
                UpdateMetrics();
            }
            catch (Exception)
            {
                // Log error if possible, but don't crash the application
                // Continue with graceful degradation
            }
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

            // Update current FPS with thread safety
            lock (_lock)
            {
                _currentFps = fps;
            }

            // Check if we should raise FPS update event (throttled to 10 Hz)
            var currentTime = DateTime.Now;
            if (currentTime - _lastFpsUpdate >= _fpsUpdateInterval)
            {
                _lastFpsUpdate = currentTime;
                try
                {
                    FpsUpdated?.Invoke(this, new FpsUpdatedEventArgs { CurrentFps = fps });
                }
                catch (Exception)
                {
                    // Continue execution even if event handlers fail
                    // This ensures the performance monitor remains functional
                }
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
            
            // Raise the event with exception handling
            try
            {
                MetricsUpdated?.Invoke(this, metrics);
            }
            catch (Exception)
            {
                // Continue execution even if event handlers fail
            }
            
            // Log metrics for debugging with safe formatting
            try
            {
                var fpsStr = double.IsNaN(fps) || double.IsInfinity(fps) ? "NaN" : fps.ToString("F1");
                var avgFrameStr = double.IsNaN(_averageFrameTimeMs) || double.IsInfinity(_averageFrameTimeMs) ? "NaN" : _averageFrameTimeMs.ToString("F2");
                var maxFrameStr = double.IsNaN(metrics.MaxFrameTimeMs) || double.IsInfinity(metrics.MaxFrameTimeMs) ? "NaN" : metrics.MaxFrameTimeMs.ToString("F2");
                var avgPhysicsStr = double.IsNaN(_averagePhysicsTimeMs) || double.IsInfinity(_averagePhysicsTimeMs) ? "NaN" : _averagePhysicsTimeMs.ToString("F2");
                var maxPhysicsStr = double.IsNaN(metrics.MaxPhysicsTimeMs) || double.IsInfinity(metrics.MaxPhysicsTimeMs) ? "NaN" : metrics.MaxPhysicsTimeMs.ToString("F2");
                
                Debug.WriteLine($"FPS: {fpsStr}, Avg Frame: {avgFrameStr}ms, Max Frame: {maxFrameStr}ms, " +
                               $"Avg Physics: {avgPhysicsStr}ms, Max Physics: {maxPhysicsStr}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error formatting performance metrics: {ex.Message}");
            }
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

    /// <summary>
    /// Event arguments for FPS updates
    /// </summary>
    public class FpsUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the current frames per second
        /// </summary>
        public double CurrentFps { get; set; }
    }
}