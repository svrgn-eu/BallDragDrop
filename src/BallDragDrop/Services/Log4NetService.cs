using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using BallDragDrop.Services.Performance;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Enhanced Log4NET implementation of ILogService with performance optimizations
    /// </summary>
    public class Log4NetService : ILogService, IDisposable
    {
        private readonly ILog _logger;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly LogEntryPool _logEntryPool;
        private readonly AsyncLogProcessor _asyncProcessor;
        private string _correlationId = Guid.NewGuid().ToString();
        private bool _disposed;

        public Log4NetService(IPerformanceMonitor? performanceMonitor = null)
        {
            _logger = LogManager.GetLogger(typeof(Log4NetService));
            _performanceMonitor = performanceMonitor ?? (IPerformanceMonitor)new LoggingPerformanceMonitor();
            _logEntryPool = new LogEntryPool();
            _asyncProcessor = new AsyncLogProcessor(_logger, _performanceMonitor);
        }

        public void LogTrace(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Trace, message, null, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Debug, message, null, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Information, message, null, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Warning, message, null, args);
        }

        public void LogError(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Error, message, null, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Error, message, exception, args);
        }

        public void LogCritical(string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Critical, message, null, args);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            if (_disposed) return;
            LogWithPerformanceTracking(LogLevel.Critical, message, exception, args);
        }

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

        public IDisposable BeginScope(string scopeName, params object[] parameters)
        {
            if (_disposed) return new DisposableScope();
            
            LogDebug("Entering scope: {ScopeName} with parameters: {Parameters}", scopeName, string.Join(", ", parameters));
            return new LogScope(this, scopeName);
        }

        public void LogMethodEntry(string methodName, params object[] parameters)
        {
            if (_disposed) return;
            LogDebug("Entering method: {MethodName} with parameters: {Parameters}", methodName, string.Join(", ", parameters));
        }

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

        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData)
        {
            if (_disposed) return;
            
            LogInformation("Performance: {OperationName} completed in {Duration}ms. Additional data: {AdditionalData}", 
                operationName, duration.TotalMilliseconds, string.Join(", ", additionalData));
            
            // Record operation performance
            _performanceMonitor.RecordLoggingOperation($"Operation_{operationName}", duration);
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? Guid.NewGuid().ToString();
        }

        public string GetCorrelationId()
        {
            return _correlationId;
        }

        /// <summary>
        /// Gets current performance statistics
        /// </summary>
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

        private Dictionary<string, object> CreatePropertiesDictionary(object[] propertyValues)
        {
            var properties = new Dictionary<string, object>();
            for (int i = 0; i < propertyValues.Length; i++)
            {
                properties[$"Property_{i}"] = propertyValues[i] ?? "null";
            }
            return properties;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            _asyncProcessor?.Dispose();
            _logEntryPool?.Dispose();
        }

        private class LogScope : IDisposable
        {
            private readonly Log4NetService _logService;
            private readonly string _scopeName;
            private readonly Stopwatch _stopwatch;

            public LogScope(Log4NetService logService, string scopeName)
            {
                _logService = logService;
                _scopeName = scopeName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _logService.LogDebug("Exiting scope: {ScopeName} (Duration: {Duration}ms)", _scopeName, _stopwatch.Elapsed.TotalMilliseconds);
                _logService._performanceMonitor.RecordLoggingOperation($"Scope_{_scopeName}", _stopwatch.Elapsed);
            }
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}