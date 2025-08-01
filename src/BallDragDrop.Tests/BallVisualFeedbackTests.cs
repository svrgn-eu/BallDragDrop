using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using BallDragDrop.Models;
using BallDragDrop.Services;
using BallDragDrop.Contracts;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Tests for ball visual feedback responsiveness and state-dependent styling
    /// </summary>
    [TestClass]
    public class BallVisualFeedbackTests
    {
        private BallViewModel _ballViewModel;
        private MockLogService _mockLogService;
        private BallStateMachine _stateMachine;
        private BallStateConfiguration _configuration;

        [TestInitialize]
        public void Setup()
        {
            _mockLogService = new MockLogService();
            _configuration = new BallStateConfiguration();
            _stateMachine = new BallStateMachine(_mockLogService, _configuration);
            _ballViewModel = new BallViewModel(_mockLogService, _stateMachine);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _ballViewModel?.Dispose();
        }

        /// <summary>
        /// Tests that visual properties change immediately when state transitions occur
        /// </summary>
        [TestMethod]
        public void VisualProperties_ShouldUpdateImmediatelyOnStateChange()
        {
            // Arrange
            var stopwatch = new Stopwatch();
            
            // Verify initial state (Idle)
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.AreEqual(1.0, _ballViewModel.StateOpacity);
            Assert.AreEqual(1.0, _ballViewModel.StateScale);
            Assert.AreEqual(0.0, _ballViewModel.StateGlowRadius);
            Assert.AreEqual(Colors.Transparent, _ballViewModel.StateGlowColor);
            Assert.AreEqual(0.0, _ballViewModel.StateBorderThickness);
            Assert.AreEqual(Colors.Transparent, _ballViewModel.StateBorderColor);

            // Act - Transition to Held state
            stopwatch.Start();
            _stateMachine.Fire(BallTrigger.MouseDown);
            stopwatch.Stop();

            // Assert - Visual properties should update immediately (within 100ms)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"State transition took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
            
            Assert.AreEqual(BallState.Held, _ballViewModel.CurrentState);
            Assert.AreEqual(0.8, _ballViewModel.StateOpacity);
            Assert.AreEqual(1.1, _ballViewModel.StateScale);
            Assert.AreEqual(8.0, _ballViewModel.StateGlowRadius);
            Assert.AreEqual(Colors.LightBlue, _ballViewModel.StateGlowColor);
            Assert.AreEqual(2.0, _ballViewModel.StateBorderThickness);
            Assert.AreEqual(Colors.Blue, _ballViewModel.StateBorderColor);
        }

        /// <summary>
        /// Tests visual feedback across all state transitions
        /// </summary>
        [TestMethod]
        public void VisualFeedback_ShouldWorkAcrossAllStateTransitions()
        {
            var stopwatch = new Stopwatch();

            // Test Idle -> Held transition
            stopwatch.Restart();
            _stateMachine.Fire(BallTrigger.MouseDown);
            stopwatch.Stop();
            
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Idle->Held transition took {stopwatch.ElapsedMilliseconds}ms");
            Assert.AreEqual(BallState.Held, _ballViewModel.CurrentState);
            Assert.AreEqual(0.8, _ballViewModel.StateOpacity);
            Assert.AreEqual(1.1, _ballViewModel.StateScale);

            // Test Held -> Thrown transition
            stopwatch.Restart();
            _stateMachine.Fire(BallTrigger.Release);
            stopwatch.Stop();
            
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Held->Thrown transition took {stopwatch.ElapsedMilliseconds}ms");
            Assert.AreEqual(BallState.Thrown, _ballViewModel.CurrentState);
            Assert.AreEqual(1.0, _ballViewModel.StateOpacity);
            Assert.AreEqual(1.0, _ballViewModel.StateScale);
            Assert.AreEqual(4.0, _ballViewModel.StateGlowRadius);
            Assert.AreEqual(Colors.Orange, _ballViewModel.StateGlowColor);
            Assert.AreEqual(1.0, _ballViewModel.StateBorderThickness);
            Assert.AreEqual(Colors.Red, _ballViewModel.StateBorderColor);

            // Test Thrown -> Idle transition
            stopwatch.Restart();
            _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
            stopwatch.Stop();
            
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Thrown->Idle transition took {stopwatch.ElapsedMilliseconds}ms");
            Assert.AreEqual(BallState.Idle, _ballViewModel.CurrentState);
            Assert.AreEqual(1.0, _ballViewModel.StateOpacity);
            Assert.AreEqual(1.0, _ballViewModel.StateScale);
            Assert.AreEqual(0.0, _ballViewModel.StateGlowRadius);
            Assert.AreEqual(Colors.Transparent, _ballViewModel.StateGlowColor);
        }

        /// <summary>
        /// Tests that visual consistency is maintained with state machine state
        /// </summary>
        [TestMethod]
        public void VisualProperties_ShouldMaintainConsistencyWithStateMachine()
        {
            // Test multiple rapid state transitions
            for (int i = 0; i < 10; i++)
            {
                // Idle -> Held
                _stateMachine.Fire(BallTrigger.MouseDown);
                Assert.AreEqual(_stateMachine.CurrentState, _ballViewModel.CurrentState);
                VerifyVisualPropertiesMatchState(_ballViewModel.CurrentState);

                // Held -> Thrown
                _stateMachine.Fire(BallTrigger.Release);
                Assert.AreEqual(_stateMachine.CurrentState, _ballViewModel.CurrentState);
                VerifyVisualPropertiesMatchState(_ballViewModel.CurrentState);

                // Thrown -> Idle
                _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                Assert.AreEqual(_stateMachine.CurrentState, _ballViewModel.CurrentState);
                VerifyVisualPropertiesMatchState(_ballViewModel.CurrentState);
            }
        }

        /// <summary>
        /// Tests that property change notifications are fired for visual properties
        /// </summary>
        [TestMethod]
        public void VisualProperties_ShouldFirePropertyChangeNotifications()
        {
            var propertyChangedEvents = new List<string>();
            _ballViewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName);

            // Trigger state change
            _stateMachine.Fire(BallTrigger.MouseDown);

            // Verify that visual property change notifications were fired
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateOpacity)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateScale)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateGlowRadius)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateGlowColor)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateBorderThickness)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(BallViewModel.StateBorderColor)));
        }

        /// <summary>
        /// Tests performance of visual property calculations
        /// </summary>
        [TestMethod]
        public void VisualProperties_ShouldCalculateQuickly()
        {
            var stopwatch = new Stopwatch();
            
            // Test performance of visual property getters
            stopwatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                var opacity = _ballViewModel.StateOpacity;
                var scale = _ballViewModel.StateScale;
                var glowRadius = _ballViewModel.StateGlowRadius;
                var glowColor = _ballViewModel.StateGlowColor;
                var borderThickness = _ballViewModel.StateBorderThickness;
                var borderColor = _ballViewModel.StateBorderColor;
            }
            stopwatch.Stop();

            // Visual property calculations should be very fast (< 10ms for 1000 calculations)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10, 
                $"Visual property calculations took {stopwatch.ElapsedMilliseconds}ms for 1000 iterations");
        }

        /// <summary>
        /// Verifies that visual properties match the expected values for a given state
        /// </summary>
        /// <param name="state">The ball state to verify</param>
        private void VerifyVisualPropertiesMatchState(BallState state)
        {
            switch (state)
            {
                case BallState.Idle:
                    Assert.AreEqual(1.0, _ballViewModel.StateOpacity);
                    Assert.AreEqual(1.0, _ballViewModel.StateScale);
                    Assert.AreEqual(0.0, _ballViewModel.StateGlowRadius);
                    Assert.AreEqual(Colors.Transparent, _ballViewModel.StateGlowColor);
                    Assert.AreEqual(0.0, _ballViewModel.StateBorderThickness);
                    Assert.AreEqual(Colors.Transparent, _ballViewModel.StateBorderColor);
                    break;

                case BallState.Held:
                    Assert.AreEqual(0.8, _ballViewModel.StateOpacity);
                    Assert.AreEqual(1.1, _ballViewModel.StateScale);
                    Assert.AreEqual(8.0, _ballViewModel.StateGlowRadius);
                    Assert.AreEqual(Colors.LightBlue, _ballViewModel.StateGlowColor);
                    Assert.AreEqual(2.0, _ballViewModel.StateBorderThickness);
                    Assert.AreEqual(Colors.Blue, _ballViewModel.StateBorderColor);
                    break;

                case BallState.Thrown:
                    Assert.AreEqual(1.0, _ballViewModel.StateOpacity);
                    Assert.AreEqual(1.0, _ballViewModel.StateScale);
                    Assert.AreEqual(4.0, _ballViewModel.StateGlowRadius);
                    Assert.AreEqual(Colors.Orange, _ballViewModel.StateGlowColor);
                    Assert.AreEqual(1.0, _ballViewModel.StateBorderThickness);
                    Assert.AreEqual(Colors.Red, _ballViewModel.StateBorderColor);
                    break;

                default:
                    Assert.Fail($"Unexpected state: {state}");
                    break;
            }
        }

        /// <summary>
        /// Mock implementation of ILogService for testing
        /// </summary>
        private class MockLogService : ILogService
        {
            public void LogTrace(string message, params object[] args) { }
            public void LogDebug(string message, params object[] args) { }
            public void LogInformation(string message, params object[] args) { }
            public void LogWarning(string message, params object[] args) { }
            public void LogError(string message, params object[] args) { }
            public void LogError(Exception exception, string message, params object[] args) { }
            public void LogCritical(string message, params object[] args) { }
            public void LogCritical(Exception exception, string message, params object[] args) { }
            public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues) { }
            public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues) { }
            public IDisposable BeginScope(string scopeName, params object[] parameters) => new MockScope();
            public void LogMethodEntry(string methodName, params object[] parameters) { }
            public void LogMethodExit(string methodName, object? returnValue = null, TimeSpan? duration = null) { }
            public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData) { }
            public void SetCorrelationId(string correlationId) { }
            public string GetCorrelationId() => Guid.NewGuid().ToString();
            
            private class MockScope : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}