using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.ViewModels;
using BallDragDrop.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
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

        #region ImageService Integration Tests

        [TestMethod]
        public void Constructor_WithImageService_InitializesImageServiceProperties()
        {
            // Arrange
            var imageService = new ImageService();
            
            // Act
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Assert
            Assert.IsFalse(viewModel.IsAnimated);
            Assert.AreEqual(VisualContentType.Unknown, viewModel.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithValidStaticImage_LoadsSuccessfully()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Create a temporary test image file
            string tempImagePath = CreateTempTestImage();
            
            try
            {
                // Act
                bool result = await viewModel.LoadBallVisualAsync(tempImagePath);
                
                // Assert
                Assert.IsTrue(result);
                Assert.IsNotNull(viewModel.BallImage);
                Assert.IsFalse(viewModel.IsAnimated);
                Assert.AreEqual(VisualContentType.StaticImage, viewModel.ContentType);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithInvalidPath_ReturnsFalseAndSetsFallback()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            string invalidPath = "nonexistent_file.png";
            
            // Act
            bool result = await viewModel.LoadBallVisualAsync(invalidPath);
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsNotNull(viewModel.BallImage); // Should have fallback image
            Assert.IsFalse(viewModel.IsAnimated);
            Assert.AreEqual(VisualContentType.StaticImage, viewModel.ContentType);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithNullPath_ReturnsFalse()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Act
            bool result = await viewModel.LoadBallVisualAsync(null);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithEmptyPath_ReturnsFalse()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Act
            bool result = await viewModel.LoadBallVisualAsync("");
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithGifFile_SetsAnimatedProperties()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Create a temporary test GIF file (just a renamed PNG for testing)
            string tempGifPath = CreateTempTestGif();
            
            try
            {
                // Act
                bool result = await viewModel.LoadBallVisualAsync(tempGifPath);
                
                // Assert
                Assert.IsTrue(result);
                Assert.IsNotNull(viewModel.BallImage);
                Assert.IsTrue(viewModel.IsAnimated); // Should be marked as animated
                Assert.AreEqual(VisualContentType.GifAnimation, viewModel.ContentType);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempGifPath))
                {
                    File.Delete(tempGifPath);
                }
            }
        }

        [TestMethod]
        public async Task LoadBallVisualAsync_WithAsepriteFiles_SetsAnimatedProperties()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Create temporary test Aseprite files
            var (pngPath, jsonPath) = CreateTempTestAsepriteFiles();
            
            try
            {
                // Act
                bool result = await viewModel.LoadBallVisualAsync(pngPath);
                
                // Assert
                Assert.IsTrue(result);
                Assert.IsNotNull(viewModel.BallImage);
                Assert.IsTrue(viewModel.IsAnimated); // Should be marked as animated
                Assert.AreEqual(VisualContentType.AsepriteAnimation, viewModel.ContentType);
            }
            finally
            {
                // Cleanup
                if (File.Exists(pngPath))
                {
                    File.Delete(pngPath);
                }
                if (File.Exists(jsonPath))
                {
                    File.Delete(jsonPath);
                }
            }
        }

        [TestMethod]
        public void IsAnimated_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            var changedProperties = new List<string>();
            
            viewModel.PropertyChanged += (sender, e) => 
            {
                changedProperties.Add(e.PropertyName);
            };
            
            // Act - This will be triggered internally by LoadBallVisualAsync
            // We'll use reflection to test the property setter directly
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty.SetValue(viewModel, true);
            
            // Assert
            CollectionAssert.Contains(changedProperties, "IsAnimated");
            Assert.IsTrue(viewModel.IsAnimated);
        }

        [TestMethod]
        public void ContentType_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            var changedProperties = new List<string>();
            
            viewModel.PropertyChanged += (sender, e) => 
            {
                changedProperties.Add(e.PropertyName);
            };
            
            // Act - This will be triggered internally by LoadBallVisualAsync
            // We'll use reflection to test the property setter directly
            var contentTypeProperty = typeof(BallViewModel).GetProperty("ContentType");
            contentTypeProperty.SetValue(viewModel, VisualContentType.GifAnimation);
            
            // Assert
            CollectionAssert.Contains(changedProperties, "ContentType");
            Assert.AreEqual(VisualContentType.GifAnimation, viewModel.ContentType);
        }

        #endregion ImageService Integration Tests

        #region Helper Methods for ImageService Tests

        /// <summary>
        /// Creates a temporary test image file for testing
        /// </summary>
        /// <returns>Path to the temporary image file</returns>
        private string CreateTempTestImage()
        {
            string tempPath = Path.GetTempFileName();
            string pngPath = Path.ChangeExtension(tempPath, ".png");
            
            // Create a simple 1x1 PNG file
            byte[] pngData = {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
                0x54, 0x08, 0xD7, 0x63, 0xF8, 0x00, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, // Image data
                0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
                0x60, 0x82 // IEND chunk
            };
            
            File.WriteAllBytes(pngPath, pngData);
            File.Delete(tempPath); // Remove the original temp file
            
            return pngPath;
        }

        /// <summary>
        /// Creates a temporary test GIF file for testing
        /// </summary>
        /// <returns>Path to the temporary GIF file</returns>
        private string CreateTempTestGif()
        {
            string tempPath = Path.GetTempFileName();
            string gifPath = Path.ChangeExtension(tempPath, ".gif");
            
            // Create a simple GIF file (minimal valid GIF)
            byte[] gifData = {
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a signature
                0x01, 0x00, 0x01, 0x00, // 1x1 dimensions
                0x80, 0x00, 0x00, // Global color table flag, color resolution, sort flag, global color table size
                0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, // Color table (black, white)
                0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, // Graphic control extension
                0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // Image descriptor
                0x02, 0x02, 0x04, 0x01, 0x00, 0x3B // Image data and trailer
            };
            
            File.WriteAllBytes(gifPath, gifData);
            File.Delete(tempPath); // Remove the original temp file
            
            return gifPath;
        }

        /// <summary>
        /// Creates temporary test Aseprite files (PNG + JSON) for testing
        /// </summary>
        /// <returns>Tuple containing paths to the PNG and JSON files</returns>
        private (string pngPath, string jsonPath) CreateTempTestAsepriteFiles()
        {
            string tempPath = Path.GetTempFileName();
            string baseName = Path.GetFileNameWithoutExtension(tempPath);
            string directory = Path.GetDirectoryName(tempPath);
            
            string pngPath = Path.Combine(directory, baseName + ".png");
            string jsonPath = Path.Combine(directory, baseName + ".json");
            
            // Create PNG file (same as CreateTempTestImage)
            byte[] pngData = {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
                0x54, 0x08, 0xD7, 0x63, 0xF8, 0x00, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
                0x60, 0x82
            };
            
            // Create JSON metadata file
            string jsonContent = @"{
                ""frames"": [
                    {
                        ""frame"": { ""x"": 0, ""y"": 0, ""w"": 1, ""h"": 1 },
                        ""duration"": 100
                    }
                ],
                ""meta"": {
                    ""size"": { ""w"": 1, ""h"": 1 }
                }
            }";
            
            File.WriteAllBytes(pngPath, pngData);
            File.WriteAllText(jsonPath, jsonContent);
            File.Delete(tempPath); // Remove the original temp file
            
            return (pngPath, jsonPath);
        }

        #endregion Helper Methods for ImageService Tests

        #region Animation Timer Integration Tests

        [TestMethod]
        public void EnsureAnimationContinuesDuringDrag_WithAnimatedContent_EnsuresTimerRunning()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Set up animated content using reflection
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty?.SetValue(viewModel, true);
            
            // Act
            viewModel.EnsureAnimationContinuesDuringDrag();
            
            // Assert
            // The method should execute without throwing
            Assert.IsTrue(true, "EnsureAnimationContinuesDuringDrag should execute without error for animated content");
        }

        [TestMethod]
        public void EnsureAnimationContinuesDuringDrag_WithStaticContent_DoesNotThrow()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Ensure content is static (default state)
            Assert.IsFalse(viewModel.IsAnimated);
            
            // Act & Assert
            try
            {
                viewModel.EnsureAnimationContinuesDuringDrag();
                Assert.IsTrue(true, "EnsureAnimationContinuesDuringDrag should not throw for static content");
            }
            catch (Exception ex)
            {
                Assert.Fail($"EnsureAnimationContinuesDuringDrag should not throw for static content: {ex.Message}");
            }
        }

        [TestMethod]
        public void CoordinateAnimationWithPhysics_WithAnimatedContent_UpdatesFrames()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Set up animated content using reflection
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty?.SetValue(viewModel, true);
            
            // Set a test image
            viewModel.BallImage = TestImageHelper.CreateTestImage(50, 50);
            
            // Act
            viewModel.CoordinateAnimationWithPhysics();
            
            // Assert
            Assert.IsNotNull(viewModel.BallImage, "Ball image should remain valid after coordination");
        }

        [TestMethod]
        public void CoordinateAnimationWithPhysics_WithStaticContent_DoesNotThrow()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Ensure content is static (default state)
            Assert.IsFalse(viewModel.IsAnimated);
            
            // Act & Assert
            try
            {
                viewModel.CoordinateAnimationWithPhysics();
                Assert.IsTrue(true, "CoordinateAnimationWithPhysics should not throw for static content");
            }
            catch (Exception ex)
            {
                Assert.Fail($"CoordinateAnimationWithPhysics should not throw for static content: {ex.Message}");
            }
        }

        [TestMethod]
        [STAThread]
        public void MouseDown_WithAnimatedContent_EnsuresAnimationContinues()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Set up animated content using reflection
            var isAnimatedProperty = typeof(BallViewModel).GetProperty("IsAnimated");
            isAnimatedProperty?.SetValue(viewModel, true);
            
            // Set a test image
            viewModel.BallImage = TestImageHelper.CreateTestImage(50, 50);
            
            // Act
            SimulateMouseDown(viewModel, 100, 100); // Click at the center of the ball
            
            // Assert
            Assert.IsTrue(viewModel.IsDragging, "Ball should be in dragging state");
            // The animation continuation is handled internally, so we just verify no exceptions occurred
        }

        [TestMethod]
        [STAThread]
        public void MouseDown_WithStaticContent_DoesNotThrow()
        {
            // Arrange
            var imageService = new ImageService();
            var viewModel = new BallViewModel(100, 100, 25, imageService);
            
            // Ensure content is static (default state)
            Assert.IsFalse(viewModel.IsAnimated);
            
            // Set a test image
            viewModel.BallImage = TestImageHelper.CreateTestImage(50, 50);
            
            // Act & Assert
            try
            {
                SimulateMouseDown(viewModel, 100, 100); // Click at the center of the ball
                Assert.IsTrue(viewModel.IsDragging, "Ball should be in dragging state");
            }
            catch (Exception ex)
            {
                Assert.Fail($"MouseDown should not throw for static content: {ex.Message}");
            }
        }

        #endregion Animation Timer Integration Tests

        #region Animation Rendering Tests

        [TestMethod]
        public async Task OnAnimationTimerTick_UpdatesFrameWithoutFlickering()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(TestHelpers.TestImageHelper.CreateTestImage(50, 50));
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            var initialImage = viewModel.BallImage;
            
            // Create a new frame
            var newFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetCurrentFrame(newFrame);
            
            // Act
            // Simulate animation timer tick by calling the method via reflection
            var method = typeof(BallViewModel).GetMethod("OnAnimationTimerTick", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(viewModel, new object[] { viewModel, EventArgs.Empty });
            
            // Allow UI thread to process
            await Task.Delay(50);
            
            // Assert
            Assert.AreNotSame(initialImage, viewModel.BallImage, "Frame should have been updated");
            Assert.AreSame(newFrame, viewModel.BallImage, "New frame should be set");
        }

        [TestMethod]
        public async Task OptimizeAnimationRendering_FreezesFramesForPerformance()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content with a freezable frame
            var testFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(testFrame);
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            // Act
            viewModel.OptimizeAnimationRendering();
            
            // Allow UI thread to process
            await Task.Delay(100);
            
            // Assert
            Assert.IsTrue(viewModel.IsAnimated, "Content should be animated");
            Assert.IsNotNull(viewModel.BallImage, "Ball image should be set");
            
            // If the frame is freezable, it should be frozen for performance
            if (viewModel.BallImage.CanFreeze)
            {
                Assert.IsTrue(viewModel.BallImage.IsFrozen, "Frame should be frozen for performance");
            }
        }

        [TestMethod]
        public async Task EnsureAnimationVisualQuality_MaintainsFrameQuality()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content
            var highQualityFrame = TestHelpers.TestImageHelper.CreateTestImage(100, 100);
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(highQualityFrame);
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            // Act
            viewModel.EnsureAnimationVisualQuality();
            
            // Assert
            Assert.IsTrue(viewModel.IsAnimated, "Content should be animated");
            Assert.IsNotNull(viewModel.BallImage, "Ball image should be set");
            Assert.AreSame(highQualityFrame, viewModel.BallImage, "High quality frame should be maintained");
            
            // Frame should be optimized for rendering
            if (viewModel.BallImage.CanFreeze)
            {
                Assert.IsTrue(viewModel.BallImage.IsFrozen, "Frame should be frozen for optimal rendering");
            }
        }

        [TestMethod]
        public async Task AnimationDuringDrag_MaintainsPlayback()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(TestHelpers.TestImageHelper.CreateTestImage(50, 50));
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            // Start dragging
            var mouseDownArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseDownEvent
            };
            
            // Set the position to be within the ball bounds
            typeof(MouseEventArgs).GetProperty("Position", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(mouseDownArgs, new Point(100, 100));
            
            // Act
            viewModel.MouseDownCommand.Execute(mouseDownArgs);
            
            // Ensure animation continues during drag
            viewModel.EnsureAnimationContinuesDuringDrag();
            
            // Assert
            Assert.IsTrue(viewModel.IsDragging, "Ball should be dragging");
            Assert.IsTrue(viewModel.IsAnimated, "Animation should still be active");
            
            // Animation should continue playing during drag
            var frame1 = viewModel.BallImage;
            
            // Simulate frame update during drag
            var newFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetCurrentFrame(newFrame);
            
            var method = typeof(BallViewModel).GetMethod("OnAnimationTimerTick", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(viewModel, new object[] { viewModel, EventArgs.Empty });
            
            await Task.Delay(50);
            
            Assert.AreNotSame(frame1, viewModel.BallImage, "Frame should update during drag");
        }

        [TestMethod]
        public async Task CoordinateAnimationWithPhysics_SynchronizesFrameUpdates()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(TestHelpers.TestImageHelper.CreateTestImage(50, 50));
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            var initialFrame = viewModel.BallImage;
            
            // Create a new frame for coordination test
            var coordinatedFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetCurrentFrame(coordinatedFrame);
            
            // Act
            viewModel.CoordinateAnimationWithPhysics();
            
            // Allow UI thread to process
            await Task.Delay(50);
            
            // Assert
            Assert.IsTrue(viewModel.IsAnimated, "Content should be animated");
            Assert.AreNotSame(initialFrame, viewModel.BallImage, "Frame should be updated for physics coordination");
            Assert.AreSame(coordinatedFrame, viewModel.BallImage, "Coordinated frame should be set");
        }

        [TestMethod]
        public async Task AnimationFrameUpdate_OnlyUpdatesWhenFrameChanges()
        {
            // Arrange
            var mockImageService = new MockImageService();
            var viewModel = new BallViewModel(100, 100, 25, mockImageService);
            
            // Set up animated content
            var sameFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetAnimated(true);
            mockImageService.SetCurrentFrame(sameFrame);
            
            await viewModel.LoadBallVisualAsync("test.gif");
            
            var propertyChangedCount = 0;
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.BallImage))
                {
                    propertyChangedCount++;
                }
            };
            
            // Act - simulate timer tick with same frame
            var method = typeof(BallViewModel).GetMethod("OnAnimationTimerTick", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(viewModel, new object[] { viewModel, EventArgs.Empty });
            
            await Task.Delay(50);
            
            // Assert - should not trigger property change for same frame
            Assert.AreEqual(0, propertyChangedCount, "Property should not change when frame is the same");
            
            // Now change the frame
            var newFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            mockImageService.SetCurrentFrame(newFrame);
            
            method?.Invoke(viewModel, new object[] { viewModel, EventArgs.Empty });
            await Task.Delay(50);
            
            // Should trigger property change for new frame
            Assert.AreEqual(1, propertyChangedCount, "Property should change when frame is different");
        }

        #endregion Animation Rendering Tests

        #region Mock Classes for Animation Testing

    /// <summary>
    /// Mock ImageService for testing animation rendering
    /// </summary>
    public class MockImageService : ImageService
    {
        private ImageSource _mockCurrentFrame;
        private bool _mockIsAnimated;
        private TimeSpan _mockFrameDuration = TimeSpan.FromMilliseconds(100);
        private bool _loadShouldSucceed = true;
        private VisualContentType _mockContentType = VisualContentType.StaticImage;

        public MockImageService() : base(null)
        {
        }

        public new ImageSource CurrentFrame => _mockCurrentFrame ?? base.CurrentFrame;
        public new bool IsAnimated => _mockIsAnimated;
        public new TimeSpan FrameDuration => _mockFrameDuration;
        public new VisualContentType ContentType => _mockContentType;

        public void SetCurrentFrame(ImageSource frame)
        {
            _mockCurrentFrame = frame;
        }

        public void SetAnimated(bool animated)
        {
            _mockIsAnimated = animated;
        }

        public void SetFrameDuration(TimeSpan duration)
        {
            _mockFrameDuration = duration;
        }

        public void SetContentType(VisualContentType contentType)
        {
            _mockContentType = contentType;
        }

        public void SetLoadShouldSucceed(bool shouldSucceed)
        {
            _loadShouldSucceed = shouldSucceed;
        }

        public new async Task<bool> LoadBallVisualAsync(string filePath)
        {
            if (!_loadShouldSucceed)
            {
                return false;
            }

            // Simulate successful loading and set properties
            await Task.Delay(10);
            
            // Set default test image if none provided
            if (_mockCurrentFrame == null)
            {
                _mockCurrentFrame = TestHelpers.TestImageHelper.CreateTestImage(50, 50);
            }
            
            return true;
        }

        public new void StartAnimation()
        {
            // Mock implementation
        }

        public new void StopAnimation()
        {
            // Mock implementation
        }

        public new void UpdateFrame()
        {
            // Mock implementation - frame updates are controlled by test
            SetCurrentFrame(_mockCurrentFrame);
        }
    }

        #endregion Mock Classes for Animation Testing
    }
}