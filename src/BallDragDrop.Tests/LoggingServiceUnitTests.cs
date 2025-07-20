using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BallDragDrop.Services;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class LoggingServiceUnitTests
    {
        private Mock<ILog> _mockLogger = null!;
        private ILogService _logService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogger = new Mock<ILog>();
            
            // Setup default logger behavior
            _mockLogger.Setup(x => x.IsDebugEnabled).Returns(true);
            _mockLogger.Setup(x => x.IsInfoEnabled).Returns(true);
            _mockLogger.Setup(x => x.IsWarnEnabled).Returns(true);
            _mockLogger.Setup(x => x.IsErrorEnabled).Returns(true);
            _mockLogger.Setup(x => x.IsFatalEnabled).Returns(true);
            
            _logService = new TestableLog4NetService(_mockLogger.Object);
        }

        #region Standard Logging Methods Tests

        [TestMethod]
        public void LogTrace_ShouldCallDebugWithTracePrefix()
        {
            // Arrange
            var message = "Test trace message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogTrace(message, args);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("[TRACE]") && s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogDebug_ShouldCallDebugFormat()
        {
            // Arrange
            var message = "Test debug message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogDebug(message, args);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogInformation_ShouldCallInfoFormat()
        {
            // Arrange
            var message = "Test info message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogInformation(message, args);

            // Assert
            _mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogWarning_ShouldCallWarnFormat()
        {
            // Arrange
            var message = "Test warning message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogWarning(message, args);

            // Assert
            _mockLogger.Verify(x => x.WarnFormat(
                It.Is<string>(s => s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogError_WithMessage_ShouldCallErrorFormat()
        {
            // Arrange
            var message = "Test error message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogError(message, args);

            // Assert
            _mockLogger.Verify(x => x.ErrorFormat(
                It.Is<string>(s => s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogError_WithException_ShouldCallErrorWithException()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var message = "Test error message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogError(exception, message, args);

            // Assert
            _mockLogger.Verify(x => x.Error(
                It.Is<string>(s => s.Contains(message)), 
                exception), Times.Once);
        }

        [TestMethod]
        public void LogCritical_WithMessage_ShouldCallFatalFormat()
        {
            // Arrange
            var message = "Test critical message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogCritical(message, args);

            // Assert
            _mockLogger.Verify(x => x.FatalFormat(
                It.Is<string>(s => s.Contains(message)), 
                args), Times.Once);
        }

        [TestMethod]
        public void LogCritical_WithException_ShouldCallFatalWithException()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var message = "Test critical message";
            var args = new object[] { "arg1", 42 };

            // Act
            _logService.LogCritical(exception, message, args);

            // Assert
            _mockLogger.Verify(x => x.Fatal(
                It.Is<string>(s => s.Contains(message)), 
                exception), Times.Once);
        }

        #endregion

        #region Structured Logging Tests

        [TestMethod]
        public void LogStructured_WithDebugLevel_ShouldCallDebugFormat()
        {
            // Arrange
            var messageTemplate = "User {0} performed action {1}";
            var propertyValues = new object[] { 123, "Login" };

            // Act
            _logService.LogStructured(LogLevel.Debug, messageTemplate, propertyValues);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("User 123 performed action Login")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void LogStructured_WithInformationLevel_ShouldCallInfoFormat()
        {
            // Arrange
            var messageTemplate = "Operation {0} completed in {1}ms";
            var propertyValues = new object[] { "DataLoad", 150 };

            // Act
            _logService.LogStructured(LogLevel.Information, messageTemplate, propertyValues);

            // Assert
            _mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains("Operation DataLoad completed in 150ms")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void LogStructured_WithException_ShouldCallErrorWithException()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument");
            var messageTemplate = "Error processing {0}";
            var propertyValues = new object[] { "User" };

            // Act
            _logService.LogStructured(LogLevel.Error, exception, messageTemplate, propertyValues);

            // Assert
            _mockLogger.Verify(x => x.Error(
                It.Is<string>(s => s.Contains("Error processing User")), 
                exception), Times.Once);
        }

        #endregion

        #region Method Logging Tests

        [TestMethod]
        public void LogMethodEntry_ShouldLogDebugWithMethodNameAndParameters()
        {
            // Arrange
            var methodName = "TestMethod";
            var parameters = new object[] { "param1", 42, true };

            // Act
            _logService.LogMethodEntry(methodName, parameters);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Entering method") && s.Contains(methodName)), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void LogMethodExit_WithoutDuration_ShouldLogDebugWithMethodNameAndReturnValue()
        {
            // Arrange
            var methodName = "TestMethod";
            var returnValue = "result";

            // Act
            _logService.LogMethodExit(methodName, returnValue);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Exiting method") && s.Contains(methodName) && s.Contains("result")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void LogMethodExit_WithDuration_ShouldLogDebugWithDuration()
        {
            // Arrange
            var methodName = "TestMethod";
            var returnValue = "result";
            var duration = TimeSpan.FromMilliseconds(150);

            // Act
            _logService.LogMethodExit(methodName, returnValue, duration);

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Exiting method") && s.Contains(methodName) && s.Contains("150")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void BeginScope_ShouldReturnDisposableAndLogEntry()
        {
            // Arrange
            var scopeName = "TestScope";
            var parameters = new object[] { "param1", 42 };

            // Act
            var scope = _logService.BeginScope(scopeName, parameters);

            // Assert
            Assert.IsNotNull(scope);
            Assert.IsInstanceOfType(scope, typeof(IDisposable));
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Entering scope") && s.Contains(scopeName)), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void BeginScope_WhenDisposed_ShouldLogExit()
        {
            // Arrange
            var scopeName = "TestScope";

            // Act
            using (var scope = _logService.BeginScope(scopeName))
            {
                // Scope is active
            }

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Exiting scope") && s.Contains(scopeName)), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion

        #region Performance Logging Tests

        [TestMethod]
        public void LogPerformance_ShouldLogInformationWithDurationAndAdditionalData()
        {
            // Arrange
            var operationName = "DatabaseQuery";
            var duration = TimeSpan.FromMilliseconds(250);
            var additionalData = new object[] { "Table: Users", "Rows: 150" };

            // Act
            _logService.LogPerformance(operationName, duration, additionalData);

            // Assert
            _mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains("Performance") && s.Contains(operationName) && s.Contains("250")), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion

        #region Correlation ID Tests

        [TestMethod]
        public void SetCorrelationId_ShouldUpdateCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-123";

            // Act
            _logService.SetCorrelationId(correlationId);
            var result = _logService.GetCorrelationId();

            // Assert
            Assert.AreEqual(correlationId, result);
        }

        [TestMethod]
        public void SetCorrelationId_WithNull_ShouldGenerateNewGuid()
        {
            // Arrange
            var originalId = _logService.GetCorrelationId();

            // Act
            _logService.SetCorrelationId(null!);
            var newId = _logService.GetCorrelationId();

            // Assert
            Assert.AreNotEqual(originalId, newId);
            Assert.IsTrue(Guid.TryParse(newId, out _));
        }

        [TestMethod]
        public void GetCorrelationId_ShouldReturnValidGuid()
        {
            // Act
            var correlationId = _logService.GetCorrelationId();

            // Assert
            Assert.IsNotNull(correlationId);
            Assert.IsTrue(Guid.TryParse(correlationId, out _));
        }

        [TestMethod]
        public void LogMessages_ShouldIncludeCorrelationId()
        {
            // Arrange
            var correlationId = "test-correlation-456";
            _logService.SetCorrelationId(correlationId);

            // Act
            _logService.LogInformation("Test message");

            // Assert
            _mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains(correlationId)), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void LogService_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new Task[10];
            var exceptions = new List<Exception>();

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                int taskId = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            _logService.SetCorrelationId($"task-{taskId}-iteration-{j}");
                            _logService.LogInformation("Thread safety test message {0} {1}", taskId, j);
                            _logService.LogDebug("Debug message from task {0}", taskId);
                            
                            using (var scope = _logService.BeginScope($"Scope-{taskId}-{j}"))
                            {
                                _logService.LogTrace("Trace message in scope");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Thread safety test failed with {exceptions.Count} exceptions");
        }

        #endregion

        #region Logger Level Checks Tests

        [TestMethod]
        public void LogTrace_WhenDebugDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsDebugEnabled).Returns(false);

            // Act
            _logService.LogTrace("Test message");

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void LogDebug_WhenDebugDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsDebugEnabled).Returns(false);

            // Act
            _logService.LogDebug("Test message");

            // Assert
            _mockLogger.Verify(x => x.DebugFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void LogInformation_WhenInfoDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsInfoEnabled).Returns(false);

            // Act
            _logService.LogInformation("Test message");

            // Assert
            _mockLogger.Verify(x => x.InfoFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void LogWarning_WhenWarnDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsWarnEnabled).Returns(false);

            // Act
            _logService.LogWarning("Test message");

            // Assert
            _mockLogger.Verify(x => x.WarnFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void LogError_WhenErrorDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsErrorEnabled).Returns(false);

            // Act
            _logService.LogError("Test message");

            // Assert
            _mockLogger.Verify(x => x.ErrorFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void LogCritical_WhenFatalDisabled_ShouldNotCallLogger()
        {
            // Arrange
            _mockLogger.Setup(x => x.IsFatalEnabled).Returns(false);

            // Act
            _logService.LogCritical("Test message");

            // Assert
            _mockLogger.Verify(x => x.FatalFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void LogMethods_WithNullMessage_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogTrace(null!);
            _logService.LogDebug(null!);
            _logService.LogInformation(null!);
            _logService.LogWarning(null!);
            _logService.LogError(null!);
            _logService.LogCritical(null!);
        }

        [TestMethod]
        public void LogMethods_WithEmptyMessage_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogTrace("");
            _logService.LogDebug("");
            _logService.LogInformation("");
            _logService.LogWarning("");
            _logService.LogError("");
            _logService.LogCritical("");
        }

        [TestMethod]
        public void LogMethods_WithNullArgs_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogTrace("Test message", null!);
            _logService.LogDebug("Test message", null!);
            _logService.LogInformation("Test message", null!);
            _logService.LogWarning("Test message", null!);
            _logService.LogError("Test message", null!);
            _logService.LogCritical("Test message", null!);
        }

        [TestMethod]
        public void BeginScope_WithNullScopeName_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            using (var scope = _logService.BeginScope(null!))
            {
                Assert.IsNotNull(scope);
            }
        }

        [TestMethod]
        public void LogMethodEntry_WithNullMethodName_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogMethodEntry(null!, "param1", "param2");
        }

        [TestMethod]
        public void LogMethodExit_WithNullMethodName_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogMethodExit(null!, "returnValue", TimeSpan.FromMilliseconds(100));
        }

        #endregion
    }

    /// <summary>
    /// Testable version of Log4NetService that allows dependency injection of ILog
    /// </summary>
    public class TestableLog4NetService : ILogService
    {
        private readonly ILog _logger;
        private string _correlationId = Guid.NewGuid().ToString();

        public TestableLog4NetService(ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogTrace(string message, params object[] args)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.DebugFormat($"[TRACE] [{_correlationId}] {message}", args);
            }
        }

        public void LogDebug(string message, params object[] args)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.DebugFormat($"[{_correlationId}] {message}", args);
            }
        }

        public void LogInformation(string message, params object[] args)
        {
            if (_logger.IsInfoEnabled)
            {
                _logger.InfoFormat($"[{_correlationId}] {message}", args);
            }
        }

        public void LogWarning(string message, params object[] args)
        {
            if (_logger.IsWarnEnabled)
            {
                _logger.WarnFormat($"[{_correlationId}] {message}", args);
            }
        }

        public void LogError(string message, params object[] args)
        {
            if (_logger.IsErrorEnabled)
            {
                _logger.ErrorFormat($"[{_correlationId}] {message}", args);
            }
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            if (_logger.IsErrorEnabled)
            {
                _logger.Error(string.Format($"[{_correlationId}] {message}", args), exception);
            }
        }

        public void LogCritical(string message, params object[] args)
        {
            if (_logger.IsFatalEnabled)
            {
                _logger.FatalFormat($"[{_correlationId}] {message}", args);
            }
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            if (_logger.IsFatalEnabled)
            {
                _logger.Fatal(string.Format($"[{_correlationId}] {message}", args), exception);
            }
        }

        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues)
        {
            var message = string.Format(messageTemplate, propertyValues);
            switch (level)
            {
                case LogLevel.Trace:
                    LogTrace(message);
                    break;
                case LogLevel.Debug:
                    LogDebug(message);
                    break;
                case LogLevel.Information:
                    LogInformation(message);
                    break;
                case LogLevel.Warning:
                    LogWarning(message);
                    break;
                case LogLevel.Error:
                    LogError(message);
                    break;
                case LogLevel.Critical:
                    LogCritical(message);
                    break;
            }
        }

        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            var message = string.Format(messageTemplate, propertyValues);
            switch (level)
            {
                case LogLevel.Error:
                    LogError(exception, message);
                    break;
                case LogLevel.Critical:
                    LogCritical(exception, message);
                    break;
                default:
                    LogStructured(level, $"{message} Exception: {exception}");
                    break;
            }
        }

        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            LogDebug("Entering scope: {0} with parameters: {1}", scopeName, string.Join(", ", parameters));
            return new LogScope(this, scopeName);
        }

        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            LogDebug("Entering method: {0} with parameters: {1}", methodName, string.Join(", ", parameters));
        }

        public void LogMethodExit(string methodName, object? returnValue = null, TimeSpan? duration = null)
        {
            if (duration.HasValue)
            {
                LogDebug("Exiting method: {0} with return value: {1} (Duration: {2}ms)", 
                    methodName, returnValue, duration.Value.TotalMilliseconds);
            }
            else
            {
                LogDebug("Exiting method: {0} with return value: {1}", methodName, returnValue);
            }
        }

        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData)
        {
            LogInformation("Performance: {0} completed in {1}ms. Additional data: {2}", 
                operationName, duration.TotalMilliseconds, string.Join(", ", additionalData));
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? Guid.NewGuid().ToString();
        }

        public string GetCorrelationId()
        {
            return _correlationId;
        }

        private class LogScope : IDisposable
        {
            private readonly TestableLog4NetService _logService;
            private readonly string _scopeName;

            public LogScope(TestableLog4NetService logService, string scopeName)
            {
                _logService = logService;
                _scopeName = scopeName;
            }

            public void Dispose()
            {
                _logService.LogDebug("Exiting scope: {0}", _scopeName);
            }
        }
    }
}