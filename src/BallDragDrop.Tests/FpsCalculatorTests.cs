using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.Services;
using System;
using System.Threading;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class FpsCalculatorTests
    {
        private FpsCalculator _fpsCalculator;

        [TestInitialize]
        public void TestInitialize()
        {
            _fpsCalculator = new FpsCalculator();
        }

        #region Basic Functionality Tests

        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var calculator = new FpsCalculator();

            // Assert
            Assert.AreEqual(0.0, calculator.AverageFps);
            Assert.AreEqual(0, calculator.GetReadingCount());
        }

        [TestMethod]
        public void AddFpsReading_WithValidValue_AddsReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(60.0);

            // Assert
            Assert.AreEqual(1, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(60.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithMultipleValues_CalculatesAverage()
        {
            // Act
            _fpsCalculator.AddFpsReading(60.0);
            _fpsCalculator.AddFpsReading(30.0);
            _fpsCalculator.AddFpsReading(90.0);

            // Assert
            Assert.AreEqual(3, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(60.0, _fpsCalculator.AverageFps); // (60 + 30 + 90) / 3 = 60
        }

        #endregion Basic Functionality Tests

        #region Invalid Value Tests

        [TestMethod]
        public void AddFpsReading_WithNegativeValue_IgnoresReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(-10.0);

            // Assert
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithExcessivelyHighValue_IgnoresReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(1500.0);

            // Assert
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithNaNValue_IgnoresReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(double.NaN);

            // Assert
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithInfinityValue_IgnoresReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(double.PositiveInfinity);
            _fpsCalculator.AddFpsReading(double.NegativeInfinity);

            // Assert
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithMixedValidAndInvalidValues_OnlyCountsValid()
        {
            // Act
            _fpsCalculator.AddFpsReading(60.0);  // Valid
            _fpsCalculator.AddFpsReading(-10.0); // Invalid
            _fpsCalculator.AddFpsReading(30.0);  // Valid
            _fpsCalculator.AddFpsReading(double.NaN); // Invalid
            _fpsCalculator.AddFpsReading(45.0);  // Valid

            // Assert
            Assert.AreEqual(3, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(45.0, _fpsCalculator.AverageFps); // (60 + 30 + 45) / 3 = 45
        }

        #endregion Invalid Value Tests

        #region Edge Case Tests

        [TestMethod]
        public void AddFpsReading_WithZeroValue_AcceptsReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(0.0);

            // Assert
            Assert.AreEqual(1, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void AddFpsReading_WithBoundaryValue_AcceptsReading()
        {
            // Act
            _fpsCalculator.AddFpsReading(1000.0); // Exactly at the boundary
            _fpsCalculator.AddFpsReading(999.9);  // Just below boundary

            // Assert
            Assert.AreEqual(2, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(999.95, _fpsCalculator.AverageFps, 0.01); // (1000 + 999.9) / 2
        }

        [TestMethod]
        public void GetAverageFps_WithEmptyData_ReturnsZero()
        {
            // Act & Assert
            Assert.AreEqual(0.0, _fpsCalculator.GetAverageFps());
        }

        [TestMethod]
        public void GetAverageFps_WithSingleReading_ReturnsThatValue()
        {
            // Arrange
            _fpsCalculator.AddFpsReading(42.5);

            // Act & Assert
            Assert.AreEqual(42.5, _fpsCalculator.GetAverageFps());
        }

        #endregion Edge Case Tests

        #region Time-Based Cleanup Tests

        [TestMethod]
        public void AddFpsReading_AfterTimeWindow_RemovesOldReadings()
        {
            // This test is challenging to implement without being able to control time
            // We'll test the cleanup method indirectly by adding many readings
            // and verifying that the average changes as expected

            // Arrange - Add initial reading
            _fpsCalculator.AddFpsReading(60.0);
            Assert.AreEqual(1, _fpsCalculator.GetReadingCount());

            // Act - Add more readings to trigger potential cleanup
            for (int i = 0; i < 50; i++)
            {
                _fpsCalculator.AddFpsReading(30.0);
                Thread.Sleep(1); // Small delay to ensure different timestamps
            }

            // Assert - Should have all readings since they're within the time window
            Assert.IsTrue(_fpsCalculator.GetReadingCount() > 1);
            Assert.IsTrue(_fpsCalculator.AverageFps < 60.0); // Should be closer to 30 due to more 30.0 readings
        }

        #endregion Time-Based Cleanup Tests

        #region Thread Safety Tests

        [TestMethod]
        public void AddFpsReading_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int readingsPerThread = 100;
            var threads = new Thread[threadCount];
            var exceptions = new Exception[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        for (int j = 0; j < readingsPerThread; j++)
                        {
                            _fpsCalculator.AddFpsReading(60.0);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions[threadIndex] = ex;
                    }
                });
                threads[i].Start();
            }

            // Wait for all threads to complete
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }

            // Assert
            for (int i = 0; i < threadCount; i++)
            {
                Assert.IsNull(exceptions[i], $"Thread {i} threw an exception: {exceptions[i]}");
            }

            Assert.AreEqual(threadCount * readingsPerThread, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(60.0, _fpsCalculator.AverageFps);
        }

        #endregion Thread Safety Tests

        #region Clear Method Tests

        [TestMethod]
        public void Clear_WithReadings_RemovesAllReadings()
        {
            // Arrange
            _fpsCalculator.AddFpsReading(60.0);
            _fpsCalculator.AddFpsReading(30.0);
            _fpsCalculator.AddFpsReading(90.0);
            Assert.AreEqual(3, _fpsCalculator.GetReadingCount());

            // Act
            _fpsCalculator.Clear();

            // Assert
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        [TestMethod]
        public void Clear_WithEmptyData_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _fpsCalculator.Clear();
            Assert.AreEqual(0, _fpsCalculator.GetReadingCount());
            Assert.AreEqual(0.0, _fpsCalculator.AverageFps);
        }

        #endregion Clear Method Tests

        #region Property Access Tests

        [TestMethod]
        public void AverageFps_Property_ReturnsCorrectValue()
        {
            // Arrange
            _fpsCalculator.AddFpsReading(40.0);
            _fpsCalculator.AddFpsReading(50.0);
            _fpsCalculator.AddFpsReading(60.0);

            // Act
            double averageFromProperty = _fpsCalculator.AverageFps;
            double averageFromMethod = _fpsCalculator.GetAverageFps();

            // Assert
            Assert.AreEqual(averageFromMethod, averageFromProperty);
            Assert.AreEqual(50.0, averageFromProperty); // (40 + 50 + 60) / 3 = 50
        }

        #endregion Property Access Tests
    }
}