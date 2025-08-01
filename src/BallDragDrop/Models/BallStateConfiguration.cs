using System;
using System.ComponentModel.DataAnnotations;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Configuration settings for the ball state machine behavior.
    /// This class provides configurable parameters that control how the state machine
    /// operates, including thresholds, timing, and feature toggles.
    /// </summary>
    public class BallStateConfiguration
    {
        private double _velocityThreshold = 50.0;
        private TimeSpan _stateTransitionDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the velocity threshold below which the ball transitions from Thrown to Idle state.
        /// When the ball's velocity magnitude drops below this value, it will automatically
        /// transition back to the Idle state.
        /// </summary>
        /// <value>The velocity threshold in pixels per second. Default is 50.0.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative or zero.
        /// </exception>
        [Range(0.1, double.MaxValue, ErrorMessage = "Velocity threshold must be greater than zero.")]
        public double VelocityThreshold
        {
            get => _velocityThreshold;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), 
                        "Velocity threshold must be greater than zero.");
                }
                _velocityThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum delay between state transitions.
        /// This prevents rapid state transitions that could cause instability
        /// or poor user experience.
        /// </summary>
        /// <value>The state transition delay. Default is 100 milliseconds.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative.
        /// </exception>
        public TimeSpan StateTransitionDelay
        {
            get => _stateTransitionDelay;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), 
                        "State transition delay cannot be negative.");
                }
                _stateTransitionDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether state transitions should be logged.
        /// When enabled, all state transitions will be logged for debugging and monitoring purposes.
        /// </summary>
        /// <value><c>true</c> if state logging is enabled; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool EnableStateLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether visual feedback should be provided for state changes.
        /// When enabled, the ball will display visual indicators that reflect its current state.
        /// </summary>
        /// <value><c>true</c> if visual feedback is enabled; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool EnableVisualFeedback { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether state transition validation should be performed.
        /// When enabled, the state machine will validate transitions and reject invalid ones.
        /// Disabling this can improve performance but may allow invalid state transitions.
        /// </summary>
        /// <value><c>true</c> if transition validation is enabled; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool EnableTransitionValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether observer notifications should be sent asynchronously.
        /// When enabled, state change notifications to observers will be sent on a background thread
        /// to prevent blocking the state machine operations.
        /// </summary>
        /// <value><c>true</c> if async notifications are enabled; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        public bool EnableAsyncNotifications { get; set; } = false;

        /// <summary>
        /// Validates the current configuration settings.
        /// This method checks that all configuration values are within acceptable ranges
        /// and that the configuration is internally consistent.
        /// </summary>
        /// <returns><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid()
        {
            return VelocityThreshold > 0 && 
                   StateTransitionDelay >= TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a copy of the current configuration with default values restored.
        /// This method is useful for resetting configuration to known good defaults.
        /// </summary>
        /// <returns>A new <see cref="BallStateConfiguration"/> instance with default values.</returns>
        public static BallStateConfiguration CreateDefault()
        {
            return new BallStateConfiguration
            {
                VelocityThreshold = 50.0,
                StateTransitionDelay = TimeSpan.FromMilliseconds(100),
                EnableStateLogging = true,
                EnableVisualFeedback = true,
                EnableTransitionValidation = true,
                EnableAsyncNotifications = false
            };
        }

        /// <summary>
        /// Returns a string representation of the configuration for debugging purposes.
        /// </summary>
        /// <returns>A string containing the current configuration values.</returns>
        public override string ToString()
        {
            return $"BallStateConfiguration {{ " +
                   $"VelocityThreshold: {VelocityThreshold}, " +
                   $"StateTransitionDelay: {StateTransitionDelay.TotalMilliseconds}ms, " +
                   $"EnableStateLogging: {EnableStateLogging}, " +
                   $"EnableVisualFeedback: {EnableVisualFeedback}, " +
                   $"EnableTransitionValidation: {EnableTransitionValidation}, " +
                   $"EnableAsyncNotifications: {EnableAsyncNotifications} }}";
        }
    }
}