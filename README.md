# Ball Drag and Drop

A WPF demonstration application showcasing physics-based ball movement with drag, drop, and realistic bouncing behavior. The application features smooth animations with hardware acceleration, responsive window resizing, and performance optimizations for interactive ball physics simulation.

## Features

- **Interactive Mechanics**: Drag and drop a ball within a window with intuitive mouse controls
- **Physics Simulation**: Realistic ball movement with gravity, velocity, and collision detection
- **Bouncing Behavior**: Dynamic bouncing when the ball hits window boundaries
- **Hardware Acceleration**: Smooth animations optimized for performance
- **Responsive Design**: Window resizing maintains ball's relative position and physics
- **State Management**: State machine-driven ball behavior for consistent interactions
- **Performance Monitoring**: Built-in performance optimization and monitoring
- **Comprehensive Logging**: Detailed logging with log4net integration

## Technology Stack

- **.NET 9.0** with Windows-specific features
- **WPF** (Windows Presentation Foundation) for UI
- **C#** with nullable reference types and implicit usings enabled
- **Microsoft.Extensions.DependencyInjection** - Dependency injection container
- **Microsoft.Extensions.Configuration** - Configuration management
- **log4net** - Logging framework
- **Castle.Core** - Interception and proxy capabilities
- **Stateless** - State machine implementation

## Requirements

- Windows operating system
- .NET 9.0 or later

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
   ```cmd
   dotnet build
   ```

4. Run the application:
   ```cmd
   dotnet run --project src/BallDragDrop
   ```

### Usage

- **Drag the ball**: Click and hold the left mouse button on the ball, then move the mouse.
- **Throw the ball**: Drag the ball and release the mouse button while moving to throw it.
- **Grab the ball in motion**: Click on the ball while it's moving to stop and grab it.
- **Resize the window**: The ball will maintain its relative position within the window.

## Project Structure

```
src/
├── BallDragDrop/              # Main WPF application
│   ├── Contracts/             # Interfaces and abstract classes
│   ├── Bootstrapper/          # Application bootstrapping and DI configuration
│   ├── Models/                # Data models and business logic
│   ├── Services/              # Application services and utilities
│   ├── ViewModels/            # MVVM view models
│   ├── Views/                 # WPF views and user controls
│   ├── Converters/            # XAML value converters
│   ├── Commands/              # Command implementations
│   ├── Build/                 # MSBuild targets and validation scripts
│   ├── CodeQuality/           # Code quality reports and metrics
│   └── Resources/             # Application resources
├── BallDragDrop.Tests/        # Unit and integration tests
└── BallDragDrop.CodeAnalysis/ # Custom code analyzers
```

## Architecture

The application follows MVVM pattern with clean separation of concerns, dependency injection, comprehensive logging, and extensive code quality enforcement through custom analyzers and coding standards.

### Core Components

- **Models**: Business logic and data representation
  - `BallModel`: Ball's physical properties and state
  - `PhysicsEngine`: Physics calculations for movement and collisions

- **ViewModels**: MVVM intermediary layer
  - `BallViewModel`: Exposes ball properties and commands for UI binding

- **Views**: WPF user interface
  - `MainWindow`: Primary application window with ball interaction
  - `SplashScreen`: Application startup screen

- **Services**: Application services and utilities
  - Logging services with log4net integration
  - Configuration management
  - Performance monitoring

- **State Management**: Stateless state machine for ball behavior transitions

## Performance Optimizations

- Hardware acceleration for smooth rendering
- Frame rate limiting to reduce CPU usage
- Event throttling for mouse move events
- Bitmap caching for improved rendering performance
- Optimized image loading with proper sizing

## Development Commands

### Build and Run
```cmd
# Build the solution
dotnet build

# Run the application
dotnet run --project src/BallDragDrop

# Run tests
dotnet test
```

### Code Quality Validation
```cmd
# PowerShell
src/BallDragDrop/Build/ValidateCodingStandards.ps1

# Batch
src/BallDragDrop/Build/RunCodingStandardsValidation.bat
```

## Code Quality & Standards

The project enforces strict coding standards through:

- **Microsoft.CodeAnalysis.Analyzers** - Core code analysis
- **Microsoft.CodeAnalysis.NetAnalyzers** - .NET-specific analyzers  
- **StyleCop.Analyzers** - Code style enforcement
- **Custom BallDragDrop.CodeAnalysis** - Project-specific analyzers
- **coding-standards.json** - Project-specific coding standards configuration

### Code Organization Requirements

All code must follow standardized folder structure and use required regions:

```csharp
#region Properties
// All properties here
#endregion Properties

#region Construction  
// Constructors here
#endregion Construction

#region Methods
// All methods here (each method in its own region)
#endregion Methods
```

## Testing

The application includes comprehensive test coverage:

- Unit tests for individual components
- Integration tests for component interactions  
- Performance tests for rendering and physics
- End-to-end tests for complete functionality

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- This project was created as a demonstration of WPF, MVVM, and physics-based animations.
## Recent U
pdates - Custom Cursor System

### Implementation Summary

I've successfully implemented a comprehensive cursor management system for the Ball Drag Drop application and resolved cursor display issues with the following key features:

#### Key Accomplishments

1. **Custom Cursor System**: Created a complete PNG-to-cursor conversion system that loads custom cursor images for different interaction states
2. **State-Based Cursors**: Implemented cursors that change based on hand state (Default, Hover, Grabbing, Releasing)
3. **Fixed Cursor Application**: Resolved cursor display issues by using `Mouse.OverrideCursor` for global cursor override
4. **Robust Error Handling**: Added comprehensive error handling with fallback mechanisms
5. **Performance Optimization**: Implemented cursor caching and async loading
6. **Comprehensive Documentation**: Created detailed documentation and architecture guides

#### Technical Implementation

- **CursorImageLoader**: Handles PNG to Windows cursor conversion with validation
- **CursorManager**: Manages cursor state transitions and global cursor application using `Mouse.OverrideCursor`
- **HandStateMachine Integration**: Seamlessly integrates with the existing state machine
- **Configuration Support**: Fully configurable through appsettings.json
- **Extensive Logging**: Detailed logging for debugging and monitoring

#### Key Fixes Applied

1. **Cursor Application Method**: Changed from setting `MainWindow.Cursor` to using `Mouse.OverrideCursor` for global cursor override
2. **Compilation Errors**: Fixed multiple entry point issue by removing `Main` method from `GenerateTestCursors.cs`
3. **Interface Completion**: Added `ClearCursorOverride` method to `ICursorService` interface
4. **Recovery Mechanisms**: Updated all cursor recovery methods to use global cursor override

#### Files Created/Modified

- `Services/CursorImageLoader.cs` - Core cursor conversion functionality
- `Services/CursorManager.cs` - Cursor management with global cursor override
- `Contracts/ICursorService.cs` - Service interface with clear override method
- `Models/CursorConfiguration.cs` - Configuration model
- `Resources/Cursors/` - Cursor image assets (default.png, hover.png, grabbing.png, releasing.png)
- `docs/cursor-system-architecture.md` - Comprehensive system documentation
- `docs/cursor-handling-guide.md` - Detailed cursor handling guide
- `GenerateTestCursors.cs` - Fixed compilation issue by removing Main method

The system is now production-ready with working custom cursors, extensive error handling, performance optimizations, and comprehensive logging. Custom cursors provide visual feedback during ball interactions, significantly enhancing the user experience.

### Cursor System Documentation

For detailed information about the cursor system, see:
- `docs/cursor-system-architecture.md` - System architecture and design
- `docs/cursor-handling-guide.md` - Implementation details and troubleshooting