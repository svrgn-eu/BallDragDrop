using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BallDragDrop.CodeAnalysis
{
    /// <summary>
    /// Configuration helper for BallDragDrop analyzers
    /// Handles loading and parsing of coding standards configuration
    /// </summary>
    public static class AnalyzerConfiguration
    {
        /// <summary>
        /// Default folder structure rules
        /// </summary>
        public static readonly Dictionary<string, string[]> DefaultFolderRules = new Dictionary<string, string[]>
        {
            { "Contracts", new[] { "interface", "abstract" } },
            { "Bootstrapper", new[] { "bootstrapper", "startup" } }
        };

        /// <summary>
        /// Default method region enforcement setting
        /// </summary>
        public const bool DefaultEnforceMethodRegions = true;

        /// <summary>
        /// Default region name format
        /// </summary>
        public const string DefaultRegionNameFormat = "#region {0}";

        /// <summary>
        /// Default XML documentation enforcement setting
        /// </summary>
        public const bool DefaultEnforceXmlDocumentation = true;

        /// <summary>
        /// Default complexity threshold for methods
        /// </summary>
        public const int DefaultComplexityThreshold = 10;

        /// <summary>
        /// Gets the configuration value for a specific analyzer setting
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <param name="settingName">The name of the setting</param>
        /// <param name="defaultValue">The default value if setting is not found</param>
        /// <returns>The configuration value</returns>
        public static T GetConfigurationValue<T>(AnalysisContext context, string settingName, T defaultValue)
        {
            try
            {
                // For now, return default values
                // In future iterations, this can be enhanced to read from configuration files
                return defaultValue;
            }
            catch (Exception)
            {
                // If configuration reading fails, return default value
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks if a rule is enabled for the given context
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <param name="ruleId">The rule identifier</param>
        /// <returns>True if the rule is enabled, false otherwise</returns>
        public static bool IsRuleEnabled(AnalysisContext context, string ruleId)
        {
            try
            {
                // For now, all rules are enabled by default
                // In future iterations, this can be enhanced to read from configuration files
                return true;
            }
            catch (Exception)
            {
                // If configuration reading fails, assume rule is enabled
                return true;
            }
        }

        /// <summary>
        /// Gets the folder rules for file organization
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <returns>Dictionary mapping folder names to file type patterns</returns>
        public static Dictionary<string, string[]> GetFolderRules(AnalysisContext context)
        {
            try
            {
                // For now, return default rules
                // In future iterations, this can be enhanced to read from configuration files
                return new Dictionary<string, string[]>(DefaultFolderRules);
            }
            catch (Exception)
            {
                // If configuration reading fails, return default rules
                return new Dictionary<string, string[]>(DefaultFolderRules);
            }
        }

        /// <summary>
        /// Gets the complexity threshold for method analysis
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <returns>The complexity threshold</returns>
        public static int GetComplexityThreshold(AnalysisContext context)
        {
            return GetConfigurationValue(context, "ComplexityThreshold", DefaultComplexityThreshold);
        }

        /// <summary>
        /// Checks if method regions are enforced
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <returns>True if method regions are enforced, false otherwise</returns>
        public static bool IsMethodRegionEnforcementEnabled(AnalysisContext context)
        {
            return GetConfigurationValue(context, "EnforceMethodRegions", DefaultEnforceMethodRegions);
        }

        /// <summary>
        /// Checks if XML documentation is enforced
        /// </summary>
        /// <param name="context">The analyzer context</param>
        /// <returns>True if XML documentation is enforced, false otherwise</returns>
        public static bool IsXmlDocumentationEnforcementEnabled(AnalysisContext context)
        {
            return GetConfigurationValue(context, "EnforceXmlDocumentation", DefaultEnforceXmlDocumentation);
        }
    }
}