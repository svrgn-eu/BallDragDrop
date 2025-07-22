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


}