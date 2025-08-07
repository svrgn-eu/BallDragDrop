using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for ThisQualifierAnalyzer
    /// Tests mandatory enforcement of 'this.' qualifier for instance member access
    /// </summary>
    [TestClass]
    public class ThisQualifierAnalyzerTests
    {
        #region Properties

        private ThisQualifierAnalyzer _analyzer;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._analyzer = new ThisQualifierAnalyzer();
        }

        #endregion Construction

        #region Methods

        #region Property Access Tests

        [TestMethod]
        public void PropertyAccessWithoutThis_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestProperty { get; set; }

        public void TestMethod()
        {
            var value = TestProperty;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10001" });
        }

        [TestMethod]
        public void PropertyAccessWithThis_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestProperty { get; set; }

        public void TestMethod()
        {
            var value = this.TestProperty;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void PropertyAssignmentWithoutThis_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestProperty { get; set; }

        public void TestMethod()
        {
            TestProperty = ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10001" });
        }

        [TestMethod]
        public void PropertyAssignmentWithThis_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestProperty { get; set; }

        public void TestMethod()
        {
            this.TestProperty = ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        #endregion Property Access Tests

        #region Method Call Tests

        [TestMethod]
        public void MethodCallWithoutThis_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            AnotherMethod();
        }

        private void AnotherMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10002" });
        }

        [TestMethod]
        public void MethodCallWithThis_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            this.AnotherMethod();
        }

        private void AnotherMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        #endregion Method Call Tests

        #region Field Access Tests

        [TestMethod]
        public void FieldAccessWithoutThis_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _testField;

        public void TestMethod()
        {
            var value = _testField;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10003" });
        }

        [TestMethod]
        public void FieldAccessWithThis_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _testField;

        public void TestMethod()
        {
            var value = this._testField;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void FieldAssignmentWithoutThis_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _testField;

        public void TestMethod()
        {
            _testField = ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10003" });
        }

        #endregion Field Access Tests

        #region Static Member Tests

        [TestMethod]
        public void StaticPropertyAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public static string StaticProperty { get; set; }

        public void TestMethod()
        {
            var value = StaticProperty;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void StaticMethodCall_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            StaticMethod();
        }

        private static void StaticMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void StaticFieldAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private static string _staticField;

        public void TestMethod()
        {
            var value = _staticField;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        #endregion Static Member Tests

        #region Local Variable and Parameter Tests

        [TestMethod]
        public void LocalVariableAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            var localVariable = ""test"";
            var value = localVariable;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void ParameterAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod(string parameter)
        {
            var value = parameter;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        #endregion Local Variable and Parameter Tests

        #region Multiple Violations Tests

        [TestMethod]
        public void MultipleViolations_ShouldReportMultipleDiagnostics()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;
        public string Property { get; set; }

        public void TestMethod()
        {
            var value1 = _field;
            var value2 = Property;
            AnotherMethod();
        }

        private void AnotherMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10003", "BDD10001", "BDD10002" });
        }

        #endregion Multiple Violations Tests

        #region External Type Access Tests

        [TestMethod]
        public void ExternalTypeAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
using System;

namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            var value = Console.WriteLine;
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        [TestMethod]
        public void NamespaceAccess_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            var type = typeof(System.String);
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source);
        }

        #endregion External Type Access Tests

        #region Constructor Tests

        [TestMethod]
        public void ConstructorFieldInitialization_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public TestClass()
        {
            _field = ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10003" });
        }

        [TestMethod]
        public void ConstructorPropertyInitialization_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string Property { get; set; }

        public TestClass()
        {
            Property = ""test"";
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD10001" });
        }

        #endregion Constructor Tests

        #endregion Methods
    }
}