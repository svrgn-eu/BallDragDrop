using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for ThisQualifierCodeFixProvider
    /// Tests automatic insertion of 'this.' qualifier for instance member access
    /// </summary>
    [TestClass]
    public class ThisQualifierCodeFixProviderTests
    {
        #region Properties

        private ThisQualifierAnalyzer _analyzer;
        private ThisQualifierCodeFixProvider _codeFixProvider;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._analyzer = new ThisQualifierAnalyzer();
            this._codeFixProvider = new ThisQualifierCodeFixProvider();
        }

        #endregion Construction

        #region Methods

        #region Property Access Code Fix Tests

        [TestMethod]
        public void PropertyAccessWithoutThis_ShouldAddThisQualifier()
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

            var expected = @"
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
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        [TestMethod]
        public void PropertyAssignmentWithoutThis_ShouldAddThisQualifier()
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

            var expected = @"
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
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Property Access Code Fix Tests

        #region Method Call Code Fix Tests

        [TestMethod]
        public void MethodCallWithoutThis_ShouldAddThisQualifier()
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

            var expected = @"
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
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        [TestMethod]
        public void MethodCallWithParameters_ShouldAddThisQualifier()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            AnotherMethod(""parameter"", 42);
        }

        private void AnotherMethod(string param1, int param2)
        {
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
            this.AnotherMethod(""parameter"", 42);
        }

        private void AnotherMethod(string param1, int param2)
        {
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Method Call Code Fix Tests

        #region Field Access Code Fix Tests

        [TestMethod]
        public void FieldAccessWithoutThis_ShouldAddThisQualifier()
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

            var expected = @"
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
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        [TestMethod]
        public void FieldAssignmentWithoutThis_ShouldAddThisQualifier()
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

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _testField;

        public void TestMethod()
        {
            this._testField = ""test"";
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Field Access Code Fix Tests

        #region Multiple Violations Code Fix Tests

        [TestMethod]
        public void MultipleViolations_ShouldFixAllViolations()
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
            _field = ""updated"";
            Property = ""updated"";
        }

        private void AnotherMethod()
        {
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;
        public string Property { get; set; }

        public void TestMethod()
        {
            var value1 = this._field;
            var value2 = this.Property;
            this.AnotherMethod();
            this._field = ""updated"";
            this.Property = ""updated"";
        }

        private void AnotherMethod()
        {
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Multiple Violations Code Fix Tests

        #region Trivia Preservation Tests

        [TestMethod]
        public void CodeFixShouldPreserveLeadingTrivia()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public void TestMethod()
        {
            // This is a comment
            var value = _field;
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public void TestMethod()
        {
            // This is a comment
            var value = this._field;
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        [TestMethod]
        public void CodeFixShouldPreserveIndentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public void TestMethod()
        {
            if (true)
            {
                var value = _field;
            }
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public void TestMethod()
        {
            if (true)
            {
                var value = this._field;
            }
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Trivia Preservation Tests

        #region Constructor Code Fix Tests

        [TestMethod]
        public void ConstructorFieldInitialization_ShouldAddThisQualifier()
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

            var expected = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        private string _field;

        public TestClass()
        {
            this._field = ""test"";
        }
    }
}";

            // Act & Assert
            CodeFixTestHelper.VerifyCodeFix(this._analyzer, this._codeFixProvider, source, expected);
        }

        #endregion Constructor Code Fix Tests

        #endregion Methods
    }
}