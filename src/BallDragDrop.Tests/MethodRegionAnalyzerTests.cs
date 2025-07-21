using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for MethodRegionAnalyzer
    /// Tests validation of method region organization standards
    /// </summary>
    [TestClass]
    public class MethodRegionAnalyzerTests
    {
        private MethodRegionAnalyzer _analyzer;

        [TestInitialize]
        public void Setup()
        {
            _analyzer = new MethodRegionAnalyzer();
        }

        #region Method Not In Region Tests

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
        public void MultipleMethodsNotInRegion_ShouldReportMultipleDiagnostics()
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
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD4001", "BDD4001" });
        }

        #endregion

        #region Method In Correct Region Tests

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
        public void MultipleMethodsInCorrectRegions_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region FirstMethod
        public void FirstMethod()
        {
            // Method implementation
        }
        #endregion

        #region SecondMethod
        public void SecondMethod()
        {
            // Method implementation
        }
        #endregion
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
        }

        #endregion

        #region Incorrect Region Naming Tests

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

        [TestMethod]
        public void MethodInRegionWithExtraSpaces_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region  TestMethod  
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

        [TestMethod]
        public void MethodInRegionWithDifferentCase_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region testmethod
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

        #endregion

        #region Property Accessor Tests

        [TestMethod]
        public void PropertyGetterSetter_ShouldNotReportDiagnostic()
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
        public void PropertyWithExplicitAccessors_ShouldNotReportDiagnostic()
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
        public void MixedCorrectAndIncorrectMethods_ShouldReportOnlyIncorrectOnes()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region CorrectMethod
        public void CorrectMethod()
        {
            // Method implementation
        }
        #endregion

        public void MethodWithoutRegion()
        {
            // Method implementation
        }

        #region WrongRegionName
        public void MethodWithWrongRegion()
        {
            // Method implementation
        }
        #endregion
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD4001", "BDD4002" });
        }

        #endregion

        #region Nested Region Tests

        [TestMethod]
        public void MethodInNestedRegion_ShouldWorkCorrectly()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        #region OuterRegion
        
        #region TestMethod
        public void TestMethod()
        {
            // Method implementation
        }
        #endregion
        
        #endregion
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source);
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
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD4001" });
        }

        #endregion

        #region Diagnostic Location Tests

        [TestMethod]
        public void MethodNotInRegionDiagnostic_ShouldReportAtMethodName()
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
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD4001", 6, 21);
        }

        [TestMethod]
        public void IncorrectRegionNamingDiagnostic_ShouldReportAtMethodName()
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
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD4002", 7, 21);
        }

        #endregion
    }
}