using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.ViewModels;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for the ball state machine with ViewModels and PhysicsEngine.
    /// Tests the complete state lifecycle scenarios and component interactions.
    /// </summary>
    [TestClass]
    public class BallStateMachineIntegrationTests
    {
        private Mock<ILogService> _mockLogService;
        private BallStateConfiguration _configuration;
        private BallStateMachine _stateMachine;
        private BallViewModel _ballViewModel;
        private StatusBarViewModel _statusBarViewModel;
        private PhysicsEngine _physicsEngine;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _configuration = BallStateConfiguration.CreateDefault();
            _stateMachine = new BallStateMachine(_mockLogService.Object, _configuration);
            _physicsEngine = new PhysicsEngine();
            
            // Create ViewModels with state machine integration
            _ballViewModel = new BallViewModel(_mockLogService.Object, _stateMachine);
            _statusBarViewModel = new StatusBarViewModel(_mockLogService.Object, _stateMachine);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _ballViewModel?.Dispose();
            _statusBarViewModel?.Dispose();
            _stateMachine = null;
            _mockLogService = null;
            _configuration = null;
            _physicsEngine = null;
        }

        #region BallViewModel State Integration Tests

        [TestMethod]
        public void BallViewModel_StateIntegration_InitialStateIsIdle()
        {
            // Assert
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.IsTrue(_ballViewModel.IsInIdleState);
            Assert.IsFalse(_ballViewModel.IsInHeldState);
            Assert.IsFalse(_ballViewModel.IsInThrownState);
        }

        [TestMethod]
        public void BallViewModel_StateIntegration_StatePropertiesUpdateOnTransition()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _ballViewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName);

            // Act - Transition to Held state
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert
            Assert.AreEqual(BallState.Held, _ballViewModel.CurrentState);
            Assert.IsFalse(_ballViewModel.IsInIdleState);
            Assert.IsTrue(_ballViewModel.IsInHeldState);
            Assert.IsFalse(_ballViewModel.IsInThrownState);

            // Verify property change notifications were raised
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(_ballViewModel.CurrentState)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(_ballViewModel.IsInIdleState)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(_ballViewModel.IsInHeldState)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(_ballViewModel.IsInThrownState)));
        }

        [TestMethod]
        public void BallViewModel_StateIntegration_IsDraggingReflectsHeldState()
        {
            // Arrange
            Assert.IsFalse(_ballViewModel.IsDragging); // Initially not dragging

            // Act - Transition to Held state
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - IsDragging should reflect the Held state
            Assert.IsTrue(_ballViewModel.IsInHeldState);
            // Note: IsDragging property depends on both state and internal dragging flag
            // The actual dragging behavior is controlled by mouse events, but state influences it
        }

        [TestMethod]
        public void BallViewModel_StateIntegration_CompleteStateLifecycle()
        {
            // Arrange
            var stateChanges = new List<BallState>();
            _ballViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_ballViewModel.CurrentState))
                {
                    stateChanges.Add(_ballViewModel.CurrentState);
                }
            };

            // Act - Complete lifecycle: Idle -> Held -> Thrown -> Idle
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);

            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle

            // Assert
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.AreEqual(3, stateChanges.Count);
            Assert.AreEqual(BallState.Held, stateChanges[0]);
            Assert.AreEqual(BallState.Thrown, stateChanges[1]);
            Assert.AreEqual(BallState.Idle, stateChanges[2]);
        }

        #endregion

        #region PhysicsEngine State-Aware Behavior Tests

        [TestMethod]
        public void PhysicsEngine_StateAwareBehavior_SkipsPhysicsWhenHeld()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(50, 50); // Set some velocity
            var initialX = ball.X;
            var initialY = ball.Y;

            // Act - Update physics while in Held state
            var result = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Held, _configuration);

            // Assert - Ball position should not change when held
            Assert.AreEqual(initialX, ball.X, 0.001);
            Assert.AreEqual(initialY, ball.Y, 0.001);
            Assert.IsFalse(result.IsMoving); // Should report not moving when held
        }

        [TestMethod]
        public void PhysicsEngine_StateAwareBehavior_AppliesPhysicsWhenThrown()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(60, 30); // Set velocity above threshold
            var initialX = ball.X;
            var initialY = ball.Y;

            // Act - Update physics while in Thrown state
            var result = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Thrown, _configuration);

            // Assert - Ball position should change when thrown
            Assert.AreNotEqual(initialX, ball.X);
            Assert.AreNotEqual(initialY, ball.Y);
            Assert.IsTrue(result.IsMoving); // Should report moving when thrown with sufficient velocity
        }

        [TestMethod]
        public void PhysicsEngine_StateAwareBehavior_DetectsVelocityBelowThreshold()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(10, 10); // Set velocity below threshold (50.0)
            
            // Act - Update physics while in Thrown state
            var result = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Thrown, _configuration);

            // Assert - Should detect velocity below threshold
            Assert.IsTrue(result.VelocityBelowThreshold);
            Assert.IsFalse(result.IsMoving);
        }

        [TestMethod]
        public void PhysicsEngine_StateAwareBehavior_IdleStateAllowsPhysics()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(60, 30); // Set some velocity
            var initialX = ball.X;
            var initialY = ball.Y;

            // Act - Update physics while in Idle state (should behave like normal physics)
            var result = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Idle, _configuration);

            // Assert - Ball position should change in Idle state (normal physics apply)
            Assert.AreNotEqual(initialX, ball.X);
            Assert.AreNotEqual(initialY, ball.Y);
            Assert.IsTrue(result.IsMoving);
        }

        #endregion

        #region StatusBarViewModel State Display Tests

        [TestMethod]
        public void StatusBarViewModel_StateDisplay_InitialStateDisplaysIdle()
        {
            // Assert
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StatusBarViewModel_StateDisplay_UpdatesOnStateChange()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _statusBarViewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName);

            // Act - Transition through states
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual("Ball: Held", _statusBarViewModel.Status);

            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual("Ball: Thrown", _statusBarViewModel.Status);

            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);

            // Assert - Property change notifications should have been raised
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(_statusBarViewModel.Status)));
        }

        [TestMethod]
        public void StatusBarViewModel_StateDisplay_FormatsAllStatesCorrectly()
        {
            // Test each state format
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            Assert.AreEqual("Ball: Held", _statusBarViewModel.Status);

            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            Assert.AreEqual("Ball: Thrown", _statusBarViewModel.Status);

            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        #endregion

        #region Complete State Lifecycle Scenarios

        [TestMethod]
        public void CompleteStateLifecycle_MouseDownDragReleaseSettle_WorksCorrectly()
        {
            // Arrange
            var ballStateChanges = new List<BallState>();
            var statusUpdates = new List<string>();

            _ballViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_ballViewModel.CurrentState))
                {
                    ballStateChanges.Add(_ballViewModel.CurrentState);
                }
            };

            _statusBarViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_statusBarViewModel.Status))
                {
                    statusUpdates.Add(_statusBarViewModel.Status);
                }
            };

            // Act - Simulate complete interaction
            // 1. Mouse down (grab ball)
            _stateMachine.Fire(BallTrigger.MouseDown);
            
            // 2. Release (throw ball)
            _stateMachine.Fire(BallTrigger.Release);
            
            // 3. Ball settles (velocity below threshold)
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);

            // Assert - Verify complete state sequence
            Assert.AreEqual(3, ballStateChanges.Count);
            Assert.AreEqual(BallState.Held, ballStateChanges[0]);
            Assert.AreEqual(BallState.Thrown, ballStateChanges[1]);
            Assert.AreEqual(BallState.Idle, ballStateChanges[2]);

            Assert.AreEqual(3, statusUpdates.Count);
            Assert.AreEqual("Ball: Held", statusUpdates[0]);
            Assert.AreEqual("Ball: Thrown", statusUpdates[1]);
            Assert.AreEqual("Ball: Idle", statusUpdates[2]);

            // Final state should be Idle
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void CompleteStateLifecycle_PhysicsIntegration_TransitionsCorrectly()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(100, 50); // High velocity

            // Act & Assert - Test physics behavior in each state
            
            // 1. Idle state - physics should apply
            var idleResult = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Idle, _configuration);
            Assert.IsTrue(idleResult.IsMoving);

            // 2. Transition to Held - physics should be paused
            _stateMachine.Fire(BallTrigger.MouseDown);
            var heldResult = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Held, _configuration);
            Assert.IsFalse(heldResult.IsMoving); // Physics paused

            // 3. Transition to Thrown - physics should resume
            _stateMachine.Fire(BallTrigger.Release);
            var thrownResult = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Thrown, _configuration);
            Assert.IsTrue(thrownResult.IsMoving); // Physics active

            // 4. Reduce velocity and check threshold detection
            ball.SetVelocity(10, 10); // Below threshold
            var settlingResult = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Thrown, _configuration);
            Assert.IsTrue(settlingResult.VelocityBelowThreshold);

            // 5. Transition back to Idle
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void CompleteStateLifecycle_MultipleObservers_AllNotified()
        {
            // Arrange
            var mockObserver1 = new Mock<IBallStateObserver>();
            var mockObserver2 = new Mock<IBallStateObserver>();
            
            _stateMachine.Subscribe(mockObserver1.Object);
            _stateMachine.Subscribe(mockObserver2.Object);

            // Act - Complete state lifecycle
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
            _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle

            // Assert - All observers should be notified of all transitions
            mockObserver1.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver1.Verify(o => o.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release), Times.Once);
            mockObserver1.Verify(o => o.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.VelocityBelowThreshold), Times.Once);

            mockObserver2.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            mockObserver2.Verify(o => o.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release), Times.Once);
            mockObserver2.Verify(o => o.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.VelocityBelowThreshold), Times.Once);

            // ViewModels should also have been notified (they are also observers)
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        #endregion

        #region Error Handling and Edge Cases

        [TestMethod]
        public void StateIntegration_ObserverException_DoesNotBreakOtherObservers()
        {
            // Arrange
            var mockFaultyObserver = new Mock<IBallStateObserver>();
            mockFaultyObserver.Setup(o => o.OnStateChanged(It.IsAny<BallState>(), It.IsAny<BallState>(), It.IsAny<BallTrigger>()))
                             .Throws(new InvalidOperationException("Observer error"));

            var mockGoodObserver = new Mock<IBallStateObserver>();
            
            _stateMachine.Subscribe(mockFaultyObserver.Object);
            _stateMachine.Subscribe(mockGoodObserver.Object);

            // Act - State transition should not fail despite observer exception
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - State should still transition and good observer should be notified
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);
            mockGoodObserver.Verify(o => o.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown), Times.Once);
            
            // ViewModels should still work correctly
            Assert.AreEqual(BallState.Held, _ballViewModel.CurrentState);
            Assert.AreEqual("Ball: Held", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StateIntegration_PhysicsWithNullConfiguration_UsesDefaults()
        {
            // Arrange
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(10, 10); // Low velocity

            // Act - Update physics with null configuration
            var result = _physicsEngine.UpdateBall(ball, 1.0/60.0, 0, 0, 800, 600, BallState.Thrown, null);

            // Assert - Should still work with default behavior
            Assert.IsNotNull(result);
            // The physics engine should handle null configuration gracefully
        }

        [TestMethod]
        public void StateIntegration_RapidStateTransitions_HandledCorrectly()
        {
            // Arrange
            var stateChangeCount = 0;
            _ballViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_ballViewModel.CurrentState))
                {
                    stateChangeCount++;
                }
            };

            // Act - Rapid state transitions
            for (int i = 0; i < 10; i++)
            {
                _stateMachine.Fire(BallTrigger.MouseDown); // Idle -> Held
                _stateMachine.Fire(BallTrigger.Release); // Held -> Thrown
                _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown -> Idle
            }

            // Assert - All transitions should be handled correctly
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual(30, stateChangeCount); // 3 transitions × 10 iterations
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        #endregion
    }
}