using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for FolderStructureAnalyzer
    /// Tests validation of file placement according to folder structure rules
    /// </summary>
    [TestClass]
    public class FolderStructureAnalyzerTests
    {
        private FolderStructureAnalyzer _analyzer;

        [TestInitialize]
        public void Setup()
        {
            _analyzer = new FolderStructureAnalyzer();
        }

        #region Interface Tests

        [TestMethod]
        public void InterfaceInContractsFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Contracts
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Contracts\ITestInterface.cs");
        }

        [TestMethod]
        public void InterfaceNotInContractsFolder_ShouldReportDiagnostic()
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

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD3001" }, @"C:\Project\BallDragDrop\Services\ITestInterface.cs");
        }

        [TestMethod]
        public void InterfaceInContractsFolderWithDifferentCasing_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.contracts
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\contracts\ITestInterface.cs");
        }

        [TestMethod]
        public void InterfaceInSubfolderOfContracts_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Contracts.Services
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Contracts\Services\ITestInterface.cs");
        }

        #endregion

        #region Abstract Class Tests

        [TestMethod]
        public void AbstractClassInContractsFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Contracts
{
    public abstract class TestAbstractClass
    {
        public abstract void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Contracts\TestAbstractClass.cs");
        }

        [TestMethod]
        public void AbstractClassNotInContractsFolder_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public abstract class TestAbstractClass
    {
        public abstract void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD3002" }, @"C:\Project\BallDragDrop\Models\TestAbstractClass.cs");
        }

        [TestMethod]
        public void ConcreteClassNotInContractsFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class TestConcreteClass
    {
        public void TestMethod() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Models\TestConcreteClass.cs");
        }

        #endregion

        #region Bootstrapper Tests

        [TestMethod]
        public void BootstrapperClassInBootstrapperFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Bootstrapper
{
    public class ServiceBootstrapper
    {
        public void Configure() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Bootstrapper\ServiceBootstrapper.cs");
        }

        [TestMethod]
        public void BootstrapperClassNotInBootstrapperFolder_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class ServiceBootstrapper
    {
        public void Configure() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD3003" }, @"C:\Project\BallDragDrop\Services\ServiceBootstrapper.cs");
        }

        [TestMethod]
        public void BootstrapClassInBootstrapperFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Bootstrapper
{
    public class ApplicationBootstrap
    {
        public void Initialize() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Bootstrapper\ApplicationBootstrap.cs");
        }

        [TestMethod]
        public void StartupClassInBootstrapperFolder_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Bootstrapper
{
    public class ApplicationStartup
    {
        public void Configure() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Bootstrapper\ApplicationStartup.cs");
        }

        [TestMethod]
        public void StartupClassNotInBootstrapperFolder_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class ApplicationStartup
    {
        public void Configure() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD3003" }, @"C:\Project\BallDragDrop\Services\ApplicationStartup.cs");
        }

        [TestMethod]
        public void RegularClassWithBootstrapperInName_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public class RegularClass
    {
        public void Method() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Models\RegularClass.cs");
        }

        #endregion

        #region Multiple Violations Tests

        [TestMethod]
        public void FileWithMultipleViolations_ShouldReportMultipleDiagnostics()
        {
            // Arrange
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
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(_analyzer, source, new[] { "BDD3001", "BDD3002", "BDD3003" }, @"C:\Project\BallDragDrop\Services\TestFile.cs");
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void EmptyFilePath_ShouldNotReportDiagnostic()
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

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, "");
        }

        [TestMethod]
        public void NullFilePath_ShouldNotReportDiagnostic()
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

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(_analyzer, source, null);
        }

        [TestMethod]
        public void NestedInterface_ShouldReportDiagnosticForOuterInterface()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public interface IOuterInterface
    {
        void OuterMethod();
        
        interface INestedInterface
        {
            void NestedMethod();
        }
    }
}";

            // Act & Assert
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(_analyzer, source, @"C:\Project\BallDragDrop\Services\TestFile.cs");
            
            // Should report diagnostic for outer interface, nested interface analysis depends on implementation
            Assert.IsTrue(diagnostics.Any(d => d.Id == "BDD3001"), "Expected BDD3001 diagnostic for outer interface");
        }

        #endregion

        #region Diagnostic Location Tests

        [TestMethod]
        public void InterfaceDiagnostic_ShouldReportAtCorrectLocation()
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

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD3001", 4, 22, @"C:\Project\BallDragDrop\Services\ITestInterface.cs");
        }

        [TestMethod]
        public void AbstractClassDiagnostic_ShouldReportAtCorrectLocation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Models
{
    public abstract class TestAbstractClass
    {
        public abstract void TestMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD3002", 4, 27, @"C:\Project\BallDragDrop\Models\TestAbstractClass.cs");
        }

        [TestMethod]
        public void BootstrapperClassDiagnostic_ShouldReportAtCorrectLocation()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class ServiceBootstrapper
    {
        public void Configure() { }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnosticLocation(_analyzer, source, "BDD3003", 4, 18, @"C:\Project\BallDragDrop\Services\ServiceBootstrapper.cs");
        }

        #endregion
    }
}