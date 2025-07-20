using System;
using System.IO;
using BallDragDrop.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class ExceptionHandlingServiceTests
    {
        private Mock<ILogService> _mockLogService = null!;
        private IExceptionHandlingService _exceptionHandlingService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _exceptionHandlingService = new ExceptionHandlingService(_mockLogService.Object);
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullLogService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ExceptionHandlingService(null!));
        }

        [TestMethod]
        public void Constructor_WithValidLogService_ShouldCreateInstance()
        {
            // Act
            var service = new ExceptionHandlingService(_mockLogService.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region HandleException Tests

        [TestMethod]
        public void HandleException_WithException_ShouldLogError()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var context = "Test context";

            // Act
            _exceptionHandlingService.HandleException(exception, context);

            // Assert
            _mockLogService.Verify(x => x.LogError(
                exception, 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                context), Times.Once);
        }

        [TestMethod]
        public void HandleException_WithExceptionAndEmptyContext_ShouldLogError()
        {
            // Arrange
            var exception = new ArgumentException("Test argument exception");

            // Act
            _exceptionHandlingService.HandleException(exception, "");

            // Assert
            _mockLogService.Verify(x => x.LogError(
                exception, 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                ""), Times.Once);
        }

        [TestMethod]
        public void HandleException_WithExceptionAndNullContext_ShouldLogError()
        {
            // Arrange
            var exception = new NullReferenceException("Test null reference exception");

            // Act
            _exceptionHandlingService.HandleException(exception);

            // Assert
            _mockLogService.Verify(x => x.LogError(
                exception, 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                ""), Times.Once);
        }

        [TestMethod]
        public void HandleException_WithDifferentExceptionTypes_ShouldLogAllCorrectly()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("Argument error"),
                new InvalidOperationException("Invalid operation"),
                new FileNotFoundException("File not found"),
                new OutOfMemoryException("Out of memory"),
                new UnauthorizedAccessException("Access denied")
            };

            // Act
            foreach (var exception in exceptions)
            {
                _exceptionHandlingService.HandleException(exception, $"Context for {exception.GetType().Name}");
            }

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                It.IsAny<string>()), Times.Exactly(exceptions.Length));
        }

        #endregion

        #region CaptureApplicationContext Tests

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnValidContext()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            Assert.IsNotNull(context);
            
            // Verify the context contains expected properties
            var contextType = context.GetType();
            Assert.IsNotNull(contextType.GetProperty("Timestamp"));
            Assert.IsNotNull(contextType.GetProperty("MachineName"));
            Assert.IsNotNull(contextType.GetProperty("OSVersion"));
            Assert.IsNotNull(contextType.GetProperty("ProcessorCount"));
            Assert.IsNotNull(contextType.GetProperty("WorkingSet"));
        }

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnCurrentTimestamp()
        {
            // Arrange
            var beforeCapture = DateTime.UtcNow;

            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();
            var afterCapture = DateTime.UtcNow;

            // Assert
            var timestamp = (DateTime)context.GetType().GetProperty("Timestamp")!.GetValue(context)!;
            Assert.IsTrue(timestamp >= beforeCapture && timestamp <= afterCapture);
        }

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnMachineName()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            var machineName = (string)context.GetType().GetProperty("MachineName")!.GetValue(context)!;
            Assert.AreEqual(Environment.MachineName, machineName);
        }

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnOSVersion()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            var osVersion = (string)context.GetType().GetProperty("OSVersion")!.GetValue(context)!;
            Assert.AreEqual(Environment.OSVersion.ToString(), osVersion);
        }

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnProcessorCount()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            var processorCount = (int)context.GetType().GetProperty("ProcessorCount")!.GetValue(context)!;
            Assert.AreEqual(Environment.ProcessorCount, processorCount);
        }

        [TestMethod]
        public void CaptureApplicationContext_ShouldReturnWorkingSet()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            var workingSet = (long)context.GetType().GetProperty("WorkingSet")!.GetValue(context)!;
            Assert.AreEqual(Environment.WorkingSet, workingSet);
        }

        #endregion

        #region GenerateUserFriendlyMessage Tests

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithArgumentException_ShouldReturnInputMessage()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument provided");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("Invalid input provided. Please check your data and try again.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithUnauthorizedAccessException_ShouldReturnAccessMessage()
        {
            // Arrange
            var exception = new UnauthorizedAccessException("Access denied");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("Access denied. Please check your permissions.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithFileNotFoundException_ShouldReturnFileMessage()
        {
            // Arrange
            var exception = new FileNotFoundException("File not found");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("Required file not found. Please ensure all files are in place.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithOutOfMemoryException_ShouldReturnMemoryMessage()
        {
            // Arrange
            var exception = new OutOfMemoryException("Out of memory");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("The application is running low on memory. Please close other applications and try again.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithUnknownException_ShouldReturnGenericMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("Some unknown error");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("An unexpected error occurred. Please try again or contact support if the problem persists.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithNullException_ShouldReturnGenericMessage()
        {
            // Arrange
            var exception = new NullReferenceException("Null reference");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("An unexpected error occurred. Please try again or contact support if the problem persists.", message);
        }

        [TestMethod]
        public void GenerateUserFriendlyMessage_WithCustomException_ShouldReturnGenericMessage()
        {
            // Arrange
            var exception = new CustomTestException("Custom error");

            // Act
            var message = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);

            // Assert
            Assert.AreEqual("An unexpected error occurred. Please try again or contact support if the problem persists.", message);
        }

        #endregion

        #region AttemptRecovery Tests

        [TestMethod]
        public void AttemptRecovery_WithAnyException_ShouldLogAttempt()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = _exceptionHandlingService.AttemptRecovery(exception);

            // Assert
            _mockLogService.Verify(x => x.LogInformation(
                It.Is<string>(s => s.Contains("Attempting recovery from exception")), 
                exception.GetType().Name), Times.Once);
        }

        [TestMethod]
        public void AttemptRecovery_WithAnyException_ShouldReturnFalse()
        {
            // Arrange
            var exception = new ArgumentException("Test exception");

            // Act
            var result = _exceptionHandlingService.AttemptRecovery(exception);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AttemptRecovery_WithDifferentExceptionTypes_ShouldLogCorrectType()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("Argument error"),
                new InvalidOperationException("Invalid operation"),
                new FileNotFoundException("File not found")
            };

            // Act & Assert
            foreach (var exception in exceptions)
            {
                _exceptionHandlingService.AttemptRecovery(exception);
                
                _mockLogService.Verify(x => x.LogInformation(
                    It.Is<string>(s => s.Contains("Attempting recovery from exception")), 
                    exception.GetType().Name), Times.Once);
                
                _mockLogService.Reset();
            }
        }

        #endregion

        #region ReportCriticalError Tests

        [TestMethod]
        public void ReportCriticalError_WithExceptionAndState_ShouldLogCritical()
        {
            // Arrange
            var exception = new OutOfMemoryException("Critical memory error");
            var applicationState = new { Status = "Critical", Memory = "Low" };

            // Act
            _exceptionHandlingService.ReportCriticalError(exception, applicationState);

            // Assert
            _mockLogService.Verify(x => x.LogCritical(
                exception, 
                It.Is<string>(s => s.Contains("Critical error reported with application state")), 
                applicationState), Times.Once);
        }

        [TestMethod]
        public void ReportCriticalError_WithNullState_ShouldLogCritical()
        {
            // Arrange
            var exception = new InvalidOperationException("Critical operation error");

            // Act
            _exceptionHandlingService.ReportCriticalError(exception, null!);

            // Assert
            _mockLogService.Verify(x => x.LogCritical(
                exception, 
                It.Is<string>(s => s.Contains("Critical error reported with application state")), 
                It.IsAny<object>()), Times.Once);
        }

        [TestMethod]
        public void ReportCriticalError_WithComplexState_ShouldLogCritical()
        {
            // Arrange
            var exception = new SystemException("System critical error");
            var applicationState = new
            {
                Timestamp = DateTime.UtcNow,
                UserActions = new[] { "Login", "Navigate", "Process" },
                SystemState = new { CPU = 95, Memory = 85, Disk = 70 },
                ErrorCount = 5
            };

            // Act
            _exceptionHandlingService.ReportCriticalError(exception, applicationState);

            // Assert
            _mockLogService.Verify(x => x.LogCritical(
                exception, 
                It.Is<string>(s => s.Contains("Critical error reported with application state")), 
                applicationState), Times.Once);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void ExceptionHandlingWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange
            var exception = new InvalidOperationException("Integration test exception");
            var context = "Integration test context";

            // Act - Simulate a complete exception handling workflow
            _exceptionHandlingService.HandleException(exception, context);
            var applicationContext = _exceptionHandlingService.CaptureApplicationContext();
            var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);
            var recoveryResult = _exceptionHandlingService.AttemptRecovery(exception);
            _exceptionHandlingService.ReportCriticalError(exception, applicationContext);

            // Assert
            _mockLogService.Verify(x => x.LogError(
                exception, 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                context), Times.Once);

            _mockLogService.Verify(x => x.LogInformation(
                It.Is<string>(s => s.Contains("Attempting recovery from exception")), 
                exception.GetType().Name), Times.Once);

            _mockLogService.Verify(x => x.LogCritical(
                exception, 
                It.Is<string>(s => s.Contains("Critical error reported with application state")), 
                applicationContext), Times.Once);

            Assert.IsNotNull(applicationContext);
            Assert.AreEqual("An unexpected error occurred. Please try again or contact support if the problem persists.", userMessage);
            Assert.IsFalse(recoveryResult);
        }

        [TestMethod]
        public void ExceptionHandlingService_ShouldHandleMultipleExceptionsSequentially()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("First exception"),
                new InvalidOperationException("Second exception"),
                new FileNotFoundException("Third exception")
            };

            // Act
            foreach (var exception in exceptions)
            {
                _exceptionHandlingService.HandleException(exception, $"Context for {exception.Message}");
                _exceptionHandlingService.AttemptRecovery(exception);
            }

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Unhandled exception occurred")), 
                It.IsAny<string>()), Times.Exactly(exceptions.Length));

            _mockLogService.Verify(x => x.LogInformation(
                It.Is<string>(s => s.Contains("Attempting recovery from exception")), 
                It.IsAny<string>()), Times.Exactly(exceptions.Length));
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void HandleException_WithNullException_ShouldNotThrow()
        {
            // Act & Assert - Should not throw an exception
            try
            {
                _exceptionHandlingService.HandleException(null!, "test context");
                // If we reach here, the method handled null gracefully
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("HandleException should handle null exceptions gracefully");
            }
        }

        [TestMethod]
        public void AttemptRecovery_WithNullException_ShouldReturnFalse()
        {
            // Act
            var result = _exceptionHandlingService.AttemptRecovery(null!);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ReportCriticalError_WithNullException_ShouldNotThrow()
        {
            // Act & Assert - Should not throw an exception
            try
            {
                _exceptionHandlingService.ReportCriticalError(null!, new { State = "Test" });
                Assert.IsTrue(true);
            }
            catch
            {
                Assert.Fail("ReportCriticalError should handle null exceptions gracefully");
            }
        }

        #endregion
    }

    /// <summary>
    /// Custom exception class for testing
    /// </summary>
    public class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }
}