using FTPSheep.Core.Exceptions;
using FTPSheep.Core.Services;

namespace FTPSheep.Tests.Services;

public class PublishProfileParserTests : IDisposable {
    private readonly string testDirectory;
    private readonly PublishProfileParser parser;

    public PublishProfileParserTests() {
        testDirectory = Path.Combine(Path.GetTempPath(), $"ftpsheep-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
        parser = new PublishProfileParser();
    }

    public void Dispose() {
        if(Directory.Exists(testDirectory)) {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    #region ParseProfile Tests

    [Fact]
    public void ParseProfile_WithNullPath_ThrowsArgumentException() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => parser.ParseProfile(null!));
    }

    [Fact]
    public void ParseProfile_WithEmptyPath_ThrowsArgumentException() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => parser.ParseProfile(string.Empty));
    }

    [Fact]
    public void ParseProfile_WithNonExistentFile_ThrowsFileNotFoundException() {
        // Arrange
        var path = Path.Combine(testDirectory, "nonexistent.pubxml");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => parser.ParseProfile(path));
    }

    [Fact]
    public void ParseProfile_WithValidFtpProfile_ParsesCorrectly() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "test.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <WebPublishMethod>FTP</WebPublishMethod>
    <PublishUrl>ftp://ftp.example.com/site/wwwroot</PublishUrl>
    <UserName>testuser</UserName>
    <DeleteExistingFiles>true</DeleteExistingFiles>
    <TargetFramework>net8.0</TargetFramework>
    <SelfContained>false</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishProtocol>ftps</PublishProtocol>
  </PropertyGroup>
</Project>";
        File.WriteAllText(pubxmlPath, xml);

        // Act
        var profile = parser.ParseProfile(pubxmlPath);

        // Assert
        Assert.Equal("FTP", profile.PublishMethod);
        Assert.Equal("ftp://ftp.example.com/site/wwwroot", profile.PublishUrl);
        Assert.Equal("testuser", profile.UserName);
        Assert.True(profile.DeleteExistingFiles);
        Assert.Equal("net8.0", profile.TargetFramework);
        Assert.False(profile.SelfContained);
        Assert.Equal("win-x64", profile.RuntimeIdentifier);
        Assert.Equal("ftps", profile.PublishProtocol);
        Assert.Equal(pubxmlPath, profile.SourceFilePath);
    }

    [Fact]
    public void ParseProfile_WithPublishMethodElement_ParsesCorrectly() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "test2.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <PublishMethod>FTP</PublishMethod>
    <PublishUrl>ftp.example.com</PublishUrl>
  </PropertyGroup>
</Project>";
        File.WriteAllText(pubxmlPath, xml);

        // Act
        var profile = parser.ParseProfile(pubxmlPath);

        // Assert
        Assert.Equal("FTP", profile.PublishMethod);
        Assert.Equal("ftp.example.com", profile.PublishUrl);
    }

    [Fact]
    public void ParseProfile_WithSavePWDTrue_ParsesCorrectly() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "test3.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <WebPublishMethod>FTP</WebPublishMethod>
    <PublishUrl>ftp.example.com</PublishUrl>
    <SavePWD>true</SavePWD>
  </PropertyGroup>
</Project>";
        File.WriteAllText(pubxmlPath, xml);

        // Act
        var profile = parser.ParseProfile(pubxmlPath);

        // Assert
        Assert.True(profile.SavePwd);
    }

    [Fact]
    public void ParseProfile_WithAdditionalProperties_CapturesProperties() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "test4.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <WebPublishMethod>FTP</WebPublishMethod>
    <PublishUrl>ftp.example.com</PublishUrl>
    <CustomProperty>CustomValue</CustomProperty>
    <AnotherProperty>AnotherValue</AnotherProperty>
  </PropertyGroup>
</Project>";
        File.WriteAllText(pubxmlPath, xml);

        // Act
        var profile = parser.ParseProfile(pubxmlPath);

        // Assert
        Assert.Equal(2, profile.AdditionalProperties.Count);
        Assert.Equal("CustomValue", profile.AdditionalProperties["CustomProperty"]);
        Assert.Equal("AnotherValue", profile.AdditionalProperties["AnotherProperty"]);
    }

    [Fact]
    public void ParseProfile_WithInvalidXml_ThrowsProfileException() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "invalid.pubxml");
        File.WriteAllText(pubxmlPath, "invalid xml content <><");

        // Act & Assert
        var ex = Assert.Throws<ProfileException>(() => parser.ParseProfile(pubxmlPath));
        Assert.Contains(pubxmlPath, ex.Message);
    }

    [Fact]
    public void ParseProfile_WithoutPropertyGroup_ThrowsProfileException() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "no-propgroup.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>";
        File.WriteAllText(pubxmlPath, xml);

        // Act & Assert
        Assert.Throws<ProfileException>(() => parser.ParseProfile(pubxmlPath));
    }

    [Fact]
    public async Task ParseProfileAsync_WithValidProfile_ParsesCorrectly() {
        // Arrange
        var pubxmlPath = Path.Combine(testDirectory, "async-test.pubxml");
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <WebPublishMethod>FTP</WebPublishMethod>
    <PublishUrl>ftp.example.com</PublishUrl>
    <UserName>asyncuser</UserName>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(pubxmlPath, xml);

        // Act
        var profile = await parser.ParseProfileAsync(pubxmlPath);

        // Assert
        Assert.Equal("FTP", profile.PublishMethod);
        Assert.Equal("ftp.example.com", profile.PublishUrl);
        Assert.Equal("asyncuser", profile.UserName);
        Assert.Equal(pubxmlPath, profile.SourceFilePath);
    }

    #endregion

    #region DiscoverProfiles Tests

    [Fact]
    public void DiscoverProfiles_WithStandardLocation_FindsProfiles() {
        // Arrange
        var propertiesDir = Path.Combine(testDirectory, "Properties", "PublishProfiles");
        Directory.CreateDirectory(propertiesDir);
        var profile1 = Path.Combine(propertiesDir, "Profile1.pubxml");
        var profile2 = Path.Combine(propertiesDir, "Profile2.pubxml");
        File.WriteAllText(profile1, "<Project />");
        File.WriteAllText(profile2, "<Project />");

        // Act
        var profiles = parser.DiscoverProfiles(testDirectory);

        // Assert
        Assert.Equal(2, profiles.Count);
        Assert.Contains(profile1, profiles);
        Assert.Contains(profile2, profiles);
    }

    [Fact]
    public void DiscoverProfiles_WithNonStandardLocation_FindsProfiles() {
        // Arrange
        var subDir = Path.Combine(testDirectory, "SubDir");
        Directory.CreateDirectory(subDir);
        var profile = Path.Combine(subDir, "Profile.pubxml");
        File.WriteAllText(profile, "<Project />");

        // Act
        var profiles = parser.DiscoverProfiles(testDirectory);

        // Assert
        Assert.Single(profiles);
        Assert.Contains(profile, profiles);
    }

    [Fact]
    public void DiscoverProfiles_WithNoProfiles_ReturnsEmptyList() {
        // Act
        var profiles = parser.DiscoverProfiles(testDirectory);

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public void DiscoverProfiles_WithNullPath_UsesCurrentDirectory() {
        // Act
        var profiles = parser.DiscoverProfiles(null!);

        // Assert - should not throw and returns a list
        Assert.NotNull(profiles);
    }

    [Fact]
    public void DiscoverProfiles_WithNonExistentDirectory_ReturnsEmptyList() {
        // Arrange
        var nonExistentPath = Path.Combine(testDirectory, "nonexistent");

        // Act
        var profiles = parser.DiscoverProfiles(nonExistentPath);

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public void DiscoverProfiles_PrefersStandardLocation_OverRecursiveSearch() {
        // Arrange
        // Create profile in standard location
        var propertiesDir = Path.Combine(testDirectory, "Properties", "PublishProfiles");
        Directory.CreateDirectory(propertiesDir);
        var standardProfile = Path.Combine(propertiesDir, "Standard.pubxml");
        File.WriteAllText(standardProfile, "<Project />");

        // Create profile in non-standard location
        var otherDir = Path.Combine(testDirectory, "OtherDir");
        Directory.CreateDirectory(otherDir);
        var otherProfile = Path.Combine(otherDir, "Other.pubxml");
        File.WriteAllText(otherProfile, "<Project />");

        // Act
        var profiles = parser.DiscoverProfiles(testDirectory);

        // Assert - should only find the standard location profile
        Assert.Single(profiles);
        Assert.Contains(standardProfile, profiles);
        Assert.DoesNotContain(otherProfile, profiles);
    }

    #endregion
}
