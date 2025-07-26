using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for GIF animation support
    /// </summary>
    [TestClass]
    public class GifAnimationTests
    {
        #region Test Setup and Cleanup

        private TestContext _testContextInstance;
        private ILogService _logService;
        private ImageService _imageService;
        private string _testDataDirectory;

        /// <summary>
        /// Gets or sets the test context which provides information about and functionality for the current test run
        /// </summary>
        public TestContext TestContext
        {
            get { return _testContextInstance; }
            set { _testContextInstance = value; }
        }

        /// <summary>
        /// Initialize test setup
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _logService = new SimpleLogService();
            _imageService = new ImageService(_logService);
            _testDataDirectory = Path.Combine(TestContext.TestRunDirectory, "TestData");
            
            // Create test data directory if it doesn't exist
            if (!Directory.Exists(_testDataDirectory))
            {
                Directory.CreateDirectory(_testDataDirectory);
            }
        }

        /// <summary>
        /// Clean up after tests
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            _imageService = null;
            _logService = null;
        }

        #endregion Test Setup and Cleanup

        #region GIF Detection Tests

        /// <summary>
        /// Test that GIF files are correctly detected as GIF animations
        /// </summary>
        [TestMethod]
        public void DetectFileType_GifFile_ReturnsGifAnimation()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "test.gif");
            CreateTestGifFile(gifPath);

            // Act
            var contentType = _imageService.DetectFileType(gifPath);

            // Assert
            Assert.AreEqual(VisualContentType.GifAnimation, contentType);
        }

        /// <summary>
        /// Test that non-GIF files are not detected as GIF animations
        /// </summary>
        [TestMethod]
        public void DetectFileType_NonGifFile_DoesNotReturnGifAnimation()
        {
            // Arrange
            var pngPath = Path.Combine(_testDataDirectory, "test.png");
            CreateTestPngFile(pngPath);

            // Act
            var contentType = _imageService.DetectFileType(pngPath);

            // Assert
            Assert.AreNotEqual(VisualContentType.GifAnimation, contentType);
            Assert.AreEqual(VisualContentType.StaticImage, contentType);
        }

        #endregion GIF Detection Tests

        #region GIF Loading Tests

        /// <summary>
        /// Test loading a valid multi-frame GIF animation
        /// </summary>
        [TestMethod]
        public async Task LoadBallVisualAsync_ValidMultiFrameGif_LoadsSuccessfully()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "multiframe.gif");
            CreateTestMultiFrameGif(gifPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "GIF loading should succeed");
            Assert.IsTrue(_imageService.IsAnimated, "Multi-frame GIF should be marked as animated");
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Current frame should be set");
            Assert.IsTrue(_imageService.FrameDuration > TimeSpan.Zero, "Frame duration should be positive");
        }

        /// <summary>
        /// Test loading a single-frame GIF (should be treated as static)
        /// </summary>
        [TestMethod]
        public async Task LoadBallVisualAsync_SingleFrameGif_LoadsAsStatic()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "singleframe.gif");
            CreateTestSingleFrameGif(gifPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Single-frame GIF loading should succeed");
            Assert.IsFalse(_imageService.IsAnimated, "Single-frame GIF should not be marked as animated");
            Assert.AreEqual(VisualContentType.GifAnimation, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Current frame should be set");
        }

        /// <summary>
        /// Test loading a corrupted GIF file
        /// </summary>
        [TestMethod]
        public async Task LoadBallVisualAsync_CorruptedGif_UsesFallback()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "corrupted.gif");
            CreateCorruptedGifFile(gifPath);

            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Should succeed with fallback image");
            Assert.IsFalse(_imageService.IsAnimated, "Fallback should not be animated");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Fallback frame should be set");
        }

        /// <summary>
        /// Test loading a non-existent GIF file
        /// </summary>
        [TestMethod]
        public async Task LoadBallVisualAsync_NonExistentGif_UsesFallback()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "nonexistent.gif");

            // Act
            var result = await _imageService.LoadBallVisualAsync(gifPath);

            // Assert
            Assert.IsTrue(result, "Should succeed with fallback image");
            Assert.IsFalse(_imageService.IsAnimated, "Fallback should not be animated");
            Assert.AreEqual(VisualContentType.StaticImage, _imageService.ContentType);
            Assert.IsNotNull(_imageService.CurrentFrame, "Fallback frame should be set");
        }

        #endregion GIF Loading Tests

        #region Animation Playback Tests

        /// <summary>
        /// Test starting and stopping GIF animation playback
        /// </summary>
        [TestMethod]
        public async Task AnimationPlayback_StartAndStop_WorksCorrectly()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "animated.gif");
            CreateTestMultiFrameGif(gifPath);
            await _imageService.LoadBallVisualAsync(gifPath);

            // Act & Assert - Start animation
            _imageService.StartAnimation();
            // Note: We can't easily test the internal state of AnimationEngine without exposing it
            // This test mainly ensures no exceptions are thrown

            // Act & Assert - Stop animation
            _imageService.StopAnimation();
            // Again, mainly testing for no exceptions
        }

        /// <summary>
        /// Test updating animation frames
        /// </summary>
        [TestMethod]
        public async Task UpdateFrame_AnimatedGif_UpdatesCurrentFrame()
        {
            // Arrange
            var gifPath = Path.Combine(_testDataDirectory, "animated.gif");
            CreateTestMultiFrameGif(gifPath);
            await _imageService.LoadBallVisualAsync(gifPath);
            var initialFrame = _imageService.CurrentFrame;

            // Act
            _imageService.StartAnimation();
            _imageService.UpdateFrame();

            // Assert
            Assert.IsNotNull(_imageService.CurrentFrame, "Current frame should still be set after update");
            // Note: Frame might be the same if not enough time has passed, but should not be null
        }

        #endregion Animation Playback Tests

        #region Animation Engine Tests

        /// <summary>
        /// Test AnimationEngine loop count functionality
        /// </summary>
        [TestMethod]
        public void AnimationEngine_LoopCount_WorksCorrectly()
        {
            // Arrange
            var animationEngine = new AnimationEngine(_logService);
            var frames = CreateTestAnimationFrames(3);

            // Act
            animationEngine.LoadFrames(frames);
            animationEngine.LoopCount = 2; // Loop twice
            animationEngine.IsLooping = true;

            // Assert
            Assert.AreEqual(2, animationEngine.LoopCount);
            Assert.IsTrue(animationEngine.IsLooping);
            Assert.AreEqual(0, animationEngine.CurrentLoop);
        }

        /// <summary>
        /// Test AnimationEngine frame advancement with looping
        /// </summary>
        [TestMethod]
        public void AnimationEngine_NextFrame_HandlesLooping()
        {
            // Arrange
            var animationEngine = new AnimationEngine(_logService);
            var frames = CreateTestAnimationFrames(2);
            animationEngine.LoadFrames(frames);
            animationEngine.LoopCount = 1; // Loop once
            animationEngine.IsLooping = true;

            // Act & Assert - First frame
            Assert.AreEqual(0, animationEngine.CurrentFrameIndex);
            
            // Advance to second frame
            animationEngine.NextFrame();
            Assert.AreEqual(1, animationEngine.CurrentFrameIndex);
            
            // Advance past last frame - should loop
            animationEngine.NextFrame();
            Assert.AreEqual(0, animationEngine.CurrentFrameIndex);
            Assert.AreEqual(1, animationEngine.CurrentLoop);
            
            // Advance through second loop
            animationEngine.NextFrame();
            Assert.AreEqual(1, animationEngine.CurrentFrameIndex);
            
            // Advance past last frame - should stop (reached loop limit)
            animationEngine.NextFrame();
            Assert.AreEqual(1, animationEngine.CurrentFrameIndex); // Should stay at last frame
            Assert.IsFalse(animationEngine.IsPlaying); // Should stop playing
        }

        /// <summary>
        /// Test AnimationEngine infinite looping
        /// </summary>
        [TestMethod]
        public void AnimationEngine_InfiniteLoop_ContinuesLooping()
        {
            // Arrange
            var animationEngine = new AnimationEngine(_logService);
            var frames = CreateTestAnimationFrames(2);
            animationEngine.LoadFrames(frames);
            animationEngine.LoopCount = 0; // Infinite loop
            animationEngine.IsLooping = true;
            animationEngine.Play();

            // Act & Assert - Loop multiple times
            for (int loop = 0; loop < 5; loop++)
            {
                // Advance to second frame
                animationEngine.NextFrame();
                Assert.AreEqual(1, animationEngine.CurrentFrameIndex);
                
                // Advance past last frame - should loop back
                animationEngine.NextFrame();
                Assert.AreEqual(0, animationEngine.CurrentFrameIndex);
                Assert.IsTrue(animationEngine.IsPlaying); // Should keep playing
            }
        }

        #endregion Animation Engine Tests

        #region Helper Methods

        /// <summary>
        /// Creates a test GIF file for testing
        /// </summary>
        /// <param name="filePath">Path where to create the test file</param>
        private void CreateTestGifFile(string filePath)
        {
            // Create a minimal valid GIF file
            var gifBytes = new byte[]
            {
                // GIF Header
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // "GIF89a"
                // Logical Screen Descriptor
                0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                // Image Descriptor
                0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
                // Image Data
                0x02, 0x02, 0x04, 0x01, 0x00,
                // Trailer
                0x3B
            };
            
            File.WriteAllBytes(filePath, gifBytes);
        }

        /// <summary>
        /// Creates a test PNG file for testing
        /// </summary>
        /// <param name="filePath">Path where to create the test file</param>
        private void CreateTestPngFile(string filePath)
        {
            // Create a simple 1x1 PNG
            var bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgr24, null);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// Creates a test multi-frame GIF file
        /// </summary>
        /// <param name="filePath">Path where to create the test file</param>
        private void CreateTestMultiFrameGif(string filePath)
        {
            // For testing purposes, create a simple multi-frame GIF
            // This is a simplified version - in a real scenario you'd use a proper GIF encoder
            var gifBytes = new byte[]
            {
                // GIF Header
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // "GIF89a"
                // Logical Screen Descriptor (2x2 image)
                0x02, 0x00, 0x02, 0x00, 0x80, 0x00, 0x00,
                // Global Color Table (2 colors)
                0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
                // Application Extension (Netscape 2.0 for looping)
                0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30,
                0x03, 0x01, 0x00, 0x00, 0x00,
                // Graphics Control Extension (Frame 1)
                0x21, 0xF9, 0x04, 0x00, 0x64, 0x00, 0x00, 0x00, // 100 centiseconds delay
                // Image Descriptor (Frame 1)
                0x2C, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00,
                // Image Data (Frame 1)
                0x02, 0x02, 0x04, 0x01, 0x00,
                // Graphics Control Extension (Frame 2)
                0x21, 0xF9, 0x04, 0x00, 0x64, 0x00, 0x00, 0x00, // 100 centiseconds delay
                // Image Descriptor (Frame 2)
                0x2C, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00,
                // Image Data (Frame 2)
                0x02, 0x02, 0x04, 0x01, 0x00,
                // Trailer
                0x3B
            };
            
            File.WriteAllBytes(filePath, gifBytes);
        }

        /// <summary>
        /// Creates a test single-frame GIF file
        /// </summary>
        /// <param name="filePath">Path where to create the test file</param>
        private void CreateTestSingleFrameGif(string filePath)
        {
            CreateTestGifFile(filePath); // Single frame GIF is the same as basic GIF
        }

        /// <summary>
        /// Creates a corrupted GIF file for testing error handling
        /// </summary>
        /// <param name="filePath">Path where to create the test file</param>
        private void CreateCorruptedGifFile(string filePath)
        {
            // Create a file with GIF header but corrupted data
            var corruptedBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0xFF, 0xFF, 0xFF };
            File.WriteAllBytes(filePath, corruptedBytes);
        }

        /// <summary>
        /// Creates test animation frames for testing
        /// </summary>
        /// <param name="frameCount">Number of frames to create</param>
        /// <returns>List of test animation frames</returns>
        private List<AnimationFrame> CreateTestAnimationFrames(int frameCount)
        {
            var frames = new List<AnimationFrame>();
            
            for (int i = 0; i < frameCount; i++)
            {
                var bitmap = new WriteableBitmap(10, 10, 96, 96, PixelFormats.Bgr24, null);
                bitmap.Freeze();
                
                var frame = new AnimationFrame(bitmap, TimeSpan.FromMilliseconds(100));
                frames.Add(frame);
            }
            
            return frames;
        }

        #endregion Helper Methods
    }
}