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
        /// Triggers a hand state transition
        /// </summary>
        /// <param name="trigger">The trigger causing the transition</param>
        public void Fire(HandTrigger trigger)
        {
            lock (_lock)
            {
                try
                {
                    var previousState = _stateMachine.State;
                    
                    if (_stateMachine.CanFire(trigger))
                    {
                        _logService.LogDebug("Firing hand state trigger: {Trigger} from state {PreviousState}", 
                            trigger, previousState);
                        
                        _stateMachine.Fire(trigger);
                        
                        _logService.LogDebug("Hand state transition completed: {PreviousState} -> {NewState} via {Trigger}", 
                            previousState, _stateMachine.State, trigger);
                    }
                    else
                    {
                        _logService.LogWarning("Invalid hand state trigger {Trigger} for current state {CurrentState}", 
                            trigger, previousState);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Error firing hand state trigger {Trigger} from state {CurrentState}", 
                        trigger, _stateMachine.State);
                    
                    // Attempt recovery by resetting to default state
                    try
                    {
                        Reset();
                    }
                    catch (Exception resetEx)
                    {
                        _logService.LogError(resetEx, "Failed to reset hand state machine after error");
                    }
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
        /// Resets hand state machine to default state
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                try
                {
                    var previousState = _stateMachine.State;
                    
                    // Cancel any active releasing timer
                    _releasingTimer?.Dispose();
                    _releasingTimer = null;
                    
                    // Force transition to default state
                    if (_stateMachine.State != HandState.Default)
                    {
                        if (_stateMachine.CanFire(HandTrigger.Reset))
                        {
                            _stateMachine.Fire(HandTrigger.Reset);
                        }
                        else
                        {
                            // Force reset by recreating state machine
                            var newStateMachine = new StateMachine<HandState, HandTrigger>(HandState.Default);
                            ConfigureStateMachine();
                            OnHandStateChanged(HandState.Default);
                        }
                    }
                    
                    _logService.LogDebug("Hand state machine reset from {PreviousState} to {CurrentState}", 
                        previousState, _stateMachine.State);
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Error resetting hand state machine");
                }
            }
        }

        #endregion Reset

        #region OnStateChanged (IBallStateObserver)

        /// <summary>
        /// Called when the ball state changes
        /// </summary>
        /// <param name="previousState">Previous ball state</param>
        /// <param name="newState">New ball state</param>
        /// <param name="trigger">Trigger that caused the ball state change</param>
        public void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger)
        {
            try
            {
                _logService.LogDebug("Ball state changed: {PreviousState} -> {NewState} via {Trigger}, evaluating hand state", 
                    previousState, newState, trigger);

                // Map ball state changes to hand state triggers
                switch (newState)
                {
                    case BallState.Held when trigger == BallTrigger.MouseDown:
                        Fire(HandTrigger.StartGrabbing);
                        break;
                        
                    case BallState.Thrown when trigger == BallTrigger.Release:
                        Fire(HandTrigger.StopGrabbing);
                        break;
                        
                    case BallState.Idle when trigger == BallTrigger.VelocityBelowThreshold:
                        // Ball has stopped moving, hand state will transition based on mouse position
                        break;
                        
                    case BallState.Idle when trigger == BallTrigger.Reset:
                        Reset();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling ball state change in hand state machine");
            }
        }

        #endregion OnStateChanged (IBallStateObserver)

        #region Mouse Event Handling

        /// <summary>
        /// Handles mouse entering ball area
        /// </summary>
        public void OnMouseOverBall()
        {
            try
            {
                _logService.LogDebug("Mouse over ball detected, attempting to fire MouseOverBall trigger");
                Fire(HandTrigger.MouseOverBall);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling mouse over ball event");
            }
        }

        /// <summary>
        /// Handles mouse leaving ball area
        /// </summary>
        public void OnMouseLeaveBall()
        {
            try
            {
                _logService.LogDebug("Mouse leave ball detected, attempting to fire MouseLeaveBall trigger");
                Fire(HandTrigger.MouseLeaveBall);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling mouse leave ball event");
            }
        }

        /// <summary>
        /// Handles drag operation start
        /// </summary>
        public void OnDragStart()
        {
            try
            {
                _logService.LogDebug("Drag start detected, attempting to fire StartGrabbing trigger");
                Fire(HandTrigger.StartGrabbing);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling drag start event");
            }
        }

        /// <summary>
        /// Handles drag operation stop
        /// </summary>
        public void OnDragStop()
        {
            try
            {
                _logService.LogDebug("Drag stop detected, attempting to fire StopGrabbing trigger");
                Fire(HandTrigger.StopGrabbing);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling drag stop event");
            }
        }

        #endregion Mouse Event Handling

        #region OnHandStateChanged

        /// <summary>
        /// Called when the hand state changes
        /// </summary>
        /// <param name="newState">New hand state</param>
        private void OnHandStateChanged(HandState newState)
        {
            try
            {
                var previousState = _stateMachine.State;
                
                // Update cursor based on new hand state
                _cursorService.SetCursorForHandState(newState);
                
                // Raise state changed event - we'll use Reset as default trigger since we don't track the specific trigger here
                var eventArgs = new HandStateChangedEventArgs(previousState, newState, HandTrigger.Reset);
                StateChanged?.Invoke(this, eventArgs);
                
                _logService.LogDebug("Hand state changed from {PreviousState} to {NewState}", previousState, newState);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error handling hand state change to {NewState}", newState);
            }
        }

        #endregion OnHandStateChanged

        #region StartReleasingTimer

        /// <summary>
        /// Starts the timer for the releasing state duration
        /// </summary>
        private void StartReleasingTimer()
        {
            try
            {
                // Cancel any existing timer
                _releasingTimer?.Dispose();
                
                // Get releasing duration from configuration
                var releasingDurationMs = _cursorConfiguration.ReleasingDurationMs;
                
                // Start new timer for releasing duration
                _releasingTimer = new System.Threading.Timer(
                    callback: _ => Fire(HandTrigger.ReleaseComplete),
                    state: null,
                    dueTime: TimeSpan.FromMilliseconds(releasingDurationMs),
                    period: Timeout.InfiniteTimeSpan);
                
                _logService.LogDebug("Started releasing timer for {ReleasingDurationMs}ms", releasingDurationMs);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error starting releasing timer");
            }
        }

        #endregion StartReleasingTimer

        #region Dispose

        /// <summary>
        /// Disposes resources used by the hand state machine
        /// </summary>
        public void Dispose()
        {
            _releasingTimer?.Dispose();
            _releasingTimer = null;
        }

        #endregion Dispose
    }
}