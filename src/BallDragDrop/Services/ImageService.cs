using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Contracts;
using BallDragDrop.Models;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Enumeration for supported visual content types
    /// </summary>
    public enum VisualContentType
    {
        StaticImage,
        GifAnimation,
        AsepriteAnimation,
        Unknown
    }

    /// <summary>
    /// Data structure for holding GIF animation data
    /// </summary>
    public class GifData
    {
        /// <summary>
        /// Gets or sets the list of animation frames
        /// </summary>
        public List<AnimationFrame> Frames { get; set; } = new List<AnimationFrame>();

        /// <summary>
        /// Gets or sets the loop count (0 = infinite, -1 = no looping)
        /// </summary>
        public int LoopCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total duration of the animation
        /// </summary>
        public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero;
    }

    /// <summary>
    /// Service for loading and managing visual content including static images and animations
    /// </summary>
    public class ImageService
    {
        #region Fields

        private AnimationEngine _animationEngine;
        private AsepriteLoader _asepriteLoader;
        private ILogService _logService;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the current frame for display
        /// </summary>
        public ImageSource CurrentFrame { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current content is animated
        /// </summary>
        public bool IsAnimated { get; private set; }

        /// <summary>
        /// Gets the duration of the current frame
        /// </summary>
        public TimeSpan FrameDuration { get; private set; }

        /// <summary>
        /// Gets the type of the currently loaded visual content
        /// </summary>
        public VisualContentType ContentType { get; private set; }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ImageService class
        /// </summary>
        /// <param name="logService">Optional logging service</param>
        public ImageService(ILogService logService = null)
        {
            _logService = logService;
            _animationEngine = new AnimationEngine(logService);
            _asepriteLoader = new AsepriteLoader(logService);
            ContentType = VisualContentType.Unknown;
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Loads visual content from the specified file path
        /// </summary>
        /// <param name="filePath">Path to the visual content file</param>
        /// <returns>True if loading was successful, false otherwise</returns>
        public async Task<bool> LoadBallVisualAsync(string filePath)
        {
            _logService?.LogMethodEntry(nameof(LoadBallVisualAsync), filePath);

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logService?.LogWarning("File path is null or empty");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    _logService?.LogWarning("File does not exist: {FilePath}", filePath);
                    return false;
                }

                // Detect file type
                var contentType = DetectFileType(filePath);
                _logService?.LogDebug("Detected content type: {ContentType} for file: {FilePath}", contentType, filePath);

                bool success = false;

                switch (contentType)
                {
                    case VisualContentType.StaticImage:
                        success = await LoadStaticImageAsync(filePath);
                        break;
                    case VisualContentType.GifAnimation:
                        success = await LoadGifAnimationAsync(filePath);
                        break;
                    case VisualContentType.AsepriteAnimation:
                        success = await LoadAsepriteAnimationAsync(filePath);
                        break;
                    default:
                        _logService?.LogWarning("Unsupported file type: {FilePath}", filePath);
                        success = false;
                        break;
                }

                if (!success)
                {
                    _logService?.LogWarning("Failed to load visual content, using fallback image");
                    CurrentFrame = GetFallbackImage();
                    IsAnimated = false;
                    ContentType = VisualContentType.StaticImage;
                    success = true;
                }

                _logService?.LogMethodExit(nameof(LoadBallVisualAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading visual content from: {FilePath}", filePath);
                CurrentFrame = GetFallbackImage();
                IsAnimated = false;
                ContentType = VisualContentType.StaticImage;
                _logService?.LogMethodExit(nameof(LoadBallVisualAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Starts animation playback if the current content is animated
        /// </summary>
        public void StartAnimation()
        {
            _logService?.LogMethodEntry(nameof(StartAnimation));

            if (IsAnimated && _animationEngine != null)
            {
                _animationEngine.Play();
                _logService?.LogDebug("Animation started");
            }
            else
            {
                _logService?.LogDebug("Animation not started - content is not animated or animation engine is null");
            }

            _logService?.LogMethodExit(nameof(StartAnimation));
        }

        /// <summary>
        /// Stops animation playback if the current content is animated
        /// </summary>
        public void StopAnimation()
        {
            _logService?.LogMethodEntry(nameof(StopAnimation));

            if (IsAnimated && _animationEngine != null)
            {
                _animationEngine.Stop();
                _logService?.LogDebug("Animation stopped");
            }
            else
            {
                _logService?.LogDebug("Animation not stopped - content is not animated or animation engine is null");
            }

            _logService?.LogMethodExit(nameof(StopAnimation));
        }

        /// <summary>
        /// Updates the current frame for animated content
        /// </summary>
        public void UpdateFrame()
        {
            _logService?.LogMethodEntry(nameof(UpdateFrame));

            if (IsAnimated && _animationEngine != null)
            {
                // Update the animation engine
                _animationEngine.Update();
                
                // Get the current frame
                var currentFrame = _animationEngine.GetCurrentFrame();
                if (currentFrame != null)
                {
                    CurrentFrame = currentFrame.Image;
                    FrameDuration = currentFrame.Duration;
                }
            }

            _logService?.LogMethodExit(nameof(UpdateFrame));
        }

        /// <summary>
        /// Gets a fallback image when content cannot be loaded
        /// </summary>
        /// <returns>ImageSource representing a fallback ball image</returns>
        public ImageSource GetFallbackImage()
        {
            _logService?.LogMethodEntry(nameof(GetFallbackImage));

            try
            {
                var fallbackImage = CreateFallbackImage(25, Colors.Orange, Colors.DarkOrange, 2, _logService);
                _logService?.LogMethodExit(nameof(GetFallbackImage), "ImageSource");
                return fallbackImage;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error creating fallback image");
                _logService?.LogMethodExit(nameof(GetFallbackImage), null);
                return null;
            }
        }

        /// <summary>
        /// Switches visual content while preserving animation state and playback position
        /// </summary>
        /// <param name="filePath">Path to the new visual content file</param>
        /// <param name="preservePlaybackState">Whether to preserve current animation playback state</param>
        /// <returns>True if switching was successful, false otherwise</returns>
        public async Task<bool> SwitchVisualContentAsync(string filePath, bool preservePlaybackState = true)
        {
            _logService?.LogMethodEntry(nameof(SwitchVisualContentAsync), filePath, preservePlaybackState);

            try
            {
                // Store current state for preservation
                var wasPlaying = _animationEngine?.IsPlaying ?? false;
                var currentFrameIndex = _animationEngine?.CurrentFrameIndex ?? 0;
                var previousContentType = ContentType;

                _logService?.LogDebug("Switching visual content. Previous state - Type: {ContentType}, Playing: {IsPlaying}, Frame: {FrameIndex}", 
                    previousContentType, wasPlaying, currentFrameIndex);

                // Stop current animation to prevent conflicts
                if (wasPlaying)
                {
                    StopAnimation();
                }

                // Load the new visual content
                bool success = await LoadBallVisualAsync(filePath);

                if (success && preservePlaybackState)
                {
                    // Handle state preservation based on content type transition
                    if (previousContentType != VisualContentType.StaticImage && IsAnimated)
                    {
                        // Both old and new content are animated - try to preserve playback position
                        if (_animationEngine != null && _animationEngine.Frames.Count > 0)
                        {
                            // Set frame index to a safe value within the new animation's range
                            var safeFrameIndex = Math.Min(currentFrameIndex, _animationEngine.Frames.Count - 1);
                            _animationEngine.SetCurrentFrame(safeFrameIndex);
                            
                            _logService?.LogDebug("Preserved animation frame position: {FrameIndex} (adjusted from {OriginalIndex})", 
                                safeFrameIndex, currentFrameIndex);
                        }

                        // Restore animation playback if it was running
                        if (wasPlaying)
                        {
                            StartAnimation();
                            _logService?.LogDebug("Restored animation playback state");
                        }
                    }
                    else if (previousContentType == VisualContentType.StaticImage && IsAnimated)
                    {
                        // Transition from static to animated - start animation if previous state was "playing"
                        if (wasPlaying)
                        {
                            StartAnimation();
                            _logService?.LogDebug("Started animation for static-to-animated transition");
                        }
                    }
                    // For animated-to-static transitions, no special handling needed as animation is naturally stopped
                }

                if (success)
                {
                    _logService?.LogInformation("Visual content switched successfully: {FilePath} (Type: {ContentType}, Animated: {IsAnimated})", 
                        filePath, ContentType, IsAnimated);
                }
                else
                {
                    _logService?.LogWarning("Failed to switch visual content: {FilePath}", filePath);
                }

                _logService?.LogMethodExit(nameof(SwitchVisualContentAsync), success);
                return success;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error switching visual content: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(SwitchVisualContentAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Detects the type of visual content based on file extension and content
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>The detected visual content type</returns>
        public VisualContentType DetectFileType(string filePath)
        {
            _logService?.LogMethodEntry(nameof(DetectFileType), filePath);

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.Unknown);
                    return VisualContentType.Unknown;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var directory = Path.GetDirectoryName(filePath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                // Check for Aseprite export (PNG with accompanying JSON)
                if (extension == ".png")
                {
                    var jsonPath = Path.Combine(directory, fileNameWithoutExtension + ".json");
                    if (File.Exists(jsonPath))
                    {
                        _logService?.LogDebug("Detected Aseprite animation: {FilePath}", filePath);
                        _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.AsepriteAnimation);
                        return VisualContentType.AsepriteAnimation;
                    }
                }

                // Check for GIF animation
                if (extension == ".gif")
                {
                    _logService?.LogDebug("Detected GIF animation: {FilePath}", filePath);
                    _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.GifAnimation);
                    return VisualContentType.GifAnimation;
                }

                // Check for static images
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp")
                {
                    _logService?.LogDebug("Detected static image: {FilePath}", filePath);
                    _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.StaticImage);
                    return VisualContentType.StaticImage;
                }

                _logService?.LogDebug("Unknown file type: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.Unknown);
                return VisualContentType.Unknown;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error detecting file type for: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(DetectFileType), VisualContentType.Unknown);
                return VisualContentType.Unknown;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Loads a static image from the specified path
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <returns>True if loading was successful, false otherwise</returns>
        private async Task<bool> LoadStaticImageAsync(string filePath)
        {
            _logService?.LogMethodEntry(nameof(LoadStaticImageAsync), filePath);

            try
            {
                var imageSource = await Task.Run(() => LoadImage(filePath, _logService));
                if (imageSource != null)
                {
                    CurrentFrame = imageSource;
                    IsAnimated = false;
                    ContentType = VisualContentType.StaticImage;
                    FrameDuration = TimeSpan.Zero;

                    _logService?.LogInformation("Static image loaded successfully: {FilePath}", filePath);
                    _logService?.LogMethodExit(nameof(LoadStaticImageAsync), true);
                    return true;
                }

                _logService?.LogMethodExit(nameof(LoadStaticImageAsync), false);
                return false;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading static image: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(LoadStaticImageAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Loads a GIF animation from the specified path
        /// </summary>
        /// <param name="filePath">Path to the GIF file</param>
        /// <returns>True if loading was successful, false otherwise</returns>
        private async Task<bool> LoadGifAnimationAsync(string filePath)
        {
            _logService?.LogMethodEntry(nameof(LoadGifAnimationAsync), filePath);

            try
            {
                var gifData = await Task.Run(() => ExtractGifData(filePath, _logService));
                if (gifData != null && gifData.Frames != null && gifData.Frames.Count > 0)
                {
                    _animationEngine.LoadFrames(gifData.Frames);
                    
                    // Set GIF-specific loop behavior
                    _animationEngine.LoopCount = gifData.LoopCount;
                    _animationEngine.IsLooping = gifData.LoopCount != -1; // -1 means no looping
                    
                    // Set the first frame as current
                    var firstFrame = _animationEngine.GetCurrentFrame();
                    if (firstFrame != null)
                    {
                        CurrentFrame = firstFrame.Image;
                        FrameDuration = firstFrame.Duration;
                    }
                    
                    IsAnimated = gifData.Frames.Count > 1;
                    ContentType = VisualContentType.GifAnimation;

                    _logService?.LogInformation("GIF animation loaded successfully: {FilePath} ({FrameCount} frames, loop count: {LoopCount})", 
                        filePath, gifData.Frames.Count, gifData.LoopCount);
                    _logService?.LogMethodExit(nameof(LoadGifAnimationAsync), true);
                    return true;
                }

                _logService?.LogMethodExit(nameof(LoadGifAnimationAsync), false);
                return false;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error loading GIF animation: {FilePath}", filePath);
                _logService?.LogMethodExit(nameof(LoadGifAnimationAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Loads an Aseprite animation from the specified PNG and JSON files
        /// </summary>
        /// <param name="pngPath">Path to the PNG sprite sheet</param>
        /// <returns>True if loading was successful, false otherwise</returns>
        private async Task<bool> LoadAsepriteAnimationAsync(string pngPath)
        {
            _logService?.LogMethodEntry(nameof(LoadAsepriteAnimationAsync), pngPath);

            try
            {
                var directory = Path.GetDirectoryName(pngPath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pngPath);
                var jsonPath = Path.Combine(directory, fileNameWithoutExtension + ".json");

                if (!File.Exists(jsonPath))
                {
                    _logService?.LogWarning("JSON metadata file not found: {JsonPath}. Aseprite animations require both PNG and JSON files. Attempting to load as static image instead.", jsonPath);
                    
                    // Fallback: try to load as static image
                    var staticImage = await Task.Run(() => LoadImage(pngPath, _logService));
                    if (staticImage != null)
                    {
                        CurrentFrame = staticImage;
                        IsAnimated = false;
                        ContentType = VisualContentType.StaticImage;
                        FrameDuration = TimeSpan.Zero;
                        _logService?.LogInformation("Loaded PNG as static image instead of Aseprite animation: {PngPath}", pngPath);
                        _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                        return true;
                    }
                    
                    _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), false);
                    return false;
                }

                // Load the sprite sheet image
                var spriteSheet = await Task.Run(() => LoadImage(pngPath, _logService));
                if (spriteSheet == null)
                {
                    _logService?.LogWarning("Failed to load sprite sheet: {PngPath}. The PNG file may be corrupted or in an unsupported format.", pngPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), false);
                    return false;
                }

                // Load Aseprite data using AsepriteLoader
                var asepriteData = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);
                if (asepriteData == null)
                {
                    _logService?.LogWarning("Failed to load Aseprite data from: {JsonPath}. The JSON file may be corrupted, invalid, or not a proper Aseprite export. Attempting to load PNG as static image.", jsonPath);
                    
                    // Fallback: try to load as static image
                    CurrentFrame = spriteSheet;
                    IsAnimated = false;
                    ContentType = VisualContentType.StaticImage;
                    FrameDuration = TimeSpan.Zero;
                    _logService?.LogInformation("Loaded PNG as static image due to invalid JSON metadata: {PngPath}", pngPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                    return true;
                }

                // Convert Aseprite data to animation frames
                var animationFrames = _asepriteLoader.ConvertToAnimationFrames(asepriteData, spriteSheet);
                if (animationFrames == null || animationFrames.Count == 0)
                {
                    _logService?.LogWarning("No animation frames extracted from Aseprite data. The frame data may be invalid or the sprite sheet dimensions may not match the JSON metadata. Using sprite sheet as static image.");
                    
                    // Fallback: use the sprite sheet as a static image
                    CurrentFrame = spriteSheet;
                    IsAnimated = false;
                    ContentType = VisualContentType.StaticImage;
                    FrameDuration = TimeSpan.Zero;
                    _logService?.LogInformation("Loaded sprite sheet as static image due to frame extraction failure: {PngPath}", pngPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                    return true;
                }

                // Handle multiple animation sequences - use default/first sequence
                var framesToUse = GetDefaultAnimationSequence(asepriteData, animationFrames);
                if (framesToUse == null || framesToUse.Count == 0)
                {
                    _logService?.LogWarning("No frames available for animation sequence. Using sprite sheet as static image.");
                    
                    // Fallback: use the sprite sheet as a static image
                    CurrentFrame = spriteSheet;
                    IsAnimated = false;
                    ContentType = VisualContentType.StaticImage;
                    FrameDuration = TimeSpan.Zero;
                    _logService?.LogInformation("Loaded sprite sheet as static image due to empty animation sequence: {PngPath}", pngPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                    return true;
                }

                // Load frames into animation engine
                _animationEngine.LoadFrames(framesToUse);
                
                // Set animation properties
                _animationEngine.IsLooping = true; // Aseprite animations typically loop
                _animationEngine.LoopCount = 0; // Infinite loop by default
                
                // Set the first frame as current
                var firstFrame = _animationEngine.GetCurrentFrame();
                if (firstFrame != null)
                {
                    CurrentFrame = firstFrame.Image;
                    FrameDuration = firstFrame.Duration;
                }
                else
                {
                    _logService?.LogWarning("No current frame available from animation engine. Using sprite sheet as static image.");
                    CurrentFrame = spriteSheet;
                    FrameDuration = TimeSpan.Zero;
                }
                
                IsAnimated = framesToUse.Count > 1;
                ContentType = VisualContentType.AsepriteAnimation;

                _logService?.LogInformation("Aseprite animation loaded successfully: {PngPath} ({FrameCount} frames, {TagCount} tags)", 
                    pngPath, animationFrames.Count, asepriteData.FrameTags?.Count ?? 0);
                _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                return true;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Unexpected error loading Aseprite animation: {PngPath}. Will attempt fallback to static image.", pngPath);
                
                // Final fallback: try to load PNG as static image
                try
                {
                    var fallbackImage = await Task.Run(() => LoadImage(pngPath, _logService));
                    if (fallbackImage != null)
                    {
                        CurrentFrame = fallbackImage;
                        IsAnimated = false;
                        ContentType = VisualContentType.StaticImage;
                        FrameDuration = TimeSpan.Zero;
                        _logService?.LogInformation("Loaded PNG as static image after error: {PngPath}", pngPath);
                        _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), true);
                        return true;
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logService?.LogError(fallbackEx, "Fallback to static image also failed for: {PngPath}", pngPath);
                }
                
                _logService?.LogMethodExit(nameof(LoadAsepriteAnimationAsync), false);
                return false;
            }
        }

        /// <summary>
        /// Extracts complete GIF data including frames, timing, and loop information
        /// </summary>
        /// <param name="gifPath">Path to the GIF file</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>GifData containing frames and metadata, or null if extraction failed</returns>
        private static GifData ExtractGifData(string gifPath, ILogService logService = null)
        {
            logService?.LogMethodEntry(nameof(ExtractGifData), gifPath);

            try
            {
                var gifData = new GifData();
                var totalDuration = TimeSpan.Zero;
                
                // Load the GIF using GifBitmapDecoder
                using (var fileStream = new FileStream(gifPath, FileMode.Open, FileAccess.Read))
                {
                    var decoder = new GifBitmapDecoder(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    
                    logService?.LogDebug("GIF decoder created, frame count: {FrameCount}", decoder.Frames.Count);
                    
                    // Check for global loop count in GIF metadata
                    var globalMetadata = decoder.Metadata as BitmapMetadata;
                    gifData.LoopCount = GetGifLoopCount(globalMetadata, logService);
                    
                    logService?.LogDebug("GIF loop count: {LoopCount} (0 = infinite)", gifData.LoopCount);
                    
                    for (int i = 0; i < decoder.Frames.Count; i++)
                    {
                        var frame = decoder.Frames[i];
                        
                        // Get frame delay from metadata
                        var delayMetadata = frame.Metadata as BitmapMetadata;
                        var delay = GetGifFrameDelay(delayMetadata, logService);
                        
                        // Create a copy of the frame that can be frozen
                        var writableBitmap = new WriteableBitmap(frame);
                        writableBitmap.Freeze();
                        
                        var animationFrame = new AnimationFrame(writableBitmap, delay);
                        gifData.Frames.Add(animationFrame);
                        totalDuration = totalDuration.Add(delay);
                        
                        logService?.LogDebug("Extracted GIF frame {FrameIndex}: {Width}x{Height}, delay: {Delay}ms", 
                            i, frame.PixelWidth, frame.PixelHeight, delay.TotalMilliseconds);
                    }
                }
                
                gifData.TotalDuration = totalDuration;
                
                // Handle single frame GIFs
                if (gifData.Frames.Count == 1)
                {
                    logService?.LogDebug("Single frame GIF detected, treating as static image");
                    gifData.LoopCount = -1; // No looping for single frame
                }
                
                logService?.LogInformation("Successfully extracted GIF data: {FrameCount} frames, {TotalDuration}ms total, loop count: {LoopCount}", 
                    gifData.Frames.Count, gifData.TotalDuration.TotalMilliseconds, gifData.LoopCount);
                logService?.LogMethodExit(nameof(ExtractGifData), "GifData");
                return gifData;
            }
            catch (Exception ex)
            {
                logService?.LogError(ex, "Error extracting GIF data from: {GifPath}", gifPath);
                logService?.LogMethodExit(nameof(ExtractGifData), null);
                return null;
            }
        }

        /// <summary>
        /// Extracts frames from a GIF file (legacy method for backward compatibility)
        /// </summary>
        /// <param name="gifPath">Path to the GIF file</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>List of animation frames extracted from the GIF</returns>
        private static List<AnimationFrame> ExtractGifFrames(string gifPath, ILogService logService = null)
        {
            var gifData = ExtractGifData(gifPath, logService);
            return gifData?.Frames;
        }

        /// <summary>
        /// Gets the frame delay from GIF metadata
        /// </summary>
        /// <param name="metadata">Bitmap metadata from the GIF frame</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>TimeSpan representing the frame delay</returns>
        private static TimeSpan GetGifFrameDelay(BitmapMetadata metadata, ILogService logService = null)
        {
            try
            {
                // Default delay if metadata is not available
                var defaultDelay = TimeSpan.FromMilliseconds(100);
                
                if (metadata == null)
                {
                    logService?.LogDebug("No metadata available, using default delay: {Delay}ms", defaultDelay.TotalMilliseconds);
                    return defaultDelay;
                }
                
                // Try to get the delay from GIF metadata
                // GIF delay is stored in 1/100th of a second units
                if (metadata.ContainsQuery("/grctlext/Delay"))
                {
                    var delayValue = metadata.GetQuery("/grctlext/Delay");
                    if (delayValue is ushort delay)
                    {
                        // Convert from 1/100th seconds to milliseconds
                        var delayMs = delay * 10;
                        
                        // GIF standard specifies minimum delay of 20ms for very fast animations
                        if (delayMs < 20)
                        {
                            delayMs = 100; // Use 100ms as default for very fast or zero delays
                        }
                        
                        var frameDelay = TimeSpan.FromMilliseconds(delayMs);
                        logService?.LogDebug("Frame delay from metadata: {Delay}ms", frameDelay.TotalMilliseconds);
                        return frameDelay;
                    }
                }
                
                logService?.LogDebug("Could not read delay from metadata, using default: {Delay}ms", defaultDelay.TotalMilliseconds);
                return defaultDelay;
            }
            catch (Exception ex)
            {
                logService?.LogWarning("Error reading GIF frame delay, using default: {Exception}", ex.Message);
                return TimeSpan.FromMilliseconds(100);
            }
        }

        /// <summary>
        /// Gets the loop count from GIF global metadata
        /// </summary>
        /// <param name="metadata">Global bitmap metadata from the GIF</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>Loop count (0 means infinite loop, -1 means no loop info found)</returns>
        private static int GetGifLoopCount(BitmapMetadata metadata, ILogService logService = null)
        {
            try
            {
                if (metadata == null)
                {
                    logService?.LogDebug("No global metadata available for loop count");
                    return 0; // Default to infinite loop
                }
                
                // Try to get the loop count from GIF application extension
                // The Netscape 2.0 extension stores loop count
                if (metadata.ContainsQuery("/appext/Application"))
                {
                    var appValue = metadata.GetQuery("/appext/Application");
                    if (appValue is byte[] appBytes && appBytes.Length >= 11)
                    {
                        // Check if this is the Netscape 2.0 extension
                        var netscapeId = System.Text.Encoding.ASCII.GetString(appBytes, 0, 11);
                        if (netscapeId == "NETSCAPE2.0")
                        {
                            // Try to get the data sub-block
                            if (metadata.ContainsQuery("/appext/Data"))
                            {
                                var dataValue = metadata.GetQuery("/appext/Data");
                                if (dataValue is byte[] dataBytes && dataBytes.Length >= 3)
                                {
                                    // Loop count is stored in bytes 1-2 (little endian)
                                    var loopCount = BitConverter.ToUInt16(dataBytes, 1);
                                    logService?.LogDebug("Loop count from Netscape extension: {LoopCount}", loopCount);
                                    return loopCount;
                                }
                            }
                        }
                    }
                }
                
                logService?.LogDebug("No loop count found in metadata, defaulting to infinite loop");
                return 0; // Default to infinite loop
            }
            catch (Exception ex)
            {
                logService?.LogWarning("Error reading GIF loop count, defaulting to infinite loop: {Exception}", ex.Message);
                return 0; // Default to infinite loop on error
            }
        }

        /// <summary>
        /// Gets the default animation sequence from Aseprite data
        /// If multiple animation tags exist, uses the first one; otherwise uses all frames
        /// </summary>
        /// <param name="asepriteData">The Aseprite data containing frame tags</param>
        /// <param name="allFrames">All available animation frames</param>
        /// <returns>List of frames for the default animation sequence</returns>
        private List<AnimationFrame> GetDefaultAnimationSequence(AsepriteData asepriteData, List<AnimationFrame> allFrames)
        {
            _logService?.LogMethodEntry(nameof(GetDefaultAnimationSequence), asepriteData?.FrameTags?.Count, allFrames?.Count);

            try
            {
                if (allFrames == null || allFrames.Count == 0)
                {
                    _logService?.LogWarning("No frames available for animation sequence");
                    _logService?.LogMethodExit(nameof(GetDefaultAnimationSequence), new List<AnimationFrame>());
                    return new List<AnimationFrame>();
                }

                // If no frame tags exist, use all frames
                if (asepriteData?.FrameTags == null || asepriteData.FrameTags.Count == 0)
                {
                    _logService?.LogDebug("No frame tags found, using all {FrameCount} frames", allFrames.Count);
                    _logService?.LogMethodExit(nameof(GetDefaultAnimationSequence), allFrames);
                    return allFrames;
                }

                // Use the first animation tag (default sequence)
                var defaultTag = asepriteData.FrameTags[0];
                _logService?.LogDebug("Using animation tag '{TagName}' (frames {From}-{To})", 
                    defaultTag.Name, defaultTag.From, defaultTag.To);

                // Validate frame indices
                var fromIndex = Math.Max(0, defaultTag.From);
                var toIndex = Math.Min(allFrames.Count - 1, defaultTag.To);

                if (fromIndex > toIndex || fromIndex >= allFrames.Count)
                {
                    _logService?.LogWarning("Invalid frame range in tag '{TagName}': {From}-{To}, using all frames", 
                        defaultTag.Name, defaultTag.From, defaultTag.To);
                    _logService?.LogMethodExit(nameof(GetDefaultAnimationSequence), allFrames);
                    return allFrames;
                }

                // Extract frames for the default sequence
                var sequenceFrames = new List<AnimationFrame>();
                for (int i = fromIndex; i <= toIndex; i++)
                {
                    sequenceFrames.Add(allFrames[i]);
                }

                _logService?.LogInformation("Selected animation sequence '{TagName}' with {FrameCount} frames", 
                    defaultTag.Name, sequenceFrames.Count);
                _logService?.LogMethodExit(nameof(GetDefaultAnimationSequence), sequenceFrames);
                return sequenceFrames;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error getting default animation sequence, using all frames");
                _logService?.LogMethodExit(nameof(GetDefaultAnimationSequence), allFrames ?? new List<AnimationFrame>());
                return allFrames ?? new List<AnimationFrame>();
            }
        }

        #endregion Private Methods

        #region Static Methods

        /// <summary>
        /// Loads an image from the specified path
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>ImageSource if successful, null if failed</returns>
        /// <exception cref="ArgumentNullException">Thrown when imagePath is null or empty</exception>
        public static ImageSource LoadImage(string imagePath, ILogService logService = null)
        {
            logService?.LogMethodEntry(nameof(LoadImage), imagePath);
            
            try
            {
                logService?.LogDebug("Loading image from path: {ImagePath}", imagePath);
                
                // Create a BitmapImage and set its properties
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load the image when created, not when displayed
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.EndInit();
                
                // Freeze the bitmap to make it thread-safe and improve performance
                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }
                
                logService?.LogInformation("Image loaded successfully: {ImagePath} ({Width}x{Height})", 
                    imagePath, bitmap.PixelWidth, bitmap.PixelHeight);
                logService?.LogMethodExit(nameof(LoadImage), "ImageSource");
                
                return bitmap;
            }
            catch (Exception ex)
            {
                // Log the error with structured logging
                logService?.LogError(ex, "Error loading image from path: {ImagePath}", imagePath);
                logService?.LogMethodExit(nameof(LoadImage), null);
                return null;
            }
        }
        
        /// <summary>
        /// Creates a fallback image (a simple circle) when the actual image cannot be loaded
        /// </summary>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="fillColor">Fill color of the circle</param>
        /// <param name="strokeColor">Stroke color of the circle</param>
        /// <param name="strokeThickness">Stroke thickness of the circle</param>
        /// <param name="logService">Optional logging service</param>
        /// <returns>ImageSource representing a circle</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when radius is less than or equal to zero</exception>
        public static ImageSource CreateFallbackImage(double radius, Color fillColor, Color strokeColor, double strokeThickness = 2, ILogService logService = null)
        {
            logService?.LogMethodEntry(nameof(CreateFallbackImage), radius, fillColor, strokeColor, strokeThickness);
            
            try
            {
                logService?.LogDebug("Creating fallback image: radius={Radius}, fill={FillColor}, stroke={StrokeColor}", 
                    radius, fillColor, strokeColor);
                
                // Handle edge cases
                if (radius <= 0)
                {
                    logService?.LogWarning("Invalid radius: {Radius}. Using minimum radius of 1.", radius);
                    radius = 1;
                }
                
                if (strokeThickness < 0)
                {
                    logService?.LogWarning("Invalid stroke thickness: {StrokeThickness}. Using 0.", strokeThickness);
                    strokeThickness = 0;
                }
                
                // Calculate the size of the drawing
                int size = Math.Max(1, (int)Math.Ceiling(radius * 2 + strokeThickness));
                
                // Create a DrawingVisual
                var drawingVisual = new DrawingVisual();
                
                // Get the DrawingContext
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    // Calculate effective radius accounting for stroke
                    double effectiveRadius = Math.Max(0.5, radius - strokeThickness / 2);
                    
                    // Draw a circle
                    drawingContext.DrawEllipse(
                        new SolidColorBrush(fillColor),
                        strokeThickness > 0 ? new Pen(new SolidColorBrush(strokeColor), strokeThickness) : null,
                        new System.Windows.Point(size / 2.0, size / 2.0),
                        effectiveRadius,
                        effectiveRadius);
                }
                
                // Create a RenderTargetBitmap to render the DrawingVisual
                var renderBitmap = new RenderTargetBitmap(
                    size, size,
                    96, 96, // DPI
                    PixelFormats.Pbgra32);
                
                // Render the DrawingVisual to the bitmap
                renderBitmap.Render(drawingVisual);
                
                // Freeze the bitmap to make it thread-safe and improve performance
                renderBitmap.Freeze();
                
                logService?.LogInformation("Fallback image created successfully: {Size}x{Size} pixels", size, size);
                logService?.LogMethodExit(nameof(CreateFallbackImage), "ImageSource");
                
                return renderBitmap;
            }
            catch (Exception ex)
            {
                logService?.LogError(ex, "Error creating fallback image with radius: {Radius}", radius);
                logService?.LogMethodExit(nameof(CreateFallbackImage), null);
                return null;
            }
        }

        #endregion Static Methods
    }
}
