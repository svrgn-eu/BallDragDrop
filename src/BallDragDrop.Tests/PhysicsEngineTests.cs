using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Models;
using System;
using System.Windows;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class PhysicsEngineTests
    {
        [TestMethod]
        public void Constructor_InitializesDefaultValues()
        {
            // Arrange & Act
            var engine = new PhysicsEngine();
            
            // Assert
            Assert.AreEqual(0.98, engine.FrictionCoefficient);
            Assert.AreEqual(0.0, engine.Gravity);
            Assert.AreEqual(0.8, engine.BounceFactor);
        }
        
        [TestMethod]
        public void Constructor_WithParameters_InitializesCorrectValues()
        {
            // Arrange & Act
            var engine = new PhysicsEngine(0.95, 9.8, 0.7);
            
            // Assert
            Assert.AreEqual(0.95, engine.FrictionCoefficient);
            Assert.AreEqual(9.8, engine.Gravity);
            Assert.AreEqual(0.7, engine.BounceFactor);
        }
        
        [TestMethod]
        public void Constructor_ClampsFrictionCoefficient()
        {
            // Arrange & Act
            var engine1 = new PhysicsEngine(-0.5, 0, 0.5); // Below minimum
            var engine2 = new PhysicsEngine(1.5, 0, 0.5);  // Above maximum
            
            // Assert
            Assert.AreEqual(0.0, engine1.FrictionCoefficient); // Should be clamped to 0
            Assert.AreEqual(1.0, engine2.FrictionCoefficient); // Should be clamped to 1
        }
        
        [TestMethod]
        public void Constructor_ClampsBounceFactor()
        {
            // Arrange & Act
            var engine1 = new PhysicsEngine(0.5, 0, -0.5); // Below minimum
            var engine2 = new PhysicsEngine(0.5, 0, 1.5);  // Above maximum
            
            // Assert
            Assert.AreEqual(0.0, engine1.BounceFactor); // Should be clamped to 0
            Assert.AreEqual(1.0, engine2.BounceFactor); // Should be clamped to 1
        }
        
        [TestMethod]
        public void UpdateBall_AppliesFriction_SlowsDownBall()
        {
            // Arrange
            var engine = new PhysicsEngine(0.9, 0, 0.8); // High friction
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(100, 100); // Initial velocity
            double timeStep = 1.0 / 60.0; // 60 fps
            
            // Act
            engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.IsTrue(ball.VelocityX < 100); // Velocity should decrease
            Assert.IsTrue(ball.VelocityY < 100);
        }
        
        [TestMethod]
        public void UpdateBall_WithGravity_IncreasesYVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 9.8, 0.8); // No friction, with gravity
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(0, 0); // Start with no velocity
            double timeStep = 1.0 / 60.0; // 60 fps
            
            // Act
            engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.AreEqual(0, ball.VelocityX); // X velocity should remain unchanged
            Assert.IsTrue(ball.VelocityY > 0); // Y velocity should increase due to gravity
        }
        
        [TestMethod]
        public void UpdateBall_StopsBallWhenVelocityIsBelowThreshold()
        {
            // Arrange
            var engine = new PhysicsEngine(0.9, 0, 0.8);
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(0.05, 0.05); // Very low velocity, below threshold
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.IsFalse(result.IsMoving); // Ball should be stopped
            Assert.AreEqual(0, ball.VelocityX);
            Assert.AreEqual(0, ball.VelocityY);
        }
        
        [TestMethod]
        public void UpdateBall_CollisionWithRightBoundary_ReverseXVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 1.0); // No friction, perfect bounce
            var ball = new BallModel(375, 100, 25);
            ball.SetVelocity(100, 0); // Moving right
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.IsTrue(ball.VelocityX < 0); // X velocity should be reversed
            Assert.AreEqual(0, ball.VelocityY); // Y velocity should remain unchanged
            Assert.IsTrue(result.HitRight); // Should report right boundary collision
            Assert.IsFalse(result.HitLeft);
            Assert.IsFalse(result.HitTop);
            Assert.IsFalse(result.HitBottom);
        }
        
        [TestMethod]
        public void UpdateBall_CollisionWithLeftBoundary_ReverseXVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 1.0); // No friction, perfect bounce
            var ball = new BallModel(25, 100, 25);
            ball.SetVelocity(-100, 0); // Moving left
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.IsTrue(ball.VelocityX > 0); // X velocity should be reversed
            Assert.AreEqual(0, ball.VelocityY); // Y velocity should remain unchanged
            Assert.IsTrue(result.HitLeft); // Should report left boundary collision
            Assert.IsFalse(result.HitRight);
            Assert.IsFalse(result.HitTop);
            Assert.IsFalse(result.HitBottom);
        }
        
        [TestMethod]
        public void UpdateBall_CollisionWithBottomBoundary_ReverseYVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 1.0); // No friction, perfect bounce
            var ball = new BallModel(100, 375, 25);
            ball.SetVelocity(0, 100); // Moving down
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.AreEqual(0, ball.VelocityX); // X velocity should remain unchanged
            Assert.IsTrue(ball.VelocityY < 0); // Y velocity should be reversed
            Assert.IsTrue(result.HitBottom); // Should report bottom boundary collision
            Assert.IsFalse(result.HitLeft);
            Assert.IsFalse(result.HitRight);
            Assert.IsFalse(result.HitTop);
        }
        
        [TestMethod]
        public void UpdateBall_CollisionWithTopBoundary_ReverseYVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 1.0); // No friction, perfect bounce
            var ball = new BallModel(100, 25, 25);
            ball.SetVelocity(0, -100); // Moving up
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.AreEqual(0, ball.VelocityX); // X velocity should remain unchanged
            Assert.IsTrue(ball.VelocityY > 0); // Y velocity should be reversed
            Assert.IsTrue(result.HitTop); // Should report top boundary collision
            Assert.IsFalse(result.HitLeft);
            Assert.IsFalse(result.HitRight);
            Assert.IsFalse(result.HitBottom);
        }
        
        [TestMethod]
        public void UpdateBall_WithBounceFactor_ReducesVelocityAfterCollision()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 0.5); // No friction, 50% bounce
            var ball = new BallModel(375, 100, 25);
            ball.SetVelocity(100, 0); // Moving right
            double timeStep = 1.0 / 60.0;
            
            // Act
            engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.AreEqual(-50, ball.VelocityX, 0.1); // X velocity should be reversed and reduced by 50%
        }
        
        [TestMethod]
        public void CalculateVelocity_ReturnsCorrectVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            double deltaX = 100;
            double deltaY = 50;
            double timeElapsed = 0.5; // Half a second
            
            // Act
            var (velocityX, velocityY) = engine.CalculateVelocity(deltaX, deltaY, timeElapsed);
            
            // Assert
            Assert.AreEqual(200, velocityX); // 100 / 0.5 = 200
            Assert.AreEqual(100, velocityY); // 50 / 0.5 = 100
        }
        
        [TestMethod]
        public void CalculateVelocity_WithZeroTime_ReturnsZeroVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            double deltaX = 100;
            double deltaY = 50;
            double timeElapsed = 0; // Zero time
            
            // Act
            var (velocityX, velocityY) = engine.CalculateVelocity(deltaX, deltaY, timeElapsed);
            
            // Assert
            Assert.AreEqual(0, velocityX);
            Assert.AreEqual(0, velocityY);
        }
        
        [TestMethod]
        public void IsThrow_AboveThreshold_ReturnsTrue()
        {
            // Arrange
            var engine = new PhysicsEngine();
            double velocityX = 150;
            double velocityY = 150;
            double threshold = 200; // Default threshold
            
            // Act
            bool isThrow = engine.IsThrow(velocityX, velocityY, threshold);
            
            // Assert
            Assert.IsTrue(isThrow); // Velocity magnitude is ~212, which is > 200
        }
        
        [TestMethod]
        public void IsThrow_BelowThreshold_ReturnsFalse()
        {
            // Arrange
            var engine = new PhysicsEngine();
            double velocityX = 100;
            double velocityY = 100;
            double threshold = 200; // Default threshold
            
            // Act
            bool isThrow = engine.IsThrow(velocityX, velocityY, threshold);
            
            // Assert
            Assert.IsFalse(isThrow); // Velocity magnitude is ~141, which is < 200
        }
        
        [TestMethod]
        public void ApplyForce_ChangesVelocity()
        {
            // Arrange
            var engine = new PhysicsEngine();
            var ball = new BallModel(100, 100, 25);
            ball.SetVelocity(0, 0); // Start with no velocity
            double forceX = 100;
            double forceY = 50;
            double timeStep = 1.0 / 60.0;
            
            // Act
            engine.ApplyForce(ball, forceX, forceY, timeStep);
            
            // Assert
            Assert.IsTrue(ball.VelocityX > 0);
            Assert.IsTrue(ball.VelocityY > 0);
        }
        
        [TestMethod]
        public void CalculateDistance_ReturnsCorrectDistance()
        {
            // Arrange
            var engine = new PhysicsEngine();
            double x1 = 0;
            double y1 = 0;
            double x2 = 3;
            double y2 = 4;
            
            // Act
            double distance = engine.CalculateDistance(x1, y1, x2, y2);
            
            // Assert
            Assert.AreEqual(5, distance); // Pythagorean theorem: sqrt(3^2 + 4^2) = 5
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UpdateBall_WithNullBall_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            
            // Act
            engine.UpdateBall(null, 1.0 / 60.0, 0, 0, 400, 400);
            
            // Assert is handled by ExpectedException attribute
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ApplyForce_WithNullBall_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new PhysicsEngine();
            
            // Act
            engine.ApplyForce(null, 100, 100);
            
            // Assert is handled by ExpectedException attribute
        }
        [TestMethod]
        public void UpdateBall_CollisionWithCorner_ReportsBothCollisions()
        {
            // Arrange
            var engine = new PhysicsEngine(1.0, 0, 1.0); // No friction, perfect bounce
            var ball = new BallModel(375, 375, 25);
            ball.SetVelocity(100, 100); // Moving toward bottom-right corner
            double timeStep = 1.0 / 60.0;
            
            // Act
            var result = engine.UpdateBall(ball, timeStep, 0, 0, 400, 400);
            
            // Assert
            Assert.IsTrue(ball.VelocityX < 0); // X velocity should be reversed
            Assert.IsTrue(ball.VelocityY < 0); // Y velocity should be reversed
            Assert.IsTrue(result.HitRight); // Should report right boundary collision
            Assert.IsTrue(result.HitBottom); // Should report bottom boundary collision
            Assert.IsFalse(result.HitLeft);
            Assert.IsFalse(result.HitTop);
        }
    }
}