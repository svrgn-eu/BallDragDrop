using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Analyzer that enforces method region organization standards
    /// Validates that methods are enclosed in regions with proper naming format
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodRegionAnalyzer : BaseAnalyzer
    {
        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                DiagnosticDescriptors.MethodNotInRegion,
                DiagnosticDescriptors.IncorrectRegionNaming);

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions
        /// </summary>
        /// <param name="context">The analysis context</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        /// <summary>
        /// Analyzes method declarations for region compliance
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodName = methodDeclaration.Identifier.ValueText;

            // Skip compiler-generated methods and property accessors
            if (IsCompilerGenerated(methodDeclaration) || IsPropertyAccessor(methodDeclaration))
                return;

            // Check if method is within a region
            var regionInfo = GetMethodRegionInfo(methodDeclaration);
            
            if (regionInfo.IsInRegion)
            {
                // Method is in a region, check if the region name matches the method name
                if (!IsRegionNameCorrect(regionInfo.RegionName, methodName))
                {
                    ReportDiagnostic(
                        context,
                        DiagnosticDescriptors.IncorrectRegionNaming,
                        methodDeclaration.Identifier.GetLocation(),
                        regionInfo.RegionName,
                        methodName);
                }
            }
            else
            {
                // Method is not in a region
                ReportDiagnostic(
                    context,
                    DiagnosticDescriptors.MethodNotInRegion,
                    methodDeclaration.Identifier.GetLocation(),
                    methodName);
            }
        }

        /// <summary>
        /// Gets region information for a method
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>Region information including whether the method is in a region and the region name</returns>
        private static MethodRegionInfo GetMethodRegionInfo(MethodDeclarationSyntax methodDeclaration)
        {
            var root = methodDeclaration.SyntaxTree.GetRoot();
            var methodSpan = methodDeclaration.Span;

            // Find all region and endregion directives
            var regionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia))
                .Select(t => new { Trivia = t, Position = t.Span.End, Name = AnalyzerUtilities.GetRegionName(t) })
                .OrderBy(r => r.Position)
                .ToList();

            var endRegionDirectives = root.DescendantTrivia()
                .Where(t => t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                .Select(t => new { Trivia = t, Position = t.Span.Start })
                .OrderBy(r => r.Position)
                .ToList();

            // Find the region that contains this method
            for (int i = 0; i < regionDirectives.Count && i < endRegionDirectives.Count; i++)
            {
                var regionStart = regionDirectives[i].Position;
                var regionEnd = endRegionDirectives[i].Position;

                if (methodSpan.Start >= regionStart && methodSpan.End <= regionEnd)
                {
                    return new MethodRegionInfo
                    {
                        IsInRegion = true,
                        RegionName = regionDirectives[i].Name
                    };
                }
            }

            return new MethodRegionInfo
            {
                IsInRegion = false,
                RegionName = string.Empty
            };
        }

        /// <summary>
        /// Checks if a region name is correct for the given method name
        /// </summary>
        /// <param name="regionName">The region name</param>
        /// <param name="methodName">The method name</param>
        /// <returns>True if the region name is correct, false otherwise</returns>
        private static bool IsRegionNameCorrect(string regionName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(regionName) || string.IsNullOrWhiteSpace(methodName))
                return false;

            // The region name should match the method name exactly
            return string.Equals(regionName.Trim(), methodName.Trim(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if a method is compiler-generated
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method is compiler-generated, false otherwise</returns>
        private static bool IsCompilerGenerated(MethodDeclarationSyntax methodDeclaration)
        {
            // Check for compiler-generated attributes
            var attributes = methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => a.Name.ToString());

            return attributes.Any(attr => 
                attr.Contains("CompilerGenerated") || 
                attr.Contains("GeneratedCode"));
        }

        /// <summary>
        /// Checks if a method is a property accessor (getter/setter)
        /// </summary>
        /// <param name="methodDeclaration">The method declaration</param>
        /// <returns>True if the method is a property accessor, false otherwise</returns>
        private static bool IsPropertyAccessor(MethodDeclarationSyntax methodDeclaration)
        {
            // Property accessors are typically within property declarations
            var parent = methodDeclaration.Parent;
            while (parent != null)
            {
                if (parent is PropertyDeclarationSyntax || parent is IndexerDeclarationSyntax)
                    return true;
                parent = parent.Parent;
            }

            // Also check for explicit get/set method names
            var methodName = methodDeclaration.Identifier.ValueText;
            return methodName.StartsWith("get_", StringComparison.Ordinal) ||
                   methodName.StartsWith("set_", StringComparison.Ordinal);
        }

        /// <summary>
        /// Information about a method's region context
        /// </summary>
        private struct MethodRegionInfo
        {
            public bool IsInRegion { get; set; }
            public string RegionName { get; set; }
        }
    }
}