using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Handles PNG image loading and conversion to WPF cursors
    /// </summary>
    public class CursorImageLoader
    {
        #region Fields

        /// <summary>
        /// Standard cursor size in pixels
        /// </summary>
        private const int CursorSize = 30;

        /// <summary>
        /// Log service for error reporting
        /// </summary>
        private readonly ILogService _logService;

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the CursorImageLoader class
        /// </summary>
        /// <param name="logService">Log service for error reporting</param>
        public CursorImageLoader(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        #endregion Construction

        #region LoadPngAsCursor

        /// <summary>
        /// Loads a PNG file and converts it to a WPF Cursor
        /// </summary>
        /// <param name="pngPath">Relative path to the PNG file from application directory</param>
        /// <returns>WPF Cursor object</returns>
        public Cursor LoadPngAsCursor(string pngPath)
        {
            return LoadPngAsCursorWithFallback(pngPath, Cursors.Arrow);
        }

        #endregion LoadPngAsCursor

        #region LoadPngAsCursorWithFallback

        /// <summary>
        /// Loads a PNG file and converts it to a WPF Cursor with comprehensive fallback handling
        /// </summary>
        /// <param name="pngPath">Relative path to the PNG file from application directory</param>
        /// <param name="fallbackCursor">Cursor to use if loading fails</param>
        /// <returns>WPF Cursor object</returns>
        public Cursor LoadPngAsCursorWithFallback(string pngPath, Cursor fallbackCursor)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logService.LogDebug("Starting PNG cursor load for path: {PngPath}", pngPath);

                // Validate input parameters
                if (fallbackCursor == null)
                {
                    _logService.LogWarning("Fallback cursor is null, using system arrow cursor");
                    fallbackCursor = Cursors.Arrow;
                }

                // Validate input path
                if (string.IsNullOrEmpty(pngPath))
                {
                    _logService.LogWarning("PNG path is null or empty, using fallback cursor");
                    return fallbackCursor;
                }

                if (pngPath.Length > 260) // Windows MAX_PATH limitation
                {
                    _logService.LogWarning("PNG path too long ({Length} characters): {PngPath}, using fallback cursor", 
                        pngPath.Length, pngPath);
                    return fallbackCursor;
                }

                // Resolve and validate path
                string fullPath;
                try
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pngPath);
                    fullPath = Path.GetFullPath(fullPath); // Normalize the path
                }
                catch (Exception pathEx)
                {
                    _logService.LogError(pathEx, "Invalid path format: {PngPath}, using fallback cursor", pngPath);
                    return fallbackCursor;
                }

                // Check file existence and accessibility
                if (!File.Exists(fullPath))
                {
                    _logService.LogWarning("PNG file not found: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Validate file accessibility
                try
                {
                    using (var testStream = File.OpenRead(fullPath))
                    {
                        // File is accessible
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logService.LogError(ex, "Access denied to PNG file: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (IOException ex)
                {
                    _logService.LogError(ex, "IO error accessing PNG file: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Validate file size (prevent loading extremely large files)
                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(fullPath);
                }
                catch (Exception fileInfoEx)
                {
                    _logService.LogError(fileInfoEx, "Error getting file info for: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                if (fileInfo.Length == 0)
                {
                    _logService.LogWarning("PNG file is empty: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                if (fileInfo.Length > 1024 * 1024) // 1MB limit
                {
                    _logService.LogWarning("PNG file too large ({Size} bytes): {FullPath}, using fallback cursor", 
                        fileInfo.Length, fullPath);
                    return fallbackCursor;
                }

                // Load the PNG image with error handling
                BitmapImage bitmap;
                try
                {
                    bitmap = LoadBitmapWithValidation(fullPath);
                    if (bitmap == null)
                    {
                        _logService.LogError("LoadBitmapWithValidation returned null for: {FullPath}, using fallback cursor", fullPath);
                        return fallbackCursor;
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    _logService.LogError(ex, "Out of memory loading PNG bitmap: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (FileFormatException ex)
                {
                    _logService.LogError(ex, "Invalid PNG file format: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (NotSupportedException ex)
                {
                    _logService.LogError(ex, "Unsupported PNG format: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to load PNG bitmap: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Resize to standard cursor size
                BitmapImage resizedBitmap;
                try
                {
                    resizedBitmap = ResizeImage(bitmap);
                    if (resizedBitmap == null)
                    {
                        _logService.LogError("ResizeImage returned null for: {FullPath}, using fallback cursor", fullPath);
                        return fallbackCursor;
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    _logService.LogError(ex, "Out of memory resizing PNG image: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to resize PNG image: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Convert to cursor
                try
                {
                    var cursor = ConvertBitmapToCursor(resizedBitmap);
                    if (cursor == null)
                    {
                        _logService.LogError("ConvertBitmapToCursor returned null for: {FullPath}, using fallback cursor", fullPath);
                        return fallbackCursor;
                    }

                    var loadTime = DateTime.Now - startTime;
                    _logService.LogDebug("Successfully loaded PNG cursor from {FullPath} in {LoadTimeMs}ms", 
                        fullPath, loadTime.TotalMilliseconds);
                    
                    return cursor;
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to convert bitmap to cursor: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory during PNG cursor loading: {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error loading PNG as cursor: {PngPath}, using fallback cursor", pngPath);
                return fallbackCursor;
            }
        }

        #endregion LoadPngAsCursorWithFallback

        #region LoadBitmapWithValidation

        /// <summary>
        /// Loads a bitmap with comprehensive validation and error handling
        /// </summary>
        /// <param name="fullPath">Full path to the image file</param>
        /// <returns>Loaded bitmap image</returns>
        private BitmapImage LoadBitmapWithValidation(string fullPath)
        {
            BitmapImage? bitmap = null;
            
            try
            {
                _logService.LogDebug("Loading bitmap from: {FullPath}", fullPath);
                
                bitmap = new BitmapImage();
                
                try
                {
                    bitmap.BeginInit();
                    
                    // Set cache option to load the image data immediately
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    
                    // Set URI source
                    try
                    {
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    }
                    catch (UriFormatException ex)
                    {
                        throw new InvalidOperationException($"Invalid URI format for path: {fullPath}", ex);
                    }
                    
                    // Complete initialization
                    bitmap.EndInit();
                    
                    // Freeze for thread safety and performance
                    bitmap.Freeze();
                }
                catch (Exception initEx)
                {
                    // Clean up bitmap if initialization failed
                    try
                    {
                        bitmap?.Freeze();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                    throw new InvalidOperationException($"Failed to initialize bitmap from {fullPath}", initEx);
                }

                // Validate bitmap properties
                if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                {
                    throw new InvalidOperationException($"Invalid bitmap dimensions: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                }

                // Check for reasonable dimensions
                if (bitmap.PixelWidth > 2000 || bitmap.PixelHeight > 2000)
                {
                    _logService.LogWarning("Very large bitmap dimensions ({Width}x{Height}) for cursor: {Path}", 
                        bitmap.PixelWidth, bitmap.PixelHeight, fullPath);
                }
                else if (bitmap.PixelWidth > 1000 || bitmap.PixelHeight > 1000)
                {
                    _logService.LogWarning("Large bitmap dimensions ({Width}x{Height}) for cursor: {Path}", 
                        bitmap.PixelWidth, bitmap.PixelHeight, fullPath);
                }

                // Validate pixel format
                if (bitmap.Format == PixelFormats.Default)
                {
                    _logService.LogWarning("Bitmap has default pixel format: {Path}", fullPath);
                }
                else
                {
                    _logService.LogDebug("Loaded bitmap: {Width}x{Height}, Format: {Format}, DPI: {DpiX}x{DpiY}", 
                        bitmap.PixelWidth, bitmap.PixelHeight, bitmap.Format, bitmap.DpiX, bitmap.DpiY);
                }

                return bitmap;
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory loading bitmap from: {FullPath}", fullPath);
                throw new InvalidOperationException($"Out of memory loading bitmap from {fullPath}", ex);
            }
            catch (FileFormatException ex)
            {
                _logService.LogError(ex, "Invalid file format for bitmap: {FullPath}", fullPath);
                throw new InvalidOperationException($"Invalid file format for bitmap from {fullPath}", ex);
            }
            catch (NotSupportedException ex)
            {
                _logService.LogError(ex, "Unsupported bitmap format: {FullPath}", fullPath);
                throw new InvalidOperationException($"Unsupported bitmap format from {fullPath}", ex);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error loading bitmap from: {FullPath}", fullPath);
                
                // Ensure cleanup
                try
                {
                    bitmap?.Freeze();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                throw new InvalidOperationException($"Failed to load bitmap from {fullPath}", ex);
            }
        }

        #endregion LoadBitmapWithValidation

        #region ResizeImage

        /// <summary>
        /// Resizes an image to the standard cursor size (30x30) with comprehensive error handling
        /// </summary>
        /// <param name="source">Source bitmap image</param>
        /// <returns>Resized bitmap image</returns>
        private BitmapImage ResizeImage(BitmapImage source)
        {
            try
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source), "Source bitmap cannot be null");
                }

                _logService.LogDebug("Resizing image from {SourceWidth}x{SourceHeight} to {TargetSize}x{TargetSize}", 
                    source.PixelWidth, source.PixelHeight, CursorSize, CursorSize);

                // If already the correct size, return as-is
                if (source.PixelWidth == CursorSize && source.PixelHeight == CursorSize)
                {
                    _logService.LogDebug("Image already at target size, returning original");
                    return source;
                }

                // Validate source dimensions
                if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
                {
                    throw new InvalidOperationException($"Invalid source dimensions: {source.PixelWidth}x{source.PixelHeight}");
                }

                DrawingVisual? drawingVisual = null;
                RenderTargetBitmap? renderTargetBitmap = null;
                MemoryStream? stream = null;

                try
                {
                    // Create a DrawingVisual to render the resized image
                    drawingVisual = new DrawingVisual();
                    
                    using (var drawingContext = drawingVisual.RenderOpen())
                    {
                        try
                        {
                            drawingContext.DrawImage(source, new Rect(0, 0, CursorSize, CursorSize));
                        }
                        catch (Exception drawEx)
                        {
                            throw new InvalidOperationException("Failed to draw image to drawing context", drawEx);
                        }
                    }

                    // Render to a RenderTargetBitmap
                    try
                    {
                        renderTargetBitmap = new RenderTargetBitmap(
                            CursorSize, CursorSize, 96, 96, PixelFormats.Pbgra32);
                        renderTargetBitmap.Render(drawingVisual);
                        renderTargetBitmap.Freeze();
                    }
                    catch (Exception renderEx)
                    {
                        throw new InvalidOperationException("Failed to render image to bitmap", renderEx);
                    }

                    // Convert back to BitmapImage
                    try
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                        stream = new MemoryStream();
                        encoder.Save(stream);
                        stream.Position = 0;

                        var resizedBitmap = new BitmapImage();
                        resizedBitmap.BeginInit();
                        resizedBitmap.StreamSource = stream;
                        resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        resizedBitmap.EndInit();
                        resizedBitmap.Freeze();

                        _logService.LogDebug("Successfully resized image to {Width}x{Height}", 
                            resizedBitmap.PixelWidth, resizedBitmap.PixelHeight);

                        return resizedBitmap;
                    }
                    catch (Exception encodeEx)
                    {
                        throw new InvalidOperationException("Failed to encode resized image", encodeEx);
                    }
                }
                finally
                {
                    // Clean up resources
                    stream?.Dispose();
                }
            }
            catch (OutOfMemoryException ex)
            {
                _logService.LogError(ex, "Out of memory resizing image to {Size}x{Size}, returning original", CursorSize, CursorSize);
                return source; // Return original on memory error
            }
            catch (ArgumentNullException ex)
            {
                _logService.LogError(ex, "Null argument while resizing image to {Size}x{Size}", CursorSize, CursorSize);
                throw; // Re-throw null argument exceptions
            }
            catch (InvalidOperationException ex)
            {
                _logService.LogError(ex, "Invalid operation while resizing image to {Size}x{Size}, returning original", CursorSize, CursorSize);
                return source; // Return original on invalid operation
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error resizing image to {Size}x{Size}, returning original", CursorSize, CursorSize);
                return source; // Return original on any other error
            }
        }

        #endregion ResizeImage

        #region ConvertBitmapToCursor

        /// <summary>
        /// Converts a BitmapImage to a Cursor with comprehensive error handling
        /// </summary>
        /// <param name="bitmap">Source bitmap</param>
        /// <returns>WPF Cursor</returns>
        private Cursor ConvertBitmapToCursor(BitmapImage bitmap)
        {
            try
            {
                if (bitmap == null)
                {
                    _logService.LogWarning("Bitmap is null, using default cursor");
                    return Cursors.Arrow;
                }

                _logService.LogDebug("Converting bitmap to cursor: {Width}x{Height}", bitmap.PixelWidth, bitmap.PixelHeight);

                // Validate bitmap dimensions
                if (bitmap.PixelWidth != CursorSize || bitmap.PixelHeight != CursorSize)
                {
                    _logService.LogWarning("Bitmap dimensions ({Width}x{Height}) don't match expected cursor size ({ExpectedSize}x{ExpectedSize})", 
                        bitmap.PixelWidth, bitmap.PixelHeight, CursorSize, CursorSize);
                }

                // For WPF, we need to use a different approach to create custom cursors
                // Since the Cursor constructor with Point is not available in WPF,
                // we'll use the stream-based constructor
                if (bitmap.StreamSource != null)
                {
                    try
                    {
                        // Validate stream
                        if (!bitmap.StreamSource.CanRead)
                        {
                            _logService.LogWarning("Bitmap stream source is not readable, using default cursor");
                            return Cursors.Arrow;
                        }

                        if (!bitmap.StreamSource.CanSeek)
                        {
                            _logService.LogWarning("Bitmap stream source is not seekable, using default cursor");
                            return Cursors.Arrow;
                        }

                        // Reset stream position
                        bitmap.StreamSource.Position = 0;
                        
                        // Create cursor from stream
                        var cursor = new Cursor(bitmap.StreamSource);
                        
                        _logService.LogDebug("Successfully converted bitmap to cursor");
                        return cursor;
                    }
                    catch (ArgumentException ex)
                    {
                        _logService.LogError(ex, "Invalid argument converting bitmap to cursor, using default cursor");
                        return Cursors.Arrow;
                    }
                    catch (NotSupportedException ex)
                    {
                        _logService.LogError(ex, "Cursor format not supported, using default cursor");
                        return Cursors.Arrow;
                    }
                    catch (IOException ex)
                    {
                        _logService.LogError(ex, "IO error converting bitmap to cursor, using default cursor");
                        return Cursors.Arrow;
                    }
                    catch (OutOfMemoryException ex)
                    {
                        _logService.LogError(ex, "Out of memory converting bitmap to cursor, using default cursor");
                        return Cursors.Arrow;
                    }
                }
                else
                {
                    _logService.LogWarning("Bitmap stream source is null, attempting alternative conversion");
                    
                    // Try alternative conversion method using RenderTargetBitmap
                    try
                    {
                        return ConvertBitmapToCursorAlternative(bitmap);
                    }
                    catch (Exception altEx)
                    {
                        _logService.LogError(altEx, "Alternative bitmap to cursor conversion failed, using default cursor");
                        return Cursors.Arrow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error converting bitmap to cursor, using default cursor");
                return Cursors.Arrow;
            }
        }

        #endregion ConvertBitmapToCursor

        #region ConvertBitmapToCursorAlternative

        /// <summary>
        /// Alternative method to convert bitmap to cursor when stream source is not available
        /// </summary>
        /// <param name="bitmap">Source bitmap</param>
        /// <returns>WPF Cursor</returns>
        private Cursor ConvertBitmapToCursorAlternative(BitmapImage bitmap)
        {
            try
            {
                _logService.LogDebug("Using alternative bitmap to cursor conversion method");

                // Create a RenderTargetBitmap from the BitmapImage
                var renderBitmap = new RenderTargetBitmap(
                    bitmap.PixelWidth, bitmap.PixelHeight, 
                    bitmap.DpiX, bitmap.DpiY, 
                    PixelFormats.Pbgra32);

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                }

                renderBitmap.Render(drawingVisual);
                renderBitmap.Freeze();

                // Encode to PNG stream
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    // Create cursor from stream
                    return new Cursor(stream);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Alternative bitmap to cursor conversion failed");
                throw;
            }
        }

        #endregion ConvertBitmapToCursorAlternative
    }
}