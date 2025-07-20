using System;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Interface for enhanced exception handling service
    /// </summary>
    public interface IExceptionHandlingService
    {
        /// <summary>
        /// Handles unhandled exceptions with context capture
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Additional context information</param>
        void HandleException(Exception exception, string context = "");

        /// <summary>
        /// Captures current application state for error reporting
        /// </summary>
        /// <returns>Application context information</returns>
        object CaptureApplicationContext();

        /// <summary>
        /// Generates user-friendly error message from exception
        /// </summary>
        /// <param name="exception">The exception to process</param>
        /// <returns>User-friendly error message</returns>
        string GenerateUserFriendlyMessage(Exception exception);

        /// <summary>
        /// Attempts to recover from an error condition
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <returns>True if recovery was successful</returns>
        bool AttemptRecovery(Exception exception);

        /// <summary>
        /// Reports critical errors that require immediate attention
        /// </summary>
        /// <param name="exception">The critical exception</param>
        /// <param name="applicationState">Current application state</param>
        void ReportCriticalError(Exception exception, object applicationState);
    }
}