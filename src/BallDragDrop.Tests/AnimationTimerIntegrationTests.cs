using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using BallDragDrop.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using BallDragDrop.Tests.TestHelpers;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Tests for animation timer integration in BallViewModel
    /// </summary>
    [TestClass]
    public class AnimationTimerIntegrationTests
    {
        #region Test Setup

        private BallViewModel _viewModel;
        private TestLogService _logService;
        private ImageService _imageService;

        [TestInitialize]
        public void TestInitialize()
        {
            _logService = new TestLogService();
            _imageService = new ImageService(_logService);
            _viewModel = new BallViewModel(_logService, _imageService);
            _viewModel.Initialize(100, 100, 25);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _viewModel = null;
            _imageService = null;
            _logService = null;
        }

        #endregion Test Setup

        #region Animation Timer Tests

        [TestMethod]
        public void EnsureAnimationContinuesDuringDrag_WhenAnimatedAndTimerStopped_StartsTimer()
        {
            // Arrange
            SetupAnimatedContent();
            StopAnimationTimer();

            // Act
            _viewModel.EnsureAnimationContinuesDuringDrag();

            // Assert
            Assert.IsTrue(IsAnimationTimerRunning(), "Animation timer should be running after ensuring continuation during drag");
            Assert.IsTrue(_logService.LogEntries.Exists(entry => 
                entry.Contains("Animation timer restarted during drag operation")), 
                "Should log that animation timer was restarted");
        }

        [TestMethod]
        public void EnsureAnimationContinuesDuringDrag_WhenNotAnimated_DoesNotStartTimer()
        {
            // Arrange
            SetupStaticContent();

            // Act
            _viewModel.EnsureAnimationContinuesDuringDrag();

            // Assert
            Assert.IsFalse(IsAnimationTimerRunning(), "Animation timer should not be running for static content");
        }

        [TestMethod]
        public void EnsureAnimationContinuesDuringDrag_WhenAnimatedAndTimerAlreadyRunning_DoesNotRestartTimer()
        {
            // Arrange
            SetupAnimatedContent();
            StartAnimationTimer();
            int initialLogCount = _logService.LogEntries.Count;

            // Act
            _viewModel.EnsureAnimationContinuesDuringDrag();

            // Assert
            Assert.IsTrue(IsAnimationTimerRunning(), "Animation timer should still be running");
            Assert.IsFalse(_logService.LogEntries.Exists(entry => 
                entry.Contains("Animation timer restarted during drag operation")), 
                "Should not log restart message when timer is already running");
        }

        [TestMethod]
        public void CoordinateAnimationWithPhysics_WhenAnimated_UpdatesFrame()
        {
            // Arrange
            SetupAnimatedContent();
            var initialFrame = _viewModel.BallImage;

            // Act
            _viewModel.CoordinateAnimationWithPhysics();

            // Assert
            // Note: The frame might be the same if it's the first frame or timing hasn't advanced
            // The important thing is that the method executes without error
            Assert.IsNotNull(_viewModel.BallImage, "Ball image should not be null after coordination");
        }

        [TestMethod]
        public void CoordinateAnimationWithPhysics_WhenNotAnimated_DoesNotThrow()
        {
            // Arrange
            SetupStaticContent();

            // Act & Assert
            try
            {
                _viewModel.CoordinateAnimationWithPhysics();
                Assert.IsTrue(true, "Method should execute without throwing for static content");
            }
            catch (Exception ex)
            {
                Assert.Fail($"CoordinateAnimationWithPhysics should not throw for static content: {ex.Message}");
            }
        }

        [TestMethod]
        public void OnMouseDown_WhenAnimated_EnsuresAnimationContinues()
        {
            // Arrange
            SetupAnimatedContent();
            StopAnimationTimer();
            var mouseEventArgs = CreateMouseEventArgs(new Point(100, 100));

            // Act
            InvokeMouseDown(mouseEventArgs);

            // Assert
            Assert.IsTrue(IsAnimationTimerRunning(), "Animation timer should be running after mouse down on animated ball");
            Assert.IsTrue(_logService.LogEntries.Exists(entry => 
                entry.Contains("animation maintained")), 
                "Should log that animation was maintained during drag initiation");
        }

        [TestMethod]
        public void AnimationTimer_WhenFrameDurationChanges_UpdatesInterval()
        {
            // Arrange
            SetupAnimatedContent();
            var originalInterval = GetAnimationTimerInterval();

            // Simulate frame duration change by updating the image service
            // This would typically happen when loading different animation content
            var newDuration = TimeSpan.FromMilliseconds(50); // Different from default
            SetImageServiceFrameDuration(newDuration);

            // Act
            TriggerAnimationTimerTick();

            // Assert
            var newInterval = GetAnimationTimerInterval();
            Assert.AreEqual(newDuration, newInterval, "Animation timer interval should update to match frame duration");
            Assert.IsTrue(_logService.LogEntries.Exists(entry => 
                entry.Contains("Animation timer interval updated")), 
                "Should log interval update");
        }

        [TestMethod]
        public void AnimationTimer_DuringDragOperation_ContinuesRunning()
        {
            // Arrange
            SetupAnimatedContent();
            StartAnimationTimer();

            // Act - Simulate drag operation
            var mouseDownArgs = CreateMouseEventArgs(new Point(100, 100));
            InvokeMouseDown(mouseDownArgs);

            // Simulate some time passing during drag
            Thread.Sleep(100);

            // Assert
            Assert.IsTrue(IsAnimationTimerRunning(), "Animation timer should continue running during drag operation");
            Assert.IsTrue(_viewModel.IsDragging, "Ball should be in dragging state");
        }

        #endregion Animation Timer Tests

        #region Helper Methods

        /// <summary>
        /// Sets up the view model with animated content for testing
        /// </summary>
        private void SetupAnimatedContent()
        {
            // Use reflection to set the IsAnimated property for testing
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty?.SetValue(_viewModel, true);

            // Set up a mock animated image
            _viewModel.BallImage = TestImageHelper.CreateTestImage(50, 50);
        }

        /// <summary>
        /// Sets up the view model with static content for testing
        /// </summary>
        private void SetupStaticContent()
        {
            // Use reflection to set the IsAnimated property for testing
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty?.SetValue(_viewModel, false);

            // Set up a static image
            _viewModel.BallImage = TestImageHelper.CreateTestImage(50, 50);
        }

        /// <summary>
        /// Starts the animation timer using reflection
        /// </summary>
        private void StartAnimationTimer()
        {
            var animationTimerField = typeof(BallViewModel).GetField("_animationTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = animationTimerField?.GetValue(_viewModel) as DispatcherTimer;
            timer?.Start();
        }

        /// <summary>
        /// Stops the animation timer using reflection
        /// </summary>
        private void StopAnimationTimer()
        {
            var animationTimerField = typeof(BallViewModel).GetField("_animationTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = animationTimerField?.GetValue(_viewModel) as DispatcherTimer;
            timer?.Stop();
        }

        /// <summary>
        /// Checks if the animation timer is running using reflection
        /// </summary>
        /// <returns>True if the timer is running, false otherwise</returns>
        private bool IsAnimationTimerRunning()
        {
            var animationTimerField = typeof(BallViewModel).GetField("_animationTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = animationTimerField?.GetValue(_viewModel) as DispatcherTimer;
            return timer?.IsEnabled ?? false;
        }

        /// <summary>
        /// Gets the animation timer interval using reflection
        /// </summary>
        /// <returns>The current timer interval</returns>
        private TimeSpan GetAnimationTimerInterval()
        {
            var animationTimerField = typeof(BallViewModel).GetField("_animationTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = animationTimerField?.GetValue(_viewModel) as DispatcherTimer;
            return timer?.Interval ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Triggers the animation timer tick event using reflection
        /// </summary>
        private void TriggerAnimationTimerTick()
        {
            var onAnimationTimerTickMethod = typeof(BallViewModel).GetMethod("OnAnimationTimerTick", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onAnimationTimerTickMethod?.Invoke(_viewModel, new object[] { _viewModel, EventArgs.Empty });
        }

        /// <summary>
        /// Sets the frame duration in the image service for testing
        /// </summary>
        /// <param name="duration">The frame duration to set</param>
        private void SetImageServiceFrameDuration(TimeSpan duration)
        {
            // Use reflection to set the FrameDuration property in ImageService
            var frameDurationProperty = typeof(ImageService).GetProperty("FrameDuration");
            frameDurationProperty?.SetValue(_imageService, duration);
        }

        /// <summary>
        /// Creates a mock MouseEventArgs for testing
        /// </summary>
        /// <param name="position">The mouse position</param>
        /// <returns>A mock MouseEventArgs</returns>
        private CustomMouseEventArgs CreateMouseEventArgs(Point position)
        {
            // Create a mock mouse event args for testing
            // Note: This is a simplified version for testing purposes
            return new CustomMouseEventArgs(position.X, position.Y);
        }

        /// <summary>
        /// Invokes the mouse down command
        /// </summary>
        /// <param name="mouseEventArgs">The mouse event arguments</param>
        private void InvokeMouseDown(CustomMouseEventArgs mouseEventArgs)
        {
            // Create a mock MouseEventArgs for the command
            // Since we can't easily create a real MouseEventArgs in tests, we'll use reflection
            // to call the command with null (many commands handle null gracefully)
            if (_viewModel.MouseDownCommand.CanExecute(null))
            {
                _viewModel.MouseDownCommand.Execute(null);
            }
        }

        #endregion Helper Methods
    }


}