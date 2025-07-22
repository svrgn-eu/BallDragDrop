using System;
using System.Collections.Generic;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Interface for performance monitoring of logging operations
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Records the execution time of a logging operation
        /// </summary>
        void RecordLoggingOperation(string operationType, TimeSpan duration);
        
        /// <summary>
        /// Records memory usage for logging operations
        /// </summary>
        void RecordMemoryUsage(long bytesAllocated, long bytesFreed);
        
        /// <summary>
        /// Records the number of log entries processed
        /// </summary>
        void RecordLogEntriesProcessed(int count);
        
        /// <summary>
        /// Records batch processing metrics
        /// </summary>
        void RecordBatchProcessing(int batchSize, TimeSpan processingTime);
        
        /// <summary>
        /// Gets current performance statistics
        /// </summary>
        PerformanceStatistics GetStatistics();
        
        /// <summary>
        /// Resets all performance counters
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Performance statistics for logging operations
    /// </summary>
    public class PerformanceStatistics
    {
        public long TotalLoggingOperations { get; set; }
        public TimeSpan TotalLoggingTime { get; set; }
        public TimeSpan AverageLoggingTime { get; set; }
        public long TotalMemoryAllocated { get; set; }
        public long TotalMemoryFreed { get; set; }
        public long NetMemoryUsage { get; set; }
        public long TotalLogEntriesProcessed { get; set; }
        public int TotalBatchesProcessed { get; set; }
        public double AverageBatchSize { get; set; }
        public TimeSpan AverageBatchProcessingTime { get; set; }
        public Dictionary<string, OperationStatistics> OperationBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Statistics for specific operation types
    /// </summary>
    public class OperationStatistics
    {
        public long Count { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MinTime { get; set; } = TimeSpan.MaxValue;
        public TimeSpan MaxTime { get; set; } = TimeSpan.MinValue;
    }
}