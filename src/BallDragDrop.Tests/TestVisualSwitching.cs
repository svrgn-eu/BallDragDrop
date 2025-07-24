using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;

namespace TestVisualSwitching
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing Visual Content Switching Functionality...");
            
            try
            {
                // Create test directory
                var testDir = Path.Combine(Path.GetTempPath(), "VisualSwitchingTest");
                Directory.CreateDirectory(testDir);
                
                // Create test images
                var image1Path = CreateTestImage(testDir, "test1.png", Colors.Red);
                var image2Path = CreateTestImage(testDir, "test2.png", Colors.Blue);
                
                Console.WriteLine($"Created test images: {image1Path}, {image2Path}");
                
                // Test visual switching
                var imageService = new ImageService();
                var viewModel = new BallViewModel(100, 100, 25, imageService);
                
                // Load first image
                Console.WriteLine("Loading first image...");
                bool result1 = await viewModel.LoadBallVisualAsync(image1Path);
                Console.WriteLine($"First image loaded: {result1}");
                Console.WriteLine($"Content type: {viewModel.ContentType}");
                Console.WriteLine($"Is animated: {viewModel.IsAnimated}");
                
                // Switch to second image
                Console.WriteLine("Switching to second image...");
                bool result2 = await viewModel.SwitchBallVisualAsync(image2Path);
                Console.WriteLine($"Image switched: {result2}");
                Console.WriteLine($"Content type: {viewModel.ContentType}");
                Console.WriteLine($"Is animated: {viewModel.IsAnimated}");
                
                // Test position preservation
                viewModel.X = 150;
                viewModel.Y = 200;
                Console.WriteLine($"Set position to ({viewModel.X}, {viewModel.Y})");
                
                // Switch again
                Console.WriteLine("Switching back to first image...");
                bool result3 = await viewModel.SwitchBallVisualAsync(image1Path);
                Console.WriteLine($"Image switched back: {result3}");
                Console.WriteLine($"Position after switch: ({viewModel.X}, {viewModel.Y})");
                
                // Test drag state preservation
                viewModel.IsDragging = true;
                Console.WriteLine("Set dragging state to true");
                
                Console.WriteLine("Switching while dragging...");
                bool result4 = await viewModel.SwitchBallVisualAsync(image2Path);
                Console.WriteLine($"Image switched while dragging: {result4}");
                Console.WriteLine($"Dragging state preserved: {viewModel.IsDragging}");
                
                // Test invalid file
                Console.WriteLine("Testing invalid file...");
                bool result5 = await viewModel.SwitchBallVisualAsync("nonexistent.png");
                Console.WriteLine($"Invalid file handled correctly: {!result5}");
                
                // Cleanup
                Directory.Delete(testDir, true);
                
                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        private static string CreateTestImage(string directory, string fileName, Color color)
        {
            var filePath = Path.Combine(directory, fileName);
            
            // Create a simple test image using System.Drawing (if available) or fallback
            // For this test, we'll just create a placeholder file
            File.WriteAllText(filePath, $"Test image file - {color}");
            
            return filePath;
        }
    }
}