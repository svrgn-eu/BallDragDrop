using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Basic unit tests for XmlDocumentationAnalyzer to verify core functionality
    /// </summary>
    [TestClass]
    public class XmlDocumentationAnalyzerBasicTests
    {
        private XmlDocumentationAnalyzer _analyzer = null!;

        [TestInitialize]
        public void Setup()
        {
            _analyzer = new XmlDocumentationAnalyzer();
        }

        [TestMethod]
        public void PublicMethodWithoutXmlDocumentation_ShouldReportMissingDocumentation()
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
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5001" });
        }

        [TestMethod]
        public void PublicMethodWithCompleteXmlDocumentation_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method performs a test operation
        /// </summary>
        /// <param name=""parameter"">The input parameter</param>
        /// <returns>The result of the operation</returns>
        public string TestMethod(string parameter)
        {
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void PrivateMethodWithoutXmlDocumentation_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private void PrivateMethod()
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void PublicMethodWithMissingSummary_ShouldReportIncompleteDocumentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <param name=""parameter"">The input parameter</param>
        /// <returns>The result of the operation</returns>
        public string TestMethod(string parameter)
        {
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5002" });
        }

        [TestMethod]
        public void PublicMethodWithThrowStatementButNoExceptionDocumentation_ShouldReportMissingExceptionDocumentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method performs a test operation
        /// </summary>
        /// <param name=""parameter"">The input parameter</param>
        /// <returns>The result of the operation</returns>
        public string TestMethod(string parameter)
        {
            if (parameter == null)
                throw new System.ArgumentNullException(nameof(parameter));
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5003" });
        }
    }
}