using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Code fix provider that automatically adds missing 'this.' qualifiers to instance member access
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThisQualifierCodeFixProvider)), Shared]
    public class ThisQualifierCodeFixProvider : CodeFixProvider
    {
        #region Properties

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MissingThisQualifierProperty.Id,
                DiagnosticDescriptors.MissingThisQualifierMethod.Id,
                DiagnosticDescriptors.MissingThisQualifierField.Id);

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
                var identifierName = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<IdentifierNameSyntax>().FirstOrDefault();

                if (identifierName != null)
                {
                    var action = CodeAction.Create(
                        title: $"Add 'this.' qualifier",
                        createChangedDocument: c => this.AddThisQualifierAsync(context.Document, identifierName, c),
                        equivalenceKey: $"AddThisQualifier_{diagnostic.Id}");

                    context.RegisterCodeFix(action, diagnostic);
                }
            }
        }

        /// <summary>
        /// Adds the 'this.' qualifier to the specified identifier
        /// </summary>
        /// <param name="document">The document to modify</param>
        /// <param name="identifierName">The identifier name to qualify</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The modified document</returns>
        private async Task<Document> AddThisQualifierAsync(Document document, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Create the 'this.' qualified member access
            var thisExpression = SyntaxFactory.ThisExpression();
            var memberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                thisExpression,
                identifierName.WithoutLeadingTrivia());

            // Preserve the original trivia
            var newMemberAccess = memberAccess.WithLeadingTrivia(identifierName.GetLeadingTrivia());

            // Replace the identifier with the qualified member access
            var newRoot = root.ReplaceNode(identifierName, newMemberAccess);

            return document.WithSyntaxRoot(newRoot);
        }

        #endregion Methods
    }
}