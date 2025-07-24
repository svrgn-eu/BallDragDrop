using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Contracts;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents a single frame in an animation with memory management capabilities
    /// </summary>
    public class AnimationFrame : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Gets or sets the image for this frame
        /// </summary>
        public ImageSource Image { get; set; }

        /// <summary>
        /// Gets or sets the duration this frame should be displayed
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the source rectangle for sprite sheet frames
        /// </summary>
        public System.Windows.Rect SourceRect { get; set; }

        /// <summary>
        /// Gets a value indicating whether this frame is cached in memory
        /// </summary>
        public bool IsCached { get; internal set; }

        /// <summary>
        /// Gets the estimated memory usage of this frame in bytes
        /// </summary>
        public long EstimatedMemoryUsage
        {
            get
            {
                if (Image is BitmapSource bitmap)
                {
                    return (long)(bitmap.PixelWidth * bitmap.PixelHeight * (bitmap.Format.BitsPerPixel / 8.0));
                }
                return 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AnimationFrame class
        /// </summary>
        public AnimationFrame()
        {
            Duration = TimeSpan.FromMilliseconds(100); // Default 100ms per frame
            SourceRect = System.Windows.Rect.Empty;
            IsCached = false;
        }

        /// <summary>
        /// Initializes a new instance of the AnimationFrame class
        /// </summary>
        /// <param name="image">The image for this frame</param>
        /// <param name="duration">The duration this frame should be displayed</param>
        public AnimationFrame(ImageSource image, TimeSpan duration)
        {
            Image = image;
            Duration = duration;
            SourceRect = System.Windows.Rect.Empty;
            IsCached = false;
        }

        /// <summary>
        /// Initializes a new instance of the AnimationFrame class
        /// </summary>
        /// <param name="image">The image for this frame</param>
        /// <param name="duration">The duration this frame should be displayed</param>
        /// <param name="sourceRect">The source rectangle for sprite sheet frames</param>
        public AnimationFrame(ImageSource image, TimeSpan duration, System.Windows.Rect sourceRect)
        {
            Image = image;
            Duration = duration;
            SourceRect = sourceRect;
            IsCached = false;
        }

        /// <summary>
        /// Optimizes the frame for memory usage by freezing the image source
        /// </summary>
        public void OptimizeForMemory()
        {
            if (Image != null && Image.CanFreeze && !Image.IsFrozen)
            {
                Image.Freeze();
            }
        }

        /// <summary>
        /// Disposes of the frame resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the frame resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Clear the image reference to allow garbage collection
                Image = null;
                IsCached = false;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Engine for managing animation playback and timing with memory management
    /// </summary>
    public class AnimationEngine : IDisposable
    {
        #region Fields

        private readonly ILogService _logService;
        private List<AnimationFrame> _frames;
        private int _currentFrameIndex;
        private DateTime _lastFrameTime;
        private TimeSpan _elapsedTime;
        private bool _disposed = false;

        // Memory management fields
        private readonly Dictionary<int, AnimationFrame> _frameCache;
        private readonly int _maxCacheSize;
        private long _currentMemoryUsage;
        private readonly long _maxMemoryUsage;
        private readonly Queue<int> _cacheAccessOrder;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the list of animation frames
        /// </summary>
        public List<AnimationFrame> Frames => _frames ?? new List<AnimationFrame>();

        /// <summary>
        /// Gets the current frame index
        /// </summary>
        public int CurrentFrameIndex => _currentFrameIndex;

        /// <summary>
        /// Gets a value indicating whether the animation is currently playing
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the animation should loop
        /// </summary>
        public bool IsLooping { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of times the animation should loop (0 = infinite, -1 = no looping)
        /// </summary>
        public int LoopCount { get; set; } = 0;

        /// <summary>
        /// Gets the current loop iteration (starts at 0)
        /// </summary>
        public int CurrentLoop { get; private set; } = 0;

        /// <summary>
        /// Gets the total number of frames in the animation
        /// </summary>
        public int FrameCount => _frames?.Count ?? 0;

        /// <summary>
        /// Gets the current memory usage of cached frames in bytes
        /// </summary>
        public long CurrentMemoryUsage => _currentMemoryUsage;

        /// <summary>
        /// Gets the maximum allowed memory usage for frame caching in bytes
        /// </summary>
        public long MaxMemoryUsage => _maxMemoryUsage;

        /// <summary>
        /// Gets the number of frames currently cached in memory
        /// </summary>
        public int CachedFrameCount => _frameCache?.Count ?? 0;

        /// <summary>
        /// Gets the cache hit ratio as a percentage (0-100)
        /// </summary>
        public double CacheHitRatio { get; private set; }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AnimationEngine class
        /// </summary>
        /// <param name="logService">Optional logging service</param>
        /// <param name="maxCacheSize">Maximum number of frames to cache (default: 50)</param>
        /// <param name="maxMemoryUsageMB">Maximum memory usage for frame caching in MB (default: 100)</param>
        public AnimationEngine(ILogService logService = null, int maxCacheSize = 50, int maxMemoryUsageMB = 100)
        {
            _logService = logService;
            _frames = new List<AnimationFrame>();
            _currentFrameIndex = 0;
            IsPlaying = false;
            _lastFrameTime = DateTime.Now;
            _elapsedTime = TimeSpan.Zero;

            // Initialize memory management
            _maxCacheSize = maxCacheSize;
            _maxMemoryUsage = maxMemoryUsageMB * 1024 * 1024; // Convert MB to bytes
            _frameCache = new Dictionary<int, AnimationFrame>();
            _cacheAccessOrder = new Queue<int>();
            _currentMemoryUsage = 0;
            CacheHitRatio = 0.0;

            _logService?.LogDebug("AnimationEngine initialized with cache size: {0}, max memory: {1}MB", 
                _maxCacheSize, maxMemoryUsageMB);
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Loads frames into the animation engine with memory management
        /// </summary>
        /// <param name="frames">The list of animation frames</param>
        public void LoadFrames(List<AnimationFrame> frames)
        {
            _logService?.LogMethodEntry(nameof(LoadFrames), frames?.Count);

            try
            {
                // Clear existing cache and frames
                ClearCache();
                
                if (_frames != null)
                {
                    foreach (var frame in _frames)
                    {
                        frame?.Dispose();
                    }
                }

                if (frames == null)
                {
                    _logService?.LogWarning("Frames list is null");
                    _frames = new List<AnimationFrame>();
                }
                else
                {
                    _frames = new List<AnimationFrame>(frames);
                    _logService?.LogInformation("Loaded {FrameCount} animation frames", _frames.Count);

                    // Pre-load initial frames for smooth playback
                    int preloadCount = Math.Min(_frames.Count, Math.Min(_maxCacheSize / 2, 10));
                    if (preloadCount > 0)
                    {
                        PreloadFrames(0, preloadCount);
                        _logService?.LogDebug("Pre-loaded {PreloadCount} frames for smooth playback", preloadCount);
                    }
                }

                _currentFrameIndex = 0;
                CurrentLoop = 0;
                _elapsedTime = TimeSpan.Zero;
                _lastFrameTime = DateTime.Now;

                _logService?.LogMethodExit(nameof(LoadFrames));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading animation frames");
                _frames = new List<AnimationFrame>();
                _logService?.LogMethodExit(nameof(LoadFrames));
            }
        }

        /// <summary>
        /// Starts animation playback
        /// </summary>
        public void Play()
        {
            _logService?.LogMethodEntry(nameof(Play));

            try
            {
                if (_frames == null || _frames.Count == 0)
                {
                    _logService?.LogWarning("Cannot play animation: no frames loaded");
                    _logService?.LogMethodExit(nameof(Play));
                    return;
                }

                IsPlaying = true;
                _lastFrameTime = DateTime.Now;
                _logService?.LogDebug("Animation playback started");

                _logService?.LogMethodExit(nameof(Play));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error starting animation playback");
                _logService?.LogMethodExit(nameof(Play));
            }
        }

        /// <summary>
        /// Pauses animation playback
        /// </summary>
        public void Pause()
        {
            _logService?.LogMethodEntry(nameof(Pause));

            try
            {
                IsPlaying = false;
                _logService?.LogDebug("Animation playback paused");

                _logService?.LogMethodExit(nameof(Pause));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error pausing animation playback");
                _logService?.LogMethodExit(nameof(Pause));
            }
        }

        /// <summary>
        /// Stops animation playback and resets to the first frame
        /// </summary>
        public void Stop()
        {
            _logService?.LogMethodEntry(nameof(Stop));

            try
            {
                IsPlaying = false;
                _currentFrameIndex = 0;
                CurrentLoop = 0;
                _elapsedTime = TimeSpan.Zero;
                _lastFrameTime = DateTime.Now;
                _logService?.LogDebug("Animation playback stopped and reset");

                _logService?.LogMethodExit(nameof(Stop));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error stopping animation playback");
                _logService?.LogMethodExit(nameof(Stop));
            }
        }

        /// <summary>
        /// Advances to the next frame
        /// </summary>
        public void NextFrame()
        {
            _logService?.LogMethodEntry(nameof(NextFrame));

            try
            {
                if (_frames == null || _frames.Count == 0)
                {
                    _logService?.LogWarning("Cannot advance frame: no frames loaded");
                    _logService?.LogMethodExit(nameof(NextFrame));
                    return;
                }

                _currentFrameIndex++;

                if (_currentFrameIndex >= _frames.Count)
                {
                    // Check if we should loop
                    bool shouldLoop = IsLooping && (LoopCount == 0 || CurrentLoop < LoopCount);
                    
                    if (shouldLoop)
                    {
                        _currentFrameIndex = 0;
                        CurrentLoop++;
                        _logService?.LogDebug("Animation looped to first frame (loop {CurrentLoop})", CurrentLoop);
                    }
                    else
                    {
                        _currentFrameIndex = _frames.Count - 1;
                        IsPlaying = false;
                        _logService?.LogDebug("Animation reached end and stopped (completed {CurrentLoop} loops)", CurrentLoop);
                    }
                }

                _elapsedTime = TimeSpan.Zero;
                _lastFrameTime = DateTime.Now;

                _logService?.LogMethodExit(nameof(NextFrame));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error advancing to next frame");
                _logService?.LogMethodExit(nameof(NextFrame));
            }
        }

        /// <summary>
        /// Gets the current animation frame with caching support
        /// </summary>
        /// <returns>The current animation frame, or null if no frames are loaded</returns>
        public AnimationFrame GetCurrentFrame()
        {
            _logService?.LogMethodEntry(nameof(GetCurrentFrame));

            try
            {
                if (_frames == null || _frames.Count == 0 || _currentFrameIndex < 0 || _currentFrameIndex >= _frames.Count)
                {
                    _logService?.LogWarning("Cannot get current frame: invalid state");
                    _logService?.LogMethodExit(nameof(GetCurrentFrame), null);
                    return null;
                }

                // Try to get from cache first
                if (_frameCache.TryGetValue(_currentFrameIndex, out var cachedFrame))
                {
                    UpdateCacheAccess(_currentFrameIndex);
                    _logService?.LogMethodExit(nameof(GetCurrentFrame), "AnimationFrame (cached)");
                    return cachedFrame;
                }

                // Get from frames list and try to cache it
                var frame = _frames[_currentFrameIndex];
                CacheFrame(_currentFrameIndex);

                // Pre-load next few frames for smooth playback
                int nextFramesToPreload = Math.Min(3, _frames.Count - _currentFrameIndex - 1);
                for (int i = 1; i <= nextFramesToPreload; i++)
                {
                    int nextIndex = _currentFrameIndex + i;
                    if (nextIndex < _frames.Count && !_frameCache.ContainsKey(nextIndex))
                    {
                        CacheFrame(nextIndex);
                    }
                }

                _logService?.LogMethodExit(nameof(GetCurrentFrame), "AnimationFrame");
                return frame;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error getting current frame");
                _logService?.LogMethodExit(nameof(GetCurrentFrame), null);
                return null;
            }
        }

        /// <summary>
        /// Sets the current frame index for animation playback
        /// </summary>
        /// <param name="frameIndex">The frame index to set (will be clamped to valid range)</param>
        /// <returns>True if the frame index was set successfully, false otherwise</returns>
        public bool SetCurrentFrame(int frameIndex)
        {
            _logService?.LogMethodEntry(nameof(SetCurrentFrame), frameIndex);

            try
            {
                if (_frames == null || _frames.Count == 0)
                {
                    _logService?.LogWarning("Cannot set current frame: no frames loaded");
                    _logService?.LogMethodExit(nameof(SetCurrentFrame), false);
                    return false;
                }

                // Clamp the frame index to valid range
                var clampedIndex = Math.Max(0, Math.Min(frameIndex, _frames.Count - 1));
                
                if (clampedIndex != frameIndex)
                {
                    _logService?.LogDebug("Frame index {RequestedIndex} clamped to {ClampedIndex} (valid range: 0-{MaxIndex})", 
                        frameIndex, clampedIndex, _frames.Count - 1);
                }

                _currentFrameIndex = clampedIndex;
                _elapsedTime = TimeSpan.Zero;
                _lastFrameTime = DateTime.Now;

                _logService?.LogDebug("Current frame set to index {FrameIndex}", _currentFrameIndex);
                _logService?.LogMethodExit(nameof(SetCurrentFrame), true);
                return true;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error setting current frame to index {FrameIndex}", frameIndex);
                _logService?.LogMethodExit(nameof(SetCurrentFrame), false);
                return false;
            }
        }

        /// <summary>
        /// Updates the animation based on elapsed time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(TimeSpan deltaTime)
        {
            _logService?.LogMethodEntry(nameof(Update), deltaTime);

            try
            {
                if (!IsPlaying || _frames == null || _frames.Count == 0)
                {
                    _logService?.LogMethodExit(nameof(Update));
                    return;
                }

                _elapsedTime += deltaTime;

                var currentFrame = GetCurrentFrame();
                if (currentFrame != null && _elapsedTime >= currentFrame.Duration)
                {
                    NextFrame();
                }

                _logService?.LogMethodExit(nameof(Update));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error updating animation");
                _logService?.LogMethodExit(nameof(Update));
            }
        }

        /// <summary>
        /// Updates the animation based on current time
        /// </summary>
        public void Update()
        {
            var currentTime = DateTime.Now;
            var deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;
            Update(deltaTime);
        }

        /// <summary>
        /// Pre-loads animation frames into cache to prevent stuttering
        /// </summary>
        /// <param name="startIndex">Starting frame index for pre-loading</param>
        /// <param name="count">Number of frames to pre-load</param>
        public void PreloadFrames(int startIndex = 0, int count = -1)
        {
            _logService?.LogMethodEntry(nameof(PreloadFrames), startIndex, count);

            try
            {
                if (_frames == null || _frames.Count == 0)
                {
                    _logService?.LogWarning("Cannot preload frames: no frames loaded");
                    return;
                }

                int endIndex = count == -1 ? _frames.Count : Math.Min(startIndex + count, _frames.Count);
                int preloadedCount = 0;

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (CacheFrame(i))
                    {
                        preloadedCount++;
                    }
                }

                _logService?.LogInformation("Preloaded {PreloadedCount} frames (range: {StartIndex}-{EndIndex})", 
                    preloadedCount, startIndex, endIndex - 1);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error preloading frames");
            }
            finally
            {
                _logService?.LogMethodExit(nameof(PreloadFrames));
            }
        }

        /// <summary>
        /// Clears the frame cache and releases memory
        /// </summary>
        public void ClearCache()
        {
            _logService?.LogMethodEntry(nameof(ClearCache));

            try
            {
                // Dispose cached frames
                foreach (var cachedFrame in _frameCache.Values)
                {
                    cachedFrame?.Dispose();
                }

                _frameCache.Clear();
                _cacheAccessOrder.Clear();
                _currentMemoryUsage = 0;

                // Update cache status for all frames
                if (_frames != null)
                {
                    foreach (var frame in _frames)
                    {
                        frame.IsCached = false;
                    }
                }

                _logService?.LogDebug("Frame cache cleared, memory usage reset to 0");
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error clearing frame cache");
            }
            finally
            {
                _logService?.LogMethodExit(nameof(ClearCache));
            }
        }

        /// <summary>
        /// Optimizes memory usage by removing least recently used frames from cache
        /// </summary>
        public void OptimizeMemoryUsage()
        {
            _logService?.LogMethodEntry(nameof(OptimizeMemoryUsage));

            try
            {
                int removedFrames = 0;
                long freedMemory = 0;

                // Remove frames until we're under memory limit
                while (_currentMemoryUsage > _maxMemoryUsage && _cacheAccessOrder.Count > 0)
                {
                    int frameIndex = _cacheAccessOrder.Dequeue();
                    if (_frameCache.TryGetValue(frameIndex, out var frame))
                    {
                        freedMemory += frame.EstimatedMemoryUsage;
                        _currentMemoryUsage -= frame.EstimatedMemoryUsage;
                        
                        frame.IsCached = false;
                        frame.Dispose();
                        
                        _frameCache.Remove(frameIndex);
                        removedFrames++;
                    }
                }

                _logService?.LogDebug("Memory optimization completed: removed {RemovedFrames} frames, freed {FreedMemoryMB:F2}MB", 
                    removedFrames, freedMemory / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error optimizing memory usage");
            }
            finally
            {
                _logService?.LogMethodExit(nameof(OptimizeMemoryUsage));
            }
        }

        /// <summary>
        /// Gets memory usage statistics for monitoring
        /// </summary>
        /// <returns>Dictionary containing memory usage statistics</returns>
        public Dictionary<string, object> GetMemoryStats()
        {
            return new Dictionary<string, object>
            {
                ["CurrentMemoryUsageMB"] = _currentMemoryUsage / (1024.0 * 1024.0),
                ["MaxMemoryUsageMB"] = _maxMemoryUsage / (1024.0 * 1024.0),
                ["CachedFrameCount"] = _frameCache.Count,
                ["TotalFrameCount"] = _frames?.Count ?? 0,
                ["CacheHitRatio"] = CacheHitRatio,
                ["MemoryUtilization"] = _maxMemoryUsage > 0 ? (_currentMemoryUsage / (double)_maxMemoryUsage) * 100 : 0
            };
        }

        /// <summary>
        /// Disposes of the animation engine and releases all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Caches a frame at the specified index
        /// </summary>
        /// <param name="frameIndex">Index of the frame to cache</param>
        /// <returns>True if the frame was cached successfully</returns>
        private bool CacheFrame(int frameIndex)
        {
            if (_frames == null || frameIndex < 0 || frameIndex >= _frames.Count)
            {
                return false;
            }

            // Check if frame is already cached
            if (_frameCache.ContainsKey(frameIndex))
            {
                // Update access order
                UpdateCacheAccess(frameIndex);
                return true;
            }

            var frame = _frames[frameIndex];
            long frameMemoryUsage = frame.EstimatedMemoryUsage;

            // Check if we have space in cache
            if (_frameCache.Count >= _maxCacheSize || _currentMemoryUsage + frameMemoryUsage > _maxMemoryUsage)
            {
                // Try to make space by removing least recently used frames
                OptimizeMemoryUsage();
                
                // Check again if we have space
                if (_frameCache.Count >= _maxCacheSize || _currentMemoryUsage + frameMemoryUsage > _maxMemoryUsage)
                {
                    return false; // Cannot cache this frame
                }
            }

            // Optimize frame for memory usage
            frame.OptimizeForMemory();

            // Add to cache
            _frameCache[frameIndex] = frame;
            _cacheAccessOrder.Enqueue(frameIndex);
            _currentMemoryUsage += frameMemoryUsage;
            frame.IsCached = true;

            return true;
        }

        /// <summary>
        /// Updates the cache access order for a frame
        /// </summary>
        /// <param name="frameIndex">Index of the accessed frame</param>
        private void UpdateCacheAccess(int frameIndex)
        {
            // Remove from current position in queue and add to end
            var tempQueue = new Queue<int>();
            while (_cacheAccessOrder.Count > 0)
            {
                int index = _cacheAccessOrder.Dequeue();
                if (index != frameIndex)
                {
                    tempQueue.Enqueue(index);
                }
            }

            // Restore queue and add accessed frame to end
            while (tempQueue.Count > 0)
            {
                _cacheAccessOrder.Enqueue(tempQueue.Dequeue());
            }
            _cacheAccessOrder.Enqueue(frameIndex);
        }

        /// <summary>
        /// Disposes of the animation engine resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Clear cache and dispose frames
                ClearCache();

                // Dispose all frames
                if (_frames != null)
                {
                    foreach (var frame in _frames)
                    {
                        frame?.Dispose();
                    }
                    _frames.Clear();
                }

                _disposed = true;
                _logService?.LogDebug("AnimationEngine disposed");
            }
        }

        #endregion Private Methods
    }
}