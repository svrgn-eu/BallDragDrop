using System;
using System.Diagnostics;
using System.IO;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Simple implementation of ILogService for integration purposes
    /// This will be replaced by Log4NetService in later tasks
    /// </summary>
    public class SimpleLogService : ILogService
    {
        #region Construction

        public SimpleLogService()
        {
            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BallDragDrop",
                "logs",
                $"app_{DateTime.Now:yyyyMMdd}.log");
                
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
            _correlationId = Guid.NewGuid().ToString("N")[..8];
        }

        #endregion Construction

        #region Fields

        private readonly string _logFilePath;
        private string _correlationId;

        #endregion Fields

        #region Methods

        #region Basic Logging Methods

        #region LogTrace
        /// <summary>
        /// Logs a trace message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogTrace(string message, params object[] args)
        {
            WriteLog(LogLevel.Trace, message, null, args);
        }
        #endregion LogTrace

        /// <summary>
        /// Logs a debug message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogDebug(string message, params object[] args)
        {
            WriteLog(LogLevel.Debug, message, null, args);
        }

        /// <summary>
        /// Logs an information message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogInformation(string message, params object[] args)
        {
            WriteLog(LogLevel.Information, message, null, args);
        }

        /// <summary>
        /// Logs a warning message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogWarning(string message, params object[] args)
        {
            WriteLog(LogLevel.Warning, message, null, args);
        }

        /// <summary>
        /// Logs an error message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogError(string message, params object[] args)
        {
            WriteLog(LogLevel.Error, message, null, args);
        }

        /// <summary>
        /// Logs an error message with an associated exception and optional formatting parameters
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogError(Exception exception, string message, params object[] args)
        {
            WriteLog(LogLevel.Error, message, exception, args);
        }

        /// <summary>
        /// Logs a critical message with optional formatting parameters
        /// </summary>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogCritical(string message, params object[] args)
        {
            WriteLog(LogLevel.Critical, message, null, args);
        }

        /// <summary>
        /// Logs a critical message with an associated exception and optional formatting parameters
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">The message template to log</param>
        /// <param name="args">Optional formatting arguments for the message</param>
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            WriteLog(LogLevel.Critical, message, exception, args);
        }

        #endregion Basic Logging Methods

        #region Structured Logging Methods

        /// <summary>
        /// Logs a structured message at the specified log level with property values
        /// </summary>
        /// <param name="level">The log level for this message</param>
        /// <param name="messageTemplate">The message template with placeholders</param>
        /// <param name="propertyValues">Values to substitute into the message template</param>
        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues)
        {
            WriteLog(level, messageTemplate, null, propertyValues);
        }

        /// <summary>
        /// Logs a structured message at the specified log level with an associated exception and property values
        /// </summary>
        /// <param name="level">The log level for this message</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="messageTemplate">The message template with placeholders</param>
        /// <param name="propertyValues">Values to substitute into the message template</param>
        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            WriteLog(level, messageTemplate, exception, propertyValues);
        }

        #endregion Structured Logging Methods

        #region Scope Management

        /// <summary>
        /// Creates a logging scope that tracks the duration of an operation
        /// </summary>
        /// <param name="scopeName">The name of the scope</param>
        /// <param name="parameters">Optional parameters associated with the scope</param>
        /// <returns>A disposable object that ends the scope when disposed</returns>
        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            return new LogScope(this, scopeName, parameters);
        }

        #endregion Scope Management

        #region Method and Performance Tracking

        /// <summary>
        /// Logs the entry into a method with optional parameters
        /// </summary>
        /// <param name="methodName">The name of the method being entered</param>
        /// <param name="parameters">Optional parameters passed to the method</param>
        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            var paramStr = parameters?.Length > 0 ? $" with parameters: {string.Join(", ", parameters)}" : "";
            LogDebug($"Entering method: {methodName}{paramStr}");
        }

        /// <summary>
        /// Logs the exit from a method with optional return value and duration
        /// </summary>
        /// <param name="methodName">The name of the method being exited</param>
        /// <param name="returnValue">Optional return value from the method</param>
        /// <param name="duration">Optional duration the method took to execute</param>
        public void LogMethodExit(string methodName, object returnValue = null, TimeSpan? duration = null)
        {
            var returnStr = returnValue != null ? $" returning: {returnValue}" : "";
            var durationStr = duration.HasValue ? $" (took {duration.Value.TotalMilliseconds:F2}ms)" : "";
            LogDebug($"Exiting method: {methodName}{returnStr}{durationStr}");
        }

        /// <summary>
        /// Logs performance information for an operation
        /// </summary>
        /// <param name="operationName">The name of the operation being measured</param>
        /// <param name="duration">The duration the operation took to complete</param>
        /// <param name="additionalData">Optional additional data to include in the performance log</param>
        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData)
        {
            var dataStr = additionalData?.Length > 0 ? $" - {string.Join(", ", additionalData)}" : "";
            LogInformation($"Performance: {operationName} took {duration.TotalMilliseconds:F2}ms{dataStr}");
        }

        #endregion Method and Performance Tracking

        #region Correlation ID Management

        /// <summary>
        /// Sets the correlation ID for tracking related log entries
        /// </summary>
        /// <param name="correlationId">The correlation ID to set, or null to generate a new one</param>
        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// Gets the current correlation ID used for tracking related log entries
        /// </summary>
        /// <returns>The current correlation ID</returns>
        public string GetCorrelationId()
        {
            return _correlationId;
        }

        #endregion Correlation ID Management

        #region Private Methods

        private void WriteLog(LogLevel level, string message, Exception exception, params object[] args)
        {
            try
            {
                var formattedMessage = args?.Length > 0 ? string.Format(message, args) : message;
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToString().ToUpper()}] [{_correlationId}] {formattedMessage}";
                
                if (exception != null)
                {
                    logMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                    logMessage += $"\nStackTrace: {exception.StackTrace}";
                    
                    if (exception.InnerException != null)
                    {
                        logMessage += $"\nInner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
                    }
                }

                // Write to file
                File.AppendAllText(_logFilePath, logMessage + "\n");
                
                // Also write to debug output
                Debug.WriteLine(logMessage);
            }
            catch
            {
                // If logging fails, at least try debug output
                Debug.WriteLine($"Failed to log: {message}");
            }
        }

        #endregion Private Methods

        #endregion Methods

        #region Nested Classes

        private class LogScope : IDisposable
        {
            private readonly SimpleLogService _logger;
            private readonly string _scopeName;
            private readonly DateTime _startTime;

            public LogScope(SimpleLogService logger, string scopeName, object[] parameters)
            {
                _logger = logger;
                _scopeName = scopeName;
                _startTime = DateTime.Now;
                
                var paramStr = parameters?.Length > 0 ? $" with parameters: {string.Join(", ", parameters)}" : "";
                _logger.LogDebug($"Beginning scope: {scopeName}{paramStr}");
            }

            #region Dispose
            /// <summary>
            /// Disposes the instance
            /// </summary>
            public void Dispose()
            {
                var duration = DateTime.Now - _startTime;
                _logger.LogDebug($"Ending scope: {_scopeName} (duration: {duration.TotalMilliseconds:F2}ms)");
            }
            #endregion Dispose
        }

        #endregion Nested Classes
    }
}
