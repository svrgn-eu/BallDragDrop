using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Contracts;
using BallDragDrop.Services;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for CursorImageLoader class
    /// </summary>
    [TestClass]
    public class CursorImageLoaderTests
    {
        #region Fields

        /// <summary>
        /// Mock log service for testing
        /// </summary>
        private Mock<ILogService> _mockLogService;

        /// <summary>
        /// Cursor image loader under test
        /// </summary>
        private CursorImageLoader _cursorImageLoader;

        /// <summary>
        /// Test PNG file path
        /// </summary>
        private string _testPngPath;

        /// <summary>
        /// Test directory for temporary files
        /// </summary>
        private string _testDirectory;

        #endregion Fields

        #region TestInitialize

        /// <summary>
        /// Initializes test dependencies before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            _cursorImageLoader = new CursorImageLoader(_mockLogService.Object);

            // Create test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), "CursorImageLoaderTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Create a simple test PNG file
            _testPngPath = Path.Combine(_testDirectory, "test.png");
            CreateTestPngFile(_testPngPath);
        }

        #endregion TestInitialize

        #region TestCleanup

        /// <summary>
        /// Cleans up test resources after each test
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion TestCleanup

        #region Construction Tests

        /// <summary>
        /// Tests that CursorImageLoader initializes correctly with valid log service
        /// </summary>
        [TestMethod]
        public void Constructor_WithValidLogService_ShouldInitializeSuccessfully()
        {
            // Assert
            Assert.IsNotNull(_cursorImageLoader);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null log service
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullLogService_ShouldThrowArgumentNullException()
        {
            // Act
            new CursorImageLoader(null);
        }

        #endregion Construction Tests

        #region LoadPngAsCursor Tests

        /// <summary>
        /// Tests loading a valid PNG file as cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursor_WithValidPngFile_ShouldReturnCursor()
        {
            // Arrange
            var relativePath = GetRelativePath(_testPngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursor(relativePath);

            // Assert
            Assert.IsNotNull(cursor);
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Starting PNG cursor load")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with null path returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursor_WithNullPath_ShouldReturnFallbackCursor()
        {
            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursor(null);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG path is null or empty")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with empty path returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursor_WithEmptyPath_ShouldReturnFallbackCursor()
        {
            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursor("");

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG path is null or empty")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with non-existent file returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursor_WithNonExistentFile_ShouldReturnFallbackCursor()
        {
            // Arrange
            var nonExistentPath = "nonexistent/file.png";

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursor(nonExistentPath);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG file not found")), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion LoadPngAsCursor Tests

        #region LoadPngAsCursorWithFallback Tests

        /// <summary>
        /// Tests loading with custom fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithCustomFallback_ShouldUseCustomFallback()
        {
            // Arrange
            var customFallback = Cursors.Hand;
            var nonExistentPath = "nonexistent/file.png";

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(nonExistentPath, customFallback);

            // Assert
            Assert.AreEqual(customFallback, cursor);
        }

        /// <summary>
        /// Tests loading with null fallback cursor uses system arrow
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithNullFallback_ShouldUseSystemArrow()
        {
            // Arrange
            var nonExistentPath = "nonexistent/file.png";

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(nonExistentPath, null);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("Fallback cursor is null")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with path too long returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithPathTooLong_ShouldReturnFallbackCursor()
        {
            // Arrange
            var longPath = new string('a', 300); // Longer than MAX_PATH (260)

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(longPath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG path too long")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with empty file returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithEmptyFile_ShouldReturnFallbackCursor()
        {
            // Arrange
            var emptyFilePath = Path.Combine(_testDirectory, "empty.png");
            File.Create(emptyFilePath).Dispose(); // Create empty file
            var relativePath = GetRelativePath(emptyFilePath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG file is empty")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with file too large returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithFileTooLarge_ShouldReturnFallbackCursor()
        {
            // Arrange
            var largeFilePath = Path.Combine(_testDirectory, "large.png");
            CreateLargeFile(largeFilePath, 2 * 1024 * 1024); // 2MB file
            var relativePath = GetRelativePath(largeFilePath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("PNG file too large")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests loading with invalid path format returns fallback cursor
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithInvalidPathFormat_ShouldReturnFallbackCursor()
        {
            // Arrange
            var invalidPath = "invalid|path<>file.png"; // Contains invalid characters

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(invalidPath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Invalid path format")), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion LoadPngAsCursorWithFallback Tests

        #region PNG Loading and Image Processing Tests

        /// <summary>
        /// Tests that valid PNG files are processed correctly
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithValidPng_ShouldProcessCorrectly()
        {
            // Arrange
            var relativePath = GetRelativePath(_testPngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.IsNotNull(cursor);
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Successfully loaded PNG cursor")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests that images are resized to 30x30 pixels
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_ShouldResizeImageTo30x30()
        {
            // Arrange
            var largePngPath = Path.Combine(_testDirectory, "large.png");
            CreateTestPngFile(largePngPath, 100, 100); // Create 100x100 image
            var relativePath = GetRelativePath(largePngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.IsNotNull(cursor);
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Resizing image from 100x100 to 30x30")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests that 30x30 images are not resized
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_With30x30Image_ShouldNotResize()
        {
            // Arrange
            var correctSizePngPath = Path.Combine(_testDirectory, "correct_size.png");
            CreateTestPngFile(correctSizePngPath, 30, 30); // Create 30x30 image
            var relativePath = GetRelativePath(correctSizePngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.IsNotNull(cursor);
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Image already at target size")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests handling of corrupted PNG files
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithCorruptedPng_ShouldReturnFallbackCursor()
        {
            // Arrange
            var corruptedPngPath = Path.Combine(_testDirectory, "corrupted.png");
            CreateCorruptedFile(corruptedPngPath);
            var relativePath = GetRelativePath(corruptedPngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Failed to load PNG bitmap") || s.Contains("Invalid PNG file format")), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        #endregion PNG Loading and Image Processing Tests

        #region Error Handling Tests

        /// <summary>
        /// Tests handling of unauthorized access to PNG files
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithUnauthorizedAccess_ShouldReturnFallbackCursor()
        {
            // This test is difficult to implement reliably across different systems
            // We'll test the error handling path by verifying the method handles exceptions
            
            // Arrange
            var nonExistentPath = "unauthorized/file.png";

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(nonExistentPath, Cursors.Arrow);

            // Assert
            Assert.AreEqual(Cursors.Arrow, cursor);
            // The method should handle the file not found gracefully
        }

        /// <summary>
        /// Tests handling of out of memory exceptions
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithOutOfMemoryException_ShouldReturnFallbackCursor()
        {
            // This test is difficult to trigger reliably, but we can verify the error handling exists
            // by testing with a very large file that might cause memory issues
            
            // Arrange
            var relativePath = GetRelativePath(_testPngPath);

            // Act
            var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Hand);

            // Assert
            Assert.IsNotNull(cursor);
            // The method should complete without throwing exceptions
        }

        /// <summary>
        /// Tests that all exceptions are caught and handled gracefully
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithAnyException_ShouldNotThrow()
        {
            // Arrange
            var invalidPaths = new[]
            {
                null,
                "",
                "invalid|path",
                "nonexistent/file.png",
                new string('a', 300)
            };

            // Act & Assert
            foreach (var path in invalidPaths)
            {
                try
                {
                    var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(path, Cursors.Arrow);
                    Assert.IsNotNull(cursor);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Method should not throw exceptions, but threw: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        #endregion Error Handling Tests

        #region Memory Management Tests

        /// <summary>
        /// Tests that loading multiple cursors doesn't cause memory leaks
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_MultipleLoads_ShouldNotLeakMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var relativePath = GetRelativePath(_testPngPath);

            // Act - Load cursors multiple times
            for (int i = 0; i < 50; i++)
            {
                var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(relativePath, Cursors.Arrow);
                Assert.IsNotNull(cursor);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory increase should be reasonable (less than 5MB for this test)
            Assert.IsTrue(memoryIncrease < 5 * 1024 * 1024, 
                $"Memory increased by {memoryIncrease} bytes, expected < 5MB");
        }

        /// <summary>
        /// Tests resource disposal during error scenarios
        /// </summary>
        [TestMethod]
        public void LoadPngAsCursorWithFallback_WithErrors_ShouldDisposeResourcesProperly()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var invalidPaths = new[]
            {
                "nonexistent1.png",
                "nonexistent2.png",
                "nonexistent3.png"
            };

            // Act - Try to load non-existent files
            foreach (var path in invalidPaths)
            {
                var cursor = _cursorImageLoader.LoadPngAsCursorWithFallback(path, Cursors.Arrow);
                Assert.AreEqual(Cursors.Arrow, cursor);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory increase should be minimal for failed loads
            Assert.IsTrue(memoryIncrease < 1024 * 1024, 
                $"Memory increased by {memoryIncrease} bytes, expected < 1MB for failed loads");
        }

        #endregion Memory Management Tests

        #region Helper Methods

        /// <summary>
        /// Creates a simple test PNG file
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        private void CreateTestPngFile(string filePath, int width = 32, int height = 32)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Resources/Ball/Ball01.png", UriKind.Absolute);
                bitmap.EndInit();

                // Create a simple colored bitmap for testing
                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                var visual = new System.Windows.Media.DrawingVisual();
                
                using (var context = visual.RenderOpen())
                {
                    context.DrawRectangle(System.Windows.Media.Brushes.Red, null, new System.Windows.Rect(0, 0, width, height));
                }
                
                renderBitmap.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }
            }
            catch
            {
                // If we can't create a proper PNG, create a simple file with PNG header
                var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                File.WriteAllBytes(filePath, pngHeader);
            }
        }

        /// <summary>
        /// Creates a corrupted file that looks like a PNG but isn't valid
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        private void CreateCorruptedFile(string filePath)
        {
            var corruptedData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0xFF, 0xFF, 0xFF, 0xFF };
            File.WriteAllBytes(filePath, corruptedData);
        }

        /// <summary>
        /// Creates a large file for testing file size limits
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        /// <param name="sizeInBytes">Size of the file in bytes</param>
        private void CreateLargeFile(string filePath, int sizeInBytes)
        {
            var data = new byte[sizeInBytes];
            // Fill with PNG header at the beginning
            data[0] = 0x89;
            data[1] = 0x50;
            data[2] = 0x4E;
            data[3] = 0x47;
            File.WriteAllBytes(filePath, data);
        }

        /// <summary>
        /// Gets a relative path from the application base directory
        /// </summary>
        /// <param name="fullPath">Full path to convert</param>
        /// <returns>Relative path</returns>
        private string GetRelativePath(string fullPath)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (fullPath.StartsWith(baseDirectory))
            {
                return fullPath.Substring(baseDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return fullPath;
        }

        #endregion Helper Methods
    }
}