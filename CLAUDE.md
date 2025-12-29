# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FTPSheep.NET is a command-line deployment tool for .NET developers that automates building and deploying ASP.NET applications to FTP servers. It integrates with Visual Studio publish profiles and the .NET build toolchain while providing concurrent uploads, progress tracking, and secure credential management.

**Status:** In active development
**Target Platform:** Windows (V1), .NET 8.0
**Development Branch:** `dev`
**Main Branch:** `main`

## Solution Architecture

The solution follows a layered architecture with clear separation of concerns:

### Project Structure

```
src/
├── FTPSheep.CLI/                    # Command-line interface (entry point)
├── FTPSheep.Core/                   # Core domain models, services, orchestration
├── FTPSheep.BuildTools/             # Build integration (MSBuild, dotnet CLI)
├── FTPSheep.Protocols/              # FTP/SFTP client implementations
├── FTPSheep.Utilities/              # Shared utility classes and helpers
├── FTPSheep.VisualStudio.Shared/    # Shared code for VS extensions
└── FTPSheep.VisualStudio.Modern/    # Visual Studio 2026 extension

tests/
├── FTPSheep.Tests/                  # Unit tests
└── FTPSheep.IntegrationTests/       # Integration tests
```

### Dependency Hierarchy

```
CLI → Core → {BuildTools, Protocols}
VisualStudio → Shared → Core → {BuildTools, Protocols, Utilities}
```

**Critical:** Never create circular dependencies. `BuildTools` and `Protocols` are lower-level libraries and should NOT reference `Core`. If they need exceptions or models, create local copies within their own project.

### Key Architectural Patterns

**Deployment Orchestration:**
- `DeploymentCoordinator` orchestrates the 9-stage deployment workflow
- Event-driven architecture using `StageChanged` and `ProgressUpdated` events
- Stage flow: Load Profile → Build → Connect → Summary → Upload app_offline → Upload Files → Cleanup → Delete app_offline → Record History
- Stages are conditional based on `DeploymentOptions` (app_offline, cleanup mode)

**State Management:**
- `DeploymentState` tracks real-time progress (files uploaded, sizes, stage timestamps)
- `DeploymentResult` represents final outcome with factory methods (FromSuccess, FromFailure, FromCancellation)
- `DeploymentStage` enum defines all 13 workflow stages

**Error Handling:**
- Custom exceptions in each layer (avoid circular dependencies):
  - `FTPSheep.Core.Exceptions.*` for Core
  - `FTPSheep.BuildTools.Exceptions.*` for BuildTools
  - `FTPSheep.Protocols.Exceptions.*` for Protocols
- All exceptions should include contextual information (file paths, hosts, ports)
- `IsTransient` flag on exceptions enables retry logic

**Configuration Management:**
- Profiles stored in `%APPDATA%\.ftpsheep\profiles\` as JSON
- Global config in `%APPDATA%\.ftpsheep\config.json`
- Credentials encrypted using Windows DPAPI (DpapiEncryptionService)
- Profile files use `.ftpsheep` extension (warning shown for incorrect extensions)
- Supports relative paths and current directory profile loading
- Two model types:
  - **Flat models** (`DeploymentProfile`) for simple property access
  - **Nested models** (`Connection`, `Build`) for logical grouping
  - Backward compatibility via `[Obsolete]` delegating properties

## Common Commands

### Build and Test

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/FTPSheep.Core/FTPSheep.Core.csproj

# Run all tests
dotnet test

# Run tests for specific project
dotnet test tests/FTPSheep.Tests/FTPSheep.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~FTPSheep.Tests.Core.DeploymentOrchestrationTests"

# Run single test method
dotnet test --filter "FullyQualifiedName~FTPSheep.Tests.Core.DeploymentOrchestrationTests.DeploymentState_ProgressPercentage_CalculatesCorrectly"

# Clean build artifacts
dotnet clean
```

### Visual Studio Extension Development

Extension supports Visual Studio 2026, built with VisualStudio.Extensibility. It has no v2022 support. 

Documentation:
https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/get-started/oop-extensibility-model-overview?view=visualstudio
https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/dotnet-management-overview?view=vs-2022

```bash
# Build the VSIX package
dotnet build src\FTPSheep.VisualStudio.Modern\FTPSheep.VisualStudio.Modern.csproj

# The VSIX file will be in:
# src\FTPSheep.VisualStudio.Modern\bin\Debug\FTPSheep.VisualStudio.Modern.vsix

# Test the extension in VS experimental instance
# Build the project, then press F5 in Visual Studio
# This launches VS with /rootsuffix Exp argument

# Install VSIX manually for testing
# Double-click the .vsix file or use:
# VSIXInstaller.exe /uninstall:FTPSheep.VisualStudio.8f9c3e4a-1b2d-4c5e-9a7b-3f8e6d2c1a4b
# VSIXInstaller.exe FTPSheep.VisualStudio.Modern.vsix

# Clean VSIX build artifacts
dotnet clean src/FTPSheep.VisualStudio.Modern/FTPSheep.VisualStudio.Modern.csproj
```

### Git Workflow

**Branching:**
- Development happens on `dev` branch
- `main` is for releases only
- Always commit to `dev`, never directly to `main`

**Commit Messages:**
Use conventional commit format:
```
feat: implement Section 5.3 - Deployment Orchestration
fix: update test for DeploymentResult changes
docs: update plan.md to mark Section 4.1 as completed
```

## Implementation Guidelines

### When Adding New Features

1. **Check the Plan:** Review `docs/plan.md` to understand requirements and mark sections complete
2. **Avoid Circular Dependencies:** Lower-level projects (BuildTools, Protocols) must NOT reference Core
3. **Create Local Exceptions:** If a lower-level project needs exceptions, create them locally (e.g., `BuildTools/Exceptions/BuildException.cs`)
4. **Write Tests First:** Aim for comprehensive unit test coverage (we have 600+ tests)
5. **Use Async/Await:** All I/O operations should be async with CancellationToken support
6. **Factory Methods:** Use static factory methods for complex object creation (e.g., `DeploymentResult.FromSuccess()`)

### Exception Handling Pattern

```csharp
// In Protocols project - create local exception
namespace FTPSheep.Protocols.Exceptions;
public class FtpException : Exception
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool IsTransient { get; set; }
}

// In service - wrap and add context
catch (Exception ex)
{
    throw new FtpException($"Failed to upload {localPath}", ex)
    {
        Host = _config.Host,
        Port = _config.Port,
        IsTransient = IsTransientError(ex)
    };
}
```

### Model Design Patterns

**Flat Properties with Nested Grouping:**
```csharp
public class DeploymentProfile
{
    // Nested models for organization
    public Connection Connection { get; set; } = new();
    public Build Build { get; set; } = new();

    // Backward-compatible flat properties
    [Obsolete("Use Connection.Host")]
    public string? Server
    {
        get => Connection.Host;
        set => Connection.Host = value ?? string.Empty;
    }
}
```

### Testing Patterns

**Unit Test Structure:**
```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var state = new DeploymentState { TotalFiles = 100, FilesUploaded = 50 };

    // Act
    var progress = state.ProgressPercentage;

    // Assert
    Assert.Equal(50.0, progress);
}
```

**Test Organization:**
Use regions to group related tests:
```csharp
#region DeploymentState Tests
// All DeploymentState tests here
#endregion

#region DeploymentResult Tests
// All DeploymentResult tests here
#endregion
```

## Key Services and Components

### Core Services

- **DeploymentCoordinator** (`FTPSheep.Core.Services`): Orchestrates entire deployment workflow
- **JsonConfigurationService** (`FTPSheep.Core.Services`): Loads/saves global configuration
- **JsonProfileRepository** (`FTPSheep.Core.Services`): Manages deployment profiles
- **DpapiEncryptionService** (`FTPSheep.Core.Services`): Encrypts credentials using Windows DPAPI
- **JsonDeploymentHistoryService** (`FTPSheep.Core.Services`): Records deployment history
- **ProfileService** (`FTPSheep.Core.Services`): High-level profile management with validation

### BuildTools Services

- **ProjectFileParser** (`FTPSheep.BuildTools.Services`): Parses .csproj/.vbproj/.fsproj files
- **ProjectTypeClassifier** (`FTPSheep.BuildTools.Services`): Identifies project types (ASP.NET, Blazor, etc.)
- **BuildToolLocator** (`FTPSheep.BuildTools.Services`): Finds MSBuild and dotnet CLI
- **MSBuildWrapper** (`FTPSheep.BuildTools.Services`): Executes MSBuild operations
- **DotnetCliExecutor** (`FTPSheep.BuildTools.Services`): Executes dotnet CLI commands
- **PublishOutputScanner** (`FTPSheep.BuildTools.Services`): Scans and validates build output

### Protocol Services

- **FtpClientService** (`FTPSheep.Protocols.Services`): Full-featured async FTP client using FluentFTP
- **ConcurrentUploadEngine** (`FTPSheep.Protocols.Services`): Manages concurrent file uploads with connection pooling
- **FtpClientFactory** (`FTPSheep.Protocols.Services`): Creates FTP/FTPS clients based on configuration

### Visual Studio Extension Services

- **VsDeploymentOrchestrator** (`FTPSheep.VisualStudio.Services`): VS-specific deployment orchestration
- **VsOutputWindowService** (`FTPSheep.VisualStudio.Services`): Writes to VS output window
- **VsStatusBarService** (`FTPSheep.VisualStudio.Services`): Updates VS status bar
- **VsErrorListService** (`FTPSheep.VisualStudio.Services`): Manages VS error list
- **VsSolutionService** (`FTPSheep.VisualStudio.Services`): Interacts with VS solution/projects
- **ProjectAssociationService** (`FTPSheep.VisualStudio.Services`): Associates projects with profiles

## Visual Studio Extension Architecture

### Extension Overview

The Visual Studio extension provides integrated deployment directly from the IDE:
- **Target VS Version:** Visual Studio 2022 (17.0+)
- **Extension Type:** Async package (Community.VisualStudio.Toolkit.17)
- **UI Integration:** Commands, tool windows, status bar, output window

### Extension Components

**FTPSheep.VisualStudio** (Main VSIX Project):
- Targets `net8.0-windows` with WPF support
- Uses Community.VisualStudio.Toolkit for modern VS integration
- Implements `FTPSheepPackage` as the main entry point
- References Core, BuildTools, Protocols, and Utilities

**FTPSheep.VisualStudio.Shared**:
- Shared code between different VS extension versions
- Common models, interfaces, and utilities
- Platform-agnostic VS integration code

**FTPSheep.VisualStudio.Modern**:
- Modern VS API implementations
- Future-proofing for newer VS versions

### Extension Development Guidelines

1. **Async Package Pattern:** All VS operations must be async and run on background threads
2. **ThreadHelper Usage:** Use `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()` for UI thread operations
3. **Service Integration:** Leverage existing Core services through dependency injection
4. **Output Window:** Use `VsOutputWindowService` for deployment progress and messages
5. **Error Handling:** Use `VsErrorListService` to report build/deployment errors
6. **Testing:** Test in experimental instance (`/rootsuffix Exp`) to avoid affecting main VS installation

### VSIX Manifest Details

- **Extension ID:** `FTPSheep.VisualStudio.8f9c3e4a-1b2d-4c5e-9a7b-3f8e6d2c1a4b`
- **Supported Editions:** Community, Professional, Enterprise
- **Architecture:** amd64
- **Prerequisites:** .NET Framework 4.5+, VS Core Editor

## Important Implementation Notes

### Recent Improvements (Latest)

- ✅ Visual Studio 2022 extension (first version completed)
- ✅ Profile loading improvements:
  - Relative path support for profiles
  - Current directory profile loading (just filename, no path)
  - Warning for incorrect profile file extensions (non-.ftpsheep)
- ✅ Credentials loading fixes for relative profile paths
- ✅ FTP upload functionality fully operational
- ✅ ConcurrentUploadEngine integrated with DeploymentCoordinator

### Completed Sections (per docs/plan.md)

- ✅ Section 2.1: Core Domain Models
- ✅ Section 2.2: Configuration Management
- ✅ Section 2.5: Error Handling
- ✅ Section 3.1: Project Type Detection
- ✅ Section 3.2: MSBuild Integration
- ✅ Section 3.3: dotnet CLI Integration (implemented in 3.2)
- ✅ Section 3.4: Build Output Processing
- ✅ Section 4.1: FTP Client Implementation
- ✅ Section 4.3: Protocol Abstraction Layer
- ✅ Section 4.4: Concurrent Upload Engine
- ✅ Section 4.5: Upload Failure and Retry Logic
- ✅ Section 5.1: Direct Deployment Strategy Implementation
- ✅ Section 5.2: Pre-Deployment Validation
- ✅ Section 5.3: Deployment Orchestration
- ✅ Section 6.1: Publish Profile Parser
- ✅ Section 6.3: Profile Import and Conversion

### Placeholder Implementations

All major `DeploymentCoordinator` methods have been implemented:
- ✅ `LoadProfileAsync()` - fully integrated with ProfileService
- ✅ `BuildProjectAsync()` - uses BuildService
- ✅ `ConnectToServerAsync()` - uses FtpClientService and ConcurrentUploadEngine
- ✅ `UploadFilesAsync()` - fully operational with concurrent uploads
- ✅ `UploadAppOfflineAsync()` - implemented
- ✅ `DeleteAppOfflineAsync()` - implemented
- ✅ `RecordHistoryAsync()` - integrated with JsonDeploymentHistoryService
- ✅ `CleanupObsoleteFilesAsync()` - implemented with FileComparisonService

### Third-Party Libraries

**Core Libraries:**
- **FluentFTP** (v53.0.2): Async FTP client with comprehensive feature set
- **xUnit**: Testing framework
- **Moq**: Mocking framework for unit tests
- **.NET 8.0**: Target framework for all projects

**Visual Studio Extension:**
- **Community.VisualStudio.Toolkit.17** (v17.0.487+): Modern VS SDK wrapper
- **Microsoft.VisualStudio.SDK** (v17.11.40252+): Core VS extensibility APIs
- **Microsoft.VSSDK.BuildTools** (v17.11.436+): VSIX build tools

**Other:**
- **Spectre.Console.Cli**: Command-line parsing for CLI
- **Spectre.Console**: Rich console UI
- **System.Text.Json**: JSON serialization for profiles and config
- **Microsoft.Extensions.Logging**: Logging framework

## Development Workflow

### Implementing a New Section

1. Read the requirements from `docs/plan.md`
2. Create necessary models in appropriate project
3. Implement services with full async support
4. Write comprehensive unit tests (aim for 20-30+ tests per section)
5. Run all tests to ensure nothing broke
6. Update `docs/plan.md` with ✅ and implementation notes
7. Commit with descriptive message following conventional commit format
8. Push to `dev` branch

### When Tests Fail

- Read the error message carefully - it shows line numbers
- Check if changes to models require updating tests in other files
- Look for properties that were renamed or have new requirements
- Ensure all `SizeUploaded` vs `TotalBytes` distinctions are correct

### Working on Visual Studio Extension

1. Open solution in Visual Studio 2022
2. Set `FTPSheep.VisualStudio` as startup project
3. Press F5 to launch VS experimental instance
4. Test extension functionality in experimental instance
5. Check Output Window for debugging messages
6. Use Debug → Attach to Process to debug running instances
7. Reset experimental instance if needed: `devenv /rootsuffix Exp /resetSettings`

## File References

When referencing code locations in responses, use the format:
```
src/FTPSheep.Core/Models/DeploymentState.cs:45
```

This helps users navigate directly to the relevant code.

## Additional Resources

- **PRD:** `docs/prd.md` - Product requirements and user personas
- **Plan:** `docs/plan.md` - Detailed development plan with checkboxes
- **Test Guide:** `tests/README.md` - Comprehensive testing documentation
- **Main README:** `README.md` - Project overview and quick start
- **VS Extension Manifest:** `src/FTPSheep.VisualStudio/source.extension.vsixmanifest` - Extension metadata
