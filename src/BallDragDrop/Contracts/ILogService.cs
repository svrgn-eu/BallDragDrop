using System;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Interface for logging services
    /// </summary>
    public interface ILogService
    {
        // Standard logging methods
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCritical(string message, params object[] args);
        void LogCritical(Exception exception, string message, params object[] args);
        
        // Structured logging methods
        void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues);
        void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues);
        
        // Method logging helpers
        IDisposable BeginScope(string scopeName, params object[] parameters);
        void LogMethodEntry(string methodName, params object[] parameters);
        void LogMethodExit(string methodName, object returnValue = null, TimeSpan? duration = null);
        
        // Performance logging
        void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData);
        
        // Context management
        void SetCorrelationId(string correlationId);
        string GetCorrelationId();
    }

    /// <summary>
    /// Log levels enumeration
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace level logging
        /// </summary>
        Trace = 0,
        /// <summary>
        /// Debug level logging
        /// </summary>
        Debug = 1,
        /// <summary>
        /// Information level logging
        /// </summary>
        Information = 2,
        /// <summary>
        /// Warning level logging
        /// </summary>
        Warning = 3,
        /// <summary>
        /// Error level logging
        /// </summary>
        Error = 4,
        /// <summary>
        /// Critical level logging
        /// </summary>
        Critical = 5
    }
}