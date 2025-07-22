using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Object pool for log entries to reduce GC pressure
    /// </summary>
    public class LogEntryPool : IDisposable
    {
        #region Fields

        /// <summary>
        /// Concurrent queue storing pooled log entries
        /// </summary>
        private readonly ConcurrentQueue<PooledLogEntry> _pool = new();
        
        /// <summary>
        /// Maximum number of entries to keep in the pool
        /// </summary>
        private readonly int _maxPoolSize;
        
        /// <summary>
        /// Current number of entries in the pool
        /// </summary>
        private int _currentPoolSize;
        
        /// <summary>
        /// Flag indicating if the pool has been disposed
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the LogEntryPool class
        /// </summary>
        /// <param name="maxPoolSize">Maximum number of entries to keep in the pool</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxPoolSize is less than or equal to zero</exception>
        public LogEntryPool(int maxPoolSize = 1000)
        {
            _maxPoolSize = maxPoolSize;
        }

        #endregion Construction

        #region Methods

        /// <summary>
        /// Gets a log entry from the pool or creates a new one
        /// </summary>
        /// <returns>A pooled log entry instance</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed</exception>
        public PooledLogEntry Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(LogEntryPool));

            if (_pool.TryDequeue(out var entry))
            {
                System.Threading.Interlocked.Decrement(ref _currentPoolSize);
                entry.Reset();
                return entry;
            }

            return new PooledLogEntry(this);
        }

        /// <summary>
        /// Returns a log entry to the pool
        /// </summary>
        /// <param name="entry">The log entry to return to the pool</param>
        internal void Return(PooledLogEntry entry)
        {
            if (_disposed || entry == null) return;

            if (_currentPoolSize < _maxPoolSize)
            {
                entry.Reset();
                _pool.Enqueue(entry);
                System.Threading.Interlocked.Increment(ref _currentPoolSize);
            }
        }

        /// <summary>
        /// Disposes the log entry pool and clears all pooled entries
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Clear the pool
            while (_pool.TryDequeue(out _))
            {
                // Just drain the queue
            }
        }

        #endregion Methods
    }

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