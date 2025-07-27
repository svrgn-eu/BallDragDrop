using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BallDragDrop.ViewModels;
using BallDragDrop.Services;
using BallDragDrop.Contracts;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class StatusBarViewModelTests
    {
        private Mock<ILogService> _mockLogService;
        private PerformanceMonitor _performanceMonitor;
        private StatusBarViewModel _viewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogService = new Mock<ILogService>();
            // Create a real PerformanceMonitor instance for testing
            _performanceMonitor = new PerformanceMonitor(60);
            _viewModel = new StatusBarViewModel(_mockLogService.Object, _performanceMonitor);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _viewModel?.Dispose();
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithValidDependencies_InitializesCorrectly()
        {
            // Arrange & Act
            var viewModel = new StatusBarViewModel(_mockLogService.Object, _performanceMonitor);

            // Assert
            Assert.AreEqual(0.0, viewModel.CurrentFps);
            Assert.AreEqual(0.0, viewModel.AverageFps);
            Assert.AreEqual("No Asset", viewModel.AssetName);
            Assert.AreEqual("Status", viewModel.StatusText);
            Assert.AreEqual("FPS: --", viewModel.CurrentFpsDisplay);
            Assert.AreEqual("Avg: --", viewModel.AverageFpsDisplay);

            viewModel.Dispose();
        }

        [TestMethod]
        public void Constructor_WithNullLogService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new StatusBarViewModel(null, _performanceMonitor));
        }

        [TestMethod]
        public void Constructor_WithNullPerformanceMonitor_InitializesWithWarning()
        {
            // Act
            var viewModel = new StatusBarViewModel(_mockLogService.Object, null);

            // Assert
            Assert.IsNotNull(viewModel);
            _mockLogService.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);

            viewModel.Dispose();
        }

        #endregion Constructor Tests

        #region Property Change Tests

        [TestMethod]
        public void CurrentFps_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act
            _viewModel.CurrentFps = 60.0;

            // Assert
            Assert.AreEqual(60.0, _viewModel.CurrentFps);
            CollectionAssert.Contains(changedProperties, "CurrentFps");
            CollectionAssert.Contains(changedProperties, "CurrentFpsDisplay");
        }

        [TestMethod]
        public void AverageFps_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act
            _viewModel.AverageFps = 58.5;

            // Assert
            Assert.AreEqual(58.5, _viewModel.AverageFps);
            CollectionAssert.Contains(changedProperties, "AverageFps");
            CollectionAssert.Contains(changedProperties, "AverageFpsDisplay");
        }

        [TestMethod]
        public void AssetName_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act
            _viewModel.AssetName = "test_asset.png";

            // Assert
            Assert.AreEqual("test_asset.png", _viewModel.AssetName);
            CollectionAssert.Contains(changedProperties, "AssetName");
        }

        [TestMethod]
        public void StatusText_PropertyChanged_FiresCorrectly()
        {
            // Arrange
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act
            _viewModel.StatusText = "Ready";

            // Assert
            Assert.AreEqual("Ready", _viewModel.StatusText);
            CollectionAssert.Contains(changedProperties, "StatusText");
        }

        #endregion Property Change Tests

        #region FPS Value Formatting Tests

        [TestMethod]
        public void CurrentFpsDisplay_WithValidFps_FormatsCorrectly()
        {
            // Act
            _viewModel.CurrentFps = 60.123;

            // Assert
            Assert.AreEqual("FPS: 60.1", _viewModel.CurrentFpsDisplay);
        }

        [TestMethod]
        public void CurrentFpsDisplay_WithZeroFps_ShowsFallback()
        {
            // Act
            _viewModel.CurrentFps = 0.0;

            // Assert
            Assert.AreEqual("FPS: --", _viewModel.CurrentFpsDisplay);
        }

        [TestMethod]
        public void CurrentFpsDisplay_WithNaNFps_ShowsFallback()
        {
            // Act
            _viewModel.CurrentFps = double.NaN;

            // Assert
            Assert.AreEqual("FPS: --", _viewModel.CurrentFpsDisplay);
        }

        [TestMethod]
        public void CurrentFpsDisplay_WithInfinityFps_ShowsFallback()
        {
            // Act
            _viewModel.CurrentFps = double.PositiveInfinity;

            // Assert
            Assert.AreEqual("FPS: --", _viewModel.CurrentFpsDisplay);
        }

        [TestMethod]
        public void AverageFpsDisplay_WithValidFps_FormatsCorrectly()
        {
            // Act
            _viewModel.AverageFps = 58.789;

            // Assert
            Assert.AreEqual("Avg: 58.8", _viewModel.AverageFpsDisplay);
        }

        [TestMethod]
        public void AverageFpsDisplay_WithZeroFps_ShowsFallback()
        {
            // Act
            _viewModel.AverageFps = 0.0;

            // Assert
            Assert.AreEqual("Avg: --", _viewModel.AverageFpsDisplay);
        }

        #endregion FPS Value Formatting Tests

        #region FPS Value Sanitization Tests

        [TestMethod]
        public void CurrentFps_WithInvalidValue_SanitizesToZero()
        {
            // Act & Assert
            _viewModel.CurrentFps = -10.0;
            Assert.AreEqual(0.0, _viewModel.CurrentFps);

            _viewModel.CurrentFps = 1500.0;
            Assert.AreEqual(0.0, _viewModel.CurrentFps);

            _viewModel.CurrentFps = double.NaN;
            Assert.AreEqual(0.0, _viewModel.CurrentFps);

            _viewModel.CurrentFps = double.PositiveInfinity;
            Assert.AreEqual(0.0, _viewModel.CurrentFps);
        }

        [TestMethod]
        public void AverageFps_WithInvalidValue_SanitizesToZero()
        {
            // Act & Assert
            _viewModel.AverageFps = -5.0;
            Assert.AreEqual(0.0, _viewModel.AverageFps);

            _viewModel.AverageFps = 2000.0;
            Assert.AreEqual(0.0, _viewModel.AverageFps);

            _viewModel.AverageFps = double.NaN;
            Assert.AreEqual(0.0, _viewModel.AverageFps);

            _viewModel.AverageFps = double.NegativeInfinity;
            Assert.AreEqual(0.0, _viewModel.AverageFps);
        }

        #endregion FPS Value Sanitization Tests

        #region Asset Name Handling Tests

        [TestMethod]
        public void ConnectToBallViewModel_WithValidViewModel_ConnectsSuccessfully()
        {
            // Arrange
            var mockBallViewModel = new Mock<BallViewModel>(100.0, 100.0, 25.0);
            mockBallViewModel.SetupGet(x => x.AssetName).Returns("test_asset.png");

            // Act
            _viewModel.ConnectToBallViewModel(mockBallViewModel.Object);

            // Assert
            Assert.AreEqual("test_asset.png", _viewModel.AssetName);
        }

        [TestMethod]
        public void ConnectToBallViewModel_WithNullViewModel_LogsWarning()
        {
            // Act
            _viewModel.ConnectToBallViewModel(null);

            // Assert
            _mockLogService.Verify(x => x.LogWarning(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void ProcessAssetName_WithNullOrEmpty_ReturnsDefault()
        {
            // Arrange
            var mockBallViewModel = new Mock<BallViewModel>(100.0, 100.0, 25.0);
            
            // Test null
            mockBallViewModel.SetupGet(x => x.AssetName).Returns((string)null);
            _viewModel.ConnectToBallViewModel(mockBallViewModel.Object);
            Assert.AreEqual("No Asset", _viewModel.AssetName);

            // Test empty
            mockBallViewModel.SetupGet(x => x.AssetName).Returns("");
            _viewModel.ConnectToBallViewModel(mockBallViewModel.Object);
            Assert.AreEqual("No Asset", _viewModel.AssetName);
        }

        #endregion Asset Name Handling Tests

        #region Disposal Tests

        [TestMethod]
        public void Dispose_CleansUpResourcesCorrectly()
        {
            // Arrange
            var mockBallViewModel = new Mock<BallViewModel>(100.0, 100.0, 25.0);
            _viewModel.ConnectToBallViewModel(mockBallViewModel.Object);

            // Act
            _viewModel.Dispose();

            // Assert - Should not throw and should log disposal
            _mockLogService.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("disposed"))), Times.Once);
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _viewModel.Dispose();
            _viewModel.Dispose();
            _viewModel.Dispose();
        }

        #endregion Disposal Tests

        #region Property Change Threshold Tests

        [TestMethod]
        public void CurrentFps_SmallChange_DoesNotFirePropertyChanged()
        {
            // Arrange
            _viewModel.CurrentFps = 60.0;
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act - Change by less than threshold (0.01)
            _viewModel.CurrentFps = 60.005;

            // Assert
            Assert.AreEqual(0, changedProperties.Count);
        }

        [TestMethod]
        public void CurrentFps_LargeChange_FiresPropertyChanged()
        {
            // Arrange
            _viewModel.CurrentFps = 60.0;
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act - Change by more than threshold (0.01)
            _viewModel.CurrentFps = 60.02;

            // Assert
            Assert.IsTrue(changedProperties.Count > 0);
            CollectionAssert.Contains(changedProperties, "CurrentFps");
        }

        [TestMethod]
        public void AverageFps_SmallChange_DoesNotFirePropertyChanged()
        {
            // Arrange
            _viewModel.AverageFps = 58.0;
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName);

            // Act - Change by less than threshold (0.01)
            _viewModel.AverageFps = 58.009;

            // Assert
            Assert.AreEqual(0, changedProperties.Count);
        }

        #endregion Property Change Threshold Tests
    }
}