using System;
using System.Windows;
using System.Windows.Input;
using System.Reflection;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// A mock implementation of MouseDevice for testing purposes
    /// This class doesn't inherit from MouseDevice but provides the necessary functionality for testing
    /// </summary>
    public class MockMouseDevice
    {
        private readonly Point _position;
        
        /// <summary>
        /// Creates a new instance of MockMouseDevice with the specified position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public MockMouseDevice(double x, double y)
        {
            _position = new Point(x, y);
        }
        
        /// <summary>
        /// Gets the position of the mouse
        /// </summary>
        /// <param name="relativeTo">The element to get the position relative to</param>
        /// <returns>The position of the mouse</returns>
        public Point GetPosition(IInputElement relativeTo)
        {
            return _position;
        }
        
        /// <summary>
        /// Creates a MouseEventArgs instance that uses this MockMouseDevice
        /// </summary>
        /// <returns>A MouseEventArgs instance</returns>
        public MouseEventArgs CreateMouseEventArgs()
        {
            // Create a TestMouseEventArgs that doesn't rely on MouseDevice
            return new TestMouseEventArgs(_position.X, _position.Y);
        }
    }
    
    /// <summary>
    /// A custom implementation of MouseEventArgs for testing
    /// </summary>
    public class TestMouseEventArgs : MouseEventArgs
    {
        private readonly Point _position;
        
        /// <summary>
        /// Creates a new instance of TestMouseEventArgs with the specified position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public TestMouseEventArgs(double x, double y) : base(InputManager.Current?.PrimaryMouseDevice, 0)
        {
            _position = new Point(x, y);
        }
        
        /// <summary>
        /// Gets the position of the mouse
        /// </summary>
        /// <param name="relativeTo">The element to get the position relative to</param>
        /// <returns>The position of the mouse</returns>
        public new Point GetPosition(IInputElement relativeTo)
        {
            return _position;
        }
    }
}