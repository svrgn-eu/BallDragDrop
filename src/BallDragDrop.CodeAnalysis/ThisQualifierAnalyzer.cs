using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Analyzer that enforces mandatory use of 'this.' qualifier for instance member access
    /// Reports violations as errors to ensure build-breaking behavior
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ThisQualifierAnalyzer : BaseAnalyzer
    {
        #region Properties

        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MissingThisQualifierProperty,
                DiagnosticDescriptors.MissingThisQualifierMethod,
                DiagnosticDescriptors.MissingThisQualifierField);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions
        /// </summary>
        /// <param name="context">The analysis context</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        /// <summary>
        /// Analyzes identifier names to detect missing 'this.' qualifiers
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifierName = (IdentifierNameSyntax)context.Node;

            // Skip if already qualified with 'this.'
            if (this.IsQualifiedWithThis(identifierName))
                return;

            // Skip if it's a static member access
            if (this.IsStaticMemberAccess(identifierName, context.SemanticModel))
                return;

            // Skip if it's not an instance member access
            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName);
            if (symbolInfo.Symbol == null)
                return;

            // Check if this is an instance member that requires 'this.' qualifier
            if (this.RequiresThisQualifier(symbolInfo.Symbol, identifierName, context.SemanticModel))
            {
                this.ReportMissingThisQualifier(context, identifierName, symbolInfo.Symbol);
            }
        }

        /// <summary>
        /// Checks if the identifier is already qualified with 'this.'
        /// </summary>
        /// <param name="identifierName">The identifier name syntax</param>
        /// <returns>True if qualified with 'this.', false otherwise</returns>
        private bool IsQualifiedWithThis(IdentifierNameSyntax identifierName)
        {
            var memberAccess = identifierName.Parent as MemberAccessExpressionSyntax;
            if (memberAccess?.Expression is ThisExpressionSyntax)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the identifier represents a static member access
        /// </summary>
        /// <param name="identifierName">The identifier name syntax</param>
        /// <param name="semanticModel">The semantic model</param>
        /// <returns>True if it's a static member access, false otherwise</returns>
        private bool IsStaticMemberAccess(IdentifierNameSyntax identifierName, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(identifierName);
            if (symbolInfo.Symbol == null)
                return false;

            return symbolInfo.Symbol.IsStatic;
        }

        /// <summary>
        /// Determines if the symbol requires a 'this.' qualifier
        /// </summary>
        /// <param name="symbol">The symbol to check</param>
        /// <param name="identifierName">The identifier name syntax</param>
        /// <param name="semanticModel">The semantic model</param>
        /// <returns>True if 'this.' qualifier is required, false otherwise</returns>
        private bool RequiresThisQualifier(ISymbol symbol, IdentifierNameSyntax identifierName, SemanticModel semanticModel)
        {
            // Skip if it's a static member
            if (symbol.IsStatic)
                return false;

            // Skip if it's a local variable or parameter
            if (symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter)
                return false;

            // Skip if it's a type or namespace
            if (symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.Namespace)
                return false;

            // Skip if it's not a member of the current class
            var containingType = this.GetContainingType(identifierName, semanticModel);
            if (containingType == null || !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, containingType))
                return false;

            // Check if it's an instance member (property, method, or field)
            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                case SymbolKind.Method:
                case SymbolKind.Field:
                    return !symbol.IsStatic;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the containing type of the identifier
        /// </summary>
        /// <param name="identifierName">The identifier name syntax</param>
        /// <param name="semanticModel">The semantic model</param>
        /// <returns>The containing type symbol, or null if not found</returns>
        private INamedTypeSymbol GetContainingType(IdentifierNameSyntax identifierName, SemanticModel semanticModel)
        {
            var classDeclaration = identifierName.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration == null)
                return null;

            return semanticModel?.GetDeclaredSymbol(classDeclaration);
        }

        /// <summary>
        /// Reports the appropriate diagnostic for missing 'this.' qualifier
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        /// <param name="identifierName">The identifier name syntax</param>
        /// <param name="symbol">The symbol being accessed</param>
        private void ReportMissingThisQualifier(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifierName, ISymbol symbol)
        {
            DiagnosticDescriptor descriptor;
            
            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    descriptor = DiagnosticDescriptors.MissingThisQualifierProperty;
                    break;
                case SymbolKind.Method:
                    descriptor = DiagnosticDescriptors.MissingThisQualifierMethod;
                    break;
                case SymbolKind.Field:
                    descriptor = DiagnosticDescriptors.MissingThisQualifierField;
                    break;
                default:
                    return; // Don't report for other symbol types
            }

            ReportDiagnostic(context, descriptor, identifierName.GetLocation(), symbol.Name);
        }

        #endregion Methods
    }
}