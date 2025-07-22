using System;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Basic implementation of method logging interceptor
    /// This is a placeholder implementation that will be enhanced in task 4
    /// </summary>
    public class MethodLoggingInterceptor : IMethodLoggingInterceptor
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the MethodLoggingInterceptor class
        /// </summary>
        /// <param name="logService">The logging service to use</param>
        /// <exception cref="ArgumentNullException">Thrown when logService is null</exception>
        public MethodLoggingInterceptor(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// Logging service for recording method interception information
        /// </summary>
        private readonly ILogService _logService;

        #endregion Fields

        #region IMethodLoggingInterceptor Implementation

        /// <summary>
        /// Configures whether a specific method should be intercepted for logging
        /// </summary>
        /// <param name="methodName">The name of the method to configure</param>
        /// <param name="shouldIntercept">True to enable interception, false to disable</param>
        public void ConfigureMethodFilter(string methodName, bool shouldIntercept)
        {
            _logService.LogDebug("Method filter configured: {MethodName} = {ShouldIntercept}", methodName, shouldIntercept);
        }

        /// <summary>
        /// Enables or disables parameter logging for intercepted methods
        /// </summary>
        /// <param name="enabled">True to enable parameter logging, false to disable</param>
        public void SetParameterLogging(bool enabled)
        {
            _logService.LogDebug("Parameter logging set to: {Enabled}", enabled);
        }

        #endregion IMethodLoggingInterceptor Implementation
    }
}