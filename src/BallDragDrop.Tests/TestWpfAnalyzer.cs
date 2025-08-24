using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using BallDragDrop.CodeAnalysis;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing WPF XAML Code-Behind File Analyzer");
        
        // Test 1: Valid WPF code-behind file
        TestValidWpfCodeBehind();
        
        // Test 2: Invalid WPF code-behind file (wrong class name)
        TestInvalidWpfCodeBehind();
        
        // Test 3: Non-WPF file with .xaml.cs extension
        TestNonWpfFileWithXamlCsExtension();
        
        Console.WriteLine("All tests completed!");
    }
    
    static void TestValidWpfCodeBehind()
    {
        Console.WriteLine("\nTest 1: Valid WPF code-behind file");
        
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
        
        var diagnostics = RunAnalyzer(source, "MainWindow.xaml.cs");
        Console.WriteLine($"Diagnostics count: {diagnostics.Length}");
        
        if (diagnostics.Length == 0)
        {
            Console.WriteLine("✓ PASS: No diagnostics reported for valid WPF code-behind file");
        }
        else
        {
            Console.WriteLine("✗ FAIL: Unexpected diagnostics:");
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"  - {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }
    }
    
    static void TestInvalidWpfCodeBehind()
    {
        Console.WriteLine("\nTest 2: Invalid WPF code-behind file (wrong class name)");
        
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
        
        var diagnostics = RunAnalyzer(source, "MainWindow.xaml.cs");
        Console.WriteLine($"Diagnostics count: {diagnostics.Length}");
        
        if (diagnostics.Length == 1 && diagnostics[0].Id == "BDD11002")
        {
            Console.WriteLine("✓ PASS: Correctly reported filename mismatch diagnostic");
        }
        else
        {
            Console.WriteLine("✗ FAIL: Expected BDD11002 diagnostic");
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"  - {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }
    }
    
    static void TestNonWpfFileWithXamlCsExtension()
    {
        Console.WriteLine("\nTest 3: Non-WPF file with .xaml.cs extension");
        
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
        
        var diagnostics = RunAnalyzer(source, "MainWindow.xaml.cs");
        Console.WriteLine($"Diagnostics count: {diagnostics.Length}");
        
        if (diagnostics.Length == 1 && diagnostics[0].Id == "BDD11002")
        {
            Console.WriteLine("✓ PASS: Correctly reported filename mismatch diagnostic for non-WPF file");
        }
        else
        {
            Console.WriteLine("✗ FAIL: Expected BDD11002 diagnostic");
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"  - {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }
    }
    
    static Diagnostic[] RunAnalyzer(string source, string filePath)
    {
        var analyzer = new ClassFileOrganizationAnalyzer();
        
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        
        return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
    }
}