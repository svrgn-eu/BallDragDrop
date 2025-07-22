using System;
using System.Collections.Generic;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Pooled log entry that automatically returns to pool when disposed
    /// </summary>
    public class PooledLogEntry : IDisposable
    {
        #region Fields

        /// <summary>
        /// Reference to the pool that owns this entry
        /// </summary>
        private readonly LogEntryPool _pool;
        
        /// <summary>
        /// Flag indicating if this entry has been disposed
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the timestamp when the log entry was created
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the log level of the entry
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// Gets or sets the category of the log entry
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the log message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the correlation ID for tracking related log entries
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets additional properties associated with the log entry
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the exception associated with the log entry, if any
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Gets or sets the thread ID where the log entry was created
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the machine name where the log entry was created
        /// </summary>
        public string MachineName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the application version
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the PooledLogEntry class
        /// </summary>
        /// <param name="pool">The pool that owns this entry</param>
        internal PooledLogEntry(LogEntryPool pool)
        {
            _pool = pool;
        }

        #endregion Construction

        #region Methods

        /// <summary>
        /// Resets the log entry for reuse
        /// </summary>
        internal void Reset()
        {
            Timestamp = default;
            Level = LogLevel.Information;
            Category = string.Empty;
            Message = string.Empty;
            CorrelationId = string.Empty;
            Properties.Clear();
            Exception = null;
            ThreadId = string.Empty;
            MachineName = string.Empty;
            ApplicationVersion = string.Empty;
            _disposed = false;
        }

        /// <summary>
        /// Disposes the log entry and returns it to the pool
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _pool.Return(this);
        }

        #endregion Methods
    }
}