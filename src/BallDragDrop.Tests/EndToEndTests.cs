using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BallDragDrop.Models;
using BallDragDrop.ViewModels;
using BallDragDrop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class EndToEndTests
    {
        /// <summary>
        /// Tests the complete ball drag and drop functionality
        /// </summary>
        [TestMethod]
        [STAThread]
        public void TestBallDragAndDrop()
        {
            // Create a new application instance
            MainWindow mainWindow = null;
            
            // Start the application on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Create a main window
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                    
                    // Verify the window is created
                    Assert.IsNotNull(mainWindow);
                    Assert.IsTrue(mainWindow.IsLoaded);
                    
                    // Verify the canvas is created
                    Assert.IsNotNull(mainWindow.MainCanvas);
                    
                    // Verify the ball view model is created
                    Assert.IsNotNull(mainWindow.DataContext);
                    Assert.IsInstanceOfType(mainWindow.DataContext, typeof(BallViewModel));
                    
                    // Get the ball view model
                    var viewModel = (BallViewModel)mainWindow.DataContext;
                    
                    // Verify the ball model is created (indirectly through properties)
                    Assert.IsTrue(viewModel.X > 0 && viewModel.Y > 0);
                    
                    // Verify the ball is positioned correctly
                    Assert.AreEqual(mainWindow.MainCanvas.Width / 2, viewModel.X);
                    Assert.AreEqual(mainWindow.MainCanvas.Height / 2, viewModel.Y);
                    
                    // Simulate dragging the ball
                    double startX = viewModel.X;
                    double startY = viewModel.Y;
                    double dragX = startX + 100;
                    double dragY = startY + 50;
                    
                    // Create mouse events
                    var mouseDownArgs = new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        0,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseDownEvent
                    };
                    
                    var mouseMoveArgs = new MouseEventArgs(
                        Mouse.PrimaryDevice,
                        0)
                    {
                        RoutedEvent = Mouse.MouseMoveEvent
                    };
                    
                    var mouseUpArgs = new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        0,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseUpEvent
                    };
                    
                    // Execute mouse down command
                    viewModel.MouseDownCommand.Execute(mouseDownArgs);
                    
                    // Verify the ball is being dragged
                    Assert.IsTrue(viewModel.IsDragging);
                    
                    // Execute mouse move command
                    viewModel.MouseMoveCommand.Execute(mouseMoveArgs);
                    
                    // Execute mouse up command
                    viewModel.MouseUpCommand.Execute(mouseUpArgs);
                    
                    // Verify the ball is no longer being dragged
                    Assert.IsFalse(viewModel.IsDragging);
                    
                    // Verify the ball stays within the window boundaries
                    Assert.IsTrue(viewModel.X >= viewModel.Radius);
                    Assert.IsTrue(viewModel.X <= mainWindow.MainCanvas.Width - viewModel.Radius);
                    Assert.IsTrue(viewModel.Y >= viewModel.Radius);
                    Assert.IsTrue(viewModel.Y <= mainWindow.MainCanvas.Height - viewModel.Radius);
                }
                finally
                {
                    // Clean up
                    if (mainWindow != null && mainWindow.IsLoaded)
                    {
                        mainWindow.Close();
                    }
                }
            });
        }
        
        /// <summary>
        /// Tests the ball throwing functionality
        /// </summary>
        [TestMethod]
        [STAThread]
        public void TestBallThrowing()
        {
            // Create a new application instance
            MainWindow mainWindow = null;
            
            // Start the application on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Create a main window
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                    
                    // Get the ball view model
                    var viewModel = (BallViewModel)mainWindow.DataContext;
                    
                    // Record the initial position
                    double initialX = viewModel.X;
                    double initialY = viewModel.Y;
                    
                    // We can't directly access the ball model, so we'll simulate throwing by
                    // using reflection to set the velocity
                    var ballModelField = typeof(BallViewModel).GetField("_ballModel", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var ballModel = ballModelField.GetValue(viewModel) as BallModel;
                    
                    // Set velocity on the ball model
                    ballModel.SetVelocity(100, 50);
                    
                    // Verify the ball is moving
                    Assert.IsTrue(ballModel.IsMoving);
                    
                    // Wait a short time for the physics to update
                    System.Threading.Thread.Sleep(100);
                    
                    // Verify the ball position has changed
                    Assert.AreNotEqual(initialX, viewModel.X);
                    Assert.AreNotEqual(initialY, viewModel.Y);
                    
                    // Verify the ball stays within the window boundaries
                    Assert.IsTrue(viewModel.X >= viewModel.Radius);
                    Assert.IsTrue(viewModel.X <= mainWindow.MainCanvas.Width - viewModel.Radius);
                    Assert.IsTrue(viewModel.Y >= viewModel.Radius);
                    Assert.IsTrue(viewModel.Y <= mainWindow.MainCanvas.Height - viewModel.Radius);
                }
                finally
                {
                    // Clean up
                    if (mainWindow != null && mainWindow.IsLoaded)
                    {
                        mainWindow.Close();
                    }
                }
            });
        }
        
        /// <summary>
        /// Tests the window resize functionality
        /// </summary>
        [TestMethod]
        [STAThread]
        public void TestWindowResize()
        {
            // Create a new application instance
            MainWindow mainWindow = null;
            
            // Start the application on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Create a main window
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                    
                    // Get the ball view model
                    var viewModel = (BallViewModel)mainWindow.DataContext;
                    
                    // Record the initial position
                    double initialX = viewModel.X;
                    double initialY = viewModel.Y;
                    
                    // Calculate the relative position
                    double relativeX = initialX / mainWindow.MainCanvas.Width;
                    double relativeY = initialY / mainWindow.MainCanvas.Height;
                    
                    // Simulate a window resize
                    double newWidth = mainWindow.MainCanvas.Width * 1.5;
                    double newHeight = mainWindow.MainCanvas.Height * 1.5;
                    mainWindow.SimulateResize(newWidth, newHeight);
                    
                    // Verify the canvas size has changed
                    Assert.AreEqual(newWidth, mainWindow.MainCanvas.Width);
                    Assert.AreEqual(newHeight, mainWindow.MainCanvas.Height);
                    
                    // Verify the ball position has been adjusted to maintain the relative position
                    double expectedX = relativeX * newWidth;
                    double expectedY = relativeY * newHeight;
                    
                    // Allow for small floating-point differences
                    Assert.AreEqual(expectedX, viewModel.X, 0.1);
                    Assert.AreEqual(expectedY, viewModel.Y, 0.1);
                    
                    // Verify the ball stays within the window boundaries
                    Assert.IsTrue(viewModel.X >= viewModel.Radius);
                    Assert.IsTrue(viewModel.X <= mainWindow.MainCanvas.Width - viewModel.Radius);
                    Assert.IsTrue(viewModel.Y >= viewModel.Radius);
                    Assert.IsTrue(viewModel.Y <= mainWindow.MainCanvas.Height - viewModel.Radius);
                }
                finally
                {
                    // Clean up
                    if (mainWindow != null && mainWindow.IsLoaded)
                    {
                        mainWindow.Close();
                    }
                }
            });
        }
        
        /// <summary>
        /// Tests the application lifecycle
        /// </summary>
        [TestMethod]
        [STAThread]
        public void TestApplicationLifecycle()
        {
            // Create a new application instance
            App app = null;
            
            // Start the application on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Create a new application instance
                    app = new App();
                    
                    // Simulate startup
                    app.InitializeComponent();
                    
                    // Verify the application is created
                    Assert.IsNotNull(app);
                    
                    // We can't easily create StartupEventArgs, so we'll just call the method directly
                    // with null arguments, which is fine for testing
                    
                    // Call the startup method via reflection (since it's private)
                    var startupMethod = typeof(App).GetMethod("Application_Startup", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (startupMethod != null)
                    {
                        startupMethod.Invoke(app, new object[] { app, null });
                    }
                    
                    // Verify the settings manager is created
                    var settingsManagerMethod = typeof(App).GetMethod("GetSettingsManager", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (settingsManagerMethod != null)
                    {
                        var settingsManager = settingsManagerMethod.Invoke(app, null);
                        Assert.IsNotNull(settingsManager);
                    }
                    
                    // We can't easily create ExitEventArgs, so we'll just call the method directly
                    // with null arguments, which is fine for testing
                    
                    // Call the exit method via reflection (since it's private)
                    var exitMethod = typeof(App).GetMethod("Application_Exit", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (exitMethod != null)
                    {
                        exitMethod.Invoke(app, new object[] { app, null });
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Application lifecycle test failed: {ex.Message}");
                }
            });
        }
    }
}