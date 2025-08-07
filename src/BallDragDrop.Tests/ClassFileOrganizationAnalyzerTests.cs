using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Unit tests for ClassFileOrganizationAnalyzer
    /// Tests enforcement of one class per file and filename-to-classname matching
    /// </summary>
    [TestClass]
    public class ClassFileOrganizationAnalyzerTests
    {
        #region Properties

        private ClassFileOrganizationAnalyzer _analyzer;

        #endregion Properties

        #region Construction

        [TestInitialize]
        public void Setup()
        {
            this._analyzer = new ClassFileOrganizationAnalyzer();
        }

        #endregion Construction

        #region Methods

        #region Single Class Tests

        [TestMethod]
        public void SingleClassWithMatchingFilename_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "TestClass.cs");
        }

        [TestMethod]
        public void SingleClassWithNonMatchingFilename_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "WrongName.cs");
        }

        [TestMethod]
        public void EmptyFile_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "EmptyFile.cs");
        }

        #endregion Single Class Tests

        #region Multiple Classes Tests

        [TestMethod]
        public void MultipleClassesInFile_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class FirstClass
    {
        public void TestMethod()
        {
        }
    }

    public class SecondClass
    {
        public void AnotherMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "FirstClass.cs");
        }

        [TestMethod]
        public void ThreeClassesInFile_ShouldReportTwoDiagnostics()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class FirstClass
    {
    }

    public class SecondClass
    {
    }

    public class ThirdClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001", "BDD11001" }, "FirstClass.cs");
        }

        [TestMethod]
        public void MultipleClassesWithFilenameIssue_ShouldReportBothViolations()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class FirstClass
    {
    }

    public class SecondClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002", "BDD11001" }, "WrongName.cs");
        }

        #endregion Multiple Classes Tests

        #region Nested Classes Tests

        [TestMethod]
        public void NestedClass_ShouldNotReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class OuterClass
    {
        public void TestMethod()
        {
        }

        public class NestedClass
        {
            public void NestedMethod()
            {
            }
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "OuterClass.cs");
        }

        [TestMethod]
        public void NestedClassWithWrongFilename_ShouldReportFilenameMismatch()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class OuterClass
    {
        public class NestedClass
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "WrongName.cs");
        }

        [TestMethod]
        public void MultipleNestedClasses_ShouldNotReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class OuterClass
    {
        public class FirstNested
        {
        }

        public class SecondNested
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "OuterClass.cs");
        }

        #endregion Nested Classes Tests

        #region Partial Classes Tests

        [TestMethod]
        public void PartialClassWithNonMatchingFilename_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public partial class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "TestClass.Designer.cs");
        }

        [TestMethod]
        public void PartialClassWithMatchingFilename_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public partial class TestClass
    {
        public void TestMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "TestClass.cs");
        }

        [TestMethod]
        public void MultiplePartialClassesInFile_ShouldReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public partial class FirstClass
    {
    }

    public partial class SecondClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "FirstClass.cs");
        }

        #endregion Partial Classes Tests

        #region Mixed Scenarios Tests

        [TestMethod]
        public void RegularAndPartialClass_ShouldReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class RegularClass
    {
    }

    public partial class PartialClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "RegularClass.cs");
        }

        [TestMethod]
        public void ClassWithNestedAndSeparateClass_ShouldReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class FirstClass
    {
        public class NestedClass
        {
        }
    }

    public class SecondClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "FirstClass.cs");
        }

        #endregion Mixed Scenarios Tests

        #region Namespace Scenarios Tests

        [TestMethod]
        public void ClassesInDifferentNamespaces_ShouldReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class FirstClass
    {
    }
}

namespace BallDragDrop.Models
{
    public class SecondClass
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "FirstClass.cs");
        }

        [TestMethod]
        public void ClassInGlobalNamespace_ShouldWorkCorrectly()
        {
            // Arrange
            var source = @"
public class GlobalClass
{
    public void TestMethod()
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "GlobalClass.cs");
        }

        [TestMethod]
        public void ClassInGlobalNamespaceWithWrongFilename_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
public class GlobalClass
{
    public void TestMethod()
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "WrongName.cs");
        }

        #endregion Namespace Scenarios Tests

        #region Interface and Struct Tests

        [TestMethod]
        public void InterfaceInFile_ShouldNotReportDiagnostic()
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
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "ITestInterface.cs");
        }

        [TestMethod]
        public void ClassAndInterface_ShouldReportMultipleClassesDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class TestClass
    {
    }

    public interface ITestInterface
    {
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "TestClass.cs");
        }

        [TestMethod]
        public void StructInFile_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public struct TestStruct
    {
        public int Value;
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "TestStruct.cs");
        }

        #endregion Interface and Struct Tests

        #region Edge Cases Tests

        [TestMethod]
        public void ClassWithGenericParameters_ShouldWorkCorrectly()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class GenericClass<T>
    {
        public T Value { get; set; }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "GenericClass.cs");
        }

        [TestMethod]
        public void AbstractClass_ShouldWorkCorrectly()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public abstract class AbstractClass
    {
        public abstract void AbstractMethod();
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "AbstractClass.cs");
        }

        [TestMethod]
        public void StaticClass_ShouldWorkCorrectly()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public static class StaticClass
    {
        public static void StaticMethod()
        {
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "StaticClass.cs");
        }

        #endregion Edge Cases Tests

        #region WPF XAML Code-Behind Tests

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithMatchingClassName_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithNonMatchingClassName_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Views
{
    public partial class WrongClassName : Window
    {
        public WrongClassName()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should report diagnostic because class name doesn't match base filename
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithMatchingClassNameAndExtension_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic because class name matches base filename
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void RegularCsFile_WithXamlCsExtension_ShouldReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Services
{
    public class SomeService
    {
        public void DoSomething()
        {
        }
    }
}";

            // Act & Assert - Should report diagnostic because it's not a valid WPF code-behind file
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_SplashScreen_ShouldNotReportDiagnostic()
        {
            // Arrange
            var source = @"
namespace BallDragDrop.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "SplashScreen.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_NonPartialClass_ShouldStillBeExempt()
        {
            // Arrange - Testing edge case where WPF code-behind class is not marked as partial
            var source = @"
namespace BallDragDrop.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic even if not partial
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_MultipleClasses_ShouldStillReportMultipleClassesDiagnostic()
        {
            // Arrange - WPF code-behind files should still be subject to one-class-per-file rule
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class AnotherClass
    {
        public void DoSomething()
        {
        }
    }
}";

            // Act & Assert - Should report multiple classes diagnostic
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11001" }, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_CaseInsensitiveExtension_ShouldWork()
        {
            // Arrange - Testing case insensitive extension matching
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should work with case insensitive extensions
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.XAML.CS");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_UserControl_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing WPF UserControl code-behind file
            var source = @"
namespace BallDragDrop.Views
{
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic for UserControl code-behind
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "UserControl1.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_Page_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing WPF Page code-behind file
            var source = @"
namespace BallDragDrop.Views
{
    public partial class Page1 : Page
    {
        public Page1()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic for Page code-behind
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "Page1.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithComplexName_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing WPF code-behind file with complex name
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MyCustomWindow : Window
    {
        public MyCustomWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic for complex named code-behind
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MyCustomWindow.xaml.cs");
        }

        [TestMethod]
        public void NonWpfFile_WithXamlCsExtension_ShouldReportDiagnostic()
        {
            // Arrange - Testing non-WPF file that happens to have .xaml.cs extension
            var source = @"
namespace BallDragDrop.Services
{
    public class SomeService
    {
        public void DoSomething()
        {
        }
    }
}";

            // Act & Assert - Should report diagnostic because class name doesn't match expected pattern
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithNestedClass_ShouldAllowNested()
        {
            // Arrange - Testing WPF code-behind file with nested class (should be allowed)
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private class NestedHelper
        {
            public void Help()
            {
            }
        }
    }
}";

            // Act & Assert - Should not report diagnostic for nested class in WPF code-behind
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithoutCorrespondingXamlFile_ShouldReportDiagnostic()
        {
            // Arrange - Testing .xaml.cs file without corresponding .xaml file
            var source = @"
namespace BallDragDrop.Views
{
    public partial class NonExistentWindow : Window
    {
        public NonExistentWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should report diagnostic because corresponding .xaml file doesn't exist on file system
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "NonExistentWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_WithCorrespondingXamlFile_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing .xaml.cs file with corresponding .xaml file (using actual existing file)
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic because MainWindow.xaml exists in the project
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "src/BallDragDrop/Views/MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_AppXaml_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing App.xaml.cs file (special WPF application file)
            var source = @"
namespace BallDragDrop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}";

            // Act & Assert - Should not report diagnostic for App.xaml.cs because App.xaml exists in the project
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "src/BallDragDrop/App.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_CaseSensitiveXamlFileCheck_ShouldWork()
        {
            // Arrange - Testing case sensitivity in XAML file existence check
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic when XAML file exists (case insensitive extension but case sensitive filename)
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_FakeXamlCsFile_ShouldReportDiagnostic()
        {
            // Arrange - Testing file with .xaml.cs extension but no corresponding .xaml file and wrong class name
            var source = @"
namespace BallDragDrop.Services
{
    public class SomeRandomService
    {
        public void DoWork()
        {
        }
    }
}";

            // Act & Assert - Should report diagnostic because it's not a valid WPF code-behind file
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "FakeWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_CorrectClassNameButNoXamlFile_ShouldReportDiagnostic()
        {
            // Arrange - Testing file with correct class name pattern but no corresponding .xaml file
            var source = @"
namespace BallDragDrop.Views
{
    public partial class MissingXamlWindow : Window
    {
        public MissingXamlWindow()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should report diagnostic because corresponding .xaml file doesn't exist on file system
            AnalyzerTestHelper.VerifyDiagnostics(this._analyzer, source, new[] { "BDD11002" }, "MissingXamlWindow.xaml.cs");
        }

        [TestMethod]
        public void WpfXamlCodeBehindFile_ValidUserControlWithXamlFile_ShouldNotReportDiagnostic()
        {
            // Arrange - Testing valid UserControl code-behind with existing XAML file (using SplashScreen as example)
            var source = @"
namespace BallDragDrop.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }
    }
}";

            // Act & Assert - Should not report diagnostic for valid code-behind because SplashScreen.xaml exists
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "src/BallDragDrop/Views/SplashScreen.xaml.cs");
        }

        #endregion WPF XAML Code-Behind Tests

        #endregion Methods
    }
}