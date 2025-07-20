using System;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Basic implementation of method logging interceptor
    /// This is a placeholder implementation that will be enhanced in task 4
    /// </summary>
    public class MethodLoggingInterceptor : IMethodLoggingInterceptor
    {
        private readonly ILogService _logService;

        public MethodLoggingInterceptor(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public void ConfigureMethodFilter(string methodName, bool shouldIntercept)
        {
            _logService.LogDebug("Method filter configured: {MethodName} = {ShouldIntercept}", methodName, shouldIntercept);
        }

        public void SetParameterLogging(bool enabled)
        {
            _logService.LogDebug("Parameter logging set to: {Enabled}", enabled);
        }
    }
}