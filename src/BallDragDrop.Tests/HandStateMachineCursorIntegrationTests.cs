using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.Services;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for hand state machine and cursor system
    /// </summary>
    [TestClass]
    public class HandStateMachineCursorIntegrationTests
    {
        #region Fields

        /// <summary>
        /// Mock configuration service for testing
        /// </summary>
        private Mock<IConfigurationService> _mockConfigurationService;

        /// <summary>
        /// Mock log service for testing
        /// </summary>
        private Mock<ILogService> _mockLogService;

        /// <summary>
        /// Mock cursor image loader for testing
        /// </summary>
        private Mock<CursorImageLoader> _mockImageLoader;

        /// <summary>
        /// Cursor configuration for testing
        /// </summary>
        private CursorConfiguration _cursorConfiguration;

        /// <summary>
        /// Hand state machine for testing
        /// </summary>
        private HandStateMachine _handStateMachine;

        /// <summary>
        /// Cursor manager for testing
        /// </summary>
        private CursorManager _cursorManager;

        /// <summary>
        /// Test directory for temporary files
        /// </summary>
        private string _testDirectory;

        #endregion Fields

        #region TestInitialize

        /// <summary>
        /// Initializes test dependencies before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockLogService = new Mock<ILogService>();
            _mockImageLoader = new Mock<CursorImageLoader>(_mockLogService.Object);

            // Create test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "CursorIntegrationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            _cursorConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = true,
                DefaultCursorPath = "Resources/Cursors/default.png",
                HoverCursorPath = "Resources/Cursors/hover.png",
                GrabbingCursorPath = "Resources/Cursors/grabbing.png",
                ReleasingCursorPath = "Resources/Cursors/releasing.png",
                DebounceTimeMs = 16,
                ReleasingDurationMs = 200
            };

            // Setup mock configuration service
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(_cursorConfiguration);
            _mockConfigurationService.Setup(x => x.ValidateCursorConfiguration(It.IsAny<CursorConfiguration>()))
                .Returns(true);

            // Setup mock image loader to return different cursors for different states
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("default.png")), It.IsAny<Cursor>()))
                .Returns(Cursors.Arrow);
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("hover.png")), It.IsAny<Cursor>()))
                .Returns(Cursors.Hand);
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("grabbing.png")), It.IsAny<Cursor>()))
                .Returns(Cursors.SizeAll);
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("releasing.png")), It.IsAny<Cursor>()))
                .Returns(Cursors.Cross);

            // Create cursor manager
            _cursorManager = new CursorManager(
                _mockConfigurationService.Object,
                _mockLogService.Object,
                _mockImageLoader.Object,
                _cursorConfiguration);

            // Create hand state machine with cursor manager
            _handStateMachine = new HandStateMachine(
                _mockLogService.Object,
                _cursorManager,
                _cursorConfiguration);
        }

        #endregion TestInitialize

        #region TestCleanup

        /// <summary>
        /// Cleans up test resources after each test
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion TestCleanup

        #region Hand State Machine and Cursor Integration Tests

        /// <summary>
        /// Tests that hand state changes trigger cursor updates
        /// </summary>
        [TestMethod]
        public void HandStateChange_ShouldTriggerCursorUpdate()
        {
            // Arrange
            bool cursorUpdateTriggered = false;
            _handStateMachine.StateChanged += (sender, args) =>
            {
                cursorUpdateTriggered = true;
            };

            // Act
            _handStateMachine.Fire(HandTrigger.MouseOverBall);

            // Assert
            Assert.IsTrue(cursorUpdateTriggered);
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            
            // Verify cursor manager was called
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Hover"));
        }

        /// <summary>
        /// Tests complete workflow from Default to Hover to Grabbing to Releasing to Default
        /// </summary>
        [TestMethod]
        public void CompleteHandStateWorkflow_ShouldUpdateCursorsCorrectly()
        {
            // Arrange
            var stateChanges = new List<HandState>();
            _handStateMachine.StateChanged += (sender, args) =>
            {
                stateChanges.Add(args.NewState);
            };

            // Act - Complete workflow
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Default -> Hover
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);

            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Hover -> Grabbing
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);

            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Grabbing -> Releasing
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            _handStateMachine.Fire(HandTrigger.ReleaseComplete); // Releasing -> Default
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Assert
            Assert.AreEqual(4, stateChanges.Count);
            Assert.AreEqual(HandState.Hover, stateChanges[0]);
            Assert.AreEqual(HandState.Grabbing, stateChanges[1]);
            Assert.AreEqual(HandState.Releasing, stateChanges[2]);
            Assert.AreEqual(HandState.Default, stateChanges[3]);

            // Verify cursor manager state tracking
            var finalState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(finalState.Contains("HandState: Default"));
        }

        /// <summary>
        /// Tests that ball state changes trigger appropriate hand state changes and cursor updates
        /// </summary>
        [TestMethod]
        public void BallStateChanges_ShouldTriggerHandStateAndCursorUpdates()
        {
            // Arrange
            var handStateChanges = new List<HandState>();
            _handStateMachine.StateChanged += (sender, args) =>
            {
                handStateChanges.Add(args.NewState);
            };

            // Act - Simulate ball state changes
            _handStateMachine.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown);
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);

            _handStateMachine.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release);
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            _handStateMachine.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.Reset);
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Assert
            Assert.AreEqual(3, handStateChanges.Count);
            Assert.AreEqual(HandState.Grabbing, handStateChanges[0]);
            Assert.AreEqual(HandState.Releasing, handStateChanges[1]);
            Assert.AreEqual(HandState.Default, handStateChanges[2]);

            // Verify cursor updates were triggered
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Queued cursor update")), 
                It.IsAny<object[]>()), Times.AtLeast(3));
        }

        /// <summary>
        /// Tests mouse interaction scenarios trigger correct cursor updates
        /// </summary>
        [TestMethod]
        public void MouseInteractionScenarios_ShouldUpdateCursorsCorrectly()
        {
            // Arrange
            var stateChanges = new List<HandState>();
            _handStateMachine.StateChanged += (sender, args) =>
            {
                stateChanges.Add(args.NewState);
            };

            // Act - Mouse interaction scenario
            _handStateMachine.OnMouseOverBall(); // Should go to Hover
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);

            _handStateMachine.OnDragStart(); // Should go to Grabbing
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);

            _handStateMachine.OnDragStop(); // Should go to Releasing
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            _handStateMachine.OnMouseLeaveBall(); // Should stay in Releasing (ignored)
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            // Wait for releasing timer to complete
            Thread.Sleep(250);
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Assert
            Assert.AreEqual(4, stateChanges.Count);
            Assert.AreEqual(HandState.Hover, stateChanges[0]);
            Assert.AreEqual(HandState.Grabbing, stateChanges[1]);
            Assert.AreEqual(HandState.Releasing, stateChanges[2]);
            Assert.AreEqual(HandState.Default, stateChanges[3]);
        }

        /// <summary>
        /// Tests that cursor loading is attempted for each hand state during transitions
        /// </summary>
        [TestMethod]
        public void HandStateTransitions_ShouldAttemptCursorLoadingForEachState()
        {
            // Act - Transition through all states
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Default -> Hover
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Hover -> Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Grabbing -> Releasing
            _handStateMachine.Fire(HandTrigger.ReleaseComplete); // Releasing -> Default

            // Assert - Verify image loader was called for each cursor type
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("default.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("hover.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("grabbing.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("releasing.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
        }

        #endregion Hand State Machine and Cursor Integration Tests

        #region Configuration Reload Integration Tests

        /// <summary>
        /// Tests that configuration reload affects both hand state machine and cursor system
        /// </summary>
        [TestMethod]
        public async Task ConfigurationReload_ShouldAffectBothHandStateMachineAndCursorSystem()
        {
            // Arrange
            var newConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = false,
                DebounceTimeMs = 32,
                ReleasingDurationMs = 100
            };

            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(newConfiguration);

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Trigger a state change to test the new configuration
            _handStateMachine.Fire(HandTrigger.MouseOverBall);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CustomCursors: Disabled"));
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);

            _mockLogService.Verify(x => x.LogInformation(
                It.Is<string>(s => s.Contains("Cursor configuration reloaded successfully")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests dynamic cursor updates when configuration changes
        /// </summary>
        [TestMethod]
        public async Task DynamicConfigurationUpdate_ShouldUpdateCursorsImmediately()
        {
            // Arrange - Start with custom cursors enabled
            Assert.IsTrue(_cursorConfiguration.EnableCustomCursors);
            _handStateMachine.Fire(HandTrigger.MouseOverBall);
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);

            // Act - Disable custom cursors
            var updatedConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = false,
                DefaultCursorPath = _cursorConfiguration.DefaultCursorPath,
                HoverCursorPath = _cursorConfiguration.HoverCursorPath,
                GrabbingCursorPath = _cursorConfiguration.GrabbingCursorPath,
                ReleasingCursorPath = _cursorConfiguration.ReleasingCursorPath,
                DebounceTimeMs = _cursorConfiguration.DebounceTimeMs,
                ReleasingDurationMs = _cursorConfiguration.ReleasingDurationMs
            };

            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(updatedConfiguration);

            await _cursorManager.ReloadConfigurationAsync();

            // Trigger another state change to test updated configuration
            _handStateMachine.Fire(HandTrigger.StartGrabbing);

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CustomCursors: Disabled"));

            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Custom cursors disabled")), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        #endregion Configuration Reload Integration Tests

        #region Error Handling Integration Tests

        /// <summary>
        /// Tests that cursor loading errors don't break hand state machine functionality
        /// </summary>
        [TestMethod]
        public void CursorLoadingErrors_ShouldNotBreakHandStateMachine()
        {
            // Arrange - Setup image loader to throw exceptions
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Throws(new InvalidOperationException("Test cursor loading error"));

            var stateChanges = new List<HandState>();
            _handStateMachine.StateChanged += (sender, args) =>
            {
                stateChanges.Add(args.NewState);
            };

            // Act - Hand state machine should continue working despite cursor errors
            _handStateMachine.Fire(HandTrigger.MouseOverBall);
            _handStateMachine.Fire(HandTrigger.StartGrabbing);
            _handStateMachine.Fire(HandTrigger.StopGrabbing);

            // Assert
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);
            Assert.AreEqual(3, stateChanges.Count);

            // Verify errors were logged but didn't stop functionality
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Error setting cursor")), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that hand state machine errors don't break cursor system
        /// </summary>
        [TestMethod]
        public void HandStateMachineErrors_ShouldNotBreakCursorSystem()
        {
            // Arrange - Force an error in hand state machine by using invalid trigger
            var invalidTrigger = (HandTrigger)999;

            // Act - Cursor system should continue working despite hand state machine errors
            _handStateMachine.Fire(invalidTrigger);
            
            // Normal operation should still work
            _cursorManager.SetCursorForHandState(HandState.Hover);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState); // Should recover to default
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Hover")); // Cursor manager should still work

            _mockLogService.Verify(x => x.LogError(
                It.Is<string>(s => s.Contains("Invalid hand state trigger value")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests recovery mechanisms when both systems encounter errors
        /// </summary>
        [TestMethod]
        public void BothSystemsWithErrors_ShouldRecoverGracefully()
        {
            // Arrange - Setup multiple error scenarios
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Throws(new OutOfMemoryException("Test memory error"));

            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Throws(new InvalidOperationException("Test configuration error"));

            // Act - Both systems should handle errors gracefully
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Should work despite cursor errors
            
            // Try to reload configuration
            var reloadTask = _cursorManager.ReloadConfigurationAsync();
            reloadTask.Wait();

            // Assert
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            
            // Both systems should log errors but continue functioning
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.IsAny<string>(), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        #endregion Error Handling Integration Tests

        #region Performance Integration Tests

        /// <summary>
        /// Tests performance of integrated system with rapid state changes
        /// </summary>
        [TestMethod]
        public void RapidStateChanges_ShouldMaintainPerformance()
        {
            // Arrange
            var startTime = DateTime.Now;
            var stateChangeCount = 0;
            _handStateMachine.StateChanged += (sender, args) => stateChangeCount++;

            // Act - Rapid state changes
            for (int i = 0; i < 100; i++)
            {
                _handStateMachine.Fire(HandTrigger.MouseOverBall);
                _handStateMachine.Fire(HandTrigger.MouseLeaveBall);
                _handStateMachine.Fire(HandTrigger.StartGrabbing);
                _handStateMachine.Fire(HandTrigger.StopGrabbing);
                _handStateMachine.Fire(HandTrigger.ReleaseComplete);
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Assert
            Assert.IsTrue(duration.TotalMilliseconds < 1000, 
                $"Rapid state changes took {duration.TotalMilliseconds}ms, expected < 1000ms");
            Assert.AreEqual(500, stateChangeCount); // 5 changes per iteration * 100 iterations
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Verify debouncing is working
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Queued cursor update")), 
                It.IsAny<object[]>()), Times.AtLeast(1));
        }

        /// <summary>
        /// Tests memory usage of integrated system over time
        /// </summary>
        [TestMethod]
        public void IntegratedSystemMemoryUsage_ShouldBeStable()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Extended operation simulation
            for (int i = 0; i < 50; i++)
            {
                // Simulate complete user interaction cycle
                _handStateMachine.OnMouseOverBall();
                _handStateMachine.OnDragStart();
                _handStateMachine.OnDragStop();
                
                // Wait for releasing timer
                Thread.Sleep(10);
                
                _handStateMachine.Reset();
                
                // Trigger cursor updates
                _cursorManager.SetCursorForHandState(HandState.Default);
                _cursorManager.SetCursorForHandState(HandState.Hover);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 2 * 1024 * 1024, 
                $"Memory increased by {memoryIncrease} bytes, expected < 2MB");
        }

        /// <summary>
        /// Tests cursor update timing meets 16ms requirement
        /// </summary>
        [TestMethod]
        public void CursorUpdateTiming_ShouldMeet16MsRequirement()
        {
            // Arrange
            var updateTimes = new List<TimeSpan>();
            var states = new[] { HandState.Default, HandState.Hover, HandState.Grabbing, HandState.Releasing };

            // Act - Measure cursor update times
            foreach (var state in states)
            {
                var startTime = DateTime.Now;
                _cursorManager.SetCursorForHandState(state);
                var endTime = DateTime.Now;
                updateTimes.Add(endTime - startTime);
            }

            // Assert
            foreach (var updateTime in updateTimes)
            {
                Assert.IsTrue(updateTime.TotalMilliseconds < 16, 
                    $"Cursor update took {updateTime.TotalMilliseconds}ms, expected < 16ms");
            }

            // Verify all updates completed quickly
            var maxUpdateTime = updateTimes.Max();
            Assert.IsTrue(maxUpdateTime.TotalMilliseconds < 16, 
                $"Maximum cursor update time was {maxUpdateTime.TotalMilliseconds}ms, expected < 16ms");
        }

        #endregion Performance Integration Tests

        #region End-to-End Workflow Tests

        /// <summary>
        /// Tests complete end-to-end user interaction workflow
        /// </summary>
        [TestMethod]
        public void CompleteUserInteractionWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange
            var workflowEvents = new List<string>();
            _handStateMachine.StateChanged += (sender, args) =>
            {
                workflowEvents.Add($"HandState: {args.PreviousState} -> {args.NewState} via {args.Trigger}");
            };

            // Act - Simulate complete user interaction
            // 1. User moves mouse over ball
            _handStateMachine.OnMouseOverBall();
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);

            // 2. User starts dragging
            _handStateMachine.OnDragStart();
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);

            // 3. Ball state changes to held (integration with ball state machine)
            _handStateMachine.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown);
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState); // Should stay in grabbing

            // 4. User stops dragging
            _handStateMachine.OnDragStop();
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            // 5. Ball state changes to thrown
            _handStateMachine.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release);
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState); // Should stay in releasing

            // 6. Wait for releasing timer to complete
            Thread.Sleep(250);
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // 7. Ball eventually stops and goes idle
            _handStateMachine.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.VelocityBelowThreshold);
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState); // Should stay in default

            // Assert
            Assert.AreEqual(4, workflowEvents.Count);
            Assert.IsTrue(workflowEvents[0].Contains("Default -> Hover"));
            Assert.IsTrue(workflowEvents[1].Contains("Hover -> Grabbing"));
            Assert.IsTrue(workflowEvents[2].Contains("Grabbing -> Releasing"));
            Assert.IsTrue(workflowEvents[3].Contains("Releasing -> Default"));

            // Verify cursor system tracked all state changes
            var finalState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(finalState.Contains("HandState: Default"));
            Assert.IsTrue(finalState.Contains("CustomCursors: Enabled"));
        }

        /// <summary>
        /// Tests error recovery during complete workflow
        /// </summary>
        [TestMethod]
        public void WorkflowWithErrors_ShouldRecoverAndContinue()
        {
            // Arrange - Setup intermittent errors
            var callCount = 0;
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Returns((string path, Cursor fallback) =>
                {
                    callCount++;
                    if (callCount % 3 == 0) // Every third call fails
                    {
                        throw new InvalidOperationException("Intermittent error");
                    }
                    return fallback;
                });

            var stateChanges = new List<HandState>();
            _handStateMachine.StateChanged += (sender, args) => stateChanges.Add(args.NewState);

            // Act - Complete workflow with errors
            _handStateMachine.Fire(HandTrigger.MouseOverBall);
            _handStateMachine.Fire(HandTrigger.StartGrabbing);
            _handStateMachine.Fire(HandTrigger.StopGrabbing);
            _handStateMachine.Fire(HandTrigger.ReleaseComplete);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.AreEqual(4, stateChanges.Count);

            // Verify errors were handled gracefully
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.IsAny<string>(), 
                It.IsAny<object[]>()), Times.AtLeastOnce);

            // System should still be functional
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsNotNull(currentState);
            Assert.IsTrue(currentState.Contains("HandState: Default"));
        }

        #endregion End-to-End Workflow Tests
    }
}