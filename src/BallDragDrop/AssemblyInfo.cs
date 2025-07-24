using System.Runtime.CompilerServices;
using System.Windows;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]

// Allow test project to access internal members
[assembly: InternalsVisibleTo("BallDragDrop.Tests")]

namespace BallDragDrop
{
    /// <summary>
    /// Assembly-level attributes and configuration for the BallDragDrop application.
    /// Contains theme information and resource dictionary location settings.
    /// </summary>
    public static class AssemblyInfo
    {
        #region Assembly Attributes

        /// <summary>
        /// Configures theme information for the application assembly.
        /// Specifies where theme-specific and generic resource dictionaries are located.
        /// </summary>
        static AssemblyInfo()
        {
            // This static constructor ensures the assembly attributes are properly initialized
        }

        #endregion Assembly Attributes
    }
}
