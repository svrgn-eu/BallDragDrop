using BallDragDrop.Models;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Defines the contract for observers that need to be notified of ball state changes.
    /// This interface implements the Observer pattern, allowing components to react
    /// to state transitions in the ball state machine.
    /// </summary>
    /// <remarks>
    /// Components that implement this interface can register with the ball state machine
    /// to receive notifications whenever the ball transitions between states.
    /// This enables loose coupling between the state machine and components that need
    /// to react to state changes, such as the UI, physics engine, or other services.
    /// 
    /// The observer pattern is particularly useful in this context because:
    /// - Multiple components can react to the same state change
    /// - Components can be added or removed without modifying the state machine
    /// - State-dependent behavior is centralized in the observing components
    /// - The state machine remains focused on state management logic
    /// </remarks>
    public interface IBallStateObserver
    {
        /// <summary>
        /// Called when the ball state changes.
        /// This method is invoked by the state machine whenever a state transition occurs,
        /// providing the observer with information about the transition.
        /// </summary>
        /// <param name="previousState">The state the ball was in before the transition.</param>
        /// <param name="newState">The state the ball transitioned to.</param>
        /// <param name="trigger">The trigger that caused the state transition.</param>
        /// <remarks>
        /// Implementations of this method should:
        /// - Execute quickly to avoid blocking other observers
        /// - Handle exceptions gracefully to prevent disrupting the state machine
        /// - Update their internal state or UI based on the new ball state
        /// - Avoid triggering additional state transitions from within this method
        /// 
        /// Common use cases for state change handling include:
        /// - Updating visual representations of the ball
        /// - Enabling or disabling physics calculations
        /// - Updating status displays or logging
        /// - Triggering animations or sound effects
        /// </remarks>
        void OnStateChanged(BallState previousState, BallState newState, BallTrigger trigger);
    }
}