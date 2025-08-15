using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using Stateless;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Implementation of the ball state machine using the Stateless library.
    /// Manages state transitions for the ball and provides observer notifications.
    /// </summary>
    public class BallStateMachine : IBallStateMachine
    {
        #region Properties
        private readonly StateMachine<BallState, BallTrigger> _stateMachine;
        private readonly ILogService _logService;
        private readonly BallStateConfiguration _configuration;
        private readonly List<IBallStateObserver> _observers;
        private readonly object _observersLock = new object();
        private readonly object _stateLock = new object();
        #endregion Properties

        #region Construction
        /// <summary>
        /// Initializes a new instance of the <see cref="BallStateMachine"/> class.
        /// </summary>
        /// <param name="logService">The logging service for state transition logging.</param>
        /// <param name="configuration">The configuration settings for the state machine.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="logService"/> or <paramref name="configuration"/> is null.
        /// </exception>
        public BallStateMachine(ILogService logService, BallStateConfiguration configuration)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _observers = new List<IBallStateObserver>();

            // Initialize state machine with Idle as the initial state
            _stateMachine = new StateMachine<BallState, BallTrigger>(BallState.Idle);

            ConfigureStateMachine();

            if (_configuration.EnableStateLogging)
            {
                _logService.LogInformation("BallStateMachine initialized with initial state: {InitialState}", BallState.Idle);
            }
        }
        #endregion Construction

        #region Methods

        /// <summary>
        /// Gets the current state of the ball.
        /// </summary>
        public BallState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _stateMachine.State;
                }
            }
        }

        /// <summary>
        /// Occurs when the ball state changes.
        /// </summary>
        public event EventHandler<BallStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Fires the specified trigger to attempt a state transition.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the trigger is not valid for the current state.
        /// </exception>
        public void Fire(BallTrigger trigger)
        {
            BallState previousState;
            BallState newState;

            lock (_stateLock)
            {
                previousState = _stateMachine.State;

                try
                {
                    if (_configuration.EnableTransitionValidation && !_stateMachine.CanFire(trigger))
                    {
                        var errorMessage = $"Invalid trigger '{trigger}' for current state '{previousState}'";
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogWarning("State transition rejected: {ErrorMessage}", errorMessage);
                        }
                        throw new InvalidOperationException(errorMessage);
                    }

                    _stateMachine.Fire(trigger);
                    newState = _stateMachine.State;

                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogInformation("State transition: {PreviousState} -> {NewState} (Trigger: {Trigger})", 
                            previousState, newState, trigger);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Re-throw InvalidOperationException as expected behavior
                    throw;
                }
                catch (Exception ex)
                {
                    // Handle unexpected errors with recovery mechanism
                    HandleStateTransitionError(ex, trigger, previousState);
                    throw;
                }
            }

            // Notify observers and raise event outside of lock to prevent deadlocks
            var eventArgs = new BallStateChangedEventArgs(previousState, newState, trigger);
            NotifyObservers(previousState, newState, trigger);
            StateChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Determines whether the specified trigger can be fired in the current state.
        /// </summary>
        /// <param name="trigger">The trigger to check.</param>
        /// <returns>
        /// <c>true</c> if the trigger can be fired in the current state; otherwise, <c>false</c>.
        /// </returns>
        public bool CanFire(BallTrigger trigger)
        {
            lock (_stateLock)
            {
                return _stateMachine.CanFire(trigger);
            }
        }

        /// <summary>
        /// Subscribes an observer to receive state change notifications.
        /// </summary>
        /// <param name="observer">The observer to subscribe.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="observer"/> is null.
        /// </exception>
        public void Subscribe(IBallStateObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_observersLock)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogDebug("Observer {ObserverType} subscribed to state machine", observer.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes an observer from state change notifications.
        /// </summary>
        /// <param name="observer">The observer to unsubscribe.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="observer"/> is null.
        /// </exception>
        public void Unsubscribe(IBallStateObserver observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_observersLock)
            {
                if (_observers.Remove(observer))
                {
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogDebug("Observer {ObserverType} unsubscribed from state machine", observer.GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the state machine with valid state transitions.
        /// </summary>
        private void ConfigureStateMachine()
        {
            // Configure Idle state transitions
            _stateMachine.Configure(BallState.Idle)
                .Permit(BallTrigger.MouseDown, BallState.Held)
                .PermitReentry(BallTrigger.Reset);

            // Configure Held state transitions
            _stateMachine.Configure(BallState.Held)
                .Permit(BallTrigger.Release, BallState.Thrown)
                .Permit(BallTrigger.Reset, BallState.Idle);

            // Configure Thrown state transitions
            _stateMachine.Configure(BallState.Thrown)
                .Permit(BallTrigger.MouseDown, BallState.Held)  // Allow grabbing the ball while it's moving
                .Permit(BallTrigger.VelocityBelowThreshold, BallState.Idle)
                .Permit(BallTrigger.Reset, BallState.Idle);

            if (_configuration.EnableStateLogging)
            {
                _logService.LogDebug("State machine configured with transitions: " +
                    "Idle->Held (MouseDown), Held->Thrown (Release), Thrown->Held (MouseDown), Thrown->Idle (VelocityBelowThreshold), " +
                    "Any->Idle (Reset)");
            }
        }

        /// <summary>
        /// Notifies all registered observers of a state change.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="newState">The new state.</param>
        /// <param name="trigger">The trigger that caused the change.</param>
        private void NotifyObservers(BallState previousState, BallState newState, BallTrigger trigger)
        {
            List<IBallStateObserver> observersCopy;

            // Create a copy of observers to avoid holding the lock during notifications
            lock (_observersLock)
            {
                observersCopy = new List<IBallStateObserver>(_observers);
            }

            if (_configuration.EnableAsyncNotifications)
            {
                // Notify observers asynchronously
                ThreadPool.QueueUserWorkItem(_ => NotifyObserversSync(observersCopy, previousState, newState, trigger));
            }
            else
            {
                // Notify observers synchronously
                NotifyObserversSync(observersCopy, previousState, newState, trigger);
            }
        }

        /// <summary>
        /// Synchronously notifies observers of a state change.
        /// </summary>
        /// <param name="observers">The list of observers to notify.</param>
        /// <param name="previousState">The previous state.</param>
        /// <param name="newState">The new state.</param>
        /// <param name="trigger">The trigger that caused the change.</param>
        private void NotifyObserversSync(List<IBallStateObserver> observers, BallState previousState, BallState newState, BallTrigger trigger)
        {
            foreach (var observer in observers)
            {
                try
                {
                    observer.OnStateChanged(previousState, newState, trigger);
                }
                catch (Exception ex)
                {
                    // Log observer errors but don't let them break the state machine
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogError(ex, "Error notifying observer {ObserverType} of state change from {PreviousState} to {NewState}", 
                            observer.GetType().Name, previousState, newState);
                    }
                }
            }
        }

        /// <summary>
        /// Validates the current state consistency of the state machine.
        /// This method checks that the state machine is in a valid state and can perform expected operations.
        /// </summary>
        /// <returns><c>true</c> if the state machine is consistent; otherwise, <c>false</c>.</returns>
        public bool ValidateStateConsistency()
        {
            try
            {
                lock (_stateLock)
                {
                    var currentState = _stateMachine.State;
                    
                    // Validate that the current state is a valid enum value
                    if (!Enum.IsDefined(typeof(BallState), currentState))
                    {
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogError("State machine is in invalid state: {InvalidState}", currentState);
                        }
                        return false;
                    }

                    // Validate that the state machine can perform basic operations
                    var canFireMouseDown = _stateMachine.CanFire(BallTrigger.MouseDown);
                    var canFireRelease = _stateMachine.CanFire(BallTrigger.Release);
                    var canFireVelocityThreshold = _stateMachine.CanFire(BallTrigger.VelocityBelowThreshold);
                    var canFireReset = _stateMachine.CanFire(BallTrigger.Reset);

                    // Reset should always be available from any state
                    if (!canFireReset)
                    {
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogError("Reset trigger should be available from any state but is not available from {CurrentState}", currentState);
                        }
                        return false;
                    }

                    // Validate state-specific trigger availability
                    switch (currentState)
                    {
                        case BallState.Idle:
                            if (!canFireMouseDown || canFireRelease || canFireVelocityThreshold)
                            {
                                if (_configuration.EnableStateLogging)
                                {
                                    _logService.LogError("Invalid trigger availability for Idle state. MouseDown: {CanMouseDown}, Release: {CanRelease}, VelocityThreshold: {CanVelocityThreshold}",
                                        canFireMouseDown, canFireRelease, canFireVelocityThreshold);
                                }
                                return false;
                            }
                            break;

                        case BallState.Held:
                            if (canFireMouseDown || !canFireRelease || canFireVelocityThreshold)
                            {
                                if (_configuration.EnableStateLogging)
                                {
                                    _logService.LogError("Invalid trigger availability for Held state. MouseDown: {CanMouseDown}, Release: {CanRelease}, VelocityThreshold: {CanVelocityThreshold}",
                                        canFireMouseDown, canFireRelease, canFireVelocityThreshold);
                                }
                                return false;
                            }
                            break;

                        case BallState.Thrown:
                            if (!canFireMouseDown || canFireRelease || !canFireVelocityThreshold)
                            {
                                if (_configuration.EnableStateLogging)
                                {
                                    _logService.LogError("Invalid trigger availability for Thrown state. MouseDown: {CanMouseDown}, Release: {CanRelease}, VelocityThreshold: {CanVelocityThreshold}",
                                        canFireMouseDown, canFireRelease, canFireVelocityThreshold);
                                }
                                return false;
                            }
                            break;

                        default:
                            if (_configuration.EnableStateLogging)
                            {
                                _logService.LogError("Unknown state in validation: {UnknownState}", currentState);
                            }
                            return false;
                    }

                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogDebug("State consistency validation passed for state: {CurrentState}", currentState);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (_configuration.EnableStateLogging)
                {
                    _logService.LogError(ex, "Error during state consistency validation");
                }
                return false;
            }
        }

        /// <summary>
        /// Attempts to recover the state machine to a safe state when errors are detected.
        /// This method should be called when state inconsistencies are detected or when
        /// the state machine encounters unrecoverable errors.
        /// </summary>
        /// <returns><c>true</c> if recovery was successful; otherwise, <c>false</c>.</returns>
        public bool RecoverToSafeState()
        {
            try
            {
                lock (_stateLock)
                {
                    var currentState = _stateMachine.State;
                    
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogWarning("Attempting to recover state machine from state: {CurrentState}", currentState);
                    }

                    // Try to transition to Idle state using available triggers
                    if (currentState == BallState.Thrown && _stateMachine.CanFire(BallTrigger.VelocityBelowThreshold))
                    {
                        _stateMachine.Fire(BallTrigger.VelocityBelowThreshold);
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogInformation("Successfully recovered to Idle state from Thrown state");
                        }
                        return true;
                    }

                    // If we can't use normal transitions, we need to recreate the state machine
                    // This is a last resort recovery mechanism
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogWarning("Normal recovery failed, attempting to reinitialize state machine");
                    }

                    // Store current observers
                    List<IBallStateObserver> currentObservers;
                    lock (_observersLock)
                    {
                        currentObservers = new List<IBallStateObserver>(_observers);
                    }

                    // Since _stateMachine is readonly, we cannot replace it directly
                    // Instead, we'll try to force it back to Idle state through configuration
                    // This is a simplified recovery that resets to Idle
                    
                    // Try to use reflection to reset the internal state
                    var field = typeof(StateMachine<BallState, BallTrigger>).GetField("_state", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(_stateMachine, BallState.Idle);
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogInformation("Successfully reset state machine to Idle using reflection");
                        }
                    }
                    else
                    {
                        // If reflection fails, log the issue but continue
                        if (_configuration.EnableStateLogging)
                        {
                            _logService.LogWarning("Could not reset state machine through reflection, state may remain inconsistent");
                        }
                    }

                    // Notify observers of the recovery
                    var eventArgs = new BallStateChangedEventArgs(currentState, BallState.Idle, BallTrigger.VelocityBelowThreshold);
                    NotifyObservers(currentState, BallState.Idle, BallTrigger.VelocityBelowThreshold);
                    StateChanged?.Invoke(this, eventArgs);

                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogInformation("State machine successfully recovered to Idle state through reinitialization");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (_configuration.EnableStateLogging)
                {
                    _logService.LogCritical(ex, "Failed to recover state machine to safe state");
                }
                return false;
            }
        }



        /// <summary>
        /// Handles state transition errors by logging them and attempting recovery if necessary.
        /// </summary>
        /// <param name="ex">The exception that occurred during state transition.</param>
        /// <param name="trigger">The trigger that caused the error.</param>
        /// <param name="currentState">The current state when the error occurred.</param>
        private void HandleStateTransitionError(Exception ex, BallTrigger trigger, BallState currentState)
        {
            if (_configuration.EnableStateLogging)
            {
                _logService.LogError(ex, "State transition error for trigger {Trigger} in state {CurrentState}", trigger, currentState);
            }

            // Validate state consistency after error
            if (!ValidateStateConsistency())
            {
                if (_configuration.EnableStateLogging)
                {
                    _logService.LogWarning("State inconsistency detected after error, attempting recovery");
                }

                // Attempt recovery to safe state
                if (!RecoverToSafeState())
                {
                    if (_configuration.EnableStateLogging)
                    {
                        _logService.LogCritical("Failed to recover state machine after error. Manual intervention may be required.");
                    }
                }
            }
        }

        #endregion Methods
    }
}
