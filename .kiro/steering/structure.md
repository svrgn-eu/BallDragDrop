# Project Structure

## Solution Organization
```
src/
├── BallDragDrop/              # Main WPF application
├── BallDragDrop.Tests/        # Unit and integration tests
└── BallDragDrop.CodeAnalysis/ # Custom code analyzers
```

## Main Application Structure (src/BallDragDrop/)

### Required Folder Organization
Following the coding standards defined in `coding-standards.json`, all code must be organized into these specific folders:

- **Contracts/** - Interfaces and abstract classes (IBallStateMachine, ILogService, etc.)
- **Bootstrapper/** - Application bootstrapping and DI configuration
- **Models/** - Data models and business logic (BallModel, PhysicsEngine, etc.)
- **Services/** - Application services and utilities
- **ViewModels/** - MVVM view models
- **Views/** - WPF views and user controls
- **Converters/** - XAML value converters (ColorToBrushConverter, OffsetConverter, etc.)
- **Commands/** - Command implementations (RelayCommand, etc.)

### Additional Folders
- **Build/** - MSBuild targets and validation scripts
- **CodeQuality/** - Code quality reports and metrics
- **Resources/** - Application resources (images, etc.)
- **logs/** - Runtime log files

## File Organization Rules

### Class Regions
All classes must use standardized regions:
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

### Method Regions
Each method must be wrapped in its own region:
```csharp
#region MethodName
public void MethodName()
{
    // Implementation
}
#endregion MethodName
```

### Naming Conventions
- **Classes, Methods, Properties**: PascalCase
- **Private fields**: camelCase with underscore prefix (`_fieldName`)
- **Interfaces**: PascalCase with "I" prefix (`IServiceName`)

### Documentation Requirements
- XML documentation required for all public members
- Must include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags as applicable

## Configuration Files
- **appsettings.json** - Application configuration
- **log4net.config** - Logging configuration
- **coding-standards.json** - Project-specific coding standards
- **Directory.Build.props** - MSBuild properties and warning suppressions