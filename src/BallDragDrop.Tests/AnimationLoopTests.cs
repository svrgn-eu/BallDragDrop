using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using BallDragDrop.Models;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class AnimationLoopTests
    {
        [TestMethod]
        public void AnimationLoop_UpdatesBallPosition_WhenBallHasVelocity()
        {
            // Arrange
            var ballModel = new BallModel(100, 100, 25);
            ballModel.SetVelocity(50, 30); // Set initial velocity
            
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Use reflection to access the private _ballModel field and replace it with our test model
            var fieldInfo = typeof(BallViewModel).GetField("_ballModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(viewModel, ballModel);
            
            var physicsEngine = new PhysicsEngine();
            
            // Act
            // Simulate a physics update with a fixed time step
            double timeStep = 1.0 / 60.0; // 60 FPS
            var result = physicsEngine.UpdateBall(ballModel, timeStep, 0, 0, 800, 600);
            
            // Calculate expected position after the update
            double expectedX = 100 + (50 * timeStep);
            double expectedY = 100 + (30 * timeStep);
            
            // Assert
            Assert.IsTrue(result.IsMoving, "Ball should still be moving after update");
            Assert.AreEqual(expectedX, ballModel.X, 0.1, "X position should be updated according to velocity");
            Assert.AreEqual(expectedY, ballModel.Y, 0.1, "Y position should be updated according to velocity");
        }
        
        [TestMethod]
        public void AnimationLoop_AppliesFriction_SlowingBallOverTime()
        {
            // Arrange
            var ballModel = new BallModel(100, 100, 25);
            ballModel.SetVelocity(50, 30); // Set initial velocity
            
            var physicsEngine = new PhysicsEngine();
            double initialVelocityX = ballModel.VelocityX;
            double initialVelocityY = ballModel.VelocityY;
            
            // Act
            // Simulate multiple physics updates to see friction effect
            for (int i = 0; i < 10; i++)
            {
                physicsEngine.UpdateBall(ballModel, 1.0 / 60.0, 0, 0, 800, 600);
            }
            
            // Assert
            Assert.IsTrue(Math.Abs(ballModel.VelocityX) < Math.Abs(initialVelocityX), 
                "X velocity should decrease due to friction");
            Assert.IsTrue(Math.Abs(ballModel.VelocityY) < Math.Abs(initialVelocityY), 
                "Y velocity should decrease due to friction");
        }
        
        [TestMethod]
        public void AnimationLoop_StopsBall_WhenVelocityBelowThreshold()
        {
            // Arrange
            var ballModel = new BallModel(100, 100, 25);
            // Set a very small velocity, just above the threshold
            ballModel.SetVelocity(0.2, 0.2);
            
            var physicsEngine = new PhysicsEngine();
            
            // Act
            // Simulate multiple physics updates until the ball stops
            bool isStopped = false;
            int maxIterations = 100; // Prevent infinite loop
            int iterations = 0;
            
            while (!isStopped && iterations < maxIterations)
            {
                var result = physicsEngine.UpdateBall(ballModel, 1.0 / 60.0, 0, 0, 800, 600);
                isStopped = !result.IsMoving;
                iterations++;
            }
            
            // Assert
            Assert.IsTrue(isStopped, "Ball should eventually stop due to friction");
            Assert.AreEqual(0, ballModel.VelocityX, 0.001, "X velocity should be zero when stopped");
            Assert.AreEqual(0, ballModel.VelocityY, 0.001, "Y velocity should be zero when stopped");
        }
        
        [TestMethod]
        public void AnimationLoop_HandlesBoundaryCollision_BouncingBall()
        {
            // Arrange
            double windowWidth = 800;
            double windowHeight = 600;
            double ballRadius = 25;
            
            // Position the ball near the right edge
            var ballModel = new BallModel(windowWidth - ballRadius - 5, 100, ballRadius);
            // Set velocity toward the right edge
            ballModel.SetVelocity(50, 0);
            
            var physicsEngine = new PhysicsEngine();
            
            // Act
            // Simulate physics updates until collision occurs
            bool hitBoundary = false;
            int maxIterations = 10; // Should hit within a few frames
            
            for (int i = 0; i < maxIterations && !hitBoundary; i++)
            {
                var result = physicsEngine.UpdateBall(ballModel, 1.0 / 60.0, 0, 0, windowWidth, windowHeight);
                hitBoundary = result.HitRight;
            }
            
            // Assert
            Assert.IsTrue(hitBoundary, "Ball should hit the right boundary");
            Assert.IsTrue(ballModel.VelocityX < 0, "Ball should bounce and have negative X velocity after hitting right wall");
        }
        
        [TestMethod]
        public void AnimationLoop_MaintainsConstantTimeStep_ForSmoothAnimation()
        {
            // Arrange
            var ballModel = new BallModel(100, 100, 25);
            ballModel.SetVelocity(50, 30);
            
            var physicsEngine = new PhysicsEngine();
            
            // Act & Assert
            // Simulate varying frame rates and ensure position updates are proportional to time
            double[] timeSteps = { 1.0/30.0, 1.0/60.0, 1.0/120.0 };
            
            foreach (double timeStep in timeSteps)
            {
                // Reset ball position
                ballModel.SetPosition(100, 100);
                ballModel.SetVelocity(50, 30);
                
                // Update with current time step
                physicsEngine.UpdateBall(ballModel, timeStep, 0, 0, 800, 600);
                
                // Calculate expected position
                double expectedX = 100 + (50 * timeStep);
                double expectedY = 100 + (30 * timeStep);
                
                // Position should be updated proportionally to the time step
                Assert.AreEqual(expectedX, ballModel.X, 0.1, 
                    $"X position should be updated proportionally with time step {timeStep}");
                Assert.AreEqual(expectedY, ballModel.Y, 0.1, 
                    $"Y position should be updated proportionally with time step {timeStep}");
            }
        }
    }
}