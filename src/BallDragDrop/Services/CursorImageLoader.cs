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
        /// Loads a PNG file and converts it to a WPF Cursor with fallback handling
        /// </summary>
        /// <param name="pngPath">Relative path to the PNG file from application directory</param>
        /// <param name="fallbackCursor">Cursor to use if loading fails</param>
        /// <returns>WPF Cursor object</returns>
        public Cursor LoadPngAsCursorWithFallback(string pngPath, Cursor fallbackCursor)
        {
            try
            {
                // Validate input path
                if (string.IsNullOrEmpty(pngPath))
                {
                    _logService.LogWarning("PNG path is null or empty, using fallback cursor");
                    return fallbackCursor;
                }

                // Resolve path relative to application directory
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pngPath);
                
                if (!File.Exists(fullPath))
                {
                    _logService.LogWarning("PNG file not found: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Validate file size (prevent loading extremely large files)
                var fileInfo = new FileInfo(fullPath);
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
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to resize PNG image: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }

                // Convert to cursor
                try
                {
                    return ConvertBitmapToCursor(resizedBitmap);
                }
                catch (Exception ex)
                {
                    _logService.LogError(ex, "Failed to convert bitmap to cursor: {FullPath}, using fallback cursor", fullPath);
                    return fallbackCursor;
                }
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
        /// Loads a bitmap with validation and error handling
        /// </summary>
        /// <param name="fullPath">Full path to the image file</param>
        /// <returns>Loaded bitmap image</returns>
        private BitmapImage LoadBitmapWithValidation(string fullPath)
        {
            var bitmap = new BitmapImage();
            
            try
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Validate bitmap properties
                if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                {
                    throw new InvalidOperationException($"Invalid bitmap dimensions: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                }

                if (bitmap.PixelWidth > 1000 || bitmap.PixelHeight > 1000)
                {
                    _logService.LogWarning("Large bitmap dimensions ({Width}x{Height}) for cursor: {Path}", 
                        bitmap.PixelWidth, bitmap.PixelHeight, fullPath);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                bitmap?.Freeze(); // Ensure cleanup
                throw new InvalidOperationException($"Failed to load bitmap from {fullPath}", ex);
            }
        }

        #endregion LoadBitmapWithValidation

        #region ResizeImage

        /// <summary>
        /// Resizes an image to the standard cursor size (30x30)
        /// </summary>
        /// <param name="source">Source bitmap image</param>
        /// <returns>Resized bitmap image</returns>
        private BitmapImage ResizeImage(BitmapImage source)
        {
            try
            {
                // If already the correct size, return as-is
                if (source.PixelWidth == CursorSize && source.PixelHeight == CursorSize)
                {
                    return source;
                }

                // Create a DrawingVisual to render the resized image
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(source, new Rect(0, 0, CursorSize, CursorSize));
                }

                // Render to a RenderTargetBitmap
                var renderTargetBitmap = new RenderTargetBitmap(
                    CursorSize, CursorSize, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);
                renderTargetBitmap.Freeze();

                // Convert back to BitmapImage
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var resizedBitmap = new BitmapImage();
                    resizedBitmap.BeginInit();
                    resizedBitmap.StreamSource = stream;
                    resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    resizedBitmap.EndInit();
                    resizedBitmap.Freeze();

                    return resizedBitmap;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to resize image to {Size}x{Size}", CursorSize, CursorSize);
                return source; // Return original on error
            }
        }

        #endregion ResizeImage

        #region ConvertBitmapToCursor

        /// <summary>
        /// Converts a BitmapImage to a Cursor
        /// </summary>
        /// <param name="bitmap">Source bitmap</param>
        /// <returns>WPF Cursor</returns>
        private Cursor ConvertBitmapToCursor(BitmapImage bitmap)
        {
            try
            {
                // For WPF, we need to use a different approach to create custom cursors
                // Since the Cursor constructor with Point is not available in WPF,
                // we'll use the stream-based constructor
                if (bitmap.StreamSource != null)
                {
                    bitmap.StreamSource.Position = 0;
                    return new Cursor(bitmap.StreamSource);
                }
                else
                {
                    _logService.LogWarning("Bitmap stream source is null, using default cursor");
                    return Cursors.Arrow;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Failed to convert bitmap to cursor");
                return Cursors.Arrow;
            }
        }

        #endregion ConvertBitmapToCursor
    }
}