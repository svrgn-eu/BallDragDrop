using System;
using System.Windows;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Extension methods for testing
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Sets the ball position in the MainWindow
        /// </summary>
        /// <param name="window">The MainWindow instance</param>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public static void SetBallPosition(this MainWindow window, double x, double y)
        {
            // Initialize DataContext if it's null
            if (window.DataContext == null)
            {
                window.DataContext = new BallViewModel(x, y, 50f);
                return;
            }
            
            if (window.DataContext is BallViewModel viewModel)
            {
                viewModel.X = x;
                viewModel.Y = y;
            }
        }
        
        /// <summary>
        /// Gets the ball position from the MainWindow
        /// </summary>
        /// <param name="window">The MainWindow instance</param>
        /// <returns>The ball position</returns>
        public static Point GetBallPosition(this MainWindow window)
        {
            if (window.DataContext is BallViewModel viewModel)
            {
                return new Point(viewModel.X, viewModel.Y);
            }
            
            return new Point(0, 0);
        }
    }
}
