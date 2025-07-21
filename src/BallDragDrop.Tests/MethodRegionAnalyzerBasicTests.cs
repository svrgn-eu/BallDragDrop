using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Basic unit tests for MethodRegionAnalyzer to verify core functionality
    /// </summary>
    [TestClass]
    public class MethodRegionAnalyzerBasicTests
    {
        private MethodRegionAnalyzer _analyzer = null!;

        [TestInitialize]
        public void Setup()
        {
            _analyzer = new MethodRegionAnalyzer();
        }

        [TestMethod]
        public void MethodNotInRegion_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD4001" });
        }

        [TestMethod]
        public void MethodInCorrectRegion_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region TestMethod
        public void TestMethod()
        {
            // Method implementation
        }
        #endregion
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void MethodInIncorrectlyNamedRegion_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region WrongName
        public void TestMethod()
        {
            // Method implementation
        }
        #endregion
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD4002" });
        }
    }
}