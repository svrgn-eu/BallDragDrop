using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Manages application settings persistence
    /// </summary>
    public class SettingsManager
    {
        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the SettingsManager class
        /// </summary>
        /// <param name="logService">Logging service</param>
        /// <param name="settingsFileName">Optional settings file name</param>
        public SettingsManager(ILogService logService, string settingsFileName = "settings.json")
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            using var scope = _logService?.BeginScope("SettingsManager.Constructor", settingsFileName);
            
            // Set up the settings file path in the local application data folder
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BallDragDrop");
                
            // Create the directory if it doesn't exist
            Directory.CreateDirectory(appDataPath);
            
            // Set the settings file path
            _settingsFilePath = Path.Combine(appDataPath, settingsFileName);
            
            // Initialize settings dictionary
            _settings = new Dictionary<string, object>();
            
            _logService?.LogDebug("SettingsManager initialized with file path: {FilePath}", _settingsFilePath);
        }

        #endregion Construction

        #region Fields

        // Default settings file path
        private readonly string _settingsFilePath;
        
        // Current settings
        private Dictionary<string, object> _settings;
        
        // Logging service
        private readonly ILogService _logService;

        #endregion Fields
        
        /// <summary>
        /// Loads settings from the settings file
        /// </summary>
        /// <returns>True if settings were loaded successfully, false otherwise</returns>
        public bool LoadSettings()
        {
            _logService?.LogMethodEntry(nameof(LoadSettings));
            
            try
            {
                // Check if the settings file exists
                if (File.Exists(_settingsFilePath))
                {
                    _logService?.LogDebug("Settings file found, loading from: {FilePath}", _settingsFilePath);
                    
                    // Read the settings file
                    string json = File.ReadAllText(_settingsFilePath);
                    
                    _logService?.LogTrace("Settings file content length: {Length} characters", json.Length);
                    
                    // Deserialize the settings
                    var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    
                    // Convert JsonElement values to appropriate types
                    _settings = new Dictionary<string, object>();
                    foreach (var kvp in loadedSettings)
                    {
                        _settings[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                    
                    _logService?.LogInformation("Settings loaded successfully. {SettingCount} settings found", _settings.Count);
                    _logService?.LogMethodExit(nameof(LoadSettings), true);
                    return true;
                }
                
                _logService?.LogInformation("Settings file does not exist: {FilePath}", _settingsFilePath);
                _logService?.LogMethodExit(nameof(LoadSettings), false);
                return false;
            }
            catch (Exception ex)
            {
                // Log the error with structured logging
                _logService?.LogError(ex, "Error loading settings from file: {FilePath}", _settingsFilePath);
                _logService?.LogMethodExit(nameof(LoadSettings), false);
                return false;
            }
        }
        
        /// <summary>
        /// Saves settings to the settings file
        /// </summary>
        /// <returns>True if settings were saved successfully, false otherwise</returns>
        public bool SaveSettings()
        {
            _logService?.LogMethodEntry(nameof(SaveSettings));
            
            try
            {
                _logService?.LogDebug("Saving {SettingCount} settings to file: {FilePath}", _settings.Count, _settingsFilePath);
                
                // Serialize the settings
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                _logService?.LogTrace("Serialized settings JSON length: {Length} characters", json.Length);
                
                // Write the settings file
                File.WriteAllText(_settingsFilePath, json);
                
                _logService?.LogInformation("Settings saved successfully to: {FilePath}", _settingsFilePath);
                _logService?.LogMethodExit(nameof(SaveSettings), true);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error with structured logging
                _logService?.LogError(ex, "Error saving settings to file: {FilePath}", _settingsFilePath);
                _logService?.LogMethodExit(nameof(SaveSettings), false);
                return false;
            }
        }
        
        /// <summary>
        /// Gets a setting value
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist</param>
        /// <returns>The setting value or the default value if the setting doesn't exist</returns>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            _logService?.LogTrace("Getting setting: {Key} (type: {Type})", key, typeof(T).Name);
            
            if (_settings.TryGetValue(key, out object value))
            {
                try
                {
                    // Try to convert the value to the requested type
                    if (value is JsonElement jsonElement)
                    {
                        var result = (T)ConvertJsonElement(jsonElement, typeof(T));
                        _logService?.LogTrace("Setting {Key} found with value: {Value}", key, result);
                        return result;
                    }
                    
                    var convertedValue = (T)Convert.ChangeType(value, typeof(T));
                    _logService?.LogTrace("Setting {Key} found with value: {Value}", key, convertedValue);
                    return convertedValue;
                }
                catch (Exception ex)
                {
                    // If conversion fails, return the default value
                    _logService?.LogError(ex, "Failed to convert setting {Key} to type {Type}, using default value: {DefaultValue}", 
                        key, typeof(T).Name, defaultValue);
                    return defaultValue;
                }
            }
            
            _logService?.LogTrace("Setting {Key} not found, using default value: {DefaultValue}", key, defaultValue);
            return defaultValue;
        }
        
        /// <summary>
        /// Sets a setting value
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="value">The setting value</param>
        public void SetSetting<T>(string key, T value)
        {
            _logService?.LogTrace("Setting value for key: {Key} = {Value} (type: {Type})", key, value, typeof(T).Name);
            
            var oldValue = _settings.ContainsKey(key) ? _settings[key] : null;
            _settings[key] = value;
            
            if (oldValue != null && !oldValue.Equals(value))
            {
                _logService?.LogDebug("Setting {Key} changed from {OldValue} to {NewValue}", key, oldValue, value);
            }
            else if (oldValue == null)
            {
                _logService?.LogDebug("New setting {Key} added with value: {Value}", key, value);
            }
        }
        
        /// <summary>
        /// Checks if a setting exists
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>True if the setting exists, false otherwise</returns>
        public bool HasSetting(string key)
        {
            var exists = _settings.ContainsKey(key);
            _logService?.LogTrace("Checking if setting exists: {Key} = {Exists}", key, exists);
            return exists;
        }
        
        /// <summary>
        /// Removes a setting
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>True if the setting was removed, false otherwise</returns>
        public bool RemoveSetting(string key)
        {
            var removed = _settings.Remove(key);
            _logService?.LogDebug("Setting {Key} removal: {Success}", key, removed ? "successful" : "failed (key not found)");
            return removed;
        }
        
        /// <summary>
        /// Clears all settings
        /// </summary>
        public void ClearSettings()
        {
            var count = _settings.Count;
            _settings.Clear();
            _logService?.LogInformation("Cleared all settings. {Count} settings removed", count);
        }
        
        /// <summary>
        /// Gets all setting keys
        /// </summary>
        /// <returns>An enumerable of setting keys</returns>
        public IEnumerable<string> GetKeys()
        {
            return _settings.Keys;
        }
        
        /// <summary>
        /// Converts a JsonElement to an appropriate .NET type
        /// </summary>
        /// <param name="element">The JsonElement to convert</param>
        /// <returns>The converted value</returns>
        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return 0;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ConvertJsonElement(property.Value);
                    }
                    return obj;
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(ConvertJsonElement(item));
                    }
                    return array;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Converts a JsonElement to a specific type
        /// </summary>
        /// <param name="element">The JsonElement to convert</param>
        /// <param name="targetType">The target type</param>
        /// <returns>The converted value</returns>
        private object ConvertJsonElement(JsonElement element, Type targetType)
        {
            if (targetType == typeof(string))
                return element.GetString();
            if (targetType == typeof(int) || targetType == typeof(int?))
                return element.GetInt32();
            if (targetType == typeof(long) || targetType == typeof(long?))
                return element.GetInt64();
            if (targetType == typeof(double) || targetType == typeof(double?))
                return element.GetDouble();
            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return element.GetBoolean();
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return element.GetDateTime();
            
            // For complex types, deserialize the element
            try {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize(element.GetRawText(), targetType, options);
            }
            catch (JsonException) {
                // If direct deserialization fails, try to convert from dictionary
                if (element.ValueKind == JsonValueKind.Object && 
                    targetType != typeof(Dictionary<string, object>))
                {
                    var dict = ConvertJsonElement(element) as Dictionary<string, object>;
                    if (dict != null)
                    {
                        try {
                            // Create an instance of the target type
                            var instance = Activator.CreateInstance(targetType);
                            
                            // Set properties from dictionary
                            foreach (var prop in targetType.GetProperties())
                            {
                                if (dict.TryGetValue(prop.Name, out var value))
                                {
                                    try {
                                        if (value != null)
                                        {
                                            if (prop.PropertyType == typeof(string))
                                                prop.SetValue(instance, value.ToString());
                                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                                                prop.SetValue(instance, Convert.ToInt32(value));
                                            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                                                prop.SetValue(instance, Convert.ToBoolean(value));
                                            else
                                                prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType));
                                        }
                                    }
                                    catch {
                                        // Ignore property setting errors
                                    }
                                }
                            }
                            
                            return instance;
                        }
                        catch {
                            // If anything fails, return null
                        }
                    }
                }
                return null;
            }
        }
    }
}