# Technology Stack

## Framework & Runtime
- .NET 9.0 with Windows-specific features
- WPF (Windows Presentation Foundation) for UI
- C# with nullable reference types enabled
- Implicit usings enabled

## Key Dependencies
- **Microsoft.Extensions.DependencyInjection** - Dependency injection container
- **Microsoft.Extensions.Configuration** - Configuration management
- **Config.Net** - Additional configuration utilities
- **log4net** - Logging framework
- **Castle.Core** - Interception and proxy capabilities
- **Stateless** - State machine implementation

## Code Quality & Analysis
- **Microsoft.CodeAnalysis.Analyzers** - Core code analysis
- **Microsoft.CodeAnalysis.NetAnalyzers** - .NET-specific analyzers
- **StyleCop.Analyzers** - Code style enforcement
- **Custom BallDragDrop.CodeAnalysis** - Project-specific analyzers

## Build & Development Commands

### Build
```cmd
dotnet build
```

### Run Application
```cmd
dotnet run --project src/BallDragDrop
```

### Run Tests
```cmd
dotnet test
```

### Code Quality Validation
```cmd
# PowerShell
src/BallDragDrop/Build/ValidateCodingStandards.ps1

# Batch
src/BallDragDrop/Build/RunCodingStandardsValidation.bat
```

## Build Configuration
- Enhanced coding standards enforcement enabled
- XML documentation generation required
- Custom MSBuild targets for coding standards validation
- Build fails on critical coding standard violations