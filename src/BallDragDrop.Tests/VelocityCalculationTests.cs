using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using System;
using System.Windows;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class VelocityCalculationTests
    {
        [TestMethod]
        public void CalculateVelocityFromHistory_WithValidData_ReturnsCorrectVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var positions = new Point[3];
            var timestamps = new DateTime[3];
            
            // Create a simple movement pattern: moving right at constant speed
            positions[0] = new Point(100, 100);
            positions[1] = new Point(150, 100);
            positions[2] = new Point(200, 100);
            
            // Timestamps 0.1 seconds apart
            timestamps[0] = new DateTime(2023, 1, 1, 12, 0, 0);
            timestamps[1] = timestamps[0].AddSeconds(0.1);
            timestamps[2] = timestamps[1].AddSeconds(0.1);
            
            // Act
            var (velocityX, velocityY) = engine.CalculateVelocityFromHistory(positions, timestamps, 3);
            
            // Assert
            // Expected velocity: 500 pixels per second in X direction (50 pixels / 0.1 seconds)
            Assert.AreEqual(500, velocityX, 1.0); // Allow small rounding error
            Assert.AreEqual(0, velocityY, 1.0);
        }
        
        [TestMethod]
        public void CalculateVelocityFromHistory_WithDiagonalMovement_ReturnsCorrectVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var positions = new Point[3];
            var timestamps = new DateTime[3];
            
            // Create a diagonal movement pattern
            positions[0] = new Point(100, 100);
            positions[1] = new Point(150, 150);
            positions[2] = new Point(200, 200);
            
            // Timestamps 0.1 seconds apart
            timestamps[0] = new DateTime(2023, 1, 1, 12, 0, 0);
            timestamps[1] = timestamps[0].AddSeconds(0.1);
            timestamps[2] = timestamps[1].AddSeconds(0.1);
            
            // Act
            var (velocityX, velocityY) = engine.CalculateVelocityFromHistory(positions, timestamps, 3);
            
            // Assert
            // Expected velocity: 500 pixels per second in both X and Y directions
            Assert.AreEqual(500, velocityX, 1.0);
            Assert.AreEqual(500, velocityY, 1.0);
        }
        
        [TestMethod]
        public void CalculateVelocityFromHistory_WithAcceleratingMovement_WeightsRecentMovementsMore()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var positions = new Point[3];
            var timestamps = new DateTime[3];
            
            // Create an accelerating movement pattern
            positions[0] = new Point(100, 100);
            positions[1] = new Point(120, 100); // 20 pixels in 0.1s = 200 pixels/s
            positions[2] = new Point(170, 100); // 50 pixels in 0.1s = 500 pixels/s
            
            // Timestamps 0.1 seconds apart
            timestamps[0] = new DateTime(2023, 1, 1, 12, 0, 0);
            timestamps[1] = timestamps[0].AddSeconds(0.1);
            timestamps[2] = timestamps[1].AddSeconds(0.1);
            
            // Act
            var (velocityX, velocityY) = engine.CalculateVelocityFromHistory(positions, timestamps, 3);
            
            // Assert
            // Expected velocity should be closer to the more recent movement (500 pixels/s)
            // than to the earlier movement (200 pixels/s)
            Assert.IsTrue(velocityX > 350); // Should be weighted toward the more recent, faster movement
            Assert.AreEqual(0, velocityY, 1.0);
        }
        
        [TestMethod]
        public void CalculateVelocityFromHistory_WithInvalidData_ReturnsZeroVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            
            // Act & Assert - Null arrays
            var (vx1, vy1) = engine.CalculateVelocityFromHistory(null, new DateTime[3], 3);
            Assert.AreEqual(0, vx1);
            Assert.AreEqual(0, vy1);
            
            var (vx2, vy2) = engine.CalculateVelocityFromHistory(new Point[3], null, 3);
            Assert.AreEqual(0, vx2);
            Assert.AreEqual(0, vy2);
            
            // Act & Assert - Count too small
            var (vx3, vy3) = engine.CalculateVelocityFromHistory(new Point[3], new DateTime[3], 1);
            Assert.AreEqual(0, vx3);
            Assert.AreEqual(0, vy3);
            
            // Act & Assert - Arrays too small
            var (vx4, vy4) = engine.CalculateVelocityFromHistory(new Point[2], new DateTime[2], 3);
            Assert.AreEqual(0, vx4);
            Assert.AreEqual(0, vy4);
        }
        
        [TestMethod]
        public void IsThrowFromHistory_AboveThreshold_ReturnsTrue()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var positions = new Point[3];
            var timestamps = new DateTime[3];
            
            // Create a fast movement pattern
            positions[0] = new Point(100, 100);
            positions[1] = new Point(200, 100);
            positions[2] = new Point(300, 100);
            
            // Timestamps 0.1 seconds apart
            timestamps[0] = new DateTime(2023, 1, 1, 12, 0, 0);
            timestamps[1] = timestamps[0].AddSeconds(0.1);
            timestamps[2] = timestamps[1].AddSeconds(0.1);
            
            // Act
            bool isThrow = engine.IsThrowFromHistory(positions, timestamps, 3, 500);
            
            // Assert
            // Expected velocity: 1000 pixels per second, which is above the 500 threshold
            Assert.IsTrue(isThrow);
        }
        
        [TestMethod]
        public void IsThrowFromHistory_BelowThreshold_ReturnsFalse()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var positions = new Point[3];
            var timestamps = new DateTime[3];
            
            // Create a slow movement pattern
            positions[0] = new Point(100, 100);
            positions[1] = new Point(110, 100);
            positions[2] = new Point(120, 100);
            
            // Timestamps 0.1 seconds apart
            timestamps[0] = new DateTime(2023, 1, 1, 12, 0, 0);
            timestamps[1] = timestamps[0].AddSeconds(0.1);
            timestamps[2] = timestamps[1].AddSeconds(0.1);
            
            // Act
            bool isThrow = engine.IsThrowFromHistory(positions, timestamps, 3, 500);
            
            // Assert
            // Expected velocity: 100 pixels per second, which is below the 500 threshold
            Assert.IsFalse(isThrow);
        }
        
        [TestMethod]
        public void IsThrowFromHistory_WithInvalidData_ReturnsFalse()
        {
            // Arrange
            var engine = new PhysicsEngine();
            
            // Act & Assert - Null arrays
            Assert.IsFalse(engine.IsThrowFromHistory(null, new DateTime[3], 3));
            Assert.IsFalse(engine.IsThrowFromHistory(new Point[3], null, 3));
            
            // Act & Assert - Count too small
            Assert.IsFalse(engine.IsThrowFromHistory(new Point[3], new DateTime[3], 1));
            
            // Act & Assert - Arrays too small
            Assert.IsFalse(engine.IsThrowFromHistory(new Point[2], new DateTime[2], 3));
        }
    }
}