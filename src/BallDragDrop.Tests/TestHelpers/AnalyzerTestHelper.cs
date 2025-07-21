using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// Helper class for testing analyzers
    /// </summary>
    public static class AnalyzerTestHelper
    {
        /// <summary>
        /// Creates a compilation from source code
        /// </summary>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        /// <returns>A compilation object</returns>
        public static Compilation CreateCompilation(string source, string filePath = "Test.cs")
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
            
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        /// <summary>
        /// Runs an analyzer on the given source code and returns diagnostics
        /// </summary>
        /// <param name="analyzer">The analyzer to run</param>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        /// <returns>Array of diagnostics</returns>
        public static Diagnostic[] GetDiagnostics(DiagnosticAnalyzer analyzer, string source, string filePath = "Test.cs")
        {
            var compilation = CreateCompilation(source, filePath);
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        /// <summary>
        /// Verifies that no diagnostics are reported for the given source
        /// </summary>
        /// <param name="analyzer">The analyzer to run</param>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        public static void VerifyNoDiagnostics(DiagnosticAnalyzer analyzer, string source, string filePath = "Test.cs")
        {
            var diagnostics = GetDiagnostics(analyzer, source, filePath);
            
            if (diagnostics.Length > 0)
            {
                var diagnosticMessages = string.Join("\n", diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
                throw new AssertFailedException($"Expected no diagnostics, but found:\n{diagnosticMessages}");
            }
        }

        /// <summary>
        /// Verifies that specific diagnostics are reported for the given source
        /// </summary>
        /// <param name="analyzer">The analyzer to run</param>
        /// <param name="source">The source code</param>
        /// <param name="expectedDiagnosticIds">The expected diagnostic IDs</param>
        /// <param name="filePath">The file path for the source</param>
        public static void VerifyDiagnostics(DiagnosticAnalyzer analyzer, string source, string[] expectedDiagnosticIds, string filePath = "Test.cs")
        {
            var diagnostics = GetDiagnostics(analyzer, source, filePath);
            var actualIds = diagnostics.Select(d => d.Id).ToArray();
            
            CollectionAssert.AreEqual(expectedDiagnosticIds, actualIds, 
                $"Expected diagnostics: [{string.Join(", ", expectedDiagnosticIds)}], " +
                $"but found: [{string.Join(", ", actualIds)}]");
        }

        /// <summary>
        /// Verifies that a specific diagnostic is reported at the expected location
        /// </summary>
        /// <param name="analyzer">The analyzer to run</param>
        /// <param name="source">The source code</param>
        /// <param name="expectedDiagnosticId">The expected diagnostic ID</param>
        /// <param name="expectedLine">The expected line number (1-based)</param>
        /// <param name="expectedColumn">The expected column number (1-based)</param>
        /// <param name="filePath">The file path for the source</param>
        public static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, string source, string expectedDiagnosticId, int expectedLine, int expectedColumn, string filePath = "Test.cs")
        {
            var diagnostics = GetDiagnostics(analyzer, source, filePath);
            var diagnostic = diagnostics.FirstOrDefault(d => d.Id == expectedDiagnosticId);
            
            Assert.IsNotNull(diagnostic, $"Expected diagnostic {expectedDiagnosticId} was not found");
            
            var lineSpan = diagnostic.Location.GetLineSpan();
            var actualLine = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
            var actualColumn = lineSpan.StartLinePosition.Character + 1; // Convert to 1-based
            
            Assert.AreEqual(expectedLine, actualLine, $"Expected diagnostic at line {expectedLine}, but found at line {actualLine}");
            Assert.AreEqual(expectedColumn, actualColumn, $"Expected diagnostic at column {expectedColumn}, but found at column {actualColumn}");
        }
    }
}