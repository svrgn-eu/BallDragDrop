using Microsoft.CodeAnalysis;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Contains all diagnostic descriptors for BallDragDrop coding standards analyzers
    /// </summary>
    public static class DiagnosticDescriptors
    {
        // Folder Structure Violations (3.x series)
        public static readonly DiagnosticDescriptor InterfaceNotInContractsFolder = new DiagnosticDescriptor(
            id: "BDD3001",
            title: "Interface should be in Contracts folder",
            messageFormat: "Interface '{0}' should be placed in the Contracts folder",
            category: "Structure",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Interfaces and abstract classes should be organized in the Contracts subfolder for better project structure.");

        public static readonly DiagnosticDescriptor AbstractClassNotInContractsFolder = new DiagnosticDescriptor(
            id: "BDD3002",
            title: "Abstract class should be in Contracts folder",
            messageFormat: "Abstract class '{0}' should be placed in the Contracts folder",
            category: "Structure",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Interfaces and abstract classes should be organized in the Contracts subfolder for better project structure.");

        public static readonly DiagnosticDescriptor BootstrapperNotInBootstrapperFolder = new DiagnosticDescriptor(
            id: "BDD3003",
            title: "Bootstrapper class should be in Bootstrapper folder",
            messageFormat: "Bootstrapper class '{0}' should be placed in the Bootstrapper folder",
            category: "Structure",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Bootstrapper files should be organized in the Bootstrapper subfolder for better project structure.");

        // Method Region Violations (4.x series)
        public static readonly DiagnosticDescriptor MethodNotInRegion = new DiagnosticDescriptor(
            id: "BDD4001",
            title: "Method should be enclosed in a region",
            messageFormat: "Method '{0}' should be enclosed in a region with format '#region {0}'",
            category: "Organization",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "All methods should be enclosed in regions following the '#region MethodName' format for better code organization.");

        public static readonly DiagnosticDescriptor IncorrectRegionNaming = new DiagnosticDescriptor(
            id: "BDD4002",
            title: "Region name should match method name",
            messageFormat: "Region name '{0}' should match the method name '{1}'",
            category: "Organization",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Region names should follow the format '#region MethodName' to match the enclosed method.");

        // XML Documentation Violations (5.x series)
        public static readonly DiagnosticDescriptor MissingXmlDocumentation = new DiagnosticDescriptor(
            id: "BDD5001",
            title: "Public method missing XML documentation",
            messageFormat: "Public method '{0}' is missing XML documentation",
            category: "Documentation",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All public methods must have comprehensive XML documentation including summary, parameters, return values, and exceptions.");

        public static readonly DiagnosticDescriptor IncompleteXmlDocumentation = new DiagnosticDescriptor(
            id: "BDD5002",
            title: "Incomplete XML documentation",
            messageFormat: "Method '{0}' has incomplete XML documentation: missing {1}",
            category: "Documentation",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "XML documentation should include all required elements: summary, parameters, return values, and exceptions.");

        public static readonly DiagnosticDescriptor MissingExceptionDocumentation = new DiagnosticDescriptor(
            id: "BDD5003",
            title: "Missing exception documentation",
            messageFormat: "Method '{0}' throws exceptions but lacks exception documentation",
            category: "Documentation",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Methods that can throw exceptions should document all possible exceptions in XML documentation.");

        // Code Quality Violations (6.x series)
        public static readonly DiagnosticDescriptor UnusedVariable = new DiagnosticDescriptor(
            id: "BDD6001",
            title: "Unused variable detected",
            messageFormat: "Variable '{0}' is declared but never used",
            category: "Quality",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Unused variables should be removed to improve code clarity and maintainability.");

        public static readonly DiagnosticDescriptor MethodTooComplex = new DiagnosticDescriptor(
            id: "BDD6002",
            title: "Method complexity too high",
            messageFormat: "Method '{0}' has complexity of {1}, consider refactoring (threshold: {2})",
            category: "Quality",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Methods with high complexity should be refactored into smaller, more manageable methods.");

        public static readonly DiagnosticDescriptor PotentialNullReference = new DiagnosticDescriptor(
            id: "BDD6003",
            title: "Potential null reference",
            messageFormat: "Potential null reference at '{0}'",
            category: "Quality",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Code should be checked for potential null reference exceptions.");
    }
}