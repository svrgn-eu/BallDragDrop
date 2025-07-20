using System;
using BallDragDrop.Services;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class SimpleLoggingTests
    {
        [TestMethod]
        public void TestLogServiceInterface_BasicFunctionality()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsDebugEnabled).Returns(true);
            mockLogger.Setup(x => x.IsInfoEnabled).Returns(true);
            mockLogger.Setup(x => x.IsWarnEnabled).Returns(true);
            mockLogger.Setup(x => x.IsErrorEnabled).Returns(true);
            mockLogger.Setup(x => x.IsFatalEnabled).Returns(true);

            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act & Assert - Test basic logging methods
            logService.LogTrace("Test trace");
            logService.LogDebug("Test debug");
            logService.LogInformation("Test info");
            logService.LogWarning("Test warning");
            logService.LogError("Test error");
            logService.LogCritical("Test critical");

            // Verify calls were made
            mockLogger.Verify(x => x.DebugFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeast(2));
            mockLogger.Verify(x => x.InfoFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(x => x.WarnFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(x => x.ErrorFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(x => x.FatalFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void TestLogServiceInterface_CorrelationId()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsInfoEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act
            var originalId = logService.GetCorrelationId();
            logService.SetCorrelationId("test-123");
            var newId = logService.GetCorrelationId();

            // Assert
            Assert.AreNotEqual(originalId, newId);
            Assert.AreEqual("test-123", newId);
            Assert.IsTrue(Guid.TryParse(originalId, out _));
        }

        [TestMethod]
        public void TestLogServiceInterface_ExceptionLogging()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsErrorEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);
            var exception = new InvalidOperationException("Test exception");

            // Act
            logService.LogError(exception, "Error occurred");

            // Assert
            mockLogger.Verify(x => x.Error(It.IsAny<string>(), exception), Times.Once);
        }

        [TestMethod]
        public void TestLogServiceInterface_MethodLogging()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsDebugEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act
            logService.LogMethodEntry("TestMethod", "param1", 42);
            logService.LogMethodExit("TestMethod", "result", TimeSpan.FromMilliseconds(100));

            // Assert
            mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Entering method")), 
                It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Exiting method")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void TestLogServiceInterface_ScopeLogging()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsDebugEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act
            using (var scope = logService.BeginScope("TestScope"))
            {
                Assert.IsNotNull(scope);
            }

            // Assert
            mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Entering scope")), 
                It.IsAny<object[]>()), Times.Once);
            mockLogger.Verify(x => x.DebugFormat(
                It.Is<string>(s => s.Contains("Exiting scope")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void TestLogServiceInterface_PerformanceLogging()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsInfoEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act
            logService.LogPerformance("TestOperation", TimeSpan.FromMilliseconds(250), "additional", "data");

            // Assert
            mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains("Performance") && s.Contains("TestOperation") && s.Contains("250")), 
                It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        public void TestLogServiceInterface_StructuredLogging()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            mockLogger.Setup(x => x.IsInfoEnabled).Returns(true);
            var logService = new SimpleTestLogService(mockLogger.Object);

            // Act
            logService.LogStructured(LogLevel.Information, "User {0} performed {1}", "John", "Login");

            // Assert
            mockLogger.Verify(x => x.InfoFormat(
                It.Is<string>(s => s.Contains("User John performed Login")), 
                It.IsAny<object[]>()), Times.Once);
        }
    }

    /// <summary>
    /// Simple test implementation of ILogService for testing
    /// </summary>
    public class SimpleTestLogService : ILogService
    {
        private readonly ILog _logger;
        private string _correlationId = Guid.NewGuid().ToString();

        public SimpleTestLogService(ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogTrace(string message, params object[] args)
        {
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat($"[TRACE] [{_correlationId}] {message}", args);
        }

        public void LogDebug(string message, params object[] args)
        {
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat($"[{_correlationId}] {message}", args);
        }

        public void LogInformation(string message, params object[] args)
        {
            if (_logger.IsInfoEnabled)
                _logger.InfoFormat($"[{_correlationId}] {message}", args);
        }

        public void LogWarning(string message, params object[] args)
        {
            if (_logger.IsWarnEnabled)
                _logger.WarnFormat($"[{_correlationId}] {message}", args);
        }

        public void LogError(string message, params object[] args)
        {
            if (_logger.IsErrorEnabled)
                _logger.ErrorFormat($"[{_correlationId}] {message}", args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            if (_logger.IsErrorEnabled)
                _logger.Error(string.Format($"[{_correlationId}] {message}", args), exception);
        }

        public void LogCritical(string message, params object[] args)
        {
            if (_logger.IsFatalEnabled)
                _logger.FatalFormat($"[{_correlationId}] {message}", args);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            if (_logger.IsFatalEnabled)
                _logger.Fatal(string.Format($"[{_correlationId}] {message}", args), exception);
        }

        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues)
        {
            var message = string.Format(messageTemplate, propertyValues);
            switch (level)
            {
                case LogLevel.Trace: LogTrace(message); break;
                case LogLevel.Debug: LogDebug(message); break;
                case LogLevel.Information: LogInformation(message); break;
                case LogLevel.Warning: LogWarning(message); break;
                case LogLevel.Error: LogError(message); break;
                case LogLevel.Critical: LogCritical(message); break;
            }
        }

        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            var message = string.Format(messageTemplate, propertyValues);
            switch (level)
            {
                case LogLevel.Error: LogError(exception, message); break;
                case LogLevel.Critical: LogCritical(exception, message); break;
                default: LogStructured(level, $"{message} Exception: {exception}"); break;
            }
        }

        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            LogDebug("Entering scope: {0} with parameters: {1}", scopeName, string.Join(", ", parameters));
            return new SimpleLogScope(this, scopeName);
        }

        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            LogDebug("Entering method: {0} with parameters: {1}", methodName, string.Join(", ", parameters));
        }

        public void LogMethodExit(string methodName, object? returnValue = null, TimeSpan? duration = null)
        {
            if (duration.HasValue)
                LogDebug("Exiting method: {0} with return value: {1} (Duration: {2}ms)", 
                    methodName, returnValue, duration.Value.TotalMilliseconds);
            else
                LogDebug("Exiting method: {0} with return value: {1}", methodName, returnValue);
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

        private class SimpleLogScope : IDisposable
        {
            private readonly SimpleTestLogService _logService;
            private readonly string _scopeName;

            public SimpleLogScope(SimpleTestLogService logService, string scopeName)
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