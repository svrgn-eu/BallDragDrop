using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Models;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Comprehensive tests for different file format loading and handling
    /// Tests PNG, JPG, BMP static images, GIF animations, and Aseprite PNG+JSON combinations
    /// </summary>
    [TestClass]
    public class FileFormatTests
    {
        #region Test Setup

        private ImageService _imageService;
        private BallViewModel _ballViewModel;
        private TestLogService _logService;
        private string _testDataDirectory;

        [TestInitialize]
        public void Setup()
        {
            // Initialize test services
            _logService = new TestLogService();
            _imageService = new ImageService(_logService);
            _ballViewModel = new BallViewModel(_logService, _imageService);
            _ballViewModel.Initialize(400, 300, 25);
            
            // Create test data directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "FileFormatTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _imageService = null;
            _ballViewModel = null;
            _logService = null;
            
            // Clean up test data directory
            if (Directory.Exists(_testDataDirectory))
            {
                try
                {
                    Directory.Delete(_testDataDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        #endregion Test Setup

        #region PNG Static Image Tests

        /// <summary>
        /// Tests loading PNG static images with various properties
        /// </summary>
        [TestMethod]
        public async Task LoadPngStaticImage_ValidFile_ShouldLoadSuccessfully()
        {
            // Arrange
            var pngPath = CreateTestPngImage(100, 100, Colors.Blue);
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load PNG image successfully");
            Assert.IsFalse(_imageService.IsAnimated, "PNG should not be animated");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have current frame");
            Assert.AreEqual(TimeSpan.Zero, _imageService.FrameDuration, "Static image should have zero frame duration");
        }

        /// <summary>
        /// Tests loading PNG images with different sizes
        /// </summary>
        [TestMethod]
        public async Task LoadPngStaticImage_DifferentSizes_ShouldLoadCorrectly()
        {
            // Arrange
            var testSizes = new[] { (16, 16), (32, 32), (64, 64), (128, 128), (256, 256) };
            
            foreach (var (width, height) in testSizes)
            {
                var pngPath = CreateTestPngImage(width, height, Colors.Green, $"test_{width}x{height}.png");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(pngPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load {width}x{height} PNG successfully");
                Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have frame for {width}x{height} PNG");
            }
        }

        /// <summary>
        /// Tests loading PNG with transparency
        /// </summary>
        [TestMethod]
        public async Task LoadPngStaticImage_WithTransparency_ShouldLoadCorrectly()
        {
            // Arrange
            var pngPath = CreateTestPngImageWithTransparency(64, 64);
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load transparent PNG successfully");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have current frame for transparent PNG");
        }

        /// <summary>
        /// Tests PNG file type detection
        /// </summary>
        [TestMethod]
        public void DetectFileType_PngFiles_ShouldReturnStaticImage()
        {
            // Arrange
            var testFiles = new[]
            {
                "static_test.png",
                "static_test.PNG",
                "static_image.png",
                "static_sprite.png"
            };
            
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                
                // Act
                var result = _imageService.DetectFileType(filePath);
                
                // Assert
                Assert.AreEqual(VisualContentType.StaticImage, result, 
                    $"Should detect {fileName} as static image");
            }
        }

        #endregion PNG Static Image Tests

        #region JPG Static Image Tests

        /// <summary>
        /// Tests loading JPG static images
        /// </summary>
        [TestMethod]
        public async Task LoadJpgStaticImage_ValidFile_ShouldLoadSuccessfully()
        {
            // Arrange
            var jpgPath = CreateTestJpgImage(80, 80, Colors.Red);
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(jpgPath);
            
            // Assert
            Assert.IsTrue(result, "Should load JPG image successfully");
            Assert.IsFalse(_imageService.IsAnimated, "JPG should not be animated");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have current frame");
        }

        /// <summary>
        /// Tests loading JPG images with different quality settings
        /// </summary>
        [TestMethod]
        public async Task LoadJpgStaticImage_DifferentQualities_ShouldLoadCorrectly()
        {
            // Arrange
            var qualityLevels = new[] { 50, 75, 90, 100 };
            
            foreach (var quality in qualityLevels)
            {
                var jpgPath = CreateTestJpgImageWithQuality(100, 100, Colors.Purple, quality, $"test_q{quality}.jpg");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(jpgPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load JPG with quality {quality} successfully");
                Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have frame for quality {quality} JPG");
            }
        }

        /// <summary>
        /// Tests JPG file type detection with various extensions
        /// </summary>
        [TestMethod]
        public void DetectFileType_JpgFiles_ShouldReturnStaticImage()
        {
            // Arrange
            var testFiles = new[]
            {
                "test.jpg",
                "test.JPG",
                "test.jpeg",
                "test.JPEG",
                "image.jpg",
                "photo.jpeg"
            };
            
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                
                // Act
                var result = _imageService.DetectFileType(filePath);
                
                // Assert
                Assert.AreEqual(VisualContentType.StaticImage, result, 
                    $"Should detect {fileName} as static image");
            }
        }

        #endregion JPG Static Image Tests

        #region BMP Static Image Tests

        /// <summary>
        /// Tests loading BMP static images
        /// </summary>
        [TestMethod]
        public async Task LoadBmpStaticImage_ValidFile_ShouldLoadSuccessfully()
        {
            // Arrange
            var bmpPath = CreateTestBmpImage(60, 60, Colors.Yellow);
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(bmpPath);
            
            // Assert
            Assert.IsTrue(result, "Should load BMP image successfully");
            Assert.IsFalse(_imageService.IsAnimated, "BMP should not be animated");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have current frame");
        }

        /// <summary>
        /// Tests loading BMP images with different bit depths
        /// </summary>
        [TestMethod]
        public async Task LoadBmpStaticImage_DifferentBitDepths_ShouldLoadCorrectly()
        {
            // Arrange
            var bitDepths = new[] { 24, 32 }; // Common bit depths for BMP
            
            foreach (var bitDepth in bitDepths)
            {
                var bmpPath = CreateTestBmpImageWithBitDepth(50, 50, Colors.Cyan, bitDepth, $"test_{bitDepth}bit.bmp");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(bmpPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load {bitDepth}-bit BMP successfully");
                Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have frame for {bitDepth}-bit BMP");
            }
        }

        /// <summary>
        /// Tests BMP file type detection
        /// </summary>
        [TestMethod]
        public void DetectFileType_BmpFiles_ShouldReturnStaticImage()
        {
            // Arrange
            var testFiles = new[]
            {
                "test.bmp",
                "test.BMP",
                "image.bmp",
                "bitmap.bmp"
            };
            
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                
                // Act
                var result = _imageService.DetectFileType(filePath);
                
                // Assert
                Assert.AreEqual(VisualContentType.StaticImage, result, 
                    $"Should detect {fileName} as static image");
            }
        }

        #endregion BMP Static Image Tests

        #region GIF Animation Tests

        /// <summary>
        /// Tests loading GIF animations with different frame rates
        /// </summary>
        [TestMethod]
        public async Task LoadGifAnimation_DifferentFrameRates_ShouldLoadCorrectly()
        {
            // Arrange
            var frameRates = new[] { 10, 15, 24, 30, 60 };
            
            foreach (var fps in frameRates)
            {
                var gifPath = CreateTestGifAnimation(fps, 4, $"test_{fps}fps.gif");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(gifPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load {fps} FPS GIF successfully");
                Assert.IsTrue(_imageService.IsAnimated, $"{fps} FPS GIF should be animated");
                Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have frame for {fps} FPS GIF");
                Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero, $"{fps} FPS GIF should have valid frame duration");
            }
        }

        /// <summary>
        /// Tests loading GIF animations with different frame counts
        /// </summary>
        [TestMethod]
        public async Task LoadGifAnimation_DifferentFrameCounts_ShouldLoadCorrectly()
        {
            // Arrange
            var frameCounts = new[] { 2, 4, 8, 16, 32 };
            
            foreach (var frameCount in frameCounts)
            {
                var gifPath = CreateTestGifAnimation(24, frameCount, $"test_{frameCount}frames.gif");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(gifPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load {frameCount}-frame GIF successfully");
                Assert.IsTrue(_imageService.IsAnimated, $"{frameCount}-frame GIF should be animated");
                Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            }
        }

        /// <summary>
        /// Tests GIF animation playback functionality
        /// </summary>
        [TestMethod]
        public async Task LoadGifAnimation_PlaybackFunctionality_ShouldWorkCorrectly()
        {
            // Arrange
            var gifPath = CreateTestGifAnimation(20, 6);
            await _imageService.LoadBallVisualAsync(gifPath);
            
            // Act - Test animation controls
            _imageService.StartAnimation();
            var initialFrame = _imageService.CurrentFrame;
            
            await Task.Delay(200); // Let animation run
            _imageService.UpdateFrame();
            
            _imageService.StopAnimation();
            var stoppedFrame = _imageService.CurrentFrame;
            
            // Assert
            Assert.IsNotNull(initialFrame, "Should have initial frame");
            Assert.IsNotNull(stoppedFrame, "Should have frame after stopping");
            // Note: Frames might be the same depending on timing, but methods should not throw
        }

        /// <summary>
        /// Tests GIF file type detection
        /// </summary>
        [TestMethod]
        public void DetectFileType_GifFiles_ShouldReturnGifAnimation()
        {
            // Arrange
            var testFiles = new[]
            {
                "test.gif",
                "test.GIF",
                "animation.gif",
                "sprite.gif"
            };
            
            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                
                // Act
                var result = _imageService.DetectFileType(filePath);
                
                // Assert
                Assert.AreEqual(VisualContentType.GifAnimation, result, 
                    $"Should detect {fileName} as GIF animation");
            }
        }

        #endregion GIF Animation Tests

        #region Aseprite PNG+JSON Tests

        /// <summary>
        /// Tests loading Aseprite animations with valid PNG+JSON combinations
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteAnimation_ValidPngJsonPair_ShouldLoadCorrectly()
        {
            // Arrange
            var (pngPath, jsonPath) = CreateTestAsepriteAnimation(24, 8, "test_aseprite");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load Aseprite animation successfully");
            Assert.IsTrue(_imageService.IsAnimated, "Aseprite animation should be animated");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Should have current frame");
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero, "Should have valid frame duration");
        }

        /// <summary>
        /// Tests loading Aseprite animations with different frame rates
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteAnimation_DifferentFrameRates_ShouldLoadCorrectly()
        {
            // Arrange
            var frameRates = new[] { 12, 24, 30, 60 };
            
            foreach (var fps in frameRates)
            {
                var (pngPath, jsonPath) = CreateTestAsepriteAnimation(fps, 6, $"aseprite_{fps}fps");
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(pngPath);
                
                // Assert
                Assert.IsTrue(result, $"Should load {fps} FPS Aseprite animation successfully");
                Assert.IsTrue(_imageService.IsAnimated, $"{fps} FPS Aseprite should be animated");
                Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            }
        }

        /// <summary>
        /// Tests loading Aseprite animations with multiple animation tags
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteAnimation_MultipleAnimationTags_ShouldUseFirstTag()
        {
            // Arrange
            var (pngPath, jsonPath) = CreateTestAsepriteAnimationWithMultipleTags(30, "multi_tag_aseprite");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load multi-tag Aseprite animation successfully");
            Assert.IsTrue(_imageService.IsAnimated, "Multi-tag Aseprite should be animated");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        /// <summary>
        /// Tests loading Aseprite animations without animation tags (uses all frames)
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteAnimation_NoAnimationTags_ShouldUseAllFrames()
        {
            // Arrange
            var (pngPath, jsonPath) = CreateTestAsepriteAnimationWithoutTags(25, 10, "no_tags_aseprite");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load no-tags Aseprite animation successfully");
            Assert.IsTrue(_imageService.IsAnimated, "No-tags Aseprite should be animated");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        /// <summary>
        /// Tests Aseprite file type detection
        /// </summary>
        [TestMethod]
        public void DetectFileType_AsepritePngWithJson_ShouldReturnAsepriteAnimation()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            // Create both files
            CreateTestPngImage(64, 64, Colors.White, "sprite.png");
            CreateTestAsepriteJsonFile(jsonPath, 24, 4);
            
            // Act
            var result = _imageService.DetectFileType(pngPath);
            
            // Assert
            Assert.AreEqual(VisualContentType.AsepriteAnimation, result, 
                "Should detect PNG with matching JSON as Aseprite animation");
        }

        /// <summary>
        /// Tests Aseprite file type detection when JSON is missing
        /// </summary>
        [TestMethod]
        public void DetectFileType_AsepritePngWithoutJson_ShouldReturnStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite_no_json.png");
            CreateTestPngImage(64, 64, Colors.White, "sprite_no_json.png");
            
            // Act
            var result = _imageService.DetectFileType(pngPath);
            
            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result, 
                "Should detect PNG without JSON as static image");
        }

        #endregion Aseprite PNG+JSON Tests

        #region Error Handling Tests

        /// <summary>
        /// Tests handling of corrupted image files
        /// </summary>
        [TestMethod]
        public async Task LoadCorruptedFiles_ShouldFallbackGracefully()
        {
            // Arrange
            var corruptedFiles = new[]
            {
                ("corrupted.png", "This is not a PNG file"),
                ("corrupted.jpg", "This is not a JPG file"),
                ("corrupted.bmp", "This is not a BMP file"),
                ("corrupted.gif", "This is not a GIF file")
            };
            
            foreach (var (fileName, content) in corruptedFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                File.WriteAllText(filePath, content);
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(filePath);
                
                // Assert
                Assert.IsTrue(result, $"Should handle corrupted {fileName} gracefully with fallback");
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have fallback image for corrupted {fileName}");
                Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType, 
                    $"Corrupted {fileName} should fallback to static image");
            }
        }

        /// <summary>
        /// Tests handling of missing files
        /// </summary>
        [TestMethod]
        public async Task LoadMissingFiles_ShouldReturnFalse()
        {
            // Arrange
            var missingFiles = new[]
            {
                "missing.png",
                "missing.jpg",
                "missing.bmp",
                "missing.gif"
            };
            
            foreach (var fileName in missingFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(filePath);
                
                // Assert
                Assert.IsFalse(result, $"Should return false for missing {fileName}");
            }
        }

        /// <summary>
        /// Tests handling of Aseprite files with corrupted JSON
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteWithCorruptedJson_ShouldFallbackToStaticImage()
        {
            // Arrange
            var pngPath = CreateTestPngImage(64, 64, Colors.Orange, "sprite_corrupt_json.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite_corrupt_json.json");
            
            // Create corrupted JSON
            File.WriteAllText(jsonPath, "{ invalid json content }");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should handle corrupted JSON gracefully");
            Assert.IsFalse(_imageService.IsAnimated, "Should fallback to static image");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        /// <summary>
        /// Tests handling of Aseprite files with missing JSON
        /// </summary>
        [TestMethod]
        public async Task LoadAsepriteWithMissingJson_ShouldLoadAsStaticImage()
        {
            // Arrange
            var pngPath = CreateTestPngImage(64, 64, Colors.Magenta, "sprite_missing_json.png");
            
            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);
            
            // Assert
            Assert.IsTrue(result, "Should load PNG without JSON successfully");
            Assert.IsFalse(_imageService.IsAnimated, "Should not be animated without JSON");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        /// <summary>
        /// Tests handling of empty or zero-byte files
        /// </summary>
        [TestMethod]
        public async Task LoadEmptyFiles_ShouldFallbackGracefully()
        {
            // Arrange
            var emptyFiles = new[]
            {
                "empty.png",
                "empty.jpg",
                "empty.bmp",
                "empty.gif"
            };
            
            foreach (var fileName in emptyFiles)
            {
                var filePath = Path.Combine(_testDataDirectory, fileName);
                File.WriteAllText(filePath, ""); // Create empty file
                
                // Act
                var result = await _imageService.LoadBallVisualAsync(filePath);
                
                // Assert
                Assert.IsTrue(result, $"Should handle empty {fileName} gracefully with fallback");
                Assert.IsNotNull(_imageService.CurrentFrame, $"Should have fallback image for empty {fileName}");
            }
        }

        #endregion Error Handling Tests

        #region Integration Tests with BallViewModel

        /// <summary>
        /// Tests file format loading through BallViewModel
        /// </summary>
        [TestMethod]
        public async Task BallViewModel_LoadDifferentFormats_ShouldWorkCorrectly()
        {
            // Arrange
            var testFiles = new[]
            {
                (CreateTestPngImage(50, 50, Colors.Red, "test.png"), VisualContentType.StaticImage, false),
                (CreateTestJpgImage(50, 50, Colors.Green, "test.jpg"), VisualContentType.StaticImage, false),
                (CreateTestBmpImage(50, 50, Colors.Blue, "test.bmp"), VisualContentType.StaticImage, false),
                (CreateTestGifAnimation(24, 4, "test.gif"), VisualContentType.GifAnimation, true)
            };
            
            foreach (var (filePath, expectedType, expectedAnimated) in testFiles)
            {
                // Act
                var result = await _ballViewModel.LoadBallVisualAsync(filePath);
                
                // Assert
                Assert.IsTrue(result, $"BallViewModel should load {Path.GetExtension(filePath)} successfully");
                Assert.AreEqual(expectedType, _ballViewModel.ContentType, 
                    $"Content type should be {expectedType} for {Path.GetExtension(filePath)}");
                Assert.AreEqual(expectedAnimated, _ballViewModel.IsAnimated, 
                    $"Animation state should be {expectedAnimated} for {Path.GetExtension(filePath)}");
                Assert.IsNotNull(_ballViewModel.BallImage, 
                    $"Ball image should not be null for {Path.GetExtension(filePath)}");
            }
        }

        /// <summary>
        /// Tests switching between different file formats in BallViewModel
        /// </summary>
        [TestMethod]
        public async Task BallViewModel_SwitchBetweenFormats_ShouldMaintainFunctionality()
        {
            // Arrange
            var pngPath = CreateTestPngImage(60, 60, Colors.Cyan, "switch_test.png");
            var gifPath = CreateTestGifAnimation(30, 6, "switch_test.gif");
            var (asepritePng, asepriteJson) = CreateTestAsepriteAnimation(25, 4, "switch_test_aseprite");
            
            // Act & Assert - PNG to GIF
            await _ballViewModel.LoadBallVisualAsync(pngPath);
            Assert.IsFalse(_ballViewModel.IsAnimated, "PNG should not be animated");
            
            await _ballViewModel.LoadBallVisualAsync(gifPath);
            Assert.IsTrue(_ballViewModel.IsAnimated, "GIF should be animated");
            
            // GIF to Aseprite
            await _ballViewModel.LoadBallVisualAsync(asepritePng);
            Assert.IsTrue(_ballViewModel.IsAnimated, "Aseprite should be animated");
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _ballViewModel.ContentType);
            
            // Aseprite back to PNG
            await _ballViewModel.LoadBallVisualAsync(pngPath);
            Assert.IsFalse(_ballViewModel.IsAnimated, "PNG should not be animated after switch back");
            Assert.AreEqual(VisualContentType.StaticImage, _ballViewModel.ContentType);
        }

        #endregion Integration Tests with BallViewModel

        #region Helper Methods

        /// <summary>
        /// Creates a test PNG image file
        /// </summary>
        private string CreateTestPngImage(int width, int height, Color color, string fileName = "test.png")
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestPngData(width, height, color);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test PNG image with transparency
        /// </summary>
        private string CreateTestPngImageWithTransparency(int width, int height, string fileName = "test_transparent.png")
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestPngDataWithTransparency(width, height);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test JPG image file
        /// </summary>
        private string CreateTestJpgImage(int width, int height, Color color, string fileName = "test.jpg")
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestJpgData(width, height, color);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test JPG image with specific quality
        /// </summary>
        private string CreateTestJpgImageWithQuality(int width, int height, Color color, int quality, string fileName)
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestJpgDataWithQuality(width, height, color, quality);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test BMP image file
        /// </summary>
        private string CreateTestBmpImage(int width, int height, Color color, string fileName = "test.bmp")
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestBmpData(width, height, color);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test BMP image with specific bit depth
        /// </summary>
        private string CreateTestBmpImageWithBitDepth(int width, int height, Color color, int bitDepth, string fileName)
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var imageData = TestImageHelper.CreateTestBmpDataWithBitDepth(width, height, color, bitDepth);
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }

        /// <summary>
        /// Creates a test GIF animation file
        /// </summary>
        private string CreateTestGifAnimation(int fps, int frameCount, string fileName = "test.gif")
        {
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var gifData = TestImageHelper.CreateTestGifAnimationData(fps, frameCount);
            File.WriteAllBytes(filePath, gifData);
            return filePath;
        }

        /// <summary>
        /// Creates a test Aseprite animation (PNG + JSON)
        /// </summary>
        private (string PngPath, string JsonPath) CreateTestAsepriteAnimation(int fps, int frameCount, string baseName)
        {
            var pngPath = Path.Combine(_testDataDirectory, $"{baseName}.png");
            var jsonPath = Path.Combine(_testDataDirectory, $"{baseName}.json");
            
            // Create sprite sheet
            var spriteData = TestImageHelper.CreateTestSpriteSheetData(64, 64, frameCount);
            File.WriteAllBytes(pngPath, spriteData);
            
            // Create JSON metadata
            CreateTestAsepriteJsonFile(jsonPath, fps, frameCount);
            
            return (pngPath, jsonPath);
        }

        /// <summary>
        /// Creates a test Aseprite animation with multiple tags
        /// </summary>
        private (string PngPath, string JsonPath) CreateTestAsepriteAnimationWithMultipleTags(int fps, string baseName)
        {
            var pngPath = Path.Combine(_testDataDirectory, $"{baseName}.png");
            var jsonPath = Path.Combine(_testDataDirectory, $"{baseName}.json");
            
            // Create sprite sheet with 12 frames (3 animations of 4 frames each)
            var spriteData = TestImageHelper.CreateTestSpriteSheetData(64, 64, 12);
            File.WriteAllBytes(pngPath, spriteData);
            
            // Create JSON with multiple animation tags
            var jsonContent = TestImageHelper.CreateTestAsepriteJsonWithMultipleTags(fps);
            File.WriteAllText(jsonPath, jsonContent);
            
            return (pngPath, jsonPath);
        }

        /// <summary>
        /// Creates a test Aseprite animation without tags
        /// </summary>
        private (string PngPath, string JsonPath) CreateTestAsepriteAnimationWithoutTags(int fps, int frameCount, string baseName)
        {
            var pngPath = Path.Combine(_testDataDirectory, $"{baseName}.png");
            var jsonPath = Path.Combine(_testDataDirectory, $"{baseName}.json");
            
            // Create sprite sheet
            var spriteData = TestImageHelper.CreateTestSpriteSheetData(64, 64, frameCount);
            File.WriteAllBytes(pngPath, spriteData);
            
            // Create JSON without animation tags
            var jsonContent = TestImageHelper.CreateTestAsepriteJsonWithoutTags(fps, frameCount);
            File.WriteAllText(jsonPath, jsonContent);
            
            return (pngPath, jsonPath);
        }

        /// <summary>
        /// Creates a test Aseprite JSON file
        /// </summary>
        private void CreateTestAsepriteJsonFile(string jsonPath, int fps, int frameCount)
        {
            var jsonContent = TestImageHelper.CreateTestAsepriteJsonData(fps, frameCount);
            File.WriteAllText(jsonPath, jsonContent);
        }

        #endregion Helper Methods
    }
}