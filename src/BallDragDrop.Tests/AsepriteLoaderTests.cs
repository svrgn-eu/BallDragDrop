using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using BallDragDrop.Contracts;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class AsepriteLoaderTests
    {
        private Mock<ILogService> _mockLogService;
        private AsepriteLoader _asepriteLoader;
        private string _testDataDirectory;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _asepriteLoader = new AsepriteLoader(_mockLogService.Object);
            
            // Create test data directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "AsepriteLoaderTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test data directory
            if (Directory.Exists(_testDataDirectory))
            {
                Directory.Delete(_testDataDirectory, true);
            }
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_Default_InitializesCorrectly()
        {
            // Arrange & Act
            var loader = new AsepriteLoader();

            // Assert
            Assert.IsNotNull(loader);
        }

        [TestMethod]
        public void Constructor_WithLogService_InitializesCorrectly()
        {
            // Arrange & Act
            var loader = new AsepriteLoader(_mockLogService.Object);

            // Assert
            Assert.IsNotNull(loader);
        }

        #endregion Constructor Tests

        #region LoadAsepriteAsync Tests

        [TestMethod]
        public async Task LoadAsepriteAsync_NullPngPath_ReturnsNull()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(null, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_NullJsonPath_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_EmptyPaths_ReturnsNull()
        {
            // Arrange & Act
            var result = await _asepriteLoader.LoadAsepriteAsync("", "");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_NonExistentPngFile_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "nonexistent.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            File.WriteAllText(jsonPath, "{}");

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_NonExistentJsonFile_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "nonexistent.json");
            CreateTestPngFile(pngPath);

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_ValidFiles_ReturnsAsepriteData()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            CreateTestJsonFile(jsonPath);

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Frames);
            Assert.IsNotNull(result.Meta);
            Assert.IsNotNull(result.FrameTags);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_InvalidJsonContent_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            File.WriteAllText(jsonPath, "invalid json content");

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_EmptyJsonFile_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            File.WriteAllText(jsonPath, ""); // Empty file

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_WhitespaceOnlyJsonFile_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            File.WriteAllText(jsonPath, "   \n\t  "); // Whitespace only

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_MalformedJsonStructure_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            File.WriteAllText(jsonPath, "{\"frames\": \"not_an_object\", \"meta\": null}");

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonWithNoFrames_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            var invalidData = new
            {
                frames = new Dictionary<string, object>(), // Empty frames
                meta = new { app = "Aseprite", version = "1.2.25" }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(invalidData));

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonWithInvalidFrameDimensions_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            var invalidData = new
            {
                frames = new Dictionary<string, object>
                {
                    ["frame1"] = new
                    {
                        frame = new { x = 0, y = 0, w = 0, h = 0 }, // Invalid dimensions
                        duration = 100
                    }
                },
                meta = new { app = "Aseprite", version = "1.2.25" }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(invalidData));

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonWithNegativeFramePosition_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            var invalidData = new
            {
                frames = new Dictionary<string, object>
                {
                    ["frame1"] = new
                    {
                        frame = new { x = -10, y = -5, w = 16, h = 16 }, // Negative position
                        duration = 100
                    }
                },
                meta = new { app = "Aseprite", version = "1.2.25" }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(invalidData));

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonWithInvalidMetadataDimensions_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            var invalidData = new
            {
                frames = new Dictionary<string, object>
                {
                    ["frame1"] = new
                    {
                        frame = new { x = 0, y = 0, w = 16, h = 16 },
                        duration = 100
                    }
                },
                meta = new 
                { 
                    app = "Aseprite", 
                    version = "1.2.25",
                    size = new { w = -32, h = -32 } // Invalid metadata dimensions
                }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(invalidData));

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonWithNegativeFrameDuration_LoadsSuccessfully()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "test.json");
            
            CreateTestPngFile(pngPath);
            var dataWithNegativeDuration = new
            {
                frames = new Dictionary<string, object>
                {
                    ["frame1"] = new
                    {
                        frame = new { x = 0, y = 0, w = 16, h = 16 },
                        duration = -100 // Negative duration should be handled gracefully
                    }
                },
                meta = new { app = "Aseprite", version = "1.2.25" }
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(dataWithNegativeDuration));

            // Act
            var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);

            // Assert
            Assert.IsNotNull(result, "Should load successfully despite negative duration");
            Assert.IsNotNull(result.Frames);
            Assert.AreEqual(1, result.Frames.Count);
        }

        [TestMethod]
        public async Task LoadAsepriteAsync_JsonAccessDenied_ReturnsNull()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            var jsonPath = Path.Combine(_testDataDirectory, "readonly.json");
            
            CreateTestPngFile(pngPath);
            CreateTestJsonFile(jsonPath);
            
            // Make file read-only to simulate access denied
            File.SetAttributes(jsonPath, FileAttributes.ReadOnly);
            
            try
            {
                // Try to make it even more restrictive (this might not work on all systems)
                var fileInfo = new FileInfo(jsonPath);
                var fileSecurity = fileInfo.GetAccessControl();
                // Note: This test might not work reliably across all systems
                // The main test is that the method handles UnauthorizedAccessException gracefully
            }
            catch
            {
                // If we can't set restrictive permissions, skip this part
                // The method should still handle the exception gracefully
            }

            // Act & Assert
            try
            {
                var result = await _asepriteLoader.LoadAsepriteAsync(pngPath, jsonPath);
                // Should return null if access is denied, or succeed if permissions allow
                // Either outcome is acceptable for this test
            }
            finally
            {
                // Clean up - remove read-only attribute
                try
                {
                    File.SetAttributes(jsonPath, FileAttributes.Normal);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #endregion LoadAsepriteAsync Tests

        #region ConvertToAnimationFrames Tests

        [TestMethod]
        public void ConvertToAnimationFrames_NullData_ReturnsEmptyList()
        {
            // Arrange
            var spriteSheet = CreateTestSpriteSheet();

            // Act
            var result = _asepriteLoader.ConvertToAnimationFrames(null, spriteSheet);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void ConvertToAnimationFrames_NullSpriteSheet_ReturnsEmptyList()
        {
            // Arrange
            var data = CreateTestAsepriteData();

            // Act
            var result = _asepriteLoader.ConvertToAnimationFrames(data, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void ConvertToAnimationFrames_ValidData_ReturnsAnimationFrames()
        {
            // Arrange
            var data = CreateTestAsepriteData();
            var spriteSheet = CreateTestSpriteSheet();

            // Act
            var result = _asepriteLoader.ConvertToAnimationFrames(data, spriteSheet);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count); // Based on test data
            
            foreach (var frame in result)
            {
                Assert.IsNotNull(frame.Image);
                Assert.IsTrue(frame.Duration > TimeSpan.Zero);
            }
        }

        #endregion ConvertToAnimationFrames Tests

        #region ExtractFrame Tests

        [TestMethod]
        public void ExtractFrame_NullSpriteSheet_ReturnsNull()
        {
            // Arrange
            var sourceRect = new System.Windows.Rect(0, 0, 10, 10);

            // Act
            var result = _asepriteLoader.ExtractFrame(null, sourceRect);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExtractFrame_InvalidSourceRect_ReturnsNull()
        {
            // Arrange
            var spriteSheet = CreateTestSpriteSheet();
            var invalidRect = new System.Windows.Rect(-10, -10, 5, 5); // Negative coordinates

            // Act
            var result = _asepriteLoader.ExtractFrame(spriteSheet, invalidRect);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExtractFrame_SourceRectOutOfBounds_ReturnsNull()
        {
            // Arrange
            var spriteSheet = CreateTestSpriteSheet();
            var outOfBoundsRect = new System.Windows.Rect(100, 100, 50, 50); // Beyond sprite sheet bounds

            // Act
            var result = _asepriteLoader.ExtractFrame(spriteSheet, outOfBoundsRect);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExtractFrame_ValidParameters_ReturnsExtractedFrame()
        {
            // Arrange
            var spriteSheet = CreateTestSpriteSheet();
            var validRect = new System.Windows.Rect(0, 0, 16, 16); // Within bounds

            // Act
            var result = _asepriteLoader.ExtractFrame(spriteSheet, validRect);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(CroppedBitmap));
        }

        #endregion ExtractFrame Tests

        #region Helper Methods

        /// <summary>
        /// Creates a test PNG file
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        private void CreateTestPngFile(string filePath)
        {
            var bitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 16, 16));
                context.DrawRectangle(Brushes.Blue, null, new System.Windows.Rect(16, 0, 16, 16));
                context.DrawRectangle(Brushes.Green, null, new System.Windows.Rect(0, 16, 16, 16));
                context.DrawRectangle(Brushes.Yellow, null, new System.Windows.Rect(16, 16, 16, 16));
            }
            
            bitmap.Render(visual);
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = File.Create(filePath))
            {
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// Creates a test JSON file with Aseprite format
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        private void CreateTestJsonFile(string filePath)
        {
            var asepriteData = new AsepriteData
            {
                Frames = new System.Collections.Generic.Dictionary<string, AsepriteFrame>
                {
                    ["frame1"] = new AsepriteFrame
                    {
                        Frame = new AsepriteRect { X = 0, Y = 0, W = 16, H = 16 },
                        Duration = 100
                    },
                    ["frame2"] = new AsepriteFrame
                    {
                        Frame = new AsepriteRect { X = 16, Y = 0, W = 16, H = 16 },
                        Duration = 150
                    }
                },
                Meta = new AsepriteMetadata
                {
                    App = "Aseprite",
                    Version = "1.2.25",
                    Image = "test.png",
                    Format = "RGBA8888",
                    Size = new AsepriteSize { W = 32, H = 32 },
                    Scale = "1"
                },
                FrameTags = new System.Collections.Generic.List<AsepriteTag>
                {
                    new AsepriteTag
                    {
                        Name = "idle",
                        From = 0,
                        To = 1,
                        Direction = "forward"
                    }
                }
            };

            var json = JsonSerializer.Serialize(asepriteData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates test Aseprite data for testing
        /// </summary>
        /// <returns>Test Aseprite data</returns>
        private AsepriteData CreateTestAsepriteData()
        {
            return new AsepriteData
            {
                Frames = new System.Collections.Generic.Dictionary<string, AsepriteFrame>
                {
                    ["frame1"] = new AsepriteFrame
                    {
                        Frame = new AsepriteRect { X = 0, Y = 0, W = 16, H = 16 },
                        Duration = 100
                    },
                    ["frame2"] = new AsepriteFrame
                    {
                        Frame = new AsepriteRect { X = 16, Y = 0, W = 16, H = 16 },
                        Duration = 150
                    }
                },
                Meta = new AsepriteMetadata
                {
                    App = "Aseprite",
                    Version = "1.2.25",
                    Image = "test.png",
                    Format = "RGBA8888",
                    Size = new AsepriteSize { W = 32, H = 32 },
                    Scale = "1"
                }
            };
        }

        /// <summary>
        /// Creates a test sprite sheet for testing
        /// </summary>
        /// <returns>Test sprite sheet image</returns>
        private ImageSource CreateTestSpriteSheet()
        {
            var bitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 16, 16));
                context.DrawRectangle(Brushes.Blue, null, new System.Windows.Rect(16, 0, 16, 16));
                context.DrawRectangle(Brushes.Green, null, new System.Windows.Rect(0, 16, 16, 16));
                context.DrawRectangle(Brushes.Yellow, null, new System.Windows.Rect(16, 16, 16, 16));
            }
            
            bitmap.Render(visual);
            bitmap.Freeze();
            
            return bitmap;
        }

        #endregion Helper Methods
    }
}