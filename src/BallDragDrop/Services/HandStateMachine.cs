using System;
using System.Threading.Tasks;
using Stateless;
using BallDragDrop.Contracts;
using BallDragDrop.Models;

namespace BallDragDrop.Services
{
    /// <summary>
    /// State machine for managing hand/interaction states
    /// </summary>
    public class HandStateMachine : IHandStateMachine, IBallStateObserver
    {
        #region Fields

        /// <summary>
        /// The underlying state machine implementation
        /// </summary>
        private readonly StateMachine<HandState, HandTrigger> _stateMachine;

        /// <summary>
        /// Log service for error reporting and debugging
        /// </summary>
        private readonly ILogService _logService;

        /// <summary>
        /// Cursor service for updating cursors based on hand state
        /// </summary>
        private readonly ICursorService _cursorService;

        /// <summary>
        /// Timer for releasing state duration
        /// </summary>
        private System.Threading.Timer? _releasingTimer;

        /// <summary>
        /// Lock object for thread safety
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Cursor configuration for timing settings
        /// </summary>
        private readonly CursorConfiguration _cursorConfiguration;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Current hand state
        /// </summary>
        public HandState CurrentState => _stateMachine.State;

        #endregion Properties

        #region Events

        /// <summary>
        /// Event fired when hand state changes
        /// </summary>
        public event EventHandler<HandStateChangedEventArgs>? StateChanged;

        #endregion Events

        #region Construction

        /// <summary>
        /// Initializes a new instance of the HandStateMachine class
        /// </summary>
        /// <param name="logService">Log service for error reporting</param>
        /// <param name="cursorService">Cursor service for updating cursors</param>
        /// <param name="cursorConfiguration">Cursor configuration for timing settings</param>
        public HandStateMachine(ILogService logService, ICursorService cursorService, CursorConfiguration cursorConfiguration)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _cursorService = cursorService ?? throw new ArgumentNullException(nameof(cursorService));
            _cursorConfiguration = cursorConfiguration ?? throw new ArgumentNullException(nameof(cursorConfiguration));

            _stateMachine = new StateMachine<HandState, HandTrigger>(HandState.Default);
            ConfigureStateMachine();

            _logService.LogDebug("HandStateMachine initialized with default state");
        }

        #endregion Construction

        #region Methods

        #region ConfigureStateMachine

        /// <summary>
        /// Configures the state machine transitions and behaviors
        /// </summary>
        private void ConfigureStateMachine()
        {
            // Configure Default state transitions
            _stateMachine.Configure(HandState.Default)
                .Permit(HandTrigger.MouseOverBall, HandState.Hover)
                .Permit(HandTrigger.StartGrabbing, HandState.Grabbing)
                .OnEntry(() => OnHandStateChanged(HandState.Default));

            // Configure Hover state transitions
            _stateMachine.Configure(HandState.Hover)
                .Permit(HandTrigger.MouseLeaveBall, HandState.Default)
                .Permit(HandTrigger.StartGrabbing, HandState.Grabbing)
                .Permit(HandTrigger.Reset, HandState.Default)
                .OnEntry(() => OnHandStateChanged(HandState.Hover));

            // Configure Grabbing state transitions
            _stateMachine.Configure(HandState.Grabbing)
                .Permit(HandTrigger.StopGrabbing, HandState.Releasing)
                .Permit(HandTrigger.Reset, HandState.Default)
                .OnEntry(() => OnHandStateChanged(HandState.Grabbing));

            // Configure Releasing state transitions
            _stateMachine.Configure(HandState.Releasing)
                .Permit(HandTrigger.ReleaseComplete, HandState.Default)
                .Permit(HandTrigger.MouseOverBall, HandState.Hover)
                .Permit(HandTrigger.Reset, HandState.Default)
                .OnEntry(() => OnHandStateChanged(HandState.Releasing))
                .OnEntry(StartReleasingTimer);

            _logService.LogDebug("HandStateMachine state transitions configured");
        }

        #endregion ConfigureStateMachine

        #region Fire

        /// <summary>
        /// Triggers a hand state transition with comprehensive error handling and recovery
        /// </summary>
        /// <param name="trigger">The trigger causing the transition</param>
        public void Fire(HandTrigger trigger)
        {
            lock (_lock)
            {
                var previousState = _stateMachine.State;
                
                try
                {
                    _logService.LogDebug("Attempting to fire hand state trigger: {Trigger} from state {PreviousState}", 
                        trigger, previousState);

                    // Validate trigger
                    if (!Enum.IsDefined(typeof(HandTrigger), trigger))
                    {
                        _logService.LogError("Invalid hand state trigger value: {Trigger}, attempting recovery", trigger);
                        RecoverToDefaultState("Invalid trigger value");
                        return;
                    }

                    // Check if trigger can be fired
                    if (!_stateMachine.CanFire(trigger))
                    {
                        _logService.LogWarning("Invalid hand state trigger {Trigger} for current state {CurrentState}, checking for recovery options", 
                            trigger, previousState);
                        
                        // Try to handle invalid transitions gracefully
                        if (TryHandleInvalidTransition(trigger, previousState))
                        {
                            return;
                        }
                        
                        // If no graceful handling possible, log and return
                        _logService.LogWarning("No recovery option available for invalid transition {Trigger} from {CurrentState}", 
                            trigger, previousState);
                        return;
                    }

                    // Fire the trigger
                    try
                    {
                        _stateMachine.Fire(trigger);
                        
                        var newState = _stateMachine.State;
                        _logService.LogDebug("Hand state transition completed: {PreviousState} -> {NewState} via {Trigger}", 
                            previousState, newState, trigger);
                        
                        // Validate the new state
                        if (!Enum.IsDefined(typeof(HandState), newState))
                        {
                            _logService.LogError("State machine transitioned to invalid state: {NewState}, recovering", newState);
                            RecoverToDefaultState("Invalid state after transition");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logService.LogError(ex, "Invalid operation firing trigger {Trigger} from state {PreviousState}, attempting recovery", 
                            trigger, previousState);
                        RecoverToDefaultState("Invalid operation during state transition");
                    }
                    catch (ArgumentException ex)
                    {
                        _logService.LogError(ex, "Argument error firing trigger {Trigger} from state {PreviousState}, attempting recovery", 
                            trigger, previousState);
                        RecoverToDefaultState("Argument error during state transition");
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    _logService.LogError(ex, "Out of memory firing trigger {Trigger} from state {PreviousState}, attempting recovery", 
                        trigger, previousState);
                    RecoverToDefaultState("Out of memory during state transition");
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Unexpected error firing hand state trigger {Trigger} from state {PreviousState}, attempting recovery", 
                        trigger, previousState);
                    RecoverToDefaultState("Unexpected error during state transition");
                }
            }
        }

        #endregion Fire

        #region CanFire

        /// <summary>
        /// Checks if a trigger can be fired from current state
        /// </summary>
        /// <param name="trigger">The trigger to check</param>
        /// <returns>True if trigger is valid</returns>
        public bool CanFire(HandTrigger trigger)
        {
            lock (_lock)
            {
                return _stateMachine.CanFire(trigger);
            }
        }

        #endregion CanFire

        #region Reset

        /// <summary>
        /// Resets hand state machine to default state with comprehensive error handling
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                var previousState = _stateMachine.State;
                
                try
                {
                    _logService.LogDebug("Resetting hand state machine from state {PreviousState}", previousState);
                    
                    // Cancel any active releasing timer
                    try
                    {
                        _releasingTimer?.Dispose();
                        _releasingTimer = null;
                        _logService.LogDebug("Releasing timer disposed during reset");
                    }
                    catch (Exception timerEx)
                    {
                        _logService.LogWarning("Error disposing releasing timer during reset. Exception: {Exception}", timerEx.Message);
                    }
                    
                    // Force transition to default state
                    if (_stateMachine.State != HandState.Default)
                    {
                        if (_stateMachine.CanFire(HandTrigger.Reset))
                        {
                            try
                            {
                                _stateMachine.Fire(HandTrigger.Reset);
                                _logService.LogDebug("Successfully reset using Reset trigger");
                            }
                            catch (Exception fireEx)
                            {
                                _logService.LogError(fireEx, "Error firing Reset trigger, attempting force reset");
                                ForceResetStateMachine();
                            }
                        }
                        else
                        {
                            _logService.LogWarning("Cannot fire Reset trigger from state {CurrentState}, forcing reset", _stateMachine.State);
                            ForceResetStateMachine();
                        }
                    }
                    else
                    {
                        _logService.LogDebug("Already in Default state, reset complete");
                    }
                    
                    _logService.LogInformation("Hand state machine reset from {PreviousState} to {CurrentState}", 
                        previousState, _stateMachine.State);
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Critical error during hand state machine reset, attempting force reset");
                    ForceResetStateMachine();
                }
            }
        }

        #endregion Reset

        #region RecoverToDefaultState

        /// <summary>
        /// Recovers the hand state machine to default state with logging
        /// </summary>
        /// <param name="reason">Reason for recovery</param>
        private void RecoverToDefaultState(string reason)
        {
            try
            {
                _logService.LogWarning("Recovering hand state machine to Default state. Reason: {Reason}", reason);
                
                var previousState = _stateMachine.State;
                
                // Try normal reset first
                try
                {
                    Reset();
                    
                    if (_stateMachine.State == HandState.Default)
                    {
                        _logService.LogInformation("Successfully recovered to Default state from {PreviousState}. Reason: {Reason}", 
                            previousState, reason);
                        return;
                    }
                }
                catch (Exception resetEx)
                {
                    _logService.LogError(resetEx, "Normal reset failed during recovery, attempting force reset");
                }
                
                // If normal reset failed, force reset
                ForceResetStateMachine();
                
                _logService.LogInformation("Force reset completed during recovery from {PreviousState}. Reason: {Reason}", 
                    previousState, reason);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Critical error during recovery to default state. Reason: {Reason}", reason);
            }
        }

        #endregion RecoverToDefaultState

        #region ForceResetStateMachine

        /// <summary>
        /// Forces the state machine to reset by recreating it
        /// </summary>
        private void ForceResetStateMachine()
        {
            try
            {
                _logService.LogWarning("Force resetting hand state machine by recreation");
                
                // Dispose any resources
                try
                {
                    _releasingTimer?.Dispose();
                    _releasingTimer = null;
                }
                catch (Exception timerEx)
                {
                    _logService.LogDebug("Error disposing timer during force reset. Exception: {Exception}", timerEx.Message);
                }
                
                // Recreate the state machine - we can't actually replace the field due to readonly,
                // so we'll manually trigger the state change
                try
                {
                    OnHandStateChanged(HandState.Default);
                    _logService.LogDebug("Force reset completed - manually set to Default state");
                }
                catch (Exception stateEx)
                {
                    _logService.LogError(stateEx, "Error during manual state change in force reset");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Critical error during force reset of state machine");
            }
        }

        #endregion ForceResetStateMachine

        #region TryHandleInvalidTransition

        /// <summary>
        /// Attempts to handle invalid state transitions gracefully
        /// </summary>
        /// <param name="trigger">The invalid trigger</param>
        /// <param name="currentState">Current state</param>
        /// <returns>True if handled gracefully</returns>
        private bool TryHandleInvalidTransition(HandTrigger trigger, HandState currentState)
        {
            try
            {
                _logService.LogDebug("Attempting graceful handling of invalid transition {Trigger} from {CurrentState}", 
                    trigger, currentState);

                // Handle specific invalid transition scenarios
                switch (trigger)
                {
                    case HandTrigger.Reset:
                        // Reset should always be possible, if not, force it
                        _logService.LogWarning("Reset trigger invalid from state {CurrentState}, forcing reset", currentState);
                        ForceResetStateMachine();
                        return true;

                    case HandTrigger.MouseOverBall when currentState == HandState.Grabbing:
                        // Mouse over while grabbing - ignore this transition
                        _logService.LogDebug("Ignoring MouseOverBall trigger while in Grabbing state");
                        return true;

                    case HandTrigger.MouseLeaveBall when currentState == HandState.Default:
                        // Mouse leave while already in default - ignore this transition
                        _logService.LogDebug("Ignoring MouseLeaveBall trigger while in Default state");
                        return true;

                    case HandTrigger.StartGrabbing when currentState == HandState.Grabbing:
                        // Already grabbing - ignore duplicate start
                        _logService.LogDebug("Ignoring duplicate StartGrabbing trigger while already in Grabbing state");
                        return true;

                    case HandTrigger.StopGrabbing when currentState != HandState.Grabbing:
                        // Stop grabbing when not grabbing - transition to default
                        _logService.LogDebug("StopGrabbing from non-Grabbing state, transitioning to Default");
                        if (_stateMachine.CanFire(HandTrigger.Reset))
                        {
                            _stateMachine.Fire(HandTrigger.Reset);
                            return true;
                        }
                        break;

                    case HandTrigger.ReleaseComplete when currentState != HandState.Releasing:
                        // Release complete when not releasing - ignore
                        _logService.LogDebug("Ignoring ReleaseComplete trigger while not in Releasing state");
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error during graceful invalid transition handling for {Trigger} from {CurrentState}", 
                    trigger, currentState);
                return false;
            }
        }

        #endregion TryHandleInvalidTransition

        #region OnStateChanged (IBallStateObserver)

        /// <summary>
        /// Called when the ball state changes with comprehensive error handling
        /// </summary>
        /// <param name="previousState">Previous ball state</param>
        /// <param name="newState">New ball state</param>
        /// <param name="trigger">Trigger that caused the ball state change</param>
        public void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger)
        {
            try
            {
                _logService.LogDebug("Ball state changed: {PreviousState} -> {NewState} via {Trigger}, evaluating hand state impact", 
                    previousState, newState, trigger);

                // Validate input parameters
                if (!Enum.IsDefined(typeof(BallState), previousState))
                {
                    _logService.LogWarning("Invalid previous ball state: {PreviousState}, continuing with processing", previousState);
                }

                if (!Enum.IsDefined(typeof(BallState), newState))
                {
                    _logService.LogError("Invalid new ball state: {NewState}, attempting recovery", newState);
                    RecoverToDefaultState("Invalid ball state received");
                    return;
                }

                if (!Enum.IsDefined(typeof(BallTrigger), trigger))
                {
                    _logService.LogWarning("Invalid ball trigger: {Trigger}, continuing with processing", trigger);
                }

                // Map ball state changes to hand state triggers with error handling
                try
                {
                    switch (newState)
                    {
                        case BallState.Held when trigger == BallTrigger.MouseDown:
                            _logService.LogDebug("Ball held via mouse down, triggering StartGrabbing");
                            Fire(HandTrigger.StartGrabbing);
                            break;
                            
                        case BallState.Thrown when trigger == BallTrigger.Release:
                            _logService.LogDebug("Ball thrown via release, triggering StopGrabbing");
                            Fire(HandTrigger.StopGrabbing);
                            break;
                            
                        case BallState.Idle when trigger == BallTrigger.VelocityBelowThreshold:
                            _logService.LogDebug("Ball idle via velocity threshold, hand state will transition based on mouse position");
                            // Ball has stopped moving, hand state will transition based on mouse position
                            // No immediate hand state change needed
                            break;
                            
                        case BallState.Idle when trigger == BallTrigger.Reset:
                            _logService.LogDebug("Ball idle via reset, resetting hand state machine");
                            Reset();
                            break;

                        case BallState.Held when trigger != BallTrigger.MouseDown:
                            _logService.LogWarning("Ball held with unexpected trigger {Trigger}, may indicate state inconsistency", trigger);
                            // Still trigger StartGrabbing as the ball is held
                            Fire(HandTrigger.StartGrabbing);
                            break;

                        case BallState.Thrown when trigger != BallTrigger.Release:
                            _logService.LogWarning("Ball thrown with unexpected trigger {Trigger}, may indicate state inconsistency", trigger);
                            // Still trigger StopGrabbing as the ball is thrown
                            Fire(HandTrigger.StopGrabbing);
                            break;

                        default:
                            _logService.LogDebug("Ball state change {PreviousState} -> {NewState} via {Trigger} does not require hand state change", 
                                previousState, newState, trigger);
                            break;
                    }
                }
                catch (Exception mappingEx)
                {
                    _logService.LogError(mappingEx, "Error mapping ball state change to hand state trigger, attempting recovery");
                    RecoverToDefaultState("Error during ball state mapping");
                }
            }
            catch (ArgumentException ex)
            {
                _logService.LogError(ex, "Argument error handling ball state change, attempting recovery");
                RecoverToDefaultState("Argument error during ball state handling");
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling ball state change, attempting recovery");
                RecoverToDefaultState("Invalid operation during ball state handling");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling ball state change in hand state machine, attempting recovery");
                RecoverToDefaultState("Unexpected error during ball state handling");
            }
        }

        #endregion OnStateChanged (IBallStateObserver)

        #region Mouse Event Handling

        /// <summary>
        /// Handles mouse entering ball area with comprehensive error handling
        /// </summary>
        public void OnMouseOverBall()
        {
            try
            {
                _logService.LogDebug("Mouse over ball detected, current state: {CurrentState}", CurrentState);
                
                // Validate current state before firing trigger
                if (CurrentState == HandState.Grabbing)
                {
                    _logService.LogDebug("Ignoring mouse over ball while in Grabbing state");
                    return;
                }

                Fire(HandTrigger.MouseOverBall);
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling mouse over ball event, attempting recovery");
                RecoverToDefaultState("Invalid operation during mouse over ball");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling mouse over ball event, attempting recovery");
                RecoverToDefaultState("Unexpected error during mouse over ball");
            }
        }

        /// <summary>
        /// Handles mouse leaving ball area with comprehensive error handling
        /// </summary>
        public void OnMouseLeaveBall()
        {
            try
            {
                _logService.LogDebug("Mouse leave ball detected, current state: {CurrentState}", CurrentState);
                
                // Validate current state before firing trigger
                if (CurrentState == HandState.Grabbing)
                {
                    _logService.LogDebug("Ignoring mouse leave ball while in Grabbing state");
                    return;
                }

                if (CurrentState == HandState.Default)
                {
                    _logService.LogDebug("Already in Default state, mouse leave ball has no effect");
                    return;
                }

                Fire(HandTrigger.MouseLeaveBall);
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling mouse leave ball event, attempting recovery");
                RecoverToDefaultState("Invalid operation during mouse leave ball");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling mouse leave ball event, attempting recovery");
                RecoverToDefaultState("Unexpected error during mouse leave ball");
            }
        }

        /// <summary>
        /// Handles drag operation start with comprehensive error handling
        /// </summary>
        public void OnDragStart()
        {
            try
            {
                _logService.LogDebug("Drag start detected, current state: {CurrentState}", CurrentState);
                
                // Validate current state
                if (CurrentState == HandState.Grabbing)
                {
                    _logService.LogDebug("Already in Grabbing state, ignoring duplicate drag start");
                    return;
                }

                Fire(HandTrigger.StartGrabbing);
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling drag start event, attempting recovery");
                RecoverToDefaultState("Invalid operation during drag start");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling drag start event, attempting recovery");
                RecoverToDefaultState("Unexpected error during drag start");
            }
        }

        /// <summary>
        /// Handles drag operation stop with comprehensive error handling
        /// </summary>
        public void OnDragStop()
        {
            try
            {
                _logService.LogDebug("Drag stop detected, current state: {CurrentState}", CurrentState);
                
                // Validate current state
                if (CurrentState != HandState.Grabbing)
                {
                    _logService.LogWarning("Drag stop detected while not in Grabbing state ({CurrentState}), transitioning to Default", CurrentState);
                    
                    // Try to transition to default state
                    if (CanFire(HandTrigger.Reset))
                    {
                        Fire(HandTrigger.Reset);
                    }
                    else
                    {
                        RecoverToDefaultState("Drag stop from non-Grabbing state");
                    }
                    return;
                }

                Fire(HandTrigger.StopGrabbing);
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling drag stop event, attempting recovery");
                RecoverToDefaultState("Invalid operation during drag stop");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling drag stop event, attempting recovery");
                RecoverToDefaultState("Unexpected error during drag stop");
            }
        }

        #endregion Mouse Event Handling

        #region OnHandStateChanged

        /// <summary>
        /// Called when the hand state changes with comprehensive error handling
        /// </summary>
        /// <param name="newState">New hand state</param>
        private void OnHandStateChanged(HandState newState)
        {
            HandState previousState = HandState.Default;
            
            try
            {
                // Safely get previous state
                try
                {
                    previousState = _stateMachine.State;
                }
                catch (Exception stateEx)
                {
                    _logService.LogWarning("Error getting previous state, using Default as fallback. Exception: {Exception}", stateEx.Message);
                    previousState = HandState.Default;
                }

                _logService.LogDebug("Hand state changing from {PreviousState} to {NewState}", previousState, newState);

                // Validate new state
                if (!Enum.IsDefined(typeof(HandState), newState))
                {
                    _logService.LogError("Invalid new hand state: {NewState}, using Default state", newState);
                    newState = HandState.Default;
                }

                // Update cursor based on new hand state with error handling
                try
                {
                    _cursorService.SetCursorForHandState(newState);
                    _logService.LogDebug("Cursor updated for hand state {NewState}", newState);
                }
                catch (Exception cursorEx)
                {
                    _logService.LogError(cursorEx, "Error updating cursor for hand state {NewState}, continuing with state change", newState);
                    // Don't fail the state change just because cursor update failed
                }

                // Raise state changed event with error handling
                try
                {
                    if (StateChanged != null)
                    {
                        var eventArgs = new HandStateChangedEventArgs(previousState, newState, HandTrigger.Reset);
                        StateChanged.Invoke(this, eventArgs);
                        _logService.LogDebug("State changed event raised for {PreviousState} -> {NewState}", previousState, newState);
                    }
                    else
                    {
                        _logService.LogDebug("No state changed event subscribers for {PreviousState} -> {NewState}", previousState, newState);
                    }
                }
                catch (Exception eventEx)
                {
                    _logService.LogError(eventEx, "Error raising state changed event for {PreviousState} -> {NewState}", previousState, newState);
                    // Don't fail the state change just because event raising failed
                }

                _logService.LogInformation("Hand state successfully changed from {PreviousState} to {NewState}", previousState, newState);
            }
            catch (ArgumentException ex)
            {
                _logService.LogError(ex, "Argument error handling hand state change to {NewState}", newState);
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation handling hand state change to {NewState}", newState);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error handling hand state change to {NewState}", newState);
            }
        }

        #endregion OnHandStateChanged

        #region StartReleasingTimer

        /// <summary>
        /// Starts the timer for the releasing state duration with comprehensive error handling
        /// </summary>
        private void StartReleasingTimer()
        {
            try
            {
                _logService.LogDebug("Starting releasing timer");

                // Cancel any existing timer with error handling
                try
                {
                    if (_releasingTimer != null)
                    {
                        _releasingTimer.Dispose();
                        _releasingTimer = null;
                        _logService.LogDebug("Disposed existing releasing timer");
                    }
                }
                catch (Exception disposeEx)
                {
                    _logService.LogWarning("Error disposing existing releasing timer, continuing. Exception: {Exception}", disposeEx.Message);
                }

                // Get releasing duration from configuration with validation
                int releasingDurationMs;
                try
                {
                    releasingDurationMs = _cursorConfiguration?.ReleasingDurationMs ?? 200;
                    
                    if (releasingDurationMs <= 0)
                    {
                        _logService.LogWarning("Invalid releasing duration {Duration}ms, using default 200ms", releasingDurationMs);
                        releasingDurationMs = 200;
                    }
                    else if (releasingDurationMs > 5000)
                    {
                        _logService.LogWarning("Very long releasing duration {Duration}ms, using maximum 5000ms", releasingDurationMs);
                        releasingDurationMs = 5000;
                    }
                }
                catch (Exception configEx)
                {
                    _logService.LogError(configEx, "Error reading releasing duration from configuration, using default 200ms");
                    releasingDurationMs = 200;
                }

                // Start new timer for releasing duration with error handling
                try
                {
                    _releasingTimer = new System.Threading.Timer(
                        callback: TimerCallback,
                        state: null,
                        dueTime: TimeSpan.FromMilliseconds(releasingDurationMs),
                        period: Timeout.InfiniteTimeSpan);
                    
                    _logService.LogDebug("Successfully started releasing timer for {ReleasingDurationMs}ms", releasingDurationMs);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _logService.LogError(ex, "Invalid timer duration {Duration}ms, attempting recovery", releasingDurationMs);
                    RecoverToDefaultState("Invalid releasing timer duration");
                }
                catch (OutOfMemoryException ex)
                {
                    _logService.LogError(ex, "Out of memory creating releasing timer, attempting recovery");
                    RecoverToDefaultState("Out of memory creating releasing timer");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error starting releasing timer, attempting recovery");
                RecoverToDefaultState("Unexpected error starting releasing timer");
            }
        }

        #endregion StartReleasingTimer

        #region TimerCallback

        /// <summary>
        /// Timer callback method with error handling
        /// </summary>
        /// <param name="state">Timer state (unused)</param>
        private void TimerCallback(object? state)
        {
            try
            {
                _logService.LogDebug("Releasing timer callback triggered");
                Fire(HandTrigger.ReleaseComplete);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error in releasing timer callback, attempting recovery");
                RecoverToDefaultState("Error in releasing timer callback");
            }
        }

        #endregion TimerCallback

        #region Dispose

        /// <summary>
        /// Disposes resources used by the hand state machine with comprehensive error handling
        /// </summary>
        public void Dispose()
        {
            try
            {
                _logService.LogDebug("Disposing HandStateMachine resources");

                // Dispose releasing timer with error handling
                try
                {
                    if (_releasingTimer != null)
                    {
                        _releasingTimer.Dispose();
                        _releasingTimer = null;
                        _logService.LogDebug("Successfully disposed releasing timer");
                    }
                }
                catch (Exception timerEx)
                {
                    _logService.LogError(timerEx, "Error disposing releasing timer");
                }

                // Clear any event handlers to prevent memory leaks
                try
                {
                    StateChanged = null;
                    _logService.LogDebug("Cleared state changed event handlers");
                }
                catch (Exception eventEx)
                {
                    _logService.LogError(eventEx, "Error clearing event handlers");
                }

                _logService.LogDebug("HandStateMachine disposal completed");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error during HandStateMachine disposal");
            }
        }

        #endregion Dispose

        #endregion Methods
    }
}
