using System;
using System.IO;

namespace BallDragDrop.Tests.PathLogic
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Testing WPF XAML Code-Behind Path Logic");
        
            TestPath("MainWindow.xaml.cs", "MainWindow");
            TestPath("SplashScreen.xaml.cs", "SplashScreen");
            TestPath("App.xaml.cs", "App");
            TestPath("MainWindow.xaml.cs", "WrongClassName");
            TestPath("SomeService.xaml.cs", "SomeService");
        
            Console.WriteLine("Path logic tests completed!");
        }
    
        static void TestPath(string filePath, string className)
        {
            Console.WriteLine($"\nTesting: {filePath} with class {className}");
        
            // Check if the file has a .xaml.cs extension (case insensitive)
            bool hasXamlCsExtension = filePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"  Has .xaml.cs extension: {hasXamlCsExtension}");
        
            if (hasXamlCsExtension)
            {
                // Get the base filename without the .xaml.cs extension
                var baseFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath));
                Console.WriteLine($"  Base filename: '{baseFileName}'");
            
                // Check if class name matches base filename
                bool matches = string.Equals(baseFileName, className, StringComparison.Ordinal);
                Console.WriteLine($"  Class name matches: {matches}");
            
                // Final result
                bool isValidWpfCodeBehind = matches;
                Console.WriteLine($"  Is valid WPF code-behind: {isValidWpfCodeBehind}");
            }
            else
            {
                Console.WriteLine("  Not a .xaml.cs file");
            }
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 8543fa9 (worked on error handling)
