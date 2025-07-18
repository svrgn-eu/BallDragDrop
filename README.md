# Ball Drag and Drop

A simple WPF application that demonstrates drag, drop, and physics-based ball movement.

## Features

- Drag and drop a ball within a window
- Throw the ball with physics-based movement
- Realistic bouncing behavior when the ball hits window boundaries
- Smooth animations with hardware acceleration
- Responsive window resizing that maintains the ball's relative position
- Performance optimizations for smooth interaction

## Requirements

- Windows operating system
- .NET 9 or later

## Getting Started

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/ball-drag-drop.git
   ```

2. Navigate to the project directory:
   ```
   cd ball-drag-drop
   ```

3. Build the application:
   ```
   dotnet build
   ```

4. Run the application:
   ```
   dotnet run --project src/BallDragDrop
   ```

### Usage

- **Drag the ball**: Click and hold the left mouse button on the ball, then move the mouse.
- **Throw the ball**: Drag the ball and release the mouse button while moving to throw it.
- **Grab the ball in motion**: Click on the ball while it's moving to stop and grab it.
- **Resize the window**: The ball will maintain its relative position within the window.

## Project Structure

- `src/BallDragDrop`: Main application project
  - `Models`: Data models for the application
  - `ViewModels`: View models for MVVM pattern
  - `Views`: UI components
  - `Services`: Application services
- `src/BallDragDrop.Tests`: Unit and integration tests
- `Resources`: Application resources (images, etc.)

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern:

- **Models**: Represent the data and business logic
  - `BallModel`: Represents the ball's physical properties
  - `PhysicsEngine`: Handles physics calculations for ball movement

- **ViewModels**: Act as an intermediary between the Model and View
  - `BallViewModel`: Exposes ball properties and commands for the View

- **Views**: Define the UI
  - `MainWindow`: The main application window
  - `SplashScreen`: Shown during application startup

## Performance Optimizations

- Hardware acceleration for smooth rendering
- Frame rate limiting to reduce CPU usage
- Event throttling for mouse move events
- Bitmap caching for improved rendering performance
- Optimized image loading with proper sizing

## Testing

The application includes comprehensive tests:

- Unit tests for individual components
- Integration tests for component interactions
- Performance tests for rendering and physics
- End-to-end tests for complete functionality

To run the tests:
```
dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- This project was created as a demonstration of WPF, MVVM, and physics-based animations.