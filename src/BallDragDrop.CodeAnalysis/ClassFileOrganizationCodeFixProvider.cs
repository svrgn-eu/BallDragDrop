using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Code fix provider that automatically fixes class file organization violations
    /// Handles splitting multiple classes into separate files and renaming files to match class names
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassFileOrganizationCodeFixProvider)), Shared]
    public class ClassFileOrganizationCodeFixProvider : CodeFixProvider
    {
        #region Properties

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MultipleClassesInFile.Id,
                DiagnosticDescriptors.FilenameClassNameMismatch.Id);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Gets the fix all provider for batch fixing
        /// </summary>
        /// <returns>The fix all provider</returns>
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Registers code fixes for the specified diagnostics
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics.Where(d => this.FixableDiagnosticIds.Contains(d.Id)))
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (classDeclaration != null)
                {
                    if (diagnostic.Id == DiagnosticDescriptors.MultipleClassesInFile.Id)
                    {
                        this.RegisterMultipleClassesFix(context, diagnostic, classDeclaration);
                    }
                    else if (diagnostic.Id == DiagnosticDescriptors.FilenameClassNameMismatch.Id)
                    {
                        this.RegisterFilenameMismatchFix(context, diagnostic, classDeclaration);
                    }
                }
            }
        }

        /// <summary>
        /// Registers code fix for multiple classes in a single file
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="diagnostic">The diagnostic</param>
        /// <param name="classDeclaration">The class declaration</param>
        private void RegisterMultipleClassesFix(CodeFixContext context, Diagnostic diagnostic, ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.ValueText;
            
            var action = CodeAction.Create(
                title: $"Move class '{className}' to separate file",
                createChangedSolution: c => this.SplitClassToSeparateFileAsync(context.Document, classDeclaration, c),
                equivalenceKey: $"SplitClass_{className}");

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Registers code fix for filename-classname mismatch
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="diagnostic">The diagnostic</param>
        /// <param name="classDeclaration">The class declaration</param>
        private void RegisterFilenameMismatchFix(CodeFixContext context, Diagnostic diagnostic, ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.ValueText;
            
            var action = CodeAction.Create(
                title: $"Rename file to '{className}.cs'",
                createChangedSolution: c => this.RenameFileToMatchClassAsync(context.Document, className, c),
                equivalenceKey: $"RenameFile_{className}");

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Splits a class into a separate file
        /// </summary>
        /// <param name="document">The original document</param>
        /// <param name="classToMove">The class declaration to move</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The modified solution</returns>
        private async Task<Solution> SplitClassToSeparateFileAsync(Document document, ClassDeclarationSyntax classToMove, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var compilationUnit = root as CompilationUnitSyntax;

            if (compilationUnit == null)
                return solution;

            var className = classToMove.Identifier.ValueText;
            var originalFilePath = document.FilePath;
            var directory = Path.GetDirectoryName(originalFilePath);
            var newFileName = $"{className}.cs";
            var newFilePath = Path.Combine(directory, newFileName);

            // Create new document with just the class to move
            var newCompilationUnit = this.CreateNewCompilationUnit(compilationUnit, classToMove);
            var newDocumentId = DocumentId.CreateNewId(document.Project.Id);
            
            // Add the new document to the solution
            solution = solution.AddDocument(newDocumentId, newFileName, newCompilationUnit, document.Folders);

            // Remove the class from the original document
            var updatedRoot = compilationUnit.RemoveNode(classToMove, SyntaxRemoveOptions.KeepNoTrivia);
            solution = solution.WithDocumentSyntaxRoot(document.Id, updatedRoot);

            return solution;
        }

        /// <summary>
        /// Renames a file to match the class name
        /// </summary>
        /// <param name="document">The document to rename</param>
        /// <param name="className">The class name to match</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The modified solution</returns>
        private async Task<Solution> RenameFileToMatchClassAsync(Document document, string className, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var newFileName = $"{className}.cs";
            
            // Create a new document with the correct name
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDocumentId = DocumentId.CreateNewId(document.Project.Id);
            
            solution = solution.AddDocument(newDocumentId, newFileName, root, document.Folders);
            solution = solution.RemoveDocument(document.Id);

            return solution;
        }

        /// <summary>
        /// Creates a new compilation unit containing only the specified class
        /// </summary>
        /// <param name="originalCompilationUnit">The original compilation unit</param>
        /// <param name="classToInclude">The class to include in the new compilation unit</param>
        /// <returns>A new compilation unit</returns>
        private CompilationUnitSyntax CreateNewCompilationUnit(CompilationUnitSyntax originalCompilationUnit, ClassDeclarationSyntax classToInclude)
        {
            // Start with a new compilation unit
            var newCompilationUnit = SyntaxFactory.CompilationUnit();

            // Copy using directives
            newCompilationUnit = newCompilationUnit.WithUsings(originalCompilationUnit.Usings);

            // Find the namespace containing the class
            var namespaceDeclaration = classToInclude.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            
            if (namespaceDeclaration != null)
            {
                // Create a new namespace with just this class
                var newNamespace = SyntaxFactory.NamespaceDeclaration(namespaceDeclaration.Name)
                    .WithUsings(namespaceDeclaration.Usings)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classToInclude));

                newCompilationUnit = newCompilationUnit.WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(newNamespace));
            }
            else
            {
                // Class is at the compilation unit level
                newCompilationUnit = newCompilationUnit.WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classToInclude));
            }

            // Preserve formatting
            newCompilationUnit = newCompilationUnit.NormalizeWhitespace();

            return newCompilationUnit;
        }

        #endregion Methods
    }
}