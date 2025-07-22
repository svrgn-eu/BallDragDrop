using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BallDragDrop.Contracts;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class ExceptionHandlingIntegrationTests
    {
        private Mock<ILogService> _mockLogService = null!;
        private IExceptionHandlingService _exceptionHandlingService = null!;
        private List<string> _loggedMessages = null!;
        private List<Exception> _loggedException = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _loggedMessages = new List<string>();
            _loggedException = new List<Exception>();

            // Setup mock to capture logged messages and exceptions
            _mockLogService.Setup(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<Exception, string, object[]>((ex, msg, args) =>
                {
                    _loggedException.Add(ex);
                    _loggedMessages.Add(string.Format(msg, args));
                });

            _mockLogService.Setup(x => x.LogCritical(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<Exception, string, object[]>((ex, msg, args) =>
                {
                    _loggedException.Add(ex);
                    _loggedMessages.Add(string.Format(msg, args));
                });

            _mockLogService.Setup(x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>((msg, args) =>
                {
                    _loggedMessages.Add(string.Format(msg, args));
                });

            _mockLogService.Setup(x => x.LogWarning(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>((msg, args) =>
                {
                    _loggedMessages.Add(string.Format(msg, args));
                });

            _exceptionHandlingService = new ExceptionHandlingService(_mockLogService.Object);
        }

        #region UI Thread Exception Handling Tests

        [TestMethod]
        [STAThread]
        public void DispatcherUnhandledException_ShouldBeHandledGracefully()
        {
            // Arrange
            var testException = new InvalidOperationException("Test UI thread exception");
            var handled = false;
            var userMessageShown = false;

            // Create a test dispatcher
            var dispatcher = Dispatcher.CurrentDispatcher;

            // Act
            dispatcher.UnhandledException += (sender, e) =>
            {
                // Simulate the App.xaml.cs exception handler
                _exceptionHandlingService.HandleException(e.Exception, "UI thread exception");
                var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(e.Exception);
                var recovered = _exceptionHandlingService.AttemptRecovery(e.Exception);

                // Simulate showing message to user (without actual MessageBox)
                userMessageShown = !string.IsNullOrEmpty(userMessage);
                handled = true;
                e.Handled = true;
            };

            // Simulate an unhandled exception in the UI thread
            dispatcher.BeginInvoke(new Action(() =>
            {
                throw testException;
            }));

            // Wait for the exception to be processed
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);

            // Assert
            Assert.IsTrue(handled, "Exception should have been handled");
            Assert.IsTrue(userMessageShown, "User message should have been generated");
            Assert.IsTrue(_loggedException.Contains(testException), "Exception should have been logged");
            Assert.IsTrue(_loggedMessages.Any(m => m.Contains("Unhandled exception occurred")), 
                "Error message should have been logged");
        }

        [TestMethod]
        public void UIThreadExceptionRecovery_ShouldMaintainApplicationStability()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("Invalid argument"),
                new InvalidOperationException("Invalid operation"),
                new NullReferenceException("Null reference")
            };

            var recoveryAttempts = 0;
            var applicationStable = true;

            // Act
            foreach (var exception in exceptions)
            {
                try
                {
                    _exceptionHandlingService.HandleException(exception, "UI stability test");
                    var recovered = _exceptionHandlingService.AttemptRecovery(exception);
                    recoveryAttempts++;
                }
                catch (Exception)
                {
                    applicationStable = false;
                }
            }

            // Assert
            Assert.IsTrue(applicationStable, "Application should remain stable after multiple exceptions");
            Assert.AreEqual(exceptions.Length, recoveryAttempts, "Recovery should be attempted for each exception");
            Assert.AreEqual(exceptions.Length, _loggedException.Count, "All exceptions should be logged");
        }

        #endregion

        #region Background Thread Exception Handling Tests

        [TestMethod]
        public void BackgroundThreadException_ShouldBeHandledWithoutCrashing()
        {
            // Arrange
            var testException = new InvalidOperationException("Background thread exception");
            var exceptionHandled = false;
            var manualResetEvent = new ManualResetEventSlim(false);

            // Act
            Task.Run(() =>
            {
                try
                {
                    // Simulate background work that throws an exception
                    throw testException;
                }
                catch (Exception ex)
                {
                    // Simulate the CurrentDomain_UnhandledException handler
                    _exceptionHandlingService.HandleException(ex, "Background thread exception");
                    exceptionHandled = true;
                    manualResetEvent.Set();
                }
            });

            // Wait for the background task to complete
            var completed = manualResetEvent.Wait(TimeSpan.FromSeconds(5));

            // Assert
            Assert.IsTrue(completed, "Background task should complete within timeout");
            Assert.IsTrue(exceptionHandled, "Exception should have been handled");
            Assert.IsTrue(_loggedException.Contains(testException), "Exception should have been logged");
        }

        [TestMethod]
        public void CriticalBackgroundException_ShouldCaptureApplicationState()
        {
            // Arrange
            var criticalException = new OutOfMemoryException("Critical memory exception");
            var applicationStateCaptured = false;

            // Act
            try
            {
                // Simulate critical exception handling
                var applicationState = _exceptionHandlingService.CaptureApplicationContext();
                _exceptionHandlingService.ReportCriticalError(criticalException, applicationState);
                applicationStateCaptured = applicationState != null;
            }
            catch (Exception)
            {
                Assert.Fail("Critical exception handling should not throw");
            }

            // Assert
            Assert.IsTrue(applicationStateCaptured, "Application state should be captured");
            Assert.IsTrue(_loggedException.Contains(criticalException), "Critical exception should be logged");
            Assert.IsTrue(_loggedMessages.Any(m => m.Contains("Critical error reported")), 
                "Critical error message should be logged");
        }

        #endregion

        #region Task Exception Handling Tests

        [TestMethod]
        public void UnobservedTaskException_ShouldBeHandledAndObserved()
        {
            // Arrange
            var taskException = new InvalidOperationException("Unobserved task exception");
            var exceptionObserved = false;
            var manualResetEvent = new ManualResetEventSlim(false);

            // Setup task exception handler
            EventHandler<UnobservedTaskExceptionEventArgs> handler = (sender, e) =>
            {
                _exceptionHandlingService.HandleException(e.Exception, "Unobserved task exception");
                e.SetObserved();
                exceptionObserved = true;
                manualResetEvent.Set();
            };

            TaskScheduler.UnobservedTaskException += handler;

            try
            {
                // Act - Create a task that throws an exception and is not awaited
                var task = Task.Run(() =>
                {
                    throw taskException;
                });

                // Don't await the task, let it become unobserved
                task = null;

                // Force garbage collection to trigger unobserved task exception
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Wait for the unobserved exception handler
                var completed = manualResetEvent.Wait(TimeSpan.FromSeconds(10));

                // Assert
                Assert.IsTrue(completed, "Unobserved task exception should be handled within timeout");
                Assert.IsTrue(exceptionObserved, "Exception should have been observed");
                Assert.IsTrue(_loggedException.Any(ex => ex.InnerException == taskException || ex == taskException), 
                    "Task exception should have been logged");
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }
        }

        [TestMethod]
        public void MultipleTaskExceptions_ShouldAllBeHandled()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("Task argument exception"),
                new InvalidOperationException("Task operation exception"),
                new FileNotFoundException("Task file exception")
            };

            var handledExceptions = new List<Exception>();
            var manualResetEvent = new ManualResetEventSlim(false);
            var expectedCount = exceptions.Length;

            // Setup task exception handler
            EventHandler<UnobservedTaskExceptionEventArgs> handler = (sender, e) =>
            {
                _exceptionHandlingService.HandleException(e.Exception, "Multiple task exception test");
                handledExceptions.Add(e.Exception);
                e.SetObserved();

                if (handledExceptions.Count >= expectedCount)
                {
                    manualResetEvent.Set();
                }
            };

            TaskScheduler.UnobservedTaskException += handler;

            try
            {
                // Act - Create multiple tasks that throw exceptions
                var tasks = exceptions.Select(ex => Task.Run(() =>
                {
                    throw ex;
                })).ToArray();

                // Don't await the tasks, let them become unobserved
                tasks = null;

                // Force garbage collection multiple times
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(100);
                }

                // Wait for all unobserved exception handlers
                var completed = manualResetEvent.Wait(TimeSpan.FromSeconds(15));

                // Assert
                Assert.IsTrue(completed, "All unobserved task exceptions should be handled within timeout");
                Assert.IsTrue(handledExceptions.Count >= expectedCount, 
                    $"Expected at least {expectedCount} exceptions, got {handledExceptions.Count}");
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }
        }

        #endregion

        #region Application State Preservation Tests

        [TestMethod]
        public void ExceptionDuringStateCapture_ShouldNotCrashApplication()
        {
            // Arrange
            var testException = new InvalidOperationException("State capture test exception");

            // Act & Assert - Should not throw
            try
            {
                var context = _exceptionHandlingService.CaptureApplicationContext();
                _exceptionHandlingService.HandleException(testException, "State preservation test");
                _exceptionHandlingService.ReportCriticalError(testException, context);

                // Verify the application context contains expected data
                Assert.IsNotNull(context, "Application context should be captured");
                
                var contextType = context.GetType();
                var timestampProperty = contextType.GetProperty("Timestamp");
                var machineNameProperty = contextType.GetProperty("MachineName");
                
                Assert.IsNotNull(timestampProperty, "Context should have Timestamp property");
                Assert.IsNotNull(machineNameProperty, "Context should have MachineName property");
                
                var timestamp = (DateTime)timestampProperty.GetValue(context)!;
                var machineName = (string)machineNameProperty.GetValue(context)!;
                
                Assert.IsTrue(timestamp > DateTime.MinValue, "Timestamp should be valid");
                Assert.IsFalse(string.IsNullOrEmpty(machineName), "Machine name should not be empty");
            }
            catch (Exception ex)
            {
                Assert.Fail($"State preservation should not throw exceptions: {ex.Message}");
            }
        }

        [TestMethod]
        public void ApplicationContextCapture_ShouldIncludeSystemInformation()
        {
            // Act
            var context = _exceptionHandlingService.CaptureApplicationContext();

            // Assert
            Assert.IsNotNull(context, "Application context should not be null");

            var contextType = context.GetType();
            var properties = contextType.GetProperties();

            // Verify expected properties exist
            var expectedProperties = new[] { "Timestamp", "MachineName", "OSVersion", "ProcessorCount", "WorkingSet" };
            foreach (var expectedProperty in expectedProperties)
            {
                var property = properties.FirstOrDefault(p => p.Name == expectedProperty);
                Assert.IsNotNull(property, $"Context should have {expectedProperty} property");
                
                var value = property.GetValue(context);
                Assert.IsNotNull(value, $"{expectedProperty} should have a value");
            }

            // Verify specific values
            var machineName = (string)contextType.GetProperty("MachineName")!.GetValue(context)!;
            var osVersion = (string)contextType.GetProperty("OSVersion")!.GetValue(context)!;
            var processorCount = (int)contextType.GetProperty("ProcessorCount")!.GetValue(context)!;

            Assert.AreEqual(Environment.MachineName, machineName, "Machine name should match environment");
            Assert.AreEqual(Environment.OSVersion.ToString(), osVersion, "OS version should match environment");
            Assert.AreEqual(Environment.ProcessorCount, processorCount, "Processor count should match environment");
        }

        #endregion

        #region User Notification Tests

        [TestMethod]
        public void UserFriendlyMessages_ShouldBeGeneratedForAllExceptionTypes()
        {
            // Arrange
            var testCases = new Dictionary<Exception, string>
            {
                { new ArgumentException("Invalid argument"), "Invalid input provided" },
                { new UnauthorizedAccessException("Access denied"), "Access denied" },
                { new FileNotFoundException("File not found"), "Required file not found" },
                { new OutOfMemoryException("Out of memory"), "running low on memory" },
                { new InvalidOperationException("Unknown error"), "unexpected error occurred" },
                { new NullReferenceException("Null reference"), "unexpected error occurred" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(testCase.Key);
                
                Assert.IsNotNull(userMessage, $"User message should be generated for {testCase.Key.GetType().Name}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(userMessage), "User message should not be empty");
                Assert.IsTrue(userMessage.Contains(testCase.Value, StringComparison.OrdinalIgnoreCase), 
                    $"User message should contain expected text for {testCase.Key.GetType().Name}. Expected: '{testCase.Value}', Actual: '{userMessage}'");
            }
        }

        [TestMethod]
        public void UserNotificationTiming_ShouldBeImmediate()
        {
            // Arrange
            var testException = new InvalidOperationException("Timing test exception");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            _exceptionHandlingService.HandleException(testException, "Timing test");
            var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(testException);
            
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"User notification should be immediate, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsNotNull(userMessage, "User message should be generated");
            Assert.IsTrue(_loggedException.Contains(testException), "Exception should be logged immediately");
        }

        #endregion

        #region Error Recovery Tests

        [TestMethod]
        public void ErrorRecovery_ShouldAttemptRecoveryForAllExceptionTypes()
        {
            // Arrange
            var exceptions = new Exception[]
            {
                new ArgumentException("Argument error"),
                new InvalidOperationException("Operation error"),
                new FileNotFoundException("File error"),
                new OutOfMemoryException("Memory error"),
                new UnauthorizedAccessException("Access error")
            };

            var recoveryAttempts = 0;

            // Act
            foreach (var exception in exceptions)
            {
                var recovered = _exceptionHandlingService.AttemptRecovery(exception);
                if (!recovered) // Current implementation always returns false
                {
                    recoveryAttempts++;
                }
            }

            // Assert
            Assert.AreEqual(exceptions.Length, recoveryAttempts, "Recovery should be attempted for all exceptions");
            Assert.IsTrue(_loggedMessages.Count(m => m.Contains("Attempting recovery")) >= exceptions.Length, 
                "Recovery attempts should be logged");
        }

        [TestMethod]
        public void RecoveryProcedures_ShouldNotThrowExceptions()
        {
            // Arrange
            var problematicExceptions = new Exception[]
            {
                new StackOverflowException("Stack overflow"),
                new OutOfMemoryException("Out of memory"),
                new AccessViolationException("Access violation")
            };

            // Act & Assert
            foreach (var exception in problematicExceptions)
            {
                try
                {
                    _exceptionHandlingService.HandleException(exception, "Recovery safety test");
                    var recovered = _exceptionHandlingService.AttemptRecovery(exception);
                    var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(exception);
                    
                    // Should reach here without throwing
                    Assert.IsNotNull(userMessage, "User message should be generated even for problematic exceptions");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Recovery procedures should not throw exceptions for {exception.GetType().Name}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Integration Workflow Tests

        [TestMethod]
        public void CompleteExceptionHandlingWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange
            var testException = new InvalidOperationException("End-to-end test exception");
            var context = "End-to-end integration test";

            // Act - Execute complete exception handling workflow
            _exceptionHandlingService.HandleException(testException, context);
            var applicationContext = _exceptionHandlingService.CaptureApplicationContext();
            var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(testException);
            var recoveryResult = _exceptionHandlingService.AttemptRecovery(testException);
            _exceptionHandlingService.ReportCriticalError(testException, applicationContext);

            // Assert - Verify all steps completed successfully
            Assert.IsTrue(_loggedException.Contains(testException), "Exception should be logged in HandleException");
            Assert.IsNotNull(applicationContext, "Application context should be captured");
            Assert.IsNotNull(userMessage, "User message should be generated");
            Assert.IsFalse(recoveryResult, "Recovery result should be false (current implementation)");
            
            // Verify logging occurred for each step
            Assert.IsTrue(_loggedMessages.Any(m => m.Contains("Unhandled exception occurred")), 
                "HandleException should log error");
            Assert.IsTrue(_loggedMessages.Any(m => m.Contains("Attempting recovery")), 
                "AttemptRecovery should log attempt");
            Assert.IsTrue(_loggedMessages.Any(m => m.Contains("Critical error reported")), 
                "ReportCriticalError should log critical error");
        }

        [TestMethod]
        public void ExceptionHandlingUnderLoad_ShouldMaintainStability()
        {
            // Arrange
            const int exceptionCount = 100;
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();

            // Generate different types of exceptions
            for (int i = 0; i < exceptionCount; i++)
            {
                Exception exception = (i % 5) switch
                {
                    0 => new ArgumentException($"Argument exception {i}"),
                    1 => new InvalidOperationException($"Operation exception {i}"),
                    2 => new FileNotFoundException($"File exception {i}"),
                    3 => new OutOfMemoryException($"Memory exception {i}"),
                    _ => new UnauthorizedAccessException($"Access exception {i}")
                };
                exceptions.Add(exception);
            }

            // Act - Process all exceptions concurrently
            foreach (var exception in exceptions)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        _exceptionHandlingService.HandleException(exception, $"Load test for {exception.GetType().Name}");
                        _exceptionHandlingService.GenerateUserFriendlyMessage(exception);
                        _exceptionHandlingService.AttemptRecovery(exception);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"Exception handling under load should not throw: {ex.Message}");
                    }
                }));
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));

            // Assert
            Assert.AreEqual(exceptionCount, _loggedException.Count, 
                $"All {exceptionCount} exceptions should be logged");
            Assert.IsTrue(_loggedMessages.Count >= exceptionCount, 
                "At least one message should be logged per exception");
        }

        #endregion

        #region Edge Cases and Error Conditions

        [TestMethod]
        public void ExceptionHandling_WithNullValues_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw exceptions
            try
            {
                _exceptionHandlingService.HandleException(null!, "Null exception test");
                var userMessage = _exceptionHandlingService.GenerateUserFriendlyMessage(null!);
                var recovered = _exceptionHandlingService.AttemptRecovery(null!);
                _exceptionHandlingService.ReportCriticalError(null!, new { State = "Test" });

                // Verify graceful handling
                Assert.IsNotNull(userMessage, "User message should be generated even for null exception");
                Assert.IsFalse(recovered, "Recovery should return false for null exception");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception handling should handle null values gracefully: {ex.Message}");
            }
        }

        [TestMethod]
        public void ExceptionHandling_WithCorruptedState_ShouldContinueOperation()
        {
            // Arrange - Simulate corrupted application state
            var corruptedException = new InvalidOperationException("Corrupted state exception");
            var corruptedState = new { CorruptedData = (string)null!, InvalidValue = -1 };

            // Act & Assert - Should handle corrupted state gracefully
            try
            {
                _exceptionHandlingService.HandleException(corruptedException, "Corrupted state test");
                _exceptionHandlingService.ReportCriticalError(corruptedException, corruptedState);
                
                var context = _exceptionHandlingService.CaptureApplicationContext();
                Assert.IsNotNull(context, "Should be able to capture context even with corrupted state");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception handling should work with corrupted state: {ex.Message}");
            }
        }

        #endregion
    }
}
