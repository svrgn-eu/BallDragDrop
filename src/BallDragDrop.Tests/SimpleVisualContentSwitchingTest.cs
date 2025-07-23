using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Simple tests for visual content switching functionality
    /// </summary>
    [TestClass]
    public class SimpleVisualContentSwitchingTest
    {
        private string _testImagesPath;

        [TestInitialize]
        public void Setup()
        {
            // Create test images directory
            _testImagesPath = Path.Combine(Path.GetTempPath(), "BallDragDropTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testImagesPath);
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
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            
            // Load initial image
            await viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = viewModel.BallImage;

            // Create a different static image
            var newStaticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static2.png", 100, 100, Colors.Blue);

            // Act
            bool result = await viewModel.SwitchBallVisualAsync(newStaticImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreNotEqual(initialImage, viewModel.BallImage, "Ball image should change");
            Assert.AreEqual(VisualContentType.StaticImage, viewModel.ContentType, "Content type should remain static");
            Assert.IsFalse(viewModel.IsAnimated, "Ball should not be animated");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WhileDragging_ShouldMaintainDragState()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            var newImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static2.png", 100, 100, Colors.Blue);
            
            // Load initial image
            await viewModel.LoadBallVisualAsync(staticImagePath);
            
            // Start dragging
            viewModel.IsDragging = true;
            var initialPosition = new System.Windows.Point(viewModel.X, viewModel.Y);

            // Act
            bool result = await viewModel.SwitchBallVisualAsync(newImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.IsTrue(viewModel.IsDragging, "Drag state should be maintained");
            Assert.AreEqual(initialPosition.X, viewModel.X, "X position should be maintained");
            Assert.AreEqual(initialPosition.Y, viewModel.Y, "Y position should be maintained");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithInvalidFile_ShouldReturnFalse()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            await viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = viewModel.BallImage;
            var initialContentType = viewModel.ContentType;

            var invalidPath = Path.Combine(_testImagesPath, "nonexistent.png");

            // Act
            bool result = await viewModel.SwitchBallVisualAsync(invalidPath);

            // Assert
            Assert.IsFalse(result, "Visual switching should fail for invalid file");
            Assert.AreEqual(initialImage, viewModel.BallImage, "Ball image should remain unchanged");
            Assert.AreEqual(initialContentType, viewModel.ContentType, "Content type should remain unchanged");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_PreservesPositionDuringTransition()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            
            // Load initial image and set specific position
            await viewModel.LoadBallVisualAsync(staticImagePath);
            viewModel.X = 150;
            viewModel.Y = 200;
            var expectedPosition = new System.Windows.Point(viewModel.X, viewModel.Y);

            // Create new image
            var newImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static2.png", 100, 100, Colors.Green);

            // Act
            bool result = await viewModel.SwitchBallVisualAsync(newImagePath);

            // Assert
            Assert.IsTrue(result, "Visual switching should succeed");
            Assert.AreEqual(expectedPosition.X, viewModel.X, "X position should be preserved");
            Assert.AreEqual(expectedPosition.Y, viewModel.Y, "Y position should be preserved");
        }

        [TestMethod]
        public async Task SwitchBallVisualAsync_WithNullOrEmptyPath_ShouldReturnFalse()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            var staticImagePath = TestImageHelper.CreateTestStaticImage(_testImagesPath, "test_static.png");
            await viewModel.LoadBallVisualAsync(staticImagePath);
            var initialImage = viewModel.BallImage;

            // Act & Assert - Test null path
            bool result1 = await viewModel.SwitchBallVisualAsync(null);
            Assert.IsFalse(result1, "Switching with null path should fail");
            Assert.AreEqual(initialImage, viewModel.BallImage, "Image should remain unchanged");

            // Act & Assert - Test empty path
            bool result2 = await viewModel.SwitchBallVisualAsync(string.Empty);
            Assert.IsFalse(result2, "Switching with empty path should fail");
            Assert.AreEqual(initialImage, viewModel.BallImage, "Image should remain unchanged");
        }
    }
}