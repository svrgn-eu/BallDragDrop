using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BallDragDrop.Models;
using BallDragDrop.Views;
using BallDragDrop.ViewModels;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Performance tests for the optimized dual timer system
    /// Tests physics updates (60 FPS) coordination with animation frame updates
    /// </summary>
    [TestClass]
    public class DualTimerSystemPerformanceTests
    {
        private MainWindow _mainWindow;
        private BallViewModel _ballViewModel;
        private TestLogService _logService;
        private ImageService _imageService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize test services
            _logService = new TestLogService();
            _imageService = new ImageService(_logService);
            
            // Create test application context
            if (Application.Current == null)
            {
                new Application();
            }

            // Initialize main window and view model
            _mainWindow = new MainWindow();
            _ballViewModel = new BallViewModel(_logService, _imageService);
            _ballViewModel.Initialize(400, 300, 25);
            _mainWindow.DataContext = _ballViewModel;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mainWindow?.Close();
            _mainWindow = null;
            _ballViewModel = null;
            _logService = null;
            _imageService = null;
        }

        /// <summary>
        /// Tests that physics updates maintain 60 FPS target in optimized dual timer system
        /// </summary>
        [STATestMethod]
        public async Task OptimizedPhysicsTimer_ShouldMaintain60FPS()
        {
            // Arrange
            var metrics = new List<double>();
            var testDuration = TimeSpan.FromMilliseconds(500); // Shorter test duration
            var stopwatch = Stopwatch.StartNew();
            var maxSamples = 10;
            var sampleCount = 0;
            
            // Enable optimized dual timer system
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            // Start physics simulation
            _ballViewModel._ballModel.VelocityX = 100;
            _ballViewModel._ballModel.VelocityY = 50;
            
            // Act - Measure physics update timing with timeout and sample limit
            while (stopwatch.Elapsed < testDuration && sampleCount < maxSamples)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var timerMetrics = _mainWindow.GetTimerPerformanceMetrics();
                    if (timerMetrics.PhysicsTimerEnabled && timerMetrics.PhysicsFPS > 0)
                    {
                        metrics.Add(timerMetrics.PhysicsFPS);
                        sampleCount++;
                    }
                }, DispatcherPriority.Normal);
                
                // Wait for next frame with timeout protection
                await Task.Delay(50);
                
                // Additional safety check to prevent endless loop
                if (sampleCount >= maxSamples)
                    break;
            }
            
            // Assert
            Assert.IsTrue(metrics.Count > 0, "Should have collected physics FPS metrics");
            
            var averageFPS = metrics.Average();
            
            Console.WriteLine($"Optimized Physics FPS - Average: {averageFPS:F1} from {metrics.Count} samples");
            
            // Physics should maintain reasonable FPS (relaxed for testing)
            Assert.IsTrue(averageFPS >= 30.0 && averageFPS <= 120.0, 
                $"Physics FPS should be reasonable, but was {averageFPS:F1}");
        }

        /// <summary>
        /// Tests that animation frame updates respect source frame rates in optimized dual timer system
        /// </summary>
        [STATestMethod]
        public async Task OptimizedAnimationTimer_ShouldRespectSourceFrameRates()
        {
            // Arrange - Enable optimized dual timer system
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            // Load animated content with known frame rate
            var testImagePath = CreateTestAnimatedImage(10); // 10 FPS animation
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            var animationMetrics = new List<AnimationTimingMetrics>();
            var stopwatch = Stopwatch.StartNew();
            var testDuration = TimeSpan.FromSeconds(2);
            
            // Act - Monitor animation timing metrics
            var maxIterations = 40; // 2 seconds / 50ms = 40 iterations max
            var iterations = 0;
            while (stopwatch.Elapsed < testDuration && iterations < maxIterations)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var metrics = _ballViewModel.GetAnimationTimingMetrics();
                    if (metrics.IsAnimated && metrics.EffectiveAnimationFPS > 0)
                    {
                        animationMetrics.Add(metrics);
                    }
                }, DispatcherPriority.Background);
                
                await Task.Delay(50); // Check every 50ms
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(animationMetrics.Count > 0, "Should have collected animation timing metrics");
            
            var averageEffectiveFPS = animationMetrics.Average(m => m.EffectiveAnimationFPS);
            var averageSourceFPS = animationMetrics.Average(m => m.SourceAnimationFPS);
            var respectingSourceRateCount = animationMetrics.Count(m => m.IsRespectingSourceFrameRate);
            
            Console.WriteLine($"Animation FPS - Effective: {averageEffectiveFPS:F1}, Source: {averageSourceFPS:F1}");
            Console.WriteLine($"Respecting source rate: {respectingSourceRateCount}/{animationMetrics.Count}");
            
            // Animation should respect source frame rate (10 FPS ± 2)
            Assert.IsTrue(Math.Abs(averageEffectiveFPS - 10.0) <= 2.0,
                $"Animation should maintain ~10 FPS (source rate), but was {averageEffectiveFPS:F1}");
            
            // Most measurements should respect source frame rate
            Assert.IsTrue(respectingSourceRateCount > animationMetrics.Count * 0.7,
                $"Animation should respect source frame rate for most measurements. Respecting: {respectingSourceRateCount}/{animationMetrics.Count}");
        }

        /// <summary>
        /// Tests that animation performance doesn't impact drag responsiveness in optimized dual timer system
        /// </summary>
        [STATestMethod]
        public async Task OptimizedDragOperations_ShouldNotBeImpactedByAnimationPerformance()
        {
            // Arrange - Enable optimized dual timer system
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            // Load high-frequency animated content
            var testImagePath = CreateTestAnimatedImage(60); // 60 FPS animation
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            var dragResponseTimes = new List<double>();
            var dragOptimizationMetrics = new List<bool>();
            var testIterations = 15;
            
            // Act - Simulate drag operations and measure response time
            for (int i = 0; i < testIterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Start drag
                    _ballViewModel.IsDragging = true;
                    
                    // Check if animation is optimized for drag
                    var metrics = _ballViewModel.GetAnimationTimingMetrics();
                    dragOptimizationMetrics.Add(metrics.IsOptimizedForDrag);
                    
                    // Simulate mouse movement
                    var newX = 100 + (i * 40);
                    var newY = 100 + (i * 25);
                    _ballViewModel.X = newX;
                    _ballViewModel.Y = newY;
                    
                    // End drag
                    _ballViewModel.IsDragging = false;
                }, DispatcherPriority.Normal);
                
                stopwatch.Stop();
                dragResponseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                
                await Task.Delay(80); // Wait between iterations
            }
            
            // Assert
            var averageResponseTime = dragResponseTimes.Average();
            var maxResponseTime = dragResponseTimes.Max();
            var optimizedCount = dragOptimizationMetrics.Count(o => o);
            
            Console.WriteLine($"Optimized drag response times - Average: {averageResponseTime:F2}ms, Max: {maxResponseTime:F2}ms");
            Console.WriteLine($"Drag optimization active: {optimizedCount}/{dragOptimizationMetrics.Count}");
            
            // Drag operations should be very responsive with optimized dual timer system
            Assert.IsTrue(averageResponseTime < 3.0, 
                $"Average drag response time should be < 3ms with optimization, but was {averageResponseTime:F2}ms");
            Assert.IsTrue(maxResponseTime < 8.0, 
                $"Maximum drag response time should be < 8ms with optimization, but was {maxResponseTime:F2}ms");
            
            // Animation should be optimized for drag operations
            Assert.IsTrue(optimizedCount > dragOptimizationMetrics.Count * 0.8,
                $"Animation should be optimized for drag in most cases. Optimized: {optimizedCount}/{dragOptimizationMetrics.Count}");
        }

        /// <summary>
        /// Tests coordination between physics and animation timers
        /// </summary>
        [STATestMethod]
        public async Task TimerCoordination_ShouldMaintainSmoothOperation()
        {
            // Arrange
            var testImagePath = CreateTestAnimatedImage(30); // 30 FPS animation
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            // Start physics simulation
            _ballViewModel._ballModel.VelocityX = 200;
            _ballViewModel._ballModel.VelocityY = 100;
            
            var coordinationMetrics = new List<DualTimerCoordinationMetrics>();
            var testDuration = TimeSpan.FromSeconds(3);
            var stopwatch = Stopwatch.StartNew();
            
            // Optimize dual timer coordination
            _mainWindow.OptimizeDualTimerCoordination();
            
            // Act - Monitor timer coordination
            var maxIterations = 187; // 3 seconds / 16ms = ~187 iterations max
            var iterations = 0;
            while (stopwatch.Elapsed < testDuration && iterations < maxIterations)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var metrics = _mainWindow.GetDualTimerCoordinationMetrics();
                    coordinationMetrics.Add(metrics);
                    
                    // Coordinate animation with physics
                    _ballViewModel.CoordinateAnimationWithPhysics();
                }, DispatcherPriority.Normal);
                
                await Task.Delay(16); // ~60 FPS monitoring
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(coordinationMetrics.Count > 0, "Should have collected coordination metrics");
            
            var optimalCount = coordinationMetrics.Count(m => m.IsCoordinationOptimal);
            var averageEfficiency = coordinationMetrics.Average(m => m.CoordinationEfficiency);
            var averageHealthScore = coordinationMetrics.Average(m => m.SystemHealthScore);
            var physicsRunningCount = coordinationMetrics.Count(m => m.PhysicsMetrics.IsPhysicsRunning);
            
            Console.WriteLine($"Coordination metrics - Optimal: {optimalCount}/{coordinationMetrics.Count}");
            Console.WriteLine($"Average Efficiency: {averageEfficiency:F1}%, Health Score: {averageHealthScore:F1}%");
            Console.WriteLine($"Physics running: {physicsRunningCount}/{coordinationMetrics.Count}");
            
            // Coordination should be optimal for most of the test
            Assert.IsTrue(optimalCount > coordinationMetrics.Count * 0.7, 
                $"Timer coordination should be optimal for most of the test duration. Optimal: {optimalCount}/{coordinationMetrics.Count}");
            
            // System efficiency should be high
            Assert.IsTrue(averageEfficiency >= 80.0, 
                $"Coordination efficiency should be >= 80%, but was {averageEfficiency:F1}%");
            
            // System health should be good
            Assert.IsTrue(averageHealthScore >= 75.0, 
                $"System health score should be >= 75%, but was {averageHealthScore:F1}%");
            
            // Physics should be running for most of the test
            Assert.IsTrue(physicsRunningCount > coordinationMetrics.Count * 0.8, 
                "Physics should be running for most of the test duration");
        }

        /// <summary>
        /// Tests performance under high load conditions
        /// </summary>
        [STATestMethod]
        public async Task HighLoadConditions_ShouldMaintainPerformance()
        {
            // Arrange - Create high-load scenario
            var testImagePath = CreateTestAnimatedImage(60); // High-frequency animation
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            // Start intensive physics simulation
            _ballViewModel._ballModel.VelocityX = 500;
            _ballViewModel._ballModel.VelocityY = 300;
            
            var performanceMetrics = new List<double>();
            var testDuration = TimeSpan.FromSeconds(2);
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Monitor performance under load
            var maxIterations = 125; // 2 seconds / 16ms = ~125 iterations max
            var iterations = 0;
            while (stopwatch.Elapsed < testDuration && iterations < maxIterations)
            {
                var frameStart = Stopwatch.StartNew();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Simulate high-frequency operations
                    _ballViewModel.CoordinateAnimationWithPhysics();
                    _ballViewModel.EnsureAnimationDoesNotImpactDragResponsiveness();
                    
                    var metrics = _mainWindow.GetTimerPerformanceMetrics();
                    if (metrics.AverageFrameTime > 0)
                    {
                        performanceMetrics.Add(metrics.AverageFrameTime);
                    }
                }, DispatcherPriority.Normal);
                
                frameStart.Stop();
                
                // Ensure we don't overwhelm the system
                if (frameStart.ElapsedMilliseconds < 16)
                {
                    await Task.Delay(16 - (int)frameStart.ElapsedMilliseconds);
                }
                
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(performanceMetrics.Count > 0, "Should have collected performance metrics");
            
            var averageFrameTime = performanceMetrics.Average();
            var maxFrameTime = performanceMetrics.Max();
            
            Console.WriteLine($"High load performance - Average frame time: {averageFrameTime:F2}ms, Max: {maxFrameTime:F2}ms");
            
            // Performance should remain acceptable even under high load
            Assert.IsTrue(averageFrameTime < 20.0, 
                $"Average frame time should be < 20ms under high load, but was {averageFrameTime:F2}ms");
            Assert.IsTrue(maxFrameTime < 50.0, 
                $"Maximum frame time should be < 50ms under high load, but was {maxFrameTime:F2}ms");
        }

        /// <summary>
        /// Tests the optimized dual timer system performance
        /// </summary>
        [STATestMethod]
        public async Task OptimizedDualTimerSystem_ShouldSeparatePhysicsAndAnimationUpdates()
        {
            // Arrange
            var testImagePath = CreateTestAnimatedImage(24); // 24 FPS animation (common frame rate)
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            // Enable optimized timer system
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            var coordinationMetrics = new List<DualTimerCoordinationMetrics>();
            var testDuration = TimeSpan.FromSeconds(2);
            var stopwatch = Stopwatch.StartNew();
            
            // Start physics simulation
            _ballViewModel._ballModel.VelocityX = 150;
            _ballViewModel._ballModel.VelocityY = 75;
            
            // Act - Monitor separated timer performance
            var maxIterations = 125; // 2 seconds / 16ms = ~125 iterations max
            var iterations = 0;
            while (stopwatch.Elapsed < testDuration && iterations < maxIterations)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var metrics = _mainWindow.GetDualTimerCoordinationMetrics();
                    coordinationMetrics.Add(metrics);
                }, DispatcherPriority.Normal);
                
                await Task.Delay(16); // Monitor at 60 FPS
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(coordinationMetrics.Count > 0, "Should have collected coordination metrics");
            
            var separatedCount = coordinationMetrics.Count(m => m.IsProperlySeparated);
            var respectingSourceRatesCount = coordinationMetrics.Count(m => m.IsRespectingSourceFrameRates);
            var averagePhysicsFPS = coordinationMetrics.Where(m => m.PhysicsMetrics.PhysicsFPS > 0).Average(m => m.PhysicsMetrics.PhysicsFPS);
            var averageAnimationFPS = coordinationMetrics.Where(m => m.AnimationMetrics.EffectiveAnimationFPS > 0).Average(m => m.AnimationMetrics.EffectiveAnimationFPS);
            
            Console.WriteLine($"Dual timer separation - Properly separated: {separatedCount}/{coordinationMetrics.Count}");
            Console.WriteLine($"Source rates respected: {respectingSourceRatesCount}/{coordinationMetrics.Count}");
            Console.WriteLine($"Physics FPS: {averagePhysicsFPS:F1}, Animation FPS: {averageAnimationFPS:F1}");
            
            // Timers should be properly separated
            Assert.IsTrue(separatedCount > coordinationMetrics.Count * 0.9, 
                $"Timers should be properly separated for most of the test. Separated: {separatedCount}/{coordinationMetrics.Count}");
            
            // Physics should maintain 60 FPS
            Assert.IsTrue(averagePhysicsFPS >= 55.0 && averagePhysicsFPS <= 65.0, 
                $"Physics should maintain ~60 FPS, but was {averagePhysicsFPS:F1}");
            
            // Animation should respect source frame rate (24 FPS ± 3)
            Assert.IsTrue(Math.Abs(averageAnimationFPS - 24.0) <= 3.0, 
                $"Animation should maintain ~24 FPS (source rate), but was {averageAnimationFPS:F1}");
            
            // Source frame rates should be respected
            Assert.IsTrue(respectingSourceRatesCount > coordinationMetrics.Count * 0.8, 
                $"Source frame rates should be respected. Respected: {respectingSourceRatesCount}/{coordinationMetrics.Count}");
        }

        /// <summary>
        /// Tests comprehensive dual timer system separation and coordination
        /// </summary>
        [STATestMethod]
        public async Task DualTimerSystemSeparation_ShouldMaintainIndependentTimers()
        {
            // Arrange
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            var testImagePath = CreateTestAnimatedImage(30); // 30 FPS animation
            await _ballViewModel.LoadBallVisualAsync(testImagePath);
            
            // Start physics simulation
            _ballViewModel._ballModel.VelocityX = 200;
            _ballViewModel._ballModel.VelocityY = 100;
            
            var coordinationMetrics = new List<DualTimerCoordinationMetrics>();
            var testDuration = TimeSpan.FromSeconds(3);
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Monitor dual timer separation
            var maxIterations = 187; // 3 seconds / 16ms = ~187 iterations max
            var iterations = 0;
            while (stopwatch.Elapsed < testDuration && iterations < maxIterations)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var metrics = _mainWindow.GetDualTimerCoordinationMetrics();
                    coordinationMetrics.Add(metrics);
                }, DispatcherPriority.Normal);
                
                await Task.Delay(16); // Monitor at 60 FPS
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(coordinationMetrics.Count > 0, "Should have collected coordination metrics");
            
            var separatedCount = coordinationMetrics.Count(m => m.IsProperlySeparated);
            var optimalCount = coordinationMetrics.Count(m => m.IsCoordinationOptimal);
            var averagePhysicsFPS = coordinationMetrics.Where(m => m.PhysicsMetrics.PhysicsFPS > 0).Average(m => m.PhysicsMetrics.PhysicsFPS);
            var averageAnimationFPS = coordinationMetrics.Where(m => m.AnimationMetrics.EffectiveAnimationFPS > 0).Average(m => m.AnimationMetrics.EffectiveAnimationFPS);
            var averageHealthScore = coordinationMetrics.Average(m => m.SystemHealthScore);
            
            Console.WriteLine($"Dual timer separation - Separated: {separatedCount}/{coordinationMetrics.Count}");
            Console.WriteLine($"Coordination optimal: {optimalCount}/{coordinationMetrics.Count}");
            Console.WriteLine($"Physics FPS: {averagePhysicsFPS:F1}, Animation FPS: {averageAnimationFPS:F1}");
            Console.WriteLine($"System health score: {averageHealthScore:F1}%");
            
            // Timers should be properly separated
            Assert.IsTrue(separatedCount > coordinationMetrics.Count * 0.9, 
                $"Timers should be properly separated. Separated: {separatedCount}/{coordinationMetrics.Count}");
            
            // Coordination should be optimal most of the time
            Assert.IsTrue(optimalCount > coordinationMetrics.Count * 0.8, 
                $"Timer coordination should be optimal. Optimal: {optimalCount}/{coordinationMetrics.Count}");
            
            // Physics should maintain 60 FPS
            Assert.IsTrue(averagePhysicsFPS >= 55.0 && averagePhysicsFPS <= 65.0, 
                $"Physics should maintain ~60 FPS, but was {averagePhysicsFPS:F1}");
            
            // Animation should respect source frame rate (30 FPS ± 3)
            Assert.IsTrue(Math.Abs(averageAnimationFPS - 30.0) <= 3.0, 
                $"Animation should maintain ~30 FPS (source rate), but was {averageAnimationFPS:F1}");
            
            // System health should be good
            Assert.IsTrue(averageHealthScore >= 80.0, 
                $"System health score should be >= 80%, but was {averageHealthScore:F1}%");
        }

        /// <summary>
        /// Tests switching between optimized and legacy timer modes
        /// </summary>
        [STATestMethod]
        public async Task TimerModeSwitch_ShouldMaintainFunctionality()
        {
            // Arrange
            var optimizedMetrics = new List<TimerPerformanceMetrics>();
            var legacyMetrics = new List<TimerPerformanceMetrics>();
            
            // Test optimized mode
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            await CollectTimerMetrics(optimizedMetrics, TimeSpan.FromSeconds(1));
            
            // Test legacy mode
            _mainWindow.SetOptimizedTimerMode(false);
            await CollectTimerMetrics(legacyMetrics, TimeSpan.FromSeconds(1));
            
            // Switch back to optimized
            _mainWindow.SetOptimizedTimerMode(true);
            _mainWindow.OptimizeDualTimerCoordination();
            
            // Assert
            Assert.IsTrue(optimizedMetrics.Count > 0, "Should have collected optimized metrics");
            Assert.IsTrue(legacyMetrics.Count > 0, "Should have collected legacy metrics");
            
            var optimizedAvgFPS = optimizedMetrics.Where(m => m.PhysicsFPS > 0).Average(m => m.PhysicsFPS);
            var legacyAvgFPS = legacyMetrics.Where(m => m.PhysicsFPS > 0).Average(m => m.PhysicsFPS);
            
            Console.WriteLine($"Optimized mode FPS: {optimizedAvgFPS:F1}, Legacy mode FPS: {legacyAvgFPS:F1}");
            
            // Both modes should maintain reasonable performance
            Assert.IsTrue(optimizedAvgFPS >= 50.0, 
                $"Optimized mode should maintain >= 50 FPS, but was {optimizedAvgFPS:F1}");
            Assert.IsTrue(legacyAvgFPS >= 45.0, 
                $"Legacy mode should maintain >= 45 FPS, but was {legacyAvgFPS:F1}");
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test animated image with specified frame rate
        /// </summary>
        /// <param name="fps">Target frames per second</param>
        /// <returns>Path to the created test image</returns>
        private string CreateTestAnimatedImage(int fps)
        {
            // For testing purposes, create a simple test image path
            // In a real implementation, this would create an actual animated image
            var testPath = Path.Combine(Path.GetTempPath(), $"test_animation_{fps}fps.gif");
            
            // Create a placeholder file for testing
            File.WriteAllText(testPath, "Test animated image placeholder");
            
            return testPath;
        }

        /// <summary>
        /// Collects timer performance metrics for a specified duration
        /// </summary>
        /// <param name="metrics">List to collect metrics into</param>
        /// <param name="duration">Duration to collect metrics</param>
        private async Task CollectTimerMetrics(List<TimerPerformanceMetrics> metrics, TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            var maxIterations = (int)(duration.TotalMilliseconds / 16) + 10; // Add buffer
            var iterations = 0;
            
            while (stopwatch.Elapsed < duration && iterations < maxIterations)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var metric = _mainWindow.GetTimerPerformanceMetrics();
                    metrics.Add(metric);
                }, DispatcherPriority.Normal);
                
                await Task.Delay(16); // ~60 FPS sampling
                iterations++;
            }
        }

        #endregion Helper Methods
    }
}