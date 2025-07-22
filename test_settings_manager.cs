using System;
using System.IO;
using BallDragDrop.Services;
using BallDragDrop.Contracts;

// Simple test to verify SettingsManager fixes
class Program
{
    static void Main()
    {
        try
        {
            var mockLogService = new MockLogService();
            var testPath = Path.Combine(Path.GetTempPath(), "test_settings.json");
            
            // Test the constructor that was causing errors
            var settingsManager = new SettingsManager(mockLogService, testPath, true);
            
            // Test basic functionality
            settingsManager.SetSetting("test", "value");
            var result = settingsManager.GetSetting<string>("test", "default");
            
            Console.WriteLine($"Test passed! Retrieved value: {result}");
            
            // Clean up
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
        }
    }
}

public class MockLogService : ILogService
{
    public void LogTrace(string message, params object[] args) { }
    public void LogDebug(string message, params object[] args) { }
    public void LogInformation(string message, params object[] args) { }
    public void LogWarning(string message, params object[] args) { }
    public void LogError(string message, params object[] args) { }
    public void LogError(Exception exception, string message, params object[] args) { }
    public void LogCritical(string message, params object[] args) { }
    public void LogCritical(Exception exception, string message, params object[] args) { }
    public void LogStructured(LogLevel level, string messageTemplate, params object[] propertyValues) { }
    public void LogStructured(LogLevel level, Exception exception, string messageTemplate, params object[] propertyValues) { }
    public IDisposable BeginScope(string scopeName, params object[] parameters) => new MockScope();
    public void LogMethodEntry(string methodName, params object[] parameters) { }
    public void LogMethodExit(string methodName, object? returnValue = null, TimeSpan? duration = null) { }
    public void LogPerformance(string operationName, TimeSpan duration, params object[] additionalData) { }
    public void SetCorrelationId(string correlationId) { }
    public string GetCorrelationId() => Guid.NewGuid().ToString();
    
    private class MockScope : IDisposable
    {
        public void Dispose() { }
    }
}