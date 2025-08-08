using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.Services;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for HandStateMachine class
    /// </summary>
    [TestClass]
    public class HandStateMachineTests
    {
        #region Fields

        /// <summary>
        /// Mock log service for testing
        /// </summary>
        private Mock<ILogService> _mockLogService;

        /// <summary>
        /// Mock cursor service for testing
        /// </summary>
        private Mock<ICursorService> _mockCursorService;

        /// <summary>
        /// Cursor configuration for testing
        /// </summary>
        private CursorConfiguration _cursorConfiguration;

        /// <summary>
        /// Hand state machine under test
        /// </summary>
        private HandStateMachine _handStateMachine;

        #endregion Fields

        #region TestInitialize

        /// <summary>
        /// Initializes test dependencies before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _mockCursorService = new Mock<ICursorService>();
            _cursorConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = true,
                DebounceTimeMs = 16,
                ReleasingDurationMs = 200
            };

            _handStateMachine = new HandStateMachine(
                _mockLogService.Object,
                _mockCursorService.Object,
                _cursorConfiguration);
        }

        #endregion TestInitialize

        #region Construction Tests

        /// <summary>
        /// Tests that HandStateMachine initializes with default state
        /// </summary>
        [TestMethod]
        public void Constructor_ShouldInitializeWithDefaultState()
        {
            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null log service
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullLogService_ShouldThrowArgumentNullException()
        {
            // Act
            new HandStateMachine(null, _mockCursorService.Object, _cursorConfiguration);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null cursor service
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullCursorService_ShouldThrowArgumentNullException()
        {
            // Act
            new HandStateMachine(_mockLogService.Object, null, _cursorConfiguration);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null cursor configuration
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullCursorConfiguration_ShouldThrowArgumentNullException()
        {
            // Act
            new HandStateMachine(_mockLogService.Object, _mockCursorService.Object, null);
        }

        #endregion Construction Tests

        #region State Transition Tests

        /// <summary>
        /// Tests transition from Default to Hover state
        /// </summary>
        [TestMethod]
        public void Fire_MouseOverBallFromDefault_ShouldTransitionToHover()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.MouseOverBall);

            // Assert
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Hover, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.MouseOverBall, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Hover to Default state
        /// </summary>
        [TestMethod]
        public void Fire_MouseLeaveBallFromHover_ShouldTransitionToDefault()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.MouseLeaveBall);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.MouseLeaveBall, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Default to Grabbing state
        /// </summary>
        [TestMethod]
        public void Fire_StartGrabbingFromDefault_ShouldTransitionToGrabbing()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.StartGrabbing);

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Grabbing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StartGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Hover to Grabbing state
        /// </summary>
        [TestMethod]
        public void Fire_StartGrabbingFromHover_ShouldTransitionToGrabbing()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.StartGrabbing);

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Grabbing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StartGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Grabbing to Releasing state
        /// </summary>
        [TestMethod]
        public void Fire_StopGrabbingFromGrabbing_ShouldTransitionToReleasing()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.StopGrabbing);

            // Assert
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Grabbing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Releasing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StopGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Releasing to Default state
        /// </summary>
        [TestMethod]
        public void Fire_ReleaseCompleteFromReleasing_ShouldTransitionToDefault()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Move to Releasing
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.ReleaseComplete);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Releasing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.ReleaseComplete, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests transition from Releasing to Hover state
        /// </summary>
        [TestMethod]
        public void Fire_MouseOverBallFromReleasing_ShouldTransitionToHover()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Move to Releasing
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.MouseOverBall);

            // Assert
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Releasing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Hover, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.MouseOverBall, eventArgs.Trigger);
        }

        #endregion State Transition Tests

        #region CanFire Tests

        /// <summary>
        /// Tests CanFire returns true for valid transitions from Default state
        /// </summary>
        [TestMethod]
        public void CanFire_ValidTriggersFromDefault_ShouldReturnTrue()
        {
            // Assert
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.MouseOverBall));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.StartGrabbing));
        }

        /// <summary>
        /// Tests CanFire returns false for invalid transitions from Default state
        /// </summary>
        [TestMethod]
        public void CanFire_InvalidTriggersFromDefault_ShouldReturnFalse()
        {
            // Assert
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.MouseLeaveBall));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.StopGrabbing));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.ReleaseComplete));
        }

        /// <summary>
        /// Tests CanFire returns true for valid transitions from Hover state
        /// </summary>
        [TestMethod]
        public void CanFire_ValidTriggersFromHover_ShouldReturnTrue()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover

            // Assert
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.MouseLeaveBall));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.StartGrabbing));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.Reset));
        }

        /// <summary>
        /// Tests CanFire returns false for invalid transitions from Hover state
        /// </summary>
        [TestMethod]
        public void CanFire_InvalidTriggersFromHover_ShouldReturnFalse()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover

            // Assert
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.MouseOverBall));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.StopGrabbing));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.ReleaseComplete));
        }

        /// <summary>
        /// Tests CanFire returns true for valid transitions from Grabbing state
        /// </summary>
        [TestMethod]
        public void CanFire_ValidTriggersFromGrabbing_ShouldReturnTrue()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing

            // Assert
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.StopGrabbing));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.Reset));
        }

        /// <summary>
        /// Tests CanFire returns false for invalid transitions from Grabbing state
        /// </summary>
        [TestMethod]
        public void CanFire_InvalidTriggersFromGrabbing_ShouldReturnFalse()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing

            // Assert
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.MouseOverBall));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.MouseLeaveBall));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.StartGrabbing));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.ReleaseComplete));
        }

        /// <summary>
        /// Tests CanFire returns true for valid transitions from Releasing state
        /// </summary>
        [TestMethod]
        public void CanFire_ValidTriggersFromReleasing_ShouldReturnTrue()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Move to Releasing

            // Assert
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.ReleaseComplete));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.MouseOverBall));
            Assert.IsTrue(_handStateMachine.CanFire(HandTrigger.Reset));
        }

        /// <summary>
        /// Tests CanFire returns false for invalid transitions from Releasing state
        /// </summary>
        [TestMethod]
        public void CanFire_InvalidTriggersFromReleasing_ShouldReturnFalse()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Move to Releasing

            // Assert
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.MouseLeaveBall));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.StartGrabbing));
            Assert.IsFalse(_handStateMachine.CanFire(HandTrigger.StopGrabbing));
        }

        #endregion CanFire Tests

        #region Reset Tests

        /// <summary>
        /// Tests Reset method returns to Default state from any state
        /// </summary>
        [TestMethod]
        public void Reset_FromAnyState_ShouldReturnToDefault()
        {
            // Test from Hover
            _handStateMachine.Fire(HandTrigger.MouseOverBall);
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            _handStateMachine.Reset();
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Test from Grabbing
            _handStateMachine.Fire(HandTrigger.StartGrabbing);
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            _handStateMachine.Reset();
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);

            // Test from Releasing
            _handStateMachine.Fire(HandTrigger.StartGrabbing);
            _handStateMachine.Fire(HandTrigger.StopGrabbing);
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);
            _handStateMachine.Reset();
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
        }

        /// <summary>
        /// Tests Reset method fires StateChanged event
        /// </summary>
        [TestMethod]
        public void Reset_ShouldFireStateChangedEvent()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Reset();

            // Assert
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.Reset, eventArgs.Trigger);
        }

        #endregion Reset Tests
       
        #region Ball State Observer Integration Tests

        /// <summary>
        /// Tests ball state change from Idle to Held triggers StartGrabbing
        /// </summary>
        [TestMethod]
        public void OnStateChanged_BallIdleToHeldViaMouseDown_ShouldTriggerStartGrabbing()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.MouseDown);

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Grabbing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StartGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests ball state change from Held to Thrown triggers StopGrabbing
        /// </summary>
        [TestMethod]
        public void OnStateChanged_BallHeldToThrownViaRelease_ShouldTriggerStopGrabbing()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnStateChanged(BallState.Held, BallState.Thrown, BallTrigger.Release);

            // Assert
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Grabbing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Releasing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StopGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests ball state change to Idle via VelocityBelowThreshold does not change hand state
        /// </summary>
        [TestMethod]
        public void OnStateChanged_BallToIdleViaVelocityThreshold_ShouldNotChangeHandState()
        {
            // Arrange
            var initialState = _handStateMachine.CurrentState;
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.VelocityBelowThreshold);

            // Assert
            Assert.AreEqual(initialState, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests ball state change to Idle via Reset triggers Reset
        /// </summary>
        [TestMethod]
        public void OnStateChanged_BallToIdleViaReset_ShouldTriggerReset()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnStateChanged(BallState.Thrown, BallState.Idle, BallTrigger.Reset);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.Reset, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests ball state change with unexpected trigger still processes correctly
        /// </summary>
        [TestMethod]
        public void OnStateChanged_BallHeldWithUnexpectedTrigger_ShouldStillTriggerStartGrabbing()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnStateChanged(BallState.Idle, BallState.Held, BallTrigger.Reset); // Unexpected trigger

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Grabbing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StartGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests ball state change with invalid enum values logs error and recovers
        /// </summary>
        [TestMethod]
        public void OnStateChanged_WithInvalidBallState_ShouldLogErrorAndRecover()
        {
            // Arrange
            var invalidBallState = (BallState)999;

            // Act
            _handStateMachine.OnStateChanged(BallState.Idle, invalidBallState, BallTrigger.MouseDown);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            _mockLogService.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        #endregion Ball State Observer Integration Tests

        #region Mouse Event Handling Tests

        /// <summary>
        /// Tests OnMouseOverBall method triggers MouseOverBall
        /// </summary>
        [TestMethod]
        public void OnMouseOverBall_FromDefault_ShouldTransitionToHover()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnMouseOverBall();

            // Assert
            Assert.AreEqual(HandState.Hover, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Hover, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.MouseOverBall, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests OnMouseOverBall while in Grabbing state is ignored
        /// </summary>
        [TestMethod]
        public void OnMouseOverBall_WhileGrabbing_ShouldBeIgnored()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.OnMouseOverBall();

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests OnMouseLeaveBall method triggers MouseLeaveBall
        /// </summary>
        [TestMethod]
        public void OnMouseLeaveBall_FromHover_ShouldTransitionToDefault()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnMouseLeaveBall();

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.MouseLeaveBall, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests OnMouseLeaveBall while in Grabbing state is ignored
        /// </summary>
        [TestMethod]
        public void OnMouseLeaveBall_WhileGrabbing_ShouldBeIgnored()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.OnMouseLeaveBall();

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests OnMouseLeaveBall while already in Default state is ignored
        /// </summary>
        [TestMethod]
        public void OnMouseLeaveBall_WhileInDefault_ShouldBeIgnored()
        {
            // Arrange
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.OnMouseLeaveBall();

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests OnDragStart method triggers StartGrabbing
        /// </summary>
        [TestMethod]
        public void OnDragStart_FromDefault_ShouldTransitionToGrabbing()
        {
            // Arrange
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnDragStart();

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Default, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Grabbing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StartGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests OnDragStart while already in Grabbing state is ignored
        /// </summary>
        [TestMethod]
        public void OnDragStart_WhileAlreadyGrabbing_ShouldBeIgnored()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing first
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.OnDragStart();

            // Assert
            Assert.AreEqual(HandState.Grabbing, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests OnDragStop method triggers StopGrabbing
        /// </summary>
        [TestMethod]
        public void OnDragStop_FromGrabbing_ShouldTransitionToReleasing()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing first
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnDragStop();

            // Assert
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Grabbing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Releasing, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.StopGrabbing, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests OnDragStop while not in Grabbing state transitions to Default
        /// </summary>
        [TestMethod]
        public void OnDragStop_WhileNotGrabbing_ShouldTransitionToDefault()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.OnDragStop();

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.Reset, eventArgs.Trigger);
        }

        #endregion Mouse Event Handling Tests

        #region Error Handling and Recovery Tests

        /// <summary>
        /// Tests Fire with invalid trigger enum value logs error and recovers
        /// </summary>
        [TestMethod]
        public void Fire_WithInvalidTriggerValue_ShouldLogErrorAndRecover()
        {
            // Arrange
            var invalidTrigger = (HandTrigger)999;

            // Act
            _handStateMachine.Fire(invalidTrigger);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            _mockLogService.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests Fire with invalid transition logs warning but doesn't crash
        /// </summary>
        [TestMethod]
        public void Fire_WithInvalidTransition_ShouldLogWarningAndContinue()
        {
            // Arrange - try to fire MouseLeaveBall from Default state (invalid)
            var initialState = _handStateMachine.CurrentState;

            // Act
            _handStateMachine.Fire(HandTrigger.MouseLeaveBall);

            // Assert
            Assert.AreEqual(initialState, _handStateMachine.CurrentState);
            _mockLogService.Verify(x => x.LogWarning(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests graceful handling of duplicate StartGrabbing triggers
        /// </summary>
        [TestMethod]
        public void Fire_DuplicateStartGrabbing_ShouldBeHandledGracefully()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing first
            var initialState = _handStateMachine.CurrentState;
            bool eventFired = false;
            _handStateMachine.StateChanged += (sender, args) => eventFired = true;

            // Act
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Duplicate

            // Assert
            Assert.AreEqual(initialState, _handStateMachine.CurrentState);
            Assert.IsFalse(eventFired);
        }

        /// <summary>
        /// Tests graceful handling of StopGrabbing when not grabbing
        /// </summary>
        [TestMethod]
        public void Fire_StopGrabbingWhenNotGrabbing_ShouldTransitionToDefault()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.MouseOverBall); // Move to Hover
            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _handStateMachine.Fire(HandTrigger.StopGrabbing);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Hover, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.Reset, eventArgs.Trigger);
        }

        /// <summary>
        /// Tests Reset trigger is always available from any state
        /// </summary>
        [TestMethod]
        public void Fire_ResetFromAnyState_ShouldAlwaysWork()
        {
            // Test from each state
            var states = new[] { HandState.Default, HandState.Hover, HandState.Grabbing, HandState.Releasing };
            
            foreach (var targetState in states)
            {
                // Move to target state
                _handStateMachine.Reset(); // Start from Default
                switch (targetState)
                {
                    case HandState.Hover:
                        _handStateMachine.Fire(HandTrigger.MouseOverBall);
                        break;
                    case HandState.Grabbing:
                        _handStateMachine.Fire(HandTrigger.StartGrabbing);
                        break;
                    case HandState.Releasing:
                        _handStateMachine.Fire(HandTrigger.StartGrabbing);
                        _handStateMachine.Fire(HandTrigger.StopGrabbing);
                        break;
                }

                // Verify we're in the target state
                Assert.AreEqual(targetState, _handStateMachine.CurrentState);

                // Test Reset works
                _handStateMachine.Fire(HandTrigger.Reset);
                Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            }
        }

        /// <summary>
        /// Tests releasing timer functionality with timeout
        /// </summary>
        [TestMethod]
        public void ReleasingState_ShouldAutomaticallyTransitionToDefaultAfterTimeout()
        {
            // Arrange
            _handStateMachine.Fire(HandTrigger.StartGrabbing); // Move to Grabbing
            _handStateMachine.Fire(HandTrigger.StopGrabbing); // Move to Releasing
            Assert.AreEqual(HandState.Releasing, _handStateMachine.CurrentState);

            HandStateChangedEventArgs eventArgs = null;
            _handStateMachine.StateChanged += (sender, args) => eventArgs = args;

            // Act - Wait for releasing timer (200ms + buffer)
            Thread.Sleep(300);

            // Assert
            Assert.AreEqual(HandState.Default, _handStateMachine.CurrentState);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(HandState.Releasing, eventArgs.PreviousState);
            Assert.AreEqual(HandState.Default, eventArgs.NewState);
            Assert.AreEqual(HandTrigger.ReleaseComplete, eventArgs.Trigger);
        }

        #endregion Error Handling and Recovery Tests
    }
}
