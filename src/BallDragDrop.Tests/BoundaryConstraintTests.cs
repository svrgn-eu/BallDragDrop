using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using System;
using System.Windows;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class BoundaryConstraintTests
    {
        [TestMethod]
        public void Ball_CannotBeDraggedOutsideWindowBoundaries()
        {
            // Arrange
            double windowWidth = 400;
            double windowHeight = 300;
            double ballRadius = 25;
            var viewModel = new BallViewModel(200, 150, ballRadius);
            
            // Act - Try to move the ball beyond the right boundary
            viewModel.X = windowWidth + 50; // Way beyond the right edge
            
            // Apply constraints
            viewModel.ConstrainPosition(0, 0, windowWidth, windowHeight);
            
            // Assert - Ball should be constrained to the right edge
            Assert.AreEqual(windowWidth - ballRadius, viewModel.X);
            Assert.AreEqual(150, viewModel.Y); // Y position should remain unchanged
            
            // Act - Try to move the ball beyond the left boundary
            viewModel.X = -50; // Way beyond the left edge
            
            // Apply constraints
            viewModel.ConstrainPosition(0, 0, windowWidth, windowHeight);
            
            // Assert - Ball should be constrained to the left edge
            Assert.AreEqual(ballRadius, viewModel.X);
            Assert.AreEqual(150, viewModel.Y); // Y position should remain unchanged
            
            // Act - Try to move the ball beyond the top boundary
            viewModel.X = 200; // Reset X to a valid position
            viewModel.Y = -50; // Way beyond the top edge
            
            // Apply constraints
            viewModel.ConstrainPosition(0, 0, windowWidth, windowHeight);
            
            // Assert - Ball should be constrained to the top edge
            Assert.AreEqual(200, viewModel.X); // X position should remain unchanged
            Assert.AreEqual(ballRadius, viewModel.Y);
            
            // Act - Try to move the ball beyond the bottom boundary
            viewModel.Y = windowHeight + 50; // Way beyond the bottom edge
            
            // Apply constraints
            viewModel.ConstrainPosition(0, 0, windowWidth, windowHeight);
            
            // Assert - Ball should be constrained to the bottom edge
            Assert.AreEqual(200, viewModel.X); // X position should remain unchanged
            Assert.AreEqual(windowHeight - ballRadius, viewModel.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_ConstrainsToMinimumBoundaries()
        {
            // Arrange
            double minX = 20;
            double minY = 30;
            double maxX = 200;
            double maxY = 200;
            double radius = 25;
            var viewModel = new BallViewModel(10, 10, radius);
            
            // Act - Position is outside minimum boundaries
            viewModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert
            Assert.AreEqual(minX + radius, viewModel.X); // Constrained to minX + radius
            Assert.AreEqual(minY + radius, viewModel.Y); // Constrained to minY + radius
        }
        
        [TestMethod]
        public void ConstrainPosition_ConstrainsToMaximumBoundaries()
        {
            // Arrange
            double minX = 0;
            double minY = 0;
            double maxX = 200;
            double maxY = 200;
            double radius = 25;
            var viewModel = new BallViewModel(250, 250, radius);
            
            // Act - Position is outside maximum boundaries
            viewModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert
            Assert.AreEqual(maxX - radius, viewModel.X); // Constrained to maxX - radius
            Assert.AreEqual(maxY - radius, viewModel.Y); // Constrained to maxY - radius
        }
        
        [TestMethod]
        public void ConstrainPosition_DoesNotChangePositionWhenInsideBoundaries()
        {
            // Arrange
            double minX = 0;
            double minY = 0;
            double maxX = 200;
            double maxY = 200;
            double radius = 25;
            double initialX = 100;
            double initialY = 100;
            var viewModel = new BallViewModel(initialX, initialY, radius);
            
            // Act - Position is inside boundaries
            viewModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert - Position should not change
            Assert.AreEqual(initialX, viewModel.X);
            Assert.AreEqual(initialY, viewModel.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_HandlesZeroOrNegativeBoundaries()
        {
            // Arrange
            double minX = -10; // Negative minimum boundary
            double minY = -10;
            double maxX = 0;   // Zero maximum boundary
            double maxY = 0;
            double radius = 25;
            var viewModel = new BallViewModel(100, 100, radius);
            
            // Act - Constrain with unusual boundaries
            viewModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert - Should be constrained to the maximum boundary
            Assert.AreEqual(maxX - radius, viewModel.X);
            Assert.AreEqual(maxY - radius, viewModel.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_HandlesEqualMinAndMaxBoundaries()
        {
            // Arrange
            double boundary = 100;
            double radius = 25;
            var viewModel = new BallViewModel(50, 50, radius);
            
            // Act - Constrain with equal min and max boundaries
            viewModel.ConstrainPosition(boundary, boundary, boundary, boundary);
            
            // Assert - Should be constrained to the boundary plus radius
            Assert.AreEqual(boundary + radius, viewModel.X);
            Assert.AreEqual(boundary + radius, viewModel.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_HandlesInvalidBoundaries()
        {
            // Arrange
            double minX = 200; // Min greater than max
            double minY = 200;
            double maxX = 100;
            double maxY = 100;
            double radius = 25;
            double initialX = 150;
            double initialY = 150;
            var viewModel = new BallViewModel(initialX, initialY, radius);
            
            // Act - Constrain with invalid boundaries
            viewModel.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert - Should be constrained to the minimum boundary plus radius
            // since that's what the implementation would do in this case
            Assert.AreEqual(minX + radius, viewModel.X);
            Assert.AreEqual(minY + radius, viewModel.Y);
        }
    }
}