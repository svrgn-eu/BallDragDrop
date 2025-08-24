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

### Quick Start Guide 🚀

1. **Launch the app** - Run the command above and wait for the window to appear
2. **Find the ball** - You'll see a ball in the center of the window
3. **Hover over it** - Notice how the cursor changes to indicate it's interactive
4. **Click and drag** - Grab the ball and move it around (cursor changes again!)
5. **Release to throw** - Let go while moving to throw the ball with physics
6. **Watch it bounce** - The ball will bounce realistically off the window edges
7. **Try resizing** - Resize the window and see how the ball maintains its relative position

**Pro Tip**: Pay attention to the cursor changes - they provide instant feedback about what you can do!

### Usage

- **Drag the ball**: Click and hold the left mouse button on the ball, then move the mouse.
- **Throw the ball**: Drag the ball and release the mouse button while moving to throw it.
- **Grab the ball in motion**: Click on the ball while it's moving to stop and grab it.
- **Resize the window**: The ball will maintain its relative position within the window.

### Interactive Cursor Guide 🖱️

The application features dynamic cursors that change based on your interaction:

| Action | Cursor Change | What It Means |
|--------|---------------|---------------|
| **Move mouse around** | Default cursor | Normal navigation mode |
| **Hover over ball** | Changes to hover cursor | Ball is ready to be grabbed |
| **Click and hold ball** | Changes to grabbing cursor | Ball is being dragged |
| **Release ball** | Briefly shows releasing cursor | Ball has been released and is in motion |

**Tip**: Watch the cursor changes to get immediate visual feedback about your interaction state with the ball!

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
## Custom Cursor System ✨

The application now features a sophisticated custom cursor system that provides visual feedback during ball interactions. The cursor changes dynamically based on your interaction state with the ball.

### Cursor States

| State | Description |
|-------|-------------|
| **Default** | Standard cursor when not interacting with the ball |
| **Hover** | Cursor when hovering over the ball |
| **Grabbing** | Cursor when clicking and dragging the ball |
| **Releasing** | Brief cursor shown when releasing the ball |

### How to Use Custom Cursors

1. **Hover Over Ball**: Move your mouse over the ball to see the hover cursor
2. **Grab the Ball**: Click and hold on the ball to see the grabbing cursor
3. **Drag and Release**: Move the ball around and release to see the releasing cursor
4. **Normal Navigation**: Move away from the ball to return to the default cursor

### Configuration

Custom cursors can be configured in `appsettings.json`:

```json
{
  "CustomCursorsEnabled": true,
  "CursorPaths": {
    "Default": "Resources/Cursors/default.png",
    "Hover": "Resources/Cursors/hover.png",
    "Grabbing": "Resources/Cursors/grabbing.png",
    "Releasing": "Resources/Cursors/releasing.png"
  }
}
```

### Troubleshooting

If custom cursors are not appearing:
1. Ensure PNG files exist in `Resources/Cursors/`
2. Check that `CustomCursorsEnabled` is set to `true` in configuration
3. Verify the application has read access to the cursor files
4. Check the application logs for cursor loading errors

## Documentation 📚

### Core Documentation
- **[README.md](README.md)** - This file, main project documentation and user guide
- **[Editor Config](.editorconfig)** - Code formatting and style configuration

### Architecture & Design
- **[Cursor System Architecture](docs/cursor-system-architecture.md)** - Detailed system architecture and design patterns
- **[Cursor Handling Guide](docs/cursor-handling-guide.md)** - Implementation details, troubleshooting, and best practices

### Development & CI/CD
- **[GitHub Actions Workflow](.github/workflows/ci.yml)** - Continuous integration configuration
- **[GitLab CI Configuration](.gitlab-ci.yml)** - GitLab continuous integration setup
- **[Git Ignore](.gitignore)** - Git ignore patterns and rules

### Code Quality & Standards
- **[Coding Standards](coding-standards.json)** - Project-wide coding standards and rules
- **[Build Scripts](src/BallDragDrop/Build/)** - MSBuild targets and validation scripts
- **[Code Analysis](src/BallDragDrop.CodeAnalysis/)** - Custom code analyzers and rules

### Configuration Files
- **[App Settings](src/BallDragDrop/appsettings.json)** - Application configuration
- **[Log4Net Config](src/BallDragDrop/log4net.config)** - Logging configuration
- **[Directory Build Props](src/BallDragDrop/Directory.Build.props)** - MSBuild properties

## Recent Updates - Custom Cursor System Implementation ✅

### What's New

🎯 **Working Custom Cursors**: The application now displays custom cursors that change based on ball interaction state  
🔧 **Fixed All Issues**: Resolved cursor display problems and compilation errors  
📖 **Complete Documentation**: Added comprehensive guides and architecture documentation  
⚡ **Performance Optimized**: Cursor caching and efficient PNG-to-cursor conversion  
🛡️ **Robust Error Handling**: Multiple fallback mechanisms ensure the application always works  

### Technical Achievements

- **Global Cursor Override**: Uses `Mouse.OverrideCursor` for application-wide cursor changes
- **PNG to Cursor Conversion**: Real-time conversion of PNG images to Windows cursor format
- **State Machine Integration**: Seamlessly integrates with existing ball state management
- **Configuration Driven**: Fully configurable cursor paths and behavior
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

### Files Added/Modified

#### Core Implementation
- `Services/CursorImageLoader.cs` - PNG to cursor conversion engine
- `Services/CursorManager.cs` - Cursor state management and application
- `Contracts/ICursorService.cs` - Service interface definition
- `Models/CursorConfiguration.cs` - Configuration model

#### Resources & Assets
- `Resources/Cursors/default.png` - Default cursor image
- `Resources/Cursors/hover.png` - Hover state cursor
- `Resources/Cursors/grabbing.png` - Grabbing state cursor  
- `Resources/Cursors/releasing.png` - Releasing state cursor

#### Documentation
- `docs/cursor-system-architecture.md` - System architecture guide
- `docs/cursor-handling-guide.md` - Implementation and troubleshooting guide

#### Utilities
- `GenerateTestCursors.cs` - Utility for generating test cursor images
- `Services/TestCursorGenerator.cs` - Test cursor generation service