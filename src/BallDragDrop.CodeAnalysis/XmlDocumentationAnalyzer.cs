using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Analyzer that validates XML documentation completeness for public methods
    /// Enforces comprehensive documentation including summary, parameters, return values, and exceptions
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class XmlDocumentationAnalyzer : BaseAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MissingXmlDocumentation,
                DiagnosticDescriptors.IncompleteXmlDocumentation,
                DiagnosticDescriptors.MissingExceptionDocumentation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Only analyze public methods
            if (!IsPublicMethod(methodDeclaration))
                return;

            // Skip compiler-generated methods and property accessors
            if (IsCompilerGenerated(methodDeclaration))
                return;

            var methodName = methodDeclaration.Identifier.ValueText;

            // Check if method has any XML documentation
            var documentationComment = GetXmlDocumentationComment(methodDeclaration);
            if (documentationComment.IsKind(SyntaxKind.None))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.MissingXmlDocumentation, 
                    methodDeclaration.Identifier.GetLocation(), methodName);
                return;
            }

            // Analyze documentation completeness
            AnalyzeDocumentationCompleteness(context, methodDeclaration, documentationComment);

            // Analyze exception documentation
            AnalyzeExceptionDocumentation(context, methodDeclaration, documentationComment);
        }

        private static bool IsPublicMethod(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private static bool IsCompilerGenerated(MethodDeclarationSyntax methodDeclaration)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            
            // Skip property accessors and event handlers
            if (methodName.StartsWith("get_") || methodName.StartsWith("set_") ||
                methodName.StartsWith("add_") || methodName.StartsWith("remove_"))
                return true;

            // Skip methods with CompilerGenerated attribute
            return methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().Contains("CompilerGenerated"));
        }

        private static SyntaxTrivia GetXmlDocumentationComment(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                    t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        }

        private static void AnalyzeDocumentationCompleteness(SyntaxNodeAnalysisContext context, 
            MethodDeclarationSyntax methodDeclaration, SyntaxTrivia documentationComment)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var docText = documentationComment.ToString();
            var missingElements = new System.Collections.Generic.List<string>();

            // Check for summary
            if (!HasDocumentationElement(docText, "summary"))
            {
                missingElements.Add("summary");
            }

            // Check for parameter documentation
            var parameters = methodDeclaration.ParameterList.Parameters;
            if (parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    var paramName = parameter.Identifier.ValueText;
                    if (!HasParameterDocumentation(docText, paramName))
                    {
                        missingElements.Add($"parameter '{paramName}'");
                    }
                }
            }

            // Check for return value documentation
            if (HasReturnValue(methodDeclaration) && !HasDocumentationElement(docText, "returns"))
            {
                missingElements.Add("returns");
            }

            // Report incomplete documentation if any elements are missing
            if (missingElements.Count > 0)
            {
                var missingElementsText = string.Join(", ", missingElements);
                ReportDiagnostic(context, DiagnosticDescriptors.IncompleteXmlDocumentation,
                    methodDeclaration.Identifier.GetLocation(), methodName, missingElementsText);
            }
        }

        private static void AnalyzeExceptionDocumentation(SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration, SyntaxTrivia documentationComment)
        {
            var methodName = methodDeclaration.Identifier.ValueText;
            var docText = documentationComment.ToString();

            // Check if method can throw exceptions
            if (CanThrowExceptions(methodDeclaration))
            {
                // Check if there's any exception documentation
                if (!HasDocumentationElement(docText, "exception"))
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.MissingExceptionDocumentation,
                        methodDeclaration.Identifier.GetLocation(), methodName);
                }
            }
        }

        private static bool HasDocumentationElement(string docText, string elementName)
        {
            var pattern = $@"<{elementName}[^>]*>.*?</{elementName}>";
            return Regex.IsMatch(docText, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private static bool HasParameterDocumentation(string docText, string parameterName)
        {
            var pattern = $@"<param\s+name\s*=\s*[""']{Regex.Escape(parameterName)}[""'][^>]*>.*?</param>";
            return Regex.IsMatch(docText, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private static bool HasReturnValue(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.ReturnType == null)
                return false;

            var returnTypeText = methodDeclaration.ReturnType.ToString();
            return !returnTypeText.Equals("void", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanThrowExceptions(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null)
                return false;

            var descendants = methodDeclaration.DescendantNodes();

            // Check for explicit throw statements
            if (descendants.OfType<ThrowStatementSyntax>().Any())
                return true;

            // Check for method calls that might throw exceptions
            var invocations = descendants.OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    
                    // Common methods that can throw exceptions
                    if (IsExceptionThrowingMethod(methodName))
                        return true;
                }
            }

            // Check for array access, null reference potential, etc.
            if (descendants.OfType<ElementAccessExpressionSyntax>().Any() ||
                descendants.OfType<CastExpressionSyntax>().Any() ||
                descendants.OfType<BinaryExpressionSyntax>().Any(b => b.OperatorToken.IsKind(SyntaxKind.AsKeyword)))
                return true;

            return false;
        }

        private static bool IsExceptionThrowingMethod(string methodName)
        {
            var exceptionThrowingMethods = new[]
            {
                "Parse", "Convert", "ToInt32", "ToDouble", "ToDecimal", "ToDateTime",
                "Substring", "Remove", "Insert", "Replace", "Split",
                "First", "Single", "Last", "ElementAt",
                "Add", "Remove", "RemoveAt", "Insert", "Clear",
                "Open", "Read", "Write", "Close", "Connect", "Execute"
            };

            return exceptionThrowingMethods.Contains(methodName, StringComparer.OrdinalIgnoreCase);
        }
    }
}