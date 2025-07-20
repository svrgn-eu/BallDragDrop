using System;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Configuration settings for logging performance optimizations
    /// </summary>
    public class PerformanceConfiguration
    {
        /// <summary>
        /// Maximum size of the log entry object pool
        /// </summary>
        public int LogEntryPoolSize { get; set; } = 1000;

        /// <summary>
        /// Batch size for async log processing
        /// </summary>
        public int AsyncBatchSize { get; set; } = 100;

        /// <summary>
        /// Timeout for batch processing
        /// </summary>
        public TimeSpan AsyncBatchTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Whether to enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Whether to enable async logging
        /// </summary>
        public bool EnableAsyncLogging { get; set; } = true;

        /// <summary>
        /// Whether to enable object pooling
        /// </summary>
        public bool EnableObjectPooling { get; set; } = true;

        /// <summary>
        /// Interval for performance statistics reporting
        /// </summary>
        public TimeSpan PerformanceReportingInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Memory threshold for triggering GC collection (in MB)
        /// </summary>
        public long MemoryThresholdMB { get; set; } = 100;

        /// <summary>
        /// Whether to enable automatic performance reporting
        /// </summary>
        public bool EnableAutomaticPerformanceReporting { get; set; } = false;

        /// <summary>
        /// Creates a default configuration optimized for performance
        /// </summary>
        public static PerformanceConfiguration CreateDefault()
        {
            return new PerformanceConfiguration();
        }

        /// <summary>
        /// Creates a configuration optimized for high-throughput scenarios
        /// </summary>
        public static PerformanceConfiguration CreateHighThroughput()
        {
            return new PerformanceConfiguration
            {
                LogEntryPoolSize = 5000,
                AsyncBatchSize = 500,
                AsyncBatchTimeout = TimeSpan.FromMilliseconds(100),
                EnablePerformanceMonitoring = true,
                EnableAsyncLogging = true,
                EnableObjectPooling = true,
                PerformanceReportingInterval = TimeSpan.FromMinutes(1),
                MemoryThresholdMB = 200,
                EnableAutomaticPerformanceReporting = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for low-memory scenarios
        /// </summary>
        public static PerformanceConfiguration CreateLowMemory()
        {
            return new PerformanceConfiguration
            {
                LogEntryPoolSize = 100,
                AsyncBatchSize = 25,
                AsyncBatchTimeout = TimeSpan.FromSeconds(1),
                EnablePerformanceMonitoring = false,
                EnableAsyncLogging = true,
                EnableObjectPooling = true,
                PerformanceReportingInterval = TimeSpan.FromMinutes(10),
                MemoryThresholdMB = 50,
                EnableAutomaticPerformanceReporting = false
            };
        }

        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        public void Validate()
        {
            if (LogEntryPoolSize <= 0)
                throw new ArgumentException("LogEntryPoolSize must be greater than 0");

            if (AsyncBatchSize <= 0)
                throw new ArgumentException("AsyncBatchSize must be greater than 0");

            if (AsyncBatchTimeout <= TimeSpan.Zero)
                throw new ArgumentException("AsyncBatchTimeout must be greater than zero");

            if (PerformanceReportingInterval <= TimeSpan.Zero)
                throw new ArgumentException("PerformanceReportingInterval must be greater than zero");

            if (MemoryThresholdMB <= 0)
                throw new ArgumentException("MemoryThresholdMB must be greater than 0");
        }
    }
}