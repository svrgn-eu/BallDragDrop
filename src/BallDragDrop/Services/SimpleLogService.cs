using System;
using System.Diagnostics;
using System.IO;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Simple implementation of ILogService for integration purposes
    /// This will be replaced by Log4NetService in later tasks
    /// </summary>
    public class SimpleLogService : ILogService
    {
        private readonly string _logFilePath;
        private string _correlationId;

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

        public void LogTrace(string message, params object[] args)
        {
            WriteLog(LogLevel.Trace, message, null, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            WriteLog(LogLevel.Debug, message, null, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            WriteLog(LogLevel.Information, message, null, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            WriteLog(LogLevel.Warning, message, null, args);
        }

        public void LogError(string message, params object[] args)
        {
            WriteLog(LogLevel.Error, message, null, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            WriteLog(LogLevel.Error, message, exception, args);
        }

        public void LogCritical(string message, params object[] args)
        {
            WriteLog(LogLevel.Critical, message, null, args);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            WriteLog(LogLevel.Critical, message, exception, args);
        }

        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues)
        {
            WriteLog(level, messageTemplate, null, propertyValues);
        }

        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            WriteLog(level, messageTemplate, exception, propertyValues);
        }

        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            return new LogScope(this, scopeName, parameters);
        }

        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            var paramStr = parameters?.Length > 0 ? $" with parameters: {string.Join(", ", parameters)}" : "";
            LogDebug($"Entering method: {methodName}{paramStr}");
        }

        public void LogMethodExit(string methodName, object returnValue = null, TimeSpan? duration = null)
        {
            var returnStr = returnValue != null ? $" returning: {returnValue}" : "";
            var durationStr = duration.HasValue ? $" (took {duration.Value.TotalMilliseconds:F2}ms)" : "";
            LogDebug($"Exiting method: {methodName}{returnStr}{durationStr}");
        }

        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData)
        {
            var dataStr = additionalData?.Length > 0 ? $" - {string.Join(", ", additionalData)}" : "";
            LogInformation($"Performance: {operationName} took {duration.TotalMilliseconds:F2}ms{dataStr}");
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? Guid.NewGuid().ToString("N")[..8];
        }

        public string GetCorrelationId()
        {
            return _correlationId;
        }

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

            public void Dispose()
            {
                var duration = DateTime.Now - _startTime;
                _logger.LogDebug($"Ending scope: {_scopeName} (duration: {duration.TotalMilliseconds:F2}ms)");
            }
        }
    }
}