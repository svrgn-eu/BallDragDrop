using System;
using System.IO;

class DebugPath
{
    static void Main()
    {
        string filePath = "MainWindow.xaml.cs";
        string className = "MainWindow";
        
        Console.WriteLine($"File path: '{filePath}'");
        Console.WriteLine($"Class name: '{className}'");
        Console.WriteLine($"Ends with .xaml.cs: {filePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)}");
        
        var step1 = Path.GetFileNameWithoutExtension(filePath);
        Console.WriteLine($"Step 1 - GetFileNameWithoutExtension: '{step1}'");
        
        var step2 = Path.GetFileNameWithoutExtension(step1);
        Console.WriteLine($"Step 2 - GetFileNameWithoutExtension again: '{step2}'");
        
        var matches = string.Equals(step2, className, StringComparison.Ordinal);
        Console.WriteLine($"Matches: {matches}");
        
        // Test with full path
        string fullPath = @"src\BallDragDrop\Views\MainWindow.xaml.cs";
        Console.WriteLine($"\nFull path: '{fullPath}'");
        Console.WriteLine($"Ends with .xaml.cs: {fullPath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)}");
        
        var fullStep1 = Path.GetFileNameWithoutExtension(fullPath);
        Console.WriteLine($"Full Step 1: '{fullStep1}'");
        
        var fullStep2 = Path.GetFileNameWithoutExtension(fullStep1);
        Console.WriteLine($"Full Step 2: '{fullStep2}'");
        
        var fullMatches = string.Equals(fullStep2, className, StringComparison.Ordinal);
        Console.WriteLine($"Full Matches: {fullMatches}");
    }
}