using System;
using System.Windows;
using System.Windows.Input;
using System.Reflection;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// A custom implementation of MouseEventArgs for testing that doesn't rely on MouseDevice
    /// </summary>
    public class CustomMouseEventArgs
    {
        private readonly Point _position;
        
        public CustomMouseEventArgs(double x, double y)
        {
            _position = new Point(x, y);
        }
        
        public Point GetPosition(IInputElement relativeTo)
        {
            return _position;
        }
        
        // Add a Source property to mimic MouseEventArgs
        public object Source { get; set; }
    }
}