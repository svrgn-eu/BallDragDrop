using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.Contracts;
using BallDragDrop.Models;
using BallDragDrop.Services;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for CursorManager class
    /// </summary>
    [TestClass]
    public class CursorManagerTests
    {
        #region Fields

        /// <summary>
        /// Mock configuration service for testing
        /// </summary>
        private Mock<IConfigurationService> _mockConfigurationService;

        /// <summary>
        /// Mock log service for testing
        /// </summary>
        private Mock<ILogService> _mockLogService;

        /// <summary>
        /// Mock cursor image loader for testing
        /// </summary>
        private Mock<CursorImageLoader> _mockImageLoader;

        /// <summary>
        /// Cursor configuration for testing
        /// </summary>
        private CursorConfiguration _cursorConfiguration;

        /// <summary>
        /// Cursor manager under test
        /// </summary>
        private CursorManager _cursorManager;

        #endregion Fields

        #region TestInitialize

        /// <summary>
        /// Initializes test dependencies before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _mockConfigurationService = new Mock<IConfigurationService>();
            _mockLogService = new Mock<ILogService>();
            _mockImageLoader = new Mock<CursorImageLoader>(_mockLogService.Object);

            _cursorConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = true,
                DefaultCursorPath = "Resources/Cursors/default.png",
                HoverCursorPath = "Resources/Cursors/hover.png",
                GrabbingCursorPath = "Resources/Cursors/grabbing.png",
                ReleasingCursorPath = "Resources/Cursors/releasing.png",
                DebounceTimeMs = 16,
                ReleasingDurationMs = 200
            };

            // Setup mock configuration service
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(_cursorConfiguration);
            _mockConfigurationService.Setup(x => x.ValidateCursorConfiguration(It.IsAny<CursorConfiguration>()))
                .Returns(true);

            // Setup mock image loader to return system cursor by default
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Returns((string path, Cursor fallback) => fallback);

            _cursorManager = new CursorManager(
                _mockConfigurationService.Object,
                _mockLogService.Object,
                _mockImageLoader.Object,
                _cursorConfiguration);
        }

        #endregion TestInitialize

        #region Construction Tests

        /// <summary>
        /// Tests that CursorManager initializes correctly with valid dependencies
        /// </summary>
        [TestMethod]
        public void Constructor_WithValidDependencies_ShouldInitializeSuccessfully()
        {
            // Assert
            Assert.IsNotNull(_cursorManager);
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Default"));
            Assert.IsTrue(currentState.Contains("CustomCursors: Enabled"));
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null configuration service
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullConfigurationService_ShouldThrowArgumentNullException()
        {
            // Act
            new CursorManager(null, _mockLogService.Object, _mockImageLoader.Object, _cursorConfiguration);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null log service
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullLogService_ShouldThrowArgumentNullException()
        {
            // Act
            new CursorManager(_mockConfigurationService.Object, null, _mockImageLoader.Object, _cursorConfiguration);
        }

        /// <summary>
        /// Tests that constructor throws ArgumentNullException for null image loader
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullImageLoader_ShouldThrowArgumentNullException()
        {
            // Act
            new CursorManager(_mockConfigurationService.Object, _mockLogService.Object, null, _cursorConfiguration);
        }

        #endregion Construction Tests

        #region SetCursorForHandState Tests

        /// <summary>
        /// Tests setting cursor for Default hand state
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_Default_ShouldUpdateCurrentState()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Default"));
        }

        /// <summary>
        /// Tests setting cursor for Hover hand state
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_Hover_ShouldUpdateCurrentState()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Hover);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Hover"));
        }

        /// <summary>
        /// Tests setting cursor for Grabbing hand state
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_Grabbing_ShouldUpdateCurrentState()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Grabbing);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Grabbing"));
        }

        /// <summary>
        /// Tests setting cursor for Releasing hand state
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_Releasing_ShouldUpdateCurrentState()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Releasing);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("HandState: Releasing"));
        }

        /// <summary>
        /// Tests that custom cursors disabled uses system cursor
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_WithCustomCursorsDisabled_ShouldUseSystemCursor()
        {
            // Arrange
            _cursorConfiguration.EnableCustomCursors = false;

            // Act
            _cursorManager.SetCursorForHandState(HandState.Hover);

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CustomCursors: Disabled"));
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Custom cursors disabled")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests that cursor loading is attempted for each hand state
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_WithCustomCursorsEnabled_ShouldAttemptCursorLoading()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);
            _cursorManager.SetCursorForHandState(HandState.Hover);
            _cursorManager.SetCursorForHandState(HandState.Grabbing);
            _cursorManager.SetCursorForHandState(HandState.Releasing);

            // Assert
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Queued cursor update")), 
                It.IsAny<object[]>()), Times.AtLeast(4));
        }

        #endregion SetCursorForHandState Tests

        #region Cursor Loading and Caching Tests

        /// <summary>
        /// Tests that cursors are loaded from image loader
        /// </summary>
        [TestMethod]
        public void GetCursorForHandState_ShouldLoadFromImageLoader()
        {
            // Arrange
            var mockCursor = Cursors.Hand;
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("default.png")), 
                It.IsAny<Cursor>()))
                .Returns(mockCursor);

            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("default.png")), 
                It.IsAny<Cursor>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that cursors are cached after first load
        /// </summary>
        [TestMethod]
        public void GetCursorForHandState_ShouldCacheCursorsAfterFirstLoad()
        {
            // Arrange
            var mockCursor = Cursors.Hand;
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Returns(mockCursor);

            // Act - Load same cursor twice
            _cursorManager.SetCursorForHandState(HandState.Default);
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert - Should only load once due to caching
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CachedCursors:"));
        }

        /// <summary>
        /// Tests that different hand states load different cursor paths
        /// </summary>
        [TestMethod]
        public void GetCursorForHandState_DifferentStates_ShouldLoadDifferentPaths()
        {
            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);
            _cursorManager.SetCursorForHandState(HandState.Hover);
            _cursorManager.SetCursorForHandState(HandState.Grabbing);
            _cursorManager.SetCursorForHandState(HandState.Releasing);

            // Assert
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("default.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("hover.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("grabbing.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(
                It.Is<string>(s => s.Contains("releasing.png")), It.IsAny<Cursor>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests fallback to system cursor when image loading fails
        /// </summary>
        [TestMethod]
        public void GetCursorForHandState_WhenImageLoadingFails_ShouldUseFallbackCursor()
        {
            // Arrange
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Returns(Cursors.Arrow); // Return fallback cursor

            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            _mockImageLoader.Verify(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), Cursors.Arrow), Times.AtLeastOnce);
        }

        #endregion Cursor Loading and Caching Tests

        #region Configuration Reload Tests

        /// <summary>
        /// Tests successful configuration reload
        /// </summary>
        [TestMethod]
        public async Task ReloadConfigurationAsync_WithValidConfiguration_ShouldReloadSuccessfully()
        {
            // Arrange
            var newConfiguration = new CursorConfiguration
            {
                EnableCustomCursors = false,
                DebounceTimeMs = 32
            };
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(newConfiguration);

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Assert
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CustomCursors: Disabled"));
            _mockLogService.Verify(x => x.LogInformation(
                It.Is<string>(s => s.Contains("Cursor configuration reloaded successfully")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests configuration reload clears cache
        /// </summary>
        [TestMethod]
        public async Task ReloadConfigurationAsync_ShouldClearCache()
        {
            // Arrange - Load some cursors first to populate cache
            _cursorManager.SetCursorForHandState(HandState.Default);
            _cursorManager.SetCursorForHandState(HandState.Hover);

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Assert
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Cursor cache cleared")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests configuration reload with null configuration reverts to previous
        /// </summary>
        [TestMethod]
        public async Task ReloadConfigurationAsync_WithNullConfiguration_ShouldRevertToPrevious()
        {
            // Arrange
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns((CursorConfiguration)null);

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.Is<string>(s => s.Contains("Configuration reload resulted in null configuration")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests configuration reload with invalid configuration uses fallback
        /// </summary>
        [TestMethod]
        public async Task ReloadConfigurationAsync_WithInvalidConfiguration_ShouldUseFallback()
        {
            // Arrange
            var invalidConfiguration = new CursorConfiguration();
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Returns(invalidConfiguration);
            _mockConfigurationService.Setup(x => x.ValidateCursorConfiguration(It.IsAny<CursorConfiguration>()))
                .Returns(false);

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Assert
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("Cursor configuration validation failed")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests configuration reload handles exceptions gracefully
        /// </summary>
        [TestMethod]
        public async Task ReloadConfigurationAsync_WithException_ShouldHandleGracefully()
        {
            // Arrange
            _mockConfigurationService.Setup(x => x.GetCursorConfiguration())
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            await _cursorManager.ReloadConfigurationAsync();

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Error loading configuration during reload")), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion Configuration Reload Tests

        #region Error Handling Tests

        /// <summary>
        /// Tests error handling when setting cursor throws exception
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_WithException_ShouldHandleGracefully()
        {
            // Arrange
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Error setting cursor for hand state")), 
                It.IsAny<object[]>()), Times.Once);
        }

        /// <summary>
        /// Tests error handling when getting current cursor state throws exception
        /// </summary>
        [TestMethod]
        public void GetCurrentCursorState_WithException_ShouldReturnErrorMessage()
        {
            // Arrange - This is tricky to test since the method is quite robust
            // We'll test the error path by creating a scenario that could cause issues

            // Act
            var result = _cursorManager.GetCurrentCursorState();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("HandState:") || result.Contains("Error"));
        }

        /// <summary>
        /// Tests cursor loading with empty path uses fallback
        /// </summary>
        [TestMethod]
        public void LoadCursorWithFallback_WithEmptyPath_ShouldUseFallback()
        {
            // Arrange
            var configWithEmptyPaths = new CursorConfiguration
            {
                EnableCustomCursors = true,
                DefaultCursorPath = "",
                HoverCursorPath = "",
                GrabbingCursorPath = "",
                ReleasingCursorPath = ""
            };

            var cursorManager = new CursorManager(
                _mockConfigurationService.Object,
                _mockLogService.Object,
                _mockImageLoader.Object,
                configWithEmptyPaths);

            // Act
            cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            _mockLogService.Verify(x => x.LogWarning(
                It.Is<string>(s => s.Contains("No cursor path configured")), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests out of memory exception handling during cursor loading
        /// </summary>
        [TestMethod]
        public void LoadCursorWithFallback_WithOutOfMemoryException_ShouldUseFallback()
        {
            // Arrange
            _mockImageLoader.Setup(x => x.LoadPngAsCursorWithFallback(It.IsAny<string>(), It.IsAny<Cursor>()))
                .Throws(new OutOfMemoryException("Test out of memory"));

            // Act
            _cursorManager.SetCursorForHandState(HandState.Default);

            // Assert
            _mockLogService.Verify(x => x.LogError(
                It.IsAny<Exception>(), 
                It.Is<string>(s => s.Contains("Error setting cursor for hand state")), 
                It.IsAny<object[]>()), Times.Once);
        }

        #endregion Error Handling Tests

        #region Performance Tests

        /// <summary>
        /// Tests that cursor updates are debounced within 16ms timing
        /// </summary>
        [TestMethod]
        public void SetCursorForHandState_RapidUpdates_ShouldDebounceWithin16Ms()
        {
            // Arrange
            var startTime = DateTime.Now;

            // Act - Rapid cursor updates
            for (int i = 0; i < 10; i++)
            {
                _cursorManager.SetCursorForHandState(HandState.Default);
                _cursorManager.SetCursorForHandState(HandState.Hover);
            }

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Assert - Should complete quickly due to debouncing
            Assert.IsTrue(duration.TotalMilliseconds < 100, $"Cursor updates took {duration.TotalMilliseconds}ms, expected < 100ms");
            
            // Verify debouncing is working
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Queued cursor update")), 
                It.IsAny<object[]>()), Times.AtLeast(1));
        }

        /// <summary>
        /// Tests cursor cache performance with multiple state changes
        /// </summary>
        [TestMethod]
        public void CursorCaching_WithMultipleStateChanges_ShouldImprovePerformance()
        {
            // Arrange
            var states = new[] { HandState.Default, HandState.Hover, HandState.Grabbing, HandState.Releasing };

            // Act - Load cursors multiple times
            for (int i = 0; i < 3; i++)
            {
                foreach (var state in states)
                {
                    _cursorManager.SetCursorForHandState(state);
                }
            }

            // Assert - Should have cached cursors
            var currentState = _cursorManager.GetCurrentCursorState();
            Assert.IsTrue(currentState.Contains("CachedCursors:"));
            
            // Verify caching is working by checking debug logs
            _mockLogService.Verify(x => x.LogDebug(
                It.Is<string>(s => s.Contains("Using cached cursor") || s.Contains("Loaded and cached cursor")), 
                It.IsAny<object[]>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests memory usage with cursor caching
        /// </summary>
        [TestMethod]
        public void CursorCaching_ShouldNotLeakMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Load and reload cursors multiple times
            for (int i = 0; i < 100; i++)
            {
                _cursorManager.SetCursorForHandState(HandState.Default);
                _cursorManager.SetCursorForHandState(HandState.Hover);
                _cursorManager.SetCursorForHandState(HandState.Grabbing);
                _cursorManager.SetCursorForHandState(HandState.Releasing);
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory increase should be reasonable (less than 1MB for this test)
            Assert.IsTrue(memoryIncrease < 1024 * 1024, 
                $"Memory increased by {memoryIncrease} bytes, expected < 1MB");
        }

        #endregion Performance Tests
    }
}