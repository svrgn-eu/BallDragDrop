using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for the BallStateMachine class.
    /// Tests initial state, valid state transitions, invalid transition rejection,
    /// and observer notification functionality.
    /// </summary>
    [TestClass]
    public class BallStateMachineTests
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

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var stateMachine = new BallStateMachine(_mockLogService.Object, _configuration);

            // Assert
            Assert.AreEqual(BallState.Idle, stateMachine.CurrentState);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullLogService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            new BallStateMachine(null, _configuration);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            new BallStateMachine(_mockLogService.Object, null);
        }

        #endregion

        #region Initial State Tests

        [TestMethod]
        public void CurrentState_AfterInitialization_IsIdle()
        {
            // Arrange & Act
            var currentState = _stateMachine.CurrentState;

            // Assert
            Assert.AreEqual(BallState.Idle, currentState);
        }

        [TestMethod]
        public void CanFire_MouseDownFromIdle_ReturnsTrue()
        {
            // Arrange & Act
            bool canFire = _stateMachine.CanFire(BallTrigger.MouseDown);

            // Assert
            Assert.IsTrue(canFire);
        }

        [TestMethod]
        public void CanFire_ReleaseFromIdle_ReturnsFalse()
        {
            // Arrange & Act
            bool canFire = _stateMachine.CanFire(BallTrigger.Release);

            // Assert
            Assert.IsFalse(canFire);
        }

        [TestMethod]
        public void CanFire_VelocityBelowThresholdFromIdle_ReturnsFalse()
        {
            // Arrange & Act
            bool canFire = _stateMachine.CanFire(BallTrigger.VelocityBelowThreshold);

            // Assert
            Assert.IsFalse(canFire);
        }

        #endregion

        #region Valid State Transition Tests

        [TestMethod]
        public void Fire_MouseDownFromIdle_TransitionsToHeld()
        {
            // Arrange
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void Fire_ReleaseFromHeld_TransitionsToThrown()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // Act
            _stateMachine.Fire(BallTrigger.Release);

            // Assert
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void Fire_VelocityBelowThresholdFromThrown_TransitionsToIdle()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // Act
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);

            // Assert
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void Fire_CompleteStateLifecycle_TransitionsCorrectly()
        {
            // Arrange
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            // Act & Assert - Complete lifecycle: Idle -> Held -> Thrown -> Idle
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            _stateMachine.Fire(BallTrigger.Release);
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        #endregion

        #region Invalid Transition Tests

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_ReleaseFromIdle_ThrowsInvalidOperationException()
        {
            // Arrange
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.Release);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_VelocityBelowThresholdFromIdle_ThrowsInvalidOperationException()
        {
            // Arrange
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_MouseDownFromHeld_ThrowsInvalidOperationException()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.MouseDown);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_VelocityBelowThresholdFromHeld_ThrowsInvalidOperationException()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_MouseDownFromThrown_ThrowsInvalidOperationException()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.MouseDown);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Fire_ReleaseFromThrown_ThrowsInvalidOperationException()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // Act & Assert
            _stateMachine.Fire(BallTrigger.Release);
        }

        #endregion

        #region CanFire Tests for All States

        [TestMethod]
        public void CanFire_FromHeldState_OnlyAllowsRelease()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // Act & Assert
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.MouseDown));
            Assert.IsTrue(_stateMachine.CanFire(BallTrigger.Release));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold));
        }

        [TestMethod]
        public void CanFire_FromThrownState_OnlyAllowsVelocityBelowThreshold()
        {
            // Arrange
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // Act & Assert
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.MouseDown));
            Assert.IsFalse(_stateMachine.CanFire(BallTrigger.Release));
            Assert.IsTrue(_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold));
        }

        #endregion

        #region Observer Notification Tests

        [TestMethod]
        public void Subscribe_WithValidObserver_AddsObserver()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();

            // Act
            _stateMachine.Subscribe(mockObserver.Object);

            // Assert - Verify observer is called when state changes
            _stateMachine.Fire(BallTrigger.MouseDown);
            mockObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_WithNullObserver_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            _stateMachine.Subscribe(null);
        }

        [TestMethod]
        public void Unsubscribe_WithValidObserver_RemovesObserver()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();
            _stateMachine.Subscribe(mockObserver.Object);

            // Act
            _stateMachine.Unsubscribe(mockObserver.Object);

            // Assert - Verify observer is not called after unsubscribing
            _stateMachine.Fire(BallTrigger.MouseDown);
            mockObserver.Verify(o => o.OnStateChanged(It.IsAny<BallState>(), It.IsAny<BallState>(), It.IsAny<BallTrigger>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Unsubscribe_WithNullObserver_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            _stateMachine.Unsubscribe(null);
        }

        [TestMethod]
        public void Fire_WithMultipleObservers_NotifiesAllObservers()
        {
            // Arrange
            var mockObserver1 = new Mock<IBallStateObserver>();
            var mockObserver2 = new Mock<IBallStateObserver>();
            var mockObserver3 = new Mock<IBallStateObserver>();

            _stateMachine.Subscribe(mockObserver1.Object);
            _stateMachine.Subscribe(mockObserver2.Object);
            _stateMachine.Subscribe(mockObserver3.Object);

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert
            mockObserver1.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver2.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver3.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        [TestMethod]
        public void Fire_WithObserverException_ContinuesNotifyingOtherObservers()
        {
            // Arrange
            var mockObserver1 = new Mock<IBallStateObserver>();
            var mockObserver2 = new Mock<IBallStateObserver>();
            var mockObserver3 = new Mock<IBallStateObserver>();

            // Setup observer2 to throw an exception
            mockObserver2.Setup(o => o.OnStateChanged(It.IsAny<BallState>(), It.IsAny<BallState>(), It.IsAny<BallTrigger>()))
                        .Throws(new InvalidOperationException("Observer error"));

            _stateMachine.Subscribe(mockObserver1.Object);
            _stateMachine.Subscribe(mockObserver2.Object);
            _stateMachine.Subscribe(mockObserver3.Object);

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - All observers should be called despite observer2 throwing an exception
            mockObserver1.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver2.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver3.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        [TestMethod]
        public void Subscribe_SameObserverTwice_OnlyAddsOnce()
        {
            // Arrange
            var mockObserver = new Mock<IBallStateObserver>();

            // Act
            _stateMachine.Subscribe(mockObserver.Object);
            _stateMachine.Subscribe(mockObserver.Object); // Subscribe same observer again

            // Assert - Observer should only be called once
            _stateMachine.Fire(BallTrigger.MouseDown);
            mockObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
        }

        #endregion

        #region StateChanged Event Tests

        [TestMethod]
        public void StateChanged_OnValidTransition_RaisesEvent()
        {
            // Arrange
            BallStateChangedEventArgs capturedEventArgs = null;
            _stateMachine.StateChanged += (sender, args) => capturedEventArgs = args;

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert
            Assert.IsNotNull(capturedEventArgs);
            Assert.AreEqual(BallState.Idle, capturedEventArgs.PreviousState);
            Assert.AreEqual(BallState.Held, capturedEventArgs.NewState);
            Assert.AreEqual(BallTrigger.MouseDown, capturedEventArgs.Trigger);
            Assert.IsTrue(capturedEventArgs.Timestamp <= DateTime.Now);
            Assert.IsTrue(capturedEventArgs.Timestamp >= DateTime.Now.AddSeconds(-1));
        }

        [TestMethod]
        public void StateChanged_OnMultipleTransitions_RaisesEventForEach()
        {
            // Arrange
            var eventArgsList = new List<BallStateChangedEventArgs>();
            _stateMachine.StateChanged += (sender, args) => eventArgsList.Add(args);

            // Act
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle

            // Assert
            Assert.AreEqual(3, eventArgsList.Count);

            // First transition: Idle -> Held
            Assert.AreEqual(BallState.Idle, eventArgsList[0].PreviousState);
            Assert.AreEqual(BallState.Held, eventArgsList[0].NewState);
            Assert.AreEqual(BallTrigger.MouseDown, eventArgsList[0].Trigger);

            // Second transition: Held -> Thrown
            Assert.AreEqual(BallState.Held, eventArgsList[1].PreviousState);
            Assert.AreEqual(BallState.Thrown, eventArgsList[1].NewState);
            Assert.AreEqual(BallTrigger.Release, eventArgsList[1].Trigger);

            // Third transition: Thrown -> Idle
            Assert.AreEqual(BallState.Thrown, eventArgsList[2].PreviousState);
            Assert.AreEqual(BallState.Idle, eventArgsList[2].NewState);
            Assert.AreEqual(BallTrigger.VelocityBelowThreshold, eventArgsList[2].Trigger);
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void CurrentState_ConcurrentAccess_ReturnsConsistentValue()
        {
            // Arrange
            const int threadCount = 10;
            const int iterationsPerThread = 100;
            var results = new BallState[threadCount * iterationsPerThread];
            var threads = new Thread[threadCount];
            var startEvent = new ManualResetEventSlim(false);

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    startEvent.Wait();
                    for (int j = 0; j < iterationsPerThread; j++)
                    {
                        results[threadIndex * iterationsPerThread + j] = _stateMachine.CurrentState;
                    }
                });
                threads[i].Start();
            }

            startEvent.Set();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert - All results should be valid BallState values
            foreach (var result in results)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(BallState), result));
            }
        }

        [TestMethod]
        public void CanFire_ConcurrentAccess_ReturnsConsistentResults()
        {
            // Arrange
            const int threadCount = 5;
            const int iterationsPerThread = 50;
            var results = new bool[threadCount * iterationsPerThread];
            var threads = new Thread[threadCount];
            var startEvent = new ManualResetEventSlim(false);

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    startEvent.Wait();
                    for (int j = 0; j < iterationsPerThread; j++)
                    {
                        results[threadIndex * iterationsPerThread + j] = _stateMachine.CanFire(BallTrigger.MouseDown);
                    }
                });
                threads[i].Start();
            }

            startEvent.Set();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Assert - All results should be true since we start in Idle state
            foreach (var result in results)
            {
                Assert.IsTrue(result);
            }
        }

        #endregion
    }
}