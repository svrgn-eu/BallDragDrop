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
    /// Unit tests for FolderStructureCodeFixProvider
    /// </summary>
    [TestClass]
    public class FolderStructureCodeFixProviderTests
    {
        private FolderStructureAnalyzer _analyzer;
        private FolderStructureCodeFixProvider _codeFixProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _analyzer = new FolderStructureAnalyzer();
            _codeFixProvider = new FolderStructureCodeFixProvider();
        }

        #region Interface Code Fix Tests

        [TestMethod]
        public async Task CodeFix_InterfaceNotInContractsFolder_UpdatesNamespace()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            var expected = @"
namespace BallDragDrop.Models.Contracts
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            await VerifyCodeFixAsync(source, expected, "Models/ITestInterface.cs");
        }

        [TestMethod]
        public async Task CodeFix_InterfaceAlreadyInContractsFolder_NoChange()
        {
            var source = @"
namespace BallDragDrop.Models.Contracts
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            // Should not produce any diagnostics, so no code fix should be offered
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source, "Models/Contracts/ITestInterface.cs");
            Assert.AreEqual(0, diagnostics.Length, "No diagnostics should be reported for interface already in Contracts folder");
        }

        #endregion

        #region Abstract Class Code Fix Tests

        [TestMethod]
        public async Task CodeFix_AbstractClassNotInContractsFolder_UpdatesNamespace()
        {
            var source = @"
namespace BallDragDrop.Models
{
    public abstract class BaseTestClass
    {
        public abstract void TestMethod();
    }
}";

            var expected = @"
namespace BallDragDrop.Models.Contracts
{
    public abstract class BaseTestClass
    {
        public abstract void TestMethod();
    }
}";

            await VerifyCodeFixAsync(source, expected, "Models/BaseTestClass.cs");
        }

        #endregion

        #region Bootstrapper Code Fix Tests

        [TestMethod]
        public async Task CodeFix_BootstrapperNotInBootstrapperFolder_UpdatesNamespace()
        {
            var source = @"
namespace BallDragDrop.Services
{
    public class ServiceBootstrapper
    {
        public void Initialize()
        {
        }
    }
}";

            var expected = @"
namespace BallDragDrop.Services.Bootstrapper
{
    public class ServiceBootstrapper
    {
        public void Initialize()
        {
        }
    }";

            await VerifyCodeFixAsync(source, expected, "Services/ServiceBootstrapper.cs");
        }

        [TestMethod]
        public async Task CodeFix_BootstrapperAlreadyInBootstrapperFolder_NoChange()
        {
            var source = @"
namespace BallDragDrop.Services.Bootstrapper
{
    public class ServiceBootstrapper
    {
        public void Initialize()
        {
        }
    }
}";

            // Should not produce any diagnostics, so no code fix should be offered
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source, "Services/Bootstrapper/ServiceBootstrapper.cs");
            Assert.AreEqual(0, diagnostics.Length, "No diagnostics should be reported for bootstrapper already in Bootstrapper folder");
        }

        #endregion

        #region Code Fix Provider Properties Tests

        [TestMethod]
        public void FixableDiagnosticIds_ContainsExpectedIds()
        {
            var fixableIds = _codeFixProvider.FixableDiagnosticIds;
            
            Assert.IsTrue(fixableIds.Contains("BDD3001"), "Should fix InterfaceNotInContractsFolder");
            Assert.IsTrue(fixableIds.Contains("BDD3002"), "Should fix AbstractClassNotInContractsFolder");
            Assert.IsTrue(fixableIds.Contains("BDD3003"), "Should fix BootstrapperNotInBootstrapperFolder");
        }

        [TestMethod]
        public void GetFixAllProvider_ReturnsValidProvider()
        {
            var fixAllProvider = _codeFixProvider.GetFixAllProvider();
            Assert.IsNotNull(fixAllProvider, "Fix all provider should not be null");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies that a code fix produces the expected result
        /// </summary>
        /// <param name="source">The original source code</param>
        /// <param name="expected">The expected fixed source code</param>
        /// <param name="filePath">The file path for the source</param>
        private async Task VerifyCodeFixAsync(string source, string expected, string filePath)
        {
            // Get diagnostics from analyzer
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source, filePath);
            Assert.IsTrue(diagnostics.Length > 0, "Expected at least one diagnostic to be reported");

            // Create compilation and document
            var compilation = AnalyzerTestHelper.CreateCompilation(source, filePath);
            var document = CreateDocument(compilation, source, filePath);

            // Apply code fix
            var actions = await GetCodeActionsAsync(document, diagnostics[0]);
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
        /// <param name="filePath">The file path</param>
        /// <returns>A document</returns>
        private static Document CreateDocument(Compilation compilation, string source, string filePath)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new Microsoft.CodeAnalysis.AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument(documentId, filePath, source);

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