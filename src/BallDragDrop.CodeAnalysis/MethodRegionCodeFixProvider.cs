using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Code fix provider for method region violations
    /// Provides automatic fixes for adding regions around methods and correcting region names
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodRegionCodeFixProvider)), Shared]
    public class MethodRegionCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MethodNotInRegion.Id,
                DiagnosticDescriptors.IncorrectRegionNaming.Id);

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
                var methodDeclaration = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();

                if (methodDeclaration == null)
                    continue;

                if (diagnostic.Id == DiagnosticDescriptors.MethodNotInRegion.Id)
                {
                    RegisterAddRegionFix(context, root, methodDeclaration, diagnostic);
                }
                else if (diagnostic.Id == DiagnosticDescriptors.IncorrectRegionNaming.Id)
                {
                    RegisterFixRegionNameFix(context, root, methodDeclaration, diagnostic);
                }
            }
        }

        /// <summary>
        /// Registers a code fix to add a region around a method
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="diagnostic">The diagnostic</param>
        private static void RegisterAddRegionFix(CodeFixContext context, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, Diagnostic diagnostic)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Add region around '{methodName}' method";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => AddRegionAroundMethod(context.Document, root, methodDeclaration, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Registers a code fix to correct the region name
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="diagnostic">The diagnostic</param>
        private static void RegisterFixRegionNameFix(CodeFixContext context, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, Diagnostic diagnostic)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Fix region name to match '{methodName}' method";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => FixRegionName(context.Document, root, methodDeclaration, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Adds a region around a method
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static Task<Document> AddRegionAroundMethod(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var methodName = methodDeclaration.Identifier.ValueText;

            // Create region directive
            var regionDirective = SyntaxFactory.Trivia(
                SyntaxFactory.RegionDirectiveTrivia(true)
                    .WithEndOfDirectiveToken(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage($" {methodName}")),
                            SyntaxKind.EndOfDirectiveToken,
                            SyntaxFactory.TriviaList())));

            // Create endregion directive
            var endRegionDirective = SyntaxFactory.Trivia(
                SyntaxFactory.EndRegionDirectiveTrivia(true)
                    .WithEndOfDirectiveToken(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage($" {methodName}")),
                            SyntaxKind.EndOfDirectiveToken,
                            SyntaxFactory.TriviaList())));

            // Add region directive before the method
            var leadingTrivia = methodDeclaration.GetLeadingTrivia()
                .Add(regionDirective)
                .Add(SyntaxFactory.CarriageReturnLineFeed);

            // Add endregion directive after the method
            var trailingTrivia = methodDeclaration.GetTrailingTrivia()
                .Add(SyntaxFactory.CarriageReturnLineFeed)
                .Add(endRegionDirective)
                .Add(SyntaxFactory.CarriageReturnLineFeed);

            var newMethodDeclaration = methodDeclaration
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);

            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Fixes the region name to match the method name
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static Task<Document> FixRegionName(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var methodSpan = methodDeclaration.Span;

            // Find the region and endregion directives that contain this method
            var regionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia))
                .Select(t => new { Trivia = t, Position = t.Span.End })
                .OrderBy(r => r.Position)
                .ToList();

            var endRegionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                .Select(t => new { Trivia = t, Position = t.Span.Start })
                .OrderBy(r => r.Position)
                .ToList();

            // Find the region pair that contains this method
            for (int i = 0; i < regionDirectives.Count && i < endRegionDirectives.Count; i++)
            {
                var regionStart = regionDirectives[i].Position;
                var regionEnd = endRegionDirectives[i].Position;

                if (methodSpan.Start >= regionStart && methodSpan.End <= regionEnd)
                {
                    var oldRegionTrivia = regionDirectives[i].Trivia;
                    var oldEndRegionTrivia = endRegionDirectives[i].Trivia;

                    // Create new region directive with correct name
                    var newRegionDirective = SyntaxFactory.Trivia(
                        SyntaxFactory.RegionDirectiveTrivia(true)
                            .WithEndOfDirectiveToken(
                                SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage($" {methodName}")),
                                    SyntaxKind.EndOfDirectiveToken,
                                    SyntaxFactory.TriviaList())));

                    // Create new endregion directive with correct name
                    var newEndRegionDirective = SyntaxFactory.Trivia(
                        SyntaxFactory.EndRegionDirectiveTrivia(true)
                            .WithEndOfDirectiveToken(
                                SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(SyntaxFactory.PreprocessingMessage($" {methodName}")),
                                    SyntaxKind.EndOfDirectiveToken,
                                    SyntaxFactory.TriviaList())));

                    // Replace the old directives with new ones
                    var newRoot = root.ReplaceTrivia(oldRegionTrivia, newRegionDirective);
                    newRoot = newRoot.ReplaceTrivia(
                        newRoot.DescendantTrivia().First(t => t.IsEquivalentTo(oldEndRegionTrivia)),
                        newEndRegionDirective);

                    return Task.FromResult(document.WithSyntaxRoot(newRoot));
                }
            }

            // If no region found, this shouldn't happen but return original document
            return Task.FromResult(document);
        }
    }
}