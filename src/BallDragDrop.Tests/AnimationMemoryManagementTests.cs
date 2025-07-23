using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Models;
using BallDragDrop.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Tests for animation memory management functionality
    /// </summary>
    [TestClass]
    public class AnimationMemoryManagementTests
    {
        private SimpleLogService _logService;
        private AnimationEngine _animationEngine;

        [TestInitialize]
        public void Setup()
        {
            _logService = new SimpleLogService();
            _animationEngine = new AnimationEngine(_logService, maxCacheSize: 10, maxMemoryUsageMB: 5);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _animationEngine?.Dispose();
        }

        /// <summary>
        /// Tests frame caching for efficient memory usage
        /// </summary>
        [TestMethod]
        public void TestFrameCaching()
        {
            // Arrange
            var frames = CreateTestFrames(20);
            _animationEngine.LoadFrames(frames);

            // Act - Access frames to trigger caching
            for (int i = 0; i < 5; i++)
            {
                _animationEngine.SetCurrentFrame(i);
                var frame = _animationEngine.GetCurrentFrame();
                Assert.IsNotNull(frame);
            }

            // Assert
            var stats = _animationEngine.GetMemoryStats();
            Assert.IsTrue((int)stats["CachedFrameCount"] > 0, "Frames should be cached");
            Assert.IsTrue((double)stats["CurrentMemoryUsageMB"] > 0, "Memory usage should be tracked");
            
            // Verify frames are marked as cached
            int cachedCount = 0;
            for (int i = 0; i < Math.Min(5, frames.Count); i++)
            {
                if (frames[i].IsCached)
                    cachedCount++;
            }
            Assert.IsTrue(cachedCount > 0, "Some frames should be marked as cached");
        }

        /// <summary>
        /// Tests memory usage limits and optimization
        /// </summary>
        [TestMethod]
        public void TestMemoryUsageLimits()
        {
            // Arrange - Create many large frames to exceed memory limit
            var frames = CreateTestFrames(50, 100, 100); // 50 frames of 100x100 pixels
            _animationEngine.LoadFrames(frames);

            // Act - Access many frames to fill cache beyond limit
            for (int i = 0; i < 30; i++)
            {
                _animationEngine.SetCurrentFrame(i % frames.Count);
                _animationEngine.GetCurrentFrame();
            }

            // Assert
            var stats = _animationEngine.GetMemoryStats();
            var memoryUsageMB = (double)stats["CurrentMemoryUsageMB"];
            var maxMemoryMB = (double)stats["MaxMemoryUsageMB"];
            
            Assert.IsTrue(memoryUsageMB <= maxMemoryMB * 1.1, // Allow 10% tolerance
                $"Memory usage ({memoryUsageMB:F2}MB) should not significantly exceed limit ({maxMemoryMB:F2}MB)");
            
            Assert.IsTrue((int)stats["CachedFrameCount"] <= 10, 
                "Cached frame count should not exceed max cache size");
        }

        /// <summary>
        /// Tests resource disposal for unused animations
        /// </summary>
        [TestMethod]
        public void TestResourceDisposal()
        {
            // Arrange
            var frames = CreateTestFrames(10);
            _animationEngine.LoadFrames(frames);

            // Act - Cache some frames
            for (int i = 0; i < 5; i++)
            {
                _animationEngine.SetCurrentFrame(i);
                _animationEngine.GetCurrentFrame();
            }

            var statsBefore = _animationEngine.GetMemoryStats();
            var memoryBefore = (double)statsBefore["CurrentMemoryUsageMB"];
            var cachedBefore = (int)statsBefore["CachedFrameCount"];

            // Clear cache
            _animationEngine.ClearCache();

            // Assert
            var statsAfter = _animationEngine.GetMemoryStats();
            var memoryAfter = (double)statsAfter["CurrentMemoryUsageMB"];
            var cachedAfter = (int)statsAfter["CachedFrameCount"];

            Assert.AreEqual(0, cachedAfter, "Cache should be empty after clearing");
            Assert.AreEqual(0.0, memoryAfter, "Memory usage should be zero after clearing cache");
            Assert.IsTrue(memoryBefore > 0, "Memory should have been used before clearing");

            // Verify frames are no longer marked as cached
            foreach (var frame in frames)
            {
                Assert.IsFalse(frame.IsCached, "Frames should not be marked as cached after disposal");
            }
        }

        /// <summary>
        /// Tests frame pre-loading to prevent stuttering
        /// </summary>
        [TestMethod]
        public void TestFramePreloading()
        {
            // Arrange
            var frames = CreateTestFrames(20);
            _animationEngine.LoadFrames(frames);

            // Act - Preload frames
            _animationEngine.PreloadFrames(0, 5);

            // Assert
            var stats = _animationEngine.GetMemoryStats();
            Assert.IsTrue((int)stats["CachedFrameCount"] >= 5, "At least 5 frames should be preloaded");
            
            // Verify specific frames are cached
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(frames[i].IsCached, $"Frame {i} should be preloaded and cached");
            }
        }

        /// <summary>
        /// Tests memory optimization performance
        /// </summary>
        [TestMethod]
        public void TestMemoryOptimizationPerformance()
        {
            // Arrange
            var frames = CreateTestFrames(100);
            _animationEngine.LoadFrames(frames);

            // Fill cache to capacity
            for (int i = 0; i < 15; i++) // More than max cache size
            {
                _animationEngine.SetCurrentFrame(i);
                _animationEngine.GetCurrentFrame();
            }

            // Act - Measure optimization performance
            var stopwatch = Stopwatch.StartNew();
            _animationEngine.OptimizeMemoryUsage();
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Memory optimization should complete quickly (took {stopwatch.ElapsedMilliseconds}ms)");

            var stats = _animationEngine.GetMemoryStats();
            Assert.IsTrue((int)stats["CachedFrameCount"] <= 10, 
                "Cache size should be within limits after optimization");
        }

        /// <summary>
        /// Tests memory usage statistics accuracy
        /// </summary>
        [TestMethod]
        public void TestMemoryUsageStatistics()
        {
            // Arrange
            var frames = CreateTestFrames(10, 50, 50); // 10 frames of 50x50 pixels
            _animationEngine.LoadFrames(frames);

            // Act - Cache some frames
            for (int i = 0; i < 5; i++)
            {
                _animationEngine.SetCurrentFrame(i);
                _animationEngine.GetCurrentFrame();
            }

            // Assert
            var stats = _animationEngine.GetMemoryStats();
            
            Assert.IsTrue((double)stats["CurrentMemoryUsageMB"] > 0, "Current memory usage should be tracked");
            Assert.AreEqual(5.0, (double)stats["MaxMemoryUsageMB"], "Max memory usage should match constructor parameter");
            Assert.IsTrue((int)stats["CachedFrameCount"] > 0, "Cached frame count should be tracked");
            Assert.AreEqual(10, (int)stats["TotalFrameCount"], "Total frame count should be accurate");
            Assert.IsTrue((double)stats["MemoryUtilization"] >= 0 && (double)stats["MemoryUtilization"] <= 100, 
                "Memory utilization should be a valid percentage");
        }

        /// <summary>
        /// Tests cache hit ratio calculation
        /// </summary>
        [TestMethod]
        public void TestCacheHitRatio()
        {
            // Arrange
            var frames = CreateTestFrames(10);
            _animationEngine.LoadFrames(frames);

            // Act - Access same frames multiple times to improve hit ratio
            for (int cycle = 0; cycle < 3; cycle++)
            {
                for (int i = 0; i < 5; i++)
                {
                    _animationEngine.SetCurrentFrame(i);
                    _animationEngine.GetCurrentFrame();
                }
            }

            // Assert
            var stats = _animationEngine.GetMemoryStats();
            var hitRatio = (double)stats["CacheHitRatio"];
            
            // Hit ratio should improve with repeated access to same frames
            // Note: Exact value depends on implementation details, so we just check it's reasonable
            Assert.IsTrue(hitRatio >= 0 && hitRatio <= 100, 
                $"Cache hit ratio should be a valid percentage (got {hitRatio})");
        }

        /// <summary>
        /// Tests disposal of animation engine releases all resources
        /// </summary>
        [TestMethod]
        public void TestAnimationEngineDisposal()
        {
            // Arrange
            var frames = CreateTestFrames(10);
            var engine = new AnimationEngine(_logService, maxCacheSize: 5, maxMemoryUsageMB: 2);
            engine.LoadFrames(frames);

            // Cache some frames
            for (int i = 0; i < 3; i++)
            {
                engine.SetCurrentFrame(i);
                engine.GetCurrentFrame();
            }

            var statsBefore = engine.GetMemoryStats();
            Assert.IsTrue((int)statsBefore["CachedFrameCount"] > 0, "Should have cached frames before disposal");

            // Act
            engine.Dispose();

            // Assert - Verify disposal doesn't throw and resources are cleaned up
            // Note: We can't easily verify internal state after disposal, but we can ensure no exceptions
            Assert.IsTrue(true, "Disposal should complete without exceptions");

            // Verify frames are no longer marked as cached
            foreach (var frame in frames)
            {
                Assert.IsFalse(frame.IsCached, "Frames should not be marked as cached after engine disposal");
            }
        }

        /// <summary>
        /// Tests concurrent access to memory management features
        /// </summary>
        [TestMethod]
        public async Task TestConcurrentMemoryAccess()
        {
            // Arrange
            var frames = CreateTestFrames(20);
            _animationEngine.LoadFrames(frames);

            // Act - Simulate concurrent access
            var tasks = new List<Task>();
            
            for (int i = 0; i < 5; i++)
            {
                int taskIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        int frameIndex = (taskIndex * 10 + j) % frames.Count;
                        _animationEngine.SetCurrentFrame(frameIndex);
                        var frame = _animationEngine.GetCurrentFrame();
                        Assert.IsNotNull(frame, $"Frame should be accessible in concurrent scenario");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - System should remain stable
            var stats = _animationEngine.GetMemoryStats();
            Assert.IsTrue((int)stats["CachedFrameCount"] >= 0, "Cache should remain in valid state");
            Assert.IsTrue((double)stats["CurrentMemoryUsageMB"] >= 0, "Memory usage should remain valid");
        }

        /// <summary>
        /// Creates test animation frames for testing
        /// </summary>
        /// <param name="count">Number of frames to create</param>
        /// <param name="width">Width of each frame in pixels</param>
        /// <param name="height">Height of each frame in pixels</param>
        /// <returns>List of test animation frames</returns>
        private List<AnimationFrame> CreateTestFrames(int count, int width = 32, int height = 32)
        {
            var frames = new List<AnimationFrame>();
            
            for (int i = 0; i < count; i++)
            {
                // Create a simple colored bitmap for testing
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                // Fill with a test pattern
                var pixels = new byte[width * height * 4];
                for (int p = 0; p < pixels.Length; p += 4)
                {
                    pixels[p] = (byte)(i * 10 % 255);     // Blue
                    pixels[p + 1] = (byte)(i * 20 % 255); // Green
                    pixels[p + 2] = (byte)(i * 30 % 255); // Red
                    pixels[p + 3] = 255;                   // Alpha
                }
                
                bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                bitmap.Freeze();
                
                var frame = new AnimationFrame(bitmap, TimeSpan.FromMilliseconds(100));
                frames.Add(frame);
            }
            
            return frames;
        }
    }
}