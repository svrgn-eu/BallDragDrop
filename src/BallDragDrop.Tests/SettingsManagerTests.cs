using System;
using System.IO;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class SettingsManagerTests
    {
        // Test settings file path
        private readonly string _testSettingsPath = Path.Combine(
            Path.GetTempPath(),
            $"BallDragDrop_TestSettings_{Guid.NewGuid()}.json");
            
        // Settings manager instance
        private SettingsManager _settingsManager;
        
        // Mock log service for testing
        private MockLogService _mockLogService;
        
        [TestInitialize]
        public void TestInitialize()
        {
            // Create mock log service
            _mockLogService = new MockLogService();
            
            // Create a new settings manager with the test settings path
            _settingsManager = new SettingsManager(_mockLogService, _testSettingsPath, true);
            
            // Ensure the test settings file doesn't exist
            if (File.Exists(_testSettingsPath))
            {
                File.Delete(_testSettingsPath);
            }
        }
        
        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up the test settings file
            if (File.Exists(_testSettingsPath))
            {
                File.Delete(_testSettingsPath);
            }
        }
        
        /// <summary>
        /// Tests that settings can be saved and loaded
        /// </summary>
        [TestMethod]
        public void TestSaveAndLoadSettings()
        {
            // Set some test settings
            _settingsManager.SetSetting("TestString", "Hello, World!");
            _settingsManager.SetSetting("TestInt", 42);
            _settingsManager.SetSetting("TestDouble", 3.14159);
            _settingsManager.SetSetting("TestBool", true);
            
            // Save the settings
            bool saveResult = _settingsManager.SaveSettings();
            Assert.IsTrue(saveResult, "Settings should be saved successfully");
            
            // Verify the settings file exists
            Assert.IsTrue(File.Exists(_testSettingsPath), "Settings file should exist after saving");
            
            // Create a new settings manager to load the settings
            var newSettingsManager = new SettingsManager(_mockLogService, _testSettingsPath, true);
            
            // Load the settings
            bool loadResult = newSettingsManager.LoadSettings();
            Assert.IsTrue(loadResult, "Settings should be loaded successfully");
            
            // Verify the settings were loaded correctly
            Assert.AreEqual("Hello, World!", newSettingsManager.GetSetting<string>("TestString"), "String setting should be loaded correctly");
            Assert.AreEqual(42, newSettingsManager.GetSetting<int>("TestInt"), "Int setting should be loaded correctly");
            Assert.AreEqual(3.14159, newSettingsManager.GetSetting<double>("TestDouble"), "Double setting should be loaded correctly");
            Assert.IsTrue(newSettingsManager.GetSetting<bool>("TestBool"), "Bool setting should be loaded correctly");
        }
        
        /// <summary>
        /// Tests that default values are returned for non-existent settings
        /// </summary>
        [TestMethod]
        public void TestDefaultValues()
        {
            // Get settings that don't exist
            string stringValue = _settingsManager.GetSetting<string>("NonExistentString", "Default");
            int intValue = _settingsManager.GetSetting<int>("NonExistentInt", 100);
            double doubleValue = _settingsManager.GetSetting<double>("NonExistentDouble", 2.71828);
            bool boolValue = _settingsManager.GetSetting<bool>("NonExistentBool", true);
            
            // Verify default values are returned
            Assert.AreEqual("Default", stringValue, "Default string value should be returned");
            Assert.AreEqual(100, intValue, "Default int value should be returned");
            Assert.AreEqual(2.71828, doubleValue, "Default double value should be returned");
            Assert.IsTrue(boolValue, "Default bool value should be returned");
        }
        
        /// <summary>
        /// Tests that settings can be removed
        /// </summary>
        [TestMethod]
        public void TestRemoveSettings()
        {
            // Set a test setting
            _settingsManager.SetSetting("TestSetting", "Value");
            
            // Verify the setting exists
            Assert.IsTrue(_settingsManager.HasSetting("TestSetting"), "Setting should exist after being set");
            
            // Remove the setting
            bool removeResult = _settingsManager.RemoveSetting("TestSetting");
            Assert.IsTrue(removeResult, "Setting should be removed successfully");
            
            // Verify the setting no longer exists
            Assert.IsFalse(_settingsManager.HasSetting("TestSetting"), "Setting should not exist after being removed");
        }
        
        /// <summary>
        /// Tests that all settings can be cleared
        /// </summary>
        [TestMethod]
        public void TestClearSettings()
        {
            // Set some test settings
            _settingsManager.SetSetting("Setting1", "Value1");
            _settingsManager.SetSetting("Setting2", "Value2");
            
            // Verify the settings exist
            Assert.IsTrue(_settingsManager.HasSetting("Setting1"), "Setting1 should exist after being set");
            Assert.IsTrue(_settingsManager.HasSetting("Setting2"), "Setting2 should exist after being set");
            
            // Clear all settings
            _settingsManager.ClearSettings();
            
            // Verify the settings no longer exist
            Assert.IsFalse(_settingsManager.HasSetting("Setting1"), "Setting1 should not exist after clearing settings");
            Assert.IsFalse(_settingsManager.HasSetting("Setting2"), "Setting2 should not exist after clearing settings");
        }
        
        /// <summary>
        /// Tests that complex objects can be saved and loaded
        /// </summary>
        [TestMethod]
        public void TestComplexObjects()
        {
            // Create a test object
            var testObject = new TestObject
            {
                Name = "Test Object",
                Value = 42,
                IsActive = true
            };
            
            // Set the object as a setting
            _settingsManager.SetSetting("TestObject", testObject);
            
            // Save the settings
            _settingsManager.SaveSettings();
            
            // Create a new settings manager to load the settings
            var newSettingsManager = new SettingsManager(_mockLogService, _testSettingsPath, true);
            newSettingsManager.LoadSettings();
            
            // Get the object from settings
            var loadedObject = newSettingsManager.GetSetting<TestObject>("TestObject");
            
            // Verify the object was loaded correctly
            Assert.IsNotNull(loadedObject, "Object should be loaded successfully");
            Assert.AreEqual("Test Object", loadedObject.Name, "Object name should be loaded correctly");
            Assert.AreEqual(42, loadedObject.Value, "Object value should be loaded correctly");
            Assert.IsTrue(loadedObject.IsActive, "Object isActive should be loaded correctly");
        }
        
        /// <summary>
        /// Test object for complex object serialization tests
        /// </summary>
        public class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public bool IsActive { get; set; }
        }
        
        /// <summary>
        /// Mock implementation of ILogService for testing
        /// </summary>
        private class MockLogService : ILogService
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
    }
}