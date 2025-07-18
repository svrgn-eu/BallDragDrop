using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using BallDragDrop.Tests.TestHelpers;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class BallViewModelTests
    {
        [TestMethod]
        public void Constructor_InitializesProperties()
        {
            // Arrange
            double initialX = 100;
            double initialY = 200;
            double radius = 30;
            
            // Act
            var viewModel = new BallViewModel(initialX, initialY, radius);
            
            // Assert
            Assert.AreEqual(initialX, viewModel.X);
            Assert.AreEqual(initialY, viewModel.Y);
            Assert.AreEqual(radius, viewModel.Radius);
            Assert.IsFalse(viewModel.IsDragging);
            Assert.IsNotNull(viewModel.MouseDownCommand);
            Assert.IsNotNull(viewModel.MouseMoveCommand);
            Assert.IsNotNull(viewModel.MouseUpCommand);
        }
        
        [TestMethod]
        public void PositionProperties_CalculateCorrectly()
        {
            // Arrange
            double initialX = 100;
            double initialY = 200;
            double radius = 30;
            var viewModel = new BallViewModel(initialX, initialY, radius);
            
            // Act & Assert
            Assert.AreEqual(initialX - radius, viewModel.Left);
            Assert.AreEqual(initialY - radius, viewModel.Top);
            Assert.AreEqual(radius * 2, viewModel.Width);
            Assert.AreEqual(radius * 2, viewModel.Height);
        }
        
        [TestMethod]
        public void PropertyChanged_FiresForDependentProperties()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            var changedProperties = new List<string>();
            
            viewModel.PropertyChanged += (sender, e) => 
            {
                changedProperties.Add(e.PropertyName);
            };
            
            // Act
            viewModel.X = 150;
            
            // Assert
            CollectionAssert.Contains(changedProperties, "X");
            CollectionAssert.Contains(changedProperties, "Left");
            
            // Reset and test Y
            changedProperties.Clear();
            viewModel.Y = 150;
            
            CollectionAssert.Contains(changedProperties, "Y");
            CollectionAssert.Contains(changedProperties, "Top");
            
            // Reset and test Radius
            changedProperties.Clear();
            viewModel.Radius = 30;
            
            CollectionAssert.Contains(changedProperties, "Radius");
            CollectionAssert.Contains(changedProperties, "Left");
            CollectionAssert.Contains(changedProperties, "Top");
            CollectionAssert.Contains(changedProperties, "Width");
            CollectionAssert.Contains(changedProperties, "Height");
        }
        
        [TestMethod]
        public void ConstrainPosition_UpdatesPositionAndNotifiesPropertyChanged()
        {
            // Arrange
            double radius = 25;
            var viewModel = new BallViewModel(10, 10, radius);
            var changedProperties = new List<string>();
            
            viewModel.PropertyChanged += (sender, e) => 
            {
                changedProperties.Add(e.PropertyName);
            };
            
            // Act - Position is outside boundaries and should be constrained
            viewModel.ConstrainPosition(50, 50, 200, 200);
            
            // Assert
            Assert.AreEqual(50 + radius, viewModel.X); // Constrained to minX + radius
            Assert.AreEqual(50 + radius, viewModel.Y); // Constrained to minY + radius
            CollectionAssert.Contains(changedProperties, "X");
            CollectionAssert.Contains(changedProperties, "Y");
            CollectionAssert.Contains(changedProperties, "Left");
            CollectionAssert.Contains(changedProperties, "Top");
            
            // Reset and test with position inside boundaries
            changedProperties.Clear();
            viewModel.X = 100;
            viewModel.Y = 100;
            changedProperties.Clear(); // Clear again after setting X and Y
            
            // Act - Position is inside boundaries and should not be constrained
            viewModel.ConstrainPosition(50, 50, 200, 200);
            
            // Assert - No properties should have changed
            Assert.AreEqual(100, viewModel.X);
            Assert.AreEqual(100, viewModel.Y);
            Assert.AreEqual(0, changedProperties.Count);
        }
        
        [TestMethod]
        public void ConstrainPosition_ConstrainsToMinimumBoundaries()
        {
            // Arrange
            double radius = 25;
            var viewModel = new BallViewModel(10, 10, radius);
            
            // Act - Position is outside minimum boundaries
            viewModel.ConstrainPosition(20, 30, 200, 200);
            
            // Assert
            Assert.AreEqual(20 + radius, viewModel.X); // Constrained to minX + radius
            Assert.AreEqual(30 + radius, viewModel.Y); // Constrained to minY + radius
        }
        
        [TestMethod]
        public void ConstrainPosition_ConstrainsToMaximumBoundaries()
        {
            // Arrange
            double radius = 25;
            var viewModel = new BallViewModel(250, 250, radius);
            
            // Act - Position is outside maximum boundaries
            viewModel.ConstrainPosition(0, 0, 200, 200);
            
            // Assert
            Assert.AreEqual(200 - radius, viewModel.X); // Constrained to maxX - radius
            Assert.AreEqual(200 - radius, viewModel.Y); // Constrained to maxY - radius
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_DraggingOutsideBoundaries_ConstrainsBallPosition()
        {
            // Arrange
            double radius = 25;
            double windowWidth = 300;
            double windowHeight = 300;
            var viewModel = new BallViewModel(150, 150, radius);
            
            // Start dragging the ball
            SimulateMouseDown(viewModel, 150, 150);
            
            // Act - Try to drag outside the right boundary
            SimulateMouseMove(viewModel, 350, 150, windowWidth, windowHeight); // Beyond right edge
            
            // Assert - Ball should be constrained to the right edge
            Assert.AreEqual(windowWidth - radius, viewModel.X);
            Assert.AreEqual(150, viewModel.Y);
            
            // Act - Try to drag outside the bottom boundary
            SimulateMouseMove(viewModel, 75, 350, windowWidth, windowHeight); // Beyond bottom edge
            
            // Assert - Ball should be constrained to the bottom edge
            Assert.AreEqual(75, viewModel.X);
            Assert.AreEqual(windowHeight - radius, viewModel.Y);
        }
        
        [TestMethod]
        public void WindowResize_ConstrainsBallPosition()
        {
            // Arrange
            double radius = 25;
            double initialWindowWidth = 400;
            double initialWindowHeight = 400;
            
            // Place ball near the right edge
            var viewModel = new BallViewModel(380, 200, radius);
            
            // Act - Simulate window resize to smaller dimensions
            double newWindowWidth = 300;
            double newWindowHeight = 300;
            viewModel.ConstrainPosition(0, 0, newWindowWidth, newWindowHeight);
            
            // Assert - Ball should be constrained to the new right edge
            Assert.AreEqual(newWindowWidth - radius, viewModel.X);
            Assert.AreEqual(200, viewModel.Y);
            
            // Arrange - Place ball near the bottom edge
            viewModel.X = 150;
            viewModel.Y = 380;
            
            // Act - Simulate another window resize
            newWindowWidth = 250;
            newWindowHeight = 250;
            viewModel.ConstrainPosition(0, 0, newWindowWidth, newWindowHeight);
            
            // Assert - Ball should be constrained to the new bottom edge
            Assert.AreEqual(150, viewModel.X);
            Assert.AreEqual(newWindowHeight - radius, viewModel.Y);
        }
        
        [TestMethod]
        public void IsDragging_ChangesCurrentCursor()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act & Assert - Initial state
            Assert.IsFalse(viewModel.IsDragging);
            Assert.AreEqual(Cursors.Arrow.ToString(), viewModel.CurrentCursor.ToString());
            
            // Act - Start dragging
            viewModel.IsDragging = true;
            
            // Assert - Cursor should change to SizeAll
            Assert.IsTrue(viewModel.IsDragging);
            Assert.AreEqual(Cursors.SizeAll.ToString(), viewModel.CurrentCursor.ToString());
            
            // Act - Stop dragging
            // The issue is that when we stop dragging, the cursor depends on whether the mouse is over the ball
            // Since _lastMousePosition is (0,0) by default and the ball is at (100,100), the cursor will be Arrow
            viewModel.IsDragging = false;
            
            // Assert - Cursor should change back to Arrow since the mouse is not over the ball
            Assert.IsFalse(viewModel.IsDragging);
            Assert.AreEqual(Cursors.Arrow.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void MouseDown_InsideBall_StartsDragging()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act
            SimulateMouseDown(viewModel, 100, 100); // Click at the center of the ball
            
            // Assert
            Assert.IsTrue(viewModel.IsDragging);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseDown_OutsideBall_DoesNotStartDragging()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act
            SimulateMouseDown(viewModel, 200, 200); // Click outside the ball
            
            // Assert
            Assert.IsFalse(viewModel.IsDragging);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_WhileDragging_UpdatesPosition()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            SimulateMouseDown(viewModel, 100, 100); // Click at the center of the ball
            
            double initialX = viewModel.X;
            double initialY = viewModel.Y;
            
            // Act - Move the mouse while dragging
            SimulateMouseMove(viewModel, 120, 130); // Move 20 pixels right, 30 pixels down
            
            // Assert
            Assert.AreEqual(initialX + 20, viewModel.X);
            Assert.AreEqual(initialY + 30, viewModel.Y);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_NotDragging_DoesNotUpdatePosition()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            double initialX = viewModel.X;
            double initialY = viewModel.Y;
            
            // Act - Move the mouse without dragging
            SimulateMouseMove(viewModel, 120, 130);
            
            // Assert
            Assert.AreEqual(initialX, viewModel.X);
            Assert.AreEqual(initialY, viewModel.Y);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_OverBall_ChangesCursorToHand()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act - Move the mouse over the ball
            SimulateMouseMove(viewModel, 100, 100);
            
            // Assert
            Assert.AreEqual(Cursors.Hand.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_NotOverBall_KeepsDefaultCursor()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act - Move the mouse away from the ball
            SimulateMouseMove(viewModel, 200, 200);
            
            // Assert
            Assert.AreEqual(Cursors.Arrow.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void MouseMove_WhileDragging_ShowsSizeAllCursor()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Start dragging the ball
            SimulateMouseDown(viewModel, 100, 100);
            
            // Act - Move the mouse while dragging
            SimulateMouseMove(viewModel, 120, 120);
            
            // Assert
            Assert.IsTrue(viewModel.IsDragging);
            Assert.AreEqual(Cursors.SizeAll.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void MouseUp_AfterDragging_ResetsCursorBasedOnPosition()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Start dragging the ball
            SimulateMouseDown(viewModel, 100, 100);
            
            // Move to a position still over the ball
            SimulateMouseMove(viewModel, 110, 110);
            
            // Act - Release the mouse while still over the ball
            SimulateMouseUp(viewModel, 110, 110);
            
            // Assert
            Assert.IsFalse(viewModel.IsDragging);
            Assert.AreEqual(Cursors.Hand.ToString(), viewModel.CurrentCursor.ToString());
            
            // Arrange again - move away from the ball
            SimulateMouseMove(viewModel, 200, 200);
            
            // Assert - Cursor should be Arrow when not over the ball
            Assert.AreEqual(Cursors.Arrow.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void CursorChanges_WhenHoveringInAndOutOfBall()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Act 1 - Move mouse over the ball
            SimulateMouseMove(viewModel, 100, 100);
            
            // Assert 1 - Cursor should be Hand when over the ball
            Assert.AreEqual(Cursors.Hand.ToString(), viewModel.CurrentCursor.ToString());
            
            // Act 2 - Move mouse away from the ball
            SimulateMouseMove(viewModel, 200, 200);
            
            // Assert 2 - Cursor should be Arrow when not over the ball
            Assert.AreEqual(Cursors.Arrow.ToString(), viewModel.CurrentCursor.ToString());
            
            // Act 3 - Move mouse back over the ball
            SimulateMouseMove(viewModel, 100, 100);
            
            // Assert 3 - Cursor should be Hand again when over the ball
            Assert.AreEqual(Cursors.Hand.ToString(), viewModel.CurrentCursor.ToString());
        }
        
        [TestMethod]
        [STAThread]
        public void MouseUp_WhileDragging_StopsDragging()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            SimulateMouseDown(viewModel, 100, 100);
            
            // Act
            SimulateMouseUp(viewModel, 120, 130);
            
            // Assert
            Assert.IsFalse(viewModel.IsDragging);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseUp_NotDragging_DoesNothing()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            bool initialDraggingState = viewModel.IsDragging;
            
            // Act
            SimulateMouseUp(viewModel, 120, 130);
            
            // Assert
            Assert.AreEqual(initialDraggingState, viewModel.IsDragging);
        }
        
        /// <summary>
        /// Helper method to create a mock MouseEventArgs for testing
        /// </summary>
        private static void SimulateMouseDown(BallViewModel viewModel, double x, double y)
        {
            BallViewModelTestHelper.SimulateMouseDown(viewModel, x, y);
        }
        
        /// <summary>
        /// Helper method to simulate mouse move for testing
        /// </summary>
        private static void SimulateMouseMove(BallViewModel viewModel, double x, double y, double windowWidth = 800, double windowHeight = 600)
        {
            BallViewModelTestHelper.SimulateMouseMove(viewModel, x, y, windowWidth, windowHeight);
        }
        
        /// <summary>
        /// Helper method to simulate mouse up for testing
        /// </summary>
        private static void SimulateMouseUp(BallViewModel viewModel, double x, double y)
        {
            BallViewModelTestHelper.SimulateMouseUp(viewModel, x, y);
        }
    }
}