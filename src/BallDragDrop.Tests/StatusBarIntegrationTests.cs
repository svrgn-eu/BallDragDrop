using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using BallDragDrop.ViewModels;
using BallDragDrop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for status bar integration with ball state machine.
    /// Tests status bar updates immediately on state changes and correct formatting.
    /// </summary>
    [TestClass]
    public class StatusBarIntegrationTests
    {
        private Mock<ILogService> _mockLogService;
        private BallStateConfiguration _configuration;
        private BallStateMachine _stateMachine;
        private StatusBarViewModel _statusBarViewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _configuration = BallStateConfiguration.CreateDefault();
            _stateMachine = new BallStateMachine(_mockLogService.Object, _configuration);
            _statusBarViewModel = new StatusBarViewModel(_mockLogService.Object, _stateMachine);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _statusBarViewModel?.Dispose();
            _stateMachine = null;
            _mockLogService = null;
            _configuration = null;
        }

        #region Status Bar Integration Tests

        [TestMethod]
        public void StatusBar_InitialState_DisplaysIdleCorrectly()
        {
            // Assert - Initial state should be displayed as "Ball: Idle"
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
        }

        [TestMethod]
        public void StatusBar_StateTransitions_UpdatesImmediately()
        {
            // Arrange
            var statusUpdates = new List<(string Status, DateTime Timestamp)>();
            var propertyChangeCount = 0;

            _statusBarViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_statusBarViewModel.Status))
                {
                    statusUpdates.Add((_statusBarViewModel.Status, DateTime.Now));
                    propertyChangeCount++;
                }
            };

            var startTime = DateTime.Now;

            // Act - Perform state transitions
            
            // Transition 1: Idle → Held
            _stateMachine.Fire(BallTrigger.MouseDown);
            var heldTime = DateTime.Now;
            
            // Transition 2: Held → Thrown
            _stateMachine.Fire(BallTrigger.Release);
            var thrownTime = DateTime.Now;
            
            // Transition 3: Thrown → Idle
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            var idleTime = DateTime.Now;

            // Assert - Verify immediate updates
            Assert.AreEqual(3, statusUpdates.Count, "Should have 3 status updates");
            Assert.AreEqual(3, propertyChangeCount, "Should have 3 property change notifications");

            // Verify status content
            Assert.AreEqual("Ball: Held", statusUpdates[0].Status);
            Assert.AreEqual("Ball: Thrown", statusUpdates[1].Status);
            Assert.AreEqual("Ball: Idle", statusUpdates[2].Status);

            // Verify timing - updates should occur within 100ms of state changes
            Assert.IsTrue((statusUpdates[0].Timestamp - heldTime).TotalMilliseconds < 100, 
                "Held status should update within 100ms");
            Assert.IsTrue((statusUpdates[1].Timestamp - thrownTime).TotalMilliseconds < 100, 
                "Thrown status should update within 100ms");
            Assert.IsTrue((statusUpdates[2].Timestamp - idleTime).TotalMilliseconds < 100, 
                "Idle status should update within 100ms");

            // Verify final state
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StatusBar_AllStateFormats_DisplayCorrectly()
        {
            // Test each state format individually
            
            // Test Idle state (initial)
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
            
            // Test Held state
            _stateMachine.Fire(BallTrigger.MouseDown);
            Assert.AreEqual("Ball: Held", _statusBarViewModel.Status);
            
            // Test Thrown state
            _stateMachine.Fire(BallTrigger.Release);
            Assert.AreEqual("Ball: Thrown", _statusBarViewModel.Status);
            
            // Test back to Idle state
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StatusBar_RapidStateChanges_HandlesAllUpdates()
        {
            // Arrange
            var statusUpdates = new List<string>();
            
            _statusBarViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_statusBarViewModel.Status))
                {
                    statusUpdates.Add(_statusBarViewModel.Status);
                }
            };

            // Act - Perform rapid state transitions
            for (int i = 0; i < 5; i++)
            {
                _stateMachine.Fire(BallTrigger.MouseDown); // Idle → Held
                _stateMachine.Fire(BallTrigger.Release); // Held → Thrown
                _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown → Idle
            }

            // Assert - All transitions should be captured
            Assert.AreEqual(15, statusUpdates.Count, "Should have 15 status updates (3 per cycle × 5 cycles)");
            
            // Verify pattern repeats correctly
            for (int i = 0; i < 5; i++)
            {
                int baseIndex = i * 3;
                Assert.AreEqual("Ball: Held", statusUpdates[baseIndex], $"Cycle {i + 1} should start with Held");
                Assert.AreEqual("Ball: Thrown", statusUpdates[baseIndex + 1], $"Cycle {i + 1} should continue with Thrown");
                Assert.AreEqual("Ball: Idle", statusUpdates[baseIndex + 2], $"Cycle {i + 1} should end with Idle");
            }

            // Verify final state
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StatusBar_StateConsistency_AlwaysMatchesStateMachine()
        {
            // Arrange
            var consistencyChecks = new List<bool>();
            
            _statusBarViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_statusBarViewModel.Status))
                {
                    // Check consistency between status display and actual state
                    var expectedStatus = FormatStateForDisplay(_stateMachine.CurrentState);
                    var isConsistent = _statusBarViewModel.Status == expectedStatus;
                    consistencyChecks.Add(isConsistent);
                }
            };

            // Act - Perform various state transitions
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle → Held
            _stateMachine.Fire(BallTrigger.Release); // Held → Thrown
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown → Idle
            
            // Additional transitions
            _stateMachine.Fire(BallTrigger.MouseDown); // Idle → Held
            _stateMachine.Fire(BallTrigger.Release); // Held → Thrown
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold); // Thrown → Idle

            // Assert - All consistency checks should pass
            Assert.IsTrue(consistencyChecks.Count > 0, "Should have performed consistency checks");
            Assert.IsTrue(consistencyChecks.TrueForAll(check => check), 
                "Status display should always be consistent with state machine state");

            // Final consistency check
            var finalExpectedStatus = FormatStateForDisplay(_stateMachine.CurrentState);
            Assert.AreEqual(finalExpectedStatus, _statusBarViewModel.Status, 
                "Final status should match final state");
        }

        [TestMethod]
        public void StatusBar_CompleteInteractionWorkflow_ShowsCorrectStatesThroughout()
        {
            // Arrange
            var workflowStates = new List<(BallState MachineState, string StatusDisplay, DateTime Timestamp)>();
            
            _stateMachine.StateChanged += (sender, e) =>
            {
                // Capture state machine state and corresponding status display
                workflowStates.Add((e.NewState, _statusBarViewModel.Status, DateTime.Now));
            };

            // Act - Complete interaction workflow
            
            // 1. Mouse down (grab ball)
            _stateMachine.Fire(BallTrigger.MouseDown);
            
            // 2. Release (throw ball)
            _stateMachine.Fire(BallTrigger.Release);
            
            // 3. Ball settles (velocity below threshold)
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);

            // Assert - Verify complete workflow state display
            Assert.AreEqual(3, workflowStates.Count, "Should have 3 state transitions");

            // Verify each transition shows correct status
            Assert.AreEqual(BallState.Held, workflowStates[0].MachineState);
            Assert.AreEqual("Ball: Held", workflowStates[0].StatusDisplay);

            Assert.AreEqual(BallState.Thrown, workflowStates[1].MachineState);
            Assert.AreEqual("Ball: Thrown", workflowStates[1].StatusDisplay);

            Assert.AreEqual(BallState.Idle, workflowStates[2].MachineState);
            Assert.AreEqual("Ball: Idle", workflowStates[2].StatusDisplay);

            // Verify chronological order
            Assert.IsTrue(workflowStates[0].Timestamp <= workflowStates[1].Timestamp);
            Assert.IsTrue(workflowStates[1].Timestamp <= workflowStates[2].Timestamp);

            // Verify final state
            Assert.AreEqual(BallState.Idle, _stateMachine.CurrentState);
            Assert.AreEqual("Ball: Idle", _statusBarViewModel.Status);
        }

        [TestMethod]
        public void StatusBar_ErrorScenarios_HandlesGracefully()
        {
            // Arrange
            var statusUpdates = new List<string>();
            
            _statusBarViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_statusBarViewModel.Status))
                {
                    statusUpdates.Add(_statusBarViewModel.Status);
                }
            };

            // Act - Try invalid transitions (should be ignored by state machine)
            try
            {
                // Try to go directly from Idle to Thrown (invalid)
                // This should be ignored by the state machine
                if (_stateMachine.CanFire(BallTrigger.VelocityBelowThreshold))
                {
                    _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                }
                
                // Try to release without holding (invalid)
                if (_stateMachine.CanFire(BallTrigger.Release))
                {
                    _stateMachine.Fire(BallTrigger.Release);
                }
            }
            catch (Exception)
            {
                // Invalid transitions might throw exceptions, which is acceptable
            }

            // Perform valid transition
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Assert - Status should remain consistent despite invalid attempts
            Assert.AreEqual("Ball: Held", _statusBarViewModel.Status);
            Assert.AreEqual(BallState.Held, _stateMachine.CurrentState);
            
            // Should have only one valid status update
            Assert.IsTrue(statusUpdates.Count <= 1, "Should have at most one valid status update");
            if (statusUpdates.Count == 1)
            {
                Assert.AreEqual("Ball: Held", statusUpdates[0]);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to format state for display (matches StatusBarViewModel logic)
        /// </summary>
        private string FormatStateForDisplay(BallState state)
        {
            return state switch
            {
                BallState.Idle => "Ball: Idle",
                BallState.Held => "Ball: Held",
                BallState.Thrown => "Ball: Thrown",
                _ => "Ball: Unknown"
            };
        }

        #endregion
    }
}