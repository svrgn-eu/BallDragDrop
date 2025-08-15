using System;
using BallDragDrop.Services;
using BallDragDrop.ViewModels;
using BallDragDrop.Contracts;

// Simple test to verify the showBoundingBox configuration is respected
class Program
{
    static void Main()
    {
        Console.WriteLine("Testing showBoundingBox configuration fix...");
        
        try
        {
            // Create configuration service
            var configService = new ConfigurationService(new SimpleLogService());
            
            // Check the configuration value
            bool configValue = configService.GetShowBoundingBox();
            Console.WriteLine($"Configuration showBoundingBox value: {configValue}");
            
            // Create BallViewModel with the configuration service
            var ballViewModel = new BallViewModel(configService, new SimpleLogService(), null, null);
            
            // Check if the BallViewModel respects the configuration
            bool viewModelValue = ballViewModel.ShowBoundingBox;
            Console.WriteLine($"BallViewModel ShowBoundingBox value: {viewModelValue}");
            
            if (configValue == viewModelValue)
            {
                Console.WriteLine("✓ SUCCESS: BallViewModel correctly uses configuration value");
            }
            else
            {
                Console.WriteLine("✗ FAILURE: BallViewModel does not match configuration value");
            }
            
            // Test MainWindowViewModel property forwarding
            var statusBarViewModel = new StatusBarViewModel(new SimpleLogService());
            var mainViewModel = new MainWindowViewModel(ballViewModel, statusBarViewModel, new SimpleLogService());
            
            bool mainViewModelValue = mainViewModel.ShowBoundingBox;
            Console.WriteLine($"MainWindowViewModel ShowBoundingBox value: {mainViewModelValue}");
            
            if (viewModelValue == mainViewModelValue)
            {
                Console.WriteLine("✓ SUCCESS: MainWindowViewModel correctly forwards BallViewModel property");
            }
            else
            {
                Console.WriteLine("✗ FAILURE: MainWindowViewModel does not match BallViewModel property");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test: {ex.Message}");
        }
        
        Console.WriteLine("Test completed. Press any key to exit...");
        Console.ReadKey();
    }
}