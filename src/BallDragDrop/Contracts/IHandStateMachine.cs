using System;
using BallDragDrop.Models;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// State machine for managing hand/interaction states that drive cursor changes
    /// </summary>
    public interface IHandStateMachine
    {
        #region Properties

        /// <summary>
        /// Current hand state
        /// </summary>
        HandState CurrentState { get; }

        #endregion Properties

        #region Events

        /// <summary>
        /// Event fired when hand state changes
        /// </summary>
        event EventHandler<HandStateChangedEventArgs> StateChanged;

        #endregion Events

        #region Methods

        /// <summary>
        /// Triggers a hand state transition
        /// </summary>
        /// <param name="trigger">The trigger causing the transition</param>
        void Fire(HandTrigger trigger);

        /// <summary>
        /// Checks if a trigger can be fired from current state
        /// </summary>
        /// <param name="trigger">The trigger to check</param>
        /// <returns>True if trigger is valid</returns>
        bool CanFire(HandTrigger trigger);

        /// <summary>
        /// Resets hand state machine to default state
        /// </summary>
        void Reset();

        /// <summary>
        /// Handles mouse entering ball area
        /// </summary>
        void OnMouseOverBall();

        /// <summary>
        /// Handles mouse leaving ball area
        /// </summary>
        void OnMouseLeaveBall();

        /// <summary>
        /// Handles drag operation start
        /// </summary>
        void OnDragStart();

        /// <summary>
        /// Handles drag operation stop
        /// </summary>
        void OnDragStop();

        #endregion Methods
    }
}