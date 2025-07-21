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
    /// Unit tests for MethodRegionCodeFixProvider
    /// </summary>
    [TestClass]
    public class MethodRegionCodeFixProviderTests
    {
        private MethodRegionAnalyzer _analyzer;
        private MethodRegionCodeFixProvider _codeFixProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _analyzer = new MethodRegionAnalyzer();
            _codeFixProvider = new MethodRegionCodeFixProvider();
        }

        #region Add Region Code Fix Tests

        [TestMethod]
        public async Task CodeFix_MethodNotInRegion_AddsRegion()
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
#region TestMethod
        public void TestMethod()
        {
            // Method implementation
        }
#endregion TestMethod
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4001");
        }

        [TestMethod]
        public async Task CodeFix_MultipleMethodsNotInRegion_AddsRegionToSpecificMethod()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        public void FirstMethod()
        {
            // First method
        }

        public void SecondMethod()
        {
            // Second method
        }
    }
}";

            // Should add region only to the first method when fixing the first diagnostic
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source);
            Assert.AreEqual(2, diagnostics.Length, "Should report two diagnostics for two methods without regions");

            // Test fixing the first method
            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region FirstMethod
        public void FirstMethod()
        {
            // First method
        }
#endregion FirstMethod

        public void SecondMethod()
        {
            // Second method
        }
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4001", diagnostics[0]);
        }

        #endregion

        #region Fix Region Name Code Fix Tests

        [TestMethod]
        public async Task CodeFix_IncorrectRegionName_FixesRegionName()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region WrongName
        public void TestMethod()
        {
            // Method implementation
        }
#endregion WrongName
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region TestMethod
        public void TestMethod()
        {
            // Method implementation
        }
#endregion TestMethod
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4002");
        }

        [TestMethod]
        public async Task CodeFix_PartiallyCorrectRegionName_FixesRegionName()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region Test
        public void TestMethod()
        {
            // Method implementation
        }
#endregion Test
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region TestMethod
        public void TestMethod()
        {
            // Method implementation
        }
#endregion TestMethod
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4002");
        }

        #endregion

        #region Code Fix Provider Properties Tests

        [TestMethod]
        public void FixableDiagnosticIds_ContainsExpectedIds()
        {
            var fixableIds = _codeFixProvider.FixableDiagnosticIds;
            
            Assert.IsTrue(fixableIds.Contains("BDD4001"), "Should fix MethodNotInRegion");
            Assert.IsTrue(fixableIds.Contains("BDD4002"), "Should fix IncorrectRegionNaming");
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
        public async Task CodeFix_MethodWithComplexSignature_AddsRegionCorrectly()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        public async Task<string> ComplexMethodName(int parameter1, string parameter2)
        {
            return await Task.FromResult(""test"");
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
#region ComplexMethodName
        public async Task<string> ComplexMethodName(int parameter1, string parameter2)
        {
            return await Task.FromResult(""test"");
        }
#endregion ComplexMethodName
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4001");
        }

        [TestMethod]
        public async Task CodeFix_MethodWithAttributes_PreservesAttributes()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public class TestClass
    {
        [System.Obsolete]
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
#region TestMethod
        [System.Obsolete]
        public void TestMethod()
        {
            // Method implementation
        }
#endregion TestMethod
    }
}";

            await VerifyCodeFixAsync(source, expected, "BDD4001");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that a code fix produces the expected result
        /// </summary>
        /// <param name="source">The original source code</param>
        /// <param name="expected">The expected fixed source code</param>
        /// <param name="expectedDiagnosticId">The expected diagnostic ID</param>
        /// <param name="specificDiagnostic">Specific diagnostic to fix (optional)</param>
        private async Task VerifyCodeFixAsync(string source, string expected, string expectedDiagnosticId, Diagnostic specificDiagnostic = null)
        {
            // Get diagnostics from analyzer
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source);
            Assert.IsTrue(diagnostics.Length > 0, "Expected at least one diagnostic to be reported");

            var diagnostic = specificDiagnostic ?? diagnostics.First(d => d.Id == expectedDiagnosticId);
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

            Assert.AreEqual(normalizedExpected, normalizedActual, "Code fix did not produce expected result");
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