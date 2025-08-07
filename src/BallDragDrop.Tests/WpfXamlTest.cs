using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Simple test to verify WPF XAML code-behind file handling
    /// </summary>
    [TestClass]
    public class WpfXamlTest
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
        public void WpfXamlCodeBehindFile_AppXaml_ShouldNotReportDiagnostic()
        {
            // Arrange
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

            // Act & Assert
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "App.xaml.cs");
        }

        #endregion Methods
    }
}