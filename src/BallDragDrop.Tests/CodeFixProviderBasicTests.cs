using Microsoft.VisualStudio.TestTools.UnitTesting;
using BallDragDrop.CodeAnalysis;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Basic tests for code fix providers to verify they can be instantiated
    /// </summary>
    [TestClass]
    public class CodeFixProviderBasicTests
    {
        [TestMethod]
        public void FolderStructureCodeFixProvider_CanBeInstantiated()
        {
            var provider = new FolderStructureCodeFixProvider();
            Assert.IsNotNull(provider);
            
            var fixableIds = provider.FixableDiagnosticIds;
            Assert.IsTrue(fixableIds.Contains("BDD3001"));
            Assert.IsTrue(fixableIds.Contains("BDD3002"));
            Assert.IsTrue(fixableIds.Contains("BDD3003"));
        }

        [TestMethod]
        public void MethodRegionCodeFixProvider_CanBeInstantiated()
        {
            var provider = new MethodRegionCodeFixProvider();
            Assert.IsNotNull(provider);
            
            var fixableIds = provider.FixableDiagnosticIds;
            Assert.IsTrue(fixableIds.Contains("BDD4001"));
            Assert.IsTrue(fixableIds.Contains("BDD4002"));
        }

        [TestMethod]
        public void XmlDocumentationCodeFixProvider_CanBeInstantiated()
        {
            var provider = new XmlDocumentationCodeFixProvider();
            Assert.IsNotNull(provider);
            
            var fixableIds = provider.FixableDiagnosticIds;
            Assert.IsTrue(fixableIds.Contains("BDD5001"));
            Assert.IsTrue(fixableIds.Contains("BDD5002"));
            Assert.IsTrue(fixableIds.Contains("BDD5003"));
        }

        [TestMethod]
        public void AllCodeFixProviders_HaveFixAllProvider()
        {
            var folderProvider = new FolderStructureCodeFixProvider();
            var regionProvider = new MethodRegionCodeFixProvider();
            var xmlProvider = new XmlDocumentationCodeFixProvider();

            Assert.IsNotNull(folderProvider.GetFixAllProvider());
            Assert.IsNotNull(regionProvider.GetFixAllProvider());
            Assert.IsNotNull(xmlProvider.GetFixAllProvider());
        }
    }
}