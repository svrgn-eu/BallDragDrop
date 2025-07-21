using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Analyzer that validates file placement according to folder structure rules
    /// Ensures interfaces and abstract classes are in Contracts folder
    /// Ensures bootstrapper files are in Bootstrapper folder
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FolderStructureAnalyzer : BaseAnalyzer
    {
        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.InterfaceNotInContractsFolder,
                DiagnosticDescriptors.AbstractClassNotInContractsFolder,
                DiagnosticDescriptors.BootstrapperNotInBootstrapperFolder);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions
        /// </summary>
        /// <param name="context">The analysis context</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclaration, SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        /// <summary>
        /// Analyzes interface declarations to ensure they are in the Contracts folder
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        private static void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;

            if (string.IsNullOrEmpty(filePath))
                return;

            // Check if the interface is in the Contracts folder
            if (!IsFileInExpectedFolder(filePath, "Contracts"))
            {
                var interfaceName = interfaceDeclaration.Identifier.ValueText;
                var location = interfaceDeclaration.Identifier.GetLocation();

                ReportDiagnostic(
                    context,
                    DiagnosticDescriptors.InterfaceNotInContractsFolder,
                    location,
                    interfaceName);
            }
        }

        /// <summary>
        /// Analyzes class declarations to ensure abstract classes and bootstrappers are in correct folders
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;

            if (string.IsNullOrEmpty(filePath))
                return;

            var className = classDeclaration.Identifier.ValueText;
            var location = classDeclaration.Identifier.GetLocation();

            // Check if it's an abstract class
            if (IsAbstractClass(classDeclaration))
            {
                if (!IsFileInExpectedFolder(filePath, "Contracts"))
                {
                    ReportDiagnostic(
                        context,
                        DiagnosticDescriptors.AbstractClassNotInContractsFolder,
                        location,
                        className);
                }
            }
            // Check if it's a bootstrapper class
            else if (IsBootstrapperClass(className))
            {
                if (!IsFileInExpectedFolder(filePath, "Bootstrapper"))
                {
                    ReportDiagnostic(
                        context,
                        DiagnosticDescriptors.BootstrapperNotInBootstrapperFolder,
                        location,
                        className);
                }
            }
        }

        /// <summary>
        /// Checks if a class declaration is abstract
        /// </summary>
        /// <param name="classDeclaration">The class declaration to check</param>
        /// <returns>True if the class is abstract, false otherwise</returns>
        private static bool IsAbstractClass(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword));
        }
    }
}