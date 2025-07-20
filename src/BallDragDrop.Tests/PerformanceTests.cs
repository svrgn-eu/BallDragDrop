using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BallDragDrop.Models;
using BallDragDrop.Services;
using BallDragDrop.Services.Performance;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class PerformanceTests
    {
        // Performance thresholds
        private const double MaxAcceptableFrameTimeMs = 16.67; // ~60 FPS
        private const double MaxAcceptablePhysicsUpdateTimeMs = 5.0; // 5ms for physics update
        private const int TestDurationSeconds = 5; // Duration of performance tests
        
        // Logging performance thresholds
        private const double MaxAcceptableLoggingOverheadMs = 0.1; // 0.1ms per log operation
        private const double MaxAcceptableMemoryOverheadMB = 10.0; // 10MB memory overhead
        private const int LoggingTestIterations = 10000; // Number of log operations for testing
        
        /// <summary>
        /// Tests the rendering performance of the application
        /// </summary>
        [TestMethod]
        public async Task TestRenderingPerformance()
        {
            // Create a test window on the UI thread
            MainWindow window = null;
            
            // Create and show the window on the UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                window = new MainWindow();
                window.Show();
            });
            
            // Wait for the window to initialize
            await Task.Delay(500);
            
            // Performance metrics
            int frameCount = 0;
            double totalFrameTimeMs = 0;
            double maxFrameTimeMs = 0;
            
            // Create a stopwatch for measuring frame times
            Stopwatch frameStopwatch = new Stopwatch();
            Stopwatch testStopwatch = new Stopwatch();
            
            // Set up a rendering event handler to measure frame times
            EventHandler renderingHandler = null;
            
            // Start the test
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Set up the rendering event handler
                renderingHandler = (sender, e) =>
                {
                    frameStopwatch.Restart();
                    
                    // Simulate some rendering work
                    if (window.DataContext is BallViewModel viewModel)
                    {
                        // Move the ball to trigger rendering
                        viewModel.X += 1;
                        if (viewModel.X > window.MainCanvas.Width - viewModel.Radius)
                        {
                            viewModel.X = viewModel.Radius;
                        }
                    }
                    
                    frameStopwatch.Stop();
                    double frameTimeMs = frameStopwatch.Elapsed.TotalMilliseconds;
                    
                    // Update metrics
                    frameCount++;
                    totalFrameTimeMs += frameTimeMs;
                    maxFrameTimeMs = Math.Max(maxFrameTimeMs, frameTimeMs);
                };
                
                // Add the event handler
                CompositionTarget.Rendering += renderingHandler;
                
                // Start the test timer
                testStopwatch.Start();
            });
            
            // Wait for the test duration
            await Task.Delay(TimeSpan.FromSeconds(TestDurationSeconds));
            
            // Remove the event handler and clean up
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Stop the test timer
                testStopwatch.Stop();
                
                // Remove the event handler
                CompositionTarget.Rendering -= renderingHandler;
                
                // Close the window
                window.Close();
            });
            
            // Calculate metrics
            double testDurationMs = testStopwatch.Elapsed.TotalMilliseconds;
            double averageFrameTimeMs = totalFrameTimeMs / frameCount;
            double framesPerSecond = frameCount / (testDurationMs / 1000.0);
            
            // Output results
            Console.WriteLine($"Rendering Performance Test Results:");
            Console.WriteLine($"Total Frames: {frameCount}");
            Console.WriteLine($"Test Duration: {testDurationMs:F2}ms");
            Console.WriteLine($"Average Frame Time: {averageFrameTimeMs:F2}ms");
            Console.WriteLine($"Max Frame Time: {maxFrameTimeMs:F2}ms");
            Console.WriteLine($"Frames Per Second: {framesPerSecond:F2}");
            
            // Assert performance meets requirements
            Assert.IsTrue(averageFrameTimeMs < MaxAcceptableFrameTimeMs, 
                $"Average frame time ({averageFrameTimeMs:F2}ms) exceeds maximum acceptable time ({MaxAcceptableFrameTimeMs}ms)");
        }
        
        /// <summary>
        /// Tests the physics engine performance
        /// </summary>
        [TestMethod]
        public void TestPhysicsEnginePerformance()
        {
            // Create a physics engine
            PhysicsEngine physicsEngine = new PhysicsEngine();
            
            // Create a ball model
            BallModel ball = new BallModel(100, 100, 25);
            
            // Set initial velocity
            ball.SetVelocity(200, 150);
            
            // Performance metrics
            int updateCount = 0;
            double totalUpdateTimeMs = 0;
            double maxUpdateTimeMs = 0;
            
            // Create a stopwatch for measuring update times
            Stopwatch updateStopwatch = new Stopwatch();
            Stopwatch testStopwatch = new Stopwatch();
            
            // Start the test timer
            testStopwatch.Start();
            
            // Run physics updates for the test duration
            while (testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Measure the time to update the physics
                updateStopwatch.Restart();
                
                // Update the physics
                physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600);
                
                updateStopwatch.Stop();
                double updateTimeMs = updateStopwatch.Elapsed.TotalMilliseconds;
                
                // Update metrics
                updateCount++;
                totalUpdateTimeMs += updateTimeMs;
                maxUpdateTimeMs = Math.Max(maxUpdateTimeMs, updateTimeMs);
                
                // Don't hog the CPU
                if (updateCount % 1000 == 0)
                {
                    Thread.Sleep(1);
                }
            }
            
            // Stop the test timer
            testStopwatch.Stop();
            
            // Calculate metrics
            double testDurationMs = testStopwatch.Elapsed.TotalMilliseconds;
            double averageUpdateTimeMs = totalUpdateTimeMs / updateCount;
            double updatesPerSecond = updateCount / (testDurationMs / 1000.0);
            
            // Output results
            Console.WriteLine($"Physics Engine Performance Test Results:");
            Console.WriteLine($"Total Updates: {updateCount}");
            Console.WriteLine($"Test Duration: {testDurationMs:F2}ms");
            Console.WriteLine($"Average Update Time: {averageUpdateTimeMs:F2}ms");
            Console.WriteLine($"Max Update Time: {maxUpdateTimeMs:F2}ms");
            Console.WriteLine($"Updates Per Second: {updatesPerSecond:F2}");
            
            // Assert performance meets requirements
            Assert.IsTrue(averageUpdateTimeMs < MaxAcceptablePhysicsUpdateTimeMs, 
                $"Average physics update time ({averageUpdateTimeMs:F2}ms) exceeds maximum acceptable time ({MaxAcceptablePhysicsUpdateTimeMs}ms)");
        }
        
        /// <summary>
        /// Tests the performance under heavy load (many physics updates)
        /// </summary>
        [TestMethod]
        public void TestPerformanceUnderLoad()
        {
            // Create a physics engine
            PhysicsEngine physicsEngine = new PhysicsEngine();
            
            // Create multiple ball models to simulate heavy load
            const int ballCount = 100;
            BallModel[] balls = new BallModel[ballCount];
            
            // Initialize balls with random positions and velocities
            Random random = new Random(42); // Fixed seed for reproducibility
            for (int i = 0; i < ballCount; i++)
            {
                balls[i] = new BallModel(
                    random.NextDouble() * 800,
                    random.NextDouble() * 600,
                    10 + random.NextDouble() * 20);
                
                balls[i].SetVelocity(
                    (random.NextDouble() * 200) - 100,
                    (random.NextDouble() * 200) - 100);
            }
            
            // Performance metrics
            int updateCount = 0;
            double totalUpdateTimeMs = 0;
            double maxUpdateTimeMs = 0;
            
            // Create a stopwatch for measuring update times
            Stopwatch updateStopwatch = new Stopwatch();
            Stopwatch testStopwatch = new Stopwatch();
            
            // Start the test timer
            testStopwatch.Start();
            
            // Run physics updates for the test duration
            while (testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
            {
                // Measure the time to update all balls
                updateStopwatch.Restart();
                
                // Update the physics for all balls
                for (int i = 0; i < ballCount; i++)
                {
                    physicsEngine.UpdateBall(balls[i], 1.0/60.0, 0, 0, 800, 600);
                    
                    // Check for collisions with other balls (O(n²) complexity)
                    for (int j = i + 1; j < ballCount; j++)
                    {
                        physicsEngine.DetectAndResolveCollision(balls[i], balls[j]);
                    }
                }
                
                updateStopwatch.Stop();
                double updateTimeMs = updateStopwatch.Elapsed.TotalMilliseconds;
                
                // Update metrics
                updateCount++;
                totalUpdateTimeMs += updateTimeMs;
                maxUpdateTimeMs = Math.Max(maxUpdateTimeMs, updateTimeMs);
                
                // Don't hog the CPU
                if (updateCount % 100 == 0)
                {
                    Thread.Sleep(1);
                }
            }
            
            // Stop the test timer
            testStopwatch.Stop();
            
            // Calculate metrics
            double testDurationMs = testStopwatch.Elapsed.TotalMilliseconds;
            double averageUpdateTimeMs = totalUpdateTimeMs / updateCount;
            double updatesPerSecond = updateCount / (testDurationMs / 1000.0);
            
            // Output results
            Console.WriteLine($"Heavy Load Performance Test Results ({ballCount} balls):");
            Console.WriteLine($"Total Updates: {updateCount}");
            Console.WriteLine($"Test Duration: {testDurationMs:F2}ms");
            Console.WriteLine($"Average Update Time: {averageUpdateTimeMs:F2}ms");
            Console.WriteLine($"Max Update Time: {maxUpdateTimeMs:F2}ms");
            Console.WriteLine($"Updates Per Second: {updatesPerSecond:F2}");
            
            // For heavy load, we expect higher update times, so we adjust our expectations
            double adjustedThreshold = MaxAcceptablePhysicsUpdateTimeMs * Math.Log10(ballCount);
            
            // Assert performance meets adjusted requirements
            Assert.IsTrue(averageUpdateTimeMs < adjustedThreshold, 
                $"Average update time under load ({averageUpdateTimeMs:F2}ms) exceeds adjusted threshold ({adjustedThreshold}ms)");
        }

        /// <summary>
        /// Tests the performance overhead of logging operations
        /// </summary>
        [TestMethod]
        public void TestLoggingPerformanceOverhead()
        {
            // Create logging service with performance monitoring
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Warm up the logging system
            for (int i = 0; i < 100; i++)
            {
                logService.LogInformation("Warmup message {Index}", i);
            }
            
            // Reset performance counters after warmup
            performanceMonitor.Reset();
            
            // Measure baseline performance (no logging)
            var baselineStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                // Simulate work without logging
                var dummy = $"Processing item {i}";
                Thread.SpinWait(10); // Minimal work simulation
            }
            baselineStopwatch.Stop();
            
            // Measure performance with logging
            var loggingStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                // Simulate work with logging
                var dummy = $"Processing item {i}";
                Thread.SpinWait(10); // Same work simulation
                logService.LogDebug("Processing item {Index} with value {Value}", i, dummy);
            }
            loggingStopwatch.Stop();
            
            // Wait for async processing to complete
            Thread.Sleep(2000);
            
            // Get performance statistics
            var stats = performanceMonitor.GetStatistics();
            
            // Calculate overhead
            double baselineTimeMs = baselineStopwatch.Elapsed.TotalMilliseconds;
            double loggingTimeMs = loggingStopwatch.Elapsed.TotalMilliseconds;
            double overheadMs = loggingTimeMs - baselineTimeMs;
            double overheadPerOperationMs = overheadMs / LoggingTestIterations;
            
            // Output results
            Console.WriteLine($"Logging Performance Overhead Test Results:");
            Console.WriteLine($"Baseline Time: {baselineTimeMs:F2}ms");
            Console.WriteLine($"Logging Time: {loggingTimeMs:F2}ms");
            Console.WriteLine($"Total Overhead: {overheadMs:F2}ms");
            Console.WriteLine($"Overhead Per Operation: {overheadPerOperationMs:F4}ms");
            Console.WriteLine($"Total Logging Operations: {stats.TotalLoggingOperations}");
            Console.WriteLine($"Average Logging Time: {stats.AverageLoggingTime.TotalMilliseconds:F4}ms");
            
            // Assert performance meets requirements
            Assert.IsTrue(overheadPerOperationMs < MaxAcceptableLoggingOverheadMs,
                $"Logging overhead per operation ({overheadPerOperationMs:F4}ms) exceeds maximum acceptable overhead ({MaxAcceptableLoggingOverheadMs}ms)");
            
            // Cleanup
            logService.Dispose();
        }

        /// <summary>
        /// Tests memory usage patterns of the logging system
        /// </summary>
        [TestMethod]
        public void TestLoggingMemoryUsage()
        {
            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long initialMemory = GC.GetTotalMemory(false);
            
            // Create logging service with performance monitoring
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Perform intensive logging operations
            var testData = new List<string>();
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                var data = $"Test data item {i} with some additional content to simulate real log messages";
                testData.Add(data);
                
                logService.LogInformation("Processing {ItemIndex}: {Data}", i, data);
                
                if (i % 1000 == 0)
                {
                    logService.LogWarning("Checkpoint reached at item {ItemIndex}", i);
                }
                
                if (i % 5000 == 0)
                {
                    logService.LogError(new Exception($"Test exception {i}"), "Simulated error at item {ItemIndex}", i);
                }
            }
            
            // Wait for async processing to complete
            Thread.Sleep(3000);
            
            // Force garbage collection after test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long finalMemory = GC.GetTotalMemory(false);
            long memoryUsedBytes = finalMemory - initialMemory;
            double memoryUsedMB = memoryUsedBytes / (1024.0 * 1024.0);
            
            // Get performance statistics
            var stats = performanceMonitor.GetStatistics();
            
            // Output results
            Console.WriteLine($"Logging Memory Usage Test Results:");
            Console.WriteLine($"Initial Memory: {initialMemory / (1024.0 * 1024.0):F2}MB");
            Console.WriteLine($"Final Memory: {finalMemory / (1024.0 * 1024.0):F2}MB");
            Console.WriteLine($"Memory Used: {memoryUsedMB:F2}MB");
            Console.WriteLine($"Memory Per Log Operation: {(memoryUsedBytes / (double)LoggingTestIterations):F2} bytes");
            Console.WriteLine($"Net Memory Usage (from monitor): {stats.NetMemoryUsage / (1024.0 * 1024.0):F2}MB");
            
            // Assert memory usage is within acceptable limits
            Assert.IsTrue(memoryUsedMB < MaxAcceptableMemoryOverheadMB,
                $"Memory usage ({memoryUsedMB:F2}MB) exceeds maximum acceptable overhead ({MaxAcceptableMemoryOverheadMB}MB)");
            
            // Cleanup
            logService.Dispose();
        }

        /// <summary>
        /// Tests the performance impact of method interception
        /// </summary>
        [TestMethod]
        public void TestMethodInterceptionPerformance()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Test method without interception
            var baselineStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                TestMethodWithoutLogging(i);
            }
            baselineStopwatch.Stop();
            
            // Test method with manual logging (simulating interception)
            var interceptedStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                TestMethodWithLogging(logService, i);
            }
            interceptedStopwatch.Stop();
            
            // Wait for async processing
            Thread.Sleep(1000);
            
            // Calculate performance impact
            double baselineTimeMs = baselineStopwatch.Elapsed.TotalMilliseconds;
            double interceptedTimeMs = interceptedStopwatch.Elapsed.TotalMilliseconds;
            double overheadMs = interceptedTimeMs - baselineTimeMs;
            double overheadPercentage = (overheadMs / baselineTimeMs) * 100;
            double overheadPerCallMs = overheadMs / LoggingTestIterations;
            
            // Output results
            Console.WriteLine($"Method Interception Performance Test Results:");
            Console.WriteLine($"Baseline Time: {baselineTimeMs:F2}ms");
            Console.WriteLine($"Intercepted Time: {interceptedTimeMs:F2}ms");
            Console.WriteLine($"Overhead: {overheadMs:F2}ms ({overheadPercentage:F1}%)");
            Console.WriteLine($"Overhead Per Call: {overheadPerCallMs:F4}ms");
            
            // Assert that method interception overhead is minimal (less than 50% overhead)
            Assert.IsTrue(overheadPercentage < 50.0,
                $"Method interception overhead ({overheadPercentage:F1}%) is too high");
            
            Assert.IsTrue(overheadPerCallMs < MaxAcceptableLoggingOverheadMs,
                $"Method interception overhead per call ({overheadPerCallMs:F4}ms) exceeds maximum acceptable overhead ({MaxAcceptableLoggingOverheadMs}ms)");
            
            // Cleanup
            logService.Dispose();
        }

        /// <summary>
        /// Tests async logging performance vs synchronous logging
        /// </summary>
        [TestMethod]
        public void TestAsyncVsSyncLoggingPerformance()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            
            // Test synchronous logging (simulated by direct log4net calls)
            var syncStopwatch = Stopwatch.StartNew();
            var syncLogger = log4net.LogManager.GetLogger(typeof(PerformanceTests));
            
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                syncLogger.InfoFormat("Sync log message {0} with data {1}", i, $"data_{i}");
            }
            syncStopwatch.Stop();
            
            // Test async logging
            var asyncLogService = new Log4NetService(performanceMonitor);
            var asyncStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                asyncLogService.LogInformation("Async log message {Index} with data {Data}", i, $"data_{i}");
            }
            asyncStopwatch.Stop();
            
            // Wait for async processing to complete
            Thread.Sleep(3000);
            
            // Calculate performance comparison
            double syncTimeMs = syncStopwatch.Elapsed.TotalMilliseconds;
            double asyncTimeMs = asyncStopwatch.Elapsed.TotalMilliseconds;
            double performanceGain = ((syncTimeMs - asyncTimeMs) / syncTimeMs) * 100;
            
            // Output results
            Console.WriteLine($"Async vs Sync Logging Performance Test Results:");
            Console.WriteLine($"Sync Logging Time: {syncTimeMs:F2}ms");
            Console.WriteLine($"Async Logging Time: {asyncTimeMs:F2}ms");
            Console.WriteLine($"Performance Gain: {performanceGain:F1}%");
            
            // Async logging should be faster or at least not significantly slower
            Assert.IsTrue(performanceGain > -20.0, // Allow up to 20% slower for async overhead
                $"Async logging performance is significantly worse than sync logging (gain: {performanceGain:F1}%)");
            
            // Cleanup
            asyncLogService.Dispose();
        }

        /// <summary>
        /// Tests batch processing performance
        /// </summary>
        [TestMethod]
        public void TestBatchProcessingPerformance()
        {
            var performanceMonitor = new LoggingPerformanceMonitor();
            var logService = new Log4NetService(performanceMonitor);
            
            // Generate a burst of log messages to test batching
            var burstStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < LoggingTestIterations; i++)
            {
                logService.LogInformation("Batch test message {Index} at {Timestamp}", i, DateTime.UtcNow);
                
                // Add some variety in log levels
                if (i % 10 == 0) logService.LogDebug("Debug message {Index}", i);
                if (i % 50 == 0) logService.LogWarning("Warning message {Index}", i);
                if (i % 100 == 0) logService.LogError("Error message {Index}", i);
            }
            
            burstStopwatch.Stop();
            
            // Wait for all batches to be processed
            Thread.Sleep(5000);
            
            // Get performance statistics
            var stats = performanceMonitor.GetStatistics();
            
            // Calculate metrics
            double burstTimeMs = burstStopwatch.Elapsed.TotalMilliseconds;
            double averageBatchProcessingTimeMs = stats.AverageBatchProcessingTime.TotalMilliseconds;
            
            // Output results
            Console.WriteLine($"Batch Processing Performance Test Results:");
            Console.WriteLine($"Burst Generation Time: {burstTimeMs:F2}ms");
            Console.WriteLine($"Total Batches Processed: {stats.TotalBatchesProcessed}");
            Console.WriteLine($"Average Batch Size: {stats.AverageBatchSize:F1}");
            Console.WriteLine($"Average Batch Processing Time: {averageBatchProcessingTimeMs:F2}ms");
            Console.WriteLine($"Total Log Entries Processed: {stats.TotalLogEntriesProcessed}");
            
            // Assert that batching is working effectively
            Assert.IsTrue(stats.TotalBatchesProcessed > 0, "No batches were processed");
            Assert.IsTrue(stats.AverageBatchSize > 1, "Batching is not effective (average batch size should be > 1)");
            Assert.IsTrue(averageBatchProcessingTimeMs < 100, "Batch processing time is too high");
            
            // Cleanup
            logService.Dispose();
        }

        // Helper methods for testing
        private int TestMethodWithoutLogging(int input)
        {
            // Simulate some work
            int result = input * 2;
            for (int i = 0; i < 10; i++)
            {
                result += i;
            }
            return result;
        }

        private int TestMethodWithLogging(ILogService logService, int input)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Log method entry
            logService.LogMethodEntry(nameof(TestMethodWithLogging), input);
            
            // Simulate some work
            int result = input * 2;
            for (int i = 0; i < 10; i++)
            {
                result += i;
            }
            
            stopwatch.Stop();
            
            // Log method exit
            logService.LogMethodExit(nameof(TestMethodWithLogging), result, stopwatch.Elapsed);
            
            return result;
        }
    }
}