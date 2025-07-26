using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using BallDragDrop.Tests.TestHelpers;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class MouseHistoryTests
    {
        [TestMethod]
        public void MouseHistory_IsInitializedInConstructor()
        {
            // Arrange & Act
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Assert - Use reflection to check private fields
            var mousePositionHistoryField = typeof(BallViewModel).GetField("_mousePositionHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mouseTimestampHistoryField = typeof(BallViewModel).GetField("_mouseTimestampHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mouseHistoryCountField = typeof(BallViewModel).GetField("_mouseHistoryCount", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(mousePositionHistoryField);
            Assert.IsNotNull(mouseTimestampHistoryField);
            Assert.IsNotNull(mouseHistoryCountField);
            
            var mousePositionHistory = mousePositionHistoryField.GetValue(viewModel) as Point[];
            var mouseTimestampHistory = mouseTimestampHistoryField.GetValue(viewModel) as DateTime[];
            var mouseHistoryCount = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            
            Assert.IsNotNull(mousePositionHistory);
            Assert.IsNotNull(mouseTimestampHistory);
            Assert.AreEqual(0, mouseHistoryCount);
            
            // Check the size of the arrays
            Assert.AreEqual(10, mousePositionHistory.Length); // Assuming MouseHistorySize = 10
            Assert.AreEqual(10, mouseTimestampHistory.Length);
        }
        
        [TestMethod]
        public void MouseMove_WhileDragging_StoresPositionInHistory()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Use reflection to access private fields and methods
            var mouseHistoryCountField = typeof(BallViewModel).GetField("_mouseHistoryCount", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var storeMousePositionMethod = typeof(BallViewModel).GetMethod("StoreMousePosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.IsNotNull(mouseHistoryCountField);
            Assert.IsNotNull(storeMousePositionMethod);
            
            // Initial count should be 0
            int initialCount = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(0, initialCount);
            
            // Act - Call the StoreMousePosition method directly
            var position = new Point(120, 130);
            var timestamp = DateTime.Now;
            storeMousePositionMethod.Invoke(viewModel, new object[] { position, timestamp });
            
            // Assert - Count should be incremented
            int newCount = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(1, newCount);
            
            // Get the stored position and timestamp
            var mousePositionHistoryField = typeof(BallViewModel).GetField("_mousePositionHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mouseTimestampHistoryField = typeof(BallViewModel).GetField("_mouseTimestampHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var mousePositionHistory = mousePositionHistoryField.GetValue(viewModel) as Point[];
            var mouseTimestampHistory = mouseTimestampHistoryField.GetValue(viewModel) as DateTime[];
            
            // Check that the position and timestamp were stored correctly
            Assert.AreEqual(position, mousePositionHistory[0]);
            Assert.AreEqual(timestamp, mouseTimestampHistory[0]);
        }
        
        [TestMethod]
        public void StoreMousePosition_WhenHistoryIsFull_ShiftsElementsAndAddsNew()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Use reflection to access private fields and methods
            var mouseHistoryCountField = typeof(BallViewModel).GetField("_mouseHistoryCount", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var storeMousePositionMethod = typeof(BallViewModel).GetMethod("StoreMousePosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mousePositionHistoryField = typeof(BallViewModel).GetField("_mousePositionHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var mouseTimestampHistoryField = typeof(BallViewModel).GetField("_mouseTimestampHistory", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Fill the history array with 10 positions (assuming MouseHistorySize = 10)
            for (int i = 0; i < 10; i++)
            {
                var position = new Point(100 + i * 10, 100);
                var timestamp = DateTime.Now.AddMilliseconds(i * 100);
                storeMousePositionMethod.Invoke(viewModel, new object[] { position, timestamp });
            }
            
            // Verify history is full
            int count = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(10, count);
            
            var mousePositionHistory = mousePositionHistoryField.GetValue(viewModel) as Point[];
            var mouseTimestampHistory = mouseTimestampHistoryField.GetValue(viewModel) as DateTime[];
            
            // Remember the first and last positions before adding a new one
            var firstPositionBefore = mousePositionHistory[0];
            var lastPositionBefore = mousePositionHistory[9];
            
            // Act - Add one more position
            var newPosition = new Point(200, 200);
            var newTimestamp = DateTime.Now.AddSeconds(1);
            storeMousePositionMethod.Invoke(viewModel, new object[] { newPosition, newTimestamp });
            
            // Assert - Count should still be 10
            count = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(10, count);
            
            // Get the updated arrays
            mousePositionHistory = mousePositionHistoryField.GetValue(viewModel) as Point[];
            mouseTimestampHistory = mouseTimestampHistoryField.GetValue(viewModel) as DateTime[];
            
            // First position should now be the second position from before
            Assert.AreNotEqual(firstPositionBefore, mousePositionHistory[0]);
            
            // Last position should be the new position
            Assert.AreEqual(newPosition, mousePositionHistory[9]);
            Assert.AreEqual(newTimestamp, mouseTimestampHistory[9]);
        }
        
        [TestMethod]
        [STAThread]
        public void MouseUp_AfterDragging_ResetsHistoryCount()
        {
            // Arrange
            var viewModel = new BallViewModel(100, 100, 25);
            
            // Use reflection to access private fields
            var mouseHistoryCountField = typeof(BallViewModel).GetField("_mouseHistoryCount", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var storeMousePositionMethod = typeof(BallViewModel).GetMethod("StoreMousePosition", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Add some positions to the history
            for (int i = 0; i < 3; i++)
            {
                var position = new Point(100 + i * 10, 100);
                var timestamp = DateTime.Now.AddMilliseconds(i * 100);
                storeMousePositionMethod.Invoke(viewModel, new object[] { position, timestamp });
            }
            
            // Verify history has items
            int countBefore = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(3, countBefore);
            
            // Start dragging
            BallViewModelTestHelper.SimulateMouseDown(viewModel, 100, 100);
            
            // Act - Release the mouse
            BallViewModelTestHelper.SimulateMouseUp(viewModel, 120, 120);
            
            // Assert - History count should be reset to 0
            int countAfter = (int)(mouseHistoryCountField.GetValue(viewModel) ?? 0);
            Assert.AreEqual(0, countAfter);
        }
    }
    
    // Helper class to create mouse event args for testing
    public static class MouseEventArgsHelper
    {
        public static MouseEventArgs CreateMouseEventArgs(double x, double y)
        {
            // Create a mock mouse device and get a MouseEventArgs from it
            var mockMouseDevice = new MockMouseDevice(x, y);
            return mockMouseDevice.CreateMouseEventArgs();
        }
    }
}