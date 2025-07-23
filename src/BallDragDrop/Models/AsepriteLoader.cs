using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using BallDragDrop.Contracts;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents metadata for an Aseprite export
    /// </summary>
    public class AsepriteMetadata
    {
        /// <summary>
        /// Gets or sets the application name
        /// </summary>
        public string App { get; set; }

        /// <summary>
        /// Gets or sets the version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the image filename
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the format
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the size information
        /// </summary>
        public AsepriteSize Size { get; set; }

        /// <summary>
        /// Gets or sets the scale
        /// </summary>
        public string Scale { get; set; }
    }

    /// <summary>
    /// Represents size information for Aseprite export
    /// </summary>
    public class AsepriteSize
    {
        /// <summary>
        /// Gets or sets the width
        /// </summary>
        public int W { get; set; }

        /// <summary>
        /// Gets or sets the height
        /// </summary>
        public int H { get; set; }
    }

    /// <summary>
    /// Represents a frame in an Aseprite export
    /// </summary>
    public class AsepriteFrame
    {
        /// <summary>
        /// Gets or sets the frame rectangle
        /// </summary>
        public AsepriteRect Frame { get; set; }

        /// <summary>
        /// Gets or sets whether the frame is rotated
        /// </summary>
        public bool Rotated { get; set; }

        /// <summary>
        /// Gets or sets whether the frame is trimmed
        /// </summary>
        public bool Trimmed { get; set; }

        /// <summary>
        /// Gets or sets the sprite source size
        /// </summary>
        public AsepriteRect SpriteSourceSize { get; set; }

        /// <summary>
        /// Gets or sets the source size
        /// </summary>
        public AsepriteSize SourceSize { get; set; }

        /// <summary>
        /// Gets or sets the frame duration in milliseconds
        /// </summary>
        public int Duration { get; set; }
    }

    /// <summary>
    /// Represents a rectangle in Aseprite format
    /// </summary>
    public class AsepriteRect
    {
        /// <summary>
        /// Gets or sets the X coordinate
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets the width
        /// </summary>
        public int W { get; set; }

        /// <summary>
        /// Gets or sets the height
        /// </summary>
        public int H { get; set; }
    }

    /// <summary>
    /// Represents an animation tag in Aseprite export
    /// </summary>
    public class AsepriteTag
    {
        /// <summary>
        /// Gets or sets the tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the starting frame index
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// Gets or sets the ending frame index
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// Gets or sets the animation direction
        /// </summary>
        public string Direction { get; set; }
    }

    /// <summary>
    /// Represents the complete Aseprite export data
    /// </summary>
    public class AsepriteData
    {
        /// <summary>
        /// Gets or sets the frames dictionary
        /// </summary>
        public Dictionary<string, AsepriteFrame> Frames { get; set; }

        /// <summary>
        /// Gets or sets the metadata
        /// </summary>
        public AsepriteMetadata Meta { get; set; }

        /// <summary>
        /// Gets or sets the animation tags
        /// </summary>
        public List<AsepriteTag> FrameTags { get; set; }

        /// <summary>
        /// Initializes a new instance of the AsepriteData class
        /// </summary>
        public AsepriteData()
        {
            Frames = new Dictionary<string, AsepriteFrame>();
            FrameTags = new List<AsepriteTag>();
        }
    }

    /// <summary>
    /// Represents the result of Aseprite data validation
    /// </summary>
    public class AsepriteValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>A validation result indicating success</returns>
        public static AsepriteValidationResult Success()
        {
            return new AsepriteValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with an error message
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A validation result indicating failure</returns>
        public static AsepriteValidationResult Failure(string errorMessage)
        {
            return new AsepriteValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }

    /// <summary>
    /// Loader for Aseprite PNG+JSON exports
    /// </summary>
    public class AsepriteLoader
    {
        #region Fields

        private readonly ILogService _logService;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AsepriteLoader class
        /// </summary>
        /// <param name="logService">Optional logging service</param>
        public AsepriteLoader(ILogService logService = null)
        {
            _logService = logService;
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Loads Aseprite data from PNG and JSON files
        /// </summary>
        /// <param name="pngPath">Path to the PNG sprite sheet</param>
        /// <param name="jsonPath">Path to the JSON metadata file</param>
        /// <returns>The loaded Aseprite data, or null if loading failed</returns>
        public async Task<AsepriteData> LoadAsepriteAsync(string pngPath, string jsonPath)
        {
            _logService?.LogMethodEntry(nameof(LoadAsepriteAsync), pngPath, jsonPath);

            try
            {
                if (string.IsNullOrWhiteSpace(pngPath) || string.IsNullOrWhiteSpace(jsonPath))
                {
                    _logService?.LogWarning("PNG or JSON path is null or empty");
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                if (!File.Exists(pngPath))
                {
                    _logService?.LogWarning("PNG file does not exist: {PngPath}", pngPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                if (!File.Exists(jsonPath))
                {
                    _logService?.LogWarning("JSON metadata file does not exist: {JsonPath}. Aseprite exports require both PNG and JSON files.", jsonPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                // Load and validate JSON content
                string jsonContent;
                try
                {
                    jsonContent = await File.ReadAllTextAsync(jsonPath);
                    if (string.IsNullOrWhiteSpace(jsonContent))
                    {
                        _logService?.LogWarning("JSON metadata file is empty: {JsonPath}", jsonPath);
                        _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                        return null;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logService?.LogError(ex, "Access denied reading JSON metadata file: {JsonPath}", jsonPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }
                catch (IOException ex)
                {
                    _logService?.LogError(ex, "IO error reading JSON metadata file: {JsonPath}", jsonPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                // Parse JSON with enhanced error handling
                AsepriteData asepriteData;
                try
                {
                    asepriteData = JsonSerializer.Deserialize<AsepriteData>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logService?.LogError(ex, "Invalid JSON format in metadata file: {JsonPath}. The file may be corrupted or not a valid Aseprite export.", jsonPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                if (asepriteData == null)
                {
                    _logService?.LogWarning("Failed to deserialize Aseprite JSON data from: {JsonPath}. The JSON structure may not match expected Aseprite format.", jsonPath);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                // Validate the loaded data structure
                var validationResult = ValidateAsepriteData(asepriteData, jsonPath);
                if (!validationResult.IsValid)
                {
                    _logService?.LogWarning("Aseprite data validation failed: {ValidationError}", validationResult.ErrorMessage);
                    _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                    return null;
                }

                _logService?.LogInformation("Aseprite data loaded successfully: {FrameCount} frames, {TagCount} tags",
                    asepriteData.Frames?.Count ?? 0, asepriteData.FrameTags?.Count ?? 0);

                _logService?.LogMethodExit(nameof(LoadAsepriteAsync), "AsepriteData");
                return asepriteData;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Unexpected error loading Aseprite data from {PngPath} and {JsonPath}", pngPath, jsonPath);
                _logService?.LogMethodExit(nameof(LoadAsepriteAsync), null);
                return null;
            }
        }

        /// <summary>
        /// Converts Aseprite data to animation frames
        /// </summary>
        /// <param name="data">The Aseprite data</param>
        /// <param name="spriteSheet">The loaded sprite sheet image</param>
        /// <returns>List of animation frames</returns>
        public List<AnimationFrame> ConvertToAnimationFrames(AsepriteData data, ImageSource spriteSheet)
        {
            _logService?.LogMethodEntry(nameof(ConvertToAnimationFrames), data?.Frames?.Count, spriteSheet != null);

            try
            {
                var frames = new List<AnimationFrame>();

                if (data?.Frames == null || spriteSheet == null)
                {
                    _logService?.LogWarning("Invalid Aseprite data or sprite sheet");
                    _logService?.LogMethodExit(nameof(ConvertToAnimationFrames), frames);
                    return frames;
                }

                foreach (var frameEntry in data.Frames)
                {
                    var frameData = frameEntry.Value;
                    if (frameData?.Frame == null)
                        continue;

                    // Create source rectangle
                    var sourceRect = new System.Windows.Rect(
                        frameData.Frame.X,
                        frameData.Frame.Y,
                        frameData.Frame.W,
                        frameData.Frame.H);

                    // Extract frame image from sprite sheet
                    var frameImage = ExtractFrame(spriteSheet, sourceRect);
                    if (frameImage == null)
                        continue;

                    // Create animation frame
                    var duration = TimeSpan.FromMilliseconds(frameData.Duration > 0 ? frameData.Duration : 100);
                    var animationFrame = new AnimationFrame(frameImage, duration, sourceRect);

                    frames.Add(animationFrame);
                }

                _logService?.LogInformation("Converted {FrameCount} Aseprite frames to animation frames", frames.Count);
                _logService?.LogMethodExit(nameof(ConvertToAnimationFrames), frames);
                return frames;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error converting Aseprite data to animation frames");
                _logService?.LogMethodExit(nameof(ConvertToAnimationFrames), new List<AnimationFrame>());
                return new List<AnimationFrame>();
            }
        }

        /// <summary>
        /// Extracts a frame from a sprite sheet
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet image</param>
        /// <param name="sourceRect">The source rectangle to extract</param>
        /// <returns>The extracted frame image</returns>
        public ImageSource ExtractFrame(ImageSource spriteSheet, System.Windows.Rect sourceRect)
        {
            _logService?.LogMethodEntry(nameof(ExtractFrame), spriteSheet != null, sourceRect);

            try
            {
                if (spriteSheet == null)
                {
                    _logService?.LogWarning("Sprite sheet is null");
                    _logService?.LogMethodExit(nameof(ExtractFrame), null);
                    return null;
                }

                // Convert ImageSource to BitmapSource if needed
                if (!(spriteSheet is BitmapSource bitmapSource))
                {
                    _logService?.LogWarning("Sprite sheet is not a BitmapSource");
                    _logService?.LogMethodExit(nameof(ExtractFrame), null);
                    return null;
                }

                // Validate source rectangle
                if (sourceRect.Width <= 0 || sourceRect.Height <= 0 ||
                    sourceRect.X < 0 || sourceRect.Y < 0 ||
                    sourceRect.X + sourceRect.Width > bitmapSource.PixelWidth ||
                    sourceRect.Y + sourceRect.Height > bitmapSource.PixelHeight)
                {
                    _logService?.LogWarning("Invalid source rectangle: {SourceRect}", sourceRect);
                    _logService?.LogMethodExit(nameof(ExtractFrame), null);
                    return null;
                }

                // Create cropped bitmap
                var croppedBitmap = new CroppedBitmap(bitmapSource, new System.Windows.Int32Rect(
                    (int)sourceRect.X,
                    (int)sourceRect.Y,
                    (int)sourceRect.Width,
                    (int)sourceRect.Height));

                // Freeze for performance
                if (croppedBitmap.CanFreeze)
                {
                    croppedBitmap.Freeze();
                }

                _logService?.LogDebug("Frame extracted successfully: {Width}x{Height} at ({X},{Y})",
                    sourceRect.Width, sourceRect.Height, sourceRect.X, sourceRect.Y);

                _logService?.LogMethodExit(nameof(ExtractFrame), "ImageSource");
                return croppedBitmap;
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "Error extracting frame from sprite sheet");
                _logService?.LogMethodExit(nameof(ExtractFrame), null);
                return null;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Validates the loaded Aseprite data for structural integrity
        /// </summary>
        /// <param name="data">The Aseprite data to validate</param>
        /// <param name="jsonPath">The path to the JSON file for error reporting</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        private AsepriteValidationResult ValidateAsepriteData(AsepriteData data, string jsonPath)
        {
            _logService?.LogMethodEntry(nameof(ValidateAsepriteData), data != null, jsonPath);

            try
            {
                if (data == null)
                {
                    var error = "Aseprite data is null";
                    _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                    return AsepriteValidationResult.Failure(error);
                }

                // Validate frames collection
                if (data.Frames == null)
                {
                    var error = $"Frames collection is null in {jsonPath}. Expected a dictionary of frame data.";
                    _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                    return AsepriteValidationResult.Failure(error);
                }

                if (data.Frames.Count == 0)
                {
                    var error = $"No frames found in {jsonPath}. Aseprite exports must contain at least one frame.";
                    _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                    return AsepriteValidationResult.Failure(error);
                }

                // Validate individual frames
                foreach (var frameEntry in data.Frames)
                {
                    if (string.IsNullOrWhiteSpace(frameEntry.Key))
                    {
                        var error = $"Frame with empty or null key found in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }

                    var frame = frameEntry.Value;
                    if (frame == null)
                    {
                        var error = $"Frame '{frameEntry.Key}' has null data in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }

                    if (frame.Frame == null)
                    {
                        var error = $"Frame '{frameEntry.Key}' has null frame rectangle in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }

                    // Validate frame dimensions
                    if (frame.Frame.W <= 0 || frame.Frame.H <= 0)
                    {
                        var error = $"Frame '{frameEntry.Key}' has invalid dimensions ({frame.Frame.W}x{frame.Frame.H}) in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }

                    // Validate frame position (should not be negative)
                    if (frame.Frame.X < 0 || frame.Frame.Y < 0)
                    {
                        var error = $"Frame '{frameEntry.Key}' has negative position ({frame.Frame.X}, {frame.Frame.Y}) in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }

                    // Validate frame duration (should be positive)
                    if (frame.Duration < 0)
                    {
                        _logService?.LogWarning("Frame '{FrameKey}' has negative duration ({Duration}ms), will use default duration", frameEntry.Key, frame.Duration);
                    }
                }

                // Validate metadata (optional but should be valid if present)
                if (data.Meta != null)
                {
                    if (data.Meta.Size != null && (data.Meta.Size.W <= 0 || data.Meta.Size.H <= 0))
                    {
                        var error = $"Invalid sprite sheet dimensions in metadata ({data.Meta.Size.W}x{data.Meta.Size.H}) in {jsonPath}";
                        _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                        return AsepriteValidationResult.Failure(error);
                    }
                }

                // Validate frame tags (optional but should be valid if present)
                if (data.FrameTags != null)
                {
                    var frameCount = data.Frames.Count;
                    foreach (var tag in data.FrameTags)
                    {
                        if (tag == null)
                        {
                            _logService?.LogWarning("Null frame tag found in {JsonPath}, skipping", jsonPath);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(tag.Name))
                        {
                            _logService?.LogWarning("Frame tag with empty name found in {JsonPath}", jsonPath);
                        }

                        if (tag.From < 0 || tag.To < 0 || tag.From >= frameCount || tag.To >= frameCount)
                        {
                            _logService?.LogWarning("Frame tag '{TagName}' has invalid range ({From}-{To}) for {FrameCount} frames in {JsonPath}", 
                                tag.Name ?? "unnamed", tag.From, tag.To, frameCount, jsonPath);
                        }

                        if (tag.From > tag.To)
                        {
                            _logService?.LogWarning("Frame tag '{TagName}' has invalid range where From ({From}) > To ({To}) in {JsonPath}", 
                                tag.Name ?? "unnamed", tag.From, tag.To, jsonPath);
                        }
                    }
                }

                _logService?.LogDebug("Aseprite data validation passed for {JsonPath}", jsonPath);
                _logService?.LogMethodExit(nameof(ValidateAsepriteData), "Success");
                return AsepriteValidationResult.Success();
            }
            catch (Exception ex)
            {
                var error = $"Error during Aseprite data validation: {ex.Message}";
                _logService?.LogError(ex, "Error validating Aseprite data from {JsonPath}", jsonPath);
                _logService?.LogMethodExit(nameof(ValidateAsepriteData), error);
                return AsepriteValidationResult.Failure(error);
            }
        }

        #endregion Private Methods
    }
}