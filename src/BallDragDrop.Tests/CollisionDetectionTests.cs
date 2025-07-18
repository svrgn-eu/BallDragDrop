using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using System;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class CollisionDetectionTests
    {
        [TestMethod]
        public void DetectCollision_BallsOverlapping_ReturnsTrue()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(140, 100, 25); // Distance between centers is 40, sum of radii is 50
            
            // Act
            bool isColliding = engine.DetectCollision(ball1, ball2);
            
            // Assert
            Assert.IsTrue(isColliding);
        }
        
        [TestMethod]
        public void DetectCollision_BallsTouching_ReturnsTrue()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(149, 100, 25); // Distance between centers is 49, sum of radii is 50 (slightly overlapping)
            
            // Act
            bool isColliding = engine.DetectCollision(ball1, ball2);
            
            // Assert
            Assert.IsTrue(isColliding);
        }
        
        [TestMethod]
        public void DetectCollision_BallsNotTouching_ReturnsFalse()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(160, 100, 25); // Distance between centers is 60, sum of radii is 50
            
            // Act
            bool isColliding = engine.DetectCollision(ball1, ball2);
            
            // Assert
            Assert.IsFalse(isColliding);
        }
        
        [TestMethod]
        public void DetectAndResolveCollision_BallsColliding_UpdatesVelocities()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0.0, 1.0); // No friction, perfect bounce
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(140, 100, 25); // Overlapping
            
            ball1.SetVelocity(50, 0);  // Moving right
            ball2.SetVelocity(-50, 0); // Moving left
            
            double initialVelocityX1 = ball1.VelocityX;
            double initialVelocityX2 = ball2.VelocityX;
            
            // Act
            bool collided = engine.DetectAndResolveCollision(ball1, ball2);
            
            // Assert
            Assert.IsTrue(collided);
            Assert.IsTrue(ball1.VelocityX < 0); // Ball 1 should now be moving left
            Assert.IsTrue(ball2.VelocityX > 0); // Ball 2 should now be moving right
            Assert.AreNotEqual(initialVelocityX1, ball1.VelocityX);
            Assert.AreNotEqual(initialVelocityX2, ball2.VelocityX);
        }
        
        [TestMethod]
        public void DetectAndResolveCollision_BallsColliding_CorrectPositions()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(140, 100, 25); // Overlapping by 10 pixels
            
            double initialDistance = engine.CalculateDistance(ball1.X, ball1.Y, ball2.X, ball2.Y);
            double minDistance = ball1.Radius + ball2.Radius;
            
            // Act
            engine.DetectAndResolveCollision(ball1, ball2);
            double newDistance = engine.CalculateDistance(ball1.X, ball1.Y, ball2.X, ball2.Y);
            
            // Assert
            Assert.IsTrue(initialDistance < minDistance); // Confirm they were overlapping
            Assert.IsTrue(newDistance >= minDistance - 0.001); // Allow for floating point precision
        }
        
        [TestMethod]
        public void DetectAndResolveCollision_BallsMovingAway_DoesNotChangeVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball1 = new BallModel(100, 100, 25);
            var ball2 = new BallModel(140, 100, 25); // Overlapping
            
            ball1.SetVelocity(-50, 0); // Moving left (away from ball2)
            ball2.SetVelocity(50, 0);  // Moving right (away from ball1)
            
            double initialVelocityX1 = ball1.VelocityX;
            double initialVelocityX2 = ball2.VelocityX;
            
            // Act
            bool collided = engine.DetectAndResolveCollision(ball1, ball2);
            
            // Assert
            Assert.IsTrue(collided); // They are still colliding spatially
            Assert.AreEqual(initialVelocityX1, ball1.VelocityX); // But velocities shouldn't change
            Assert.AreEqual(initialVelocityX2, ball2.VelocityX);
        }
        
        [TestMethod]
        public void DetectAndResolveCollision_DifferentMasses_ProportionalVelocityChange()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0.0, 1.0); // No friction, perfect bounce
            var ball1 = new BallModel(100, 100, 25) { Mass = 1 };
            var ball2 = new BallModel(140, 100, 25) { Mass = 2 }; // Twice the mass
            
            ball1.SetVelocity(60, 0);  // Moving right
            ball2.SetVelocity(-30, 0); // Moving left
            
            // Act
            engine.DetectAndResolveCollision(ball1, ball2);
            
            // Assert
            // The lighter ball should experience a greater change in velocity
            Assert.IsTrue(Math.Abs(ball1.VelocityX) > Math.Abs(ball2.VelocityX));
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectCollision_FirstBallNull_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball = new BallModel(100, 100, 25);
            
            // Act
            engine.DetectCollision(null, ball);
            
            // Assert is handled by ExpectedException attribute
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectCollision_SecondBallNull_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball = new BallModel(100, 100, 25);
            
            // Act
            engine.DetectCollision(ball, null);
            
            // Assert is handled by ExpectedException attribute
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectAndResolveCollision_FirstBallNull_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball = new BallModel(100, 100, 25);
            
            // Act
            engine.DetectAndResolveCollision(null, ball);
            
            // Assert is handled by ExpectedException attribute
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DetectAndResolveCollision_SecondBallNull_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball = new BallModel(100, 100, 25);
            
            // Act
            engine.DetectAndResolveCollision(ball, null);
            
            // Assert is handled by ExpectedException attribute
        }
    }
}