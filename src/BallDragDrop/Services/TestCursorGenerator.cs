using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BallDragDrop.Contracts;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Generates test cursor images for debugging cursor system
    /// </summary>
    public class TestCursorGenerator
    {
        #region Fields

        /// <summary>
        /// Log service for debugging
        /// </summary>
        private readonly ILogService _logService;

        #endregion Fields

        #region Construction

        /// <summary>
        /// Initializes a new instance of the TestCursorGenerator class
        /// </summary>
        /// <param name="logService">Log service</param>
        public TestCursorGenerator(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        #endregion Construction

        #region Methods

        #region GenerateTestCursors

        /// <summary>
        /// Generates test cursor images for all hand states
        /// </summary>
        /// <param name="outputDirectory">Directory to save cursor images</param>
        public void GenerateTestCursors(string outputDirectory)
        {
            try
            {
                _logService.LogInformation("Generating test cursor images in directory: {Directory}", outputDirectory);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    _logService.LogDebug("Created output directory: {Directory}", outputDirectory);
                }

                // Generate cursors for each state
                GenerateCursor("default.png", Color.Blue, "DEFAULT", outputDirectory);
                GenerateCursor("hover.png", Color.Green, "HOVER", outputDirectory);
                GenerateCursor("grabbing.png", Color.Red, "GRAB", outputDirectory);
                GenerateCursor("releasing.png", Color.Orange, "RELEASE", outputDirectory);

                _logService.LogInformation("Test cursor generation completed successfully");
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error generating test cursors");
            }
        }

        #endregion GenerateTestCursors

        #region GenerateCursor

        /// <summary>
        /// Generates a single test cursor image
        /// </summary>
        /// <param name="filename">Output filename</param>
        /// <param name="color">Cursor color</param>
        /// <param name="text">Text to display on cursor</param>
        /// <param name="outputDirectory">Output directory</param>
        private void GenerateCursor(string filename, Color color, string text, string outputDirectory)
        {
            try
            {
                const int size = 32;
                var outputPath = Path.Combine(outputDirectory, filename);

                using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                
                // Clear with transparent background
                graphics.Clear(Color.Transparent);
                
                // Draw cursor shape (arrow)
                var points = new Point[]
                {
                    new Point(2, 2),   // Top
                    new Point(2, 20),  // Bottom left
                    new Point(8, 14),  // Inner point
                    new Point(14, 20), // Bottom right
                    new Point(10, 10), // Arrow tip inner
                    new Point(2, 2)    // Back to top
                };

                using var brush = new SolidBrush(color);
                using var pen = new Pen(Color.Black, 1);
                
                // Fill and outline the cursor
                graphics.FillPolygon(brush, points);
                graphics.DrawPolygon(pen, points);
                
                // Add text label
                using var font = new Font("Arial", 6, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                using var textPen = new Pen(Color.Black, 1);
                
                var textSize = graphics.MeasureString(text, font);
                var textX = (size - textSize.Width) / 2;
                var textY = size - textSize.Height - 2;
                
                // Draw text with outline for visibility
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx != 0 || dy != 0)
                        {
                            graphics.DrawString(text, font, Brushes.Black, textX + dx, textY + dy);
                        }
                    }
                }
                graphics.DrawString(text, font, textBrush, textX, textY);

                // Save as PNG
                bitmap.Save(outputPath, ImageFormat.Png);
                
                _logService.LogDebug("Generated test cursor: {Filename} ({Color})", filename, color.Name);
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "Error generating cursor: {Filename}", filename);
            }
        }

        #endregion GenerateCursor

        #endregion Methods
    }
}