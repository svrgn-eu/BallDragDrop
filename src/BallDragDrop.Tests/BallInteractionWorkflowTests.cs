using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for complete ball interaction workflow.
    /// Tests the complete mouse down → drag → release → physics → idle cycle.
    /// </summary>
    [TestClass]
    public class BallInteractionWorkflowTests
    {
        private Mock<ILogService> _mockLogService;
        private BallStateConfiguration _configuration;
        private BallStateMachine _stateMachine;
        private PhysicsEngine _physicsEngine;
        private BallModel _ballModel;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _configuration = BallStateConfiguration.CreateDefault();
            _stateMachine = new BallStateMachine(_mockLogService.Object, _configuration);
            _physicsEngine = new PhysicsEngine();
            
            // Create a simple ball model for testing
            _ballModel = new BallModel(100, 100, 25);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _stateMachine = null;
            _mockLogService = null;
            _configuration = null;
            _physicsEngine = null;
            _ballModel = null;
        }

        #region Complete Ball Interaction Workflow Tests

        [TestMethod]
        public void CompleteWorkflow_MouseDownDragReleasePhysicsIdle_StateTransitionsCorrect()
        {
            // Arrange
            var stateTransitions = new List<(BallState Previous, BallState New, BallTrigger Trigger)>();

            // Track state transitions
            _stateMachine.StateChanged += (sender, e) =>
            {
                stateTransitions.Add((e.PreviousState, e.NewState, e.Trigger));
            };

            // Initial state verification
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            var initialX = _ballModel.X;
            var initialY = _ballModel.Y;

            // Act - Complete interaction workflow

            // 1. Mouse Down (Grab ball) - Idle → Held
            _stateMachine.Fire(BallTrigger.MouseDown);
            
            // Verify Held state
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // 2. Simulate dragging (position changes while held)
            _ballModel.SetPosition(initialX + 50, initialY + 30);
            var draggedX = _ballModel.X;
            var draggedY = _ballModel.Y;

            // Verify position changed during drag
            Assert.AreNotEqual(initialX, draggedX);
            Assert.AreNotEqual(initialY, draggedY);

            // 3. Release (Throw ball) - Held → Thrown
            // Set velocity before release to simulate throw
            _ballModel.SetVelocity(80, 60); // Above threshold velocity
            _stateMachine.Fire(BallTrigger.Release);

            // Verify Thrown state
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // 4. Physics simulation - ball should move while thrown
            var physicsSteps = 10;
            var timeStep = 1.0 / 60.0; // 60 FPS
            
            for (int i = 0; i < physicsSteps; i++)
            {
                var result = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                    _stateMachine.CurrentState, _configuration);
                
                if (result.VelocityBelowThreshold)
                {
                    // 5. Velocity below threshold - Thrown → Idle
                    _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                    break;
                }
            }

            // Final state verification
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);

            // Assert - Verify complete state transition sequence
            Assert.AreEqual(3, stateTransitions.Count);
            
            // Verify transition 1: Idle → Held
            Assert.AreEqual(BallState.Idle, stateTransitions[0].Previous);
            Assert.AreEqual(BallState.Held, stateTransitions[0].New);
            Assert.AreEqual(BallTrigger.MouseDown, stateTransitions[0].Trigger);

            // Verify transition 2: Held → Thrown
            Assert.AreEqual(BallState.Held, stateTransitions[1].Previous);
            Assert.AreEqual(BallState.Thrown, stateTransitions[1].New);
            Assert.AreEqual(BallTrigger.Release, stateTransitions[1].Trigger);

            // Verify transition 3: Thrown → Idle
            Assert.AreEqual(BallState.Thrown, stateTransitions[2].Previous);
            Assert.AreEqual(BallState.Idle, stateTransitions[2].New);
            Assert.AreEqual(BallTrigger.VelocityBelowThreshold, stateTransitions[2].Trigger);
        }

        [TestMethod]
        public void CompleteWorkflow_StateTransitionTiming_OccursAtCorrectTimes()
        {
            // Arrange
            var transitionTimestamps = new List<(BallState State, DateTime Timestamp)>();
            
            _stateMachine.StateChanged += (sender, e) =>
            {
                transitionTimestamps.Add((e.NewState, e.Timestamp));
            };

            var startTime = DateTime.Now;

            // Act - Execute workflow with timing verification
            
            // 1. Mouse down should transition immediately
            _stateMachine.Fire(BallTrigger.MouseDown);
            var heldTime = DateTime.Now;
            
            // 2. Release should transition immediately
            _ballModel.SetVelocity(100, 75); // High velocity
            _stateMachine.Fire(BallTrigger.Release);
            var thrownTime = DateTime.Now;
            
            // 3. Velocity threshold should be detected during physics update
            _ballModel.SetVelocity(10, 10); // Low velocity
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            var idleTime = DateTime.Now;

            // Assert - Verify timing constraints
            Assert.AreEqual(3, transitionTimestamps.Count);

            // Verify transitions occurred in correct order and timing
            var heldTransition = transitionTimestamps.Find(t => t.State == BallState.Held);
            var thrownTransition = transitionTimestamps.Find(t => t.State == BallState.Thrown);
            var idleTransition = transitionTimestamps.Find(t => t.State == BallState.Idle);

            // Verify chronological order
            Assert.IsTrue(heldTransition.Timestamp <= thrownTransition.Timestamp);
            Assert.IsTrue(thrownTransition.Timestamp <= idleTransition.Timestamp);

            // Verify transitions occurred within reasonable time windows
            Assert.IsTrue((heldTransition.Timestamp - startTime).TotalMilliseconds < 100);
            Assert.IsTrue((thrownTransition.Timestamp - heldTime).TotalMilliseconds < 100);
            Assert.IsTrue((idleTransition.Timestamp - thrownTime).TotalMilliseconds < 100);
        }

        [TestMethod]
        public void CompleteWorkflow_PhysicsBehavior_MatchesStateRequirements()
        {
            // Arrange
            var initialX = _ballModel.X;
            var initialY = _ballModel.Y;
            var timeStep = 1.0 / 60.0;

            // Act & Assert - Test physics behavior in each state

            // 1. Idle state - physics should apply normally
            _ballModel.SetVelocity(60, 40);
            var idleResult = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                BallState.Idle, _configuration);
            
            Assert.IsTrue(idleResult.IsMoving, "Ball should move in Idle state with velocity");
            var idleX = _ballModel.X;
            var idleY = _ballModel.Y;
            Assert.AreNotEqual(initialX, idleX, "Ball X should change in Idle state");
            Assert.AreNotEqual(initialY, idleY, "Ball Y should change in Idle state");

            // 2. Transition to Held state - physics should be paused
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            var heldStartX = _ballModel.X;
            var heldStartY = _ballModel.Y;
            
            var heldResult = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                BallState.Held, _configuration);
            
            Assert.IsFalse(heldResult.IsMoving, "Ball should not move in Held state");
            Assert.AreEqual(heldStartX, _ballModel.X, 0.001, "Ball X should not change in Held state");
            Assert.AreEqual(heldStartY, _ballModel.Y, 0.001, "Ball Y should not change in Held state");

            // 3. Transition to Thrown state - physics should resume
            _ballModel.SetVelocity(90, 70); // High velocity
            _stateMachine.Fire(BallTrigger.Release);
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            var thrownStartX = _ballModel.X;
            var thrownStartY = _ballModel.Y;
            
            var thrownResult = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                BallState.Thrown, _configuration);
            
            Assert.IsTrue(thrownResult.IsMoving, "Ball should move in Thrown state");
            Assert.AreNotEqual(thrownStartX, _ballModel.X, "Ball X should change in Thrown state");
            Assert.AreNotEqual(thrownStartY, _ballModel.Y, "Ball Y should change in Thrown state");

            // 4. Reduce velocity below threshold
            _ballModel.SetVelocity(20, 15); // Below threshold (50.0)
            
            var settlingResult = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                BallState.Thrown, _configuration);
            
            Assert.IsTrue(settlingResult.VelocityBelowThreshold, "Should detect velocity below threshold");
            Assert.IsFalse(settlingResult.IsMoving, "Ball should not be moving when below threshold");

            // 5. Transition back to Idle
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void CompleteWorkflow_BoundaryCollisions_StateTransitionsCorrect()
        {
            // Arrange - Position ball near boundary
            _ballModel.SetPosition(750, 300); // Near right boundary (assuming 800px width)
            _ballModel.SetVelocity(100, 0); // Moving right towards boundary

            // Act - Complete workflow with boundary collision

            // 1. Start in Idle, transition to Held
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

            // 2. Release near boundary
            _stateMachine.Fire(BallTrigger.Release);
            Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

            // 3. Simulate physics with boundary collision
            var timeStep = 1.0 / 60.0;
            var maxSteps = 100;
            var collisionDetected = false;
            
            for (int i = 0; i < maxSteps; i++)
            {
                var result = _physicsEngine.UpdateBall(_ballModel, timeStep, 0, 0, 800, 600, 
                    _stateMachine.CurrentState, _configuration);
                
                if (result.HitRight || result.HitLeft || result.HitTop || result.HitBottom)
                {
                    collisionDetected = true;
                    // Ball should still be in Thrown state after collision
                    Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState, 
                        "Ball should remain in Thrown state after boundary collision");
                }
                
                if (result.VelocityBelowThreshold)
                {
                    _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                    break;
                }
            }

            // Assert
            Assert.IsTrue(collisionDetected, "Boundary collision should have been detected");
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState, 
                "Ball should end in Idle state after settling");
        }

        [TestMethod]
        public void CompleteWorkflow_MultipleInteractions_EachCycleCorrect()
        {
            // Arrange
            var completedCycles = 0;
            var stateTransitionCounts = new Dictionary<BallState, int>();
            
            _stateMachine.StateChanged += (sender, e) =>
            {
                if (!stateTransitionCounts.ContainsKey(e.NewState))
                    stateTransitionCounts[e.NewState] = 0;
                stateTransitionCounts[e.NewState]++;
                
                if (e.NewState == BallState.Idle && e.PreviousState == BallState.Thrown)
                    completedCycles++;
            };

            // Act - Perform multiple complete interaction cycles
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Verify starting in Idle
                Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState, 
                    $"Cycle {cycle + 1} should start in Idle state");

                // Mouse down
                _stateMachine.Fire(BallTrigger.MouseDown);
                Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);

                // Simulate drag
                _ballModel.SetPosition(_ballModel.X + (cycle * 10), _ballModel.Y + (cycle * 5));

                // Release with velocity
                _ballModel.SetVelocity(80 + (cycle * 10), 60 + (cycle * 5));
                _stateMachine.Fire(BallTrigger.Release);
                Assert.AreEqual(BallState.Thrown, _stateMachine.CurrentState);

                // Simulate physics until settling
                var steps = 0;
                while (steps < 50) // Prevent infinite loop
                {
                    var result = _physicsEngine.UpdateBall(_ballModel, 1.0/60.0, 0, 0, 800, 600, 
                        _stateMachine.CurrentState, _configuration);
                    
                    if (result.VelocityBelowThreshold)
                    {
                        _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                        break;
                    }
                    
                    // Gradually reduce velocity to simulate settling
                    if (steps > 20)
                    {
                        _ballModel.SetVelocity(_ballModel.VelocityX * 0.9, _ballModel.VelocityY * 0.9);
                    }
                    
                    steps++;
                }

                // Verify cycle completed
                Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState, 
                    $"Cycle {cycle + 1} should end in Idle state");
            }

            // Assert - Verify all cycles completed correctly
            Assert.AreEqual(3, completedCycles, "All 3 cycles should have completed");
            
            // Verify state transition counts (3 cycles × 3 transitions per cycle = 9 total)
            // Plus initial Idle state = 4 Idle, 3 Held, 3 Thrown
            Assert.AreEqual(4, stateTransitionCounts[BallState.Idle], "Should have 4 Idle transitions");
            Assert.AreEqual(3, stateTransitionCounts[BallState.Held], "Should have 3 Held transitions");
            Assert.AreEqual(3, stateTransitionCounts[BallState.Thrown], "Should have 3 Thrown transitions");
        }



        #endregion
    }
}