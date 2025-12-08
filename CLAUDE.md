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
├── FTPSheep.CLI/          # Command-line interface (entry point)
├── FTPSheep.Core/         # Core domain models, services, orchestration
├── FTPSheep.BuildTools/   # Build integration (MSBuild, dotnet CLI)
└── FTPSheep.Protocols/    # FTP/SFTP client implementations

tests/
├── FTPSheep.Tests/             # Unit tests
└── FTPSheep.IntegrationTests/  # Integration tests
```

### Dependency Hierarchy

```
CLI → Core → {BuildTools, Protocols}
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
4. **Write Tests First:** Aim for comprehensive unit test coverage (we have 415+ tests)
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

### BuildTools Services

- **ProjectFileParser** (`FTPSheep.BuildTools.Services`): Parses .csproj/.vbproj/.fsproj files
- **ProjectTypeClassifier** (`FTPSheep.BuildTools.Services`): Identifies project types (ASP.NET, Blazor, etc.)
- **BuildToolLocator** (`FTPSheep.BuildTools.Services`): Finds MSBuild and dotnet CLI
- **MSBuildWrapper** (`FTPSheep.BuildTools.Services`): Executes MSBuild operations
- **DotnetCliExecutor** (`FTPSheep.BuildTools.Services`): Executes dotnet CLI commands
- **PublishOutputScanner** (`FTPSheep.BuildTools.Services`): Scans and validates build output

### Protocol Services

- **FtpClientService** (`FTPSheep.Protocols.Services`): Full-featured async FTP client using FluentFTP

## Important Implementation Notes

### Completed Sections (per docs/plan.md)

- ✅ Section 2.1: Core Domain Models
- ✅ Section 2.2: Configuration Management
- ✅ Section 2.5: Error Handling
- ✅ Section 3.1: Project Type Detection
- ✅ Section 3.2: MSBuild Integration
- ✅ Section 3.3: dotnet CLI Integration (implemented in 3.2)
- ✅ Section 3.4: Build Output Processing
- ✅ Section 4.1: FTP Client Implementation
- ✅ Section 5.3: Deployment Orchestration

### Placeholder Implementations

The `DeploymentCoordinator` has placeholder methods for stages that will be implemented when dependencies are ready:
- `LoadProfileAsync()` - Awaiting profile management completion
- `BuildProjectAsync()` - Will use BuildTools services
- `ConnectToServerAsync()` - Will use FtpClientService
- `UploadFilesAsync()` - Awaiting concurrent upload engine (Section 4.4)
- `UploadAppOfflineAsync()`, `DeleteAppOfflineAsync()` - Awaiting Section 5.1
- `CleanupObsoleteFilesAsync()` - Awaiting Section 5.1

### Third-Party Libraries

- **FluentFTP** (v53.0.2): Async FTP client with comprehensive feature set
- **xUnit**: Testing framework
- **Moq**: Mocking framework for unit tests
- **.NET 8.0**: Target framework for all projects

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
