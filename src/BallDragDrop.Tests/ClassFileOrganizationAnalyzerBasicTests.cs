using BallDragDrop.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Basic unit tests for ClassFileOrganizationAnalyzer
    /// Tests core functionality without dependencies on main project
    /// </summary>
    [TestClass]
    public class ClassFileOrganizationAnalyzerBasicTests
    {
        #region Properties

        private ClassFileOrganizationAnalyzer _analyzer;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._analyzer = new ClassFileOrganizationAnalyzer();
        }

        #endregion Construction

        #region Methods

        [TestMethod]
        public void SingleClassWithMatchingFilename_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act
            var diagnostics = this.GetDiagnostics(source, "TestClass.cs");

            // Assert
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for single class with matching filename");
        }

        [TestMethod]
        public void SingleClassWithNonMatchingFilename_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act
            var diagnostics = this.GetDiagnostics(source, "WrongName.cs");

            // Assert
            Assert.AreEqual(1, diagnostics.Length, "Expected one diagnostic for filename mismatch");
            Assert.AreEqual("BDD11002", diagnostics[0].Id, "Expected BDD11002 diagnostic for filename mismatch");
        }

        [TestMethod]
        public void MultipleClassesInFile_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class FirstClass
    {
    }

    public class SecondClass
    {
    }
}";

            // Act
            var diagnostics = this.GetDiagnostics(source, "FirstClass.cs");

            // Assert
            Assert.AreEqual(1, diagnostics.Length, "Expected one diagnostic for multiple classes");
            Assert.AreEqual("BDD11001", diagnostics[0].Id, "Expected BDD11001 diagnostic for multiple classes");
        }

        [TestMethod]
        public void PartialClassWithNonMatchingFilename_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace Test
{
    public partial class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act
            var diagnostics = this.GetDiagnostics(source, "TestClass.Designer.cs");

            // Assert
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for partial class with non-matching filename");
        }

        [TestMethod]
        public void NestedClass_ShouldNotReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class OuterClass
    {
        public class NestedClass
        {
        }
    }
}";

            // Act
            var diagnostics = this.GetDiagnostics(source, "OuterClass.cs");

            // Assert
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for nested class");
        }

        private Diagnostic[] GetDiagnostics(string source, string filePath)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
            
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(this._analyzer));
            
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        #endregion Methods
    }
}