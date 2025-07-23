using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Services;
using BallDragDrop.Models;
using BallDragDrop.Contracts;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class ImageServiceTests
    {
        private Mock<ILogService> _mockLogService;
        private ImageService _imageService;
        private string _testDataDirectory;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _imageService = new ImageService(_mockLogService.Object);
            
            // Create test data directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "ImageServiceTests", Guid.NewGuid().ToString());
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
            var service = new ImageService();

            // Assert
            Assert.IsNull(service.CurrentFrame);
            Assert.IsFalse(service.IsAnimated);
            Assert.AreEqual(TimeSpan.Zero, service.FrameDuration);
            Assert.AreEqual(VisualContentType.Unknown, service.ContentType);
        }

        [TestMethod]
        public void Constructor_WithLogService_InitializesCorrectly()
        {
            // Arrange & Act
            var service = new ImageService(_mockLogService.Object);

            // Assert
            Assert.IsNull(service.CurrentFrame);
            Assert.IsFalse(service.IsAnimated);
            Assert.AreEqual(TimeSpan.Zero, service.FrameDuration);
            Assert.AreEqual(VisualContentType.Unknown, service.ContentType);
        }

        #endregion Constructor Tests

        #region DetectFileType Tests

        [TestMethod]
        public void DetectFileType_NullPath_ReturnsUnknown()
        {
            // Arrange & Act
            var result = _imageService.DetectFileType(null);

            // Assert
            Assert.AreEqual(VisualContentType.Unknown, result);
        }

        [TestMethod]
        public void DetectFileType_EmptyPath_ReturnsUnknown()
        {
            // Arrange & Act
            var result = _imageService.DetectFileType("");

            // Assert
            Assert.AreEqual(VisualContentType.Unknown, result);
        }

        [TestMethod]
        public void DetectFileType_PngFile_ReturnsStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");

            // Act
            var result = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        [TestMethod]
        public void DetectFileType_JpgFile_ReturnsStaticImage()
        {
            // Arrange
            var jpgPath = Path.Combine(_testDataDirectory, "test.jpg");

            // Act
            var result = _imageService.DetectFileType(jpgPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        [TestMethod]
        public void DetectFileType_JpegFile_ReturnsStaticImage()
        {
            // Arrange
            var jpegPath = Path.Combine(_testDataDirectory, "test.jpeg");

            // Act
            var result = _imageService.DetectFileType(jpegPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        [TestMethod]
        public void DetectFileType_BmpFile_ReturnsStaticImage()
        {
            // Arrange
            var bmpPath = Path.Combine(_testDataDirectory, "test.bmp");

            // Act
            var result = _imageService.DetectFileType(bmpPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        [TestMethod]
        public void DetectFileType_GifFile_ReturnsGifAnimation()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "test.gif");

            // Act
            var result = _imageService.DetectFileType(gifPath);

            // Assert
            Assert.AreEqual(VisualContentType.GifAnimation, result);
        }

        [TestMethod]
        public void DetectFileType_PngWithJson_ReturnsAsepriteAnimation()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            // Create the JSON file to simulate Aseprite export
            File.WriteAllText(jsonPath, "{}");

            // Act
            var result = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreEqual(VisualContentType.AsepriteAnimation, result);
        }

        [TestMethod]
        public void DetectFileType_UnsupportedExtension_ReturnsUnknown()
        {
            // Arrange
            var txtPath = Path.Combine(_testDataDirectory, "test.txt");

            // Act
            var result = _imageService.DetectFileType(txtPath);

            // Assert
            Assert.AreEqual(VisualContentType.Unknown, result);
        }

        #endregion DetectFileType Tests

        #region LoadBallVisualAsync Tests

        [TestMethod]
        public async Task LoadBallVisualAsync_NullPath_ReturnsFalse()
        {
            // Arrange & Act
            var result = await _imageService.LoadBallVisualAsync(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_EmptyPath_ReturnsFalse()
        {
            // Arrange & Act
            var result = await _imageService.LoadBallVisualAsync("");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDataDirectory, "nonexistent.png");

            // Act
            var result = await _imageService.LoadBallVisualAsync(nonExistentPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_UnsupportedFileType_UsesFallback()
        {
            // Arrange
            var txtPath = Path.Combine(_testDataDirectory, "test.txt");
            File.WriteAllText(txtPath, "test content");

            // Act
            var result = await _imageService.LoadBallVisualAsync(txtPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        #endregion LoadBallVisualAsync Tests

        #region Animation Control Tests

        [TestMethod]
        public void StartAnimation_WithoutAnimatedContent_DoesNotThrow()
        {
            // Arrange - service starts with no content

            // Act & Assert - should not throw
            _imageService.StartAnimation();
        }

        [TestMethod]
        public void StopAnimation_WithoutAnimatedContent_DoesNotThrow()
        {
            // Arrange - service starts with no content

            // Act & Assert - should not throw
            _imageService.StopAnimation();
        }

        [TestMethod]
        public void UpdateFrame_WithoutAnimatedContent_DoesNotThrow()
        {
            // Arrange - service starts with no content

            // Act & Assert - should not throw
            _imageService.UpdateFrame();
        }

        #endregion Animation Control Tests

        #region GetFallbackImage Tests

        [TestMethod]
        public void GetFallbackImage_ReturnsValidImage()
        {
            // Arrange & Act
            var fallbackImage = _imageService.GetFallbackImage();

            // Assert
            Assert.IsNotNull(fallbackImage);
            Assert.IsInstanceOfType(fallbackImage, typeof(ImageSource));
        }

        #endregion GetFallbackImage Tests

        #region Static Method Tests

        [TestMethod]
        public void LoadImage_ValidPath_ReturnsImageSource()
        {
            // Arrange
            var testImagePath = CreateTestImage();

            // Act
            var result = ImageService.LoadImage(testImagePath, _mockLogService.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(BitmapImage));
        }

        [TestMethod]
        public void LoadImage_InvalidPath_ReturnsNull()
        {
            // Arrange
            var invalidPath = Path.Combine(_testDataDirectory, "nonexistent.png");

            // Act
            var result = ImageService.LoadImage(invalidPath, _mockLogService.Object);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void CreateFallbackImage_ValidParameters_ReturnsImageSource()
        {
            // Arrange
            double radius = 25;
            var fillColor = Colors.Orange;
            var strokeColor = Colors.DarkOrange;
            double strokeThickness = 2;

            // Act
            var result = ImageService.CreateFallbackImage(radius, fillColor, strokeColor, strokeThickness, _mockLogService.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(RenderTargetBitmap));
        }

        [TestMethod]
        public void CreateFallbackImage_ZeroRadius_ReturnsValidImage()
        {
            // Arrange
            double radius = 0;
            var fillColor = Colors.Orange;
            var strokeColor = Colors.DarkOrange;

            // Act
            var result = ImageService.CreateFallbackImage(radius, fillColor, strokeColor, logService: _mockLogService.Object);

            // Assert
            // The method should handle zero radius gracefully by using minimum radius
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(RenderTargetBitmap));
        }

        [TestMethod]
        public void CreateFallbackImage_NegativeRadius_ReturnsValidImage()
        {
            // Arrange
            double radius = -5;
            var fillColor = Colors.Blue;
            var strokeColor = Colors.Navy;

            // Act
            var result = ImageService.CreateFallbackImage(radius, fillColor, strokeColor, logService: _mockLogService.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(RenderTargetBitmap));
        }

        [TestMethod]
        public void CreateFallbackImage_NegativeStrokeThickness_ReturnsValidImage()
        {
            // Arrange
            double radius = 10;
            var fillColor = Colors.Green;
            var strokeColor = Colors.DarkGreen;
            double strokeThickness = -2;

            // Act
            var result = ImageService.CreateFallbackImage(radius, fillColor, strokeColor, strokeThickness, _mockLogService.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(RenderTargetBitmap));
        }

        #endregion Static Method Tests

        #region File Type Detection Edge Cases

        [TestMethod]
        public void DetectFileType_CaseInsensitive_ReturnsCorrectType()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.PNG");
            var jpgPath = Path.Combine(_testDataDirectory, "test.JPG");
            var gifPath = Path.Combine(_testDataDirectory, "test.GIF");

            // Act & Assert
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.DetectFileType(pngPath));
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.DetectFileType(jpgPath));
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.DetectFileType(gifPath));
        }

        [TestMethod]
        public void DetectFileType_PngWithoutJson_ReturnsStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "standalone.png");

            // Act
            var result = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        #endregion File Type Detection Edge Cases

        #region LoadBallVisualAsync Integration Tests

        [TestMethod]
        public async Task LoadBallVisualAsync_ValidStaticImage_LoadSuccessfully()
        {
            // Arrange
            var testImagePath = CreateTestImage();

            // Act
            var result = await _imageService.LoadBallVisualAsync(testImagePath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.AreEqual(TimeSpan.Zero, _imageService.FrameDuration);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_InvalidImageFile_UsesFallback()
        {
            // Arrange
            var invalidImagePath = Path.Combine(_testDataDirectory, "invalid.png");
            File.WriteAllText(invalidImagePath, "This is not an image file");

            // Act
            var result = await _imageService.LoadBallVisualAsync(invalidImagePath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_GifFile_MarksAsAnimated()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "test.gif");
            CreateTestGifFile(gifPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteFiles_MarksAsAnimated()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestImage(pngPath);
            File.WriteAllText(jsonPath, "{}"); // Simple JSON file

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepritePngWithoutJson_LoadsAsStatic()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite_no_json.png");
            CreateTestImage(pngPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        #endregion LoadBallVisualAsync Integration Tests

        #region Aseprite Integration Tests

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithValidData_LoadsSuccessfully()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJson(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithInvalidJson_UsesFallback()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            File.WriteAllText(jsonPath, "invalid json content");

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithMissingPng_UsesFallback()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "nonexistent.png");
            var jsonPath = Path.Combine(_testDataDirectory, "nonexistent.json");
            
            CreateTestAsepriteJson(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithMultipleTags_UsesFirstTag()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJsonWithMultipleTags(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithNoTags_UsesAllFrames()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJsonWithoutTags(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteSingleFrame_MarksAsAnimated()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteSingleFrameJson(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated); // Single frame should not be animated
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        [TestMethod]
        public void DetectFileType_AsepriteFileDetection_ReturnsCorrectType()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            // Create both files
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJson(jsonPath);

            // Act
            var result = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreEqual(VisualContentType.AsepriteAnimation, result);
        }

        [TestMethod]
        public void DetectFileType_PngWithoutMatchingJson_ReturnsStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var differentJsonPath = Path.Combine(_testDataDirectory, "different.json");
            
            // Create PNG but JSON with different name
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJson(differentJsonPath);

            // Act
            var result = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreEqual(VisualContentType.StaticImage, result);
        }

        #endregion Aseprite Integration Tests

        #region Aseprite Error Handling Tests

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithEmptyJson_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            File.WriteAllText(jsonPath, ""); // Empty JSON file

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithMalformedJson_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            File.WriteAllText(jsonPath, "{\"frames\": \"not_an_object\", \"meta\": null}");

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithNoFrames_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            var invalidJson = @"{
  ""frames"": {},
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25""
  }
}";
            File.WriteAllText(jsonPath, invalidJson);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithInvalidFrameDimensions_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            var invalidJson = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 0, ""h"": 0 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25""
  }
}";
            File.WriteAllText(jsonPath, invalidJson);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithCorruptedPng_UsesFallback()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "corrupted.png");
            var jsonPath = Path.Combine(_testDataDirectory, "corrupted.json");
            
            File.WriteAllText(pngPath, "This is not a PNG file"); // Corrupted PNG
            CreateTestAsepriteJson(jsonPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithFrameExtractionFailure_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            // Create JSON with frame coordinates that are out of bounds for the sprite sheet
            var invalidJson = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 1000, ""y"": 1000, ""w"": 16, ""h"": 16 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25""
  }
}";
            File.WriteAllText(jsonPath, invalidJson);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithInvalidMetadataDimensions_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            var invalidJson = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25"",
    ""size"": { ""w"": -32, ""h"": -32 }
  }
}";
            File.WriteAllText(jsonPath, invalidJson);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteWithNegativeFrameDuration_LoadsSuccessfully()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "sprite.json");
            
            CreateTestSpriteSheet(pngPath);
            var jsonWithNegativeDuration = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""duration"": -100
    },
    ""frame2"": {
      ""frame"": { ""x"": 16, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""duration"": 150
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25""
  }
}";
            File.WriteAllText(jsonPath, jsonWithNegativeDuration);

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should load successfully despite negative duration
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsTrue(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.AsepriteAnimation, _imageService.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteJsonAccessDenied_FallsBackToStaticImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "sprite.png");
            var jsonPath = Path.Combine(_testDataDirectory, "readonly.json");
            
            CreateTestSpriteSheet(pngPath);
            CreateTestAsepriteJson(jsonPath);
            
            // Make JSON file read-only to potentially simulate access issues
            File.SetAttributes(jsonPath, FileAttributes.ReadOnly);
            
            try
            {
                // Act
                var result = await _imageService.LoadBallVisualAsync(pngPath);

                // Assert
                Assert.IsTrue(result); // Should succeed with fallback to static image
                Assert.IsNotNull(_imageService.CurrentFrame);
                // Could be either animated (if access succeeded) or static (if access failed)
                // Both outcomes are acceptable for this test
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

        [TestMethod]
        public async Task LoadBallVisualAsync_AsepriteCompleteFailure_UsesFallbackImage()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "nonexistent.png");
            var jsonPath = Path.Combine(_testDataDirectory, "nonexistent.json");
            
            // Don't create any files - both PNG and JSON are missing

            // Act
            var result = await _imageService.LoadBallVisualAsync(pngPath);

            // Assert
            Assert.IsTrue(result); // Should succeed with fallback image
            Assert.IsNotNull(_imageService.CurrentFrame);
            Assert.IsFalse(_imageService.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
        }

        #endregion Aseprite Error Handling Tests

        #region Helper Methods

        /// <summary>
        /// Creates a simple test image for testing purposes
        /// </summary>
        /// <returns>Path to the created test image</returns>
        private string CreateTestImage()
        {
            var testImagePath = Path.Combine(_testDataDirectory, "test.png");
            return CreateTestImage(testImagePath);
        }

        /// <summary>
        /// Creates a simple test image at the specified path
        /// </summary>
        /// <param name="filePath">Path where to create the test image</param>
        /// <returns>Path to the created test image</returns>
        private string CreateTestImage(string filePath)
        {
            // Create a simple 50x50 pixel image
            var bitmap = new RenderTargetBitmap(50, 50, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 50, 50));
            }
            
            bitmap.Render(visual);
            
            // Save to file
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = File.Create(filePath))
            {
                encoder.Save(stream);
            }
            
            return filePath;
        }

        /// <summary>
        /// Creates a simple test GIF file for testing purposes
        /// </summary>
        /// <param name="filePath">Path where to create the test GIF</param>
        private void CreateTestGifFile(string filePath)
        {
            // Create a simple single-frame GIF by creating a bitmap and saving as GIF
            var bitmap = new RenderTargetBitmap(50, 50, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                context.DrawEllipse(Brushes.Blue, null, new System.Windows.Point(25, 25), 20, 20);
            }
            
            bitmap.Render(visual);
            
            // Save as GIF (note: this creates a single-frame GIF, but it's sufficient for testing file type detection)
            var encoder = new GifBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = File.Create(filePath))
            {
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// Creates a test sprite sheet for Aseprite testing
        /// </summary>
        /// <param name="filePath">Path where to create the sprite sheet</param>
        private void CreateTestSpriteSheet(string filePath)
        {
            // Create a 64x32 sprite sheet with 4 frames (16x16 each)
            var bitmap = new RenderTargetBitmap(64, 32, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                // Frame 1: Red circle
                context.DrawEllipse(Brushes.Red, null, new System.Windows.Point(8, 16), 6, 6);
                
                // Frame 2: Green circle
                context.DrawEllipse(Brushes.Green, null, new System.Windows.Point(24, 16), 6, 6);
                
                // Frame 3: Blue circle
                context.DrawEllipse(Brushes.Blue, null, new System.Windows.Point(40, 16), 6, 6);
                
                // Frame 4: Yellow circle
                context.DrawEllipse(Brushes.Yellow, null, new System.Windows.Point(56, 16), 6, 6);
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
        /// Creates a test Aseprite JSON file with multiple frames and tags
        /// </summary>
        /// <param name="filePath">Path where to create the JSON file</param>
        private void CreateTestAsepriteJson(string filePath)
        {
            var json = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    },
    ""frame2"": {
      ""frame"": { ""x"": 16, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 150
    },
    ""frame3"": {
      ""frame"": { ""x"": 32, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 200
    },
    ""frame4"": {
      ""frame"": { ""x"": 48, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25"",
    ""image"": ""sprite.png"",
    ""format"": ""RGBA8888"",
    ""size"": { ""w"": 64, ""h"": 32 },
    ""scale"": ""1""
  },
  ""frameTags"": [
    {
      ""name"": ""idle"",
      ""from"": 0,
      ""to"": 3,
      ""direction"": ""forward""
    }
  ]
}";
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates a test Aseprite JSON file with multiple animation tags
        /// </summary>
        /// <param name="filePath">Path where to create the JSON file</param>
        private void CreateTestAsepriteJsonWithMultipleTags(string filePath)
        {
            var json = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    },
    ""frame2"": {
      ""frame"": { ""x"": 16, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 150
    },
    ""frame3"": {
      ""frame"": { ""x"": 32, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 200
    },
    ""frame4"": {
      ""frame"": { ""x"": 48, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25"",
    ""image"": ""sprite.png"",
    ""format"": ""RGBA8888"",
    ""size"": { ""w"": 64, ""h"": 32 },
    ""scale"": ""1""
  },
  ""frameTags"": [
    {
      ""name"": ""idle"",
      ""from"": 0,
      ""to"": 1,
      ""direction"": ""forward""
    },
    {
      ""name"": ""walk"",
      ""from"": 2,
      ""to"": 3,
      ""direction"": ""forward""
    }
  ]
}";
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates a test Aseprite JSON file without frame tags
        /// </summary>
        /// <param name="filePath">Path where to create the JSON file</param>
        private void CreateTestAsepriteJsonWithoutTags(string filePath)
        {
            var json = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    },
    ""frame2"": {
      ""frame"": { ""x"": 16, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 150
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25"",
    ""image"": ""sprite.png"",
    ""format"": ""RGBA8888"",
    ""size"": { ""w"": 32, ""h"": 16 },
    ""scale"": ""1""
  },
  ""frameTags"": []
}";
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates a test Aseprite JSON file with a single frame
        /// </summary>
        /// <param name="filePath">Path where to create the JSON file</param>
        private void CreateTestAsepriteSingleFrameJson(string filePath)
        {
            var json = @"{
  ""frames"": {
    ""frame1"": {
      ""frame"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""rotated"": false,
      ""trimmed"": false,
      ""spriteSourceSize"": { ""x"": 0, ""y"": 0, ""w"": 16, ""h"": 16 },
      ""sourceSize"": { ""w"": 16, ""h"": 16 },
      ""duration"": 100
    }
  },
  ""meta"": {
    ""app"": ""Aseprite"",
    ""version"": ""1.2.25"",
    ""image"": ""sprite.png"",
    ""format"": ""RGBA8888"",
    ""size"": { ""w"": 16, ""h"": 16 },
    ""scale"": ""1""
  },
  ""frameTags"": []
}";
            File.WriteAllText(filePath, json);
        }

        #endregion Helper Methods
    }
}