using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Performance and scalability tests for coding standards analyzers
    /// Tests analyzer behavior with large codebases and complex scenarios
    /// </summary>
    [TestClass]
    public class AnalyzerPerformanceTests
    {
        #region Performance Benchmarks

        /// <summary>
        /// Tests folder structure analyzer performance with large number of files
        /// </summary>
        [TestMethod]
        public void FolderStructureAnalyzer_LargeCodebase_ShouldMaintainPerformance()
        {
            // Arrange - Generate large source with many violations
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            
            // Generate 200 interfaces and abstract classes (all violations)
            for (int i = 0; i < 100; i++)
            {
                sourceBuilder.AppendLine($"    public interface ITestInterface{i}");
                sourceBuilder.AppendLine("    {");
                sourceBuilder.AppendLine("        void TestMethod();");
                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine();
                
                sourceBuilder.AppendLine($"    public abstract class TestAbstractClass{i}");
                sourceBuilder.AppendLine("    {");
                sourceBuilder.AppendLine("        public abstract void TestMethod();");
                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("}");
            var largeSource = sourceBuilder.ToString();

            // Act - Measure analysis time
            var analyzer = new FolderStructureAnalyzer();
            var stopwatch = Stopwatch.StartNew();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, largeSource, 
                @"C:\Project\BallDragDrop\Services\LargeFile.cs");
            
            stopwatch.Stop();

            // Assert - Performance requirements
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
                $"Folder structure analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
            
            Assert.AreEqual(200, diagnostics.Length, "Should detect all 200 violations");
            
            // Verify diagnostic types
            var interfaceViolations = diagnostics.Count(d => d.Id == "BDD3001");
            var abstractClassViolations = diagnostics.Count(d => d.Id == "BDD3002");
            
            Assert.AreEqual(100, interfaceViolations, "Should detect 100 interface violations");
            Assert.AreEqual(100, abstractClassViolations, "Should detect 100 abstract class violations");
        }

        /// <summary>
        /// Tests method region analyzer performance with many methods
        /// </summary>
        [TestMethod]
        public void MethodRegionAnalyzer_ManyMethods_ShouldMaintainPerformance()
        {
            // Arrange - Generate source with many methods
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    public class LargeClass");
            sourceBuilder.AppendLine("    {");
            
            // Generate 500 methods without regions
            for (int i = 0; i < 500; i++)
            {
                sourceBuilder.AppendLine($"        public void TestMethod{i}()");
                sourceBuilder.AppendLine("        {");
                sourceBuilder.AppendLine("            // Method implementation");
                sourceBuilder.AppendLine("        }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // Act - Measure analysis time
            var analyzer = new MethodRegionAnalyzer();
            var stopwatch = Stopwatch.StartNew();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, sourceBuilder.ToString());
            
            stopwatch.Stop();

            // Assert - Performance requirements
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
                $"Method region analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");
            
            Assert.AreEqual(500, diagnostics.Length, "Should detect all 500 method violations");
            Assert.IsTrue(diagnostics.All(d => d.Id == "BDD4001"), "All violations should be missing region violations");
        }

        /// <summary>
        /// Tests XML documentation analyzer performance with complex methods
        /// </summary>
        [TestMethod]
        public void XmlDocumentationAnalyzer_ComplexMethods_ShouldMaintainPerformance()
        {
            // Arrange - Generate source with complex methods
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    public class ComplexClass");
            sourceBuilder.AppendLine("    {");
            
            // Generate 100 complex methods with various signatures
            for (int i = 0; i < 100; i++)
            {
                sourceBuilder.AppendLine($"        public async Task<Dictionary<string, List<int>>> ComplexMethod{i}<T>(");
                sourceBuilder.AppendLine($"            T parameter1,");
                sourceBuilder.AppendLine($"            string parameter2 = \"default\",");
                sourceBuilder.AppendLine($"            params object[] parameters) where T : class");
                sourceBuilder.AppendLine("        {");
                sourceBuilder.AppendLine("            if (parameter1 == null)");
                sourceBuilder.AppendLine("                throw new ArgumentNullException(nameof(parameter1));");
                sourceBuilder.AppendLine("            if (parameter2 == null)");
                sourceBuilder.AppendLine("                throw new ArgumentException(\"Invalid parameter\");");
                sourceBuilder.AppendLine("            ");
                sourceBuilder.AppendLine("            var result = new Dictionary<string, List<int>>();");
                sourceBuilder.AppendLine("            return await Task.FromResult(result);");
                sourceBuilder.AppendLine("        }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // Act - Measure analysis time
            var analyzer = new XmlDocumentationAnalyzer();
            var stopwatch = Stopwatch.StartNew();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, sourceBuilder.ToString());
            
            stopwatch.Stop();

            // Assert - Performance requirements
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
                $"XML documentation analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
            
            // Should detect missing documentation and exception documentation
            Assert.IsTrue(diagnostics.Length >= 100, "Should detect at least 100 violations");
            
            var missingDocViolations = diagnostics.Count(d => d.Id == "BDD5001");
            var missingExceptionViolations = diagnostics.Count(d => d.Id == "BDD5003");
            
            Assert.AreEqual(100, missingDocViolations, "Should detect 100 missing documentation violations");
            Assert.AreEqual(100, missingExceptionViolations, "Should detect 100 missing exception documentation violations");
        }

        #endregion

        #region Memory Usage Tests

        /// <summary>
        /// Tests memory usage during analysis of large files
        /// </summary>
        [TestMethod]
        public void AnalyzerMemoryUsage_LargeFiles_ShouldBeReasonable()
        {
            // Arrange - Force garbage collection to get baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);
            
            // Generate very large source file
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            
            for (int i = 0; i < 1000; i++)
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

            // Act - Run all analyzers
            var folderAnalyzer = new FolderStructureAnalyzer();
            var regionAnalyzer = new MethodRegionAnalyzer();
            var docAnalyzer = new XmlDocumentationAnalyzer();

            var folderDiagnostics = AnalyzerTestHelper.GetDiagnostics(folderAnalyzer, sourceBuilder.ToString());
            var regionDiagnostics = AnalyzerTestHelper.GetDiagnostics(regionAnalyzer, sourceBuilder.ToString());
            var docDiagnostics = AnalyzerTestHelper.GetDiagnostics(docAnalyzer, sourceBuilder.ToString());

            // Force garbage collection and measure memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // Assert - Memory usage should be reasonable (< 100MB for this test)
            Assert.IsTrue(memoryUsed < 100 * 1024 * 1024, 
                $"Memory usage was {memoryUsed / (1024 * 1024)}MB, expected < 100MB");
            
            // Verify analysis completed successfully
            Assert.AreEqual(5000, regionDiagnostics.Length, "Should detect 5000 region violations");
            Assert.AreEqual(5000, docDiagnostics.Length, "Should detect 5000 documentation violations");
        }

        /// <summary>
        /// Tests memory cleanup after analysis
        /// </summary>
        [TestMethod]
        public void AnalyzerMemoryCleanup_AfterAnalysis_ShouldReleaseMemory()
        {
            // Arrange - Get baseline memory
            GC.Collect();
            var baselineMemory = GC.GetTotalMemory(true);
            
            // Act - Run analysis multiple times
            for (int iteration = 0; iteration < 10; iteration++)
            {
                var source = GenerateMediumSizeSource(100); // 100 classes
                
                var analyzer = new MethodRegionAnalyzer();
                var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
                
                // Verify analysis worked
                Assert.IsTrue(diagnostics.Length > 0, $"Iteration {iteration} should produce diagnostics");
            }
            
            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - baselineMemory;

            // Assert - Memory growth should be minimal (< 20MB)
            Assert.IsTrue(memoryGrowth < 20 * 1024 * 1024, 
                $"Memory growth was {memoryGrowth / (1024 * 1024)}MB, expected < 20MB");
        }

        #endregion

        #region Concurrent Analysis Tests

        /// <summary>
        /// Tests analyzer behavior under concurrent analysis
        /// </summary>
        [TestMethod]
        public async Task ConcurrentAnalysis_MultipleFiles_ShouldHandleCorrectly()
        {
            // Arrange - Generate multiple source files
            var sources = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                sources.Add(GenerateMediumSizeSource(50, $"TestNamespace{i}"));
            }

            // Act - Run analysis concurrently
            var analyzer = new MethodRegionAnalyzer();
            var tasks = sources.Select(async (source, index) =>
            {
                return await Task.Run(() => 
                {
                    var stopwatch = Stopwatch.StartNew();
                    var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, source);
                    stopwatch.Stop();
                    
                    return new { Index = index, Diagnostics = diagnostics, Time = stopwatch.ElapsedMilliseconds };
                });
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert - All analyses should complete successfully
            Assert.AreEqual(10, results.Length, "All concurrent analyses should complete");
            
            foreach (var result in results)
            {
                Assert.IsTrue(result.Diagnostics.Length > 0, $"Analysis {result.Index} should produce diagnostics");
                Assert.IsTrue(result.Time < 5000, $"Analysis {result.Index} took {result.Time}ms, expected < 5000ms");
            }
            
            // Verify total diagnostics count is reasonable
            var totalDiagnostics = results.Sum(r => r.Diagnostics.Length);
            Assert.AreEqual(500, totalDiagnostics, "Should detect 500 total violations (50 per file × 10 files)");
        }

        #endregion

        #region Stress Tests

        /// <summary>
        /// Tests analyzer behavior with deeply nested code structures
        /// </summary>
        [TestMethod]
        public void StressTest_DeeplyNestedStructures_ShouldHandleCorrectly()
        {
            // Arrange - Generate deeply nested source
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            
            // Create nested classes and methods
            for (int depth = 0; depth < 10; depth++)
            {
                var indent = new string(' ', (depth + 1) * 4);
                sourceBuilder.AppendLine($"{indent}public class NestedClass{depth}");
                sourceBuilder.AppendLine($"{indent}{{");
                
                // Add methods at each nesting level
                for (int method = 0; method < 5; method++)
                {
                    sourceBuilder.AppendLine($"{indent}    public void Method{method}()");
                    sourceBuilder.AppendLine($"{indent}    {{");
                    sourceBuilder.AppendLine($"{indent}        // Implementation at depth {depth}");
                    sourceBuilder.AppendLine($"{indent}    }}");
                }
            }
            
            // Close all nested classes
            for (int depth = 9; depth >= 0; depth--)
            {
                var indent = new string(' ', (depth + 1) * 4);
                sourceBuilder.AppendLine($"{indent}}}");
            }
            
            sourceBuilder.AppendLine("}");

            // Act - Run analysis
            var analyzer = new MethodRegionAnalyzer();
            var stopwatch = Stopwatch.StartNew();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, sourceBuilder.ToString());
            
            stopwatch.Stop();

            // Assert - Should handle nested structures correctly
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
                $"Nested structure analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");
            
            Assert.AreEqual(50, diagnostics.Length, "Should detect 50 method violations (5 methods × 10 classes)");
        }

        /// <summary>
        /// Tests analyzer with very long method names and complex signatures
        /// </summary>
        [TestMethod]
        public void StressTest_ComplexSignatures_ShouldHandleCorrectly()
        {
            // Arrange - Generate source with complex method signatures
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace BallDragDrop.Services");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    public class ComplexSignatureClass");
            sourceBuilder.AppendLine("    {");
            
            // Generate methods with increasingly complex signatures
            for (int i = 0; i < 50; i++)
            {
                var methodName = $"VeryLongMethodNameThatTestsAnalyzerPerformanceWithComplexSignature{i}";
                var genericConstraints = i % 3 == 0 ? " where T : class, new()" : "";
                var asyncModifier = i % 2 == 0 ? "async " : "";
                var returnType = i % 2 == 0 ? "Task<Dictionary<string, List<Tuple<int, string>>>>" : "Dictionary<string, List<int>>";
                
                sourceBuilder.AppendLine($"        public {asyncModifier}{returnType} {methodName}<T>(");
                sourceBuilder.AppendLine($"            T parameter1,");
                sourceBuilder.AppendLine($"            Dictionary<string, List<int>> parameter2,");
                sourceBuilder.AppendLine($"            Func<T, Task<string>> parameter3,");
                sourceBuilder.AppendLine($"            Action<Exception> onError = null,");
                sourceBuilder.AppendLine($"            params object[] additionalParameters){genericConstraints}");
                sourceBuilder.AppendLine("        {");
                
                if (i % 2 == 0)
                {
                    sourceBuilder.AppendLine("            return await Task.FromResult(new Dictionary<string, List<Tuple<int, string>>>());");
                }
                else
                {
                    sourceBuilder.AppendLine("            return new Dictionary<string, List<int>>();");
                }
                
                sourceBuilder.AppendLine("        }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // Act - Run analysis
            var analyzer = new XmlDocumentationAnalyzer();
            var stopwatch = Stopwatch.StartNew();
            
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(analyzer, sourceBuilder.ToString());
            
            stopwatch.Stop();

            // Assert - Should handle complex signatures correctly
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 4000, 
                $"Complex signature analysis took {stopwatch.ElapsedMilliseconds}ms, expected < 4000ms");
            
            Assert.AreEqual(50, diagnostics.Length, "Should detect 50 missing documentation violations");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a medium-sized source file for testing
        /// </summary>
        private string GenerateMediumSizeSource(int classCount, string namespaceName = "BallDragDrop.Services")
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");
            
            for (int i = 0; i < classCount; i++)
            {
                sourceBuilder.AppendLine($"    public class TestClass{i}");
                sourceBuilder.AppendLine("    {");
                sourceBuilder.AppendLine($"        public void TestMethod()");
                sourceBuilder.AppendLine("        {");
                sourceBuilder.AppendLine("            // Method implementation");
                sourceBuilder.AppendLine("        }");
                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine();
            }
            
            sourceBuilder.AppendLine("}");
            return sourceBuilder.ToString();
        }

        #endregion
    }
}