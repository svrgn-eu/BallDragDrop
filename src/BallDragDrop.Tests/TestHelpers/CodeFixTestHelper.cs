using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace BallDragDrop.Tests.TestHelpers
{
    /// <summary>
    /// Helper class for testing code fix providers
    /// </summary>
    public static class CodeFixTestHelper
    {
        /// <summary>
        /// Verifies that a code fix provider correctly fixes the given source code
        /// </summary>
        /// <param name="analyzer">The analyzer that reports the diagnostics</param>
        /// <param name="codeFixProvider">The code fix provider to test</param>
        /// <param name="source">The source code with issues</param>
        /// <param name="expected">The expected fixed source code</param>
        /// <param name="filePath">The file path for the source</param>
        public static void VerifyCodeFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string source, string expected, string filePath = "Test.cs")
        {
            var document = CreateDocument(source, filePath);
            var compilation = document.Project.GetCompilationAsync().Result;
            
            // Get diagnostics from the analyzer
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            
            Assert.IsTrue(diagnostics.Length > 0, "No diagnostics found to fix");
            
            // Apply all available code fixes
            var fixedDocument = ApplyCodeFixes(document, codeFixProvider, diagnostics);
            
            // Get the fixed source code
            var fixedSource = fixedDocument.GetSyntaxRootAsync().Result.ToFullString();
            
            // Normalize whitespace for comparison
            var normalizedExpected = NormalizeWhitespace(expected);
            var normalizedActual = NormalizeWhitespace(fixedSource);
            
            Assert.AreEqual(normalizedExpected, normalizedActual, 
                $"Code fix did not produce expected result.\n\nExpected:\n{normalizedExpected}\n\nActual:\n{normalizedActual}");
        }

        /// <summary>
        /// Creates a test document from source code (public version for test access)
        /// </summary>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        /// <returns>A document object</returns>
        public static Document CreateTestDocument(string source, string filePath = "Test.cs")
        {
            return CreateDocument(source, filePath);
        }

        /// <summary>
        /// Gets diagnostics from an analyzer for a document
        /// </summary>
        /// <param name="analyzer">The analyzer to run</param>
        /// <param name="document">The document to analyze</param>
        /// <returns>Array of diagnostics</returns>
        public static Diagnostic[] GetDiagnostics(DiagnosticAnalyzer analyzer, Document document)
        {
            var compilation = document.Project.GetCompilationAsync().Result;
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result.ToArray();
        }

        /// <summary>
        /// Gets available code actions for a diagnostic (public version for test access)
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="codeFixProvider">The code fix provider</param>
        /// <param name="diagnostic">The diagnostic</param>
        /// <returns>Available code actions</returns>
        public static CodeAction[] GetCodeActions(Document document, CodeFixProvider codeFixProvider, Diagnostic diagnostic)
        {
            return GetCodeActionsInternal(document, codeFixProvider, diagnostic);
        }

        /// <summary>
        /// Creates a document from source code
        /// </summary>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        /// <returns>A document object</returns>
        private static Document CreateDocument(string source, string filePath)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location))
                .AddDocument(documentId, filePath, source);
            
            return solution.GetDocument(documentId);
        }

        /// <summary>
        /// Applies all available code fixes to the document
        /// </summary>
        /// <param name="document">The document to fix</param>
        /// <param name="codeFixProvider">The code fix provider</param>
        /// <param name="diagnostics">The diagnostics to fix</param>
        /// <returns>The fixed document</returns>
        private static Document ApplyCodeFixes(Document document, CodeFixProvider codeFixProvider, ImmutableArray<Diagnostic> diagnostics)
        {
            var currentDocument = document;
            
            // Apply fixes for each diagnostic
            foreach (var diagnostic in diagnostics.Where(d => codeFixProvider.FixableDiagnosticIds.Contains(d.Id)))
            {
                var actions = GetCodeActionsInternal(currentDocument, codeFixProvider, diagnostic);
                
                if (actions.Any())
                {
                    // Apply the first available code action
                    var action = actions.First();
                    var operations = action.GetOperationsAsync(CancellationToken.None).Result;
                    
                    foreach (var operation in operations)
                    {
                        if (operation is ApplyChangesOperation applyChangesOperation)
                        {
                            var newSolution = applyChangesOperation.ChangedSolution;
                            currentDocument = newSolution.GetDocument(currentDocument.Id);
                        }
                    }
                }
            }
            
            return currentDocument;
        }

        /// <summary>
        /// Gets available code actions for a diagnostic (internal implementation)
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="codeFixProvider">The code fix provider</param>
        /// <param name="diagnostic">The diagnostic</param>
        /// <returns>Available code actions</returns>
        private static CodeAction[] GetCodeActionsInternal(Document document, CodeFixProvider codeFixProvider, Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();
            
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, diagnostics) => actions.Add(action),
                CancellationToken.None);
            
            codeFixProvider.RegisterCodeFixesAsync(context).Wait();
            
            return actions.ToArray();
        }

        /// <summary>
        /// Normalizes whitespace in source code for comparison
        /// </summary>
        /// <param name="source">The source code</param>
        /// <returns>Normalized source code</returns>
        private static string NormalizeWhitespace(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetRoot();
            return root.NormalizeWhitespace().ToFullString();
        }

        /// <summary>
        /// Verifies that no code fixes are available for the given source
        /// </summary>
        /// <param name="analyzer">The analyzer that reports the diagnostics</param>
        /// <param name="codeFixProvider">The code fix provider to test</param>
        /// <param name="source">The source code</param>
        /// <param name="filePath">The file path for the source</param>
        public static void VerifyNoCodeFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string source, string filePath = "Test.cs")
        {
            var document = CreateDocument(source, filePath);
            var compilation = document.Project.GetCompilationAsync().Result;
            
            // Get diagnostics from the analyzer
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            
            // Check that no code fixes are available
            foreach (var diagnostic in diagnostics.Where(d => codeFixProvider.FixableDiagnosticIds.Contains(d.Id)))
            {
                var actions = GetCodeActionsInternal(document, codeFixProvider, diagnostic);
                Assert.AreEqual(0, actions.Length, $"Expected no code fixes for diagnostic {diagnostic.Id}, but found {actions.Length}");
            }
        }
    }
}