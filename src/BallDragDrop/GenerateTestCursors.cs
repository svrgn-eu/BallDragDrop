using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BallDragDrop
{
    /// <summary>
    /// Utility class to generate test cursor images
    /// </summary>
    public static class GenerateTestCursors
    {
        /// <summary>
        /// Generates all test cursor images
        /// </summary>
        public static void GenerateAll()
        {
            try
            {
                var outputDir = "Resources/Cursors";
                
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                Console.WriteLine("Generating test cursor images...");

                // Generate cursors for each state
                GenerateCursor(Path.Combine(outputDir, "default.png"), Color.Blue, "DEF");
                GenerateCursor(Path.Combine(outputDir, "hover.png"), Color.Green, "HOV");
                GenerateCursor(Path.Combine(outputDir, "grabbing.png"), Color.Red, "GRAB");
                GenerateCursor(Path.Combine(outputDir, "releasing.png"), Color.Orange, "REL");

                Console.WriteLine("Test cursor generation completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a single test cursor image
        /// </summary>
        /// <param name="outputPath">Output file path</param>
        /// <param name="color">Cursor color</param>
        /// <param name="text">Text to display</param>
        private static void GenerateCursor(string outputPath, Color color, string text)
        {
            const int size = 32;

            using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Clear with transparent background
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw cursor arrow shape
            var points = new Point[]
            {
                new Point(2, 2),   // Top
                new Point(2, 22),  // Bottom left
                new Point(8, 16),  // Inner point
                new Point(16, 22), // Bottom right
                new Point(12, 12), // Arrow tip inner
                new Point(2, 2)    // Back to top
            };

            using var brush = new SolidBrush(color);
            using var pen = new Pen(Color.Black, 2);
            
            // Fill and outline the cursor
            graphics.FillPolygon(brush, points);
            graphics.DrawPolygon(pen, points);
            
            // Add text label
            using var font = new Font("Arial", 7, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            
            var textSize = graphics.MeasureString(text, font);
            var textX = (size - textSize.Width) / 2;
            var textY = size - textSize.Height - 1;
            
            // Draw text with black outline for visibility
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
            
            Console.WriteLine($"Generated: {outputPath} ({new FileInfo(outputPath).Length} bytes)");
        }
    }
}