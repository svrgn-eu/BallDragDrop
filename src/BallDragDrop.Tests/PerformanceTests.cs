using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BallDragDrop.Models;
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
    }
}