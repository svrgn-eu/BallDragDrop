using System;
using System.IO;
using BallDragDrop.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing WPF XAML code-behind detection...");

        // Test case 1: MainWindow.xaml.cs
        TestFile("MainWindow.xaml.cs", @"
namespace BallDragDrop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}");

        // Test case 2: SplashScreen.xaml.cs
        TestFile("SplashScreen.xaml.cs", @"
namespace BallDragDrop.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }
    }
}");

        // Test case 3: App.xaml.cs
        TestFile("App.xaml.cs", @"
namespace BallDragDrop
{
    public partial class App : Application
    {
        public App()
        {
        }
    }
}");

        // Test case 4: Regular class (should report diagnostic)
        TestFile("RegularClass.cs", @"
namespace BallDragDrop.Services
{
    public class TestService
    {
        public void DoSomething()
        {
        }
    }
}");

        Console.WriteLine("Testing complete.");
    }

    static void TestFile(string fileName, string source)
    {
        Console.WriteLine($"\n--- Testing {fileName} ---");

        var analyzer = new ClassFileOrganizationAnalyzer();
        
        // Create compilation
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run analyzer
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        Console.WriteLine($"Found {diagnostics.Length} diagnostics:");
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine($"  {diagnostic.Id}: {diagnostic.GetMessage()}");
        }

        // Check for filename mismatch specifically
        var filenameMismatchDiagnostics = diagnostics.Where(d => d.Id == "BDD11002").ToArray();
        if (filenameMismatchDiagnostics.Length > 0)
        {
            Console.WriteLine($"FILENAME MISMATCH DETECTED: {string.Join(", ", filenameMismatchDiagnostics.Select(d => d.GetMessage()))}");
        }
        else
        {
            Console.WriteLine("No filename mismatch diagnostics (good for XAML code-behind files)");
        }
    }
}