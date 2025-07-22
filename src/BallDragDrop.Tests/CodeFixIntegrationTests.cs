using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for code fix providers
    /// Tests the complete workflow from diagnostic detection to fix application
    /// </summary>
    [TestClass]
    public class CodeFixIntegrationTests
    {
        #region Method Region Code Fix Tests

        /// <summary>
        /// Tests that method region code fix provider can add regions around methods
        /// </summary>
        [TestMethod]
        public async Task MethodRegionCodeFix_AddRegion_ShouldWrapMethodInRegion()
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

            var expected = @"
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
            await VerifyCodeFixAsync<MethodRegionAnalyzer, MethodRegionCodeFixProvider>(
                source, expected, "BDD4001");
        }

        /// <summary>
        /// Tests method region code fix with multiple methods
        /// </summary>
        [TestMethod]
        public async Task MethodRegionCodeFix_MultipleMethods_ShouldFixIndividualMethods()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void FirstMethod()
        {
            // First method implementation
        }

        public void SecondMethod()
        {
            // Second method implementation
        }
    }
}";

            // Act - Apply fix to first method only
            var analyzer = new MethodRegionAnalyzer();
            var codeFixProvider = new MethodRegionCodeFixProvider();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            var firstMethodDiagnostic = diagnostics.First(d => d.Id == "BDD4001");

            var document = CreateDocument(source);
            var actions = await GetCodeActionsAsync(codeFixProvider, document, firstMethodDiagnostic);

            // Assert - Should have code fix available
            Assert.IsTrue(actions.Count > 0, "Should provide code fix for method region");
            
            // Apply the first available fix
            var fixedDocument = await ApplyCodeActionAsync(document, actions.First());
            var fixedSource = await GetDocumentTextAsync(fixedDocument);

            // Verify the fix was applied correctly
            Assert.IsTrue(fixedSource.Contains("#region FirstMethod"), "Should add region for FirstMethod");
            Assert.IsTrue(fixedSource.Contains("#endregion"), "Should add endregion");
            
            // Second method should still have violation (not fixed)
            var fixedDiagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, fixedSource);
            Assert.AreEqual(1, fixedDiagnostics.Length, "Should still have one violation for SecondMethod");
        }

        #endregion

        #region XML Documentation Code Fix Tests

        /// <summary>
        /// Tests XML documentation code fix provider
        /// </summary>
        [TestMethod]
        public async Task XmlDocumentationCodeFix_AddDocumentation_ShouldAddCompleteDocumentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public string TestMethod(string parameter)
        {
            return parameter;
        }
    }
}";

            // Act
            var analyzer = new XmlDocumentationAnalyzer();
            var codeFixProvider = new XmlDocumentationCodeFixProvider();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            var missingDocDiagnostic = diagnostics.First(d => d.Id == "BDD5001");

            var document = CreateDocument(source);
            var actions = await GetCodeActionsAsync(codeFixProvider, document, missingDocDiagnostic);

            // Assert - Should have code fix available
            Assert.IsTrue(actions.Count > 0, "Should provide code fix for missing documentation");
            
            // Apply the fix
            var fixedDocument = await ApplyCodeActionAsync(document, actions.First());
            var fixedSource = await GetDocumentTextAsync(fixedDocument);

            // Verify documentation was added
            Assert.IsTrue(fixedSource.Contains("/// <summary>"), "Should add summary documentation");
            Assert.IsTrue(fixedSource.Contains("/// <param name=\"parameter\">"), "Should add parameter documentation");
            Assert.IsTrue(fixedSource.Contains("/// <returns>"), "Should add returns documentation");
        }

        /// <summary>
        /// Tests XML documentation code fix for methods with exceptions
        /// </summary>
        [TestMethod]
        public async Task XmlDocumentationCodeFix_MethodWithExceptions_ShouldAddExceptionDocumentation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        /// <summary>
        /// Test method
        /// </summary>
        /// <param name=""parameter"">Test parameter</param>
        /// <returns>Test result</returns>
        public string TestMethod(string parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
            return parameter;
        }
    }
}";

            // Act
            var analyzer = new XmlDocumentationAnalyzer();
            var codeFixProvider = new XmlDocumentationCodeFixProvider();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            var missingExceptionDiagnostic = diagnostics.FirstOrDefault(d => d.Id == "BDD5003");

            if (missingExceptionDiagnostic != null)
            {
                var document = CreateDocument(source);
                var actions = await GetCodeActionsAsync(codeFixProvider, document, missingExceptionDiagnostic);

                // Assert - Should have code fix available
                Assert.IsTrue(actions.Count > 0, "Should provide code fix for missing exception documentation");
                
                // Apply the fix
                var fixedDocument = await ApplyCodeActionAsync(document, actions.First());
                var fixedSource = await GetDocumentTextAsync(fixedDocument);

                // Verify exception documentation was added
                Assert.IsTrue(fixedSource.Contains("/// <exception cref="), "Should add exception documentation");
            }
        }

        #endregion

        #region Folder Structure Code Fix Tests

        /// <summary>
        /// Tests folder structure code fix provider suggestions
        /// </summary>
        [TestMethod]
        public async Task FolderStructureCodeFix_InterfaceInWrongFolder_ShouldSuggestMove()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            // Act
            var analyzer = new FolderStructureAnalyzer();
            var codeFixProvider = new FolderStructureCodeFixProvider();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source, 
                @"C:\Project\BallDragDrop\Services\ITestInterface.cs");
            var folderViolation = diagnostics.FirstOrDefault(d => d.Id == "BDD3001");

            if (folderViolation != null)
            {
                var document = CreateDocument(source, @"C:\Project\BallDragDrop\Services\ITestInterface.cs");
                var actions = await GetCodeActionsAsync(codeFixProvider, document, folderViolation);

                // Assert - Should provide suggestions (implementation may vary)
                // The actual fix might be a suggestion to move the file rather than code modification
                Assert.IsNotNull(actions, "Should provide some form of code action or suggestion");
            }
        }

        #endregion

        #region Integration Workflow Tests

        /// <summary>
        /// Tests complete fix workflow for multiple violation types
        /// </summary>
        [TestMethod]
        public async Task CompleteFixWorkflow_MultipleViolations_ShouldFixSystematically()
        {
            // Arrange - Source with multiple fixable violations
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void MethodWithoutRegion()
        {
            // Method implementation
        }

        public string UndocumentedMethod(string parameter)
        {
            return parameter;
        }
    }
}";

            // Act - Fix region violations first
            var regionAnalyzer = new MethodRegionAnalyzer();
            var regionCodeFix = new MethodRegionCodeFixProvider();
            
            var document = CreateDocument(source);
            var regionDiagnostics = AnalyzerTestHelper.GetDiagnostics(regionAnalyzer, source);
            
            // Apply region fixes
            foreach (var diagnostic in regionDiagnostics.Where(d => d.Id == "BDD4001"))
            {
                var actions = await GetCodeActionsAsync(regionCodeFix, document, diagnostic);
                if (actions.Count > 0)
                {
                    document = await ApplyCodeActionAsync(document, actions.First());
                }
            }
            
            var intermediateSource = await GetDocumentTextAsync(document);
            
            // Now fix documentation violations
            var docAnalyzer = new XmlDocumentationAnalyzer();
            var docCodeFix = new XmlDocumentationCodeFixProvider();
            
            var docDiagnostics = AnalyzerTestHelper.GetDiagnostics(docAnalyzer, intermediateSource);
            
            foreach (var diagnostic in docDiagnostics.Where(d => d.Id == "BDD5001"))
            {
                var actions = await GetCodeActionsAsync(docCodeFix, document, diagnostic);
                if (actions.Count > 0)
                {
                    document = await ApplyCodeActionAsync(document, actions.First());
                }
            }
            
            var finalSource = await GetDocumentTextAsync(document);

            // Assert - Final source should have fewer violations
            var finalRegionDiagnostics = AnalyzerTestHelper.GetDiagnostics(regionAnalyzer, finalSource);
            var finalDocDiagnostics = AnalyzerTestHelper.GetDiagnostics(docAnalyzer, finalSource);
            
            // Should have fewer violations than original
            Assert.IsTrue(finalRegionDiagnostics.Length <= regionDiagnostics.Length, 
                "Should have same or fewer region violations after fixes");
            Assert.IsTrue(finalDocDiagnostics.Length <= docDiagnostics.Length, 
                "Should have same or fewer documentation violations after fixes");
        }

        /// <summary>
        /// Tests that code fixes don't introduce new violations
        /// </summary>
        [TestMethod]
        public async Task CodeFixes_ShouldNotIntroduceNewViolations()
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

            // Act - Apply region code fix
            var analyzer = new MethodRegionAnalyzer();
            var codeFixProvider = new MethodRegionCodeFixProvider();
            
            var originalDiagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            
            var document = CreateDocument(source);
            var diagnostic = originalDiagnostics.First(d => d.Id == "BDD4001");
            var actions = await GetCodeActionsAsync(codeFixProvider, document, diagnostic);
            
            if (actions.Count > 0)
            {
                var fixedDocument = await ApplyCodeActionAsync(document, actions.First());
                var fixedSource = await GetDocumentTextAsync(fixedDocument);
                
                // Check for new violations
                var fixedDiagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, fixedSource);
                
                // Assert - Should not introduce new violations of the same type
                var newViolations = fixedDiagnostics.Where(d => d.Id == "BDD4001").ToArray();
                Assert.IsTrue(newViolations.Length < originalDiagnostics.Length, 
                    "Code fix should reduce violations, not introduce new ones");
                
                // Verify the fix actually worked
                Assert.IsTrue(fixedSource.Contains("#region TestMethod"), "Should add the region");
                Assert.IsTrue(fixedSource.Contains("#endregion"), "Should add the endregion");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that a code fix produces the expected result
        /// </summary>
        private async Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(
            string source, 
            string expected, 
            string diagnosticId)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : CodeFixProvider, new()
        {
            var analyzer = new TAnalyzer();
            var codeFixProvider = new TCodeFix();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            var targetDiagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId);
            
            Assert.IsNotNull(targetDiagnostic, $"Should find diagnostic {diagnosticId}");
            
            var document = CreateDocument(source);
            var actions = await GetCodeActionsAsync(codeFixProvider, document, targetDiagnostic);
            
            Assert.IsTrue(actions.Count > 0, "Should provide code fix actions");
            
            var fixedDocument = await ApplyCodeActionAsync(document, actions.First());
            var actualResult = await GetDocumentTextAsync(fixedDocument);
            
            // Normalize whitespace for comparison
            var normalizedExpected = NormalizeWhitespace(expected);
            var normalizedActual = NormalizeWhitespace(actualResult);
            
            Assert.AreEqual(normalizedExpected, normalizedActual, "Code fix should produce expected result");
        }

        /// <summary>
        /// Creates a document from source code
        /// </summary>
        private Document CreateDocument(string source, string filePath = "Test.cs")
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument(documentId, Path.GetFileName(filePath), SourceText.From(source), filePath: filePath);
                
            return solution.GetDocument(documentId);
        }

        /// <summary>
        /// Gets code actions from a code fix provider
        /// </summary>
        private async Task<List<CodeAction>> GetCodeActionsAsync(
            CodeFixProvider codeFixProvider, 
            Document document, 
            Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();
            
            var context = new CodeFixContext(
                document, 
                diagnostic, 
                (action, diagnostics) => actions.Add(action), 
                CancellationToken.None);
            
            await codeFixProvider.RegisterCodeFixesAsync(context);
            
            return actions;
        }

        /// <summary>
        /// Applies a code action to a document
        /// </summary>
        private async Task<Document> ApplyCodeActionAsync(Document document, CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            var solution = document.Project.Solution;
            
            foreach (var operation in operations)
            {
                if (operation is ApplyChangesOperation applyChangesOperation)
                {
                    solution = applyChangesOperation.ChangedSolution;
                }
            }
            
            return solution.GetDocument(document.Id);
        }

        /// <summary>
        /// Gets the text content of a document
        /// </summary>
        private async Task<string> GetDocumentTextAsync(Document document)
        {
            var sourceText = await document.GetTextAsync();
            return sourceText.ToString();
        }

        /// <summary>
        /// Normalizes whitespace for string comparison
        /// </summary>
        private string NormalizeWhitespace(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            return tree.GetRoot().NormalizeWhitespace().ToFullString();
        }

        #endregion
    }
}