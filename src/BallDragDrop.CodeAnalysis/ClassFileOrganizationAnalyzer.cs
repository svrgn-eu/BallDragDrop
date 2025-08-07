using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Analyzer that enforces one class per file organization and filename-to-classname matching
    /// Reports violations as errors to ensure build-breaking behavior
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassFileOrganizationAnalyzer : BaseAnalyzer
    {
        #region Properties

        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MultipleClassesInFile,
                DiagnosticDescriptors.FilenameClassNameMismatch);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes the analyzer by registering syntax tree actions
        /// </summary>
        /// <param name="context">The analysis context</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationAction(this.AnalyzeCompilation);
        }

        /// <summary>
        /// Analyzes the compilation to detect class file organization violations
        /// </summary>
        /// <param name="context">The compilation analysis context</param>
        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            // Analyze each syntax tree
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                this.AnalyzeSyntaxTree(syntaxTree, context.ReportDiagnostic, context.CancellationToken);
            }
        }

        /// <summary>
        /// Analyzes the syntax tree to detect class file organization violations
        /// </summary>
        /// <param name="syntaxTree">The syntax tree to analyze</param>
        /// <param name="reportDiagnostic">Action to report diagnostics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private void AnalyzeSyntaxTree(SyntaxTree syntaxTree, 
            System.Action<Diagnostic> reportDiagnostic, System.Threading.CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetCompilationUnitRoot(cancellationToken);
            var filePath = syntaxTree.FilePath;

            if (string.IsNullOrEmpty(filePath))
                return;

            // Get all class declarations in the file (excluding nested classes)
            var classDeclarations = this.GetTopLevelClassDeclarations(root);

            if (classDeclarations.Length == 0)
                return;

            // Check for multiple classes in a single file
            if (classDeclarations.Length > 1)
            {
                this.ReportMultipleClassesViolation(reportDiagnostic, classDeclarations, filePath);
            }

            // Check filename-to-classname matching for each class
            foreach (var classDeclaration in classDeclarations)
            {
                this.CheckFilenameClassNameMatch(reportDiagnostic, classDeclaration, filePath);
            }
        }

        /// <summary>
        /// Gets all top-level class declarations from the compilation unit
        /// Excludes nested classes and partial classes in other files
        /// </summary>
        /// <param name="root">The compilation unit root</param>
        /// <returns>List of top-level class declarations</returns>
        private ImmutableArray<ClassDeclarationSyntax> GetTopLevelClassDeclarations(CompilationUnitSyntax root)
        {
            var classes = ImmutableArray.CreateBuilder<ClassDeclarationSyntax>();

            // Get all class declarations that are direct children of namespaces or the compilation unit
            var topLevelClasses = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(this.IsTopLevelClass)
                .ToArray();

            classes.AddRange(topLevelClasses);

            return classes.ToImmutable();
        }

        /// <summary>
        /// Determines if a class declaration is a top-level class (not nested)
        /// </summary>
        /// <param name="classDeclaration">The class declaration to check</param>
        /// <returns>True if it's a top-level class, false if it's nested</returns>
        private bool IsTopLevelClass(ClassDeclarationSyntax classDeclaration)
        {
            // Check if the class is directly under a namespace or compilation unit
            var parent = classDeclaration.Parent;
            
            while (parent != null)
            {
                if (parent is ClassDeclarationSyntax || parent is StructDeclarationSyntax)
                {
                    // This is a nested class
                    return false;
                }
                
                if (parent is NamespaceDeclarationSyntax || parent is CompilationUnitSyntax)
                {
                    // This is a top-level class
                    return true;
                }
                
                parent = parent.Parent;
            }

            return true;
        }

        /// <summary>
        /// Reports violation for multiple classes in a single file
        /// </summary>
        /// <param name="reportDiagnostic">Action to report diagnostics</param>
        /// <param name="classDeclarations">The class declarations found in the file</param>
        /// <param name="filePath">The file path</param>
        private void ReportMultipleClassesViolation(System.Action<Diagnostic> reportDiagnostic, 
            ImmutableArray<ClassDeclarationSyntax> classDeclarations, string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            // WPF XAML code-behind files are still subject to the one class per file rule
            // Even though they have special naming rules, they should still contain only one class

            // Report the violation on each class after the first one
            for (int i = 1; i < classDeclarations.Length; i++)
            {
                var classDeclaration = classDeclarations[i];
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.MultipleClassesInFile,
                    classDeclaration.Identifier.GetLocation(),
                    fileName);
                
                reportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Checks if the filename matches the class name
        /// </summary>
        /// <param name="reportDiagnostic">Action to report diagnostics</param>
        /// <param name="classDeclaration">The class declaration to check</param>
        /// <param name="filePath">The file path</param>
        private void CheckFilenameClassNameMatch(System.Action<Diagnostic> reportDiagnostic, 
            ClassDeclarationSyntax classDeclaration, string filePath)
        {
            var className = classDeclaration.Identifier.ValueText;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fullFileName = Path.GetFileName(filePath);

            // Skip partial classes - they are allowed to have different filenames
            if (this.IsPartialClass(classDeclaration))
                return;

            // Skip WPF XAML code-behind files - they are allowed to have different naming patterns
            if (this.IsWpfXamlCodeBehindFile(filePath, className))
                return;

            // Check if filename matches class name
            if (!string.Equals(fileName, className, System.StringComparison.Ordinal))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.FilenameClassNameMismatch,
                    classDeclaration.Identifier.GetLocation(),
                    className,
                    fullFileName);
                
                reportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determines if a class declaration is a partial class
        /// </summary>
        /// <param name="classDeclaration">The class declaration to check</param>
        /// <returns>True if it's a partial class, false otherwise</returns>
        private bool IsPartialClass(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        /// <summary>
        /// Determines if a file is a WPF XAML code-behind file that should be exempt from strict filename matching
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <param name="className">The class name to validate against</param>
        /// <returns>True if it's a valid WPF XAML code-behind file, false otherwise</returns>
        private bool IsWpfXamlCodeBehindFile(string filePath, string className)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(className))
                return false;

            // Check if the file has a .xaml.cs extension (case insensitive)
            if (!filePath.EndsWith(".xaml.cs", System.StringComparison.OrdinalIgnoreCase))
                return false;

            // Get the base filename without the .xaml.cs extension
            var baseFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath));
            
            // For WPF XAML code-behind files, the class name should match the base filename
            // This is the standard WPF pattern: MainWindow.xaml -> MainWindow.xaml.cs -> class MainWindow
            // If the class name matches the base filename, we consider it a valid WPF code-behind file
            return string.Equals(baseFileName, className, System.StringComparison.Ordinal);
        }

        #endregion Methods
    }
}