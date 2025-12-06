# FTPSheep.NET Testing Guide

This directory contains the test suite for FTPSheep.NET, including both unit tests and integration tests.

## Project Structure

```
tests/
├── FTPSheep.Tests/              # Unit tests
│   ├── Fixtures/                # Test fixtures and shared test infrastructure
│   ├── TestData/                # Sample data for testing
│   │   ├── SampleProfiles/      # Sample Visual Studio .pubxml files
│   │   └── SampleConfigs/       # Sample FTPSheep deployment configurations
│   └── *.cs                     # Unit test files
│
└── FTPSheep.IntegrationTests/   # Integration tests
    ├── Fixtures/                # Integration test fixtures
    ├── TestData/                # Integration test data
    └── *.cs                     # Integration test files
```

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider (optional)
- Docker Desktop (optional, for integration tests with containerized FTP server)

## Running Tests

### Run All Tests

```bash
# From solution root
dotnet test

# With detailed output
dotnet test --verbosity normal
```

### Run Unit Tests Only

```bash
dotnet test tests/FTPSheep.Tests/FTPSheep.Tests.csproj
```

### Run Integration Tests Only

```bash
dotnet test tests/FTPSheep.IntegrationTests/FTPSheep.IntegrationTests.csproj
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~FTPSheep.Tests.YourTestClass.YourTestMethod"
```

### Run Tests with Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Testing Frameworks and Tools

### xUnit
- **Purpose**: Primary testing framework
- **Usage**: All test classes use xUnit attributes (`[Fact]`, `[Theory]`, etc.)
- **Documentation**: https://xunit.net/

### Moq
- **Purpose**: Mocking framework for creating test doubles
- **Usage**: Mock dependencies like IFtpClient, IBuildTool, ICredentialStore
- **Documentation**: https://github.com/moq/moq4

Example:
```csharp
var mockFtpClient = new Mock<IFtpClient>();
mockFtpClient.Setup(x => x.Connect()).Returns(true);
```

## Test Data and Fixtures

### Sample Visual Studio Publish Profiles

Located in `FTPSheep.Tests/TestData/SampleProfiles/`:

- **Sample.pubxml**: Basic FTP profile with standard settings
- **Sample-SFTP.pubxml**: SFTP profile for testing SFTP protocol support
- **Sample-CustomPort.pubxml**: FTP profile with custom port configuration

Use these files to test .pubxml parsing and import functionality.

### Sample Deployment Configurations

Located in `FTPSheep.Tests/TestData/SampleConfigs/`:

- **basic-deployment.json**: Minimal deployment profile with required fields only
- **advanced-deployment.json**: Complete profile with all optional settings

Use these files to test profile serialization, deserialization, and validation.

## Integration Testing Setup

Integration tests verify end-to-end functionality including FTP connections, file uploads, and build integration.

### Option 1: Local FTP Server (Docker)

For testing FTP functionality, you can use a containerized FTP server:

```bash
# Run FileZilla FTP Server in Docker
docker run -d \
  --name ftpsheep-test-ftp \
  -p 21:21 \
  -p 21000-21010:21000-21010 \
  -e FTP_USER=testuser \
  -e FTP_PASS=testpass \
  -e PASV_ADDRESS=127.0.0.1 \
  fauria/vsftpd

# Stop and remove when done
docker stop ftpsheep-test-ftp
docker rm ftpsheep-test-ftp
```

### Option 2: Manual FTP Server Setup

1. Install and configure a local FTP server (e.g., FileZilla Server, IIS FTP)
2. Create a test user account
3. Configure test directory with write permissions
4. Update integration test configuration with connection details

### Test .NET Projects

Integration tests require sample .NET projects to build and deploy. These will be added as the project progresses:

- .NET Framework 4.8 Web Application
- .NET Core 3.1 Web Application
- .NET 6+ Web API
- Blazor Server Application

## Writing Tests

### Unit Test Example

```csharp
using Xunit;
using Moq;
using FTPSheep.Core;

namespace FTPSheep.Tests
{
    public class DeploymentProfileTests
    {
        [Fact]
        public void DeploymentProfile_Serialization_RoundTrip_Success()
        {
            // Arrange
            var profile = new DeploymentProfile
            {
                Name = "Test Profile",
                Server = "ftp.example.com",
                Port = 21
            };

            // Act
            var json = JsonSerializer.Serialize(profile);
            var deserialized = JsonSerializer.Deserialize<DeploymentProfile>(json);

            // Assert
            Assert.Equal(profile.Name, deserialized.Name);
            Assert.Equal(profile.Server, deserialized.Server);
            Assert.Equal(profile.Port, deserialized.Port);
        }
    }
}
```

### Integration Test Example

```csharp
using Xunit;

namespace FTPSheep.IntegrationTests
{
    public class FtpConnectionTests
    {
        [Fact(Skip = "Requires FTP server")]
        public async Task FtpClient_Connect_ValidCredentials_Success()
        {
            // Arrange
            var ftpClient = new FtpClient("ftp://localhost:21", "testuser", "testpass");

            // Act
            var connected = await ftpClient.ConnectAsync();

            // Assert
            Assert.True(connected);
        }
    }
}
```

## Test Naming Conventions

Follow the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `UploadFile_ValidFile_ReturnsSuccess`
- `ParseProfile_InvalidXml_ThrowsException`
- `BuildProject_MissingDependencies_ReturnsError`

## Continuous Integration

Tests are automatically run on every push and pull request via GitHub Actions (or your CI/CD platform).

The CI pipeline:
1. Restores NuGet packages
2. Builds the solution
3. Runs all unit tests
4. Runs integration tests (if FTP server is available)
5. Generates code coverage reports
6. Fails the build if any tests fail

## Troubleshooting

### Tests Fail to Discover

```bash
# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

### Integration Tests Timeout

- Ensure FTP server is running and accessible
- Check firewall settings
- Verify connection credentials
- Increase timeout in test configuration

### Moq Errors

- Ensure all mock setups match the actual interface signatures
- Verify return types are correct
- Check that async methods are properly mocked with `ReturnsAsync()`

## Code Coverage

To generate and view code coverage reports:

```bash
# Install report generator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate HTML report
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report

# Open report
start coverage-report/index.html  # Windows
open coverage-report/index.html   # macOS
```

## Contributing

When adding new features:
1. Write unit tests first (TDD approach recommended)
2. Ensure all existing tests pass
3. Aim for >80% code coverage on new code
4. Add integration tests for end-to-end scenarios
5. Update this README if adding new test categories or infrastructure

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [FTPSheep.NET Main Documentation](../README.md)
