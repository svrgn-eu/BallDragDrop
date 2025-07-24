using System;
using System.Collections.Generic;
using BallDragDrop.Contracts;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// Test implementation of ILogService for unit testing
    /// </summary>
    public class TestLogService : ILogService
    {
        public List<string> LogEntries { get; } = new List<string>();

        public void LogTrace(string message, params object[] args) 
        {
            LogEntries.Add($"TRACE: {string.Format(message, args)}");
        }
        
        public void LogDebug(string message, params object[] args) 
        {
            LogEntries.Add($"DEBUG: {string.Format(message, args)}");
        }
        
        public void LogInformation(string message, params object[] args) 
        {
            LogEntries.Add($"INFO: {string.Format(message, args)}");
        }
        
        public void LogWarning(string message, params object[] args) 
        {
            LogEntries.Add($"WARN: {string.Format(message, args)}");
        }
        
        public void LogError(string message, params object[] args) 
        {
            try
            {
                LogEntries.Add($"ERROR: {string.Format(message, args)}");
            }
            catch (FormatException)
            {
                // If formatting fails, just log the raw message
                LogEntries.Add($"ERROR: {message}");
            }
        }
        
        public void LogError(Exception exception, string message, params object[] args) 
        {
            try
            {
                LogEntries.Add($"ERROR: {string.Format(message, args)} - {exception.Message}");
            }
            catch (FormatException)
            {
                // If formatting fails, just log the raw message
                LogEntries.Add($"ERROR: {message} - {exception.Message}");
            }
        }
        
        public void LogCritical(string message, params object[] args) 
        {
            LogEntries.Add($"CRITICAL: {string.Format(message, args)}");
        }
        
        public void LogCritical(Exception exception, string message, params object[] args) 
        {
            LogEntries.Add($"CRITICAL: {string.Format(message, args)} - {exception.Message}");
        }
        
        public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues) 
        {
            LogEntries.Add($"{level}: {string.Format(messageTemplate, propertyValues)}");
        }
        
        public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues) 
        {
            LogEntries.Add($"{level}: {string.Format(messageTemplate, propertyValues)} - {exception.Message}");
        }
        
        public IDisposable BeginScope(string scopeName, params object[] parameters) => new TestScope();
        
        public void LogMethodEntry(string methodName, params object[] parameters) 
        {
            LogEntries.Add($"ENTRY: {methodName}");
        }
        
        public void LogMethodExit(string methodName, object? returnValue = null, TimeSpan? duration = null) 
        {
            LogEntries.Add($"EXIT: {methodName}");
        }
        
        public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData) 
        {
            LogEntries.Add($"PERF: {operationName} - {duration.TotalMilliseconds}ms");
        }
        
        public void SetCorrelationId(string correlationId) { }
        public string GetCorrelationId() => Guid.NewGuid().ToString();
        
        private class TestScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}