using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using BallDragDrop.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class AnimationEngineTests
    {
        private Mock<ILogService> _mockLogService;
        private AnimationEngine _animationEngine;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _animationEngine = new AnimationEngine(_mockLogService.Object);
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_Default_InitializesCorrectly()
        {
            // Arrange & Act
            var engine = new AnimationEngine();

            // Assert
            Assert.AreEqual(0, engine.FrameCount);
            Assert.AreEqual(0, engine.CurrentFrameIndex);
            Assert.IsFalse(engine.IsPlaying);
            Assert.IsTrue(engine.IsLooping);
            Assert.IsNotNull(engine.Frames);
        }

        [TestMethod]
        public void Constructor_WithLogService_InitializesCorrectly()
        {
            // Arrange & Act
            var engine = new AnimationEngine(_mockLogService.Object);

            // Assert
            Assert.AreEqual(0, engine.FrameCount);
            Assert.AreEqual(0, engine.CurrentFrameIndex);
            Assert.IsFalse(engine.IsPlaying);
            Assert.IsTrue(engine.IsLooping);
            Assert.IsNotNull(engine.Frames);
        }

        #endregion Constructor Tests

        #region LoadFrames Tests

        [TestMethod]
        public void LoadFrames_ValidFrames_LoadsCorrectly()
        {
            // Arrange
            var frames = CreateTestFrames(3);

            // Act
            _animationEngine.LoadFrames(frames);

            // Assert
            Assert.AreEqual(3, _animationEngine.FrameCount);
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
            Assert.IsFalse(_animationEngine.IsPlaying);
        }

        [TestMethod]
        public void LoadFrames_NullFrames_HandlesGracefully()
        {
            // Arrange & Act
            _animationEngine.LoadFrames(null);

            // Assert
            Assert.AreEqual(0, _animationEngine.FrameCount);
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void LoadFrames_EmptyFrames_LoadsCorrectly()
        {
            // Arrange
            var frames = new List<AnimationFrame>();

            // Act
            _animationEngine.LoadFrames(frames);

            // Assert
            Assert.AreEqual(0, _animationEngine.FrameCount);
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        #endregion LoadFrames Tests

        #region Playback Control Tests

        [TestMethod]
        public void Play_WithFrames_StartsPlayback()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);

            // Act
            _animationEngine.Play();

            // Assert
            Assert.IsTrue(_animationEngine.IsPlaying);
        }

        [TestMethod]
        public void Play_WithoutFrames_DoesNotStart()
        {
            // Arrange - no frames loaded

            // Act
            _animationEngine.Play();

            // Assert
            Assert.IsFalse(_animationEngine.IsPlaying);
        }

        [TestMethod]
        public void Pause_WhilePlaying_PausesPlayback()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.Play();

            // Act
            _animationEngine.Pause();

            // Assert
            Assert.IsFalse(_animationEngine.IsPlaying);
        }

        [TestMethod]
        public void Stop_WhilePlaying_StopsAndResets()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.Play();
            _animationEngine.NextFrame(); // Advance to frame 1

            // Act
            _animationEngine.Stop();

            // Assert
            Assert.IsFalse(_animationEngine.IsPlaying);
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        #endregion Playback Control Tests

        #region Frame Navigation Tests

        [TestMethod]
        public void NextFrame_WithFrames_AdvancesFrame()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);

            // Act
            _animationEngine.NextFrame();

            // Assert
            Assert.AreEqual(1, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void NextFrame_AtLastFrameWithLooping_LoopsToFirst()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.IsLooping = true;
            
            // Advance to last frame
            _animationEngine.NextFrame(); // Frame 1
            _animationEngine.NextFrame(); // Frame 2

            // Act
            _animationEngine.NextFrame(); // Should loop to frame 0

            // Assert
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void NextFrame_AtLastFrameWithoutLooping_StopsAtLast()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.IsLooping = false;
            _animationEngine.Play();
            
            // Advance to last frame
            _animationEngine.NextFrame(); // Frame 1
            _animationEngine.NextFrame(); // Frame 2

            // Act
            _animationEngine.NextFrame(); // Should stay at frame 2 and stop

            // Assert
            Assert.AreEqual(2, _animationEngine.CurrentFrameIndex);
            Assert.IsFalse(_animationEngine.IsPlaying);
        }

        [TestMethod]
        public void NextFrame_WithoutFrames_DoesNotThrow()
        {
            // Arrange - no frames loaded

            // Act & Assert - should not throw
            _animationEngine.NextFrame();
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        #endregion Frame Navigation Tests

        #region GetCurrentFrame Tests

        [TestMethod]
        public void GetCurrentFrame_WithFrames_ReturnsCurrentFrame()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);

            // Act
            var currentFrame = _animationEngine.GetCurrentFrame();

            // Assert
            Assert.IsNotNull(currentFrame);
            Assert.AreSame(frames[0], currentFrame);
        }

        [TestMethod]
        public void GetCurrentFrame_WithoutFrames_ReturnsNull()
        {
            // Arrange - no frames loaded

            // Act
            var currentFrame = _animationEngine.GetCurrentFrame();

            // Assert
            Assert.IsNull(currentFrame);
        }

        [TestMethod]
        public void GetCurrentFrame_AfterAdvancing_ReturnsCorrectFrame()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.NextFrame();

            // Act
            var currentFrame = _animationEngine.GetCurrentFrame();

            // Assert
            Assert.IsNotNull(currentFrame);
            Assert.AreSame(frames[1], currentFrame);
        }

        #endregion GetCurrentFrame Tests

        #region Update Tests

        [TestMethod]
        public void Update_WithTimeSpan_NotPlaying_DoesNotAdvance()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            var deltaTime = TimeSpan.FromMilliseconds(200);

            // Act
            _animationEngine.Update(deltaTime);

            // Assert
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void Update_WithTimeSpan_PlayingButNotEnoughTime_DoesNotAdvance()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.Play();
            var deltaTime = TimeSpan.FromMilliseconds(50); // Less than frame duration

            // Act
            _animationEngine.Update(deltaTime);

            // Assert
            Assert.AreEqual(0, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void Update_WithTimeSpan_PlayingWithEnoughTime_AdvancesFrame()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.Play();
            var deltaTime = TimeSpan.FromMilliseconds(150); // More than frame duration

            // Act
            _animationEngine.Update(deltaTime);

            // Assert
            Assert.AreEqual(1, _animationEngine.CurrentFrameIndex);
        }

        [TestMethod]
        public void Update_WithoutTimeSpan_DoesNotThrow()
        {
            // Arrange
            var frames = CreateTestFrames(3);
            _animationEngine.LoadFrames(frames);
            _animationEngine.Play();

            // Act & Assert - should not throw
            _animationEngine.Update();
        }

        #endregion Update Tests

        #region Helper Methods

        /// <summary>
        /// Creates test animation frames for testing
        /// </summary>
        /// <param name="count">Number of frames to create</param>
        /// <returns>List of test animation frames</returns>
        private List<AnimationFrame> CreateTestFrames(int count)
        {
            var frames = new List<AnimationFrame>();
            
            for (int i = 0; i < count; i++)
            {
                // Create a simple test image
                var bitmap = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                
                using (var context = visual.RenderOpen())
                {
                    var brush = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
                    context.DrawRectangle(brush, null, new System.Windows.Rect(0, 0, 10, 10));
                }
                
                bitmap.Render(visual);
                bitmap.Freeze();
                
                var frame = new AnimationFrame(bitmap, TimeSpan.FromMilliseconds(100));
                frames.Add(frame);
            }
            
            return frames;
        }

        #endregion Helper Methods
    }
}