using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Service for loading and managing images
    /// </summary>
    public class ImageService
    {
        /// <summary>
        /// Loads an image from the specified path
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>ImageSource if successful, null if failed</returns>
        public static ImageSource LoadImage(string imagePath)
        {
            try
            {
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
                
                return bitmap;
            }
            catch (Exception ex)
            {
                // Log the error (in a real application, we would use a proper logging framework)
                Console.WriteLine($"Error loading image from {imagePath}: {ex.Message}");
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
        /// <returns>ImageSource representing a circle</returns>
        public static ImageSource CreateFallbackImage(double radius, Color fillColor, Color strokeColor, double strokeThickness = 2)
        {
            // Calculate the size of the drawing
            int size = (int)(radius * 2);
            
            // Create a DrawingVisual
            var drawingVisual = new DrawingVisual();
            
            // Get the DrawingContext
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Draw a circle
                drawingContext.DrawEllipse(
                    new SolidColorBrush(fillColor),
                    new Pen(new SolidColorBrush(strokeColor), strokeThickness),
                    new System.Windows.Point(radius, radius),
                    radius - strokeThickness / 2, // Adjust radius to account for stroke thickness
                    radius - strokeThickness / 2);
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
            
            return renderBitmap;
        }
    }
}
