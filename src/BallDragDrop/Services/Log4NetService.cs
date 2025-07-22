using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using BallDragDrop.Contracts;
using BallDragDrop.Services.Performance;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Enhanced Log4NET implementation of ILogService with performance optimizations
    /// </summary>
    public class Log4NetService : ILogService, IDisposable
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the Log4NetService class
        /// </summary>
        /// <param name="performanceMonitor">Optional performance monitor instance</param>
        public Log4NetService(IPerformanceMonitor? performanceMonitor = null)
        {
            _logger = LogManager.GetLogger(typeof(Log4NetService));
            _performanceMonitor = performanceMonitor ?? (IPerformanceMonitor)new LoggingPerformanceMonitor();
            _logEntryPool = new LogEntryPool();
            _asyncProcessor = new AsyncLogProcessor(_logger, _performanceMonitor);
        }

        #endregion Construction

        #region Fields

        /// <summary>
        /// Log4NET logger instance
        /// </summary>
        private readonly ILog _logger;
        
        /// <summary>
        /// Performance monitor for tracking logging operations
        /// </summary>
        private readonly IPerformanceMonitor _performanceMonitor;
        
        /// <summary>
        /// Object pool for log entries to reduce GC pressure
        /// </summary>
        private readonly LogEntryPool _logEntryPool;
        
        /// <summary>
        /// Async processor for batched log processing
        /// </summary>
        private readonly AsyncLogProcessor _asyncProcessor;
        
        /// <summary>
        /// Current correlation ID for tracking related log entries
        /// </summary>
        private string _correlationId = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Flag indicating if the service has been disposed
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region ILogService Implementation

        /// <summary>
        /// Logs a trace message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogTrace(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Trace, message, null, args);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogDebug(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Debug, message, null, args);
        }

        /// <summary>
        /// Logs an information message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogInformation(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Information, message, null, args);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogWarning(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Warning, message, null, args);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogError(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Error, message, null, args);
        }

        /// <summary>
        /// Logs an error message with an associated exception
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogError(Exception exception, string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Error, message, exception, args);
        }

        /// <summary>
        /// Logs a critical message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogCritical(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Critical, message, null, args);
        }

        /// <summary>
        /// Logs a critical message with an associated exception
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">The message to log</param>
        /// <param name="args">Arguments for string formatting</param>
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Critical, message, exception, args);
        }

        /// <summary>
        /// Logs a structured message with property values
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="messageTemplate">The message template</param>
        /// <param name="propertyValues">Property values for the template</param>
        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues)
        {
            if (_disposed) return;
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var message = string.Format(messageTemplate, propertyValues);
                var properties = CreatePropertiesDictionary(propertyValues);
                
                _asyncProcessor.QueueLogItem(level, message, null, _correlationId, properties);
                
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"Structured_{level}", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"Structured_{level}_Error", stopwatch.Elapsed);
                
                // Fallback to basic logging
                LogWithPerformanceTracking(level, $"LogStructured failed: {ex.Message}. Original template: {messageTemplate}", ex);
            }
        }

        /// <summary>
        /// Logs a structured message with an exception and property values
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="messageTemplate">The message template</param>
        /// <param name="propertyValues">Property values for the template</param>
        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            if (_disposed) return;
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var message = string.Format(messageTemplate, propertyValues);
                var properties = CreatePropertiesDictionary(propertyValues);
                
                _asyncProcessor.QueueLogItem(level, message, exception, _correlationId, properties);
                
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"StructuredException_{level}", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"StructuredException_{level}_Error", stopwatch.Elapsed);
                
                // Fallback to basic logging
                LogWithPerformanceTracking(level, $"LogStructured with exception failed: {ex.Message}. Original template: {messageTemplate}", exception);
            }
        }

        /// <summary>
        /// Begins a logging scope
        /// </summary>
        /// <param name="scopeName">Name of the scope</param>
        /// <param name="parameters">Parameters for the scope</param>
        /// <returns>A disposable scope object</returns>
        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            if (_disposed) return new DisposableScope();
            
            LogDebug("Entering scope: {ScopeName} with parameters: {Parameters}", scopeName, string.Join(", ", parameters));
            return new LogScope(this, scopeName);
        }

        /// <summary>
        /// Logs method entry
        /// </summary>
        /// <param name="methodName">Name of the method</param>
        /// <param name="parameters">Method parameters</param>
        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            if (_disposed) return;
            LogDebug("Entering method: {MethodName} with parameters: {Parameters}", methodName, string.Join(", ", parameters));
        }

        /// <summary>
        /// Logs method exit
        /// </summary>
        /// <param name="methodName">Name of the method</param>
        /// <param name="returnValue">Return value of the method</param>
        /// <param name="duration">Duration of method execution</param>
        public void LogMethodExit(string methodName, object returnValue = null, TimeSpan? duration = null)
        {
            if (_disposed) return;
            
            if (duration.HasValue)
            {
                LogDebug("Exiting method: {MethodName} with return value: {ReturnValue} (Duration: {Duration}ms)", 
                    methodName, returnValue, duration.Value.TotalMilliseconds);
                
                // Record method performance
                _performanceMonitor.RecordLoggingOperation($"Method_{methodName}", duration.Value);
            }
            else
            {
                LogDebug("Exiting method: {MethodName} with return value: {ReturnValue}", methodName, returnValue);
            }
        }

        /// <summary>
        /// Logs performance information
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="additionalData">Additional data to log</param>
        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData)
        {
            if (_disposed) return;
            
            LogInformation("Performance: {OperationName} completed in {Duration}ms. Additional data: {AdditionalData}", 
                operationName, duration.TotalMilliseconds, string.Join(", ", additionalData));
            
            // Record operation performance
            _performanceMonitor.RecordLoggingOperation($"Operation_{operationName}", duration);
        }

        /// <summary>
        /// Sets the correlation ID for tracking related log entries
        /// </summary>
        /// <param name="correlationId">The correlation ID to set</param>
        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets the current correlation ID
        /// </summary>
        /// <returns>The current correlation ID</returns>
        public string GetCorrelationId()
        {
            return _correlationId;
        }

        #endregion ILogService Implementation

        #region Performance Methods

        /// <summary>
        /// Gets current performance statistics
        /// </summary>
        /// <returns>Current performance statistics</returns>
        public PerformanceStatistics GetPerformanceStatistics()
        {
            return _performanceMonitor.GetStatistics();
        }

        /// <summary>
        /// Resets performance counters
        /// </summary>
        public void ResetPerformanceCounters()
        {
            _performanceMonitor.Reset();
        }

        #endregion Performance Methods

        #region Private Methods

        /// <summary>
        /// Logs a message with performance tracking
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">Optional exception to log</param>
        /// <param name="args">Arguments for string formatting</param>
        private void LogWithPerformanceTracking(LogLevel level, string message, Exception? exception, params object[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                _asyncProcessor.QueueLogItem(level, formattedMessage, exception, _correlationId);
                
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation(level.ToString(), stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"{level}_Error", stopwatch.Elapsed);
                
                // Fallback to synchronous logging to prevent loss of critical information
                try
                {
                    var fallbackMessage = $"[{_correlationId}] Async logging failed: {ex.Message}. Original: {message}";
                    switch (level)
                    {
                        case LogLevel.Trace:
                        case LogLevel.Debug:
                            if (_logger.IsDebugEnabled) _logger.Debug(fallbackMessage, exception ?? ex);
                            break;
                        case LogLevel.Information:
                            if (_logger.IsInfoEnabled) _logger.Info(fallbackMessage, exception ?? ex);
                            break;
                        case LogLevel.Warning:
                            if (_logger.IsWarnEnabled) _logger.Warn(fallbackMessage, exception ?? ex);
                            break;
                        case LogLevel.Error:
                            if (_logger.IsErrorEnabled) _logger.Error(fallbackMessage, exception ?? ex);
                            break;
                        case LogLevel.Critical:
                            if (_logger.IsFatalEnabled) _logger.Fatal(fallbackMessage, exception ?? ex);
                            break;
                    }
                }
                catch
                {
                    // Last resort - ignore to prevent infinite loops
                }
            }
        }

        /// <summary>
        /// Creates a properties dictionary from property values
        /// </summary>
        /// <param name="propertyValues">The property values to convert</param>
        /// <returns>A dictionary of properties</returns>
        private Dictionary<string, object> CreatePropertiesDictionary(object[] propertyValues)
        {
            var properties = new Dictionary<string, object>();
            for (int i = 0; i < propertyValues.Length; i++)
            {
                properties[$"Property_{i}"] = propertyValues[i] ?? "null";
            }
            return properties;
        }

        #endregion Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the service and releases all resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            _asyncProcessor?.Dispose();
            _logEntryPool?.Dispose();
        }

        #endregion IDisposable Implementation

        #region Nested Types

        /// <summary>
        /// Represents a logging scope that tracks duration
        /// </summary>
        private class LogScope : IDisposable
        {
            /// <summary>
            /// Reference to the log service
            /// </summary>
            private readonly Log4NetService _logService;
            
            /// <summary>
            /// Name of the scope
            /// </summary>
            private readonly string _scopeName;
            
            /// <summary>
            /// Stopwatch for measuring scope duration
            /// </summary>
            private readonly Stopwatch _stopwatch;

            /// <summary>
            /// Initializes a new instance of the LogScope class
            /// </summary>
            /// <param name="logService">The log service</param>
            /// <param name="scopeName">Name of the scope</param>
            public LogScope(Log4NetService logService, string scopeName)
            {
                _logService = logService;
                _scopeName = scopeName;
                _stopwatch = Stopwatch.StartNew();
            }

            /// <summary>
            /// Disposes the scope and logs the duration
            /// </summary>
            public void Dispose()
            {
                _stopwatch.Stop();
                _logService.LogDebug("Exiting scope: {ScopeName} (Duration: {Duration}ms)", _scopeName, _stopwatch.Elapsed.TotalMilliseconds);
                _logService._performanceMonitor.RecordLoggingOperation($"Scope_{_scopeName}", _stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Empty disposable scope for fallback scenarios
        /// </summary>
        private class DisposableScope : IDisposable
        {
            /// <summary>
            /// Empty dispose implementation
            /// </summary>
            public void Dispose() { }
        }

        #endregion Nested Types
    }
}