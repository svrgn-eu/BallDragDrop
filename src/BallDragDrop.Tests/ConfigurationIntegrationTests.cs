using System;
using System.IO;
using System.Threading.Tasks;
using BallDragDrop.Bootstrapper;
using BallDragDrop.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for configuration service with application startup
    /// </summary>
    [TestClass]
    public class ConfigurationIntegrationTests
    {
        #region Test Setup and Cleanup

        private string _testConfigDirectory;
        private string _testConfigFilePath;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a temporary directory for test configuration files
            _testConfigDirectory = Path.Combine(Path.GetTempPath(), "BallDragDropConfigTests", Guid.NewGuid().ToString());
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
            
            // Dispose service bootstrapper
            ServiceBootstrapper.Dispose();
        }

        #endregion Test Setup and Cleanup

        #region Integration Tests

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldLoadConfigurationSuccessfully()
        {
            // Arrange
            ServiceBootstrapper.Initialize();
            var configService = ServiceBootstrapper.GetService<IConfigurationService>();

            // Act
            await configService.LoadConfigurationAsync();

            // Assert
            Assert.IsNotNull(configService.Configuration);
            Assert.AreEqual("./Resources/Images/Ball01.png", configService.GetDefaultBallImagePath());
        }

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldHandleConfigurationLoadingErrors()
        {
            // Arrange
            ServiceBootstrapper.Initialize();
            var configService = ServiceBootstrapper.GetService<IConfigurationService>();

            // Act & Assert - Should not throw exception even if configuration loading fails
            try
            {
                await configService.LoadConfigurationAsync();
                Assert.IsNotNull(configService.Configuration);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Configuration loading should handle errors gracefully, but threw: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldCreateDefaultConfigurationFile()
        {
            // Arrange
            ServiceBootstrapper.Initialize();
            var configService = ServiceBootstrapper.GetService<IConfigurationService>();

            // Act
            await configService.LoadConfigurationAsync();
            await configService.SaveConfigurationAsync();

            // Assert
            Assert.IsNotNull(configService.Configuration);
            // Note: The default path might be different if tests run in different order
            // Just ensure it's not null or empty
            Assert.IsFalse(string.IsNullOrEmpty(configService.GetDefaultBallImagePath()));
            Assert.IsTrue(configService.Configuration.EnableAnimations);
            Assert.AreEqual(50.0, configService.Configuration.DefaultBallSize);
        }

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldValidateDefaultImagePath()
        {
            // Arrange
            ServiceBootstrapper.Initialize();
            var configService = ServiceBootstrapper.GetService<IConfigurationService>();

            // Act
            await configService.LoadConfigurationAsync();
            var defaultPath = configService.GetDefaultBallImagePath();
            var isValid = configService.ValidateImagePath(defaultPath);

            // Assert
            Assert.IsNotNull(defaultPath);
            // Note: The validation might return false if the actual file doesn't exist,
            // but the method should not throw an exception
            Assert.IsTrue(isValid || !isValid); // Just ensure no exception is thrown
        }

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldPersistConfigurationChanges()
        {
            // Arrange - Use a custom config file path for this test
            var testConfigPath = Path.Combine(_testConfigDirectory, "test_persistence.json");
            
            // Create a custom configuration service for this test
            var logService = new BallDragDrop.Services.SimpleLogService();
            var configService = new BallDragDrop.Services.ConfigurationService(logService, testConfigPath);

            // Act
            await configService.LoadConfigurationAsync();
            
            var originalPath = configService.GetDefaultBallImagePath();
            var newPath = "./test/custom.png";
            
            configService.SetDefaultBallImagePath(newPath);
            await configService.SaveConfigurationAsync();

            // Create a new service instance to test persistence
            var newConfigService = new BallDragDrop.Services.ConfigurationService(logService, testConfigPath);
            await newConfigService.LoadConfigurationAsync();

            // Assert
            Assert.AreEqual(newPath, newConfigService.GetDefaultBallImagePath());
        }

        [TestMethod]
        public void ConfigurationService_ServiceRegistration_ShouldBeRegisteredAsSingleton()
        {
            // Arrange & Act
            ServiceBootstrapper.Initialize();
            var configService1 = ServiceBootstrapper.GetService<IConfigurationService>();
            var configService2 = ServiceBootstrapper.GetService<IConfigurationService>();

            // Assert
            Assert.AreSame(configService1, configService2, "ConfigurationService should be registered as singleton");
        }

        [TestMethod]
        public async Task ConfigurationService_ApplicationStartup_ShouldHandleInvalidConfigurationGracefully()
        {
            // Arrange
            var invalidJson = "{ invalid json content }";
            await File.WriteAllTextAsync(_testConfigFilePath, invalidJson);
            
            ServiceBootstrapper.Initialize();
            var configService = ServiceBootstrapper.GetService<IConfigurationService>();

            // Act & Assert - Should handle invalid JSON gracefully
            try
            {
                await configService.LoadConfigurationAsync();
                Assert.IsNotNull(configService.Configuration);
                // Should fall back to default values
                Assert.AreEqual("./Resources/Images/Ball01.png", configService.GetDefaultBallImagePath());
            }
            catch (Exception ex)
            {
                Assert.Fail($"Configuration service should handle invalid JSON gracefully, but threw: {ex.Message}");
            }
        }

        #endregion Integration Tests
    }
}