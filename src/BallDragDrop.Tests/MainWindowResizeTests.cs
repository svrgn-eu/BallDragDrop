using System;
using System.Windows;
using System.Threading;
using BallDragDrop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class MainWindowResizeTests
    {
        [STATestMethod]
        public void SetBallPosition_ShouldInitializePositionCorrectly()
        {
            // Arrange
            var window = new MainWindow();
            
            // Set initial canvas size for testing
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            
            // Act
            window.SetBallPosition(400, 300);
            var position = window.GetBallPosition();
            
            // Assert
            Assert.AreEqual(400, position.X);
            Assert.AreEqual(300, position.Y);
        }
        
        [STATestMethod]
        public void WindowResize_ShouldMaintainRelativePosition()
        {
            // Arrange
            var window = new MainWindow();
            
            // Set initial canvas size
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            
            // Position the ball at the center of the canvas
            window.SetBallPosition(400, 300);
            
            // Act - simulate window resize
            window.SimulateResize(1000, 750);
            
            var newPosition = window.GetBallPosition();
            
            // Assert - ball should maintain its relative position (center of the canvas)
            Assert.AreEqual(500, newPosition.X);  // 50% of 1000
            Assert.AreEqual(375, newPosition.Y);  // 50% of 750
        }
        
        [STATestMethod]
        public void WindowResize_ShouldHandleZeroInitialSize()
        {
            // Arrange
            var window = new MainWindow();
            
            // Set initial canvas size to zero (edge case)
            window.MainCanvas.Width = 0;
            window.MainCanvas.Height = 0;
            
            // Try to position the ball
            window.SetBallPosition(100, 100);
            
            // Act - simulate window resize
            window.SimulateResize(800, 600);
            
            var newPosition = window.GetBallPosition();
            
            // Assert - ball position should remain at the initial values
            // since relative positioning couldn't be initialized with zero dimensions
            Assert.AreEqual(100, newPosition.X);
            Assert.AreEqual(100, newPosition.Y);
        }
        
        [STATestMethod]
        public void BallPositionChanged_EventShouldFireOnResize()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            window.SetBallPosition(400, 300);
            
            bool eventFired = false;
            Point newPosition = new Point();
            
            window.BallPositionChanged += (sender, point) => 
            {
                eventFired = true;
                newPosition = point;
            };
            
            // Act
            window.SimulateResize(1000, 750);
            
            // Assert
            Assert.IsTrue(eventFired, "BallPositionChanged event should fire when window is resized");
            Assert.AreEqual(500, newPosition.X);
            Assert.AreEqual(375, newPosition.Y);
        }
        
        [STATestMethod]
        public void WindowResize_ShouldHandleMultipleResizeEvents()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            window.SetBallPosition(400, 300); // 50% of width, 50% of height
            
            // Act - simulate multiple resize events
            window.SimulateResize(1000, 750); // First resize
            window.SimulateResize(1200, 900); // Second resize
            
            var finalPosition = window.GetBallPosition();
            
            // Assert - ball should maintain its relative position through multiple resizes
            Assert.AreEqual(600, finalPosition.X);  // 50% of 1200
            Assert.AreEqual(450, finalPosition.Y);  // 50% of 900
        }
        
        [STATestMethod]
        public void WindowResize_ShouldHandleCornerPositioning()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            
            // Position the ball in the bottom-right corner
            window.SetBallPosition(800, 600);
            
            // Act - simulate window resize
            window.SimulateResize(1000, 750);
            
            var newPosition = window.GetBallPosition();
            
            // Get the ball view model to check the radius
            var viewModel = (BallDragDrop.ViewModels.BallViewModel)window.DataContext;
            double radius = viewModel.Radius;
            
            // Assert - ball should maintain its relative position (bottom-right corner)
            // but constrained by the radius
            Assert.AreEqual(1000 - radius, newPosition.X);  // 100% of 1000 minus radius
            Assert.AreEqual(750 - radius, newPosition.Y);   // 100% of 750 minus radius
        }
        
        [STATestMethod]
        public void WindowResize_ShouldHandleShrinking()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            window.SetBallPosition(400, 300); // 50% of width, 50% of height
            
            // Act - simulate window shrinking
            window.SimulateResize(400, 300);
            
            var newPosition = window.GetBallPosition();
            
            // Assert - ball should maintain its relative position when window shrinks
            Assert.AreEqual(200, newPosition.X);  // 50% of 400
            Assert.AreEqual(150, newPosition.Y);  // 50% of 300
        }
        
        [STATestMethod]
        public void WindowResize_ShouldApplyBoundaryConstraints()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            
            // Position the ball at the edge of the canvas
            window.SetBallPosition(800, 600);
            
            // Act - simulate window shrinking to a size smaller than the ball's position
            window.SimulateResize(400, 300);
            
            var newPosition = window.GetBallPosition();
            
            // Get the ball view model to check the radius
            var viewModel = (BallDragDrop.ViewModels.BallViewModel)window.DataContext;
            double radius = viewModel.Radius;
            
            // Assert - ball should be constrained to the new window boundaries
            Assert.AreEqual(400 - radius, newPosition.X);  // Constrained to max width minus radius
            Assert.AreEqual(300 - radius, newPosition.Y);  // Constrained to max height minus radius
            
            // Verify that the relative position has been updated
            // We can do this by resizing again and checking if the ball stays at the edge
            window.SimulateResize(600, 450);
            
            var finalPosition = window.GetBallPosition();
            
            // The ball should be at the edge minus the radius
            Assert.AreEqual(600 - radius, finalPosition.X);
            Assert.AreEqual(450 - radius, finalPosition.Y);
        }
        
        [STATestMethod]
        public void ConstrainToWindowBoundaries_ShouldAccountForBallRadius()
        {
            // Arrange
            var window = new MainWindow();
            window.MainCanvas.Width = 800;
            window.MainCanvas.Height = 600;
            
            // Act - constrain a position with a ball radius
            double ballRadius = 50;
            Point constrainedPosition = window.ConstrainToWindowBoundaries(780, 580, ballRadius);
            
            // Assert - position should be constrained to account for the ball's radius
            Assert.AreEqual(750, constrainedPosition.X);  // 800 - 50 (radius)
            Assert.AreEqual(550, constrainedPosition.Y);  // 600 - 50 (radius)
            
            // Test minimum boundaries
            Point constrainedPosition2 = window.ConstrainToWindowBoundaries(30, 40, ballRadius);
            Assert.AreEqual(50, constrainedPosition2.X);  // Minimum is radius (50)
            Assert.AreEqual(50, constrainedPosition2.Y);  // Minimum is radius (50)
        }
    }
}