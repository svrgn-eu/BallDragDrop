using System;
using BallDragDrop.Models;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Defines the contract for a ball state machine that manages state transitions
    /// and provides notifications to observers about state changes.
    /// </summary>
    public interface IBallStateMachine
    {
        /// <summary>
        /// Gets the current state of the ball.
        /// </summary>
        /// <value>The current ball state.</value>
        BallState CurrentState { get; }

        /// <summary>
        /// Occurs when the ball state changes.
        /// This event is raised whenever a state transition occurs, providing
        /// information about the previous state, new state, and trigger.
        /// </summary>
        event EventHandler<BallStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Fires the specified trigger to attempt a state transition.
        /// If the trigger is valid for the current state, the state machine
        /// will transition to the appropriate new state and notify observers.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the trigger is not valid for the current state.
        /// </exception>
        void Fire(BallTrigger trigger);

        /// <summary>
        /// Determines whether the specified trigger can be fired in the current state.
        /// This method allows checking if a state transition is valid before attempting it.
        /// </summary>
        /// <param name="trigger">The trigger to check.</param>
        /// <returns>
        /// <c>true</c> if the trigger can be fired in the current state; otherwise, <c>false</c>.
        /// </returns>
        bool CanFire(BallTrigger trigger);

        /// <summary>
        /// Subscribes an observer to receive state change notifications.
        /// The observer will be notified whenever the ball state changes.
        /// </summary>
        /// <param name="observer">The observer to subscribe.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="observer"/> is null.
        /// </exception>
        void Subscribe(IBallStateObserver observer);

        /// <summary>
        /// Unsubscribes an observer from state change notifications.
        /// The observer will no longer receive state change notifications.
        /// </summary>
        /// <param name="observer">The observer to unsubscribe.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="observer"/> is null.
        /// </exception>
        void Unsubscribe(IBallStateObserver observer);
    }
}