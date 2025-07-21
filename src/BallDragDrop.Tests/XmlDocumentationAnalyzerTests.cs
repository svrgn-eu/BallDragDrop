using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for XmlDocumentationAnalyzer
    /// Tests validation of XML documentation completeness for public methods
    /// </summary>
    [TestClass]
    public class XmlDocumentationAnalyzerTests
    {
        private XmlDocumentationAnalyzer _analyzer;

        [TestInitialize]
        public void Setup()
        {
            _analyzer = new XmlDocumentationAnalyzer();
        }

        #region Missing XML Documentation Tests

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
        public void MultiplePublicMethodsWithoutXmlDocumentation_ShouldReportMultipleDiagnostics()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void FirstMethod()
        {
            // Method implementation
        }

        public void SecondMethod()
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5001", "BDD5001" });
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
        public void InternalMethodWithoutXmlDocumentation_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        internal void InternalMethod()
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion

        #region Complete XML Documentation Tests

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
        public void PublicVoidMethodWithCompleteXmlDocumentation_ShouldNotReportDiagnostic()
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
        public void TestMethod(string parameter)
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void PublicMethodWithNoParametersAndCompleteDocumentation_ShouldNotReportDiagnostic()
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
        /// <returns>The result of the operation</returns>
        public string TestMethod()
        {
            return ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion

        #region Incomplete XML Documentation Tests

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
        public void PublicMethodWithMissingParameterDocumentation_ShouldReportIncompleteDocumentation()
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
        public void PublicMethodWithMissingReturnsDocumentation_ShouldReportIncompleteDocumentation()
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
        public void PublicMethodWithMultipleMissingElements_ShouldReportIncompleteDocumentation()
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
        public string TestMethod(string parameter1, int parameter2)
        {
            return parameter1;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5002" });
        }

        [TestMethod]
        public void PublicMethodWithPartialParameterDocumentation_ShouldReportIncompleteDocumentation()
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
        /// <param name=""parameter1"">The first parameter</param>
        /// <returns>The result of the operation</returns>
        public string TestMethod(string parameter1, int parameter2)
        {
            return parameter1;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5002" });
        }

        #endregion

        #region Exception Documentation Tests

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

        [TestMethod]
        public void PublicMethodWithExceptionDocumentation_ShouldNotReportDiagnostic()
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
        /// <exception cref=""System.ArgumentNullException"">Thrown when parameter is null</exception>
        public string TestMethod(string parameter)
        {
            if (parameter == null)
                throw new System.ArgumentNullException(nameof(parameter));
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void PublicMethodWithArrayAccessButNoExceptionDocumentation_ShouldReportMissingExceptionDocumentation()
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
        /// <param name=""array"">The input array</param>
        /// <returns>The first element</returns>
        public string TestMethod(string[] array)
        {
            return array[0];
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5003" });
        }

        [TestMethod]
        public void PublicMethodWithCastButNoExceptionDocumentation_ShouldReportMissingExceptionDocumentation()
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
        /// <param name=""obj"">The input object</param>
        /// <returns>The converted string</returns>
        public string TestMethod(object obj)
        {
            return (string)obj;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5003" });
        }

        #endregion

        #region Property Accessor Tests

        [TestMethod]
        public void PublicAutoProperty_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestProperty { get; set; }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void PublicPropertyWithExplicitAccessors_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _testField;
        
        public string TestProperty
        {
            get { return _testField; }
            set { _testField = value; }
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion

        #region Compiler Generated Tests

        [TestMethod]
        public void CompilerGeneratedMethod_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
using System.Runtime.CompilerServices;

namespace BallDragDrop.Services
{
    public class TestClass
    {
        [CompilerGenerated]
        public void GeneratedMethod()
        {
            // Generated method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion

        #region Mixed Scenarios Tests

        [TestMethod]
        public void MixedPublicAndPrivateMethods_ShouldReportOnlyPublicMethodViolations()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method has complete documentation
        /// </summary>
        /// <param name=""parameter"">The input parameter</param>
        /// <returns>The result</returns>
        public string DocumentedMethod(string parameter)
        {
            return parameter;
        }

        public void UndocumentedPublicMethod()
        {
            // Method implementation
        }

        private void UndocumentedPrivateMethod()
        {
            // Method implementation
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5001" });
        }

        [TestMethod]
        public void MethodWithBothIncompleteAndMissingExceptionDocumentation_ShouldReportBothDiagnostics()
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
        public string TestMethod(string parameter)
        {
            if (parameter == null)
                throw new System.ArgumentNullException(nameof(parameter));
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5002", "BDD5003" });
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void EmptyClass_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void ClassWithOnlyFields_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field1;
        public int Field2;
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        [TestMethod]
        public void MethodWithComplexSignature_ShouldReportCorrectly()
        {
            // Arrange
            var source = @"
using System.Threading.Tasks;

namespace BallDragDrop.Services
{
    public class TestClass
    {
        public async Task<string> ComplexMethodName<T>(T parameter, int count = 0) where T : class
        {
            return await Task.FromResult(""test"");
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5001" });
        }

        [TestMethod]
        public void MethodWithExpressionBody_ShouldReportCorrectly()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestMethod(string parameter) => parameter.ToUpper();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5001" });
        }

        #endregion

        #region Diagnostic Location Tests

        [TestMethod]
        public void MissingXmlDocumentationDiagnostic_ShouldReportAtMethodName()
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
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD5001", 6, 21);
        }

        [TestMethod]
        public void IncompleteXmlDocumentationDiagnostic_ShouldReportAtMethodName()
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
        public string TestMethod(string parameter)
        {
            return parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD5002", 9, 23);
        }

        [TestMethod]
        public void MissingExceptionDocumentationDiagnostic_ShouldReportAtMethodName()
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
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD5003", 11, 23);
        }

        #endregion

        #region Exception Detection Tests

        [TestMethod]
        public void MethodWithParseCall_ShouldReportMissingExceptionDocumentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method parses a string to integer
        /// </summary>
        /// <param name=""value"">The string value</param>
        /// <returns>The parsed integer</returns>
        public int TestMethod(string value)
        {
            return int.Parse(value);
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5003" });
        }

        [TestMethod]
        public void MethodWithLinqFirstCall_ShouldReportMissingExceptionDocumentation()
        {
            // Arrange
            var source = @"
using System.Linq;

namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method gets the first element
        /// </summary>
        /// <param name=""array"">The input array</param>
        /// <returns>The first element</returns>
        public string TestMethod(string[] array)
        {
            return array.First();
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD5003" });
        }

        [TestMethod]
        public void MethodWithoutExceptionThrowingCode_ShouldNotReportExceptionDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// This method performs a safe operation
        /// </summary>
        /// <param name=""parameter"">The input parameter</param>
        /// <returns>The result</returns>
        public string TestMethod(string parameter)
        {
            return parameter?.ToUpper() ?? ""default"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion
    }
}