using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services.Performance
{
    /// <summary>
    /// Async log processor with batching for efficient I/O operations
    /// </summary>
    public class AsyncLogProcessor : IDisposable
    {
        private readonly ILog _logger;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ConcurrentQueue<LogItem> _logQueue = new();
        private readonly Timer _batchTimer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _processingTask;
        private readonly int _batchSize;
        private readonly TimeSpan _batchTimeout;
        private bool _disposed;

        public AsyncLogProcessor(ILog logger, IPerformanceMonitor performanceMonitor, 
            int batchSize = 100, TimeSpan? batchTimeout = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _batchSize = batchSize;
            _batchTimeout = batchTimeout ?? TimeSpan.FromMilliseconds(500);

            _batchTimer = new Timer(ProcessBatch, null, _batchTimeout, _batchTimeout);
            _processingTask = Task.Run(ProcessQueueAsync, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Queues a log item for async processing
        /// </summary>
        public void QueueLogItem(LogLevel level, string message, Exception? exception = null, 
            string? correlationId = null, Dictionary<string, object>? properties = null)
        {
            if (_disposed) return;

            var logItem = new LogItem
            {
                Level = level,
                Message = message,
                Exception = exception,
                CorrelationId = correlationId ?? string.Empty,
                Properties = properties ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };

            _logQueue.Enqueue(logItem);
        }

        private async Task ProcessQueueAsync()
        {
            var batch = new List<LogItem>(_batchSize);
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Collect items for batch processing
                    var itemsCollected = 0;
                    while (itemsCollected < _batchSize && _logQueue.TryDequeue(out var item))
                    {
                        batch.Add(item);
                        itemsCollected++;
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch);
                        batch.Clear();
                    }
                    else
                    {
                        // No items to process, wait a bit
                        await Task.Delay(50, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log processing error (fallback to synchronous logging)
                    try
                    {
                        _logger.Error($"Error in async log processing: {ex.Message}", ex);
                    }
                    catch
                    {
                        // Ignore errors in error logging to prevent infinite loops
                    }
                }
            }

            // Process remaining items
            var remainingBatch = new List<LogItem>();
            while (_logQueue.TryDequeue(out var item))
            {
                remainingBatch.Add(item);
            }

            if (remainingBatch.Count > 0)
            {
                await ProcessBatchAsync(remainingBatch);
            }
        }

        private void ProcessBatch(object? state)
        {
            // Timer-based batch processing for timeout scenarios
            if (_disposed) return;

            var batch = new List<LogItem>();
            var itemsCollected = 0;
            
            while (itemsCollected < _batchSize && _logQueue.TryDequeue(out var item))
            {
                batch.Add(item);
                itemsCollected++;
            }

            if (batch.Count > 0)
            {
                _ = Task.Run(() => ProcessBatchAsync(batch));
            }
        }

        private async Task ProcessBatchAsync(List<LogItem> batch)
        {
            if (batch.Count == 0) return;

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Process batch items
                await Task.Run(() =>
                {
                    foreach (var item in batch)
                    {
                        ProcessLogItem(item);
                    }
                });

                stopwatch.Stop();
                _performanceMonitor.RecordBatchProcessing(batch.Count, stopwatch.Elapsed);
                _performanceMonitor.RecordLogEntriesProcessed(batch.Count);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                
                // Fallback to individual processing
                foreach (var item in batch)
                {
                    try
                    {
                        ProcessLogItem(item);
                    }
                    catch (Exception itemEx)
                    {
                        // Log the error synchronously as a last resort
                        try
                        {
                            _logger.Error($"Failed to process log item: {itemEx.Message}", itemEx);
                        }
                        catch
                        {
                            // Ignore to prevent infinite loops
                        }
                    }
                }
            }
        }

        private void ProcessLogItem(LogItem item)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var formattedMessage = $"[{item.CorrelationId}] {item.Message}";
                
                switch (item.Level)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        if (_logger.IsDebugEnabled)
                            _logger.Debug(formattedMessage, item.Exception);
                        break;
                    case LogLevel.Information:
                        if (_logger.IsInfoEnabled)
                            _logger.Info(formattedMessage, item.Exception);
                        break;
                    case LogLevel.Warning:
                        if (_logger.IsWarnEnabled)
                            _logger.Warn(formattedMessage, item.Exception);
                        break;
                    case LogLevel.Error:
                        if (_logger.IsErrorEnabled)
                            _logger.Error(formattedMessage, item.Exception);
                        break;
                    case LogLevel.Critical:
                        if (_logger.IsFatalEnabled)
                            _logger.Fatal(formattedMessage, item.Exception);
                        break;
                }
                
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation(item.Level.ToString(), stopwatch.Elapsed);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordLoggingOperation($"{item.Level}_Error", stopwatch.Elapsed);
                throw;
            }
        }

        #region Dispose
        /// <summary>
        /// Disposes the instance
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            _batchTimer?.Dispose();
            _cancellationTokenSource.Cancel();
            
            try
            {
                _processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Ignore timeout exceptions during shutdown
            }
            
            _cancellationTokenSource.Dispose();
        }
        #endregion Dispose

    }
}
