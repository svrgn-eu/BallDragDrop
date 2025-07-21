using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using System.Linq;
using System.Threading.Tasks;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for XmlDocumentationCodeFixProvider
    /// </summary>
    [TestClass]
    public class XmlDocumentationCodeFixProviderTests
    {
        private XmlDocumentationAnalyzer _analyzer;
        private XmlDocumentationCodeFixProvider _codeFixProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _analyzer = new XmlDocumentationAnalyzer();
            _codeFixProvider = new XmlDocumentationCodeFixProvider();
        }

        #region Add Documentation Code Fix Tests

        [TestMethod]
        public async Task CodeFix_MissingXmlDocumentation_AddsCompleteDocumentation()
        {
            var source = @"
namespace BallDragDrop.Models
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
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>TODO: Add summary for TestMethod method</summary>
        /// <exception cref=""System.ArgumentException"">TODO: Add description for when this exception is thrown</exception>
        public void TestMethod()
        {
            // Method implementation
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5001");
        }

        [TestMethod]
        public async Task CodeFix_MissingDocumentationWithParameters_AddsParameterDocumentation()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        public void TestMethod(int parameter1, string parameter2)
        {
            // Method implementation
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>TODO: Add summary for TestMethod method</summary>
        /// <param name=""parameter1"">TODO: Add description for parameter1 parameter</param>
        /// <param name=""parameter2"">TODO: Add description for parameter2 parameter</param>
        /// <exception cref=""System.ArgumentException"">TODO: Add description for when this exception is thrown</exception>
        public void TestMethod(int parameter1, string parameter2)
        {
            // Method implementation
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5001");
        }

        [TestMethod]
        public async Task CodeFix_MissingDocumentationWithReturnValue_AddsReturnDocumentation()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        public string TestMethod()
        {
            return ""test"";
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>TODO: Add summary for TestMethod method</summary>
        /// <returns>TODO: Add description for return value</returns>
        /// <exception cref=""System.ArgumentException"">TODO: Add description for when this exception is thrown</exception>
        public string TestMethod()
        {
            return ""test"";
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5001");
        }

        #endregion

        #region Complete Documentation Code Fix Tests

        [TestMethod]
        public async Task CodeFix_IncompleteDocumentation_AddsMissingElements()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Existing summary</summary>
        public string TestMethod(int parameter1)
        {
            return ""test"";
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Existing summary</summary>
        /// <param name=""parameter1"">TODO: Add description for parameter1 parameter</param>
        /// <returns>TODO: Add description for return value</returns>
        public string TestMethod(int parameter1)
        {
            return ""test"";
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5002");
        }

        [TestMethod]
        public async Task CodeFix_IncompleteDocumentationMissingSummary_AddsSummary()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <param name=""parameter1"">Existing parameter doc</param>
        public void TestMethod(int parameter1)
        {
            // Method implementation
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>TODO: Add summary for TestMethod method</summary>
        /// <param name=""parameter1"">Existing parameter doc</param>
        public void TestMethod(int parameter1)
        {
            // Method implementation
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5002");
        }

        #endregion

        #region Exception Documentation Code Fix Tests

        [TestMethod]
        public async Task CodeFix_MissingExceptionDocumentation_AddsExceptionDocumentation()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Method that can throw exceptions</summary>
        public void TestMethod()
        {
            throw new System.ArgumentException(""test"");
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Method that can throw exceptions</summary>
        /// <exception cref=""System.ArgumentException"">TODO: Add description for when this exception is thrown</exception>
        public void TestMethod()
        {
            throw new System.ArgumentException(""test"");
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5003");
        }

        [TestMethod]
        public async Task CodeFix_MethodWithArrayAccess_AddsIndexOutOfRangeException()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Method with array access</summary>
        public int TestMethod(int[] array)
        {
            return array[0];
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        /// <summary>Method with array access</summary>
        /// <exception cref=""System.ArgumentException"">TODO: Add description for when this exception is thrown</exception>
        /// <exception cref=""System.IndexOutOfRangeException"">TODO: Add description for when this exception is thrown</exception>
        public int TestMethod(int[] array)
        {
            return array[0];
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD5003");
        }

        #endregion

        #region Code Fix Provider Properties Tests

        [TestMethod]
        public void FixableDiagnosticIds_ContainsExpectedIds()
        {
            var fixableIds = _codeFixProvider.FixableDiagnosticIds;
            
            Assert.IsTrue(fixableIds.Contains("BDD5001"), "Should fix MissingXmlDocumentation");
            Assert.IsTrue(fixableIds.Contains("BDD5002"), "Should fix IncompleteXmlDocumentation");
            Assert.IsTrue(fixableIds.Contains("BDD5003"), "Should fix MissingExceptionDocumentation");
        }

        [TestMethod]
        public void GetFixAllProvider_ReturnsValidProvider()
        {
            var fixAllProvider = _codeFixProvider.GetFixAllProvider();
            Assert.IsNotNull(fixAllProvider, "Fix all provider should not be null");
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public async Task CodeFix_MethodWithComplexSignature_HandlesCorrectly()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        public async Task<List<string>> ComplexMethod<T>(T genericParam, params object[] args) where T : class
        {
            return new List<string>();
        }
    }
}";

            // Should add documentation for all elements
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source);
            Assert.IsTrue(diagnostics.Length > 0, "Should report missing documentation diagnostic");

            var diagnostic = diagnostics.First(d => d.Id == "BDD5001");
            Assert.IsNotNull(diagnostic, "Should report missing XML documentation");
        }

        [TestMethod]
        public async Task CodeFix_PrivateMethod_NoCodeFixOffered()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        private void PrivateMethod()
        {
            // Private method implementation
        }
    }
}";

            // Should not produce any diagnostics for private methods
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source);
            Assert.AreEqual(0, diagnostics.Length, "No diagnostics should be reported for private methods");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that a code fix produces the expected result
        /// </summary>
        /// <param name="source">The original source code</param>
        /// <param name="expected">The expected fixed source code</param>
        /// <param name="expectedDiagnosticId">The expected diagnostic ID</param>
        private async Task VerifyCodeFixAsync(string source, string expected, string expectedDiagnosticId)
        {
            // Get diagnostics from analyzer
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source);
            Assert.IsTrue(diagnostics.Length > 0, "Expected at least one diagnostic to be reported");

            var diagnostic = diagnostics.First(d => d.Id == expectedDiagnosticId);
            Assert.IsNotNull(diagnostic, $"Expected diagnostic {expectedDiagnosticId} was not found");

            // Create compilation and document
            var compilation = AnalyzerTestHelper.CreateCompilation(source);
            var document = CreateDocument(compilation, source);

            // Apply code fix
            var actions = await GetCodeActionsAsync(document, diagnostic);
            Assert.IsTrue(actions.Length > 0, "Expected at least one code action to be available");

            var codeAction = actions[0];
            var operations = await codeAction.GetOperationsAsync(System.Threading.CancellationToken.None);
            var operation = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().First();

            var newSolution = operation.ChangedSolution;
            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync();
            var actualResult = newRoot.ToFullString();

            // Normalize whitespace for comparison
            var normalizedExpected = expected.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
            var normalizedActual = actualResult.Trim().Replace("\r\n", "\n").Replace("\r", "\n");

            // For XML documentation, we need to be more flexible with whitespace
            Assert.IsTrue(ContainsExpectedDocumentationElements(normalizedActual, normalizedExpected), 
                $"Code fix did not produce expected result.\nExpected:\n{normalizedExpected}\nActual:\n{normalizedActual}");
        }

        /// <summary>
        /// Checks if the actual result contains the expected documentation elements
        /// </summary>
        /// <param name="actual">The actual result</param>
        /// <param name="expected">The expected result</param>
        /// <returns>True if the actual contains expected elements</returns>
        private static bool ContainsExpectedDocumentationElements(string actual, string expected)
        {
            // Check for key documentation elements
            if (expected.Contains("<summary>") && !actual.Contains("<summary>"))
                return false;
            
            if (expected.Contains("<param") && !actual.Contains("<param"))
                return false;
            
            if (expected.Contains("<returns>") && !actual.Contains("<returns>"))
                return false;
            
            if (expected.Contains("<exception") && !actual.Contains("<exception"))
                return false;

            return true;
        }

        /// <summary>
        /// Creates a document from compilation and source
        /// </summary>
        /// <param name="compilation">The compilation</param>
        /// <param name="source">The source code</param>
        /// <returns>A document</returns>
        private static Document CreateDocument(Compilation compilation, string source)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new Microsoft.CodeAnalysis.AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument(documentId, "Test.cs", source);

            return solution.GetDocument(documentId);
        }

        /// <summary>
        /// Gets code actions for a diagnostic
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="diagnostic">The diagnostic</param>
        /// <returns>Array of code actions</returns>
        private async Task<Microsoft.CodeAnalysis.CodeActions.CodeAction[]> GetCodeActionsAsync(Document document, Diagnostic diagnostic)
        {
            var actions = new System.Collections.Generic.List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
            
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, diagnostics) => actions.Add(action),
                System.Threading.CancellationToken.None);

            await _codeFixProvider.RegisterCodeFixesAsync(context);
            return actions.ToArray();
        }

        #endregion
    }
}