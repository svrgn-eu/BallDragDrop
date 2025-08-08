using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
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
            Assert.AreEqual("../../Resources/Ball/Ball01.png", service.Configuration.DefaultBallImagePath);
            Assert.IsTrue(service.Configuration.EnableAnimations);
            Assert.AreEqual(50.0, service.Configuration.DefaultBallSize);
            Assert.IsFalse(service.Configuration.ShowBoundingBox);
        }

        [TestMethod]
        public async Task Constructor_WithCustomPath_ShouldInitializeCorrectly()
        {
            // Act
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Assert
            Assert.IsNotNull(service.Configuration);
            Assert.AreEqual("../../Resources/Ball/Ball01.png", service.Configuration.DefaultBallImagePath);
            Assert.IsFalse(service.Configuration.ShowBoundingBox);
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
            Assert.AreEqual("../../Resources/Ball/Ball01.png", service.Configuration.DefaultBallImagePath);
            Assert.IsTrue(service.Configuration.EnableAnimations);
            Assert.AreEqual(50.0, service.Configuration.DefaultBallSize);
            Assert.IsFalse(service.Configuration.ShowBoundingBox);
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
            Assert.AreEqual("../../Resources/Ball/Ball01.png", service.Configuration.DefaultBallImagePath);
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
            Assert.AreEqual("../../Resources/Ball/Ball01.png", result);
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

        #region ShowBoundingBox Tests

        [TestMethod]
        public async Task GetShowBoundingBox_WithDefaultConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            var result = service.GetShowBoundingBox();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SetShowBoundingBox_WithValidValue_ShouldUpdateConfiguration()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            service.SetShowBoundingBox(true);

            // Assert
            Assert.IsTrue(service.Configuration.ShowBoundingBox);
            Assert.IsTrue(service.GetShowBoundingBox());
        }

        [TestMethod]
        public async Task ShowBoundingBox_ToggleValue_ShouldWorkCorrectly()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act & Assert - Initial state should be false
            Assert.IsFalse(service.GetShowBoundingBox());

            // Act & Assert - Toggle to true
            service.SetShowBoundingBox(true);
            Assert.IsTrue(service.GetShowBoundingBox());

            // Act & Assert - Toggle back to false
            service.SetShowBoundingBox(false);
            Assert.IsFalse(service.GetShowBoundingBox());
        }

        [TestMethod]
        public async Task ShowBoundingBox_WithConfigurationFile_ShouldPersist()
        {
            // Arrange
            var json = @"{
                ""DefaultBallImagePath"": ""./test/path.png"",
                ""EnableAnimations"": true,
                ""DefaultBallSize"": 50.0,
                ""ShowBoundingBox"": true
            }";
            await File.WriteAllTextAsync(_testConfigFilePath, json);

            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            service.Initialize();

            // Assert
            Assert.IsTrue(service.GetShowBoundingBox());
            Assert.IsTrue(service.Configuration.ShowBoundingBox);
        }

        #endregion ShowBoundingBox Tests

        #region Integration Tests

        [TestMethod]
        public async Task ConfigurationService_FullWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act & Assert - Load default configuration
            Assert.AreEqual("../../Resources/Ball/Ball01.png", service.GetDefaultBallImagePath());

            // Act & Assert - Modify configuration
            service.SetDefaultBallImagePath("./custom/ball.png");
            service.Configuration.EnableAnimations = false;
            service.Configuration.DefaultBallSize = 80.0;
            service.SetShowBoundingBox(true);

            // Act & Assert - Save configuration
            Assert.IsTrue(File.Exists(_testConfigFilePath));

            // Act & Assert - Create new service instance and load
            var newService = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            newService.Initialize();

            Assert.AreEqual("./custom/ball.png", newService.GetDefaultBallImagePath());
            Assert.IsFalse(newService.Configuration.EnableAnimations);
            Assert.AreEqual(80.0, newService.Configuration.DefaultBallSize);
            Assert.IsTrue(newService.GetShowBoundingBox());
        }

        #endregion Integration Tests

        #region Cursor Configuration Tests

        [TestMethod]
        public async Task GetCursorConfiguration_WithDefaultConfiguration_ShouldReturnDefaults()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            var result = service.GetCursorConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.EnableCustomCursors);
            Assert.AreEqual("Resources/Cursors/default.png", result.DefaultCursorPath);
            Assert.AreEqual("Resources/Cursors/hover.png", result.HoverCursorPath);
            Assert.AreEqual("Resources/Cursors/grabbing.png", result.GrabbingCursorPath);
            Assert.AreEqual("Resources/Cursors/releasing.png", result.ReleasingCursorPath);
            Assert.AreEqual(16, result.DebounceTimeMs);
            Assert.AreEqual(200, result.ReleasingDurationMs);
        }

        [TestMethod]
        public async Task GetCursorConfiguration_WithCustomConfiguration_ShouldReturnCustomValues()
        {
            // Arrange
            var json = @"{
                ""CursorConfiguration_EnableCustomCursors"": false,
                ""CursorConfiguration_DefaultCursorPath"": ""Custom/default.png"",
                ""CursorConfiguration_HoverCursorPath"": ""Custom/hover.png"",
                ""CursorConfiguration_GrabbingCursorPath"": ""Custom/grabbing.png"",
                ""CursorConfiguration_ReleasingCursorPath"": ""Custom/releasing.png"",
                ""CursorConfiguration_DebounceTimeMs"": 32,
                ""CursorConfiguration_ReleasingDurationMs"": 500
            }";
            await File.WriteAllTextAsync(_testConfigFilePath, json);

            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            service.Initialize();

            // Act
            var result = service.GetCursorConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.EnableCustomCursors);
            Assert.AreEqual("Custom/default.png", result.DefaultCursorPath);
            Assert.AreEqual("Custom/hover.png", result.HoverCursorPath);
            Assert.AreEqual("Custom/grabbing.png", result.GrabbingCursorPath);
            Assert.AreEqual("Custom/releasing.png", result.ReleasingCursorPath);
            Assert.AreEqual(32, result.DebounceTimeMs);
            Assert.AreEqual(500, result.ReleasingDurationMs);
        }

        [TestMethod]
        public void GetCursorConfiguration_WithUninitializedConfiguration_ShouldReturnDefaults()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            // Don't call Initialize()

            // Act
            var result = service.GetCursorConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.EnableCustomCursors);
            Assert.AreEqual("Resources/Cursors/default.png", result.DefaultCursorPath);
        }

        [TestMethod]
        public void GetDefaultCursorConfiguration_ShouldReturnValidDefaults()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            var result = service.GetDefaultCursorConfiguration();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.EnableCustomCursors);
            Assert.AreEqual("Resources/Cursors/default.png", result.DefaultCursorPath);
            Assert.AreEqual("Resources/Cursors/hover.png", result.HoverCursorPath);
            Assert.AreEqual("Resources/Cursors/grabbing.png", result.GrabbingCursorPath);
            Assert.AreEqual("Resources/Cursors/releasing.png", result.ReleasingCursorPath);
            Assert.AreEqual(16, result.DebounceTimeMs);
            Assert.AreEqual(200, result.ReleasingDurationMs);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithNullConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);

            // Act
            var result = service.ValidateCursorConfiguration(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithInvalidDebounceTime_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();
            config.DebounceTimeMs = 0; // Invalid - too low

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithInvalidReleasingDuration_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();
            config.ReleasingDurationMs = 10; // Invalid - too low

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithEmptyCursorPath_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();
            config.DefaultCursorPath = ""; // Invalid - empty path

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithNonPngExtension_ShouldReturnFalse()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();
            config.DefaultCursorPath = "Resources/Cursors/default.jpg"; // Invalid - not PNG

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateCursorConfiguration_WithCustomCursorsDisabled_ShouldSkipPathValidation()
        {
            // Arrange
            var service = new ConfigurationService(_mockLogService.Object, _testConfigFilePath);
            var config = service.GetDefaultCursorConfiguration();
            config.EnableCustomCursors = false;
            config.DefaultCursorPath = ""; // Would be invalid if custom cursors were enabled

            // Act
            var result = service.ValidateCursorConfiguration(config);

            // Assert
            Assert.IsTrue(result); // Should be valid because custom cursors are disabled
        }

        #endregion Cursor Configuration Tests
    }
}
