# FTPSheep.NET Development Plan

## Overview

FTPSheep.NET is a command-line deployment tool designed specifically for .NET developers who build and deploy ASP.NET applications to servers using FTP protocol. The tool streamlines the deployment workflow by integrating with the existing .NET build toolchain (msbuild/dotnet.exe) and Visual Studio publish profiles, while adding enhanced deployment capabilities including concurrent uploads, progress tracking, and secure credential management.

**Target Platform:** Windows (V1)
**Target Framework:** .NET 6.0+ (for the CLI tool itself)
**Deployment Targets:** .NET Framework 4.x, .NET Core, .NET 5+

## 1. Project Setup

### 1.1 Repository and Solution Structure
- [ ] Initialize Git repository with appropriate .gitignore for .NET projects
  - Exclude bin/, obj/, .vs/, user-specific files
  - Include README.md, LICENSE, .editorconfig
- [ ] Create solution structure with organized project layout
  - Main CLI project (FTPSheep.CLI)
  - Core library project (FTPSheep.Core)
  - FTP/SFTP integration project (FTPSheep.Protocols)
  - Build integration project (FTPSheep.BuildTools)
  - Unit tests project (FTPSheep.Tests)
  - Integration tests project (FTPSheep.IntegrationTests)
- [ ] Configure solution-level settings
  - Target .NET 6.0+ for CLI tool
  - Set up consistent code style and formatting rules
  - Configure nullable reference types
- [ ] Set up project references and dependencies
  - Establish dependency hierarchy (CLI -> Core -> Protocols/BuildTools)
  - Add NuGet package references (see dependencies section)

### 1.2 Development Environment Configuration
- [ ] Create .editorconfig for consistent coding standards
  - Indentation, line endings, naming conventions
  - C# specific formatting rules
- [ ] Create local development setup documentation
  - Prerequisites (SDK versions, tools required)
  - Build and run instructions
  - Testing setup

### 1.4 Third-Party Dependencies
- [ ] Research and select FTP library
  - Evaluate FluentFTP (recommended for robust FTP support)
  - Test compatibility with various FTP servers
  - Verify concurrent connection support
- [ ] Research and select SFTP library
  - Evaluate SSH.NET (recommended for SFTP support)
  - Test SSH key authentication support
  - Verify compatibility with common SFTP servers
- [ ] Select command-line parsing library
  - Evaluate System.CommandLine or Spectre.Console.Cli
  - Ensure support for subcommands, options, arguments
  - Verify help generation capabilities
- [ ] Select console UI/progress library
  - Evaluate Spectre.Console for rich console output
  - Verify progress bar, color support, formatting capabilities
- [ ] Add logging framework
  - NLog via Microsoft.Extensions.Logging
  - Configure console and file sinks
- [ ] Add JSON/XML serialization libraries
  - System.Text.Json for profile storage
  - XML parsing for .pubxml files

### 1.5 Initial Project Scaffolding
- [x] Create basic CLI entry point with version and help commands
  - Implement `ftpsheep --version`
  - Implement `ftpsheep --help`
  - Set up command routing infrastructure
- [x] Set up logging infrastructure
  - Configure log levels and output formats
  - Create console logger with color support
  - Set up file logging for debugging
- [x] Create core domain models
  - DeploymentProfile class
  - DeploymentResult class
  - BuildConfiguration class
  - ServerConnection class


### 1.6 Testing Infrastructure
- [x] Set up unit testing framework
  - xUnit
  - Configure test project structure
- [x] Set up mocking framework
  - Moq for dependency mocking
- [x] Set up integration testing environment
  - Test FTP server setup (local or containerized)  
  - Sample .NET projects for build testing
- [x] Create test data and fixtures
  - Sample Visual Studio publish profiles
  - Sample deployment configurations
  - Mock server responses

## 2. Backend Foundation

### 2.1 Core Domain Models and Interfaces
- [ ] Define DeploymentProfile model
  - Properties: Name, Server, Port, Protocol, Username, RemotePath, ProjectPath
  - Properties: Concurrency, Timeout, RetryCount, BuildConfiguration
  - Properties: ExclusionPatterns, CleanupMode, App_OfflineEnabled
  - Serialization/deserialization support
- [ ] Define DeploymentResult model
  - Properties: Success, StartTime, EndTime, Duration
  - Properties: FilesUploaded, TotalSize, AverageSpeed
  - Properties: ErrorMessages, WarningMessages
- [ ] Define IFtpClient interface
  - Methods: Connect, Disconnect, UploadFile, DeleteFile, ListFiles
  - Methods: CreateDirectory, TestConnection, GetServerInfo
  - Support for both FTP and SFTP implementations
- [ ] Define IBuildTool interface
  - Methods: Build, Publish, GetProjectInfo
  - Support for both MSBuild and dotnet CLI implementations
- [ ] Define ICredentialStore interface
  - Methods: SaveCredentials, LoadCredentials, DeleteCredentials
  - Encryption/decryption abstraction
- [ ] Define IProgressTracker interface
  - Methods: ReportProgress, UpdateStatus, ReportFileUploaded
  - Event-based progress updates

### 2.2 Configuration and Settings Management
- [ ] Implement configuration file structure
  - Global configuration file location (~/.ftpsheep/config.json or %APPDATA%)
  - Profile storage location and structure
  - Default settings and overrides
- [ ] Create configuration loader
  - Read global configuration
  - Merge with profile-specific settings
  - Apply command-line overrides
- [ ] Implement profile persistence
  - Save profiles to JSON files
  - Load profiles by name or path
  - Profile validation on load
- [ ] Create settings validation
  - Validate server URLs and ports
  - Validate file paths and patterns
  - Validate numeric ranges (concurrency, timeouts)

### 2.3 Credential Management and Encryption
- [ ] Implement Windows DPAPI encryption wrapper
  - Encrypt string data using DPAPI
  - Decrypt protected data
  - Handle encryption errors gracefully
- [ ] Create credential storage service
  - Save encrypted credentials to profile
  - Load and decrypt credentials
  - Prompt for credentials if not stored
  - Support environment variable credentials (FTP_USERNAME, FTP_PASSWORD)
- [ ] Implement secure credential handling
  - Never log credentials in plain text
  - Clear credentials from memory after use
  - Validate credential encryption/decryption

### 2.4 Logging and Diagnostics
- [ ] Implement structured logging system
  - Configure log levels (Minimal, Normal, Verbose, Debug)
  - Timestamp all log entries
  - Support colored console output
- [ ] Create deployment history storage
  - SQLite database or JSON file storage
  - Record timestamp, profile, result, duration, file counts
  - Store error messages for failed deployments
- [ ] Implement log file management
  - Rotate log files by size or date
  - Store logs in appropriate application data folder
  - Provide command to view recent logs

### 2.5 Error Handling and Recovery
- [ ] Define custom exception hierarchy
  - BuildException, ConnectionException, AuthenticationException
  - DeploymentException, ConfigurationException
- [ ] Implement retry logic framework
  - Configurable retry count and backoff strategy
  - Exponential backoff for transient failures
  - Don't retry permanent errors (authentication failures)
- [ ] Create error message formatting
  - User-friendly error messages with context
  - Suggestions for common issues
  - Technical details available in verbose mode
- [ ] Implement exit code handling
  - Exit code 0: Success
  - Exit code 1: General error
  - Exit code 2: Build failure
  - Exit code 3: Connection failure
  - Exit code 4: Authentication failure

## 3. Build Integration (Backend)

### 3.1 Project Type Detection
- [ ] Implement .NET project file parser
  - Read .csproj, .vbproj, .fsproj files
  - Extract TargetFramework or TargetFrameworks
  - Determine project SDK (Microsoft.NET.Sdk.Web, etc.)
- [ ] Create project type classifier
  - Detect .NET Framework (4.x) projects
  - Detect .NET Core / .NET 5+ projects
  - Identify ASP.NET project types (MVC, Blazor, Web API, Razor Pages)
- [ ] Implement build tool selector
  - Choose MSBuild for .NET Framework projects
  - Choose dotnet CLI for .NET Core/5+ projects
  - Locate build tools on system (PATH, registry, well-known locations)

### 3.2 MSBuild Integration
- [ ] Implement MSBuild tool wrapper
  - Locate msbuild.exe (Developer Command Prompt, VS installation)
  - Build command-line arguments for publish operation
  - Support for different build configurations (Debug, Release)
- [ ] Create MSBuild process executor
  - Execute msbuild with appropriate arguments
  - Capture stdout and stderr
  - Parse build output for errors and warnings
  - Detect build success or failure
- [ ] Implement publish operation for .NET Framework
  - Publish to temporary local folder
  - Support Web Application projects
  - Handle project dependencies and references

### 3.3 dotnet CLI Integration
- [ ] Implement dotnet CLI tool wrapper
  - Locate dotnet.exe on system
  - Build command-line arguments for publish operation
  - Support for different build configurations and runtime identifiers
- [ ] Create dotnet process executor
  - Execute `dotnet publish` with appropriate arguments
  - Capture stdout and stderr
  - Parse build output for errors and warnings
  - Detect build success or failure
- [ ] Implement publish operation for .NET Core/5+
  - Publish to temporary local folder
  - Support self-contained and framework-dependent deployments
  - Handle multi-targeting projects

### 3.4 Build Output Processing
- [ ] Implement publish folder scanner
  - Enumerate all files in publish output
  - Calculate total file count and size
  - Apply exclusion patterns to filter files
- [ ] Create file metadata collection
  - Capture file paths, sizes, timestamps
  - Build file upload queue
  - Sort files for optimal upload order (small files first)
- [ ] Implement build output validation
  - Verify essential files exist (web.config, assemblies)
  - Warn about missing or unexpected files
  - Check for known issues (locked files, permission problems)

### 3.5 Build Error Handling
- [ ] Implement build error parser
  - Extract error codes and messages from build output
  - Identify file and line number references
  - Categorize errors (compilation, missing dependencies, etc.)
- [ ] Create build failure reporting
  - Display build errors clearly in console
  - Suggest resolution steps for common errors
  - Provide option to view full build log
  - Abort deployment on build failure

## 4. FTP/SFTP Integration (Backend)

### 4.1 FTP Client Implementation
- [ ] Integrate FluentFTP library
  - Configure FTP client with connection settings
  - Support active and passive modes
  - Handle custom ports
- [ ] Implement FTP connection management
  - Connect to FTP server with credentials
  - Validate connection before operations
  - Handle connection timeouts
  - Implement connection pooling for concurrent uploads
- [ ] Create FTP upload operations
  - Upload single file to specified path
  - Create remote directories as needed
  - Overwrite existing files
  - Set file permissions if supported
- [ ] Implement FTP directory operations
  - List files in remote directory
  - Create nested directory structures
  - Delete files and directories
  - Verify remote path exists
- [ ] Add FTP error handling
  - Handle connection errors, timeouts, authentication failures
  - Retry transient failures
  - Provide clear error messages for FTP-specific issues

### 4.2 SFTP Client Implementation (Low Priority for V1)
- [ ] Integrate SSH.NET library
  - Configure SFTP client with connection settings
  - Support password and SSH key authentication
  - Handle custom ports
- [ ] Implement SFTP connection management
  - Connect to SFTP server with credentials
  - Validate connection before operations
  - Handle SSH host key verification
  - Implement connection pooling for concurrent uploads
- [ ] Create SFTP upload operations
  - Upload single file to specified path
  - Create remote directories as needed
  - Overwrite existing files
  - Set file permissions
- [ ] Implement SFTP directory operations
  - List files in remote directory
  - Create nested directory structures
  - Delete files and directories
  - Verify remote path exists
- [ ] Add SFTP error handling
  - Handle connection errors, timeouts, authentication failures
  - SSH key verification errors
  - Provide clear error messages for SFTP-specific issues

### 4.3 Protocol Abstraction Layer
- [ ] Create unified IFtpClient interface implementation
  - Abstract FTP and SFTP behind common interface
  - Protocol-agnostic deployment logic
- [ ] Implement protocol factory
  - Select appropriate client based on profile protocol
  - Configure client with protocol-specific settings
- [ ] Create connection validation
  - Test connection before deployment
  - Verify write permissions on remote path
  - Check server disk space if supported

### 4.4 Concurrent Upload Engine
- [ ] Implement upload queue manager
  - Queue files for concurrent upload
  - Manage multiple concurrent connections
  - Track upload progress for each file
- [ ] Create concurrent upload executor
  - Configure concurrency level (default 4-8)
  - Upload multiple files in parallel using multiple connections
  - Handle individual file upload failures
  - Coordinate completion of all uploads
- [ ] Implement upload throttling
  - Respect server connection limits
  - Configurable max concurrent connections (1-20)
  - Queue overflow handling
- [ ] Add upload performance tracking
  - Track upload speed per file
  - Calculate average upload speed
  - Estimate time remaining

### 4.5 Upload Failure and Retry Logic
- [ ] Implement file-level retry mechanism
  - Retry failed file uploads (default 3 retries)
  - Exponential backoff between retries
  - Track retry attempts per file
- [ ] Create failure recovery
  - Identify failed files after all retries exhausted
  - Report failed files to user
  - Option to retry only failed files
- [ ] Add upload verification
  - Verify file size after upload (if server supports)
  - Compare checksums if available
  - Re-upload corrupted files

## 5. Deployment Engine (Backend)

### 5.1 Direct Deployment Strategy Implementation
- [ ] Implement direct upload workflow
  - Upload all files directly to destination folder
  - Overwrite existing files in place
  - Create directory structure as needed
- [ ] Create app_offline.htm handling
  - Generate app_offline.htm file (default or custom template)
  - Upload app_offline.htm to destination root before deployment
  - Delete app_offline.htm after successful deployment
  - Keep app_offline.htm on failure (with error message if configured)
  - Option to skip app_offline.htm for non-IIS deployments
- [ ] Implement cleanup mode (optional)
  - Compare server files with published files
  - Identify obsolete files and folders
  - Apply exclusion patterns (exclude App_Data, uploads, logs, etc.)
  - Display list of files to be deleted
  - Prompt user for confirmation (unless --yes flag)
  - Delete obsolete files after upload succeeds
- [ ] Create exclusion pattern engine
  - Support glob patterns (*.log, temp/*, etc.)
  - Default exclusions for common folders (App_Data, uploads, logs)
  - User-configurable exclusion patterns in profile
  - Apply exclusions to both upload and cleanup operations

### 5.2 Pre-Deployment Validation
- [ ] Implement connection validation
  - Test FTP/SFTP connection before build
  - Verify credentials and authentication
  - Check remote path exists and is writable
  - Option to skip validation with --skip-connection-test
- [ ] Create publish folder validation
  - Verify publish folder is not empty
  - Check for required files (web.config, assemblies)
  - Calculate total deployment size
  - Warn about very large deployments
- [ ] Implement server capacity checks
  - Check available disk space on server (if supported)
  - Warn if deployment size exceeds available space
  - Estimate deployment time based on file size and connection speed

### 5.3 Deployment Orchestration
- [ ] Create deployment coordinator
  - Orchestrate entire deployment workflow
  - Build -> Validate -> Upload -> Cleanup -> Finalize
  - Handle failures at each stage
  - Maintain deployment state
- [ ] Implement deployment stages
  - Stage 1: Load profile and validate configuration
  - Stage 2: Build and publish project
  - Stage 3: Connect to server and validate connection
  - Stage 4: Display pre-deployment summary and confirm
  - Stage 5: Upload app_offline.htm (if enabled)
  - Stage 6: Upload all published files (concurrent)
  - Stage 7: Clean up obsolete files (if cleanup mode enabled)
  - Stage 8: Delete app_offline.htm (if deployment succeeded)
  - Stage 9: Record deployment history and display summary
- [ ] Add deployment state management
  - Track current stage and progress
  - Allow graceful cancellation (Ctrl+C)
  - Clean up temporary files on abort

### 5.4 Deployment Rollback Preparation (Future)
- [ ] Design rollback strategy for future versions
  - Document limitations of FTP for rollback
  - Plan for backup creation before deployment
  - Design restore mechanism
  - Note: Not implemented in V1

### 5.5 Dry-Run Mode Implementation
- [ ] Create dry-run deployment executor
  - Execute all validation steps
  - Build project (optional, can skip)
  - Validate connection and credentials
  - Display pre-deployment summary
  - Show files that would be uploaded
  - Show files that would be deleted (if cleanup mode)
  - Calculate estimated deployment time
  - Do not upload any files or make server changes
  - Exit with success if validation passes

## 6. Visual Studio Publish Profile Integration

### 6.1 Publish Profile Parser
- [ ] Implement .pubxml XML parser
  - Parse Visual Studio publish profile XML structure
  - Handle various .pubxml schema versions
  - Extract all relevant properties
- [ ] Create profile property extractor
  - Extract PublishMethod (should be FTP or SFTP)
  - Extract PublishUrl (FTP server and path)
  - Extract UserName
  - Extract WebPublishMethod
  - Extract any FTP-specific settings
- [ ] Implement profile validation
  - Verify profile is FTP or SFTP type
  - Validate extracted settings
  - Handle missing or malformed properties

### 6.2 Profile Auto-Discovery
- [ ] Implement .pubxml file search
  - Search project directory for Properties/PublishProfiles/*.pubxml
  - Recursively search common locations
  - Support custom search paths
- [ ] Create profile discovery logic
  - If exactly one profile found: auto-select and use it
  - If multiple profiles found: list profiles and prompt user to select
  - If no profiles found: guide user to create profile manually
- [ ] Implement profile listing UI
  - Display profile names with server URLs
  - Allow selection by number or name
  - Show profile details (server, protocol, path)

### 6.3 Profile Import and Conversion
- [ ] Create import command handler
  - Accept .pubxml file path as input
  - Validate file exists and is readable
- [ ] Implement profile converter
  - Convert .pubxml settings to FTPSheep.NET profile format
  - Map Visual Studio settings to tool settings
  - Preserve all relevant connection details
  - Add default FTPSheep.NET specific settings (concurrency, etc.)
- [ ] Create imported profile save
  - Prompt for FTPSheep.NET profile name
  - Save converted profile to appropriate location
  - Display confirmation with profile location
- [ ] Add import validation
  - Verify imported profile is usable
  - Test connection with imported settings
  - Prompt for any missing required information

### 6.4 Zero-Configuration First Run
- [ ] Implement zero-config deployment flow
  - `ftpsheep deploy` with no parameters triggers auto-discovery
  - Auto-discover and auto-select single .pubxml profile
  - Extract settings from discovered profile
  - Prompt only for missing information (typically just password)
  - Validate connection with provided credentials
  - Ask to save credentials for future use
  - Create and save FTPSheep.NET profile automatically
  - Proceed with deployment immediately
- [ ] Add first-run guidance
  - Clear messages explaining what tool is doing
  - Helpful prompts for user input
  - Confirmation before proceeding with deployment

## 7. CLI Interface and Commands

### 7.1 Command Framework Setup
- [ ] Implement command-line parsing infrastructure
  - Use System.CommandLine or Spectre.Console.Cli
  - Set up command routing
  - Configure global options (--verbose, --quiet, --no-color)
- [ ] Create base command handler
  - Common logging and error handling
  - Exit code management
  - Cancellation token support (Ctrl+C)

### 7.2 Deploy Command
- [ ] Implement `ftpsheep deploy` command
  - Options: --profile <name>, --yes/-y, --dry-run
  - Options: --verbose, --quiet, --concurrency <count>
  - Options: --configuration <Debug|Release>, --publish-folder <path>
  - Options: --skip-connection-test, --timeout <seconds>
  - Default behavior: auto-discover profile if not specified
- [ ] Add deploy command validation
  - Validate profile exists (if specified)
  - Validate options and argument combinations
  - Display helpful errors for invalid usage
- [ ] Create deploy command execution flow
  - Load or discover profile
  - Execute deployment orchestration
  - Display progress and results
  - Return appropriate exit code

### 7.3 Profile Management Commands
- [ ] Implement `ftpsheep profile list` command
  - Display all saved profiles
  - Show profile name, server, protocol, last used date
  - Sort alphabetically or by last used
  - Handle empty profile list gracefully
- [ ] Implement `ftpsheep profile show <name>` command
  - Display detailed profile settings
  - Show all configuration options
  - Hide credentials (show "stored" or "not stored")
  - Error if profile not found with suggestions
- [ ] Implement `ftpsheep profile create` command
  - Interactive prompts for all settings
  - Validate inputs at each step
  - Test connection before saving
  - Save profile to disk
- [ ] Implement `ftpsheep profile edit <name>` command
  - Interactive editing of existing profile
  - Display current values as defaults
  - Validate new values
  - Option to test connection after editing
- [ ] Implement `ftpsheep profile delete <name>` command
  - Confirmation prompt (unless --force)
  - Delete profile file
  - Display confirmation message

### 7.4 Import and Init Commands
- [ ] Implement `ftpsheep import <pubxml-path>` command
  - Accept path to .pubxml file
  - Parse and convert to FTPSheep.NET profile
  - Prompt for profile name
  - Save imported profile
  - Display success message
- [ ] Implement `ftpsheep init` command
  - Interactive guided setup for new users
  - Ask: import from VS or create new profile
  - Execute appropriate workflow
  - Display next steps after completion

### 7.5 History Command (Medium Priority)
- [ ] Implement `ftpsheep history` command
  - Display recent deployment history (default last 10)
  - Options: --count <n>, --profile <name>
  - Show timestamp, profile, result, duration, file count
  - Color-code success/failure
- [ ] Implement `ftpsheep history --export <path>` command
  - Export history to JSON or CSV
  - Options: --format <json|csv>, --from <date>, --to <date>
  - Display export file location

### 7.6 Help and Version Commands
- [ ] Implement `ftpsheep --help` command
  - Display general help and command list
  - Show global options
  - Display examples
  - Reference full documentation URL
- [ ] Implement `ftpsheep <command> --help` command
  - Display help for specific command
  - Show command syntax and options
  - Provide usage examples
- [ ] Implement `ftpsheep --version` command
  - Display version number (semantic versioning)
  - Show build date or commit hash
  - Display copyright and license info

## 8. Progress Tracking and Console UI

### 8.1 Console Output Infrastructure
- [ ] Implement console output manager
  - Centralize all console output
  - Support in-place updates (progress without scrolling)
  - Handle different console types (CMD, PowerShell, Terminal)
- [ ] Create color output support
  - Detect console color capability
  - Color-code message types (error=red, warning=yellow, success=green)
  - Support --no-color flag to disable colors
- [ ] Implement output formatting utilities
  - Format file sizes (KB, MB, GB)
  - Format durations (HH:MM:SS or "2m 30s")
  - Format speeds (MB/s, KB/s)
  - Format progress percentages

### 8.2 Progress Bar and Status Display
- [ ] Implement progress bar component
  - Use Spectre.Console or custom implementation
  - Display percentage completion
  - Display files uploaded vs. total (e.g., 45/120)
  - Display data uploaded vs. total (e.g., 12.3 MB / 25.6 MB)
  - Update in place without excessive scrolling
- [ ] Create upload speed tracker
  - Calculate current upload speed
  - Calculate average upload speed
  - Use moving average for smoothing
  - Display in appropriate units (KB/s, MB/s)
- [ ] Implement time remaining estimator
  - Calculate based on average upload speed
  - Display in readable format (e.g., "2m 30s remaining")
  - Show "Calculating..." initially
  - Update estimate as upload progresses

### 8.3 Pre-Deployment Summary
- [ ] Create pre-deployment summary display
  - Show profile name and destination server
  - Show total files to upload and total size
  - Show estimated deployment time
  - Show cleanup mode status (if enabled)
  - Display files to be deleted (if cleanup mode)
- [ ] Implement confirmation prompt
  - Prompt user to proceed with deployment
  - Accept 'y', 'yes', 'n', 'no' input
  - Skip prompt if --yes flag provided
  - Option to abort deployment

### 8.4 Deployment Summary
- [ ] Create deployment completion summary display
  - Show deployment result (success or failure)
  - Show profile name and destination server
  - Show total files uploaded and total size
  - Show total deployment duration
  - Show average upload speed
  - Display timestamp of completion
- [ ] Implement failure summary
  - List failed files (if any)
  - Show error messages
  - Suggest next steps or troubleshooting

### 8.5 Verbosity Levels
- [ ] Implement minimal/quiet output mode
  - Show only critical messages and final result
  - Suppress progress bars and detailed logging
  - Still display errors
  - Suitable for CI/CD usage
- [ ] Implement normal output mode (default)
  - Show standard progress and summaries
  - Display key deployment stages
  - Show progress bar and time estimates
  - Balanced detail level
- [ ] Implement verbose/detailed output mode
  - Show file-level operations
  - Display all FTP commands and responses
  - Show detailed build output
  - Include timestamps on all messages
- [ ] Implement debug output mode
  - Show all internal operations
  - Display configuration values
  - Show decision logic
  - Maximum detail for troubleshooting

### 8.6 Status Messages and Notifications
- [ ] Create status message templates
  - "Building project..."
  - "Connecting to server..."
  - "Uploading files..."
  - "Cleaning up obsolete files..."
  - "Deployment complete!"
- [ ] Implement stage transition messages
  - Clear indication when moving between deployment stages
  - Display what operation is currently running
  - Show completion of each stage

## 9. Integration and End-to-End Features

### 9.1 Full Deployment Flow Integration
- [ ] Integrate all components for complete deployment
  - Profile loading -> Build -> Connect -> Upload -> Cleanup -> Complete
  - Ensure proper error propagation between stages
  - Validate data flow between components
- [ ] Test end-to-end deployment scenarios
  - Deploy .NET Framework 4.x project
  - Deploy .NET Core 3.1 project
  - Deploy .NET 5/6/7/8 project
  - Deploy Blazor Server project
  - Deploy Blazor WebAssembly project
  - Deploy Web API project
- [ ] Verify deployment with various configurations
  - Different concurrency levels
  - With and without cleanup mode
  - With and without app_offline.htm
  - Different build configurations (Debug, Release)

### 9.2 Profile Management Integration
- [ ] Test profile create -> save -> load -> use workflow
  - Create profile via command
  - Save profile to disk
  - Load profile in new session
  - Use profile for deployment
- [ ] Test profile import workflow
  - Import Visual Studio .pubxml profile
  - Validate converted settings
  - Deploy using imported profile
- [ ] Test profile auto-discovery
  - Auto-discover single .pubxml profile
  - Auto-select and use discovered profile
  - Handle multiple profiles (user selection)

### 9.3 Error Handling and Recovery Integration
- [ ] Test connection failure scenarios
  - Invalid server address
  - Incorrect port
  - Authentication failure
  - Connection timeout
  - Network disconnection during upload
- [ ] Test build failure scenarios
  - Compilation errors
  - Missing dependencies
  - Invalid project configuration
- [ ] Test upload failure scenarios
  - Insufficient disk space on server
  - Permission denied on server
  - Individual file upload failures
  - Concurrent upload errors

### 9.4 Security and Credential Integration
- [ ] Test credential encryption and storage
  - Save credentials to profile
  - Load and decrypt credentials
  - Verify credentials not stored in plain text
  - Test DPAPI encryption on different Windows versions
- [ ] Test environment variable credentials
  - Set FTP_USERNAME and FTP_PASSWORD
  - Verify environment variables override profile
  - Test in CI/CD scenario
- [ ] Verify credential security
  - Credentials never logged in plain text
  - Encrypted data cannot be decrypted by other users
  - Credentials cleared from memory after use

## 10. Testing

### 10.1 Unit Testing
- [ ] Write unit tests for core domain models
  - DeploymentProfile serialization/deserialization
  - DeploymentResult calculations
  - Configuration validation
- [ ] Write unit tests for build integration
  - Project type detection
  - Build tool selection
  - Command-line argument generation
  - Build output parsing
- [ ] Write unit tests for credential management
  - DPAPI encryption/decryption
  - Credential save/load
  - Environment variable handling
- [ ] Write unit tests for profile management
  - Profile create, edit, delete
  - Profile import and conversion
  - Profile validation
- [ ] Write unit tests for utilities
  - File size formatting
  - Duration formatting
  - Speed calculations
  - Progress estimation

### 10.2 Integration Testing
- [ ] Set up test FTP server
  - Use local FTP server or container
  - Configure test accounts and permissions
  - Create test directory structures
- [ ] Set up test SFTP server (if implementing SFTP)
  - Use local SFTP server or container
  - Configure SSH keys and authentication
  - Create test directory structures
- [ ] Create sample .NET test projects
  - .NET Framework 4.x Web Application
  - .NET Core 3.1 Web Application
  - .NET 6+ Web Application
  - Blazor Server project
  - Web API project
- [ ] Write integration tests for FTP operations
  - Connect to test FTP server
  - Upload files
  - List files
  - Delete files
  - Create directories
- [ ] Write integration tests for build operations
  - Build sample .NET Framework project
  - Build sample .NET Core project
  - Build sample .NET 6+ project
  - Verify publish output
- [ ] Write integration tests for full deployment
  - Deploy to test FTP server
  - Verify all files uploaded correctly
  - Test cleanup mode
  - Test app_offline.htm handling

### 10.3 End-to-End Testing
- [ ] Test complete deployment scenarios
  - First-time deployment with profile auto-discovery
  - Routine deployment with saved profile
  - Deployment with cleanup mode
  - Deployment with custom concurrency
  - Dry-run deployment
- [ ] Test error scenarios
  - Deployment with invalid credentials
  - Deployment with unreachable server
  - Deployment with build failure
  - Deployment with insufficient disk space
  - Deployment interrupted by user (Ctrl+C)
- [ ] Test profile management workflows
  - Create, list, show, edit, delete profiles
  - Import Visual Studio profile
  - Use imported profile for deployment
- [ ] Test CLI commands and options
  - All command variants and option combinations
  - Help and version commands
  - Invalid command usage (verify helpful errors)

### 10.4 Performance Testing
- [ ] Test concurrent upload performance
  - Compare sequential vs. concurrent upload times
  - Verify 40%+ improvement with concurrency
  - Test with various concurrency levels (1, 4, 8, 16)
  - Measure upload throughput
- [ ] Test large deployment handling
  - Deploy project with 1,000+ files
  - Deploy project with 5,000+ files
  - Deploy project with 10,000+ files
  - Verify memory usage remains reasonable
  - Verify progress tracking accuracy
- [ ] Test upload speed and estimation accuracy
  - Measure actual vs. estimated deployment time
  - Verify time remaining estimates are conservative
  - Test with various file sizes and counts

### 10.5 Security Testing
- [ ] Verify credential encryption
  - Confirm DPAPI encryption is used
  - Verify encrypted credentials cannot be decrypted by other users
  - Test on different Windows user accounts
- [ ] Verify credential security in logs
  - Scan all log output for plain text credentials
  - Verify credentials are masked or not logged
  - Check verbose/debug logs
- [ ] Test profile file security
  - Verify profile files have appropriate permissions
  - Test profile file access by other users
  - Verify credentials in profile are encrypted

### 10.6 Compatibility Testing
- [ ] Test on different Windows versions
  - Windows 10
  - Windows 11
  - Windows Server 2019/2022
- [ ] Test with different .NET SDK versions
  - .NET SDK 6.0
  - .NET SDK 7.0
  - .NET SDK 8.0
  - Visual Studio 2022 MSBuild
- [ ] Test with different FTP servers
  - FileZilla Server
  - IIS FTP
  - ProFTPD
  - vsftpd
  - Various shared hosting FTP servers
- [ ] Test with different console types
  - Windows Command Prompt
  - PowerShell 5.1
  - PowerShell 7+
  - Windows Terminal
  - VS Code integrated terminal

## 11. Documentation

### 11.1 User Documentation
- [ ] Create README.md
  - Project overview and features
  - Installation instructions
  - Quick start guide
  - Basic usage examples
  - Link to full documentation
- [ ] Write getting started guide
  - Prerequisites and requirements
  - Installation steps
  - First deployment walkthrough
  - Common workflows
- [ ] Create command reference documentation
  - Document all commands and subcommands
  - Document all options and flags
  - Provide usage examples for each command
  - Document exit codes
- [ ] Write deployment guide
  - Profile creation and management
  - Visual Studio profile import
  - Deployment workflow explanation
  - Best practices and recommendations
- [ ] Create troubleshooting guide
  - Common errors and solutions
  - Connection issues
  - Build failures
  - Upload problems
  - FAQ section
- [ ] Document configuration options
  - Profile file structure and format
  - Global configuration settings
  - Environment variables
  - Exclusion patterns

### 11.2 API and Developer Documentation
- [ ] Generate API documentation
  - XML documentation comments for all public APIs
  - Use DocFX or similar tool to generate docs
  - Host on GitHub Pages or docs site
- [ ] Document architecture and design
  - High-level architecture overview
  - Component responsibilities
  - Data flow diagrams
  - Extension points for future features
- [ ] Create contribution guide
  - Development environment setup
  - Coding standards and conventions
  - Pull request process
  - Testing requirements

### 11.3 Release Documentation
- [ ] Write changelog
  - Document all features, fixes, and changes
  - Follow Keep a Changelog format
  - Organize by version and date
- [ ] Create release notes
  - Highlight key features for each release
  - Document breaking changes
  - Provide upgrade instructions
  [ ] Document known issues and limitations
  - List current limitations
  - Document planned features for future versions
  - Workarounds for known issues

### 11.4 Security Documentation
- [ ] Document credential security model
  - DPAPI encryption details
  - Credential storage locations
  - Environment variable usage
  - Best practices for credential management
- [ ] Create security considerations guide
  - Secure profile storage
  - Using credentials in CI/CD
  - FTP vs. SFTP security implications
  - Recommendations for production use

## 12. Deployment and Distribution

### 12.1 Build Configuration
- [ ] Configure release build settings
  - Optimize for size and performance
  - Enable ReadyToRun compilation (if appropriate)
  - Trim unused assemblies
  - Set appropriate target frameworks
- [ ] Create self-contained executable builds
  - Windows x64 self-contained build
  - Single-file executable option
  - Include all dependencies
- [ ] Configure versioning
  - Embed version information in assembly
  - Tag releases in Git with version number
  - Use semantic versioning (X.Y.Z)

### 12.2 Installation Package
- [ ] Create Windows installer (optional)
  - MSI or setup.exe installer
  - Add to PATH during installation
  - Create Start Menu shortcuts
  - Uninstaller support
- [ ] Create portable ZIP distribution
  - Self-contained executable
  - README and LICENSE files
  - Quick start documentation
- [ ] Create installation scripts
  - PowerShell install script
  - Add to PATH script
  - Verification script

### 12.3 Distribution Channels
- [ ] Publish to GitHub Releases
  - Create release with version tag
  - Attach build artifacts (ZIP, installer)
  - Include release notes
  - Mark pre-releases appropriately
- [ ] Publish to NuGet (as .NET tool)
  - Package as .NET global tool
  - Publish to nuget.org
  - Enable installation via `dotnet tool install -g ftpsheep`
- [ ] Create Chocolatey package (optional)
  - Package for Chocolatey repository
  - Enable installation via `choco install ftpsheep`
- [ ] Create winget package (optional)
  - Submit to Windows Package Manager repository
  - Enable installation via `winget install ftpsheep`

### 12.4 CI/CD Pipeline
- [ ] Configure automated build pipeline
  - Build on every push and PR
  - Run all tests
  - Generate code coverage reports
  - Create build artifacts
- [ ] Configure automated release pipeline
  - Trigger on version tag push
  - Build release artifacts
  - Run full test suite
  - Create GitHub release
  - Publish to NuGet
  - Update documentation site

### 12.5 Monitoring and Analytics
- [ ] Set up error tracking (optional for future)
  - Opt-in telemetry for error reporting
  - Anonymous usage statistics
  - Performance metrics
- [ ] Monitor GitHub metrics
  - Stars, forks, watchers
  - Issue and PR activity
  - Download counts
  - Community engagement

## 13. Maintenance and Post-Release

### 13.1 Bug Tracking and Prioritization
- [ ] Set up GitHub Issues templates
  - Bug report template
  - Feature request template
  - Question/support template
- [ ] Create issue labels and milestones
  - Priority labels (P0, P1, P2, P3)
  - Type labels (bug, enhancement, documentation)
  - Status labels (in-progress, blocked, needs-review)
  - Milestone for each planned version
- [ ] Establish bug triage process
  - Regular review of new issues
  - Prioritization criteria
  - Assignment workflow
  - Target resolution times (P0: 1-2 days, P1: 1 week, etc.)

### 13.2 Support and Community Management
- [ ] Create support channels
  - GitHub Discussions for Q&A
  - GitHub Issues for bugs and feature requests
  - Documentation site for self-service help
- [ ] Monitor and respond to issues
  - Acknowledge new issues within 48 hours
  - Provide helpful responses
  - Request additional information when needed
  - Close resolved or stale issues
- [ ] Engage with community contributions
  - Review pull requests promptly
  - Provide constructive feedback
  - Acknowledge and thank contributors
  - Maintain welcoming environment

### 13.3 Version Updates and Maintenance
- [ ] Plan maintenance releases
  - Bug fix releases (patch versions)
  - Security updates
  - Dependency updates
  - Performance improvements
- [ ] Plan feature releases
  - Minor version releases with new features
  - Major version releases with breaking changes
  - Follow semantic versioning
- [ ] Maintain backward compatibility
  - Avoid breaking changes in minor/patch releases
  - Provide migration guides for major versions
  - Deprecate features gracefully

### 13.4 Performance Monitoring
- [ ] Monitor deployment success rates
  - Track successful vs. failed deployments (from community feedback)
  - Identify common failure patterns
  - Address reliability issues
- [ ] Gather performance feedback
  - Collect deployment duration statistics
  - Monitor upload speeds and concurrency effectiveness
  - Identify performance bottlenecks
  - Optimize based on real-world usage

### 13.5 Future Feature Planning
- [ ] Gather feature requests
  - Collect from GitHub Issues
  - Prioritize based on user demand
  - Evaluate feasibility and scope
- [ ] Plan V1.1 and V2.0 features
  - SFTP support (FR-016)
  - Incremental deployment (FR-017)
  - Pre/post deployment hooks (FR-018)
  - Visual Studio extension
  - GUI application
  - Cross-platform support (macOS, Linux)
- [ ] Create roadmap document
  - Public roadmap on GitHub
  - Planned features by version
  - Estimated timelines
  - Community input welcome

## 14. High Priority Features - Implementation Checklist

### 14.1 FR-001: Visual Studio Publish Profile Auto-Discovery
- [ ] Implement .pubxml file search in project directory
- [ ] Auto-select if exactly one profile found
- [ ] List and prompt if multiple profiles found
- [ ] Parse .pubxml XML and extract FTP settings
- [ ] Convert to FTPSheep.NET profile format
- [ ] Enable zero-config `ftpsheep deploy` command
- [ ] Test with various Visual Studio profile configurations

### 14.2 FR-002: Build and Publish Integration
- [ ] Detect project type from .csproj file
- [ ] Invoke dotnet.exe for .NET Core/5+ projects
- [ ] Invoke msbuild.exe for .NET Framework projects
- [ ] Execute publish to local temp folder
- [ ] Support Debug and Release configurations
- [ ] Capture and display build output
- [ ] Handle build errors gracefully
- [ ] Test with all ASP.NET project types

### 14.3 FR-003: FTP Connectivity
- [ ] Integrate FTP library (FluentFTP)
- [ ] Connect to FTP servers with credentials
- [ ] Support custom ports
- [ ] Validate connection before deployment
- [ ] Implement connection timeout handling
- [ ] Implement automatic retry for failed connections
- [ ] Test with various FTP server types

### 14.4 FR-004: Secure Credential Management
- [ ] Implement DPAPI encryption wrapper
- [ ] Encrypt credentials before saving to profile
- [ ] Decrypt credentials when loading profile
- [ ] Prompt for credentials if not stored
- [ ] Option to save credentials for future use
- [ ] Never store credentials in plain text
- [ ] Support environment variable credentials (FTP_USERNAME, FTP_PASSWORD)
- [ ] Test encryption across Windows user accounts

### 14.5 FR-005: Direct Deployment Upload Strategy
- [ ] Upload files directly to destination folder
- [ ] Overwrite existing files in place
- [ ] Implement optional cleanup mode
- [ ] Compare server files with published files
- [ ] Apply exclusion patterns (App_Data, uploads, logs)
- [ ] Display list of files to be deleted
- [ ] Prompt for confirmation before cleanup
- [ ] Only clean up after successful upload
- [ ] Test deployment with and without cleanup mode

### 14.6 FR-006: Concurrent File Uploads
- [ ] Implement concurrent upload queue
- [ ] Configure concurrency level (default 4-8)
- [ ] Upload multiple files in parallel
- [ ] Handle individual file upload failures
- [ ] Coordinate completion of all uploads
- [ ] Test performance improvement vs. sequential uploads
- [ ] Verify 40%+ performance improvement

### 14.7 FR-007: Console Progress Tracking
- [ ] Display pre-deployment summary (file count, size, estimated time)
- [ ] Show real-time upload progress (files uploaded / total)
- [ ] Display current upload speed
- [ ] Display average upload speed
- [ ] Calculate and display time remaining
- [ ] Show percentage completion
- [ ] Update progress in-place without excessive scrolling
- [ ] Test with various deployment sizes

### 14.8 FR-008: Deployment Profile Management
- [ ] Implement profile create command
- [ ] Implement profile save to disk (JSON format)
- [ ] Implement profile load by name or path
- [ ] Implement profile list command
- [ ] Store all deployment settings in profile
- [ ] Support fully automated, non-interactive deployments
- [ ] Test profile create -> save -> load -> deploy workflow

### 14.9 FR-009: Console Logging
- [ ] Output informative messages at each stage
- [ ] Include timestamps in log output
- [ ] Display errors in red, warnings in yellow, success in green
- [ ] Support verbosity levels (minimal, normal, detailed, debug)
- [ ] Log deployment summary on completion
- [ ] Test logging at all verbosity levels

### 14.10 FR-010: Error Handling and Recovery
- [ ] Handle connection failures gracefully
- [ ] Retry failed uploads (configurable retry count)
- [ ] Provide clear error messages for common issues
- [ ] Clean up temp folders after failures
- [ ] Exit with appropriate error codes
- [ ] Test all error scenarios (connection, auth, disk space, permissions)

### 14.11 FR-011: IIS app_offline.htm Handling
- [ ] Create app_offline.htm template
- [ ] Upload app_offline.htm before deployment starts
- [ ] Delete app_offline.htm after successful deployment
- [ ] Keep app_offline.htm on failure
- [ ] Option to skip app_offline.htm creation
- [ ] Support custom app_offline.htm template
- [ ] Test with IIS server (files properly unlocked)

## 15. Medium Priority Features - Implementation Checklist

### 15.1 FR-012: Deployment Validation
- [ ] Verify published folder contents before upload
- [ ] Check server disk space availability (if supported)
- [ ] Validate deployment profile settings
- [ ] Warn about large deployments or slow connections
- [ ] Test validation with various scenarios

### 15.2 FR-013: Dry-Run Mode
- [ ] Implement --dry-run flag
- [ ] Simulate deployment without uploading
- [ ] Show what would be uploaded
- [ ] Validate settings and connections
- [ ] Display estimated deployment time
- [ ] Test dry-run with various profiles

### 15.3 FR-014: Configuration File Support
- [ ] Create global configuration file structure
- [ ] Support default settings override
- [ ] Configure default concurrency, timeout, retry values
- [ ] Test global config with profile-specific overrides

## Project Milestones and Timeline

### Phase 1: Foundation (Weeks 1-4)
**Deliverables:**
- Project structure, dependencies, CI/CD
- FTP connectivity and basic file upload
- Simple credential management
- Unit test framework

**Success Criteria:**
- Connect to FTP server and upload files
- Handle basic connection errors
- Tests passing in CI/CD

### Phase 2: Build Integration (Weeks 5-6)
**Deliverables:**
- MSBuild and dotnet CLI detection and invocation
- Build and publish to local folder
- Build error handling
- Support for .NET Framework and .NET Core/5+

**Success Criteria:**
- Successfully build and publish various .NET project types
- Handle build errors with clear messages
- Generate publish output ready for upload

### Phase 3: Core Deployment Flow (Weeks 7-9)
**Deliverables:**
- Direct upload deployment strategy
- Concurrent upload implementation
- Basic progress tracking
- Deployment profile save/load
- app_offline.htm handling

**Success Criteria:**
- Complete end-to-end deployment from build to server
- Concurrent file uploads working
- Basic progress display
- Profiles saved and loaded successfully

### Phase 4: Visual Studio Integration (Weeks 10-11)
**Deliverables:**
- .pubxml parser
- Profile auto-discovery
- Profile import command
- Zero-configuration first run

**Success Criteria:**
- Auto-discover and import VS profiles
- Deploy using imported profiles
- Zero-config `ftpsheep deploy` works

### Phase 5: Enhanced UX and Security (Weeks 12-13)
**Deliverables:**
- Advanced progress tracking with estimates
- Colored console output
- DPAPI credential encryption
- Deployment history
- Verbosity levels
- Cleanup mode with exclusions

**Success Criteria:**
- Rich progress display with time estimates
- Credentials encrypted and secure
- Professional console UI
- Deployment history tracked

### Phase 6: Testing and Refinement (Weeks 14-16)
**Deliverables:**
- Comprehensive unit and integration tests
- Real-world deployment testing
- Error handling improvements
- Performance optimization
- Bug fixes
- User documentation

**Success Criteria:**
- All high-priority features tested
- Successful deployments to various FTP servers
- All P0 and P1 bugs resolved
- Documentation complete

### Phase 7: Release Preparation (Week 17)
**Deliverables:**
- Release candidate builds
- Installation packages
- README and getting started guide
- GitHub repository preparation
- V1.0.0 release

**Success Criteria:**
- Installer tested on clean machines
- Documentation accessible
- GitHub repository public
- V1.0.0 released to NuGet and GitHub

## Technical Stack Summary

**Development:**
- .NET 6.0+ (for CLI tool)
- C# 10+
- Visual Studio 2022 or JetBrains Rider

**Libraries:**
- FluentFTP - FTP client
- SSH.NET - SFTP client (future)
- System.CommandLine or Spectre.Console.Cli - CLI framework
- Spectre.Console - Rich console UI
- NLog - Logging
- System.Text.Json - JSON serialization
- xUnit - Unit testing
- Moq - Mocking

**Tools:**
- Git for version control
- GitHub Actions or Azure DevOps for CI/CD
- DocFX for API documentation
- Nuget for package distribution

**Target Deployment Projects:**
- .NET Framework 4.x
- .NET Core 3.1
- .NET 5/6/7/8+
- ASP.NET MVC, Web API, Blazor, Razor Pages

## Success Metrics Targets

**User-Centric Metrics:**
- Deployment success rate: >95%
- Time savings per deployment: >60% vs. manual FTP
- User adoption: 500 users within 6 months
- User retention: >70% monthly active
- Profile reuse rate: >80%

**Business Metrics:**
- GitHub stars: 100 stars in 6 months
- Downloads: 1,000 downloads in 6 months
- Issue resolution time: <7 days for critical bugs
- Community contributions: 10 contributors

**Technical Metrics:**
- Average deployment duration: <5 minutes for 100 MB project
- Concurrent upload efficiency: >40% improvement vs. sequential
- Error recovery rate: >80% of failed uploads recovered via retry
- Build integration success: 100% for supported project types
- Credential security: Zero credential exposure incidents

---

**Document Version:** 1.0
**Created:** 2025-12-06
**Status:** Ready for Development
