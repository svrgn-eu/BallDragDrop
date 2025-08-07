using BallDragDrop.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Basic tests for ClassFileOrganizationCodeFixProvider to verify it compiles and has correct properties
    /// </summary>
    [TestClass]
    public class ClassFileOrganizationCodeFixProviderBasicTests
    {
        #region Properties

        private ClassFileOrganizationCodeFixProvider _codeFixProvider;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._codeFixProvider = new ClassFileOrganizationCodeFixProvider();
        }

        #endregion Construction

        #region Methods

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

        [TestMethod]
        public void CodeFixProvider_ShouldImplementCorrectInterface()
        {
            // Assert
            Assert.IsInstanceOfType(this._codeFixProvider, typeof(CodeFixProvider), 
                "Should implement CodeFixProvider interface");
        }

        [TestMethod]
        public void CodeFixProvider_ShouldHaveCorrectExportAttribute()
        {
            // Act
            var type = typeof(ClassFileOrganizationCodeFixProvider);
            var exportAttributes = type.GetCustomAttributes(typeof(Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute), false);

            // Assert
            Assert.IsTrue(exportAttributes.Length > 0, "Should have ExportCodeFixProvider attribute");
        }

        #endregion Methods
    }
}