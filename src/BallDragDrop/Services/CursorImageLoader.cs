using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
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
                _logService.LogInformation("CURSOR LOADING: Starting PNG cursor load for path: {PngPath}", pngPath);

                // Validate input parameters
                if (fallbackCursor == null)
                {
                    _logService.LogWarning("Fallback cursor is null, using system arrow cursor");
                    fallbackCursor = Cursors.Arrow;
                }

                // Validate input path
                if (string.IsNullOrEmpty(pngPath))
                {
                    _logService.LogWarning("CURSOR LOADING: PNG path is null or empty, using fallback cursor");
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
                    _logService.LogWarning("CURSOR LOADING: PNG file not found: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                
                _logService.LogInformation("CURSOR LOADING: PNG file found: {FullPath}", fullPath);

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

                _logService.LogInformation("CURSOR LOADING: File validation passed, size: {Size} bytes. Starting bitmap loading...", fileInfo.Length);

                // Load the PNG image with error handling
                BitmapImage bitmap;
                try
                {
                    _logService.LogInformation("CURSOR LOADING: Calling LoadBitmapWithValidation...");
                    bitmap = LoadBitmapWithValidation(fullPath);
                    if (bitmap == null)
                    {
                        _logService.LogError("CURSOR LOADING: LoadBitmapWithValidation returned null for: {FullPath}, using fallback cursor", fullPath);
                        return fallbackCursor;
                    }
                    _logService.LogInformation("CURSOR LOADING: Bitmap loaded successfully, proceeding to resize...");
                }
                catch (OutOfMemoryException ex)
                {
                    _logService.LogError(ex, "CURSOR LOADING: Out of memory loading PNG bitmap: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (FileFormatException ex)
                {
                    _logService.LogError(ex, "CURSOR LOADING: Invalid PNG file format: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (NotSupportedException ex)
                {
                    _logService.LogError(ex, "CURSOR LOADING: Unsupported PNG format: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "CURSOR LOADING: Failed to load PNG bitmap: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Resize to standard cursor size
                BitmapImage resizedBitmap;
                try
                {
                    _logService.LogInformation("CURSOR LOADING: Starting image resize...");
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
                    _logService.LogInformation("CURSOR LOADING: Starting cursor conversion...");
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
                _logService.LogInformation("CURSOR LOADING: LoadBitmapWithValidation - Loading bitmap from: {FullPath}", fullPath);
                
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

                // Convert PNG bitmap to cursor format
                try
                {
                    return CreateCursorFromBitmap(bitmap);
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to create cursor from bitmap, using default cursor");
                    return Cursors.Arrow;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Unexpected error converting bitmap to cursor, using default cursor");
                return Cursors.Arrow;
            }
        }

        #endregion ConvertBitmapToCursor

        #region CreateCursorFromBitmap

        /// <summary>
        /// Creates a cursor from a bitmap using a temporary file approach
        /// </summary>
        /// <param name="bitmap">Source bitmap</param>
        /// <returns>WPF Cursor</returns>
        private Cursor CreateCursorFromBitmap(BitmapImage bitmap)
        {
            try
            {
                _logService.LogDebug("Creating cursor from bitmap using temporary file approach");

                // Create a temporary cursor file
                var tempCursorFile = Path.GetTempFileName();
                tempCursorFile = Path.ChangeExtension(tempCursorFile, ".cur");

                try
                {
                    // Create cursor file data in memory
                    var cursorData = CreateCursorFileFromBitmap(bitmap);
                    
                    // Write to temporary file
                    File.WriteAllBytes(tempCursorFile, cursorData);
                    
                    // Create cursor from file
                    var cursor = new Cursor(tempCursorFile);
                    _logService.LogDebug("Successfully created cursor from bitmap using temporary file");
                    return cursor;
                }
                finally
                {
                    // Clean up temporary file
                    try
                    {
                        if (File.Exists(tempCursorFile))
                        {
                            File.Delete(tempCursorFile);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logService.LogWarning("Failed to clean up temporary cursor file: {TempFile}. Error: {Error}", 
                            tempCursorFile, cleanupEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to create cursor from bitmap, using hand cursor");
                return Cursors.Hand; // Use hand cursor as fallback to show it's working
            }
        }

        #endregion CreateCursorFromBitmap

        #region CreateCursorFileFromBitmap

        /// <summary>
        /// Creates cursor file data from a bitmap
        /// </summary>
        /// <param name="bitmap">Source bitmap</param>
        /// <returns>Cursor file data as byte array</returns>
        private byte[] CreateCursorFileFromBitmap(BitmapImage bitmap)
        {
            // Create a RenderTargetBitmap to ensure consistent format
            var renderBitmap = new RenderTargetBitmap(
                CursorSize, CursorSize, 96, 96, PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(bitmap, new Rect(0, 0, CursorSize, CursorSize));
            }

            renderBitmap.Render(drawingVisual);
            renderBitmap.Freeze();

            // Convert to pixel data
            var stride = CursorSize * 4; // 4 bytes per pixel (BGRA)
            var pixelData = new byte[stride * CursorSize];
            renderBitmap.CopyPixels(pixelData, stride, 0);

            // Create cursor file structure
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Cursor file header (6 bytes)
                writer.Write((ushort)0);    // Reserved (must be 0)
                writer.Write((ushort)2);    // Type (2 = cursor)
                writer.Write((ushort)1);    // Number of images

                // Cursor directory entry (16 bytes)
                writer.Write((byte)CursorSize);         // Width
                writer.Write((byte)CursorSize);         // Height
                writer.Write((byte)0);                  // Color count (0 for true color)
                writer.Write((byte)0);                  // Reserved
                writer.Write((ushort)(CursorSize / 2)); // Hotspot X (center)
                writer.Write((ushort)(CursorSize / 2)); // Hotspot Y (center)

                // Calculate sizes
                var bitmapInfoSize = 40;
                var imageDataSize = stride * CursorSize;
                var maskDataSize = ((CursorSize + 31) / 32) * 4 * CursorSize;
                var totalImageSize = bitmapInfoSize + imageDataSize + maskDataSize;

                writer.Write((uint)totalImageSize);     // Size of image data
                writer.Write((uint)(6 + 16));           // Offset to image data

                // BITMAPINFOHEADER (40 bytes)
                writer.Write((uint)40);                 // Header size
                writer.Write((int)CursorSize);          // Width
                writer.Write((int)(CursorSize * 2));    // Height (doubled for cursor)
                writer.Write((ushort)1);                // Planes
                writer.Write((ushort)32);               // Bits per pixel
                writer.Write((uint)0);                  // Compression (BI_RGB)
                writer.Write((uint)imageDataSize);      // Image size
                writer.Write((int)0);                   // X pixels per meter
                writer.Write((int)0);                   // Y pixels per meter
                writer.Write((uint)0);                  // Colors used
                writer.Write((uint)0);                  // Important colors

                // Write pixel data (bottom-up, as required by bitmap format)
                for (int y = CursorSize - 1; y >= 0; y--)
                {
                    var rowOffset = y * stride;
                    writer.Write(pixelData, rowOffset, stride);
                }

                // Write AND mask (all zeros for full alpha support)
                var maskBytes = new byte[maskDataSize];
                writer.Write(maskBytes);

                return stream.ToArray();
            }
        }

        #endregion CreateCursorFileFromBitmap

        #region DestroyIcon

        /// <summary>
        /// Windows API function to destroy an icon handle
        /// </summary>
        /// <param name="hIcon">Icon handle to destroy</param>
        /// <returns>True if successful</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion DestroyIcon
    }
}