using System;
using System.Windows;
using System.Windows.Input;
using BallDragDrop.ViewModels;
using System.Reflection;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// Helper class for testing BallViewModel without relying on MouseEventArgs
    /// </summary>
    public static class BallViewModelTestHelper
    {
        /// <summary>
        /// Simulates a mouse down event on the ball
        /// </summary>
        /// <param name="viewModel">The BallViewModel to test</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public static void SimulateMouseDown(BallViewModel viewModel, double x, double y)
        {
            // Use reflection to directly manipulate the BallViewModel's state
            // This avoids the need to use MouseEventArgs
            
            // Get the ball model
            var ballModelField = typeof(BallViewModel).GetField("_ballModel", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var ballModel = ballModelField?.GetValue(viewModel);
            
            // Check if the click is inside the ball
            var containsPointMethod = ballModel?.GetType().GetMethod("ContainsPoint");
            bool isInsideBall = (bool)(containsPointMethod?.Invoke(ballModel, new object[] { x, y }) ?? false);
            
            // Set the _lastMousePosition field
            var lastMousePositionField = typeof(BallViewModel).GetField("_lastMousePosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            lastMousePositionField?.SetValue(viewModel, new Point(x, y));
            
            // Only set IsDragging to true if the click is inside the ball
            if (isInsideBall)
            {
                // Set the IsDragging property to true
                var isDraggingProperty = typeof(BallViewModel).GetProperty("IsDragging");
                isDraggingProperty?.SetValue(viewModel, true);
                
                // Set the _dragStartPosition field
                var dragStartPositionField = typeof(BallViewModel).GetField("_dragStartPosition", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                dragStartPositionField?.SetValue(viewModel, new Point(viewModel.X, viewModel.Y));
                
                // Set the _lastUpdateTime field
                var lastUpdateTimeField = typeof(BallViewModel).GetField("_lastUpdateTime", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                lastUpdateTimeField?.SetValue(viewModel, DateTime.Now);
                
                // Stop any current movement by calling the Stop method on the ball model
                var stopMethod = ballModel?.GetType().GetMethod("Stop");
                stopMethod?.Invoke(ballModel, null);
            }
        }
        
        /// <summary>
        /// Simulates a mouse move event
        /// </summary>
        /// <param name="viewModel">The BallViewModel to test</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="windowWidth">Window width for constraint checking (default: 800)</param>
        /// <param name="windowHeight">Window height for constraint checking (default: 600)</param>
        public static void SimulateMouseMove(BallViewModel viewModel, double x, double y, double windowWidth = 800, double windowHeight = 600)
        {
            // Get the current mouse position
            var lastMousePositionField = typeof(BallViewModel).GetField("_lastMousePosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var lastMousePosition = (Point)(lastMousePositionField?.GetValue(viewModel) ?? new Point());
            
            // Calculate the delta
            double deltaX = x - lastMousePosition.X;
            double deltaY = y - lastMousePosition.Y;
            
            // Update the ball position if dragging
            if (viewModel.IsDragging)
            {
                viewModel.X += deltaX;
                viewModel.Y += deltaY;
                
                // Constrain the position
                viewModel.ConstrainPosition(0, 0, windowWidth, windowHeight);
                
                // Store the mouse position in history
                var storeMousePositionMethod = typeof(BallViewModel).GetMethod("StoreMousePosition", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                storeMousePositionMethod?.Invoke(viewModel, new object[] { new Point(x, y), DateTime.Now });
            }
            
            // Update the last mouse position
            lastMousePositionField?.SetValue(viewModel, new Point(x, y));
            
            // Update the cursor
            var updateCursorMethod = typeof(BallViewModel).GetMethod("UpdateCursor", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            updateCursorMethod?.Invoke(viewModel, null);
        }
        
        /// <summary>
        /// Simulates a mouse up event
        /// </summary>
        /// <param name="viewModel">The BallViewModel to test</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public static void SimulateMouseUp(BallViewModel viewModel, double x, double y)
        {
            if (viewModel.IsDragging)
            {
                // Set IsDragging to false
                var isDraggingProperty = typeof(BallViewModel).GetProperty("IsDragging");
                isDraggingProperty?.SetValue(viewModel, false);
                
                // Store the final position in the history
                var storeMousePositionMethod = typeof(BallViewModel).GetMethod("StoreMousePosition", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var currentTime = DateTime.Now;
                storeMousePositionMethod?.Invoke(viewModel, new object[] { new Point(x, y), currentTime });
                
                // Get the physics engine
                var physicsEngineField = typeof(BallViewModel).GetField("_physicsEngine", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var physicsEngine = physicsEngineField?.GetValue(viewModel);
                
                // Reset the mouse history count
                var mouseHistoryCountField = typeof(BallViewModel).GetField("_mouseHistoryCount", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                mouseHistoryCountField?.SetValue(viewModel, 0);
                
                // Update the last mouse position
                var lastMousePositionField = typeof(BallViewModel).GetField("_lastMousePosition", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                lastMousePositionField?.SetValue(viewModel, new Point(x, y));
                
                // Update the cursor
                var updateCursorMethod = typeof(BallViewModel).GetMethod("UpdateCursor", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                updateCursorMethod?.Invoke(viewModel, null);
            }
        }
    }
}