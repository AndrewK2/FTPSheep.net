# FTPSheep.NET Product Requirements Document

## Product overview

### Document information
- **Product Name:** FTPSheep.NET
- **Version:** 1.0
- **Document Version:** 1.0
- **Last Updated:** 2025-12-06
- **Status:** Draft

### Product summary

FTPSheep.NET is a command-line deployment tool designed specifically for .NET developers who build and deploy ASP.NET applications to servers using FTP protocol. The tool streamlines the deployment workflow by integrating with the existing .NET build toolchain (msbuild/dotnet.exe) and Visual Studio publish profiles, while adding enhanced deployment capabilities including concurrent uploads, progress tracking, and secure credential management.

The initial version (V1) focuses on delivering a reliable CLI tool for Windows that enables developers to deploy full application builds to FTP servers with detailed progress feedback and safe deployment strategies. Future versions will expand to include SFTP support, incremental deployments, Visual Studio extension integration, and a standalone GUI application.

## Goals

### Business goals

- Provide .NET developers with a free, efficient alternative to manual FTP deployment workflows
- Establish FTPSheep.NET as a trusted deployment tool in the .NET ecosystem
- Create a foundation for future monetization opportunities through premium features or enterprise versions
- Build a community around the tool to drive adoption and gather feedback for future enhancements
- Position the product for expansion into Visual Studio marketplace and standalone applications

### User goals

- Reduce deployment time through automated build and upload processes
- Minimize deployment errors caused by manual file transfers
- Gain visibility into deployment progress with detailed metrics and time estimates
- Securely store and reuse FTP credentials without exposing sensitive information
- Leverage existing Visual Studio publish profiles while gaining additional deployment capabilities
- Execute deployments from command line for integration into CI/CD pipelines or build scripts

### Non-goals

- Building or replacing the .NET build system (tool uses existing msbuild/dotnet.exe)
- Providing rollback capabilities in V1
- Managing application configuration files (web.config, appsettings.json) in V1
- Supporting SFTP or other protocols beyond FTP in V1
- Implementing pre/post deployment hooks in V1
- Creating macOS or Linux versions in V1
- Deploying non-.NET applications
- Providing hosting or server management capabilities

## User personas

### Primary persona: Mid-level .NET developer

**Name:** Sarah Thompson

**Role:** Full-stack .NET Developer at a small web development agency

**Experience:** 3-5 years of .NET development experience

**Environment:** Works in Visual Studio daily, deploys multiple client websites to shared hosting environments with FTP access

**Pain points:**
- Spends significant time manually uploading files via FTP client after each build
- Frequently forgets to upload newly added files or updated assemblies
- Has difficulty tracking which files have been uploaded during large deployments
- Manages multiple deployment profiles for different clients and environments
- Concerned about credential security when storing FTP passwords

**Goals:**
- Automate the build and deploy process to save time
- Ensure all necessary files are deployed correctly every time
- See clear progress during deployments to estimate completion time
- Securely manage credentials for multiple deployment targets

### Secondary persona: Senior .NET architect

**Name:** Michael Chen

**Role:** Senior Software Architect at a medium-sized enterprise

**Experience:** 10+ years of .NET development and architecture

**Environment:** Manages deployment processes for multiple teams, integrates deployment into automated build scripts

**Pain points:**
- Needs to standardize deployment processes across multiple development teams
- Requires scriptable deployment tools for automation scenarios
- Must ensure deployment reliability and safety (avoiding partial deployments)
- Needs detailed logging for audit and troubleshooting purposes

**Goals:**
- Implement consistent deployment workflows across teams
- Integrate deployment into existing build automation
- Monitor deployment success and failure rates
- Ensure safe deployment practices with atomic-like operations

### Tertiary persona: Junior .NET developer

**Name:** Alex Rivera

**Role:** Junior Developer at a startup

**Experience:** Less than 2 years of professional .NET development

**Environment:** Learning Visual Studio and deployment workflows, occasionally deploys to staging environments

**Pain points:**
- Unfamiliar with FTP deployment best practices
- Uncertain about which files need to be deployed
- Lacks confidence in manual deployment processes
- Limited understanding of build and publish workflows

**Goals:**
- Learn proper deployment procedures through guided tooling
- Avoid breaking staging or production environments
- Understand what is happening during the deployment process
- Leverage existing Visual Studio configurations without deep FTP knowledge

### Role-based access

For V1 (CLI tool), role-based access is not applicable as the tool runs locally with user-level permissions. Future versions with centralized credential management or team features may implement:

- **Administrator:** Manage shared deployment profiles and credentials for team
- **Developer:** Create and execute deployments, manage personal credentials
- **Read-only:** View deployment history and logs only

## Functional requirements

### High priority (Must-have for V1)

**FR-001: Visual Studio publish profile import and auto-discovery**
- Automatically search for Visual Studio publish profiles (.pubxml files) in project directory
- If exactly one profile found, use it automatically (prompt only for missing info)
- If multiple profiles found, list them and let user select one
- Read and parse existing Visual Studio publish profiles (.pubxml files)
- Extract FTP connection details (server, port, username, path)
- Convert to FTPSheep.NET deployment profile format with additional settings
- Support FTP protocol specifications from Visual Studio profiles
- Enable zero-configuration first run: `ftpsheep deploy` with no parameters

**FR-002: Build and publish integration**
- Invoke dotnet.exe or msbuild.exe based on project type and .NET version
- Execute publish operation to local temporary folder
- Support all .NET versions (.NET Framework 4.x, .NET Core, .NET 5+)
- Support all ASP.NET project types (MVC, Blazor, Web API, Razor Pages, etc.)
- Capture and display build output and errors

**FR-003: FTP connectivity**
- Connect to FTP servers using standard FTP protocol
- Support custom ports for FTP
- Validate connection before starting upload process
- Handle connection timeouts and retries

**FR-004: Secure credential management**
- Encrypt FTP credentials using Windows Data Protection API (DPAPI)
- Store encrypted credentials in deployment profile files
- Prompt for credentials if not stored in profile
- Option to save credentials for future use
- Never store credentials in plain text

**FR-005: Direct deployment upload strategy**
- Upload all published files directly to destination folder, overwriting existing files in place (primary method)
- Support optional cleanup mode to remove files/folders that no longer exist in the source
- Allow exclusion of specific folders/files from deletion (e.g., App_Data, uploads, logs)
- Configurable exclusion patterns in deployment profile
- Display list of files to be deleted before cleanup (with confirmation prompt)
- Safe deployment: validate all uploads complete successfully before cleanup phase

**FR-006: Concurrent file uploads**
- Upload multiple files simultaneously to improve performance
- Configure concurrency level (default and user-configurable)
- Handle upload failures for individual files within concurrent batch
- Maintain upload queue with proper error handling

**FR-007: Console progress tracking**
- Display pre-deployment summary (total files, total size, estimated time)
- Show real-time upload progress (files uploaded vs remaining)
- Display current upload speed and average speed
- Calculate and display estimated time remaining
- Show percentage completion
- Update progress in console without excessive scrolling

**FR-008: Deployment profile management**
- Create new deployment profiles via CLI parameters
- Save deployment profiles to disk in JSON or XML format
- Load existing deployment profiles by name or path
- List available deployment profiles
- Store profile-specific settings (server details, paths, concurrency settings, credentials)
- Profiles contain all information needed for fully automated, non-interactive deployments
- Support unattended execution when profile has complete configuration

**FR-009: Console logging**
- Output informative messages at each deployment stage
- Include timestamps in log output
- Show errors and warnings clearly
- Support different verbosity levels (minimal, normal, detailed)
- Log deployment summary (success/failure, files uploaded, duration)

**FR-010: Error handling and recovery**
- Gracefully handle connection failures
- Retry failed uploads with configurable retry count
- Provide clear error messages for common issues (authentication, permissions, disk space)
- Clean up temporary folders on server after failures
- Exit with appropriate error codes for scripting integration

**FR-011: IIS app_offline.htm handling**
- Create app_offline.htm file in destination root folder before upload starts
- Ensure IIS releases locked executable files (DLLs, EXEs) before deployment
- Delete app_offline.htm file after successful deployment completion
- If deployment fails, keep app_offline.htm in place to prevent broken application access
- Configurable option to skip app_offline.htm creation for non-IIS deployments
- Support custom app_offline.htm content/template

### Medium priority (Should-have for V1 or V1.1)

**FR-012: Deployment validation**
- Verify published folder contents before upload
- Check server disk space availability before deployment
- Validate deployment profile settings before execution
- Warn about potential issues (large deployment size, slow connection)

**FR-013: Dry-run mode**
- Simulate deployment without actually uploading files
- Show what would be uploaded and where
- Validate all settings and connections without making changes
- Display estimated deployment time based on file sizes

**FR-014: Configuration file support**
- Support global configuration file for tool defaults
- Override global settings with profile-specific settings
- Configure default concurrency, timeout, and retry values

### Low priority (Nice-to-have for future versions)

**FR-015: Deployment history**
- Record deployment history locally (timestamp, profile, outcome)
- Display recent deployment history via CLI command
- Include deployment duration and file counts in history

**FR-016: SFTP protocol support**
- Connect to SFTP servers using SSH File Transfer Protocol
- Support custom ports for SFTP
- Support both password and SSH key-based authentication
- Validate SFTP connections before deployment
- Handle SFTP-specific operations and errors
- Encrypt SFTP credentials using appropriate security mechanisms

**FR-017: Incremental deployment**
- Compare local published files with server files
- Upload only changed or new files
- Support file comparison strategies (timestamp, hash, size)
- Display savings from incremental deployment

**FR-018: Pre/post deployment hooks**
- Execute custom scripts before deployment starts
- Execute custom scripts after deployment completes
- Support PowerShell and batch scripts
- Pass deployment context to hook scripts

**FR-019: Multiple deployment target support**
- Deploy to multiple servers in sequence or parallel
- Define server groups in profiles
- Report on multi-target deployment status

**FR-020: IIS App_offline.htm progress updates**
- Optional feature to upload App_offline.htm at deployment start to take IIS application offline
- Periodically update App_offline.htm with deployment progress information
- Display progress percentage, files uploaded count, and estimated completion time
- Customizable HTML template for App_offline.htm page
- Remove App_offline.htm automatically when deployment completes successfully
- Keep App_offline.htm if deployment fails (with error message) until manual intervention

## User experience

### Entry points

**Command-line interface**

Users interact with FTPSheep.NET exclusively through the command line in V1. The tool is invoked using the `ftpsheep` command with various subcommands and options.

Primary commands:
- `ftpsheep deploy` - Execute a deployment
- `ftpsheep profile` - Manage deployment profiles
- `ftpsheep import` - Import Visual Studio publish profiles
- `ftpsheep init` - Initialize a new deployment profile

Users may invoke the tool from:
- Windows Command Prompt
- PowerShell
- Windows Terminal
- Integrated terminals in Visual Studio or VS Code
- Build scripts and CI/CD pipelines

### Core user flows

**Flow 1: First-time deployment setup (automatic)**

1. Developer navigates to .NET project directory in terminal
2. Runs `ftpsheep deploy` without any parameters or profiles
3. Tool searches for Visual Studio publish profiles (.pubxml files) in project
4. **If exactly one VS profile found:**
   - Tool automatically reads the profile and extracts connection details
   - Tool prompts only for missing information (e.g., credentials if not in profile)
5. **If multiple VS profiles found:**
   - Tool lists all available profiles with names and servers
   - User selects which profile to use
   - Tool reads selected profile and prompts for any missing information
6. **If no VS profiles found:**
   - Tool prompts user to create profile manually or provides guidance
7. Tool validates credentials by connecting to FTP server
8. If connection successful, tool asks if credentials should be saved (encrypted)
9. If connection fails, tool prompts to re-enter credentials or abort
10. Tool creates FTPSheep.NET deployment profile and saves it
11. Tool displays pre-deployment summary
12. Developer confirms deployment (or uses `--yes` to skip)
13. Tool builds project, uploads files, and displays progress
14. Tool shows deployment completion summary

**Flow 2: Routine deployment execution**

1. Developer makes changes to project code
2. Runs `ftpsheep deploy --profile [name]` in project directory
3. Tool loads deployment profile (credentials already saved)
4. Tool invokes build and publish process
5. Tool connects to FTP server
6. Tool displays deployment summary (X files, Y MB, estimated Z minutes)
7. Tool uploads app_offline.htm to destination root to take IIS application offline
8. Tool begins upload with progress display showing:
   - Progress bar or percentage
   - Files uploaded (e.g., 45/120)
   - Current speed (e.g., 1.2 MB/s)
   - Time remaining (e.g., 2m 30s remaining)
9. Tool completes upload, overwriting files directly on server
10. If cleanup mode enabled, tool removes obsolete files (excluding App_Data and other configured exclusions)
11. Tool deletes app_offline.htm to bring application back online
12. Tool displays success message with total deployment time
13. Developer verifies deployment on server

**Flow 3: Managing multiple deployment profiles**

1. Developer runs `ftpsheep profile list` to see available profiles
2. Developer runs `ftpsheep profile create` to create new profile manually
3. Tool prompts for all required settings (server, port, paths, credentials)
4. Tool validates connection with provided credentials
5. Tool saves new profile
6. Developer can now deploy to any profile using `--profile [name]`

**Flow 4: Troubleshooting failed deployment**

1. Developer runs deployment command
2. Upload begins but fails midway (connection error)
3. Tool displays clear error message with failure reason
4. Tool cleans up temporary folder on server
5. Tool exits with error code
6. Developer checks connection/credentials
7. Developer runs deployment again with `--verbose` flag for detailed logging
8. Tool retries with more detailed output
9. Developer identifies and resolves issue
10. Deployment succeeds

### Advanced features

**Profile customization**

Power users can edit deployment profile files directly to configure:
- Custom concurrency levels for faster or slower uploads
- Timeout and retry settings
- Specific build configurations and publish options
- Exclusion patterns for files that should not be uploaded
- Server-side folder structures

**Scripting and automation**

The tool supports integration into automated workflows:
- Exit codes indicate success or failure for script error handling
- `--yes` or `-y` flag for unattended execution without confirmation prompts
- Profiles contain all necessary information for fully automated deployments
- JSON output mode for parsing deployment results
- Environment variable support for credentials in CI/CD environments
- Silent mode for non-interactive execution

**Verbosity control**

Users can control output detail level:
- Minimal: Show only critical information and errors
- Normal: Show standard progress and summary (default)
- Detailed: Show verbose logging including file-level operations
- Debug: Show all internal operations for troubleshooting

### UI/UX highlights

**Console output design**

- Clean, readable output with clear visual hierarchy
- Progress indicators update in-place (no excessive scrolling)
- Color coding for different message types (success=green, error=red, warning=yellow)
- Timestamps for all significant events
- Consistent formatting across all commands

**Progress visualization**

```
Deploying to Production (profile: prod-server)
Build completed: 245 files, 15.3 MB

Uploading to ftp://example.com/wwwroot
[████████████░░░░░░░░] 65% | 159/245 files | 10.2 MB/15.3 MB
Speed: 1.4 MB/s | Elapsed: 00:01:23 | Remaining: 00:00:45
```

**Error messaging**

- Clear, actionable error messages
- Suggestions for resolution when possible
- Reference to common issues in documentation
- Display of relevant context (profile name, server, operation)

**Deployment summary**

```
Deployment completed successfully!
Profile: prod-server
Files uploaded: 245
Total size: 15.3 MB
Duration: 2m 15s
Server: ftp://example.com/wwwroot
```

## Narrative

As a .NET developer working on an ASP.NET web application, I open my terminal and navigate to my project folder after making some updates to the codebase. I type `ftpsheep deploy --profile production` and press Enter. Within seconds, FTPSheep.NET begins building my project using the same dotnet tooling I'm familiar with, and I see the build output confirming a successful compilation. The tool then displays a summary showing that 156 files totaling 12.4 MB will be uploaded to my production server, with an estimated time of about 3 minutes. I confirm the deployment, and a clean progress display appears showing me exactly how many files have been uploaded, the current upload speed, and how much time remains. The progress updates smoothly without cluttering my console. After a few minutes, I see a success message confirming all files were deployed successfully to the server. The tool efficiently uploaded and overwrote the changed files directly on the server, and because I have cleanup mode enabled, it also removed old files that are no longer in my project—while safely preserving my App_Data folder and other configured exclusions. I feel confident that the deployment completed correctly because the tool handled the entire process—building, uploading, and cleaning up obsolete files. I didn't have to manually select files, worry about missing something, or wonder if my credentials were stored securely. The whole process was automated, transparent, and gave me the visibility I needed to trust that my application is now running the latest version on the server.

## Success metrics

### User-centric metrics

- **Deployment success rate:** Percentage of deployments that complete without errors (target: >95%)
- **Time savings per deployment:** Average time saved compared to manual FTP deployment (target: >60% reduction)
- **User adoption rate:** Number of active users within 6 months of launch (target: 500 users)
- **User retention rate:** Percentage of users who deploy at least once per month (target: >70%)
- **Profile reuse rate:** Percentage of deployments using saved profiles vs. manual configuration (target: >80%)
- **Documentation access rate:** Percentage of users who access help documentation or commands (baseline metric)

### Business metrics

- **GitHub stars and forks:** Measure of community interest and engagement (target: 100 stars in 6 months)
- **Download count:** Total number of tool downloads (target: 1,000 downloads in 6 months)
- **Issue resolution time:** Average time to resolve reported issues (target: <7 days for critical bugs)
- **Community contributions:** Number of external contributors or feature requests (target: 10 contributors)
- **Visual Studio extension waitlist:** Number of users expressing interest in VS extension (baseline metric for V2 planning)

### Technical metrics

- **Average deployment duration:** Mean time from command execution to completion for typical project (target: <5 minutes for 100 MB project)
- **Upload throughput:** Average upload speed achieved during deployments (baseline metric, varies by connection)
- **Concurrent upload efficiency:** Performance improvement from concurrent uploads vs. sequential (target: >40% improvement)
- **Error recovery rate:** Percentage of failed uploads successfully recovered through retry logic (target: >80%)
- **Build integration success rate:** Percentage of .NET projects that build successfully through the tool (target: 100% for supported project types)
- **Credential encryption reliability:** Zero incidents of credential exposure or encryption failures (target: 100% security)

## Technical considerations

### Integration points

**External build tools**
- Must detect and invoke appropriate build tool (msbuild.exe for .NET Framework, dotnet.exe for .NET Core/5+)
- Parse MSBuild project files to determine project type and configuration
- Capture stdout/stderr from build process for user feedback
- Handle build failures gracefully with clear error messages

**Visual Studio publish profiles**
- Parse .pubxml XML format used by Visual Studio
- Extract PublishMethod, PublishUrl, UserName, and other FTP-related settings
- Handle various publish profile configurations (Debug, Release, custom configs)
- Maintain compatibility with publish profiles across Visual Studio versions

**FTP/SFTP libraries**
- Integrate with reliable .NET FTP library (e.g., FluentFTP)
- Integrate with reliable .NET SFTP library (e.g., SSH.NET)
- Handle library-specific configuration and capabilities
- Abstract protocol differences behind unified deployment interface

**Windows Data Protection API**
- Use DPAPI for credential encryption tied to Windows user account
- Handle encryption/decryption errors gracefully
- Ensure encrypted credentials are machine and user-specific

**File system operations**
- Read/write deployment profiles to disk
- Monitor and read published output folders
- Create temporary directories for build output
- Handle file path length limitations on Windows

### Data storage and privacy

**Local storage**
- Deployment profiles stored in user's application data folder or project directory
- Profile files contain server details, paths, and encrypted credentials
- Deployment history stored in local SQLite database or JSON files
- Configuration files stored in standard Windows application data locations

**Credential security**
- All passwords encrypted using Windows DPAPI before storage
- Encrypted credentials only decryptable by the same Windows user on the same machine
- No credentials transmitted or stored in plain text
- Option to use credentials from environment variables for CI/CD scenarios
- Clear documentation on credential security model and limitations

**Privacy considerations**
- No telemetry or usage data collected in V1
- No server-side storage or cloud services required
- All operations occur locally or directly between user's machine and their FTP server
- Future versions may include opt-in anonymous usage analytics

### Scalability and performance

**Concurrent upload optimization**
- Default concurrency of 4-8 simultaneous uploads
- User-configurable concurrency (1-20 connections)
- Efficient queue management for thousands of files
- Memory-efficient streaming for large files
- Connection pooling to minimize overhead

**Large deployment handling**
- Support for deployments with 10,000+ files
- Progress tracking optimized for minimal console overhead
- Chunked processing for very large file lists
- Efficient file comparison for incremental deployments (future)

**Build process efficiency**
- Reuse existing build artifacts when possible
- Clean temporary build folders after deployment
- Support for pre-built publish folders (skip build step)
- Parallel processing of build and connection validation where possible

**Resource management**
- Limit memory footprint during large deployments
- Properly dispose of network connections
- Clean up temporary files and folders
- Handle interruption gracefully (Ctrl+C)

### Potential technical challenges

**Challenge 1: Build tool detection and invocation**
- **Issue:** Different .NET versions use different build tools and command syntax
- **Mitigation:** Implement robust project file parsing to determine .NET version and select appropriate tool; maintain comprehensive test suite covering all supported .NET versions
- **Risk level:** Medium

**Challenge 2: FTP server compatibility**
- **Issue:** FTP servers vary in behavior, feature support, and directory structure handling
- **Mitigation:** Use well-tested FTP libraries with broad server compatibility; implement server feature detection; provide manual override options for problematic servers
- **Risk level:** High

**Challenge 3: Safe deployment strategy implementation**
- **Issue:** Upload-to-temp, purge-destination, move-to-final strategy requires atomic-like operations on FTP servers that may not support transactions
- **Mitigation:** Implement careful error handling at each step; maintain state to enable recovery; thoroughly test failure scenarios; provide clear user guidance for manual recovery if needed
- **Risk level:** High

**Challenge 4: Progress tracking accuracy**
- **Issue:** Estimating remaining time requires accurate tracking of variable upload speeds
- **Mitigation:** Use moving average of recent upload speeds; account for file size distribution; provide conservative estimates; update estimates in real-time
- **Risk level:** Low

**Challenge 5: SFTP key-based authentication**
- **Issue:** SFTP servers may use SSH keys instead of or in addition to passwords
- **Mitigation:** Support both password and key-based authentication; securely store key file paths in profiles; provide clear documentation for key setup
- **Risk level:** Medium

**Challenge 6: Windows-specific dependencies**
- **Issue:** V1 targets Windows only, but using Windows-specific APIs (DPAPI) limits future cross-platform support
- **Mitigation:** Abstract credential storage behind interface; document platform-specific components; plan for cross-platform credential storage in future versions
- **Risk level:** Low

**Challenge 7: Handling deployment interruptions**
- **Issue:** User may cancel deployment or network may disconnect during upload
- **Mitigation:** Implement graceful cancellation; clean up partial uploads; provide resume capability in future versions; never leave server in inconsistent state
- **Risk level:** Medium

## Milestones and sequencing

### Project estimate

**Timeline:** 3-4 months for V1.0 release

**Team size:** 2-3 developers (1 senior, 1-2 mid-level)

**Effort breakdown:**
- Core deployment engine: 4-5 weeks
- FTP/SFTP integration: 2-3 weeks
- Build tool integration: 2 weeks
- Profile management: 1-2 weeks
- CLI interface and progress tracking: 2 weeks
- Security and credential management: 1 week
- Testing and bug fixes: 2-3 weeks
- Documentation and release preparation: 1 week

### Suggested phases

**Phase 1: Foundation (Weeks 1-4)**

Deliverables:
- Project structure and architecture
- FTP/SFTP connectivity module
- Basic file upload functionality
- Simple credential management (prompt-based)
- Unit test framework

Success criteria:
- Successfully connect to FTP and SFTP servers
- Upload files from local folder to remote server
- Handle basic connection errors

**Phase 2: Build integration (Weeks 5-6)**

Deliverables:
- dotnet.exe and msbuild.exe detection and invocation
- Build output parsing and error handling
- Publish to local folder functionality
- Support for major .NET versions and ASP.NET project types

Success criteria:
- Successfully build and publish .NET Framework 4.x projects
- Successfully build and publish .NET Core and .NET 5+ projects
- Handle build errors gracefully with clear messages

**Phase 3: Core deployment flow (Weeks 7-9)**

Deliverables:
- Safe upload strategy (temp folder → purge → move)
- Concurrent upload implementation
- Basic progress tracking
- Deployment profile format and storage
- Profile load/save functionality

Success criteria:
- Complete end-to-end deployment from build to server
- Upload multiple files concurrently
- Show basic progress during upload
- Save and load deployment profiles

**Phase 4: Visual Studio integration (Weeks 10-11)**

Deliverables:
- Visual Studio publish profile parser
- Import command to convert .pubxml to FTPSheep.NET profiles
- Profile conversion validation
- Support for various .pubxml configurations

Success criteria:
- Successfully import and convert Visual Studio FTP publish profiles
- Preserve all relevant connection and deployment settings
- Create valid FTPSheep.NET deployment profiles from imports

**Phase 5: Enhanced UX and security (Weeks 12-13)**

Deliverables:
- Advanced progress tracking with estimates
- Console output formatting and color coding
- DPAPI credential encryption
- Secure credential save/load
- Deployment history tracking
- Verbosity levels

Success criteria:
- Display detailed progress with time estimates and file counts
- Credentials encrypted and securely stored
- Clean, professional console output
- Deployment history retrievable via CLI

**Phase 6: Testing and refinement (Weeks 14-16)**

Deliverables:
- Comprehensive integration testing
- Real-world deployment testing across various server types
- Error handling improvements
- Performance optimization
- Bug fixes
- User documentation and help content

Success criteria:
- All critical and high-priority functional requirements tested
- Successful deployments to at least 5 different FTP/SFTP server types
- All P0 and P1 bugs resolved
- User documentation complete

**Phase 7: Release preparation (Week 17)**

Deliverables:
- Release candidate builds
- Installation and distribution package
- README and getting started guide
- GitHub repository preparation
- Initial marketing materials

Success criteria:
- Installer tested on clean Windows machines
- Documentation accessible and comprehensive
- GitHub repository ready for public release
- V1.0.0 released

## User stories

### Authentication and security

**US-001: Store encrypted credentials**

**Description:** As a developer, I want to securely save my FTP credentials in a deployment profile so that I don't have to enter them every time I deploy.

**Acceptance criteria:**
- Deployment profile creation prompts user to save credentials
- Credentials are encrypted using Windows DPAPI before storage
- Encrypted credentials are saved in profile file
- Saved credentials are automatically loaded on subsequent deployments
- User can opt out of saving credentials
- Clear message confirms whether credentials were saved or not

**US-002: Prompt for credentials when not stored**

**Description:** As a developer, I want to be prompted for credentials if they're not saved in the profile so that I can deploy without having to edit profile files manually.

**Acceptance criteria:**
- Tool detects missing credentials in loaded profile
- User is prompted to enter username and password
- Connection is validated with provided credentials
- User is asked if credentials should be saved for future use
- Deployment continues after credentials are provided
- Invalid credentials result in clear error message and re-prompt

**US-003: Use environment variables for credentials**

**Description:** As a developer using CI/CD pipelines, I want to provide credentials via environment variables so that I can deploy in automated scenarios without storing credentials in files.

**Acceptance criteria:**
- Tool checks for FTP_USERNAME and FTP_PASSWORD environment variables
- Environment variables override profile credentials if present
- Environment variable credentials are not saved to profile
- Clear documentation on environment variable names and usage
- Works in non-interactive/silent mode

### Profile management

**US-004: First run with automatic profile discovery**

**Description:** As a developer running FTPSheep.NET for the first time, I want the tool to automatically discover my Visual Studio publish profiles so that I can deploy without learning complex commands or manually creating profiles.

**Acceptance criteria:**
- Running `ftpsheep deploy` without parameters triggers profile discovery
- Tool searches for .pubxml files in project directory and subdirectories
- If exactly one VS profile found, tool automatically uses it without asking
- If multiple VS profiles found, tool displays numbered list with profile names and server URLs
- User selects profile by entering number or name
- Tool extracts settings from selected/discovered profile
- Tool prompts only for missing required information (e.g., credentials)
- After setup, profile is saved for future use
- Clear messages guide user through the process
- If no VS profiles found, tool provides helpful guidance on next steps

**US-005: Import Visual Studio publish profile**

**Description:** As a developer, I want to explicitly import my existing Visual Studio publish profile so that I can use FTPSheep.NET without manually recreating my deployment configuration.

**Acceptance criteria:**
- `ftpsheep import` command accepts path to .pubxml file
- Tool parses .pubxml and extracts FTP server, port, username, path, and protocol
- Tool prompts for new FTPSheep.NET profile name
- Tool creates new deployment profile with extracted settings
- Confirmation message displays profile location
- Imported profile can be immediately used for deployment
- Error message shown if .pubxml file is invalid or not found

**US-006: Create deployment profile manually**

**Description:** As a developer, I want to create a new deployment profile from scratch so that I can configure deployments for servers not currently in Visual Studio.

**Acceptance criteria:**
- `ftpsheep profile create` command initiates profile creation
- Tool prompts for all required settings: profile name, server, port, protocol, username, password, remote path, project path
- Tool validates inputs (e.g., valid port number, valid protocol)
- Tool tests connection before saving profile
- Profile is saved to disk in application data folder or specified location
- Confirmation message includes profile name and location

**US-007: List available deployment profiles**

**Description:** As a developer, I want to see a list of all my saved deployment profiles so that I can choose which one to use for deployment.

**Acceptance criteria:**
- `ftpsheep profile list` command displays all saved profiles
- Output shows profile name, server, protocol, and last used date
- Profiles are sorted alphabetically or by last used date
- Message shown if no profiles exist
- Command suggests creating or importing a profile if none exist

**US-008: View deployment profile details**

**Description:** As a developer, I want to view the details of a specific deployment profile so that I can verify its configuration without opening the file.

**Acceptance criteria:**
- `ftpsheep profile show [name]` command displays profile details
- Output includes server, port, protocol, remote path, project path, and concurrency settings
- Credentials are not displayed in plain text (shown as "stored" or "not stored")
- Error message if profile name doesn't exist
- Suggestion of similar profile names if exact match not found

**US-009: Edit deployment profile**

**Description:** As a developer, I want to update an existing deployment profile so that I can change settings without creating a new profile.

**Acceptance criteria:**
- `ftpsheep profile edit [name]` command opens interactive edit mode
- User can modify server, port, remote path, credentials, and other settings
- Tool validates new values before saving
- Changes are saved to profile file
- Option to test connection after editing
- Confirmation message after successful save

**US-010: Delete deployment profile**

**Description:** As a developer, I want to delete a deployment profile I no longer need so that I can keep my profile list organized.

**Acceptance criteria:**
- `ftpsheep profile delete [name]` command removes profile
- Tool prompts for confirmation before deletion
- Profile file is deleted from disk
- Confirmation message after successful deletion
- Error message if profile doesn't exist
- Option to skip confirmation with `--force` flag

### Build and publish operations

**US-010: Build .NET Framework project**

**Description:** As a developer working on a .NET Framework project, I want FTPSheep.NET to build my project using msbuild so that I can deploy the latest code.

**Acceptance criteria:**
- Tool detects .NET Framework project type from project file
- Tool locates msbuild.exe on the system
- Tool invokes msbuild with appropriate parameters for web publish
- Build output is displayed in console
- Build errors are captured and displayed clearly
- Build succeeds and outputs to temporary local folder
- Deployment aborts if build fails

**US-011: Build .NET Core/5+ project**

**Description:** As a developer working on a modern .NET project, I want FTPSheep.NET to build my project using dotnet.exe so that I can deploy the latest code.

**Acceptance criteria:**
- Tool detects .NET Core/.NET 5+ project type from project file
- Tool locates dotnet.exe on the system
- Tool invokes `dotnet publish` with appropriate parameters
- Build output is displayed in console
- Build errors are captured and displayed clearly
- Build succeeds and outputs to temporary local folder
- Deployment aborts if build fails

**US-012: Publish to local folder**

**Description:** As a developer, I want my project published to a local temporary folder before upload so that only the necessary deployment files are uploaded.

**Acceptance criteria:**
- Tool creates temporary folder for publish output
- Publish operation outputs all necessary files to temp folder
- Unnecessary files (e.g., .pdb files, source code) are excluded based on standard publish behavior
- Published folder contains only deployable files
- Temporary folder location is displayed in console
- Temporary folder is cleaned up after successful deployment

**US-013: Support different build configurations**

**Description:** As a developer, I want to specify which build configuration to use (Debug, Release, etc.) so that I can deploy appropriate builds for different environments.

**Acceptance criteria:**
- Deployment profile includes build configuration setting (default: Release)
- Tool passes configuration to build command
- `--configuration` flag overrides profile setting
- Build output indicates which configuration was used
- Different configurations can be saved in different profiles

**US-014: Handle build failures gracefully**

**Description:** As a developer, I want clear error messages when my build fails so that I can quickly identify and fix the issue.

**Acceptance criteria:**
- Build errors are captured from build tool output
- Errors are displayed with context (line numbers, file names if available)
- Deployment stops immediately when build fails
- Exit code indicates build failure
- Suggestion to run build directly in Visual Studio for debugging
- No upload attempt is made after build failure

### FTP/SFTP connectivity

**US-015: Connect to FTP server**

**Description:** As a developer, I want to deploy to an FTP server so that I can update my application on shared hosting or legacy servers.

**Acceptance criteria:**
- Tool connects to FTP server using host and port from profile
- Standard FTP protocol (port 21 by default)
- Successful connection confirmed before upload begins
- Connection timeout results in clear error message
- Support for active and passive FTP modes

**US-016: Connect to SFTP server**

**Description:** As a developer, I want to deploy to an SFTP server so that I can use secure encrypted connections for deployment.

**Acceptance criteria:**
- Tool connects to SFTP server using SSH protocol
- SFTP protocol (port 22 by default)
- Successful connection confirmed before upload begins
- SSH host key verification (prompt on first connection)
- Connection timeout results in clear error message

**US-017: Validate connection before deployment**

**Description:** As a developer, I want the tool to test the server connection before starting the build so that I don't waste time building if the server is unreachable.

**Acceptance criteria:**
- Connection test performed before build starts
- Basic authentication validation
- Remote path existence verification
- Write permission verification on remote path
- Clear error message if connection fails
- Option to skip connection test with `--skip-connection-test` flag

**US-018: Handle connection timeouts**

**Description:** As a developer, I want appropriate timeout handling so that the tool doesn't hang indefinitely on slow or unresponsive servers.

**Acceptance criteria:**
- Configurable connection timeout (default: 30 seconds)
- Configurable operation timeout (default: 60 seconds)
- Timeout errors are clearly indicated
- Suggestion to check server status or increase timeout
- Timeout settings can be configured in profile or via CLI flags

**US-019: Retry failed connections**

**Description:** As a developer, I want automatic retry for transient connection failures so that temporary network issues don't cause my deployment to fail.

**Acceptance criteria:**
- Failed connections are retried automatically (default: 3 retries)
- Exponential backoff between retries
- Retry count is configurable in profile or via CLI flag
- Console displays retry attempts and reasons
- Deployment fails after all retries exhausted
- Permanent errors (invalid credentials) are not retried

### Deployment execution

**US-020: Execute direct deployment**

**Description:** As a developer, I want to deploy all published files directly to the server so that the server has a complete, up-to-date version of my application.

**Acceptance criteria:**
- All files from publish folder are uploaded to server destination
- Files are uploaded directly to destination folder, overwriting existing files
- Optional cleanup mode removes files/folders that no longer exist in source
- Cleanup respects exclusion patterns (e.g., App_Data folder preserved by default)
- User is prompted to confirm cleanup with list of files to be deleted
- Deployment is marked as successful only after all steps complete

**US-021: Upload files concurrently**

**Description:** As a developer, I want multiple files uploaded simultaneously so that large deployments complete faster.

**Acceptance criteria:**
- Multiple files (4-8) are uploaded in parallel by default
- Concurrency level is configurable in profile
- Upload queue manages pending files
- Failed individual file uploads are retried
- All concurrent uploads complete before move operation
- Console progress reflects concurrent upload status

**US-022: Display pre-deployment summary**

**Description:** As a developer, I want to see a summary of what will be deployed before upload starts so that I can verify the deployment scope.

**Acceptance criteria:**
- Summary shows total number of files to upload
- Summary shows total size of all files
- Summary shows destination server and path
- Summary shows estimated deployment time based on average upload speeds
- User must confirm before upload starts (unless `--yes` flag used)
- Option to abort deployment at this stage

**US-023: Show upload progress**

**Description:** As a developer, I want to see real-time progress during upload so that I know how the deployment is proceeding.

**Acceptance criteria:**
- Progress bar or percentage shows overall completion
- Display shows files uploaded vs. total files (e.g., 45/120)
- Display shows data uploaded vs. total data (e.g., 12.3 MB / 25.6 MB)
- Current upload speed is displayed (e.g., 1.4 MB/s)
- Average upload speed is displayed
- Progress updates at least once per second
- Console output doesn't scroll excessively (updates in place)

**US-024: Estimate time remaining**

**Description:** As a developer, I want to see estimated time remaining during deployment so that I can plan my time accordingly.

**Acceptance criteria:**
- Time remaining calculated based on average upload speed
- Estimate updates as upload progresses
- Estimate displayed in readable format (e.g., "2m 30s remaining")
- Estimate becomes more accurate as more files are uploaded
- "Calculating..." shown initially before enough data for estimate

**US-025: Display deployment completion summary**

**Description:** As a developer, I want to see a summary when deployment completes so that I can confirm success and review deployment details.

**Acceptance criteria:**
- Summary shows deployment result (success or failure)
- Summary shows profile name and destination server
- Summary shows total files uploaded
- Summary shows total size uploaded
- Summary shows total deployment duration
- Summary shows average upload speed
- Timestamp of completion

**US-026: Handle upload failures**

**Description:** As a developer, I want the tool to handle upload failures gracefully so that I can understand what went wrong and retry if needed.

**Acceptance criteria:**
- Individual file upload failures are retried automatically
- After retries exhausted, clear error message identifies failed files
- Deployment stops if critical uploads fail
- Exit code indicates failure
- Failed files are listed in console output
- Suggestion to check logs or retry deployment
- No cleanup of obsolete files performed if upload phase fails

**US-027: Clean up obsolete files on server**

**Description:** As a developer, I want obsolete files removed from the server after deployment so that the server only contains current application files.

**Acceptance criteria:**
- Optional cleanup mode removes files/folders that don't exist in published source
- Cleanup runs only after all uploads complete successfully
- Exclusion patterns prevent deletion of specific folders (e.g., App_Data, uploads, logs)
- User is shown list of files to be deleted and prompted for confirmation (unless `--yes` flag used)
- `--yes` or `-y` flag skips all confirmation prompts for unattended execution
- Temporary build folder on local machine is deleted after deployment
- Cleanup errors are logged but don't fail the deployment if upload succeeded

### Progress tracking and logging

**US-028: Display detailed logs in verbose mode**

**Description:** As a developer troubleshooting issues, I want detailed logging so that I can understand exactly what the tool is doing.

**Acceptance criteria:**
- `--verbose` flag enables detailed logging
- Verbose mode shows file-level operations
- Verbose mode shows all FTP commands and responses
- Verbose mode shows build tool command line and full output
- Timestamps included in verbose logs
- Verbose logs help diagnose connection and upload issues

**US-029: Display minimal output in quiet mode**

**Description:** As a developer using the tool in scripts, I want minimal console output so that logs are clean and focused.

**Acceptance criteria:**
- `--quiet` flag enables minimal output mode
- Quiet mode shows only critical messages and final result
- Errors are still displayed in quiet mode
- Progress bars and detailed logging suppressed
- Exit code still indicates success or failure
- Suitable for CI/CD pipeline usage

**US-030: Log deployment events with timestamps**

**Description:** As a developer, I want all significant deployment events timestamped so that I can track timing and sequence of operations.

**Acceptance criteria:**
- All log messages include timestamp
- Timestamp format is readable (e.g., "14:35:22")
- Timestamps show elapsed time for long operations
- Deployment start and end times clearly marked
- Duration calculated and displayed for major steps

**US-031: Show colored output for message types**

**Description:** As a developer, I want different message types color-coded so that I can quickly identify errors, warnings, and success messages.

**Acceptance criteria:**
- Error messages displayed in red
- Warning messages displayed in yellow
- Success messages displayed in green
- Informational messages in default console color
- Colors work in Windows Command Prompt, PowerShell, and Windows Terminal
- Option to disable colors with `--no-color` flag

**US-032: Display current operation status**

**Description:** As a developer watching a deployment, I want to know what operation is currently running so that I understand where time is being spent.

**Acceptance criteria:**
- Clear message when build starts
- Clear message when connection is being established
- Clear message when upload begins
- Clear message when purge operation runs
- Clear message when files are being moved to destination
- Status updates don't clutter console

### Deployment history and reporting

**US-033: Record deployment history**

**Description:** As a developer, I want deployment history recorded so that I can review past deployments and their outcomes.

**Acceptance criteria:**
- Each deployment is recorded with timestamp, profile name, result, and duration
- History stored in local database or file
- History includes file count and total size
- History includes deployment start and end times
- Failed deployments are also recorded with error summary

**US-034: View recent deployment history**

**Description:** As a developer, I want to view recent deployment history so that I can check when I last deployed and if it succeeded.

**Acceptance criteria:**
- `ftpsheep history` command displays recent deployments
- Default display shows last 10 deployments
- Each entry shows date/time, profile name, result, and duration
- Success/failure indicated clearly
- Option to show more history with `--count` parameter
- Option to filter by profile name

**US-035: Export deployment history**

**Description:** As a developer, I want to export deployment history so that I can keep records or share with my team.

**Acceptance criteria:**
- `ftpsheep history --export` command exports to file
- Export format is JSON or CSV
- Export includes all available deployment details
- Export file location is displayed after creation
- Option to filter exported history by date range or profile

### Error handling and recovery

**US-036: Display actionable error messages**

**Description:** As a developer encountering an error, I want clear, actionable error messages so that I can quickly resolve the issue.

**Acceptance criteria:**
- Error messages clearly state what went wrong
- Error messages suggest potential solutions when possible
- Common errors have specific, helpful messages (e.g., "Cannot connect to server. Check that the server address and port are correct.")
- Errors reference documentation or help resources when appropriate
- Technical details available in verbose mode

**US-037: Handle authentication failures**

**Description:** As a developer, I want clear feedback when authentication fails so that I can correct my credentials.

**Acceptance criteria:**
- Authentication failures are detected and reported clearly
- Error message distinguishes between username and password errors when possible
- User is prompted to re-enter credentials
- Option to update saved credentials after authentication failure
- Suggestion to check username and password in profile

**US-038: Handle insufficient permissions**

**Description:** As a developer, I want clear feedback when my FTP account lacks necessary permissions so that I can address access issues.

**Acceptance criteria:**
- Permission errors detected (cannot write, cannot delete, etc.)
- Clear message indicates which operation failed due to permissions
- Suggestion to check FTP account permissions with hosting provider
- Deployment aborts safely without partial changes when possible

**US-039: Handle disk space issues**

**Description:** As a developer, I want to be notified if the server runs out of disk space so that I can address storage issues.

**Acceptance criteria:**
- Disk space errors detected during upload
- Clear message indicates server is out of space
- Upload stops to prevent further issues
- Temporary folder cleaned up if possible
- Suggestion to free up space on server or contact hosting provider

**US-040: Exit with appropriate codes**

**Description:** As a developer using the tool in scripts, I want appropriate exit codes so that my scripts can detect success or failure.

**Acceptance criteria:**
- Exit code 0 for successful deployment
- Exit code 1 for general errors
- Exit code 2 for build failures
- Exit code 3 for connection failures
- Exit code 4 for authentication failures
- Exit codes documented in help and documentation

### Help and documentation

**US-041: Display help information**

**Description:** As a developer using the tool, I want help information available so that I can learn how to use commands and options.

**Acceptance criteria:**
- `ftpsheep --help` displays general help and command list
- `ftpsheep [command] --help` displays help for specific command
- Help includes command syntax, parameters, and examples
- Help is formatted clearly and readable in console
- Help references full documentation URL

**US-042: Display version information**

**Description:** As a developer, I want to check the tool version so that I can verify I'm using the latest version or report bugs accurately.

**Acceptance criteria:**
- `ftpsheep --version` displays current version number
- Version follows semantic versioning (e.g., 1.0.0)
- Version command also shows build date or commit hash for debugging
- Version information shown in help output as well

**US-043: Initialize new project with profile**

**Description:** As a developer starting with FTPSheep.NET, I want a guided setup command so that I can quickly create my first deployment profile.

**Acceptance criteria:**
- `ftpsheep init` command starts interactive setup
- User is asked whether to import from Visual Studio or create new profile
- Step-by-step prompts for all required settings
- Connection is tested before saving profile
- Profile is saved and ready to use
- Next steps displayed (how to run first deployment)

### Advanced configuration

**US-044: Configure concurrency level**

**Description:** As a developer, I want to control how many files are uploaded simultaneously so that I can optimize for my connection and server.

**Acceptance criteria:**
- Concurrency level configurable in deployment profile
- `--concurrency` flag overrides profile setting
- Valid range: 1-20 concurrent uploads
- Default is 4-8 concurrent uploads
- Lower concurrency for slower connections or sensitive servers
- Higher concurrency for fast connections and robust servers

**US-045: Configure timeout values**

**Description:** As a developer working with slow or distant servers, I want to configure timeout values so that deployments don't fail prematurely.

**Acceptance criteria:**
- Connection timeout configurable in profile
- Operation timeout configurable in profile
- Timeout values specified in seconds
- `--timeout` flag overrides profile settings
- Reasonable defaults provided (connection: 30s, operation: 60s)
- Very long operations (large file uploads) don't timeout inappropriately

**US-046: Exclude files from deployment**

**Description:** As a developer, I want to exclude certain files or patterns from deployment so that I can avoid uploading unnecessary or sensitive files.

**Acceptance criteria:**
- Exclusion patterns configurable in deployment profile
- Supports glob patterns (e.g., "*.log", "temp/*")
- Multiple exclusion patterns can be specified
- Excluded files not included in file count or size calculations
- Exclusions logged in verbose mode
- Default exclusions for common non-deployable files (.git, .vs, etc.)

**US-047: Specify custom build arguments**

**Description:** As a developer with complex build requirements, I want to pass custom arguments to the build tool so that I can control build behavior.

**Acceptance criteria:**
- Custom build arguments configurable in deployment profile
- Arguments passed directly to msbuild or dotnet publish
- `--build-args` flag allows runtime override
- Arguments properly escaped and quoted
- Invalid arguments result in build failure with clear message

**US-048: Use pre-built publish folder**

**Description:** As a developer who has already built my project, I want to skip the build step and deploy from an existing publish folder so that I can save time.

**Acceptance criteria:**
- `--publish-folder` flag specifies pre-built folder path
- Tool validates that folder exists and contains files
- Build step is skipped when publish folder specified
- All other deployment steps proceed normally
- Useful for testing deployment without rebuilding

**US-049: Run deployment in dry-run mode**

**Description:** As a developer, I want to simulate deployment without making changes so that I can verify settings and see what would be deployed.

**Acceptance criteria:**
- `--dry-run` flag enables simulation mode
- Tool performs all steps except actual upload and server changes
- Dry run shows files that would be uploaded
- Dry run validates connection and credentials
- Dry run displays pre-deployment summary
- Clear indication that no changes were made
- Exit code indicates success of simulation

**US-050: Deploy with auto-confirmation**

**Description:** As a developer using the tool in automated scripts, I want to skip confirmation prompts so that deployments can run unattended.

**Acceptance criteria:**
- `--yes` or `-y` flag skips all confirmation prompts
- Deployment proceeds immediately after validation
- Suitable for CI/CD pipeline usage
- Still displays summary and progress
- Errors still cause deployment to abort