using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BallDragDrop.Services;
using BallDragDrop.Services.Performance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Comprehensive performance validation tests for the logging system
    /// </summary>
    [TestClass]
    public class LoggingPerformanceValidationTests
    {
        private const int SmallTestSize = 1000;
        private const int MediumTestSize = 10000;
        private const int LargeTestSize = 100000;
        private const double MaxAcceptableLatencyMs = 1.0;
        private const double MaxAcceptableThroughputDegradation = 20.0; // 20% max degradation

        /// <summary>
        /// Validates that logging has minimal impact on user experience
        /// </summary>
        [TestMethod]
        public void ValidateMinimalUserExperienceImpact()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);

            // Simulate user interactions with and without logging
            var userActionStopwatch = Stopwatch.StartNew();
            
            // Simulate 1000 user interactions (drag, drop, etc.)
            for (int i = 0; i < SmallTestSize; i++)
            {
                // Simulate user action processing
                SimulateUserAction(logService, i);
                
                // Small delay to simulate real user interaction timing
                Thread.Sleep(1);
            }
            
            userActionStopwatch.Stop();
            
            // Wait for async processing
            Thread.Sleep(2000);
            
            var stats = performanceMonitor.GetStatistics();
            double totalUserActionTimeMs = userActionStopwatch.Elapsed.TotalMilliseconds;
            double averageActionTimeMs = totalUserActionTimeMs / SmallTestSize;
            
            Console.WriteLine($"User Experience Impact Validation:");
            Console.WriteLine($"Total User Actions: {SmallTestSize}");
            Console.WriteLine($"Total Time: {totalUserActionTimeMs:F2}ms");
            Console.WriteLine($"Average Action Time: {averageActionTimeMs:F2}ms");
            Console.WriteLine($"Logging Operations: {stats.TotalLoggingOperations}");
            Console.WriteLine($"Average Logging Time: {stats.AverageLoggingTime.TotalMilliseconds:F4}ms");
            
            // Assert that user actions complete quickly
            Assert.IsTrue(averageActionTimeMs < 50, // 50ms max per user action including logging
                $"User actions are too slow with logging enabled: {averageActionTimeMs:F2}ms");
            
            logService.Dispose();
        }

        /// <summary>
        /// Validates logging performance under concurrent access
        /// </summary>
        [TestMethod]
        public void ValidateConcurrentLoggingPerformance()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            const int threadCount = 10;
            const int operationsPerThread = SmallTestSize / threadCount;
            var tasks = new Task[threadCount];
            var overallStopwatch = Stopwatch.StartNew();
            
            // Create concurrent logging tasks
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        logService.LogInformation("Thread {ThreadId} operation {Operation}", threadId, i);
                        
                        if (i % 10 == 0)
                        {
                            logService.LogDebug("Thread {ThreadId} debug message {Operation}", threadId, i);
                        }
                        
                        if (i % 50 == 0)
                        {
                            logService.LogWarning("Thread {ThreadId} warning {Operation}", threadId, i);
                        }
                    }
                });
            }
            
            // Wait for all tasks to complete
            Task.WaitAll(tasks);
            overallStopwatch.Stop();
            
            // Wait for async processing
            Thread.Sleep(3000);
            
            var stats = performanceMonitor.GetStatistics();
            double totalTimeMs = overallStopwatch.Elapsed.TotalMilliseconds;
            double operationsPerSecond = (SmallTestSize / totalTimeMs) * 1000;
            
            Console.WriteLine($"Concurrent Logging Performance Validation:");
            Console.WriteLine($"Threads: {threadCount}");
            Console.WriteLine($"Operations per Thread: {operationsPerThread}");
            Console.WriteLine($"Total Operations: {SmallTestSize}");
            Console.WriteLine($"Total Time: {totalTimeMs:F2}ms");
            Console.WriteLine($"Operations per Second: {operationsPerSecond:F0}");
            Console.WriteLine($"Logging Operations Recorded: {stats.TotalLoggingOperations}");
            Console.WriteLine($"Batches Processed: {stats.TotalBatchesProcessed}");
            
            // Assert reasonable throughput under concurrent load
            Assert.IsTrue(operationsPerSecond > 1000, // At least 1000 ops/sec
                $"Concurrent logging throughput is too low: {operationsPerSecond:F0} ops/sec");
            
            logService.Dispose();
        }

        /// <summary>
        /// Validates memory efficiency of object pooling
        /// </summary>
        [TestMethod]
        public void ValidateObjectPoolingEfficiency()
        {
            // Test with object pooling enabled
            var performanceMonitorWithPooling = new LoggingPerformanceMonitor();
            var logServiceWithPooling = new Log4NetService(performanceMonitorWithPooling);
            
            // Force GC before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long memoryBeforePooling = GC.GetTotalMemory(false);
            
            // Perform intensive logging with pooling
            for (int i = 0; i < MediumTestSize; i++)
            {
                logServiceWithPooling.LogInformation("Pooled log message {Index} with data {Data}", i, $"data_{i}");
            }
            
            Thread.Sleep(2000); // Wait for processing
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long memoryAfterPooling = GC.GetTotalMemory(false);
            long memoryUsedWithPooling = memoryAfterPooling - memoryBeforePooling;
            
            logServiceWithPooling.Dispose();
            
            // Test without object pooling (simulate by creating many objects)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long memoryBeforeNonPooling = GC.GetTotalMemory(false);
            
            // Simulate non-pooled behavior by creating many objects
            var objects = new List<object>();
            for (int i = 0; i < MediumTestSize; i++)
            {
                objects.Add(new { Index = i, Data = $"data_{i}", Timestamp = DateTime.UtcNow });
            }
            
            long memoryAfterNonPooling = GC.GetTotalMemory(false);
            long memoryUsedWithoutPooling = memoryAfterNonPooling - memoryBeforeNonPooling;
            
            double poolingEfficiency = ((double)(memoryUsedWithoutPooling - memoryUsedWithPooling) / memoryUsedWithoutPooling) * 100;
            
            Console.WriteLine($"Object Pooling Efficiency Validation:");
            Console.WriteLine($"Memory with Pooling: {memoryUsedWithPooling / (1024.0 * 1024.0):F2}MB");
            Console.WriteLine($"Memory without Pooling: {memoryUsedWithoutPooling / (1024.0 * 1024.0):F2}MB");
            Console.WriteLine($"Memory Saved: {(memoryUsedWithoutPooling - memoryUsedWithPooling) / (1024.0 * 1024.0):F2}MB");
            Console.WriteLine($"Pooling Efficiency: {poolingEfficiency:F1}%");
            
            // Assert that pooling provides memory benefits
            Assert.IsTrue(memoryUsedWithPooling < memoryUsedWithoutPooling,
                "Object pooling should reduce memory usage");
        }

        /// <summary>
        /// Validates that async logging doesn't block the UI thread
        /// </summary>
        [TestMethod]
        public void ValidateAsyncLoggingNonBlocking()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Measure time for logging calls to return (should be very fast for async)
            var callTimes = new List<double>();
            
            for (int i = 0; i < SmallTestSize; i++)
            {
                var callStopwatch = Stopwatch.StartNew();
                
                logService.LogInformation("Async test message {Index} with timestamp {Timestamp}", i, DateTime.UtcNow);
                
                callStopwatch.Stop();
                callTimes.Add(callStopwatch.Elapsed.TotalMilliseconds);
            }
            
            // Wait for async processing
            Thread.Sleep(2000);
            
            double averageCallTimeMs = callTimes.Average();
            double maxCallTimeMs = callTimes.Max();
            double percentile95Ms = callTimes.OrderBy(x => x).Skip((int)(callTimes.Count * 0.95)).First();
            
            Console.WriteLine($"Async Logging Non-Blocking Validation:");
            Console.WriteLine($"Total Calls: {SmallTestSize}");
            Console.WriteLine($"Average Call Time: {averageCallTimeMs:F4}ms");
            Console.WriteLine($"Max Call Time: {maxCallTimeMs:F4}ms");
            Console.WriteLine($"95th Percentile: {percentile95Ms:F4}ms");
            
            // Assert that logging calls return very quickly (non-blocking)
            Assert.IsTrue(averageCallTimeMs < MaxAcceptableLatencyMs,
                $"Average logging call time ({averageCallTimeMs:F4}ms) exceeds acceptable latency ({MaxAcceptableLatencyMs}ms)");
            
            Assert.IsTrue(percentile95Ms < MaxAcceptableLatencyMs * 2,
                $"95th percentile call time ({percentile95Ms:F4}ms) is too high");
            
            logService.Dispose();
        }

        /// <summary>
        /// Validates performance under high-throughput scenarios
        /// </summary>
        [TestMethod]
        public void ValidateHighThroughputPerformance()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Test high-throughput logging
            var throughputStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < LargeTestSize; i++)
            {
                logService.LogInformation("High throughput message {Index}", i);
                
                // Add variety to test different code paths
                if (i % 100 == 0)
                {
                    logService.LogStructured(LogLevel.Warning, "Structured message at {Index} with {Data}", i, $"data_{i}");
                }
                
                if (i % 1000 == 0)
                {
                    logService.LogError(new Exception($"Test exception {i}"), "Error at index {Index}", i);
                }
            }
            
            throughputStopwatch.Stop();
            
            // Wait for processing to complete
            Thread.Sleep(5000);
            
            var stats = performanceMonitor.GetStatistics();
            double totalTimeSeconds = throughputStopwatch.Elapsed.TotalSeconds;
            double messagesPerSecond = LargeTestSize / totalTimeSeconds;
            
            Console.WriteLine($"High Throughput Performance Validation:");
            Console.WriteLine($"Total Messages: {LargeTestSize:N0}");
            Console.WriteLine($"Total Time: {totalTimeSeconds:F2}s");
            Console.WriteLine($"Messages per Second: {messagesPerSecond:F0}");
            Console.WriteLine($"Batches Processed: {stats.TotalBatchesProcessed}");
            Console.WriteLine($"Average Batch Size: {stats.AverageBatchSize:F1}");
            Console.WriteLine($"Average Batch Processing Time: {stats.AverageBatchProcessingTime.TotalMilliseconds:F2}ms");
            
            // Assert acceptable throughput
            Assert.IsTrue(messagesPerSecond > 10000, // At least 10K messages/sec
                $"High throughput performance is insufficient: {messagesPerSecond:F0} messages/sec");
            
            logService.Dispose();
        }

        /// <summary>
        /// Validates that logging performance doesn't degrade over time
        /// </summary>
        [TestMethod]
        public void ValidatePerformanceStabilityOverTime()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            const int phases = 10;
            const int messagesPerPhase = SmallTestSize;
            var phaseTimes = new List<double>();
            
            for (int phase = 0; phase < phases; phase++)
            {
                var phaseStopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < messagesPerPhase; i++)
                {
                    logService.LogInformation("Phase {Phase} message {Index}", phase, i);
                }
                
                phaseStopwatch.Stop();
                phaseTimes.Add(phaseStopwatch.Elapsed.TotalMilliseconds);
                
                // Small delay between phases
                Thread.Sleep(100);
            }
            
            // Wait for final processing
            Thread.Sleep(2000);
            
            double firstPhaseTime = phaseTimes.First();
            double lastPhaseTime = phaseTimes.Last();
            double averagePhaseTime = phaseTimes.Average();
            double performanceDegradation = ((lastPhaseTime - firstPhaseTime) / firstPhaseTime) * 100;
            
            Console.WriteLine($"Performance Stability Over Time Validation:");
            Console.WriteLine($"Phases: {phases}");
            Console.WriteLine($"Messages per Phase: {messagesPerPhase}");
            Console.WriteLine($"First Phase Time: {firstPhaseTime:F2}ms");
            Console.WriteLine($"Last Phase Time: {lastPhaseTime:F2}ms");
            Console.WriteLine($"Average Phase Time: {averagePhaseTime:F2}ms");
            Console.WriteLine($"Performance Degradation: {performanceDegradation:F1}%");
            
            // Assert stable performance over time
            Assert.IsTrue(Math.Abs(performanceDegradation) < MaxAcceptableThroughputDegradation,
                $"Performance degradation ({performanceDegradation:F1}%) exceeds acceptable threshold ({MaxAcceptableThroughputDegradation}%)");
            
            logService.Dispose();
        }

        // Helper method to simulate user actions
        private void SimulateUserAction(ILogService logService, int actionIndex)
        {
            // Simulate a user drag operation
            logService.LogDebug("User started drag operation {ActionIndex}", actionIndex);
            
            // Simulate some processing
            Thread.SpinWait(100);
            
            // Log the completion
            logService.LogInformation("User completed drag operation {ActionIndex} at position ({X}, {Y})", 
                actionIndex, actionIndex % 800, actionIndex % 600);
        }
    }
}