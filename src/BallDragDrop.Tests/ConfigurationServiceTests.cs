using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using BallDragDrop.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for ConfigurationService
    /// </summary>
    [TestClass]
    public class ConfigurationServiceTests
    {
        #region Test Setup and Cleanup

        private Mock<ILogService> _mockLogService;
        private string _testConfigDirectory;
        private string _testConfigFilePath;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            
            // Create a temporary directory for test configuration files
            _testConfigDirectory = Path.Combine(Path.GetTempPath(), "BallDragDropTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testConfigDirectory);
            _testConfigFilePath = Path.Combine(_testConfigDirectory, "appsettings.json");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test files and directories
            if (Directory.Exists(_testConfigDirectory))
            {
                Directory.Delete(_testConfigDirectory, true);
            }
        }

        #endregion Test Setup and Cleanup

        #region Constructor Tests

        [TestMethod]
        public async Task Constructor_WithLogService_ShouldInitializeCorrectly()
        {
            // Act
            var service = new ConfigurationService(_mockLogService.Object);
            service.Initialize();

            // Assert
            Assert.IsNotNull(service.Configuration);
            Assert.AreEqual("./Resources/Images/Ball01.png", service.Configuration.DefaultBallImagePath);
            Assert.IsTrue(service.Configuration.EnableAnimations);
            Assert.AreEqual(50.0, service.Configuration.DefaultBallSize);
        }

        [TestMethod]
        public async Task Constructor_WithCustomPath_ShouldInitializeCorrectly()
        {
            // Act
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Assert
            Assert.IsNotNull(service.Configuration);
            Assert.AreEqual("./Resources/Images/Ball01.png", service.Configuration.DefaultBallImagePath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullLogService_ShouldThrowArgumentNullException()
        {
            // Act
            new ConfigurationService(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullConfigPath_ShouldThrowArgumentNullException()
        {
            // Act
            new ConfigurationService(_mockLogService.Object, null);
        }

        #endregion Constructor Tests

        #region InitializeAsync Tests

        [TestMethod]
        public async Task InitializeAsync_WithNonExistentFile_ShouldUseDefaults()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.Initialize();

            // Assert
            Assert.IsNotNull(service.Configuration);
            Assert.AreEqual("./Resources/Images/Ball01.png", service.Configuration.DefaultBallImagePath);
            Assert.IsTrue(service.Configuration.EnableAnimations);
            Assert.AreEqual(50.0, service.Configuration.DefaultBallSize);
        }

        [TestMethod]
        public async Task InitializeAsync_WithValidFile_ShouldLoadConfiguration()
        {
            // Arrange
            var json = @"{
                ""DefaultBallImagePath"": ""./test/path.png"",
                ""EnableAnimations"": false,
                ""DefaultBallSize"": 75.0
            }";
            await File.WriteAllTextAsync(_testConfigFilePath, json);

            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.Initialize();

            // Assert
            Assert.AreEqual("./test/path.png", service.Configuration.DefaultBallImagePath);
            Assert.IsFalse(service.Configuration.EnableAnimations);
            Assert.AreEqual(75.0, service.Configuration.DefaultBallSize);
        }

        [TestMethod]
        public async Task InitializeAsync_WithInvalidJson_ShouldUseFallback()
        {
            // Arrange
            await File.WriteAllTextAsync(_testConfigFilePath, "invalid json content");
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.Initialize();

            // Assert
            Assert.IsNotNull(service.Configuration);
            // Should use default values from the interface attributes
            Assert.AreEqual("./Resources/Images/Ball01.png", service.Configuration.DefaultBallImagePath);
        }

        #endregion InitializeAsync Tests

        #region GetDefaultBallImagePath Tests

        [TestMethod]
        public async Task GetDefaultBallImagePath_ShouldReturnConfiguredPath()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();
            service.Configuration.DefaultBallImagePath = "./test/custom.png";

            // Act
            var result = service.GetDefaultBallImagePath();

            // Assert
            Assert.AreEqual("./test/custom.png", result);
        }

        [TestMethod]
        public async Task GetDefaultBallImagePath_WithDefaultConfiguration_ShouldReturnDefault()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            var result = service.GetDefaultBallImagePath();

            // Assert
            Assert.AreEqual("./Resources/Images/Ball01.png", result);
        }

        #endregion GetDefaultBallImagePath Tests

        #region SetDefaultBallImagePath Tests

        [TestMethod]
        public async Task SetDefaultBallImagePath_WithValidPath_ShouldUpdateConfiguration()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();
            var newPath = "./new/path.png";

            // Act
            service.SetDefaultBallImagePath(newPath);

            // Assert
            Assert.AreEqual(newPath, service.Configuration.DefaultBallImagePath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task SetDefaultBallImagePath_WithNullPath_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            service.SetDefaultBallImagePath(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetDefaultBallImagePath_WithEmptyPath_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.SetDefaultBallImagePath("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetDefaultBallImagePath_WithWhitespacePath_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.SetDefaultBallImagePath("   ");
        }

        #endregion SetDefaultBallImagePath Tests

        #region ValidateImagePath Tests

        [TestMethod]
        public void ValidateImagePath_WithNullPath_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            var result = service.ValidateImagePath(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateImagePath_WithEmptyPath_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            var result = service.ValidateImagePath("");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateImagePath_WithNonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var nonExistentPath = Path.Combine(_testConfigDirectory, "nonexistent.png");

            // Act
            var result = service.ValidateImagePath(nonExistentPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateImagePath_WithExistingPngFile_ShouldReturnTrue()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var testImagePath = Path.Combine(_testConfigDirectory, "test.png");
            
            // Create a dummy PNG file
            await File.WriteAllBytesAsync(testImagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header

            // Act
            var result = service.ValidateImagePath(testImagePath);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidateImagePath_WithExistingJpgFile_ShouldReturnTrue()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var testImagePath = Path.Combine(_testConfigDirectory, "test.jpg");
            
            // Create a dummy JPG file
            await File.WriteAllBytesAsync(testImagePath, new byte[] { 0xFF, 0xD8, 0xFF }); // JPEG header

            // Act
            var result = service.ValidateImagePath(testImagePath);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ValidateImagePath_WithUnsupportedExtension_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var testFilePath = Path.Combine(_testConfigDirectory, "test.txt");
            
            // Create a dummy text file
            await File.WriteAllTextAsync(testFilePath, "test content");

            // Act
            var result = service.ValidateImagePath(testFilePath);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion ValidateImagePath Tests

        #region Integration Tests

        [TestMethod]
        public async Task ConfigurationService_FullWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act & Assert - Load default configuration
            Assert.AreEqual("./Resources/Images/Ball01.png", service.GetDefaultBallImagePath());

            // Act & Assert - Modify configuration
            service.SetDefaultBallImagePath("./custom/ball.png");
            service.Configuration.EnableAnimations = false;
            service.Configuration.DefaultBallSize = 80.0;

            // Act & Assert - Save configuration
            Assert.IsTrue(File.Exists(_testConfigFilePath));

            // Act & Assert - Create new service instance and load
            var newService = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            newService.Initialize();

            Assert.AreEqual("./custom/ball.png", newService.GetDefaultBallImagePath());
            Assert.IsFalse(newService.Configuration.EnableAnimations);
            Assert.AreEqual(80.0, newService.Configuration.DefaultBallSize);
        }

        #endregion Integration Tests
    }
}
