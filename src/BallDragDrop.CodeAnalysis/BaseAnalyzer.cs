using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Base class for all BallDragDrop coding standards analyzers
    /// Provides common functionality and utilities for analyzer implementations
    /// </summary>
    public abstract class BaseAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// Gets the diagnostic descriptors supported by this analyzer
        /// </summary>
        public abstract override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        /// <summary>
        /// Initializes the analyzer by registering syntax node actions
        /// </summary>
        /// <param name="context">The analysis context</param>
        public abstract override void Initialize(AnalysisContext context);

        /// <summary>
        /// Helper method to check if a file is in the expected folder structure
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <param name="expectedFolder">The expected folder name</param>
        /// <returns>True if the file is in the expected folder, false otherwise</returns>
        protected static bool IsFileInExpectedFolder(string filePath, string expectedFolder)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(expectedFolder))
                return false;

            var normalizedPath = filePath.Replace('\\', '/');
            var folderPattern = $"/{expectedFolder}/";
            
            return normalizedPath.IndexOf(folderPattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Helper method to extract the class name from a file path
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>The class name without extension</returns>
        protected static string GetClassNameFromFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }

        /// <summary>
        /// Helper method to check if a class name indicates it's a bootstrapper
        /// </summary>
        /// <param name="className">The class name to check</param>
        /// <returns>True if the class appears to be a bootstrapper, false otherwise</returns>
        protected static bool IsBootstrapperClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                return false;

            return className.EndsWith("Bootstrapper", StringComparison.OrdinalIgnoreCase) ||
                   className.EndsWith("Bootstrap", StringComparison.OrdinalIgnoreCase) ||
                   className.IndexOf("Startup", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Helper method to report a diagnostic
        /// </summary>
        /// <param name="context">The syntax node analysis context</param>
        /// <param name="descriptor">The diagnostic descriptor</param>
        /// <param name="location">The location of the issue</param>
        /// <param name="messageArgs">Arguments for the diagnostic message</param>
        protected static void ReportDiagnostic(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Helper method to report a diagnostic
        /// </summary>
        /// <param name="context">The symbol analysis context</param>
        /// <param name="descriptor">The diagnostic descriptor</param>
        /// <param name="location">The location of the issue</param>
        /// <param name="messageArgs">Arguments for the diagnostic message</param>
        protected static void ReportDiagnostic(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }
    }
}