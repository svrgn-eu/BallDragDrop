using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using System;
using System.Windows;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class BallModelTests
    {
        [TestMethod]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Arrange & Act
            var ball = new BallModel();
            
            // Assert
            Assert.AreEqual(0, ball.X);
            Assert.AreEqual(0, ball.Y);
            Assert.AreEqual(0, ball.VelocityX);
            Assert.AreEqual(0, ball.VelocityY);
            Assert.AreEqual(25, ball.Radius);
            Assert.AreEqual(1, ball.Mass);
            Assert.IsFalse(ball.IsMoving);
        }
        
        [TestMethod]
        public void Constructor_WithPosition_SetsPositionAndDefaultValues()
        {
            // Arrange & Act
            double x = 100;
            double y = 200;
            var ball = new BallModel(x, y);
            
            // Assert
            Assert.AreEqual(x, ball.X);
            Assert.AreEqual(y, ball.Y);
            Assert.AreEqual(0, ball.VelocityX);
            Assert.AreEqual(0, ball.VelocityY);
            Assert.AreEqual(25, ball.Radius);
            Assert.AreEqual(1, ball.Mass);
        }
        
        [TestMethod]
        public void Constructor_WithPositionAndRadius_SetsAllValues()
        {
            // Arrange & Act
            double x = 100;
            double y = 200;
            double radius = 30;
            var ball = new BallModel(x, y, radius);
            
            // Assert
            Assert.AreEqual(x, ball.X);
            Assert.AreEqual(y, ball.Y);
            Assert.AreEqual(radius, ball.Radius);
        }
        
        [TestMethod]
        public void UpdatePosition_WithDefaultTimeStep_UpdatesPosition()
        {
            // Arrange
            var ball = new BallModel(100, 100);
            ball.VelocityX = 60; // 60 pixels per second
            ball.VelocityY = 30; // 30 pixels per second
            
            // Act
            ball.UpdatePosition(); // Default time step is 1/60 second
            
            // Assert
            Assert.AreEqual(101, ball.X, 0.001); // 100 + 60 * (1/60) = 101
            Assert.AreEqual(100.5, ball.Y, 0.001); // 100 + 30 * (1/60) = 100.5
        }
        
        [TestMethod]
        public void UpdatePosition_WithCustomTimeStep_UpdatesPosition()
        {
            // Arrange
            var ball = new BallModel(100, 100);
            ball.VelocityX = 60; // 60 pixels per second
            ball.VelocityY = 30; // 30 pixels per second
            double timeStep = 0.5; // Half a second
            
            // Act
            ball.UpdatePosition(timeStep);
            
            // Assert
            Assert.AreEqual(130, ball.X, 0.001); // 100 + 60 * 0.5 = 130
            Assert.AreEqual(115, ball.Y, 0.001); // 100 + 30 * 0.5 = 115
        }
        
        [TestMethod]
        public void ApplyForce_ChangesVelocity()
        {
            // Arrange
            var ball = new BallModel();
            double forceX = 10;
            double forceY = 5;
            double timeStep = 1.0; // 1 second for easier calculation
            
            // Act
            ball.ApplyForce(forceX, forceY, timeStep);
            
            // Assert
            // F = ma, so a = F/m, and v = v0 + at
            // With m = 1, a = F, and v = 0 + F*t = F*1 = F
            Assert.AreEqual(forceX, ball.VelocityX);
            Assert.AreEqual(forceY, ball.VelocityY);
        }
        
        [TestMethod]
        public void SetVelocity_SetsVelocityDirectly()
        {
            // Arrange
            var ball = new BallModel();
            double velocityX = 15;
            double velocityY = -10;
            
            // Act
            ball.SetVelocity(velocityX, velocityY);
            
            // Assert
            Assert.AreEqual(velocityX, ball.VelocityX);
            Assert.AreEqual(velocityY, ball.VelocityY);
            Assert.IsTrue(ball.IsMoving);
        }
        
        [TestMethod]
        public void Stop_SetsVelocityToZero()
        {
            // Arrange
            var ball = new BallModel();
            ball.SetVelocity(10, 10);
            Assert.IsTrue(ball.IsMoving);
            
            // Act
            ball.Stop();
            
            // Assert
            Assert.AreEqual(0, ball.VelocityX);
            Assert.AreEqual(0, ball.VelocityY);
            Assert.IsFalse(ball.IsMoving);
        }
        
        [TestMethod]
        public void ContainsPoint_PointInsideBall_ReturnsTrue()
        {
            // Arrange
            var ball = new BallModel(100, 100, 20);
            
            // Act & Assert
            // Point at the center
            Assert.IsTrue(ball.ContainsPoint(100, 100));
            
            // Point inside the ball but not at the center
            Assert.IsTrue(ball.ContainsPoint(110, 110));
            
            // Point exactly on the edge of the ball
            Assert.IsTrue(ball.ContainsPoint(100 + 20, 100)); // Right edge
        }
        
        [TestMethod]
        public void ContainsPoint_PointOutsideBall_ReturnsFalse()
        {
            // Arrange
            var ball = new BallModel(100, 100, 20);
            
            // Act & Assert
            // Point just outside the ball
            Assert.IsFalse(ball.ContainsPoint(100 + 20.1, 100));
            
            // Point far from the ball
            Assert.IsFalse(ball.ContainsPoint(200, 200));
        }
        
        [TestMethod]
        public void GetPosition_ReturnsCorrectPoint()
        {
            // Arrange
            double x = 150;
            double y = 250;
            var ball = new BallModel(x, y);
            
            // Act
            Point position = ball.GetPosition();
            
            // Assert
            Assert.AreEqual(x, position.X);
            Assert.AreEqual(y, position.Y);
        }
        
        [TestMethod]
        public void SetPosition_WithCoordinates_SetsPosition()
        {
            // Arrange
            var ball = new BallModel();
            double x = 75;
            double y = 125;
            
            // Act
            ball.SetPosition(x, y);
            
            // Assert
            Assert.AreEqual(x, ball.X);
            Assert.AreEqual(y, ball.Y);
        }
        
        [TestMethod]
        public void SetPosition_WithPoint_SetsPosition()
        {
            // Arrange
            var ball = new BallModel();
            Point position = new Point(75, 125);
            
            // Act
            ball.SetPosition(position);
            
            // Assert
            Assert.AreEqual(position.X, ball.X);
            Assert.AreEqual(position.Y, ball.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_BallOutsideBoundaries_ConstrainsPosition()
        {
            // Arrange
            double radius = 10;
            var ball = new BallModel(0, 0, radius);
            double minX = 0;
            double minY = 0;
            double maxX = 100;
            double maxY = 100;
            
            // Act & Assert
            
            // Test left boundary
            ball.SetPosition(-5, 50);
            bool wasConstrained = ball.ConstrainPosition(minX, minY, maxX, maxY);
            Assert.IsTrue(wasConstrained);
            Assert.AreEqual(minX + radius, ball.X);
            Assert.AreEqual(50, ball.Y);
            
            // Test right boundary
            ball.SetPosition(105, 50);
            wasConstrained = ball.ConstrainPosition(minX, minY, maxX, maxY);
            Assert.IsTrue(wasConstrained);
            Assert.AreEqual(maxX - radius, ball.X);
            Assert.AreEqual(50, ball.Y);
            
            // Test top boundary
            ball.SetPosition(50, -5);
            wasConstrained = ball.ConstrainPosition(minX, minY, maxX, maxY);
            Assert.IsTrue(wasConstrained);
            Assert.AreEqual(50, ball.X);
            Assert.AreEqual(minY + radius, ball.Y);
            
            // Test bottom boundary
            ball.SetPosition(50, 105);
            wasConstrained = ball.ConstrainPosition(minX, minY, maxX, maxY);
            Assert.IsTrue(wasConstrained);
            Assert.AreEqual(50, ball.X);
            Assert.AreEqual(maxY - radius, ball.Y);
        }
        
        [TestMethod]
        public void ConstrainPosition_BallInsideBoundaries_DoesNotConstrainPosition()
        {
            // Arrange
            double radius = 10;
            var ball = new BallModel(50, 50, radius);
            double minX = 0;
            double minY = 0;
            double maxX = 100;
            double maxY = 100;
            
            // Act
            bool wasConstrained = ball.ConstrainPosition(minX, minY, maxX, maxY);
            
            // Assert
            Assert.IsFalse(wasConstrained);
            Assert.AreEqual(50, ball.X);
            Assert.AreEqual(50, ball.Y);
        }
        
        [TestMethod]
        public void IsMoving_WithZeroVelocity_ReturnsFalse()
        {
            // Arrange
            var ball = new BallModel();
            
            // Act & Assert
            Assert.IsFalse(ball.IsMoving);
        }
        
        [TestMethod]
        public void IsMoving_WithNonZeroVelocity_ReturnsTrue()
        {
            // Arrange
            var ball = new BallModel();
            
            // Act
            ball.SetVelocity(0.02, 0); // Just above the threshold
            
            // Assert
            Assert.IsTrue(ball.IsMoving);
        }
        
        [TestMethod]
        public void IsMoving_WithVelocityBelowThreshold_ReturnsFalse()
        {
            // Arrange
            var ball = new BallModel();
            
            // Act
            ball.SetVelocity(0.005, 0.005); // Below the threshold
            
            // Assert
            Assert.IsFalse(ball.IsMoving);
        }
    }
}