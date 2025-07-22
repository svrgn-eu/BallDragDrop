using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BallDragDrop.Contracts;
using BallDragDrop.Services;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class LoggingIntegrationTests
    {
        private ILogService _logService = null!;
        private string _testLogDirectory = null!;
        private string _testLogFile = null!;
        private MemoryAppender _memoryAppender = null!;
        private ILoggerRepository _repository = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create test log directory
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "BallDragDropTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testLogDirectory);
            _testLogFile = Path.Combine(_testLogDirectory, "test.log");

            // Configure Log4NET for testing
            _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
            ConfigureLog4NetForTesting();

            // Create the log service
            _logService = new Log4NetService();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test files
            try
            {
                if (Directory.Exists(_testLogDirectory))
                {
                    Directory.Delete(_testLogDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Shutdown log4net
            _repository?.Shutdown();
        }

        private void ConfigureLog4NetForTesting()
        {
            // Create memory appender for in-memory testing
            _memoryAppender = new MemoryAppender();
            _memoryAppender.Layout = new PatternLayout("%date [%thread] %-5level %logger - %message%newline");
            _memoryAppender.ActivateOptions();

            // Create file appender for file testing
            var fileAppender = new FileAppender
            {
                File = _testLogFile,
                AppendToFile = true,
                Layout = new PatternLayout("%date [%thread] %-5level %logger - %message%newline"),
                Threshold = Level.Info
            };
            fileAppender.ActivateOptions();

            // Create console appender for console testing
            var consoleAppender = new ConsoleAppender
            {
                Layout = new PatternLayout("%date %-5level - %message%newline"),
                Threshold = Level.Debug
            };
            consoleAppender.ActivateOptions();

            // Configure the repository
            BasicConfigurator.Configure(_repository, _memoryAppender, fileAppender, consoleAppender);
        }

        #region End-to-End Logging Flow Tests

        [TestMethod]
        public void LoggingFlow_ShouldReachAllConfiguredOutputs()
        {
            // Arrange
            var testMessage = "Integration test message";
            var correlationId = Guid.NewGuid().ToString();

            // Act
            _logService.SetCorrelationId(correlationId);
            _logService.LogInformation(testMessage);
            _logService.LogError("Test error message");
            _logService.LogDebug("Test debug message");

            // Wait for async operations to complete
            Thread.Sleep(100);

            // Assert - Check memory appender
            var logEvents = _memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Length >= 3, "Expected at least 3 log events in memory appender");
            
            var infoEvent = logEvents.FirstOrDefault(e => e.Level == Level.Info);
            Assert.IsNotNull(infoEvent, "Info log event should be present");
            Assert.IsTrue(infoEvent.RenderedMessage.Contains(testMessage), "Info message should contain test message");
            Assert.IsTrue(infoEvent.RenderedMessage.Contains(correlationId), "Info message should contain correlation ID");

            // Assert - Check file output (INFO level and above)
            Assert.IsTrue(File.Exists(_testLogFile), "Log file should be created");
            var fileContent = File.ReadAllText(_testLogFile);
            Assert.IsTrue(fileContent.Contains(testMessage), "File should contain info message");
            Assert.IsTrue(fileContent.Contains("Test error message"), "File should contain error message");
            Assert.IsFalse(fileContent.Contains("Test debug message"), "File should not contain debug message (below threshold)");
        }

        [TestMethod]
        public void StructuredLogging_ShouldPreserveStructuredData()
        {
            // Arrange
            var operationName = "TestOperation";
            var userId = 12345;
            var duration = TimeSpan.FromMilliseconds(250);

            // Act
            _logService.LogStructured(LogLevel.Information, "Operation {0} completed for user {1} in {2}ms", 
                operationName, userId, duration.TotalMilliseconds);

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var structuredEvent = logEvents.FirstOrDefault(e => e.Level == Level.Info);
            Assert.IsNotNull(structuredEvent, "Structured log event should be present");
            Assert.IsTrue(structuredEvent.RenderedMessage.Contains(operationName), "Should contain operation name");
            Assert.IsTrue(structuredEvent.RenderedMessage.Contains(userId.ToString()), "Should contain user ID");
            Assert.IsTrue(structuredEvent.RenderedMessage.Contains("250"), "Should contain duration");
        }

        [TestMethod]
        public void PerformanceLogging_ShouldCaptureMetrics()
        {
            // Arrange
            var operationName = "DatabaseQuery";
            var duration = TimeSpan.FromMilliseconds(150);
            var additionalData = new object[] { "Table: Users", "Rows: 100" };

            // Act
            _logService.LogPerformance(operationName, duration, additionalData);

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var perfEvent = logEvents.FirstOrDefault(e => e.Level == Level.Info && 
                e.RenderedMessage.Contains("Performance"));
            Assert.IsNotNull(perfEvent, "Performance log event should be present");
            Assert.IsTrue(perfEvent.RenderedMessage.Contains(operationName), "Should contain operation name");
            Assert.IsTrue(perfEvent.RenderedMessage.Contains("150"), "Should contain duration");
            Assert.IsTrue(perfEvent.RenderedMessage.Contains("Table: Users"), "Should contain additional data");
        }

        [TestMethod]
        public void MethodLogging_ShouldCaptureEntryAndExit()
        {
            // Arrange
            var methodName = "TestMethod";
            var parameters = new object[] { "param1", 42, true };
            var returnValue = "success";
            var duration = TimeSpan.FromMilliseconds(75);

            // Act
            _logService.LogMethodEntry(methodName, parameters);
            _logService.LogMethodExit(methodName, returnValue, duration);

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var entryEvent = logEvents.FirstOrDefault(e => e.RenderedMessage.Contains("Entering method"));
            var exitEvent = logEvents.FirstOrDefault(e => e.RenderedMessage.Contains("Exiting method"));

            Assert.IsNotNull(entryEvent, "Method entry event should be present");
            Assert.IsNotNull(exitEvent, "Method exit event should be present");
            Assert.IsTrue(entryEvent.RenderedMessage.Contains(methodName), "Entry should contain method name");
            Assert.IsTrue(exitEvent.RenderedMessage.Contains(methodName), "Exit should contain method name");
            Assert.IsTrue(exitEvent.RenderedMessage.Contains("success"), "Exit should contain return value");
            Assert.IsTrue(exitEvent.RenderedMessage.Contains("75"), "Exit should contain duration");
        }

        [TestMethod]
        public void ScopeLogging_ShouldTrackScopeLifecycle()
        {
            // Arrange
            var scopeName = "TestScope";
            var parameters = new object[] { "scopeParam1", "scopeParam2" };

            // Act
            using (var scope = _logService.BeginScope(scopeName, parameters))
            {
                _logService.LogInformation("Message within scope");
            }

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var enterEvent = logEvents.FirstOrDefault(e => e.RenderedMessage.Contains("Entering scope"));
            var exitEvent = logEvents.FirstOrDefault(e => e.RenderedMessage.Contains("Exiting scope"));

            Assert.IsNotNull(enterEvent, "Scope entry event should be present");
            Assert.IsNotNull(exitEvent, "Scope exit event should be present");
            Assert.IsTrue(enterEvent.RenderedMessage.Contains(scopeName), "Entry should contain scope name");
            Assert.IsTrue(exitEvent.RenderedMessage.Contains(scopeName), "Exit should contain scope name");
        }

        #endregion

        #region Performance Under Load Tests

        [TestMethod]
        public void HighVolumeLogging_ShouldMaintainPerformance()
        {
            // Arrange
            const int messageCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                _logService.LogInformation("High volume test message {0}", i);
                if (i % 100 == 0)
                {
                    _logService.LogDebug("Debug message {0}", i);
                    _logService.LogWarning("Warning message {0}", i);
                }
            }

            stopwatch.Stop();

            // Wait for all async operations to complete
            Thread.Sleep(500);

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
                $"High volume logging took too long: {stopwatch.ElapsedMilliseconds}ms");

            var logEvents = _memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Length >= messageCount, 
                $"Expected at least {messageCount} log events, got {logEvents.Length}");
        }

        [TestMethod]
        public void ConcurrentLogging_ShouldBeThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int messagesPerThread = 100;
            var tasks = new Task[threadCount];
            var exceptions = new List<Exception>();

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < messagesPerThread; j++)
                        {
                            _logService.SetCorrelationId($"thread-{threadId}-msg-{j}");
                            _logService.LogInformation("Concurrent message from thread {0}, iteration {1}", threadId, j);
                            
                            if (j % 10 == 0)
                            {
                                _logService.LogDebug("Debug from thread {0}", threadId);
                                using (var scope = _logService.BeginScope($"Scope-{threadId}-{j}"))
                                {
                                    _logService.LogTrace("Trace in scope");
                                }
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

            // Wait for all async operations to complete
            Thread.Sleep(1000);

            // Assert
            Assert.AreEqual(0, exceptions.Count, 
                $"Concurrent logging failed with {exceptions.Count} exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");

            var logEvents = _memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Length >= threadCount * messagesPerThread, 
                $"Expected at least {threadCount * messagesPerThread} log events, got {logEvents.Length}");
        }

        [TestMethod]
        public void AsyncLogging_ShouldNotBlockCallingThread()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Log many messages quickly
            for (int i = 0; i < 100; i++)
            {
                _logService.LogInformation("Async test message {0}", i);
                _logService.LogDebug("Debug message {0}", i);
                _logService.LogError("Error message {0}", i);
            }

            stopwatch.Stop();

            // Assert - Should complete quickly (not waiting for I/O)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Async logging blocked calling thread for {stopwatch.ElapsedMilliseconds}ms");

            // Wait for async operations to complete
            Thread.Sleep(500);

            // Verify messages were logged
            var logEvents = _memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Length >= 200, "Expected at least 200 log events");
        }

        #endregion

        #region Log File Rotation Tests

        [TestMethod]
        public void LogFileRotation_ShouldCreateNewFilesWhenNeeded()
        {
            // Note: This is a simplified test since we're using FileAppender instead of RollingFileAppender
            // In a real implementation, we would configure RollingFileAppender for rotation
            
            // Arrange
            var largeMessage = new string('X', 1000); // 1KB message

            // Act - Write many large messages
            for (int i = 0; i < 100; i++)
            {
                _logService.LogInformation("Large message {0}: {1}", i, largeMessage);
            }

            // Wait for file operations
            Thread.Sleep(200);

            // Assert
            Assert.IsTrue(File.Exists(_testLogFile), "Log file should exist");
            var fileInfo = new FileInfo(_testLogFile);
            Assert.IsTrue(fileInfo.Length > 50000, "Log file should be substantial size"); // At least 50KB
        }

        #endregion

        #region Error Handling in Logging Tests

        [TestMethod]
        public void LoggingWithExceptions_ShouldHandleGracefully()
        {
            // Arrange
            var testException = new InvalidOperationException("Test exception for logging");

            // Act
            _logService.LogError(testException, "Error occurred during operation {0}", "TestOperation");
            _logService.LogCritical(testException, "Critical error in component {0}", "TestComponent");

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var errorEvent = logEvents.FirstOrDefault(e => e.Level == Level.Error);
            var criticalEvent = logEvents.FirstOrDefault(e => e.Level == Level.Fatal);

            Assert.IsNotNull(errorEvent, "Error event should be present");
            Assert.IsNotNull(criticalEvent, "Critical event should be present");
            Assert.IsNotNull(errorEvent.ExceptionObject, "Error event should have exception");
            Assert.IsNotNull(criticalEvent.ExceptionObject, "Critical event should have exception");
        }

        [TestMethod]
        public void LoggingWithNullValues_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            _logService.LogInformation(null!);
            _logService.LogDebug("Message with null parameter: {0}", (object)null!);
            _logService.LogError((Exception)null!, "Error with null exception");
            _logService.SetCorrelationId(null!);

            // Wait for async operations
            Thread.Sleep(50);

            // Verify logging continued to work
            _logService.LogInformation("Test message after null handling");
            var logEvents = _memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Length > 0, "Logging should continue after null handling");
        }

        #endregion

        #region Correlation ID Tests

        [TestMethod]
        public void CorrelationId_ShouldPersistAcrossLogCalls()
        {
            // Arrange
            var correlationId = "test-correlation-123";

            // Act
            _logService.SetCorrelationId(correlationId);
            _logService.LogInformation("First message");
            _logService.LogDebug("Second message");
            _logService.LogWarning("Third message");

            // Wait for async operations
            Thread.Sleep(50);

            // Assert
            var logEvents = _memoryAppender.GetEvents();
            var infoEvents = logEvents.Where(e => e.Level == Level.Info || e.Level == Level.Debug || e.Level == Level.Warn).ToArray();
            
            Assert.IsTrue(infoEvents.Length >= 3, "Should have at least 3 log events");
            foreach (var logEvent in infoEvents)
            {
                Assert.IsTrue(logEvent.RenderedMessage.Contains(correlationId), 
                    $"Log event should contain correlation ID: {logEvent.RenderedMessage}");
            }
        }

        [TestMethod]
        public void CorrelationId_ShouldBeUniqueWhenNotSet()
        {
            // Act
            var id1 = _logService.GetCorrelationId();
            
            // Create new service instance
            var logService2 = new Log4NetService();
            var id2 = logService2.GetCorrelationId();

            // Assert
            Assert.AreNotEqual(id1, id2, "Different service instances should have different correlation IDs");
            Assert.IsTrue(Guid.TryParse(id1, out _), "Correlation ID should be a valid GUID");
            Assert.IsTrue(Guid.TryParse(id2, out _), "Correlation ID should be a valid GUID");
        }

        #endregion
    }
}
