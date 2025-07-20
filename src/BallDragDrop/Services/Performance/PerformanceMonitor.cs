using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Thread-safe performance monitor for logging operations
    /// </summary>
    public class LoggingPerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
        private long _totalLoggingOperations;
        private long _totalLoggingTimeTicks;
        private long _totalMemoryAllocated;
        private long _totalMemoryFreed;
        private long _totalLogEntriesProcessed;
        private long _totalBatchesProcessed;
        private long _totalBatchSize;
        private long _totalBatchProcessingTimeTicks;

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

        public void RecordMemoryUsage(long bytesAllocated, long bytesFreed)
        {
            Interlocked.Add(ref _totalMemoryAllocated, bytesAllocated);
            Interlocked.Add(ref _totalMemoryFreed, bytesFreed);
        }

        public void RecordLogEntriesProcessed(int count)
        {
            Interlocked.Add(ref _totalLogEntriesProcessed, count);
        }

        public void RecordBatchProcessing(int batchSize, TimeSpan processingTime)
        {
            Interlocked.Increment(ref _totalBatchesProcessed);
            Interlocked.Add(ref _totalBatchSize, batchSize);
            Interlocked.Add(ref _totalBatchProcessingTimeTicks, processingTime.Ticks);
        }

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

        private class OperationMetrics
        {
            public long Count;
            public long TotalTimeTicks;
            public long MinTimeTicks = long.MaxValue;
            public long MaxTimeTicks = long.MinValue;
        }
    }
}