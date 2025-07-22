using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Thread-safe performance monitor for logging operations
    /// </summary>
    public class LoggingPerformanceMonitor : IPerformanceMonitor
    {
        #region Fields

        /// <summary>
        /// Dictionary storing metrics for different operation types
        /// </summary>
        private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
        
        /// <summary>
        /// Total number of logging operations performed
        /// </summary>
        private long _totalLoggingOperations;
        
        /// <summary>
        /// Total time spent on logging operations in ticks
        /// </summary>
        private long _totalLoggingTimeTicks;
        
        /// <summary>
        /// Total memory allocated in bytes
        /// </summary>
        private long _totalMemoryAllocated;
        
        /// <summary>
        /// Total memory freed in bytes
        /// </summary>
        private long _totalMemoryFreed;
        
        /// <summary>
        /// Total number of log entries processed
        /// </summary>
        private long _totalLogEntriesProcessed;
        
        /// <summary>
        /// Total number of batches processed
        /// </summary>
        private long _totalBatchesProcessed;
        
        /// <summary>
        /// Total size of all batches processed
        /// </summary>
        private long _totalBatchSize;
        
        /// <summary>
        /// Total time spent processing batches in ticks
        /// </summary>
        private long _totalBatchProcessingTimeTicks;

        #endregion Fields

        #region Methods

        /// <summary>
        /// Records a logging operation with its duration
        /// </summary>
        /// <param name="operationType">The type of operation performed</param>
        /// <param name="duration">The duration of the operation</param>
        public void RecordLoggingOperation(string operationType, TimeSpan duration)
        {
            Interlocked.Increment(ref _totalLoggingOperations);
            Interlocked.Add(ref _totalLoggingTimeTicks, duration.Ticks);

            _operationMetrics.AddOrUpdate(operationType,
                new OperationMetrics { Count = 1, TotalTimeTicks = duration.Ticks, MinTimeTicks = duration.Ticks, MaxTimeTicks = duration.Ticks },
                (key, existing) =>
                {
                    Interlocked.Increment(ref existing.Count);
                    Interlocked.Add(ref existing.TotalTimeTicks, duration.Ticks);
                    
                    // Update min time
                    long currentMin = existing.MinTimeTicks;
                    while (duration.Ticks < currentMin)
                    {
                        long original = Interlocked.CompareExchange(ref existing.MinTimeTicks, duration.Ticks, currentMin);
                        if (original == currentMin) break;
                        currentMin = existing.MinTimeTicks;
                    }
                    
                    // Update max time
                    long currentMax = existing.MaxTimeTicks;
                    while (duration.Ticks > currentMax)
                    {
                        long original = Interlocked.CompareExchange(ref existing.MaxTimeTicks, duration.Ticks, currentMax);
                        if (original == currentMax) break;
                        currentMax = existing.MaxTimeTicks;
                    }
                    
                    return existing;
                });
        }

        /// <summary>
        /// Records memory usage statistics
        /// </summary>
        /// <param name="bytesAllocated">Number of bytes allocated</param>
        /// <param name="bytesFreed">Number of bytes freed</param>
        public void RecordMemoryUsage(long bytesAllocated, long bytesFreed)
        {
            Interlocked.Add(ref _totalMemoryAllocated, bytesAllocated);
            Interlocked.Add(ref _totalMemoryFreed, bytesFreed);
        }

        /// <summary>
        /// Records the number of log entries processed
        /// </summary>
        /// <param name="count">Number of log entries processed</param>
        public void RecordLogEntriesProcessed(int count)
        {
            Interlocked.Add(ref _totalLogEntriesProcessed, count);
        }

        /// <summary>
        /// Records batch processing statistics
        /// </summary>
        /// <param name="batchSize">Size of the batch processed</param>
        /// <param name="processingTime">Time taken to process the batch</param>
        public void RecordBatchProcessing(int batchSize, TimeSpan processingTime)
        {
            Interlocked.Increment(ref _totalBatchesProcessed);
            Interlocked.Add(ref _totalBatchSize, batchSize);
            Interlocked.Add(ref _totalBatchProcessingTimeTicks, processingTime.Ticks);
        }

        /// <summary>
        /// Gets the current performance statistics
        /// </summary>
        /// <returns>A snapshot of current performance statistics</returns>
        public PerformanceStatistics GetStatistics()
        {
            var totalOps = Interlocked.Read(ref _totalLoggingOperations);
            var totalTimeTicks = Interlocked.Read(ref _totalLoggingTimeTicks);
            var totalBatches = Interlocked.Read(ref _totalBatchesProcessed);
            var totalBatchSizeValue = Interlocked.Read(ref _totalBatchSize);
            var totalBatchTimeTicks = Interlocked.Read(ref _totalBatchProcessingTimeTicks);

            return new PerformanceStatistics
            {
                TotalLoggingOperations = totalOps,
                TotalLoggingTime = new TimeSpan(totalTimeTicks),
                AverageLoggingTime = totalOps > 0 ? new TimeSpan(totalTimeTicks / totalOps) : TimeSpan.Zero,
                TotalMemoryAllocated = Interlocked.Read(ref _totalMemoryAllocated),
                TotalMemoryFreed = Interlocked.Read(ref _totalMemoryFreed),
                NetMemoryUsage = Interlocked.Read(ref _totalMemoryAllocated) - Interlocked.Read(ref _totalMemoryFreed),
                TotalLogEntriesProcessed = Interlocked.Read(ref _totalLogEntriesProcessed),
                TotalBatchesProcessed = (int)totalBatches,
                AverageBatchSize = totalBatches > 0 ? (double)totalBatchSizeValue / totalBatches : 0,
                AverageBatchProcessingTime = totalBatches > 0 ? new TimeSpan(totalBatchTimeTicks / totalBatches) : TimeSpan.Zero,
                OperationBreakdown = _operationMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new OperationStatistics
                    {
                        Count = kvp.Value.Count,
                        TotalTime = new TimeSpan(kvp.Value.TotalTimeTicks),
                        AverageTime = kvp.Value.Count > 0 ? new TimeSpan(kvp.Value.TotalTimeTicks / kvp.Value.Count) : TimeSpan.Zero,
                        MinTime = new TimeSpan(kvp.Value.MinTimeTicks),
                        MaxTime = new TimeSpan(kvp.Value.MaxTimeTicks)
                    })
            };
        }

        /// <summary>
        /// Resets all performance statistics to zero
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalLoggingOperations, 0);
            Interlocked.Exchange(ref _totalLoggingTimeTicks, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);
            Interlocked.Exchange(ref _totalMemoryFreed, 0);
            Interlocked.Exchange(ref _totalLogEntriesProcessed, 0);
            Interlocked.Exchange(ref _totalBatchesProcessed, 0);
            Interlocked.Exchange(ref _totalBatchSize, 0);
            Interlocked.Exchange(ref _totalBatchProcessingTimeTicks, 0);
            _operationMetrics.Clear();
        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// Internal class for storing operation metrics
        /// </summary>
        private class OperationMetrics
        {
            /// <summary>
            /// Number of operations recorded
            /// </summary>
            public long Count;
            
            /// <summary>
            /// Total time for all operations in ticks
            /// </summary>
            public long TotalTimeTicks;
            
            /// <summary>
            /// Minimum operation time in ticks
            /// </summary>
            public long MinTimeTicks = long.MaxValue;
            
            /// <summary>
            /// Maximum operation time in ticks
            /// </summary>
            public long MaxTimeTicks = long.MinValue;
        }

        #endregion Nested Types
    }
}