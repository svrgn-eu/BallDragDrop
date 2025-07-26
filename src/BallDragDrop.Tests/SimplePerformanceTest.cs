using System;
using System.Threading;
using BallDragDrop.Services;
using BallDragDrop.Services.Performance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Simple test to verify performance monitoring functionality
    /// </summary>
    [TestClass]
    public class SimplePerformanceTest
    {
        [TestMethod]
        public void TestBasicPerformanceMonitoring()
        {
            // Create performance monitor and logging service
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);

            // Perform some logging operations
            for (int i = 0; i < 100; i++)
            {
                logService.LogInformation("Test message {0}", i);
            }

            // Wait for async processing
            Thread.Sleep(1000);

            // Get performance statistics
            var stats = performanceMonitor.GetStatistics();

            // Verify that operations were recorded
            Assert.IsTrue(stats.TotalLoggingOperations > 0, "No logging operations were recorded");
            Assert.IsTrue(stats.TotalLogEntriesProcessed > 0, "No log entries were processed");

            Console.WriteLine($"Total Logging Operations: {stats.TotalLoggingOperations}");
            Console.WriteLine($"Average Logging Time: {stats.AverageLoggingTime.TotalMilliseconds:F4}ms");
            Console.WriteLine($"Total Log Entries Processed: {stats.TotalLogEntriesProcessed}");

            // Cleanup
            logService.Dispose();
        }

        [TestMethod]
        public void TestObjectPooling()
        {
            // Test object pooling functionality
            var pool = new LogEntryPool(10);

            // Get some entries from the pool
            var entry1 = pool.Get();
            var entry2 = pool.Get();

            Assert.IsNotNull(entry1);
            Assert.IsNotNull(entry2);

            // Set some values
            entry1.Message = "Test message 1";
            entry2.Message = "Test message 2";

            // Return to pool
            entry1.Dispose();
            entry2.Dispose();

            // Get new entries (should be recycled)
            var entry3 = pool.Get();
            var entry4 = pool.Get();

            // Entries should be reset
            Assert.AreEqual(string.Empty, entry3.Message);
            Assert.AreEqual(string.Empty, entry4.Message);

            // Cleanup
            entry3.Dispose();
            entry4.Dispose();
            pool.Dispose();
        }

        [TestMethod]
        public void TestPerformanceConfiguration()
        {
            // Test performance configuration
            var config = PerformanceConfiguration.CreateDefault();
            config.Validate();

            Assert.AreEqual(1000, config.LogEntryPoolSize);
            Assert.AreEqual(100, config.AsyncBatchSize);
            Assert.IsTrue(config.EnablePerformanceMonitoring);

            var highThroughputConfig = PerformanceConfiguration.CreateHighThroughput();
            highThroughputConfig.Validate();

            Assert.AreEqual(5000, highThroughputConfig.LogEntryPoolSize);
            Assert.AreEqual(500, highThroughputConfig.AsyncBatchSize);

            var lowMemoryConfig = PerformanceConfiguration.CreateLowMemory();
            lowMemoryConfig.Validate();

            Assert.AreEqual(100, lowMemoryConfig.LogEntryPoolSize);
            Assert.AreEqual(25, lowMemoryConfig.AsyncBatchSize);
        }
    }
}