using System;
using System.Collections.Generic;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Represents a log item for async processing
    /// </summary>
    internal class LogItem
    {
        /// <summary>
        /// Gets or sets the log level
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// Gets or sets the log message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the exception associated with the log item
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation ID for tracking related log entries
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets additional properties associated with the log item
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the timestamp when the log item was created
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the thread ID where the log item was created
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;
    }
}