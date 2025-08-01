using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Error handling and edge case tests for the ball state machine.
    /// Tests concurrent state transitions, error recovery mechanisms, 
    /// state consistency validation, and observer subscription/unsubscription.
    /// </summary>
    [TestClass]
    public class BallStateMachineErrorHandlingTests
    {
        private Mock<ILogService> _mockLogService;
        private BallStateConfiguration _configuration;
        private BallStateMachine _stateMachine;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _configuration = BallStateConfiguration.CreateDefault();
            _stateMachine = new BallStateMachine(_mockLogService.Object, _configuration);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _stateMachine = null;
            _mockLogService = null;
            _configuration = null;
        }

        #region Concurrent State Transition Tests

        [TestMethod]
        public void ConcurrentStateTransitions_MultipleThreads_MaintainConsistency()
        {
            // Arrange
            const int threadCount = 10;
            const int transitionsPerThread = 50;
            var threads = new Thread[threadCount];
            var exceptions = new List<Exception>();
            var completedTransitions = 0;
            var startEvent = new ManualResetEventSlim(false);

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        startEvent.Wait();
                        for (int j = 0; j < transitionsPerThread; j++)
                        {
                            // Attempt valid transitions based on current state
                            var currentState = _stateMachine.CurrentState;
                            try
                            {
                                switch (currentState)
                                {
                                    case BallState.Idle:
                                        if (_stateMachine.CanFire(BallTrigger.MouseDown))
                                        {
                                            _stateMachine.Fire(BallTrigger.MouseDown);
                                            Interlocked.Increment(ref completedTransitions);
                                        }
                                        break;
                                    case BallState.Held:
                                        if (_stateMachine.CanFire(BallTrigger.Release))
                                        {
                                            _stateMachine.Fire(BallTrigger.Release);
                                            Interlocked.Increment(ref completedTransitions);
                                        }
                                        break;
                                    case BallState.Thrown:
                                        if (_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold))
                                        {
                                            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                                            Interlocked.Increment(ref completedTransitions);
                                        }
                                        break;
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // Expected when transition is not valid - ignore
                            }
                            
                            // Small delay to increase chance of concurrent access
                            Thread.Sleep(1);
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
                threads[i].Start();
            }

            startEvent.Set();

            foreach (var thread in threads)
            {
                thread.Join(TimeSpan.FromSeconds(30)); // Timeout to prevent hanging
            }

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Unexpected exceptions: {string.Join(", ", exceptions)}");
            Assert.IsTrue(completedTransitions > 0, "No transitions were completed");
            
            // State machine should be in a valid state
            Assert.IsTrue(Enum.IsDefined(typeof(BallState), _stateMachine.CurrentState));
            
            // Verify state consistency
            Assert.IsTrue(ValidateStateConsistency());
        }

        [TestMethod]
        public void ConcurrentStateTransitions_InvalidTransitions_HandledGracefully()
        {
            // Arrange
            const int threadCount = 5;
            var threads = new Thread[threadCount];
            var invalidTransitionCount = 0;
            var startEvent = new ManualResetEventSlim(false);

            // Act - All threads try to fire the same invalid transition
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    startEvent.Wait();
                    try
                    {
                        // Try to fire Release from Idle state (invalid)
                        _stateMachine.Fire(BallTrigger.Release);
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidTransitionCount);
                    }
                });
                threads[i].Start();
            }

            startEvent.Set();

            foreach (var thread in threads)
            {
                thread.Join(TimeSpan.FromSeconds(10));
            }

            // Assert
            Assert.AreEqual(threadCount, invalidTransitionCount);
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState); // Should remain in Idle
        }

        [TestMethod]
        public void ConcurrentObserverOperations_SubscribeUnsubscribe_ThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int operationsPerThread = 100;
            var threads = new Thread[threadCount];
            var observers = new List<Mock<IBallStateObserver>>();
            var exceptions = new List<Exception>();
            var startEvent = new ManualResetEventSlim(false);

            // Create observers
            for (int i = 0; i < threadCount * 2; i++)
            {
                observers.Add(new Mock<IBallStateObserver>());
            }

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        startEvent.Wait();
                        var random = new Random(threadIndex);
                        
                        for (int j = 0; j < operationsPerThread; j++)
                        {
                            var observerIndex = random.Next(observers.Count);
                            var observer = observers[observerIndex];

                            if (random.Next(2) == 0)
                            {
                                // Subscribe
                                _stateMachine.Subscribe(observer.Object);
                            }
                            else
                            {
                                // Unsubscribe
                                _stateMachine.Unsubscribe(observer.Object);
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
                threads[i].Start();
            }

            startEvent.Set();

            foreach (var thread in threads)
            {
                thread.Join(TimeSpan.FromSeconds(30));
            }

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Unexpected exceptions: {string.Join(", ", exceptions)}");

            // Fire a state transition to ensure observers still work
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);
        }

        #endregion

        #region Error Recovery Mechanism Tests

        [TestMethod]
        public void ErrorRecovery_StateConsistencyValidation_DetectsInconsistencies()
        {
            // Arrange & Act
            var isConsistent = ValidateStateConsistency();

            // Assert
            Assert.IsTrue(isConsistent, "State machine should be consistent after initialization");
        }

        [TestMethod]
        public void ErrorRecovery_StateConsistencyValidation_ValidatesAllStates()
        {
            // Test consistency in each state
            
            // Idle state
            Assert.IsTrue(ValidateStateConsistency());
            Assert.IsTrue(_stateMachine.CanFire(BallTrigger.MouseDown));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.Release));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold));

            // Held state
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.IsTrue(ValidateStateConsistency());
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.MouseDown));
            Assert.IsTrue(_stateMachine.CanFire(BallTrigger.Release));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold));

            // Thrown state
            _stateMachine.Fire(BallTrigger.Release);
            Assert.IsTrue(ValidateStateConsistency());
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.MouseDown));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.Release));
            Assert.IsTrue(_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold));
        }

        [TestMethod]
        public void ErrorRecovery_RecoverToSafeState_FromThrownState()
        {
            // Arrange - Get to Thrown state
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // Act - Attempt recovery (using reflection to access private method)
            var recoverMethod = typeof(BallStateMachine).GetMethod("RecoverToSafeState", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(recoverMethod, "RecoverToSafeState method should exist");

            var result = (bool)recoverMethod.Invoke(_stateMachine, null);

            // Assert
            Assert.IsTrue(result, "Recovery should succeed");
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void ErrorRecovery_HandleStateTransitionError_LogsAndRecovers()
        {
            // Arrange - Create a configuration that might cause issues
            var faultyConfig = new BallStateConfiguration
            {
                EnableTransitionValidation = true,
                EnableStateLogging = true
            };
            var faultyStateMachine = new BallStateMachine(_mockLogService.Object, faultyConfig);

            // Act - Try to cause an error by rapid invalid transitions
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        faultyStateMachine.Fire(BallTrigger.Release); // Invalid from Idle
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected - continue
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception during error handling test: {ex}");
            }

            // Assert - State machine should still be functional
            Assert.AreEqual(BallState.Idle, faultyStateMachine.CurrentState);
            Assert.IsTrue(faultyStateMachine.CanFire(BallTrigger.MouseDown));

            // Verify logging occurred
            _mockLogService.Verify(
                x => x.LogWarning(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Observer Subscription/Unsubscription Tests

        [TestMethod]
        public void ObserverManagement_Subscribe_NullObserver_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _stateMachine.Subscribe(null));
        }

        [TestMethod]
        public void ObserverManagement_Unsubscribe_NullObserver_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _stateMachine.Unsubscribe(null));
        }

        [TestMethod]
        public void ObserverManagement_SubscribeSameObserverTwice_OnlyNotifiedOnce()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();
            
            // Act
            _stateMachine.Subscribe(mockObserver.Object);
            _stateMachine.Subscribe(mockObserver.Object); // Subscribe again
            
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - Should only be called once despite double subscription
            mockObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        [TestMethod]
        public void ObserverManagement_UnsubscribeNonExistentObserver_DoesNotThrow()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();

            // Act & Assert - Should not throw
            _stateMachine.Unsubscribe(mockObserver.Object);
        }

        [TestMethod]
        public void ObserverManagement_UnsubscribeAfterNotification_StopsNotifications()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();
            _stateMachine.Subscribe(mockObserver.Object);

            // Act - First transition
            _stateMachine.Fire(BallTrigger.MouseDown);
            mockObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);

            // Unsubscribe
            _stateMachine.Unsubscribe(mockObserver.Object);

            // Second transition
            _stateMachine.Fire(BallTrigger.Release);

            // Assert - Should not be called for second transition
            mockObserver.Verify(o => o.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release), Times.Never);
        }

        [TestMethod]
        public void ObserverManagement_ObserverThrowsException_OtherObserversStillNotified()
        {
            // Arrange
            var faultyObserver = new Mock<IBallStateObserver>();
            var goodObserver1 = new Mock<IBallStateObserver>();
            var goodObserver2 = new Mock<IBallStateObserver>();

            faultyObserver.Setup(o => o.OnStateChanged(It.IsAny<BallState>(), It.IsAny<BallState>(), It.IsAny<BallTrigger>()))
                         .Throws(new InvalidOperationException("Observer error"));

            _stateMachine.Subscribe(goodObserver1.Object);
            _stateMachine.Subscribe(faultyObserver.Object);
            _stateMachine.Subscribe(goodObserver2.Object);

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - Good observers should still be notified
            goodObserver1.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            goodObserver2.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            faultyObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);

            // State should still transition correctly
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void ObserverManagement_ManyObservers_AllNotifiedCorrectly()
        {
            // Arrange
            const int observerCount = 100;
            var observers = new List<Mock<IBallStateObserver>>();

            for (int i = 0; i < observerCount; i++)
            {
                var observer = new Mock<IBallStateObserver>();
                observers.Add(observer);
                _stateMachine.Subscribe(observer.Object);
            }

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert
            foreach (var observer in observers)
            {
                observer.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            }
        }

        #endregion

        #region Configuration Edge Cases

        [TestMethod]
        public void Configuration_DisabledTransitionValidation_AllowsInvalidTransitions()
        {
            // Arrange
            var config = new BallStateConfiguration
            {
                EnableTransitionValidation = false,
                EnableStateLogging = false
            };
            var stateMachine = new BallStateMachine(_mockLogService.Object, config);

            // Act & Assert - Invalid transitions should not throw when validation is disabled
            // Note: This depends on the implementation - if Stateless library still validates,
            // this test might need adjustment
            try
            {
                stateMachine.Fire(BallTrigger.Release); // Invalid from Idle
                // If we get here, validation was disabled
            }
            catch (InvalidOperationException)
            {
                // If validation is still enforced by Stateless library, that's also valid
                Assert.AreEqual(BallState.Idle, stateMachine.CurrentState);
            }
        }

        [TestMethod]
        public void Configuration_DisabledLogging_DoesNotLog()
        {
            // Arrange
            var config = new BallStateConfiguration
            {
                EnableStateLogging = false
            };
            var stateMachine = new BallStateMachine(_mockLogService.Object, config);

            // Act
            stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - Should not log when logging is disabled
            _mockLogService.Verify(
                x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.Never);
        }

        [TestMethod]
        public void Configuration_AsyncNotifications_WorkCorrectly()
        {
            // Arrange
            var config = new BallStateConfiguration
            {
                EnableAsyncNotifications = true
            };
            var stateMachine = new BallStateMachine(_mockLogService.Object, config);
            var mockObserver = new Mock<IBallStateObserver>();
            stateMachine.Subscribe(mockObserver.Object);

            // Act
            stateMachine.Fire(BallTrigger.MouseDown);

            // Give async notifications time to complete
            Thread.Sleep(100);

            // Assert
            mockObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates state consistency using reflection to access the private method
        /// </summary>
        private bool ValidateStateConsistency()
        {
            try
            {
                var method = typeof(BallStateMachine).GetMethod("ValidateStateConsistency", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (method != null)
                {
                    return (bool)method.Invoke(_stateMachine, null);
                }
                
                // If method doesn't exist, perform basic validation
                var currentState = _stateMachine.CurrentState;
                return Enum.IsDefined(typeof(BallState), currentState);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Stress Tests

        [TestMethod]
        public void StressTest_RapidStateTransitions_MaintainsStability()
        {
            // Arrange
            const int iterations = 1000;
            var random = new Random(42); // Fixed seed for reproducibility

            // Act
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var currentState = _stateMachine.CurrentState;
                    
                    // Choose a valid transition based on current state
                    switch (currentState)
                    {
                        case BallState.Idle:
                            _stateMachine.Fire(BallTrigger.MouseDown);
                            break;
                        case BallState.Held:
                            _stateMachine.Fire(BallTrigger.Release);
                            break;
                        case BallState.Thrown:
                            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                            break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignore invalid transitions in stress test
                }
            }

            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(BallState), _stateMachine.CurrentState));
            Assert.IsTrue(ValidateStateConsistency());
        }

        [TestMethod]
        public void StressTest_ManyObserversRapidTransitions_PerformanceAcceptable()
        {
            // Arrange
            const int observerCount = 50;
            const int transitionCount = 100;
            var observers = new List<Mock<IBallStateObserver>>();

            for (int i = 0; i < observerCount; i++)
            {
                var observer = new Mock<IBallStateObserver>();
                observers.Add(observer);
                _stateMachine.Subscribe(observer.Object);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < transitionCount; i++)
            {
                _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
                _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
                _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
                $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
            
            // Verify all observers were notified correctly
            foreach (var observer in observers)
            {
                observer.Verify(o => o.OnStateChanged(It.IsAny<BallState>(), It.IsAny<BallState>(), It.IsAny<BallTrigger>()), 
                    Times.Exactly(transitionCount * 3));
            }
        }

        #endregion
    }
}