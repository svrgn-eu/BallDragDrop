using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Code fix provider for folder structure violations
    /// Provides automatic fixes for moving files to correct folders
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FolderStructureCodeFixProvider)), Shared]
    public class FolderStructureCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticDescriptors.InterfaceNotInContractsFolder.Id,
                DiagnosticDescriptors.AbstractClassNotInContractsFolder.Id,
                DiagnosticDescriptors.BootstrapperNotInBootstrapperFolder.Id);

        /// <summary>
        /// Gets the fix all provider for batch fixes
        /// </summary>
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Registers code fixes for the specified diagnostics
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!FixableDiagnosticIds.Contains(diagnostic.Id))
                    continue;

                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var node = root.FindNode(diagnosticSpan);

                string targetFolder = GetTargetFolder(diagnostic.Id);
                if (string.IsNullOrEmpty(targetFolder))
                    continue;

                var title = $"Move to {targetFolder} folder";
                var action = CodeAction.Create(
                    title: title,
                    createChangedDocument: c => MoveFileToFolder(context.Document, node, targetFolder, c),
                    equivalenceKey: title);

                context.RegisterCodeFix(action, diagnostic);
            }
        }

        /// <summary>
        /// Gets the target folder for a specific diagnostic ID
        /// </summary>
        /// <param name="diagnosticId">The diagnostic ID</param>
        /// <returns>The target folder name</returns>
        private static string GetTargetFolder(string diagnosticId)
        {
            switch (diagnosticId)
            {
                case "BDD3001": // InterfaceNotInContractsFolder
                    return "Contracts";
                case "BDD3002": // AbstractClassNotInContractsFolder
                    return "Contracts";
                case "BDD3003": // BootstrapperNotInBootstrapperFolder
                    return "Bootstrapper";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates a new document with the file moved to the correct folder
        /// </summary>
        /// <param name="document">The original document</param>
        /// <param name="node">The syntax node with the violation</param>
        /// <param name="targetFolder">The target folder name</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static async Task<Document> MoveFileToFolder(Document document, SyntaxNode node, string targetFolder, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Update namespace if needed
            var newRoot = UpdateNamespaceForFolder(root, targetFolder);

            // Create new document with updated namespace
            var newDocument = document.WithSyntaxRoot(newRoot);

            // Note: Actual file moving would need to be handled by the IDE/build system
            // This code fix only updates the namespace to match the new folder structure
            return newDocument;
        }

        /// <summary>
        /// Updates the namespace declaration to match the target folder
        /// </summary>
        /// <param name="root">The syntax root</param>
        /// <param name="targetFolder">The target folder name</param>
        /// <returns>The updated syntax root</returns>
        private static SyntaxNode UpdateNamespaceForFolder(SyntaxNode root, string targetFolder)
        {
            var namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespaceDeclaration == null)
                return root;

            var currentNamespace = namespaceDeclaration.Name.ToString();
            var newNamespace = UpdateNamespaceWithFolder(currentNamespace, targetFolder);

            if (currentNamespace == newNamespace)
                return root;

            var newNamespaceDeclaration = namespaceDeclaration.WithName(
                SyntaxFactory.ParseName(newNamespace).WithTriviaFrom(namespaceDeclaration.Name));

            return root.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);
        }

        /// <summary>
        /// Updates a namespace string to include the target folder
        /// </summary>
        /// <param name="currentNamespace">The current namespace</param>
        /// <param name="targetFolder">The target folder name</param>
        /// <returns>The updated namespace</returns>
        private static string UpdateNamespaceWithFolder(string currentNamespace, string targetFolder)
        {
            if (string.IsNullOrEmpty(currentNamespace))
                return targetFolder;

            // If the namespace already contains the target folder, don't modify it
            if (currentNamespace.EndsWith($".{targetFolder}", StringComparison.OrdinalIgnoreCase))
                return currentNamespace;

            // Add the target folder to the namespace
            return $"{currentNamespace}.{targetFolder}";
        }
    }
}