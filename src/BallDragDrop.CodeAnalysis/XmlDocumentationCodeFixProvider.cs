using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Code fix provider for XML documentation violations
    /// Provides automatic fixes for adding and completing XML documentation
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocumentationCodeFixProvider)), Shared]
    public class XmlDocumentationCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix
        /// </summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MissingXmlDocumentation.Id,
                DiagnosticDescriptors.IncompleteXmlDocumentation.Id,
                DiagnosticDescriptors.MissingExceptionDocumentation.Id);

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

                if (diagnostic.Id == DiagnosticDescriptors.MissingXmlDocumentation.Id)
                {
                    RegisterAddDocumentationFix(context, root, methodDeclaration, diagnostic);
                }
                else if (diagnostic.Id == DiagnosticDescriptors.IncompleteXmlDocumentation.Id)
                {
                    RegisterCompleteDocumentationFix(context, root, methodDeclaration, diagnostic);
                }
                else if (diagnostic.Id == DiagnosticDescriptors.MissingExceptionDocumentation.Id)
                {
                    RegisterAddExceptionDocumentationFix(context, root, methodDeclaration, diagnostic);
                }
            }
        }

        /// <summary>
        /// Registers a code fix to add complete XML documentation
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="diagnostic">The diagnostic</param>
        private static void RegisterAddDocumentationFix(CodeFixContext context, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, Diagnostic diagnostic)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Add XML documentation for '{methodName}' method";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => AddXmlDocumentation(context.Document, root, methodDeclaration, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Registers a code fix to complete existing XML documentation
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="diagnostic">The diagnostic</param>
        private static void RegisterCompleteDocumentationFix(CodeFixContext context, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, Diagnostic diagnostic)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Complete XML documentation for '{methodName}' method";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => CompleteXmlDocumentation(context.Document, root, methodDeclaration, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Registers a code fix to add exception documentation
        /// </summary>
        /// <param name="context">The code fix context</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="diagnostic">The diagnostic</param>
        private static void RegisterAddExceptionDocumentationFix(CodeFixContext context, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, Diagnostic diagnostic)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Add exception documentation for '{methodName}' method";

            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => AddExceptionDocumentation(context.Document, root, methodDeclaration, c),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        /// <summary>
        /// Adds complete XML documentation to a method
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static Task<Document> AddXmlDocumentation(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var documentation = GenerateXmlDocumentation(methodDeclaration);
            var documentationComment = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, documentation);

            var newMethodDeclaration = methodDeclaration.WithLeadingTrivia(
                methodDeclaration.GetLeadingTrivia().Add(SyntaxFactory.Trivia(documentationComment)));

            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Completes existing XML documentation for a method
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static Task<Document> CompleteXmlDocumentation(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var existingDoc = GetExistingDocumentation(methodDeclaration);
            var completeDoc = CompleteDocumentation(existingDoc, methodDeclaration);
            
            var documentationComment = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, completeDoc);

            // Remove existing documentation and add new complete documentation
            var leadingTrivia = methodDeclaration.GetLeadingTrivia()
                .Where(t => !t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && 
                           !t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .ToList();

            leadingTrivia.Add(SyntaxFactory.Trivia(documentationComment));

            var newMethodDeclaration = methodDeclaration.WithLeadingTrivia(leadingTrivia);
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Adds exception documentation to a method
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="root">The syntax root</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task containing the modified document</returns>
        private static Task<Document> AddExceptionDocumentation(Document document, SyntaxNode root, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var existingDoc = GetExistingDocumentation(methodDeclaration);
            var docWithExceptions = AddExceptionDocumentationToExisting(existingDoc, methodDeclaration);
            
            var documentationComment = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, docWithExceptions);

            // Remove existing documentation and add new documentation with exceptions
            var leadingTrivia = methodDeclaration.GetLeadingTrivia()
                .Where(t => !t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && 
                           !t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .ToList();

            leadingTrivia.Add(SyntaxFactory.Trivia(documentationComment));

            var newMethodDeclaration = methodDeclaration.WithLeadingTrivia(leadingTrivia);
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Generates complete XML documentation for a method
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>The XML documentation syntax list</returns>
        private static SyntaxList<XmlNodeSyntax> GenerateXmlDocumentation(MethodDeclarationSyntax methodDeclaration)
        {
            var nodes = new System.Collections.Generic.List<XmlNodeSyntax>();

            // Add summary
            nodes.Add(CreateXmlElement("summary", $"TODO: Add summary for {methodDeclaration.Identifier.ValueText} method"));

            // Add parameters
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                var paramName = parameter.Identifier.ValueText;
                nodes.Add(CreateXmlElementWithAttribute("param", "name", paramName, $"TODO: Add description for {paramName} parameter"));
            }

            // Add returns if method has return value
            if (HasReturnValue(methodDeclaration))
            {
                nodes.Add(CreateXmlElement("returns", "TODO: Add description for return value"));
            }

            // Add common exceptions
            if (CanThrowExceptions(methodDeclaration))
            {
                nodes.Add(CreateXmlElementWithAttribute("exception", "cref", "System.ArgumentException", "TODO: Add description for when this exception is thrown"));
            }

            return SyntaxFactory.List(nodes);
        }

        /// <summary>
        /// Gets existing documentation from a method
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>The existing documentation text</returns>
        private static string GetExistingDocumentation(MethodDeclarationSyntax methodDeclaration)
        {
            var docComment = methodDeclaration.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                    t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            return docComment.IsKind(SyntaxKind.None) ? string.Empty : docComment.ToString();
        }

        /// <summary>
        /// Completes existing documentation by adding missing elements
        /// </summary>
        /// <param name="existingDoc">The existing documentation</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>The completed documentation syntax list</returns>
        private static SyntaxList<XmlNodeSyntax> CompleteDocumentation(string existingDoc, MethodDeclarationSyntax methodDeclaration)
        {
            var nodes = new System.Collections.Generic.List<XmlNodeSyntax>();

            // Add summary if missing
            if (!existingDoc.Contains("<summary>"))
            {
                nodes.Add(CreateXmlElement("summary", $"TODO: Add summary for {methodDeclaration.Identifier.ValueText} method"));
            }

            // Add missing parameter documentation
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                var paramName = parameter.Identifier.ValueText;
                if (!existingDoc.Contains($"<param name=\"{paramName}\""))
                {
                    nodes.Add(CreateXmlElementWithAttribute("param", "name", paramName, $"TODO: Add description for {paramName} parameter"));
                }
            }

            // Add returns if missing and method has return value
            if (HasReturnValue(methodDeclaration) && !existingDoc.Contains("<returns>"))
            {
                nodes.Add(CreateXmlElement("returns", "TODO: Add description for return value"));
            }

            return SyntaxFactory.List(nodes);
        }

        /// <summary>
        /// Adds exception documentation to existing documentation
        /// </summary>
        /// <param name="existingDoc">The existing documentation</param>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>The documentation with exception information</returns>
        private static SyntaxList<XmlNodeSyntax> AddExceptionDocumentationToExisting(string existingDoc, MethodDeclarationSyntax methodDeclaration)
        {
            var nodes = new System.Collections.Generic.List<XmlNodeSyntax>();

            // Add exception documentation if missing
            if (!existingDoc.Contains("<exception"))
            {
                nodes.Add(CreateXmlElementWithAttribute("exception", "cref", "System.ArgumentException", "TODO: Add description for when this exception is thrown"));
                
                // Add more specific exceptions based on method content
                if (HasArrayAccess(methodDeclaration))
                {
                    nodes.Add(CreateXmlElementWithAttribute("exception", "cref", "System.IndexOutOfRangeException", "TODO: Add description for when this exception is thrown"));
                }
                
                if (HasNullReferenceRisk(methodDeclaration))
                {
                    nodes.Add(CreateXmlElementWithAttribute("exception", "cref", "System.NullReferenceException", "TODO: Add description for when this exception is thrown"));
                }
            }

            return SyntaxFactory.List(nodes);
        }

        /// <summary>
        /// Creates an XML element with text content
        /// </summary>
        /// <param name="elementName">The element name</param>
        /// <param name="content">The text content</param>
        /// <returns>The XML element syntax</returns>
        private static XmlElementSyntax CreateXmlElement(string elementName, string content)
        {
            return SyntaxFactory.XmlElement(
                SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName(elementName)),
                SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(content)),
                SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(elementName)));
        }

        /// <summary>
        /// Creates an XML element with an attribute and text content
        /// </summary>
        /// <param name="elementName">The element name</param>
        /// <param name="attributeName">The attribute name</param>
        /// <param name="attributeValue">The attribute value</param>
        /// <param name="content">The text content</param>
        /// <returns>The XML element syntax</returns>
        private static XmlElementSyntax CreateXmlElementWithAttribute(string elementName, string attributeName, string attributeValue, string content)
        {
            var attribute = SyntaxFactory.XmlTextAttribute(
                SyntaxFactory.XmlName(attributeName),
                SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.XmlTextLiteralToken, attributeValue, attributeValue, SyntaxTriviaList.Empty)),
                SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

            return SyntaxFactory.XmlElement(
                SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName(elementName))
                    .WithAttributes(SyntaxFactory.SingletonList<XmlAttributeSyntax>(attribute)),
                SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(content)),
                SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(elementName)));
        }

        /// <summary>
        /// Checks if a method has a return value
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method has a return value, false otherwise</returns>
        private static bool HasReturnValue(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.ReturnType == null)
                return false;

            var returnTypeText = methodDeclaration.ReturnType.ToString();
            return !returnTypeText.Equals("void", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a method can throw exceptions
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method can throw exceptions, false otherwise</returns>
        private static bool CanThrowExceptions(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
                return false;

            var descendants = methodDeclaration.DescendantNodes();
            return descendants.OfType<ThrowStatementSyntax>().Any() ||
                   descendants.OfType<InvocationExpressionSyntax>().Any() ||
                   descendants.OfType<ElementAccessExpressionSyntax>().Any();
        }

        /// <summary>
        /// Checks if a method has array access
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method has array access, false otherwise</returns>
        private static bool HasArrayAccess(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.DescendantNodes().OfType<ElementAccessExpressionSyntax>().Any();
        }

        /// <summary>
        /// Checks if a method has null reference risk
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method has null reference risk, false otherwise</returns>
        private static bool HasNullReferenceRisk(MethodDeclarationSyntax methodDeclaration)
        {
            var descendants = methodDeclaration.DescendantNodes();
            return descendants.OfType<MemberAccessExpressionSyntax>().Any() ||
                   descendants.OfType<InvocationExpressionSyntax>().Any();
        }
    }
}