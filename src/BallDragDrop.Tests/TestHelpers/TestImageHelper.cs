using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Models;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// Helper class for creating test images for unit tests
    /// </summary>
    public static class TestImageHelper
    {
        /// <summary>
        /// Creates a test image with the specified dimensions
        /// </summary>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="color">Color of the image (optional, defaults to blue)</param>
        /// <returns>A WriteableBitmap for testing</returns>
        public static WriteableBitmap CreateTestImage(int width, int height, Color? color = null)
        {
            var testColor = color ?? Colors.Blue;
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            
            // Fill the bitmap with the specified color
            var stride = width * 4; // 4 bytes per pixel for BGRA32
            var pixels = new byte[height * stride];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    pixels[index] = testColor.B;     // Blue
                    pixels[index + 1] = testColor.G; // Green
                    pixels[index + 2] = testColor.R; // Red
                    pixels[index + 3] = testColor.A; // Alpha
                }
            }
            
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            bitmap.Freeze(); // Freeze for better performance in tests
            
            return bitmap;
        }

        /// <summary>
        /// Creates a test image with a gradient pattern for visual distinction
        /// </summary>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <returns>A WriteableBitmap with gradient pattern</returns>
        public static WriteableBitmap CreateGradientTestImage(int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            
            var stride = width * 4;
            var pixels = new byte[height * stride];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    
                    // Create a gradient effect
                    byte intensity = (byte)((x + y) * 255 / (width + height));
                    
                    pixels[index] = intensity;     // Blue
                    pixels[index + 1] = (byte)(255 - intensity); // Green
                    pixels[index + 2] = (byte)(intensity / 2);    // Red
                    pixels[index + 3] = 255;       // Alpha (fully opaque)
                }
            }
            
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            bitmap.Freeze();
            
            return bitmap;
        }

        /// <summary>
        /// Creates a series of test images with different colors for animation testing
        /// </summary>
        /// <param name="frameCount">Number of frames to create</param>
        /// <param name="width">Width of each frame</param>
        /// <param name="height">Height of each frame</param>
        /// <returns>Array of WriteableBitmap frames</returns>
        public static WriteableBitmap[] CreateAnimationFrames(int frameCount, int width, int height)
        {
            var frames = new WriteableBitmap[frameCount];
            var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Purple, Colors.Orange };
            
            for (int i = 0; i < frameCount; i++)
            {
                var color = colors[i % colors.Length];
                frames[i] = CreateTestImage(width, height, color);
            }
            
            return frames;
        }

        /// <summary>
        /// Creates a test static image file on disk
        /// </summary>
        /// <param name="directory">Directory to create the file in</param>
        /// <param name="fileName">Name of the file to create</param>
        /// <param name="width">Width of the image (default 50)</param>
        /// <param name="height">Height of the image (default 50)</param>
        /// <param name="color">Color of the image (default Red)</param>
        /// <returns>Full path to the created image file</returns>
        public static string CreateTestStaticImage(string directory, string fileName, int width = 50, int height = 50, Color? color = null)
        {
            var testColor = color ?? Colors.Red;
            var bitmap = CreateTestImage(width, height, testColor);
            var filePath = Path.Combine(directory, fileName);
            
            // Save as PNG
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
            
            return filePath;
        }

        /// <summary>
        /// Creates a test GIF animation file on disk
        /// </summary>
        /// <param name="directory">Directory to create the file in</param>
        /// <param name="fileName">Name of the file to create</param>
        /// <param name="frameCount">Number of frames in the animation (default 3)</param>
        /// <param name="width">Width of each frame (default 50)</param>
        /// <param name="height">Height of each frame (default 50)</param>
        /// <returns>Full path to the created GIF file</returns>
        public static string CreateTestGifAnimation(string directory, string fileName, int frameCount = 3, int width = 50, int height = 50)
        {
            var filePath = Path.Combine(directory, fileName);
            
            // Create a simple multi-frame GIF by creating multiple PNG frames
            // Note: This is a simplified approach for testing. In a real scenario, 
            // you might want to use a proper GIF encoder library.
            
            // For testing purposes, we'll create a simple animated GIF-like structure
            // by creating multiple frames and saving them as a single image with metadata
            var frames = CreateAnimationFrames(frameCount, width, height);
            
            // Save the first frame as the base image (this simulates a GIF for testing)
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new GifBitmapEncoder();
                
                foreach (var frame in frames)
                {
                    encoder.Frames.Add(BitmapFrame.Create(frame));
                }
                
                encoder.Save(fileStream);
            }
            
            return filePath;
        }

        /// <summary>
        /// Creates a test Aseprite animation (PNG + JSON) files on disk
        /// </summary>
        /// <param name="directory">Directory to create the files in</param>
        /// <param name="baseFileName">Base name for the files (without extension)</param>
        /// <param name="frameCount">Number of frames in the animation (default 4)</param>
        /// <param name="frameWidth">Width of each frame (default 32)</param>
        /// <param name="frameHeight">Height of each frame (default 32)</param>
        /// <returns>Full path to the created PNG file</returns>
        public static string CreateTestAsepriteAnimation(string directory, string baseFileName, int frameCount = 4, int frameWidth = 32, int frameHeight = 32)
        {
            var pngPath = Path.Combine(directory, baseFileName + ".png");
            var jsonPath = Path.Combine(directory, baseFileName + ".json");
            
            // Create sprite sheet (horizontal layout)
            var spriteSheetWidth = frameWidth * frameCount;
            var spriteSheetHeight = frameHeight;
            var spriteSheet = new WriteableBitmap(spriteSheetWidth, spriteSheetHeight, 96, 96, PixelFormats.Bgra32, null);
            
            // Fill sprite sheet with frames
            var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow };
            for (int i = 0; i < frameCount; i++)
            {
                var frameColor = colors[i % colors.Length];
                var frameImage = CreateTestImage(frameWidth, frameHeight, frameColor);
                
                // Copy frame to sprite sheet
                var sourceRect = new Int32Rect(0, 0, frameWidth, frameHeight);
                var destRect = new Int32Rect(i * frameWidth, 0, frameWidth, frameHeight);
                
                var stride = frameWidth * 4;
                var pixels = new byte[frameHeight * stride];
                frameImage.CopyPixels(sourceRect, pixels, stride, 0);
                spriteSheet.WritePixels(destRect, pixels, stride, 0);
            }
            
            spriteSheet.Freeze();
            
            // Save PNG sprite sheet
            using (var fileStream = new FileStream(pngPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(spriteSheet));
                encoder.Save(fileStream);
            }
            
            // Create JSON metadata
            var jsonContent = CreateAsepriteJsonMetadata(frameCount, frameWidth, frameHeight, spriteSheetWidth, spriteSheetHeight);
            File.WriteAllText(jsonPath, jsonContent);
            
            return pngPath;
        }

        /// <summary>
        /// Creates Aseprite JSON metadata content
        /// </summary>
        /// <param name="frameCount">Number of frames</param>
        /// <param name="frameWidth">Width of each frame</param>
        /// <param name="frameHeight">Height of each frame</param>
        /// <param name="spriteSheetWidth">Total width of sprite sheet</param>
        /// <param name="spriteSheetHeight">Total height of sprite sheet</param>
        /// <returns>JSON content as string</returns>
        private static string CreateAsepriteJsonMetadata(int frameCount, int frameWidth, int frameHeight, int spriteSheetWidth, int spriteSheetHeight)
        {
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"frames\": [");
            
            // Create frame definitions
            for (int i = 0; i < frameCount; i++)
            {
                json.AppendLine($"    {{");
                json.AppendLine($"      \"filename\": \"frame_{i}.png\",");
                json.AppendLine($"      \"frame\": {{ \"x\": {i * frameWidth}, \"y\": 0, \"w\": {frameWidth}, \"h\": {frameHeight} }},");
                json.AppendLine($"      \"rotated\": false,");
                json.AppendLine($"      \"trimmed\": false,");
                json.AppendLine($"      \"spriteSourceSize\": {{ \"x\": 0, \"y\": 0, \"w\": {frameWidth}, \"h\": {frameHeight} }},");
                json.AppendLine($"      \"sourceSize\": {{ \"w\": {frameWidth}, \"h\": {frameHeight} }},");
                json.AppendLine($"      \"duration\": 100");
                json.Append($"    }}");
                if (i < frameCount - 1) json.AppendLine(",");
                else json.AppendLine();
            }
            
            json.AppendLine("  ],");
            json.AppendLine("  \"meta\": {");
            json.AppendLine($"    \"app\": \"http://www.aseprite.org/\",");
            json.AppendLine($"    \"version\": \"1.2.25\",");
            json.AppendLine($"    \"image\": \"test_sprite.png\",");
            json.AppendLine($"    \"format\": \"RGBA8888\",");
            json.AppendLine($"    \"size\": {{ \"w\": {spriteSheetWidth}, \"h\": {spriteSheetHeight} }},");
            json.AppendLine($"    \"scale\": \"1\",");
            json.AppendLine("    \"frameTags\": [");
            json.AppendLine("      {");
            json.AppendLine("        \"name\": \"default\",");
            json.AppendLine("        \"from\": 0,");
            json.AppendLine($"        \"to\": {frameCount - 1},");
            json.AppendLine("        \"direction\": \"forward\"");
            json.AppendLine("      }");
            json.AppendLine("    ]");
            json.AppendLine("  }");
            json.AppendLine("}");
            
            return json.ToString();
        }

        /// <summary>
        /// Creates a test image file with a specific format
        /// </summary>
        /// <param name="directory">Directory to create the file in</param>
        /// <param name="fileName">Name of the file to create</param>
        /// <param name="format">Image format (PNG, JPEG, BMP)</param>
        /// <param name="width">Width of the image (default 50)</param>
        /// <param name="height">Height of the image (default 50)</param>
        /// <param name="color">Color of the image (default Blue)</param>
        /// <returns>Full path to the created image file</returns>
        public static string CreateTestImageWithFormat(string directory, string fileName, string format, int width = 50, int height = 50, Color? color = null)
        {
            var testColor = color ?? Colors.Blue;
            var bitmap = CreateTestImage(width, height, testColor);
            var filePath = Path.Combine(directory, fileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = format.ToUpperInvariant() switch
                {
                    "PNG" => new PngBitmapEncoder(),
                    "JPEG" or "JPG" => new JpegBitmapEncoder(),
                    "BMP" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };
                
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
            
            return filePath;
        }

        #region Additional File Format Helper Methods

        /// <summary>
        /// Creates test PNG image data as byte array
        /// </summary>
        public static byte[] CreateTestPngData(int width, int height, Color color)
        {
            var bitmap = CreateTestImage(width, height, color);
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test PNG image data with transparency
        /// </summary>
        public static byte[] CreateTestPngDataWithTransparency(int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            var stride = width * 4;
            var pixels = new byte[height * stride];
            
            // Create a pattern with transparency
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    pixels[index] = 255;     // Blue
                    pixels[index + 1] = 0;   // Green
                    pixels[index + 2] = 0;   // Red
                    pixels[index + 3] = (byte)((x + y) % 256); // Alpha (transparency pattern)
                }
            }
            
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            bitmap.Freeze();
            
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test JPG image data as byte array
        /// </summary>
        public static byte[] CreateTestJpgData(int width, int height, Color color)
        {
            var bitmap = CreateTestImage(width, height, color);
            using (var stream = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test JPG image data with specific quality
        /// </summary>
        public static byte[] CreateTestJpgDataWithQuality(int width, int height, Color color, int quality)
        {
            var bitmap = CreateTestImage(width, height, color);
            using (var stream = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test BMP image data as byte array
        /// </summary>
        public static byte[] CreateTestBmpData(int width, int height, Color color)
        {
            var bitmap = CreateTestImage(width, height, color);
            using (var stream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test BMP image data with specific bit depth
        /// </summary>
        public static byte[] CreateTestBmpDataWithBitDepth(int width, int height, Color color, int bitDepth)
        {
            // Note: WPF BmpBitmapEncoder doesn't directly support bit depth control
            // This method creates a standard BMP and simulates different bit depths
            var bitmap = CreateTestImage(width, height, color);
            using (var stream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test GIF animation data as byte array
        /// </summary>
        public static byte[] CreateTestGifAnimationData(int fps, int frameCount)
        {
            var frames = CreateAnimationFrames(frameCount, 32, 32);
            using (var stream = new MemoryStream())
            {
                var encoder = new GifBitmapEncoder();
                foreach (var frame in frames)
                {
                    encoder.Frames.Add(BitmapFrame.Create(frame));
                }
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test GIF data for testing purposes
        /// </summary>
        public static byte[] CreateTestGifData(int fps)
        {
            return CreateTestGifAnimationData(fps, 4);
        }

        /// <summary>
        /// Creates test sprite sheet data as byte array
        /// </summary>
        public static byte[] CreateTestSpriteSheetData(int frameWidth, int frameHeight, int frameCount)
        {
            var spriteSheetWidth = frameWidth * frameCount;
            var spriteSheet = new WriteableBitmap(spriteSheetWidth, frameHeight, 96, 96, PixelFormats.Bgra32, null);
            
            var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Purple, Colors.Orange };
            for (int i = 0; i < frameCount; i++)
            {
                var frameColor = colors[i % colors.Length];
                var frameImage = CreateTestImage(frameWidth, frameHeight, frameColor);
                
                var sourceRect = new Int32Rect(0, 0, frameWidth, frameHeight);
                var destRect = new Int32Rect(i * frameWidth, 0, frameWidth, frameHeight);
                
                var stride = frameWidth * 4;
                var pixels = new byte[frameHeight * stride];
                frameImage.CopyPixels(sourceRect, pixels, stride, 0);
                spriteSheet.WritePixels(destRect, pixels, stride, 0);
            }
            
            spriteSheet.Freeze();
            
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(spriteSheet));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates test sprite sheet for testing
        /// </summary>
        public static byte[] CreateTestSpriteSheet(int frameWidth, int frameHeight, int frameCount)
        {
            return CreateTestSpriteSheetData(frameWidth, frameHeight, frameCount);
        }

        /// <summary>
        /// Creates test image data as byte array
        /// </summary>
        public static byte[] CreateTestImageData(int width, int height, Color color)
        {
            return CreateTestPngData(width, height, color);
        }

        /// <summary>
        /// Creates test Aseprite JSON data
        /// </summary>
        public static string CreateTestAsepriteJsonData(int fps, int frameCount)
        {
            var frameDuration = 1000 / fps; // Convert FPS to milliseconds
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"frames\": {");
            
            for (int i = 0; i < frameCount; i++)
            {
                json.AppendLine($"    \"frame_{i}\": {{");
                json.AppendLine($"      \"frame\": {{ \"x\": {i * 64}, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"rotated\": false,");
                json.AppendLine($"      \"trimmed\": false,");
                json.AppendLine($"      \"spriteSourceSize\": {{ \"x\": 0, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"sourceSize\": {{ \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"duration\": {frameDuration}");
                json.Append($"    }}");
                if (i < frameCount - 1) json.AppendLine(",");
                else json.AppendLine();
            }
            
            json.AppendLine("  },");
            json.AppendLine("  \"meta\": {");
            json.AppendLine("    \"app\": \"Aseprite\",");
            json.AppendLine("    \"version\": \"1.2.25\",");
            json.AppendLine("    \"format\": \"RGBA8888\",");
            json.AppendLine($"    \"size\": {{ \"w\": {frameCount * 64}, \"h\": 64 }},");
            json.AppendLine("    \"scale\": \"1\",");
            json.AppendLine("    \"frameTags\": [");
            json.AppendLine("      {");
            json.AppendLine("        \"name\": \"default\",");
            json.AppendLine("        \"from\": 0,");
            json.AppendLine($"        \"to\": {frameCount - 1},");
            json.AppendLine("        \"direction\": \"forward\"");
            json.AppendLine("      }");
            json.AppendLine("    ]");
            json.AppendLine("  }");
            json.AppendLine("}");
            
            return json.ToString();
        }

        /// <summary>
        /// Creates test Aseprite JSON with multiple animation tags
        /// </summary>
        public static string CreateTestAsepriteJsonWithMultipleTags(int fps)
        {
            var frameDuration = 1000 / fps;
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"frames\": {");
            
            // Create 12 frames for 3 animations of 4 frames each
            for (int i = 0; i < 12; i++)
            {
                json.AppendLine($"    \"frame_{i}\": {{");
                json.AppendLine($"      \"frame\": {{ \"x\": {i * 64}, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"rotated\": false,");
                json.AppendLine($"      \"trimmed\": false,");
                json.AppendLine($"      \"spriteSourceSize\": {{ \"x\": 0, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"sourceSize\": {{ \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"duration\": {frameDuration}");
                json.Append($"    }}");
                if (i < 11) json.AppendLine(",");
                else json.AppendLine();
            }
            
            json.AppendLine("  },");
            json.AppendLine("  \"meta\": {");
            json.AppendLine("    \"app\": \"Aseprite\",");
            json.AppendLine("    \"version\": \"1.2.25\",");
            json.AppendLine("    \"format\": \"RGBA8888\",");
            json.AppendLine("    \"size\": { \"w\": 768, \"h\": 64 },");
            json.AppendLine("    \"scale\": \"1\",");
            json.AppendLine("    \"frameTags\": [");
            json.AppendLine("      {");
            json.AppendLine("        \"name\": \"walk\",");
            json.AppendLine("        \"from\": 0,");
            json.AppendLine("        \"to\": 3,");
            json.AppendLine("        \"direction\": \"forward\"");
            json.AppendLine("      },");
            json.AppendLine("      {");
            json.AppendLine("        \"name\": \"run\",");
            json.AppendLine("        \"from\": 4,");
            json.AppendLine("        \"to\": 7,");
            json.AppendLine("        \"direction\": \"forward\"");
            json.AppendLine("      },");
            json.AppendLine("      {");
            json.AppendLine("        \"name\": \"jump\",");
            json.AppendLine("        \"from\": 8,");
            json.AppendLine("        \"to\": 11,");
            json.AppendLine("        \"direction\": \"forward\"");
            json.AppendLine("      }");
            json.AppendLine("    ]");
            json.AppendLine("  }");
            json.AppendLine("}");
            
            return json.ToString();
        }

        /// <summary>
        /// Creates test Aseprite JSON without animation tags
        /// </summary>
        public static string CreateTestAsepriteJsonWithoutTags(int fps, int frameCount)
        {
            var frameDuration = 1000 / fps;
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"frames\": {");
            
            for (int i = 0; i < frameCount; i++)
            {
                json.AppendLine($"    \"frame_{i}\": {{");
                json.AppendLine($"      \"frame\": {{ \"x\": {i * 64}, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"rotated\": false,");
                json.AppendLine($"      \"trimmed\": false,");
                json.AppendLine($"      \"spriteSourceSize\": {{ \"x\": 0, \"y\": 0, \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"sourceSize\": {{ \"w\": 64, \"h\": 64 }},");
                json.AppendLine($"      \"duration\": {frameDuration}");
                json.Append($"    }}");
                if (i < frameCount - 1) json.AppendLine(",");
                else json.AppendLine();
            }
            
            json.AppendLine("  },");
            json.AppendLine("  \"meta\": {");
            json.AppendLine("    \"app\": \"Aseprite\",");
            json.AppendLine("    \"version\": \"1.2.25\",");
            json.AppendLine("    \"format\": \"RGBA8888\",");
            json.AppendLine($"    \"size\": {{ \"w\": {frameCount * 64}, \"h\": 64 }},");
            json.AppendLine("    \"scale\": \"1\",");
            json.AppendLine("    \"frameTags\": []");
            json.AppendLine("  }");
            json.AppendLine("}");
            
            return json.ToString();
        }

        /// <summary>
        /// Creates test Aseprite JSON data
        /// </summary>
        public static string CreateTestAsepriteJson(int fps, int frameCount)
        {
            return CreateTestAsepriteJsonData(fps, frameCount);
        }

        #endregion Additional File Format Helper Methods
    }
}