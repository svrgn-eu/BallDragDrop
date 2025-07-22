using System;
using System.Windows;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Provides physics calculations for ball movement including velocity, friction, and collisions.
    /// </summary>
    public class PhysicsEngine
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the friction coefficient (0-1 where 1 is no friction)
        /// </summary>
        public double FrictionCoefficient { get; set; }

        /// <summary>
        /// Gets or sets the gravity acceleration in pixels per second squared
        /// </summary>
        public double Gravity { get; set; }

        /// <summary>
        /// Gets or sets the bounce elasticity factor (0-1 where 1 is perfect bounce)
        /// </summary>
        public double BounceFactor { get; set; }

        #endregion Properties

        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the PhysicsEngine class with default values.
        /// </summary>
        public PhysicsEngine()
        {
            FrictionCoefficient = DefaultFrictionCoefficient;
            Gravity = DefaultGravity;
            BounceFactor = DefaultBounceFactor;
        }
        
        /// <summary>
        /// Initializes a new instance of the PhysicsEngine class with specified values.
        /// </summary>
        /// <param name="frictionCoefficient">Coefficient of friction (0-1 where 1 is no friction)</param>
        /// <param name="gravity">Gravity acceleration in pixels per second squared</param>
        /// <param name="bounceFactor">Bounce elasticity factor (0-1 where 1 is perfect bounce)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when friction coefficient or bounce factor is not between 0 and 1</exception>
        public PhysicsEngine(double frictionCoefficient, double gravity, double bounceFactor)
        {
            FrictionCoefficient = Math.Clamp(frictionCoefficient, 0.0, 1.0);
            Gravity = gravity;
            BounceFactor = Math.Clamp(bounceFactor, 0.0, 1.0);
        }

        #endregion Construction

        #region Constants

        /// <summary>
        /// Default friction coefficient for ball movement
        /// </summary>
        private const double DefaultFrictionCoefficient = 0.98;

        /// <summary>
        /// Default gravity acceleration
        /// </summary>
        private const double DefaultGravity = 0.0;

        /// <summary>
        /// Default bounce factor for collisions
        /// </summary>
        private const double DefaultBounceFactor = 0.8;

        /// <summary>
        /// Velocity threshold below which the ball is considered stopped
        /// </summary>
        private const double VelocityThreshold = 0.1;

        #endregion Constants

        #region Methods
        
        /// <summary>
        /// Updates the ball's position and velocity based on physics calculations.
        /// </summary>
        /// <param name="ball">The ball model to update</param>
        /// <param name="timeStep">Time step for the update in seconds</param>
        /// <param name="minX">Minimum X boundary</param>
        /// <param name="minY">Minimum Y boundary</param>
        /// <param name="maxX">Maximum X boundary</param>
        /// <param name="maxY">Maximum Y boundary</param>
        /// <returns>A tuple containing whether the ball is still moving and which boundaries were hit</returns>
        public (bool IsMoving, bool HitLeft, bool HitRight, bool HitTop, bool HitBottom) UpdateBall(BallModel ball, double timeStep, double minX, double minY, double maxX, double maxY)
        {
            if (ball == null)
                throw new ArgumentNullException(nameof(ball));
                
            // Apply gravity
            ball.VelocityY += Gravity * timeStep;
            
            // Apply friction
            ball.VelocityX *= Math.Pow(FrictionCoefficient, timeStep * 60); // Scale friction by frame rate
            ball.VelocityY *= Math.Pow(FrictionCoefficient, timeStep * 60);
            
            // Stop the ball if it's moving very slowly
            if (Math.Abs(ball.VelocityX) < VelocityThreshold && Math.Abs(ball.VelocityY) < VelocityThreshold)
            {
                ball.Stop();
                return (false, false, false, false, false);
            }
            
            // Update position
            double newX = ball.X + ball.VelocityX * timeStep;
            double newY = ball.Y + ball.VelocityY * timeStep;
            
            // Handle collisions with boundaries
            var collisionResult = HandleCollisions(ball, ref newX, ref newY, minX, minY, maxX, maxY);
            
            // Update ball position
            ball.SetPosition(newX, newY);
            
            return (ball.IsMoving, collisionResult.HitLeft, collisionResult.HitRight, collisionResult.HitTop, collisionResult.HitBottom);
        }
        
        /// <summary>
        /// Handles collisions with boundaries and updates velocity for bouncing.
        /// </summary>
        /// <param name="ball">The ball model</param>
        /// <param name="newX">New X position (will be modified if collision occurs)</param>
        /// <param name="newY">New Y position (will be modified if collision occurs)</param>
        /// <param name="minX">Minimum X boundary</param>
        /// <param name="minY">Minimum Y boundary</param>
        /// <param name="maxX">Maximum X boundary</param>
        /// <param name="maxY">Maximum Y boundary</param>
        /// <returns>A tuple indicating which boundaries were hit (left, right, top, bottom)</returns>
        private (bool HitLeft, bool HitRight, bool HitTop, bool HitBottom) HandleCollisions(BallModel ball, ref double newX, ref double newY, double minX, double minY, double maxX, double maxY)
        {
            // Adjust for the ball's radius
            double effectiveMinX = minX + ball.Radius;
            double effectiveMinY = minY + ball.Radius;
            double effectiveMaxX = maxX - ball.Radius;
            double effectiveMaxY = maxY - ball.Radius;
            
            bool hitLeft = false;
            bool hitRight = false;
            bool hitTop = false;
            bool hitBottom = false;
            
            // Check for X-axis collisions
            if (newX < effectiveMinX)
            {
                // Collision with left boundary
                newX = effectiveMinX + (effectiveMinX - newX) * BounceFactor;
                ball.VelocityX = -ball.VelocityX * BounceFactor;
                hitLeft = true;
            }
            else if (newX > effectiveMaxX)
            {
                // Collision with right boundary
                newX = effectiveMaxX - (newX - effectiveMaxX) * BounceFactor;
                ball.VelocityX = -ball.VelocityX * BounceFactor;
                hitRight = true;
            }
            
            // Check for Y-axis collisions
            if (newY < effectiveMinY)
            {
                // Collision with top boundary
                newY = effectiveMinY + (effectiveMinY - newY) * BounceFactor;
                ball.VelocityY = -ball.VelocityY * BounceFactor;
                hitTop = true;
            }
            else if (newY > effectiveMaxY)
            {
                // Collision with bottom boundary
                newY = effectiveMaxY - (newY - effectiveMaxY) * BounceFactor;
                ball.VelocityY = -ball.VelocityY * BounceFactor;
                hitBottom = true;
            }
            
            return (hitLeft, hitRight, hitTop, hitBottom);
        }
        
        /// <summary>
        /// Calculates the velocity based on the movement delta and time elapsed.
        /// </summary>
        /// <param name="deltaX">Change in X position</param>
        /// <param name="deltaY">Change in Y position</param>
        /// <param name="timeElapsed">Time elapsed in seconds</param>
        /// <returns>A tuple containing the X and Y velocity components</returns>
        public (double VelocityX, double VelocityY) CalculateVelocity(double deltaX, double deltaY, double timeElapsed)
        {
            if (timeElapsed <= 0)
                return (0, 0);
                
            // Calculate velocity components
            double velocityX = deltaX / timeElapsed;
            double velocityY = deltaY / timeElapsed;
            
            return (velocityX, velocityY);
        }
        
        /// <summary>
        /// Calculates velocity based on a series of mouse positions and timestamps.
        /// This provides a more accurate velocity calculation by considering multiple data points.
        /// </summary>
        /// <param name="positions">Array of mouse positions</param>
        /// <param name="timestamps">Array of timestamps corresponding to the positions</param>
        /// <param name="count">Number of valid positions to consider (most recent ones)</param>
        /// <returns>A tuple containing the X and Y velocity components</returns>
        public (double VelocityX, double VelocityY) CalculateVelocityFromHistory(Point[] positions, DateTime[] timestamps, int count)
        {
            if (positions == null || timestamps == null || count <= 1 || positions.Length < count || timestamps.Length < count)
                return (0, 0);
                
            // Use weighted average of velocities from the last few position updates
            // More recent movements have higher weight
            double totalWeightedVelocityX = 0;
            double totalWeightedVelocityY = 0;
            double totalWeight = 0;
            
            for (int i = 1; i < count; i++)
            {
                // Calculate time difference in seconds
                double timeDiff = (timestamps[i] - timestamps[i-1]).TotalSeconds;
                
                // Skip if time difference is too small to avoid division by very small numbers
                if (timeDiff < 0.001)
                    continue;
                    
                // Calculate position difference
                double deltaX = positions[i].X - positions[i-1].X;
                double deltaY = positions[i].Y - positions[i-1].Y;
                
                // Calculate instantaneous velocity
                double velocityX = deltaX / timeDiff;
                double velocityY = deltaY / timeDiff;
                
                // Weight is higher for more recent movements (i is higher)
                double weight = i;
                
                // Add weighted velocity to totals
                totalWeightedVelocityX += velocityX * weight;
                totalWeightedVelocityY += velocityY * weight;
                totalWeight += weight;
            }
            
            // Calculate weighted average velocity
            if (totalWeight > 0)
            {
                return (totalWeightedVelocityX / totalWeight, totalWeightedVelocityY / totalWeight);
            }
            
            return (0, 0);
        }
        
        /// <summary>
        /// Determines if a movement should be considered a throw based on velocity.
        /// </summary>
        /// <param name="velocityX">X velocity component</param>
        /// <param name="velocityY">Y velocity component</param>
        /// <param name="throwThreshold">Velocity threshold for considering a movement a throw</param>
        /// <returns>True if the movement is a throw, false otherwise</returns>
        public bool IsThrow(double velocityX, double velocityY, double throwThreshold = 200.0)
        {
            // Calculate the magnitude of the velocity vector
            double velocityMagnitude = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
            
            // If the velocity is above the threshold, consider it a throw
            return velocityMagnitude > throwThreshold;
        }
        
        /// <summary>
        /// Determines if a movement should be considered a throw based on velocity and movement pattern.
        /// </summary>
        /// <param name="positions">Array of mouse positions</param>
        /// <param name="timestamps">Array of timestamps corresponding to the positions</param>
        /// <param name="count">Number of valid positions to consider</param>
        /// <param name="throwThreshold">Velocity threshold for considering a movement a throw</param>
        /// <returns>True if the movement is a throw, false otherwise</returns>
        public bool IsThrowFromHistory(Point[] positions, DateTime[] timestamps, int count, double throwThreshold = 200.0)
        {
            // Calculate velocity using the history
            var (velocityX, velocityY) = CalculateVelocityFromHistory(positions, timestamps, count);
            
            // Check if it's a throw based on the calculated velocity
            return IsThrow(velocityX, velocityY, throwThreshold);
        }
        
        /// <summary>
        /// Applies a force to the ball, changing its velocity.
        /// </summary>
        /// <param name="ball">The ball model</param>
        /// <param name="forceX">Force in X direction</param>
        /// <param name="forceY">Force in Y direction</param>
        /// <param name="timeStep">Time step for the force application in seconds</param>
        public void ApplyForce(BallModel ball, double forceX, double forceY, double timeStep = 1.0/60.0)
        {
            if (ball == null)
                throw new ArgumentNullException(nameof(ball));
                
            ball.ApplyForce(forceX, forceY, timeStep);
        }
        
        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        /// <param name="x1">X coordinate of the first point</param>
        /// <param name="y1">Y coordinate of the first point</param>
        /// <param name="x2">X coordinate of the second point</param>
        /// <param name="y2">Y coordinate of the second point</param>
        /// <returns>The distance between the two points</returns>
        public double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }
    
  
   /// <summary>
        /// Detects collision between two balls and handles the physics response.
        /// </summary>
        /// <param name="ball1">The first ball</param>
        /// <param name="ball2">The second ball</param>
        /// <returns>True if the balls collided, false otherwise</returns>
        public bool DetectAndResolveCollision(BallModel ball1, BallModel ball2)
        {
            if (ball1 == null || ball2 == null)
                throw new ArgumentNullException(ball1 == null ? nameof(ball1) : nameof(ball2));
                
            // Calculate distance between ball centers
            double distance = CalculateDistance(ball1.X, ball1.Y, ball2.X, ball2.Y);
            
            // Sum of radii
            double minDistance = ball1.Radius + ball2.Radius;
            
            // If distance is less than sum of radii, collision occurred
            if (distance < minDistance)
            {
                // Calculate normal vector (direction from ball1 to ball2)
                double nx = (ball2.X - ball1.X) / distance;
                double ny = (ball2.Y - ball1.Y) / distance;
                
                // Calculate relative velocity
                double relativeVelocityX = ball2.VelocityX - ball1.VelocityX;
                double relativeVelocityY = ball2.VelocityY - ball1.VelocityY;
                
                // Calculate relative velocity along the normal
                double velocityAlongNormal = relativeVelocityX * nx + relativeVelocityY * ny;
                
                // If balls are moving away from each other, no collision response needed
                if (velocityAlongNormal > 0)
                    return true;
                
                // Calculate restitution (bounce factor)
                double restitution = BounceFactor;
                
                // Calculate impulse scalar
                double impulseScalar = -(1 + restitution) * velocityAlongNormal;
                impulseScalar /= 1 / ball1.Mass + 1 / ball2.Mass;
                
                // Apply impulse
                double impulseX = impulseScalar * nx;
                double impulseY = impulseScalar * ny;
                
                // Update velocities
                ball1.VelocityX -= impulseX / ball1.Mass;
                ball1.VelocityY -= impulseY / ball1.Mass;
                ball2.VelocityX += impulseX / ball2.Mass;
                ball2.VelocityY += impulseY / ball2.Mass;
                
                // Correct position to prevent balls from sticking together
                double overlap = minDistance - distance;
                double correctionX = overlap * nx * 0.5; // Split correction between both balls
                double correctionY = overlap * ny * 0.5;
                
                ball1.X -= correctionX;
                ball1.Y -= correctionY;
                ball2.X += correctionX;
                ball2.Y += correctionY;
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Detects if two balls are colliding without resolving the collision.
        /// </summary>
        /// <param name="ball1">The first ball</param>
        /// <param name="ball2">The second ball</param>
        /// <returns>True if the balls are colliding, false otherwise</returns>
        public bool DetectCollision(BallModel ball1, BallModel ball2)
        {
            if (ball1 == null || ball2 == null)
                throw new ArgumentNullException(ball1 == null ? nameof(ball1) : nameof(ball2));
                
            // Calculate distance between ball centers
            double distance = CalculateDistance(ball1.X, ball1.Y, ball2.X, ball2.Y);
            
            // Sum of radii
            double minDistance = ball1.Radius + ball2.Radius;
            
            // If distance is less than sum of radii, collision occurred
            return distance < minDistance;
        }

        #endregion Methods
    }
}