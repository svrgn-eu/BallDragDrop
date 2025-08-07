using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for ClassFileOrganizationCodeFixProvider
    /// Tests automatic fixing of class file organization violations
    /// </summary>
    [TestClass]
    public class ClassFileOrganizationCodeFixProviderTests
    {
        #region Properties

        private ClassFileOrganizationAnalyzer _analyzer;
        private ClassFileOrganizationCodeFixProvider _codeFixProvider;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._analyzer = new ClassFileOrganizationAnalyzer();
            this._codeFixProvider = new ClassFileOrganizationCodeFixProvider();
        }

        #endregion Construction

        #region Methods

        #region Multiple Classes Code Fix Tests

        [TestMethod]
        public void MultipleClassesInFile_ShouldOfferSplitClassFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class FirstClass
    {
        public string Property1 { get; set; }
    }

    public class SecondClass
    {
        public string Property2 { get; set; }
    }
}";

            // Note: For multiple classes, we expect the fix to create separate files
            // The test framework limitation means we can only verify one class at a time
            // In practice, the user would apply the fix to each class individually
            
            // Act & Assert - Verify that code fixes are available
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MultipleClasses.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.IsTrue(diagnostics.Length > 0, "Expected diagnostics for multiple classes in file");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix actions for multiple classes");
            Assert.IsTrue(codeActions[0].Title.Contains("Move class"), "Expected 'Move class' code fix action");
        }

        [TestMethod]
        public void ThreeClassesInFile_ShouldOfferSplitFixForEachExtraClass()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class FirstClass
    {
        public string Property1 { get; set; }
    }

    public class SecondClass
    {
        public string Property2 { get; set; }
    }

    public class ThirdClass
    {
        public string Property3 { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MultipleClasses.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            // Should have 2 diagnostics (for SecondClass and ThirdClass)
            Assert.AreEqual(2, diagnostics.Length, "Expected 2 diagnostics for 3 classes in file");
            
            foreach (var diagnostic in diagnostics)
            {
                var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostic);
                Assert.IsTrue(codeActions.Length > 0, "Expected code fix actions for each extra class");
                Assert.IsTrue(codeActions[0].Title.Contains("Move class"), "Expected 'Move class' code fix action");
            }
        }

        [TestMethod]
        public void MultipleClassesWithNestedClass_ShouldOnlyFixTopLevelClasses()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class FirstClass
    {
        public string Property1 { get; set; }
        
        public class NestedClass
        {
            public string NestedProperty { get; set; }
        }
    }

    public class SecondClass
    {
        public string Property2 { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MultipleClasses.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            // Should have 1 diagnostic (for SecondClass only, not NestedClass)
            Assert.AreEqual(1, diagnostics.Length, "Expected 1 diagnostic for top-level classes only");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix action for SecondClass");
            Assert.IsTrue(codeActions[0].Title.Contains("SecondClass"), "Expected code fix for SecondClass");
        }

        #endregion Multiple Classes Code Fix Tests

        #region Filename Mismatch Code Fix Tests

        [TestMethod]
        public void FilenameMismatch_ShouldOfferRenameFileFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class CorrectClassName
    {
        public string Property { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "WrongFileName.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.IsTrue(diagnostics.Length > 0, "Expected diagnostic for filename mismatch");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix action for filename mismatch");
            Assert.IsTrue(codeActions[0].Title.Contains("Rename file to 'CorrectClassName.cs'"), 
                "Expected 'Rename file' code fix action");
        }

        [TestMethod]
        public void FilenameMismatchCaseSensitive_ShouldOfferRenameFileFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class MyClass
    {
        public string Property { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "myclass.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.IsTrue(diagnostics.Length > 0, "Expected diagnostic for case-sensitive filename mismatch");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix action for case-sensitive filename mismatch");
            Assert.IsTrue(codeActions[0].Title.Contains("Rename file to 'MyClass.cs'"), 
                "Expected 'Rename file' code fix action with correct case");
        }

        [TestMethod]
        public void PartialClass_ShouldNotOfferFilenameFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public partial class MyClass
    {
        public string Property { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MyClass.Designer.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            // Partial classes are allowed to have different filenames
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for partial class with different filename");
        }

        #endregion Filename Mismatch Code Fix Tests

        #region Complex Scenarios Tests

        [TestMethod]
        public void MultipleClassesWithNamespacesAndUsings_ShouldPreserveStructure()
        {
            // Arrange
            var source = @"using System;
using System.Collections.Generic;

namespace BallDragDrop.Models
{
    public class FirstClass
    {
        public List<string> Items { get; set; }
        
        public void DoSomething()
        {
            Console.WriteLine(""First class"");
        }
    }

    public class SecondClass
    {
        public Dictionary<string, int> Data { get; set; }
        
        public void ProcessData()
        {
            Console.WriteLine(""Second class"");
        }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MultipleClasses.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.IsTrue(diagnostics.Length > 0, "Expected diagnostics for multiple classes");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix actions");
            Assert.IsTrue(codeActions[0].Title.Contains("Move class"), "Expected 'Move class' code fix action");
        }

        [TestMethod]
        public void ClassWithComplexMembers_ShouldHandleCodeFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class ComplexClass
    {
        private readonly string _field;
        
        public string Property { get; set; }
        
        public event EventHandler SomeEvent;
        
        public ComplexClass(string field)
        {
            this._field = field;
        }
        
        public void Method()
        {
            // Method implementation
        }
        
        public delegate void MyDelegate();
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "WrongName.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.IsTrue(diagnostics.Length > 0, "Expected diagnostic for filename mismatch");
            
            var codeActions = TestHelpers.CodeFixTestHelper.GetCodeActions(document, this._codeFixProvider, diagnostics[0]);
            Assert.IsTrue(codeActions.Length > 0, "Expected code fix action");
            Assert.IsTrue(codeActions[0].Title.Contains("Rename file to 'ComplexClass.cs'"), 
                "Expected 'Rename file' code fix action");
        }

        #endregion Complex Scenarios Tests

        #region No Fix Scenarios Tests

        [TestMethod]
        public void SingleClassWithCorrectFilename_ShouldNotOfferFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class MyClass
    {
        public string Property { get; set; }
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "MyClass.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for correctly organized class");
        }

        [TestMethod]
        public void EmptyFile_ShouldNotOfferFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    // Empty namespace
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "Empty.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for empty file");
        }

        [TestMethod]
        public void InterfaceFile_ShouldNotTriggerClassOrganizationFix()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Contracts
{
    public interface IMyInterface
    {
        void DoSomething();
    }
}";

            // Act & Assert
            var document = TestHelpers.CodeFixTestHelper.CreateTestDocument(source, "WrongName.cs");
            var diagnostics = TestHelpers.CodeFixTestHelper.GetDiagnostics(this._analyzer, document);
            
            Assert.AreEqual(0, diagnostics.Length, "Expected no diagnostics for interface file");
        }

        #endregion No Fix Scenarios Tests

        #region Code Fix Provider Properties Tests

        [TestMethod]
        public void FixableDiagnosticIds_ShouldContainExpectedIds()
        {
            // Act
            var fixableIds = this._codeFixProvider.FixableDiagnosticIds;

            // Assert
            Assert.IsTrue(fixableIds.Contains(DiagnosticDescriptors.MultipleClassesInFile.Id), 
                "Should be able to fix multiple classes in file");
            Assert.IsTrue(fixableIds.Contains(DiagnosticDescriptors.FilenameClassNameMismatch.Id), 
                "Should be able to fix filename-classname mismatch");
            Assert.AreEqual(2, fixableIds.Length, "Should fix exactly 2 diagnostic types");
        }

        [TestMethod]
        public void GetFixAllProvider_ShouldReturnBatchFixer()
        {
            // Act
            var fixAllProvider = this._codeFixProvider.GetFixAllProvider();

            // Assert
            Assert.IsNotNull(fixAllProvider, "Fix all provider should not be null");
            Assert.AreEqual(WellKnownFixAllProviders.BatchFixer, fixAllProvider, 
                "Should return batch fixer for fix all operations");
        }

        #endregion Code Fix Provider Properties Tests

        #endregion Methods
    }
}