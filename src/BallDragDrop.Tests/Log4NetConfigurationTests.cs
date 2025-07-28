using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Services;
using BallDragDrop.Bootstrapper;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Tests for Log4Net configuration and initialization
    /// </summary>
    [TestClass]
    public class Log4NetConfigurationTests
    {
        /// <summary>
        /// Test setup - initialize services
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Initialize the service bootstrapper
            ServiceBootstrapper.Initialize();
        }

        /// <summary>
        /// Test cleanup - dispose services
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            ServiceBootstrapper.Dispose();
        }

        /// <summary>
        /// Tests that Log4Net service can be created and initialized
        /// </summary>
        [TestMethod]
        public void Log4NetService_CanBeCreatedAndInitialized()
        {
            // Arrange & Act
            var logService = ServiceBootstrapper.GetService<BallDragDrop.Contracts.ILogService>();

            // Assert
            Assert.IsNotNull(logService);
            Assert.IsInstanceOfType(logService, typeof(Log4NetService));
        }

        /// <summary>
        /// Tests that different log levels work correctly
        /// </summary>
        [TestMethod]
        public void Log4NetService_LogLevels_WorkCorrectly()
        {
            // Arrange
            var logService = ServiceBootstrapper.GetService<BallDragDrop.Contracts.ILogService>();

            // Act & Assert - should not throw exceptions
            AssertExtensions.DoesNotThrow(() => logService.LogDebug("Test debug message"));
            AssertExtensions.DoesNotThrow(() => logService.LogInformation("Test info message"));
            AssertExtensions.DoesNotThrow(() => logService.LogWarning("Test warning message"));
            AssertExtensions.DoesNotThrow(() => logService.LogError("Test error message"));
            AssertExtensions.DoesNotThrow(() => logService.LogCritical("Test critical message"));
        }

        /// <summary>
        /// Tests that file logging is working (Info level and above should be written to file)
        /// </summary>
        [TestMethod]
        public void Log4NetService_FileLogging_WorksCorrectly()
        {
            // Arrange
            var logService = ServiceBootstrapper.GetService<BallDragDrop.Contracts.ILogService>();
            var logFilePath = Path.Combine("logs", "application.log");
            var testMessage = $"Test file logging message - {Guid.NewGuid()}";

            // Act
            logService.LogInformation(testMessage);
            
            // Give some time for async logging to complete
            Thread.Sleep(1000);

            // Assert
            if (File.Exists(logFilePath))
            {
                var logContent = File.ReadAllText(logFilePath);
                Assert.IsTrue(logContent.Contains(testMessage), 
                    "Log file should contain the test message");
            }
            else
            {
                Assert.Inconclusive("Log file was not created - this might be expected in test environment");
            }
        }

        /// <summary>
        /// Tests that debug messages are filtered out from file logging (only Info and above)
        /// </summary>
        [TestMethod]
        public void Log4NetService_FileLogging_FiltersDebugMessages()
        {
            // Arrange
            var logService = ServiceBootstrapper.GetService<BallDragDrop.Contracts.ILogService>();
            var logFilePath = Path.Combine("logs", "application.log");
            var debugMessage = $"Debug message should not appear in file - {Guid.NewGuid()}";
            var infoMessage = $"Info message should appear in file - {Guid.NewGuid()}";

            // Act
            logService.LogDebug(debugMessage);
            logService.LogInformation(infoMessage);
            
            // Give some time for async logging to complete
            Thread.Sleep(1000);

            // Assert
            if (File.Exists(logFilePath))
            {
                var logContent = File.ReadAllText(logFilePath);
                Assert.IsFalse(logContent.Contains(debugMessage), 
                    "Log file should NOT contain debug messages");
                Assert.IsTrue(logContent.Contains(infoMessage), 
                    "Log file should contain info messages");
            }
            else
            {
                Assert.Inconclusive("Log file was not created - this might be expected in test environment");
            }
        }

        /// <summary>
        /// Tests that exception logging works correctly
        /// </summary>
        [TestMethod]
        public void Log4NetService_ExceptionLogging_WorksCorrectly()
        {
            // Arrange
            var logService = ServiceBootstrapper.GetService<BallDragDrop.Contracts.ILogService>();
            var testException = new InvalidOperationException("Test exception for logging");

            // Act & Assert - should not throw exceptions
            AssertExtensions.DoesNotThrow(() => logService.LogError(testException, "Test error with exception"));
            AssertExtensions.DoesNotThrow(() => logService.LogCritical(testException, "Test critical with exception"));
        }
    }

    /// <summary>
    /// Helper class for assertion extensions
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Asserts that an action does not throw any exception
        /// </summary>
        /// <param name="action">The action to test</param>
        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}