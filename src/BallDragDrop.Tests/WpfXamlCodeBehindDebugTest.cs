using BallDragDrop.CodeAnalysis;
using BallDragDrop.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    /// <summary>
    /// Debug test for WPF XAML code-behind file detection
    /// </summary>
    [TestClass]
    public class WpfXamlCodeBehindDebugTest
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
        public void DebugMainWindowXamlCs_ShouldNotReportDiagnostic()
        {
            // Arrange - This is the exact structure from the real MainWindow.xaml.cs
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

            // Act & Assert - This should NOT report a diagnostic
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "MainWindow.xaml.cs");
        }

        [TestMethod]
        public void DebugSplashScreenXamlCs_ShouldNotReportDiagnostic()
        {
            // Arrange - This is the exact structure from the real SplashScreen.xaml.cs
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

            // Act & Assert - This should NOT report a diagnostic
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "SplashScreen.xaml.cs");
        }

        [TestMethod]
        public void DebugAppXamlCs_ShouldNotReportDiagnostic()
        {
            // Arrange - This is the exact structure from the real App.xaml.cs
            var source = @"
namespace BallDragDrop
{
    public partial class App : Application
    {
        public App()
        {
        }
    }
}";

            // Act & Assert - This should NOT report a diagnostic
            AnalyzerTestHelper.VerifyNoDiagnostics(this._analyzer, source, "App.xaml.cs");
        }

        #endregion Methods
    }
}