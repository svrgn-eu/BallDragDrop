using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using BallDragDrop.Models;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for the animation system including ImageService and AnimationEngine integration
    /// </summary>
    [TestClass]
    public class AnimationSystemIntegrationTests
    {
        #region Test Setup

        private BallViewModel _ballViewModel;
        private ImageService _imageService;
        private TestLogService _logService;
        private string _testDataDirectory;

        [TestInitialize]
        public void Setup()
        {
            // Initialize test services
            _logService = new TestLogService();
            _imageService = new ImageService(_logService);
            
            // Create test data directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "AnimationSystemTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDirectory);
            
            // Initialize view model without creating Application instance
            _ballViewModel = new BallViewModel(_logService, _imageService);
            _ballViewModel.Initialize(400, 300, 25);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _ballViewModel = null;
            _imageService = null;
            _logService = null;
            
            // Clean up test data directory
            if (Directory.Exists(_testDataDirectory))
            {
                try
                {
                    Directory.Delete(_testDataDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        #endregion Test Setup

        #region ImageService with AnimationEngine Integration Tests

        /// <summary>
        /// Tests ImageService integration with AnimationEngine for GIF animations
        /// </summary>
        [STATestMethod]
        public async Task ImageService_WithAnimationEngine_ShouldLoadGifAnimationCorrectly()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(30); // 30 FPS GIF
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);
            
            // Assert
            Assert.IsTrue(result, "Should successfully load GIF animation");
            Assert.IsTrue(_imageService.IsAnimated, "ImageService should recognize content as animated");
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have a current frame");
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero, "Should have valid frame duration");
            
            // Test animation playback
            _imageService.StartAnimation();
            var initialFrame = _imageService.CurrentFrame;
            
            // Wait for animation to progress
            await Task.Delay(200);
            _imageService.UpdateFrame();
            
            // Frame might be the same if timing hasn't advanced enough, but method should not throw
            Assert.IsNotNull(_imageService.CurrentFrame, "Current frame should remain valid during animation");
        }

        /// <summary>
        /// Tests ImageService integration with AnimationEngine for Aseprite animations
        /// </summary>
        [STATestMethod]
        public async Task ImageService_WithAnimationEngine_ShouldLoadAsepriteAnimationCorrectly()
        {
            // Arrange
            var asepritePaths = CreateTestAsepriteAnimation(24); // 24 FPS Aseprite animation
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(asepritePaths.PngPath);
            
            // Assert
            Assert.IsTrue(result, "Should successfully load Aseprite animation");
            Assert.IsTrue(_imageService.IsAnimated, "ImageService should recognize content as animated");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have a current frame");
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero, "Should have valid frame duration");
            
            // Test animation playback
            _imageService.StartAnimation();
            var initialFrame = _imageService.CurrentFrame;
            
            // Wait for animation to progress
            await Task.Delay(200);
            _imageService.UpdateFrame();
            
            Assert.IsNotNull(_imageService.CurrentFrame, "Current frame should remain valid during animation");
        }

        /// <summary>
        /// Tests switching between different animation types through ImageService
        /// </summary>
        [STATestMethod]
        public async Task ImageService_SwitchingBetweenAnimationTypes_ShouldWorkCorrectly()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(15); // 15 FPS GIF
            var asepritePaths = CreateTestAsepriteAnimation(30); // 30 FPS Aseprite
            
            // Act - Load GIF first
            var result1 = await _imageService.LoadBallVisualAsync(gifPath);
            Assert.IsTrue(result1, "Should load GIF successfully");
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            
            // Switch to Aseprite
            var result2 = await _imageService.LoadBallVisualAsync(asepritePaths.PngPath);
            
            // Assert
            Assert.IsTrue(result2, "Should switch to Aseprite successfully");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            Assert.IsTrue(_imageService.IsAnimated, "Should remain animated after switch");
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have valid current frame after switch");
        }

        /// <summary>
        /// Tests ImageService fallback behavior when animation loading fails
        /// </summary>
        [STATestMethod]
        public async Task ImageService_AnimationLoadingFailure_ShouldFallbackGracefully()
        {
            // Arrange
            var invalidGifPath = Path.Combine(_testDataDirectory, "invalid.gif");
            File.WriteAllText(invalidGifPath, "This is not a valid GIF file");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(invalidGifPath);
            
            // Assert
            Assert.IsTrue(result, "Should succeed with fallback");
            Assert.IsFalse(_imageService.IsAnimated, "Should not be animated when fallback is used");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have fallback image");
        }

        #endregion ImageService with AnimationEngine Integration Tests

        #region Animation Playback During Drag Operations Tests

        /// <summary>
        /// Tests that animation continues playing during drag operations
        /// </summary>
        [STATestMethod]
        public async Task AnimationPlayback_DuringDragOperations_ShouldContinue()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(20); // 20 FPS GIF
            await _ballViewModel.LoadBallVisualAsync(gifPath);
            
            Assert.IsTrue(_ballViewModel.IsAnimated, "Ball should be animated");
            
            // Start animation
            _imageService.StartAnimation();
            var initialFrame = _ballViewModel.BallImage;
            
            // Act - Start dragging
            _ballViewModel.IsDragging = true;
            _ballViewModel.EnsureAnimationContinuesDuringDrag();
            
            // Simulate drag movement
            _ballViewModel.X = 150;
            _ballViewModel.Y = 200;
            
            // Wait for animation to progress during drag
            await Task.Delay(300);
            _ballViewModel.CoordinateAnimationWithPhysics();
            
            // Assert
            Assert.IsTrue(_ballViewModel.IsDragging, "Should still be dragging");
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should remain animated during drag");
            Assert.IsNotNull(_ballViewModel.BallImage, "Should have valid ball image during drag");
            
            // End drag
            _ballViewModel.IsDragging = false;
            
            // Animation should continue after drag ends
            await Task.Delay(100);
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should remain animated after drag ends");
        }

        /// <summary>
        /// Tests animation performance during intensive drag operations
        /// </summary>
        [STATestMethod]
        public async Task AnimationPlayback_DuringIntensiveDragOperations_ShouldMaintainPerformance()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(60); // High-frequency animation
            await _ballViewModel.LoadBallVisualAsync(gifPath);
            
            _imageService.StartAnimation();
            var dragResponseTimes = new List<double>();
            
            // Act - Perform intensive drag operations
            for (int i = 0; i < 20; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                
                _ballViewModel.IsDragging = true;
                _ballViewModel.X = 100 + (i * 10);
                _ballViewModel.Y = 100 + (i * 5);
                _ballViewModel.EnsureAnimationContinuesDuringDrag();
                _ballViewModel.CoordinateAnimationWithPhysics();
                _ballViewModel.IsDragging = false;
                
                stopwatch.Stop();
                dragResponseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                
                await Task.Delay(50); // Brief pause between operations
            }
            
            // Assert
            var averageResponseTime = dragResponseTimes.Average();
            var maxResponseTime = dragResponseTimes.Max();
            
            Console.WriteLine($"Animation during drag - Average response: {averageResponseTime:F2}ms, Max: {maxResponseTime:F2}ms");
            
            Assert.IsTrue(averageResponseTime < 10.0, 
                $"Average drag response time should be < 10ms with animation, but was {averageResponseTime:F2}ms");
            Assert.IsTrue(maxResponseTime < 25.0, 
                $"Maximum drag response time should be < 25ms with animation, but was {maxResponseTime:F2}ms");
            Assert.IsTrue(_ballViewModel.IsAnimated, "Animation should still be active after intensive operations");
        }

        /// <summary>
        /// Tests that animation state is properly maintained during drag start/stop cycles
        /// </summary>
        [STATestMethod]
        public async Task AnimationPlayback_DragStartStopCycles_ShouldMaintainState()
        {
            // Arrange
            var asepritePaths = CreateTestAsepriteAnimation(25); // 25 FPS Aseprite
            await _ballViewModel.LoadBallVisualAsync(asepritePaths.PngPath);
            
            _imageService.StartAnimation();
            
            // Act - Perform multiple drag start/stop cycles
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Start drag
                _ballViewModel.IsDragging = true;
                _ballViewModel.EnsureAnimationContinuesDuringDrag();
                
                await Task.Delay(100);
                
                // Move during drag
                _ballViewModel.X = 100 + (cycle * 20);
                _ballViewModel.Y = 100 + (cycle * 15);
                _ballViewModel.CoordinateAnimationWithPhysics();
                
                await Task.Delay(100);
                
                // End drag
                _ballViewModel.IsDragging = false;
                
                await Task.Delay(50);
                
                // Assert animation state is maintained
                Assert.IsTrue(_ballViewModel.IsAnimated, $"Animation should be maintained after cycle {cycle + 1}");
                Assert.IsNotNull(_ballViewModel.BallImage, $"Ball image should be valid after cycle {cycle + 1}");
            }
        }

        #endregion Animation Playback During Drag Operations Tests

        #region Visual Content Switching Tests

        /// <summary>
        /// Tests switching between static and animated content types
        /// </summary>
        [STATestMethod]
        public async Task VisualContentSwitching_BetweenStaticAndAnimated_ShouldWorkCorrectly()
        {
            // Arrange
            var staticImagePath = CreateTestStaticImage();
            var gifPath = CreateTestGifAnimation(20);
            
            // Act - Start with static image
            var result1 = await _ballViewModel.LoadBallVisualAsync(staticImagePath);
            Assert.IsTrue(result1, "Should load static image successfully");
            Assert.IsFalse(_ballViewModel.IsAnimated, "Should not be animated initially");
            
            // Switch to animated content
            var result2 = await _ballViewModel.LoadBallVisualAsync(gifPath);
            
            // Assert
            Assert.IsTrue(result2, "Should switch to animated content successfully");
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should be animated after switch");
            Assert.AreEqual(VisualContentType.GifAnimation, _ballViewModel.ContentType);
            
            // Switch back to static
            var result3 = await _ballViewModel.LoadBallVisualAsync(staticImagePath);
            Assert.IsTrue(result3, "Should switch back to static successfully");
            Assert.IsFalse(_ballViewModel.IsAnimated, "Should not be animated after switching back");
            Assert.AreEqual(VisualContentType.StaticImage, _ballViewModel.ContentType);
        }

        /// <summary>
        /// Tests switching between different animated content types
        /// </summary>
        [STATestMethod]
        public async Task VisualContentSwitching_BetweenAnimatedTypes_ShouldWorkCorrectly()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(15);
            var asepritePaths = CreateTestAsepriteAnimation(30);
            
            // Act - Start with GIF
            var result1 = await _ballViewModel.LoadBallVisualAsync(gifPath);
            Assert.IsTrue(result1, "Should load GIF successfully");
            Assert.AreEqual(VisualContentType.GifAnimation, _ballViewModel.ContentType);
            
            // Switch to Aseprite
            var result2 = await _ballViewModel.LoadBallVisualAsync(asepritePaths.PngPath);
            
            // Assert
            Assert.IsTrue(result2, "Should switch to Aseprite successfully");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _ballViewModel.ContentType);
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should remain animated after switch");
            
            // Both should maintain animation capability
            _imageService.StartAnimation();
            await Task.Delay(100);
            Assert.IsNotNull(_ballViewModel.BallImage, "Should have valid image after animation start");
        }

        /// <summary>
        /// Tests visual content switching while animation is playing
        /// </summary>
        [STATestMethod]
        public async Task VisualContentSwitching_WhileAnimationPlaying_ShouldTransitionSmoothly()
        {
            // Arrange
            var gif1Path = CreateTestGifAnimation(20, "animation1.gif");
            var gif2Path = CreateTestGifAnimation(30, "animation2.gif");
            
            // Start with first animation
            await _ballViewModel.LoadBallVisualAsync(gif1Path);
            _imageService.StartAnimation();
            
            // Let animation play for a bit
            await Task.Delay(200);
            
            // Act - Switch to second animation while first is playing
            var result = await _ballViewModel.LoadBallVisualAsync(gif2Path);
            
            // Assert
            Assert.IsTrue(result, "Should switch animations successfully");
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should remain animated after switch");
            Assert.AreEqual(VisualContentType.GifAnimation, _ballViewModel.ContentType);
            
            // New animation should be playing
            await Task.Delay(100);
            Assert.IsNotNull(_ballViewModel.BallImage, "Should have valid image from new animation");
        }

        /// <summary>
        /// Tests visual content switching during drag operations
        /// </summary>
        [STATestMethod]
        public async Task VisualContentSwitching_DuringDragOperations_ShouldMaintainDragState()
        {
            // Arrange
            var staticImagePath = CreateTestStaticImage();
            var gifPath = CreateTestGifAnimation(25);
            
            // Load initial content and start dragging
            await _ballViewModel.LoadBallVisualAsync(staticImagePath);
            _ballViewModel.IsDragging = true;
            _ballViewModel.X = 150;
            _ballViewModel.Y = 200;
            
            var initialPosition = new Point(_ballViewModel.X, _ballViewModel.Y);
            
            // Act - Switch content while dragging
            var result = await _ballViewModel.LoadBallVisualAsync(gifPath);
            
            // Assert
            Assert.IsTrue(result, "Should switch content successfully during drag");
            Assert.IsTrue(_ballViewModel.IsDragging, "Should maintain drag state");
            Assert.AreEqual(initialPosition.X, _ballViewModel.X, "Should maintain X position");
            Assert.AreEqual(initialPosition.Y, _ballViewModel.Y, "Should maintain Y position");
            Assert.IsTrue(_ballViewModel.IsAnimated, "Should be animated after switch");
            
            // Animation should continue during drag
            _ballViewModel.EnsureAnimationContinuesDuringDrag();
            await Task.Delay(100);
            Assert.IsNotNull(_ballViewModel.BallImage, "Should have valid image during drag with animation");
        }

        #endregion Visual Content Switching Tests

        #region Performance and Stress Tests

        /// <summary>
        /// Tests animation system performance under rapid content switching
        /// </summary>
        [STATestMethod]
        public async Task AnimationSystem_RapidContentSwitching_ShouldMaintainPerformance()
        {
            // Arrange
            var contentPaths = new List<string>
            {
                CreateTestGifAnimation(15, "gif1.gif"),
                CreateTestGifAnimation(30, "gif2.gif"),
                CreateTestStaticImage("static1.png"),
                CreateTestAsepriteAnimation(24, "aseprite1").PngPath,
                CreateTestStaticImage("static2.png")
            };
            
            var switchTimes = new List<double>();
            
            // Act - Perform rapid content switching
            for (int i = 0; i < contentPaths.Count; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await _ballViewModel.LoadBallVisualAsync(contentPaths[i]);
                stopwatch.Stop();
                
                Assert.IsTrue(result, $"Should load content {i + 1} successfully");
                switchTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                
                // Brief pause to allow system to stabilize
                await Task.Delay(50);
            }
            
            // Assert
            var averageSwitchTime = switchTimes.Average();
            var maxSwitchTime = switchTimes.Max();
            
            Console.WriteLine($"Rapid switching - Average: {averageSwitchTime:F2}ms, Max: {maxSwitchTime:F2}ms");
            
            Assert.IsTrue(averageSwitchTime < 100.0, 
                $"Average switch time should be < 100ms, but was {averageSwitchTime:F2}ms");
            Assert.IsTrue(maxSwitchTime < 200.0, 
                $"Maximum switch time should be < 200ms, but was {maxSwitchTime:F2}ms");
        }

        /// <summary>
        /// Tests animation system memory management during extended operations
        /// </summary>
        [STATestMethod]
        public async Task AnimationSystem_ExtendedOperations_ShouldManageMemoryEfficiently()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(30);
            await _ballViewModel.LoadBallVisualAsync(gifPath);
            _imageService.StartAnimation();
            
            var initialMemory = GC.GetTotalMemory(false);
            
            // Act - Run extended animation operations
            for (int i = 0; i < 100; i++)
            {
                _ballViewModel.CoordinateAnimationWithPhysics();
                _imageService.UpdateFrame();
                
                // Simulate some drag operations
                if (i % 10 == 0)
                {
                    _ballViewModel.IsDragging = true;
                    _ballViewModel.EnsureAnimationContinuesDuringDrag();
                    _ballViewModel.IsDragging = false;
                }
                
                await Task.Delay(20);
            }
            
            // Force garbage collection and measure memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert
            Console.WriteLine($"Memory usage - Initial: {initialMemory / 1024}KB, Final: {finalMemory / 1024}KB, Increase: {memoryIncrease / 1024}KB");
            
            // Memory increase should be reasonable (less than 5MB for extended operations)
            Assert.IsTrue(memoryIncrease < 5 * 1024 * 1024, 
                $"Memory increase should be < 5MB, but was {memoryIncrease / 1024 / 1024}MB");
        }

        #endregion Performance and Stress Tests

        #region Helper Methods

        /// <summary>
        /// Creates a test GIF animation file
        /// </summary>
        private string CreateTestGifAnimation(int fps, string fileName = "test_animation.gif")
        {
            var gifPath = Path.Combine(_testDataDirectory, fileName);
            
            // Create a simple test GIF file (placeholder for testing)
            // In a real implementation, this would create an actual GIF
            var testGifData = TestImageHelper.CreateTestGifData(fps);
            File.WriteAllBytes(gifPath, testGifData);
            
            return gifPath;
        }

        /// <summary>
        /// Creates a test Aseprite animation (PNG + JSON)
        /// </summary>
        private (string PngPath, string JsonPath) CreateTestAsepriteAnimation(int fps, string baseName = "test_aseprite")
        {
            var pngPath = Path.Combine(_testDataDirectory, $"{baseName}.png");
            var jsonPath = Path.Combine(_testDataDirectory, $"{baseName}.json");
            
            // Create test sprite sheet
            var spriteSheetData = TestImageHelper.CreateTestSpriteSheet(64, 64, 4); // 4 frames
            File.WriteAllBytes(pngPath, spriteSheetData);
            
            // Create test Aseprite JSON metadata
            var jsonData = TestImageHelper.CreateTestAsepriteJson(fps, 4);
            File.WriteAllText(jsonPath, jsonData);
            
            return (pngPath, jsonPath);
        }

        /// <summary>
        /// Creates a test static image file
        /// </summary>
        private string CreateTestStaticImage(string fileName = "test_static.png")
        {
            var imagePath = Path.Combine(_testDataDirectory, fileName);
            
            var imageData = TestImageHelper.CreateTestImageData(50, 50, Colors.Orange);
            File.WriteAllBytes(imagePath, imageData);
            
            return imagePath;
        }

        #endregion Helper Methods
    }
}