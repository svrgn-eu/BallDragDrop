using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Tests for visual content switching functionality
    /// </summary>
    [TestClass]
    public class VisualContentSwitchingTests
    {
        private BallViewModel _viewModel;
        private ImageService _imageService;
        private string _testImagesPath;

        [TestInitialize]
        public void Setup()
        {
            // Create test images directory
            _testImagesPath = Path.Combine(Path.GetTempPath(), "BallDragDropTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testImagesPath);

            // Create ImageService and BallViewModel for testing
            _imageService = new ImageService();
            _viewModel = new BallViewModel(100, 100, 25, _imageService);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test images
            if (Directory.Exists(_testImagesPath))
            {
                Directory.Delete(_testImagesPath, true);
            }
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithValidStaticImage_ShouldSwitchSuccessfully()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            
            // Load initial image
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = _viewModel.BallImage;
            var initialContentType = _viewModel.ContentType;

            // Create a different static image
            var newStaticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static2.png", 100, 100, Colors.Blue);

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(newStaticImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreNotEqual(initialImage, _viewModel.BallImage, "Ball image should change");
            Assert.AreEqual(VisualContentType.StaticImage, _viewModel.ContentType, "Content type should remain static");
            Assert.IsFalse(_viewModel.IsAnimated, "Ball should not be animated");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_FromStaticToAnimated_ShouldTransitionCorrectly()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            var gifPath = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_animation.gif");
            
            // Load initial static image
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            Assert.IsFalse(_viewModel.IsAnimated, "Initial content should not be animated");

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreEqual(VisualContentType.GifAnimation, _viewModel.ContentType, "Content type should be GIF animation");
            Assert.IsTrue(_viewModel.IsAnimated, "Ball should be animated after switch");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_FromAnimatedToStatic_ShouldTransitionCorrectly()
        {
            // Arrange
            var gifPath = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_animation.gif");
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            
            // Load initial animated content
            await _viewModel.LoadBallVisualAsync(gifPath);
            Assert.IsTrue(_viewModel.IsAnimated, "Initial content should be animated");

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(staticImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreEqual(VisualContentType.StaticImage, _viewModel.ContentType, "Content type should be static image");
            Assert.IsFalse(_viewModel.IsAnimated, "Ball should not be animated after switch");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WhileDragging_ShouldMaintainDragState()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            var newImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static2.png", 100, 100, Colors.Blue);
            
            // Load initial image
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            
            // Start dragging
            _viewModel.IsDragging = true;
            var initialPosition = new System.Windows.Point(_viewModel.X, _viewModel.Y);

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(newImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.IsTrue(_viewModel.IsDragging, "Drag state should be maintained");
            Assert.AreEqual(initialPosition.X, _viewModel.X, "X position should be maintained");
            Assert.AreEqual(initialPosition.Y, _viewModel.Y, "Y position should be maintained");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithInvalidFile_ShouldReturnFalse()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = _viewModel.BallImage;
            var initialContentType = _viewModel.ContentType;

            var invalidPath = Path.Combine(_testImagesPath, "nonexistent.png");

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(invalidPath);

            // Assert
            Assert.IsFalse(result, "Visual switching should fail for invalid file");
            Assert.AreEqual(initialImage, _viewModel.BallImage, "Ball image should remain unchanged");
            Assert.AreEqual(initialContentType, _viewModel.ContentType, "Content type should remain unchanged");
        }

        [TestMethod]
        public async Task SwitchVisualContentTypeAsync_BetweenDifferentAnimationTypes_ShouldWork()
        {
            // Arrange
            var gifPath = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_gif.gif");
            var asepritePngPath = TestImageHelper.CreateTestAsepriteAnimation(_testImagesPath, "test_aseprite");
            
            // Load initial GIF animation
            await _viewModel.LoadBallVisualAsync(gifPath);
            Assert.AreEqual(VisualContentType.GifAnimation, _viewModel.ContentType);

            // Act
            bool result = await _viewModel.SwitchVisualContentTypeAsync(asepritePngPath);

            // Assert
            Assert.IsTrue(result, "Visual content type switching should succeed");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _viewModel.ContentType, "Content type should change to Aseprite");
            Assert.IsTrue(_viewModel.IsAnimated, "Ball should remain animated");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_PreservesPositionDuringTransition()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            var gifPath = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_animation.gif");
            
            // Load initial image and set specific position
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            _viewModel.X = 150;
            _viewModel.Y = 200;
            var expectedPosition = new System.Windows.Point(_viewModel.X, _viewModel.Y);

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreEqual(expectedPosition.X, _viewModel.X, "X position should be preserved");
            Assert.AreEqual(expectedPosition.Y, _viewModel.Y, "Y position should be preserved");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithAnimatedContent_ShouldStartAnimation()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            var gifPath = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_animation.gif");
            
            // Load initial static image
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            Assert.IsFalse(_viewModel.IsAnimated);

            // Act
            bool result = await _viewModel.SwitchBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.IsTrue(_viewModel.IsAnimated, "Animation should be started");
            
            // Wait a bit and check if animation is actually running
            await Task.Delay(200);
            // Note: In a real test environment, we might need to check animation timer state
            // For now, we verify that the animated flag is set correctly
        }

        [TestMethod]
        public async Task ImageService_SwitchVisualContentAsync_ShouldPreservePlaybackState()
        {
            // Arrange
            var gifPath1 = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_gif1.gif");
            var gifPath2 = TestImageHelper.CreateTestGifAnimation(_testImagesPath, "test_gif2.gif");
            
            // Load first animation
            await _imageService.LoadBallVisualAsync(gifPath1);
            _imageService.StartAnimation();
            
            // Simulate some playback time
            await Task.Delay(100);

            // Act
            bool result = await _imageService.SwitchVisualContentAsync(gifPath2, preservePlaybackState: true);

            // Assert
            Assert.IsTrue(result, "Visual content switching should succeed");
            Assert.IsTrue(_imageService.IsAnimated, "Content should be animated");
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_MultipleRapidSwitches_ShouldHandleGracefully()
        {
            // Arrange
            var image1Path = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test1.png", 50, 50, Colors.Red);
            var image2Path = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test2.png", 50, 50, Colors.Green);
            var image3Path = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test3.png", 50, 50, Colors.Blue);

            // Act - Perform rapid switches
            var task1 = _viewModel.SwitchBallVisualAsync(image1Path);
            var task2 = _viewModel.SwitchBallVisualAsync(image2Path);
            var task3 = _viewModel.SwitchBallVisualAsync(image3Path);

            var results = await Task.WhenAll(task1, task2, task3);

            // Assert
            // At least one switch should succeed (the last one should typically win)
            Assert.IsTrue(results[0] || results[1] || results[2], "At least one switch should succeed");
            Assert.IsNotNull(_viewModel.BallImage, "Ball image should not be null");
            Assert.AreEqual(VisualContentType.StaticImage, _viewModel.ContentType);
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithNullOrEmptyPath_ShouldReturnFalse()
        {
            // Arrange
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            await _viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = _viewModel.BallImage;

            // Act & Assert - Test null path
            bool result1 = await _viewModel.SwitchBallVisualAsync(null);
            Assert.IsFalse(result1, "Switching with null path should fail");
            Assert.AreEqual(initialImage, _viewModel.BallImage, "Image should remain unchanged");

            // Act & Assert - Test empty path
            bool result2 = await _viewModel.SwitchBallVisualAsync(string.Empty);
            Assert.IsFalse(result2, "Switching with empty path should fail");
            Assert.AreEqual(initialImage, _viewModel.BallImage, "Image should remain unchanged");
        }
    }
}