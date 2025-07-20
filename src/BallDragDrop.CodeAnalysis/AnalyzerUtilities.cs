using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Utility methods for analyzer implementations
    /// </summary>
    public static class AnalyzerUtilities
    {
        /// <summary>
        /// Checks if a syntax node is within a region directive
        /// </summary>
        /// <param name="node">The syntax node to check</param>
        /// <param name="expectedRegionName">The expected region name (optional)</param>
        /// <returns>True if the node is within a region, false otherwise</returns>
        public static bool IsNodeInRegion(SyntaxNode node, string expectedRegionName = null)
        {
            if (node == null)
                return false;

            var root = node.SyntaxTree.GetRoot();
            var nodeSpan = node.Span;

            // Find all region directives in the file
            var regionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia))
                .ToList();

            var endRegionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                .ToList();

            // Check if the node is within any region
            for (int i = 0; i < regionDirectives.Count && i < endRegionDirectives.Count; i++)
            {
                var regionStart = regionDirectives[i].Span.End;
                var regionEnd = endRegionDirectives[i].Span.Start;

                if (nodeSpan.Start >= regionStart && nodeSpan.End <= regionEnd)
                {
                    if (string.IsNullOrEmpty(expectedRegionName))
                        return true;

                    // Check if the region name matches the expected name
                    var regionText = regionDirectives[i].ToString();
                    return regionText.IndexOf(expectedRegionName, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the region name from a region directive
        /// </summary>
        /// <param name="regionDirective">The region directive trivia</param>
        /// <returns>The region name, or empty string if not found</returns>
        public static string GetRegionName(SyntaxTrivia regionDirective)
        {
            if (!regionDirective.IsKind(SyntaxKind.RegionDirectiveTrivia))
                return string.Empty;

            var regionText = regionDirective.ToString();
            var regionKeyword = "#region";
            
            if (regionText.StartsWith(regionKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return regionText.Substring(regionKeyword.Length).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if a method has XML documentation
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to check</param>
        /// <returns>True if the method has XML documentation, false otherwise</returns>
        public static bool HasXmlDocumentation(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration == null)
                return false;

            var documentationComment = methodDeclaration.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                    t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            return !documentationComment.IsKind(SyntaxKind.None);
        }

        /// <summary>
        /// Checks if XML documentation is complete for a method
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to check</param>
        /// <returns>Array of missing documentation elements</returns>
        public static string[] GetMissingDocumentationElements(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration == null)
                return new[] { "summary", "parameters", "returns" };

            var missing = new System.Collections.Generic.List<string>();

            var documentationComment = methodDeclaration.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                    t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (documentationComment.IsKind(SyntaxKind.None))
            {
                missing.Add("summary");
                if (methodDeclaration.ParameterList.Parameters.Count > 0)
                    missing.Add("parameters");
                if (methodDeclaration.ReturnType != null && !methodDeclaration.ReturnType.ToString().Equals("void"))
                    missing.Add("returns");
                return missing.ToArray();
            }

            var docText = documentationComment.ToString();

            // Check for summary
            if (docText.IndexOf("<summary>", StringComparison.OrdinalIgnoreCase) < 0)
                missing.Add("summary");

            // Check for parameters
            if (methodDeclaration.ParameterList.Parameters.Count > 0 &&
                docText.IndexOf("<param", StringComparison.OrdinalIgnoreCase) < 0)
                missing.Add("parameters");

            // Check for return value
            if (methodDeclaration.ReturnType != null && 
                !methodDeclaration.ReturnType.ToString().Equals("void") &&
                docText.IndexOf("<returns>", StringComparison.OrdinalIgnoreCase) < 0)
                missing.Add("returns");

            return missing.ToArray();
        }

        /// <summary>
        /// Calculates the cyclomatic complexity of a method
        /// </summary>
        /// <param name="methodDeclaration">The method declaration to analyze</param>
        /// <returns>The cyclomatic complexity value</returns>
        public static int CalculateComplexity(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration == null)
                return 0;

            int complexity = 1; // Base complexity

            var descendants = methodDeclaration.DescendantNodes();

            // Count decision points
            complexity += descendants.OfType<IfStatementSyntax>().Count();
            complexity += descendants.OfType<WhileStatementSyntax>().Count();
            complexity += descendants.OfType<ForStatementSyntax>().Count();
            complexity += descendants.OfType<ForEachStatementSyntax>().Count();
            complexity += descendants.OfType<DoStatementSyntax>().Count();
            complexity += descendants.OfType<SwitchStatementSyntax>().Count();
            complexity += descendants.OfType<CatchClauseSyntax>().Count();
            complexity += descendants.OfType<ConditionalExpressionSyntax>().Count();

            // Count logical operators in conditions
            complexity += descendants.OfType<BinaryExpressionSyntax>()
                .Count(b => b.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
                           b.OperatorToken.IsKind(SyntaxKind.BarBarToken));

            return complexity;
        }
    }
}