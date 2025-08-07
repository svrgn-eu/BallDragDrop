using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;
using BallDragDrop.CodeAnalysis;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Simple standalone test for WPF XAML code-behind file handling
    /// </summary>
    [TestClass]
    public class SimpleWpfTest
    {
        #region Methods

        [TestMethod]
        public void TestWpfXamlCodeBehindFileHandling()
        {
            // Arrange
            var analyzer = new ClassFileOrganizationAnalyzer();
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Create compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "MainWindow.xaml.cs");
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Act
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

            // Assert
            Assert.AreEqual(0, diagnostics.Length, $"Expected no diagnostics for valid WPF code-behind file, but found: {string.Join(", ", diagnostics.Select(d => d.Id))}");
        }

        [TestMethod]
        public void TestNonWpfFileWithXamlCsExtension()
        {
            // Arrange
            var analyzer = new ClassFileOrganizationAnalyzer();
            var source = @"
namespace BallDragDrop.Services
{
    public class SomeService
    {
        public void DoSomething()
        {
        }
    }
}";

            // Create compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "MainWindow.xaml.cs");
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Act
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

            // Assert
            Assert.AreEqual(1, diagnostics.Length, "Expected one diagnostic for invalid WPF code-behind file");
            Assert.AreEqual("BDD11002", diagnostics[0].Id, "Expected filename mismatch diagnostic");
        }

        #endregion Methods
    }
}