using System;
using System.Windows;

namespace BallDragDrop.Models
{
    /// <summary>
    /// Represents the data model for a ball in the application.
    /// Contains properties for position, velocity, size, and other physical attributes.
    /// </summary>
    public class BallModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the X position of the ball
        /// </summary>
        public double X 
        { 
            get => _x;
            set => _x = ValidateDouble(value, _x);
        }
        private double _x;

        /// <summary>
        /// Gets or sets the Y position of the ball
        /// </summary>
        public double Y 
        { 
            get => _y;
            set => _y = ValidateDouble(value, _y);
        }
        private double _y;
        
        /// <summary>
        /// Gets or sets the X velocity of the ball
        /// </summary>
        public double VelocityX 
        { 
            get => _velocityX;
            set => _velocityX = ValidateDouble(value, _velocityX);
        }
        private double _velocityX;

        /// <summary>
        /// Gets or sets the Y velocity of the ball
        /// </summary>
        public double VelocityY 
        { 
            get => _velocityY;
            set => _velocityY = ValidateDouble(value, _velocityY);
        }
        private double _velocityY;
        
        /// <summary>
        /// Gets or sets the radius of the ball
        /// </summary>
        public double Radius 
        { 
            get => _radius;
            set => _radius = ValidateDouble(value, _radius, 1.0); // Minimum radius of 1.0
        }
        private double _radius;

        /// <summary>
        /// Gets or sets the mass of the ball
        /// </summary>
        public double Mass 
        { 
            get => _mass;
            set => _mass = ValidateDouble(value, _mass, 0.1); // Minimum mass of 0.1
        }
        private double _mass;
        
        /// <summary>
        /// Gets a value indicating whether the ball is currently moving
        /// </summary>
        public bool IsMoving => Math.Abs(VelocityX) > 0.01 || Math.Abs(VelocityY) > 0.01;

        #endregion Properties

        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the BallModel class with default values.
        /// </summary>
        public BallModel()
        {
            // Default values - set backing fields directly to avoid validation during construction
            _x = 0;
            _y = 0;
            _velocityX = 0;
            _velocityY = 0;
            _radius = 25; // Default radius in pixels
            _mass = 1;    // Default mass (unitless)
        }
        
        /// <summary>
        /// Initializes a new instance of the BallModel class with specified position.
        /// </summary>
        /// <param name="x">Initial X position</param>
        /// <param name="y">Initial Y position</param>
        public BallModel(double x, double y) : this()
        {
            X = x;
            Y = y;
        }
        
        /// <summary>
        /// Initializes a new instance of the BallModel class with specified position and radius.
        /// </summary>
        /// <param name="x">Initial X position</param>
        /// <param name="y">Initial Y position</param>
        /// <param name="radius">Ball radius</param>
        public BallModel(double x, double y, double radius) : this(x, y)
        {
            Radius = radius;
        }

        #endregion Construction

        #region Methods
        
        /// <summary>
        /// Updates the ball's position based on its current velocity.
        /// </summary>
        /// <param name="timeStep">Time step for the update (in seconds)</param>
        public void UpdatePosition(double timeStep = 1.0/60.0)
        {
            X += VelocityX * timeStep;
            Y += VelocityY * timeStep;
        }
        
        /// <summary>
        /// Applies a force to the ball, changing its velocity.
        /// </summary>
        /// <param name="forceX">Force in X direction</param>
        /// <param name="forceY">Force in Y direction</param>
        /// <param name="timeStep">Time step for the force application (in seconds)</param>
        public void ApplyForce(double forceX, double forceY, double timeStep = 1.0/60.0)
        {
            // F = ma, so a = F/m
            double accelerationX = forceX / Mass;
            double accelerationY = forceY / Mass;
            
            // v = v0 + at
            VelocityX += accelerationX * timeStep;
            VelocityY += accelerationY * timeStep;
        }
        
        /// <summary>
        /// Sets the ball's velocity directly.
        /// </summary>
        /// <param name="velocityX">New X velocity</param>
        /// <param name="velocityY">New Y velocity</param>
        public void SetVelocity(double velocityX, double velocityY)
        {
            VelocityX = velocityX;
            VelocityY = velocityY;
        }
        
        /// <summary>
        /// Stops the ball's movement by setting its velocity to zero.
        /// </summary>
        public void Stop()
        {
            VelocityX = 0;
            VelocityY = 0;
        }
        
        /// <summary>
        /// Checks if a point is inside the ball.
        /// </summary>
        /// <param name="pointX">X coordinate of the point</param>
        /// <param name="pointY">Y coordinate of the point</param>
        /// <returns>True if the point is inside the ball, false otherwise</returns>
        public bool ContainsPoint(double pointX, double pointY)
        {
            // Calculate the distance from the center of the ball to the point
            double distanceSquared = Math.Pow(pointX - X, 2) + Math.Pow(pointY - Y, 2);
            
            // Check if the distance is less than or equal to the radius
            return distanceSquared <= Math.Pow(Radius, 2);
        }
        
        /// <summary>
        /// Gets the current position as a Point.
        /// </summary>
        /// <returns>A Point representing the ball's position</returns>
        public Point GetPosition()
        {
            return new Point(X, Y);
        }
        
        /// <summary>
        /// Sets the ball's position.
        /// </summary>
        /// <param name="x">New X position</param>
        /// <param name="y">New Y position</param>
        public void SetPosition(double x, double y)
        {
            X = x;
            Y = y;
        }
        
        /// <summary>
        /// Sets the ball's position from a Point.
        /// </summary>
        /// <param name="position">New position</param>
        public void SetPosition(Point position)
        {
            X = position.X;
            Y = position.Y;
        }
        
        /// <summary>
        /// Constrains the ball's position to be within the specified boundaries.
        /// </summary>
        /// <param name="minX">Minimum X coordinate</param>
        /// <param name="minY">Minimum Y coordinate</param>
        /// <param name="maxX">Maximum X coordinate</param>
        /// <param name="maxY">Maximum Y coordinate</param>
        /// <returns>True if the position was constrained, false otherwise</returns>
        public bool ConstrainPosition(double minX, double minY, double maxX, double maxY)
        {
            bool wasConstrained = false;
            
            // Adjust for the ball's radius
            double effectiveMinX = minX + Radius;
            double effectiveMinY = minY + Radius;
            double effectiveMaxX = maxX - Radius;
            double effectiveMaxY = maxY - Radius;
            
            // Constrain X position
            if (X < effectiveMinX)
            {
                X = effectiveMinX;
                wasConstrained = true;
            }
            else if (X > effectiveMaxX)
            {
                X = effectiveMaxX;
                wasConstrained = true;
            }
            
            // Constrain Y position
            if (Y < effectiveMinY)
            {
                Y = effectiveMinY;
                wasConstrained = true;
            }
            else if (Y > effectiveMaxY)
            {
                Y = effectiveMaxY;
                wasConstrained = true;
            }
            
            return wasConstrained;
        }

        /// <summary>
        /// Validates a double value to ensure it's not NaN or Infinity
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="fallback">The fallback value to use if validation fails</param>
        /// <param name="minimum">Optional minimum value constraint</param>
        /// <returns>The validated value or fallback if invalid</returns>
        private static double ValidateDouble(double value, double fallback, double? minimum = null)
        {
            // Check for NaN or Infinity
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return fallback;
            }
            
            // Check minimum constraint if provided
            if (minimum.HasValue && value < minimum.Value)
            {
                return Math.Max(fallback, minimum.Value);
            }
            
            return value;
        }

        #endregion Methods
    }
}