using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Object pool for log entries to reduce GC pressure
    /// </summary>
    public class LogEntryPool : IDisposable
    {
        private readonly ConcurrentQueue<PooledLogEntry> _pool = new();
        private readonly int _maxPoolSize;
        private int _currentPoolSize;
        private bool _disposed;

        public LogEntryPool(int maxPoolSize = 1000)
        {
            _maxPoolSize = maxPoolSize;
        }

        /// <summary>
        /// Gets a log entry from the pool or creates a new one
        /// </summary>
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
    }

    /// <summary>
    /// Pooled log entry that automatically returns to pool when disposed
    /// </summary>
    public class PooledLogEntry : IDisposable
    {
        private readonly LogEntryPool _pool;
        private bool _disposed;

        internal PooledLogEntry(LogEntryPool pool)
        {
            _pool = pool;
        }

        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public Exception? Exception { get; set; }
        public string ThreadId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string ApplicationVersion { get; set; } = string.Empty;

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

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _pool.Return(this);
        }
    }
}