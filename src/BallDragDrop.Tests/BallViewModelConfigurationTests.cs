using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for BallViewModel configuration integration
    /// </summary>
    [TestClass]
    public class BallViewModelConfigurationTests
    {
        #region Test Setup and Cleanup

        private Mock<ILogService> _mockLogService;
        private Mock<IConfigurationService> _mockConfigurationService;
        private string _testImageDirectory;
        private string _testImagePath;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _mockConfigurationService = new Mock<IConfigurationService>();
            
            // Create a temporary directory for test images
            _testImageDirectory = Path.Combine(Path.GetTempPath(), "BallViewModelConfigTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testImageDirectory);
            _testImagePath = Path.Combine(_testImageDirectory, "test.png");
            
            // Create a dummy PNG file
            File.WriteAllBytes(_testImagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test files and directories
            if (Directory.Exists(_testImageDirectory))
            {
                Directory.Delete(_testImageDirectory, true);
            }
        }

        #endregion Test Setup and Cleanup

        #region Constructor Tests

        [TestMethod]
        public void BallViewModel_Constructor_WithConfigurationService_ShouldInitializeCorrectly()
        {
            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Assert
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(0, viewModel.X);
            Assert.AreEqual(0, viewModel.Y);
            Assert.AreEqual(25, viewModel.Radius);
        }

        [TestMethod]
        public void BallViewModel_Constructor_WithoutConfigurationService_ShouldInitializeCorrectly()
        {
            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, null);

            // Assert
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(0, viewModel.X);
            Assert.AreEqual(0, viewModel.Y);
            Assert.AreEqual(25, viewModel.Radius);
        }

        #endregion Constructor Tests

        #region LoadDefaultBallImageAsync Tests

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithValidConfiguration_ShouldLoadImageSuccessfully()
        {
            // Arrange
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(_testImagePath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(_testImagePath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = await viewModel.LoadDefaultBallImageAsync();

            // Assert
            Assert.IsTrue(result);
            _mockConfigurationService.Verify(c => c.GetDefaultBallImagePath(), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(_testImagePath), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithInvalidPath_ShouldUseFallbackPath()
        {
            // Arrange
            var invalidPath = "./invalid/path.png";
            var fallbackPath = "./Resources/Images/Ball01.png";
            
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(invalidPath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(invalidPath)).Returns(false);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(fallbackPath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = await viewModel.LoadDefaultBallImageAsync();

            // Assert
            // The result might be false if the fallback image doesn't actually exist, but the method should handle it gracefully
            _mockConfigurationService.Verify(c => c.GetDefaultBallImagePath(), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(invalidPath), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(fallbackPath), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.SetDefaultBallImagePath(fallbackPath), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithInvalidPathAndInvalidFallback_ShouldReturnFalse()
        {
            // Arrange
            var invalidPath = "./invalid/path.png";
            var fallbackPath = "./Resources/Images/Ball01.png";
            
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(invalidPath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(invalidPath)).Returns(false);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(fallbackPath)).Returns(false);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = await viewModel.LoadDefaultBallImageAsync();

            // Assert
            Assert.IsFalse(result);
            _mockConfigurationService.Verify(c => c.GetDefaultBallImagePath(), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(invalidPath), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(fallbackPath), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.SetDefaultBallImagePath(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithoutConfigurationService_ShouldReturnFalse()
        {
            // Arrange
            var viewModel = new BallViewModel(_mockLogService.Object, null, null);

            // Act
            var result = await viewModel.LoadDefaultBallImageAsync();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithConfigurationException_ShouldHandleGracefully()
        {
            // Arrange
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Throws(new InvalidOperationException("Test exception"));
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = await viewModel.LoadDefaultBallImageAsync();

            // Assert
            Assert.IsFalse(result);
            _mockLogService.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        #endregion LoadDefaultBallImageAsync Tests

        #region Configuration Validation Tests

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_ShouldLogAppropriateMessages()
        {
            // Arrange
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(_testImagePath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(_testImagePath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            await viewModel.LoadDefaultBallImageAsync();

            // Assert
            _mockLogService.Verify(l => l.LogMethodEntry(It.Is<string>(s => s == "LoadDefaultBallImageAsync")), Times.AtLeastOnce);
            _mockLogService.Verify(l => l.LogDebug(It.Is<string>(s => s.Contains("Loading default ball image from configuration")), It.IsAny<object[]>()), Times.AtLeastOnce);
            _mockLogService.Verify(l => l.LogMethodExit(It.Is<string>(s => s == "LoadDefaultBallImageAsync"), It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task LoadDefaultBallImageAsync_WithInvalidPath_ShouldLogWarningMessages()
        {
            // Arrange
            var invalidPath = "./invalid/path.png";
            var fallbackPath = "./Resources/Images/Ball01.png";
            
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(invalidPath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(invalidPath)).Returns(false);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(fallbackPath)).Returns(false);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            await viewModel.LoadDefaultBallImageAsync();

            // Assert
            _mockLogService.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Default ball image path is invalid")), It.IsAny<object[]>()), Times.AtLeastOnce);
            _mockLogService.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Both default and fallback image paths are invalid"))), Times.AtLeastOnce);
        }

        #endregion Configuration Validation Tests

        #region Default Ball Size Configuration Tests

        [TestMethod]
        public void BallViewModel_Constructor_ShouldUseDefaultBallSizeFromConfiguration()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(35.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);

            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Assert
            Assert.AreEqual(35.0, viewModel.Radius);
        }

        [TestMethod]
        public void BallViewModel_Constructor_WithoutConfiguration_ShouldUseFallbackBallSize()
        {
            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, null);

            // Assert
            Assert.AreEqual(25.0, viewModel.Radius); // Fallback size
        }

        [TestMethod]
        public void BallViewModel_Constructor_WithConfigurationException_ShouldUseFallbackBallSize()
        {
            // Arrange
            _mockConfigurationService.Setup(c => c.Configuration).Throws(new InvalidOperationException("Test exception"));

            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Assert
            Assert.AreEqual(25.0, viewModel.Radius); // Fallback size
            _mockLogService.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void Initialize_WithoutRadius_ShouldUseConfigurationDefault()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(40.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            viewModel.Initialize(100, 200); // No radius provided

            // Assert
            Assert.AreEqual(40.0, viewModel.Radius);
        }

        [TestMethod]
        public void Initialize_WithRadius_ShouldUseProvidedRadius()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(40.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            viewModel.Initialize(100, 200, 30.0); // Explicit radius provided

            // Assert
            Assert.AreEqual(30.0, viewModel.Radius);
        }

        #endregion Default Ball Size Configuration Tests

        #region Configuration Validation Tests

        [TestMethod]
        public void ValidateAndUpdateConfiguration_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(50.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(_testImagePath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(_testImagePath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = viewModel.ValidateAndUpdateConfiguration();

            // Assert
            Assert.IsTrue(result);
            _mockConfigurationService.Verify(c => c.GetDefaultBallImagePath(), Times.AtLeastOnce);
            _mockConfigurationService.Verify(c => c.ValidateImagePath(_testImagePath), Times.AtLeastOnce);
        }

        [TestMethod]
        public void ValidateAndUpdateConfiguration_WithInvalidImagePath_ShouldUpdateWithFallback()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(50.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            
            var invalidPath = "./invalid/path.png";
            var fallbackPath = "./Resources/Images/Ball01.png";
            
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(invalidPath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(invalidPath)).Returns(false);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(fallbackPath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = viewModel.ValidateAndUpdateConfiguration();

            // Assert
            Assert.IsTrue(result);
            _mockConfigurationService.Verify(c => c.SetDefaultBallImagePath(fallbackPath), Times.AtLeastOnce);
        }

        [TestMethod]
        public void ValidateAndUpdateConfiguration_WithInvalidBallSize_ShouldUpdateToValidSize()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.SetupProperty(c => c.DefaultBallSize, -10.0); // Invalid size
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(_testImagePath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(_testImagePath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = viewModel.ValidateAndUpdateConfiguration();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(50.0, mockConfiguration.Object.DefaultBallSize); // Should be reset to 50.0
        }

        [TestMethod]
        public void ValidateAndUpdateConfiguration_WithoutConfigurationService_ShouldReturnFalse()
        {
            // Arrange
            var viewModel = new BallViewModel(_mockLogService.Object, null, null);

            // Act
            var result = viewModel.ValidateAndUpdateConfiguration();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateAndUpdateConfiguration_WithException_ShouldReturnFalse()
        {
            // Arrange
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Throws(new InvalidOperationException("Test exception"));
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            var result = viewModel.ValidateAndUpdateConfiguration();

            // Assert
            Assert.IsFalse(result);
            _mockLogService.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        #endregion Configuration Validation Tests

        #region Configuration Integration Logging Tests

        [TestMethod]
        public void BallViewModel_Constructor_ShouldLogConfigurationUsage()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(45.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);

            // Act
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Assert
            _mockLogService.Verify(l => l.LogDebug(It.Is<string>(s => s.Contains("Using default ball size from configuration")), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void ValidateAndUpdateConfiguration_ShouldLogValidationResults()
        {
            // Arrange
            var mockConfiguration = new Mock<IAppConfiguration>();
            mockConfiguration.Setup(c => c.DefaultBallSize).Returns(50.0);
            _mockConfigurationService.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
            _mockConfigurationService.Setup(c => c.GetDefaultBallImagePath()).Returns(_testImagePath);
            _mockConfigurationService.Setup(c => c.ValidateImagePath(_testImagePath)).Returns(true);
            
            var viewModel = new BallViewModel(_mockLogService.Object, null, _mockConfigurationService.Object);

            // Act
            viewModel.ValidateAndUpdateConfiguration();

            // Assert
            _mockLogService.Verify(l => l.LogMethodEntry(It.Is<string>(s => s == "ValidateAndUpdateConfiguration")), Times.Once);
            _mockLogService.Verify(l => l.LogDebug(It.Is<string>(s => s.Contains("Configuration validation completed - no updates needed"))), Times.Once);
            _mockLogService.Verify(l => l.LogMethodExit(It.Is<string>(s => s == "ValidateAndUpdateConfiguration"), It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        #endregion Configuration Integration Logging Tests
    }
}