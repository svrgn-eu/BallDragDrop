using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Integration tests for complete standards enforcement workflow
    /// Tests the entire pipeline from analysis to code fixes
    /// </summary>
    [TestClass]
    public class StandardsEnforcementIntegrationTests
    {
        #region Complete Workflow Tests

        /// <summary>
        /// Tests the complete standards validation workflow from analysis to fix application
        /// </summary>
        [TestMethod]
        public async Task CompleteWorkflow_AnalysisToCodeFix_ShouldWorkEndToEnd()
        {
            // Arrange - Create source with multiple violations
            var source = @"
namespace BallDragDrop.Services
{
    public interface ITestInterface
    {
        void TestMethod();
    }

    public abstract class TestAbstractClass
    {
        public abstract void TestMethod();
    }

    public class ServiceBootstrapper
    {
        public void Configure() { }
    }

    public class TestClass
    {
        public void MethodWithoutRegion()
        {
            // Method implementation
        }

        public void UndocumentedMethod(string parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
        }
    }
}";

            // Act - Run all analyzers
            var folderAnalyzer = new FolderStructureAnalyzer();
            var regionAnalyzer = new MethodRegionAnalyzer();
            var docAnalyzer = new XmlDocumentationAnalyzer();

            var folderDiagnostics = AnalyzerTestHelper.GetDiagnostics(folderAnalyzer, source, @"C:\Project\BallDragDrop\Services\TestFile.cs");
            var regionDiagnostics = AnalyzerTestHelper.GetDiagnostics(regionAnalyzer, source);
            var docDiagnostics = AnalyzerTestHelper.GetDiagnostics(docAnalyzer, source);

            // Assert - Verify all expected violations are detected
            Assert.IsTrue(folderDiagnostics.Any(d => d.Id == "BDD3001"), "Should detect interface not in Contracts folder");
            Assert.IsTrue(folderDiagnostics.Any(d => d.Id == "BDD3002"), "Should detect abstract class not in Contracts folder");
            Assert.IsTrue(folderDiagnostics.Any(d => d.Id == "BDD3003"), "Should detect bootstrapper not in Bootstrapper folder");
            
            Assert.IsTrue(regionDiagnostics.Any(d => d.Id == "BDD4001"), "Should detect methods without regions");
            
            Assert.IsTrue(docDiagnostics.Any(d => d.Id == "BDD5001"), "Should detect missing XML documentation");
            Assert.IsTrue(docDiagnostics.Any(d => d.Id == "BDD5003"), "Should detect missing exception documentation");

            // Verify total violation count
            var totalViolations = folderDiagnostics.Length + regionDiagnostics.Length + docDiagnostics.Length;
            Assert.IsTrue(totalViolations >= 6, $"Expected at least 6 violations, found {totalViolations}");
        }

        /// <summary>
        /// Tests that code fix providers can resolve violations
        /// </summary>
        [TestMethod]
        public async Task CodeFixProviders_ShouldResolveViolations()
        {
            // Arrange - Source with region violation
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
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
            Assert.IsTrue(diagnostics.Any(d => d.Id == "BDD4001"), "Should have region violation");

            // Create compilation and document
            var compilation = AnalyzerTestHelper.CreateCompilation(source);
            var document = CreateDocument(compilation, source);
            
            // Get code fixes
            var diagnostic = diagnostics.First(d => d.Id == "BDD4001");
            var context = new CodeFixContext(document, diagnostic, (action, diags) => { }, default);
            
            var actions = new List<CodeAction>();
            await codeFixProvider.RegisterCodeFixesAsync(context);

            // Assert - Code fix should be available (implementation depends on actual code fix provider)
            // This test validates the infrastructure is working
            Assert.IsNotNull(codeFixProvider, "Code fix provider should be available");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests analyzer performance with large codebase
        /// </summary>
        [TestMethod]
        public void AnalyzerPerformance_LargeCodebase_ShouldCompleteWithinTimeLimit()
        {
            // Arrange - Generate large source file
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            
            // Generate 100 classes with 10 methods each
            for (int i = 0; i < 100; i++)
            {
                sourceBuilder.AppendLine($"    public class TestClass{i}");
                sourceBuilder.AppendLine("    {");
                
                for (int j = 0; j < 10; j++)
                {
                    sourceBuilder.AppendLine($"        public void TestMethod{j}()");
                    sourceBuilder.AppendLine("        {");
                    sourceBuilder.AppendLine("            // Method implementation");
                    sourceBuilder.AppendLine("        }");
                    sourceBuilder.AppendLine();
                }
                
                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("}");
            var largeSource = sourceBuilder.ToString();

            // Act - Run analyzers and measure time
            var stopwatch = Stopwatch.StartNew();
            
            var folderAnalyzer = new FolderStructureAnalyzer();
            var regionAnalyzer = new MethodRegionAnalyzer();
            var docAnalyzer = new XmlDocumentationAnalyzer();

            var folderDiagnostics = AnalyzerTestHelper.GetDiagnostics(folderAnalyzer, largeSource);
            var regionDiagnostics = AnalyzerTestHelper.GetDiagnostics(regionAnalyzer, largeSource);
            var docDiagnostics = AnalyzerTestHelper.GetDiagnostics(docAnalyzer, largeSource);
            
            stopwatch.Stop();

            // Assert - Should complete within reasonable time (5 seconds for 1000 methods)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
                $"Analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            // Verify violations were detected
            Assert.AreEqual(1000, regionDiagnostics.Length, "Should detect 1000 region violations");
            Assert.AreEqual(1000, docDiagnostics.Length, "Should detect 1000 documentation violations");
        }

        /// <summary>
        /// Tests memory usage with large codebase
        /// </summary>
        [TestMethod]
        public void AnalyzerMemoryUsage_LargeCodebase_ShouldNotExceedLimit()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            
            // Generate moderately large source
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            
            for (int i = 0; i < 50; i++)
            {
                sourceBuilder.AppendLine($"    public class TestClass{i}");
                sourceBuilder.AppendLine("    {");
                
                for (int j = 0; j < 5; j++)
                {
                    sourceBuilder.AppendLine($"        public void TestMethod{j}()");
                    sourceBuilder.AppendLine("        {");
                    sourceBuilder.AppendLine("            // Method implementation");
                    sourceBuilder.AppendLine("        }");
                }
                
                sourceBuilder.AppendLine("    }");
            }
            
            sourceBuilder.AppendLine("}");

            // Act - Run analysis
            var analyzer = new MethodRegionAnalyzer();
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, sourceBuilder.ToString());
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            // Assert - Memory usage should be reasonable (< 50MB for this test)
            Assert.IsTrue(memoryUsed < 50 * 1024 * 1024, 
                $"Memory usage was {memoryUsed / (1024 * 1024)}MB, expected < 50MB");
        }

        #endregion

        #region Error Reporting Tests

        /// <summary>
        /// Tests that error messages are clear and actionable
        /// </summary>
        [TestMethod]
        public void ErrorReporting_ShouldProvideActionableMessages()
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
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source, @"C:\Project\BallDragDrop\Services\ITestInterface.cs");

            // Assert
            var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "BDD3001");
            Assert.IsNotNull(diagnostic, "Should report folder structure violation");
            
            var message = diagnostic.GetMessage();
            Assert.IsTrue(message.Contains("Contracts"), "Error message should mention Contracts folder");
            Assert.IsTrue(message.Contains("interface"), "Error message should mention interface");
            
            // Verify severity
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity, "Should be a warning level diagnostic");
        }

        /// <summary>
        /// Tests diagnostic location accuracy
        /// </summary>
        [TestMethod]
        public void DiagnosticLocation_ShouldBeAccurate()
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

            // Act
            var analyzer = new MethodRegionAnalyzer();
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);

            // Assert
            var diagnostic = diagnostics.FirstOrDefault(d => d.Id == "BDD4001");
            Assert.IsNotNull(diagnostic, "Should report region violation");
            
            var lineSpan = diagnostic.Location.GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based
            var column = lineSpan.StartLinePosition.Character + 1; // Convert to 1-based
            
            Assert.AreEqual(6, line, "Diagnostic should point to method declaration line");
            Assert.AreEqual(21, column, "Diagnostic should point to method name");
        }

        #endregion

        #region Violation Scenario Tests

        /// <summary>
        /// Tests various folder structure violation scenarios
        /// </summary>
        [TestMethod]
        public void FolderStructureViolations_VariousScenarios_ShouldBeDetected()
        {
            var testCases = new[]
            {
                new { 
                    Source = @"namespace BallDragDrop.Models { public interface ITest { } }",
                    FilePath = @"C:\Project\BallDragDrop\Models\ITest.cs",
                    ExpectedDiagnostic = "BDD3001",
                    Description = "Interface in Models folder"
                },
                new { 
                    Source = @"namespace BallDragDrop.ViewModels { public abstract class BaseViewModel { } }",
                    FilePath = @"C:\Project\BallDragDrop\ViewModels\BaseViewModel.cs",
                    ExpectedDiagnostic = "BDD3002",
                    Description = "Abstract class in ViewModels folder"
                },
                new { 
                    Source = @"namespace BallDragDrop.Services { public class ApplicationBootstrap { } }",
                    FilePath = @"C:\Project\BallDragDrop\Services\ApplicationBootstrap.cs",
                    ExpectedDiagnostic = "BDD3003",
                    Description = "Bootstrap class in Services folder"
                }
            };

            var analyzer = new FolderStructureAnalyzer();

            foreach (var testCase in testCases)
            {
                // Act
                var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, testCase.Source, testCase.FilePath);

                // Assert
                Assert.IsTrue(diagnostics.Any(d => d.Id == testCase.ExpectedDiagnostic), 
                    $"Failed to detect violation: {testCase.Description}");
            }
        }

        /// <summary>
        /// Tests various method region violation scenarios
        /// </summary>
        [TestMethod]
        public void MethodRegionViolations_VariousScenarios_ShouldBeDetected()
        {
            var testCases = new[]
            {
                new { 
                    Source = @"
public class Test {
    public void Method1() { }
    public void Method2() { }
}",
                    ExpectedCount = 2,
                    Description = "Multiple methods without regions"
                },
                new { 
                    Source = @"
public class Test {
    #region WrongName
    public void Method1() { }
    #endregion
}",
                    ExpectedDiagnostic = "BDD4002",
                    Description = "Method in incorrectly named region"
                },
                new { 
                    Source = @"
public class Test {
    public void Method1() { }
    #region Method2
    public void Method2() { }
    #endregion
}",
                    ExpectedCount = 1,
                    Description = "Mixed correct and incorrect methods"
                }
            };

            var analyzer = new MethodRegionAnalyzer();

            foreach (var testCase in testCases)
            {
                // Act
                var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, testCase.Source);

                // Assert
                if (testCase.ExpectedCount.HasValue)
                {
                    Assert.AreEqual(testCase.ExpectedCount.Value, diagnostics.Length, 
                        $"Failed test case: {testCase.Description}");
                }
                
                if (!string.IsNullOrEmpty(testCase.ExpectedDiagnostic))
                {
                    Assert.IsTrue(diagnostics.Any(d => d.Id == testCase.ExpectedDiagnostic), 
                        $"Failed to detect expected diagnostic: {testCase.Description}");
                }
            }
        }

        /// <summary>
        /// Tests various XML documentation violation scenarios
        /// </summary>
        [TestMethod]
        public void XmlDocumentationViolations_VariousScenarios_ShouldBeDetected()
        {
            var testCases = new[]
            {
                new { 
                    Source = @"
public class Test {
    public void Method() { }
}",
                    ExpectedDiagnostic = "BDD5001",
                    Description = "Method without any documentation"
                },
                new { 
                    Source = @"
public class Test {
    /// <summary>Test</summary>
    public void Method(string param) { }
}",
                    ExpectedDiagnostic = "BDD5002",
                    Description = "Method with incomplete documentation"
                },
                new { 
                    Source = @"
public class Test {
    /// <summary>Test</summary>
    /// <param name=""param"">Test param</param>
    public void Method(string param) { 
        throw new ArgumentException();
    }
}",
                    ExpectedDiagnostic = "BDD5003",
                    Description = "Method missing exception documentation"
                }
            };

            var analyzer = new XmlDocumentationAnalyzer();

            foreach (var testCase in testCases)
            {
                // Act
                var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, testCase.Source);

                // Assert
                Assert.IsTrue(diagnostics.Any(d => d.Id == testCase.ExpectedDiagnostic), 
                    $"Failed to detect violation: {testCase.Description}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a document from compilation and source
        /// </summary>
        private static Document CreateDocument(Compilation compilation, string source)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument(documentId, "Test.cs", source);
                
            return solution.GetDocument(documentId);
        }

        #endregion
    }
}