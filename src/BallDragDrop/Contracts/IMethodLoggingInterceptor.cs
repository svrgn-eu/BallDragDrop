namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Interface for method logging interception
    /// This will be implemented in task 4
    /// </summary>
    public interface IMethodLoggingInterceptor
    {
        /// <summary>
        /// Configures method filtering for interception
        /// </summary>
        /// <param name="methodName">Method name to filter</param>
        /// <param name="shouldIntercept">Whether to intercept this method</param>
        void ConfigureMethodFilter(string methodName, bool shouldIntercept);

        /// <summary>
        /// Enables or disables parameter logging
        /// </summary>
        /// <param name="enabled">Whether parameter logging is enabled</param>
        void SetParameterLogging(bool enabled);
    }
}