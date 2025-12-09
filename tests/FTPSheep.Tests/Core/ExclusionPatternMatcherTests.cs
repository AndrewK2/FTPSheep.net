using FTPSheep.Core.Services;
using Xunit;

namespace FTPSheep.Tests.Core;

public class ExclusionPatternMatcherTests {
    #region Constructor Tests

    [Fact]
    public void Constructor_WithPatterns_InitializesCorrectly() {
        // Arrange
        var patterns = new[] { "*.log", "temp/**" };

        // Act
        var matcher = new ExclusionPatternMatcher(patterns);

        // Assert
        Assert.NotNull(matcher);
        Assert.Equal(2, matcher.Patterns.Count);
        Assert.Contains("*.log", matcher.Patterns);
        Assert.Contains("temp/**", matcher.Patterns);
    }

    [Fact]
    public void Constructor_WithNullPatterns_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExclusionPatternMatcher(null!));
    }

    [Fact]
    public void Constructor_Default_UsesDefaultPatterns() {
        // Act
        var matcher = new ExclusionPatternMatcher();

        // Assert
        Assert.NotNull(matcher);
        Assert.NotEmpty(matcher.Patterns);
        Assert.Contains("App_Data/**", matcher.Patterns);
        Assert.Contains("uploads/**", matcher.Patterns);
        Assert.Contains("logs/**", matcher.Patterns);
    }

    #endregion

    #region IsExcluded Tests - Simple Patterns

    [Theory]
    [InlineData("test.log", "*.log", true)]
    [InlineData("error.log", "*.log", true)]
    [InlineData("test.txt", "*.log", false)]
    [InlineData("test.LOG", "*.log", true)] // Case insensitive
    public void IsExcluded_SimpleWildcard_MatchesCorrectly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("test.pdb", "*.pdb", true)]
    [InlineData("assembly.xml", "*.xml", true)]
    [InlineData("config.json", "*.json", true)]
    [InlineData("readme.md", "*.json", false)]
    public void IsExcluded_FileExtensions_MatchesCorrectly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("test.l?g", "test.l?g", true)]
    [InlineData("test.log", "test.l?g", true)]
    [InlineData("test.lag", "test.l?g", true)]
    [InlineData("test.logg", "test.l?g", false)] // ? matches single char only
    public void IsExcluded_QuestionMarkWildcard_MatchesSingleCharacter(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsExcluded Tests - Directory Patterns

    [Theory]
    [InlineData("App_Data/users.db", "App_Data/**", true)]
    [InlineData("App_Data/logs/error.log", "App_Data/**", true)]
    [InlineData("App_Data/cache/temp/file.tmp", "App_Data/**", true)]
    [InlineData("OtherFolder/file.txt", "App_Data/**", false)]
    [InlineData("app_data/file.txt", "App_Data/**", true)] // Case insensitive
    public void IsExcluded_DoubleStarPattern_MatchesSubdirectories(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("temp/file.txt", "temp/*", true)]
    [InlineData("temp/sub/file.txt", "temp/*", false)] // * doesn't match subdirectories
    [InlineData("temp/anything.log", "temp/*", true)]
    public void IsExcluded_SingleStarInDirectory_MatchesDirectFilesOnly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("logs/error.log", "logs/**", true)]
    [InlineData("logs/2024/01/error.log", "logs/**", true)]
    [InlineData("app/logs/error.log", "logs/**", false)] // logs must be at root
    [InlineData("mylogs/error.log", "logs/**", false)]
    public void IsExcluded_DirectoryWithDoubleStarPattern_MatchesNestedFiles(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsExcluded Tests - Complex Patterns

    [Theory]
    [InlineData("appsettings.Development.json", "appsettings.*.json", true)]
    [InlineData("appsettings.Production.json", "appsettings.*.json", true)]
    [InlineData("appsettings.json", "appsettings.*.json", false)] // No middle part
    [InlineData("web.config", "appsettings.*.json", false)]
    public void IsExcluded_EnvironmentSpecificFiles_MatchesCorrectly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".git/config", ".git/**", true)]
    [InlineData(".git/objects/abc123", ".git/**", true)]
    [InlineData(".gitignore", ".git/**", false)]
    [InlineData("somedir/.git/config", ".git/**", false)] // .git must be at root
    public void IsExcluded_HiddenDirectories_MatchesCorrectly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("node_modules/package/index.js", "node_modules/**", true)]
    [InlineData("node_modules/deep/nested/path/file.js", "node_modules/**", true)]
    [InlineData("src/node_modules/package/index.js", "node_modules/**", false)]
    public void IsExcluded_NodeModules_MatchesCorrectly(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsExcluded Tests - Path Separators

    [Theory]
    [InlineData("App_Data\\users.db", "App_Data/**", true)]
    [InlineData("App_Data\\logs\\error.log", "App_Data/**", true)]
    [InlineData("temp\\file.txt", "temp/*", true)]
    public void IsExcluded_BackslashSeparators_NormalizedAndMatched(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("App_Data/users.db", "App_Data\\**", true)]
    [InlineData("temp/file.txt", "temp\\*", true)]
    public void IsExcluded_PatternWithBackslashes_NormalizedAndMatched(string path, string pattern, bool expected) {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { pattern });

        // Act
        var result = matcher.IsExcluded(path);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsExcluded Tests - Edge Cases

    [Fact]
    public void IsExcluded_NullPath_ReturnsFalse() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log" });

        // Act
        var result = matcher.IsExcluded(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExcluded_EmptyPath_ReturnsFalse() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log" });

        // Act
        var result = matcher.IsExcluded(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExcluded_WhitespacePath_ReturnsFalse() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log" });

        // Act
        var result = matcher.IsExcluded("   ");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region FilterExcluded Tests

    [Fact]
    public void FilterExcluded_WithMatchingPaths_RemovesExcludedPaths() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log", "temp/**" });
        var paths = new[] {
            "file1.txt",
            "error.log",
            "file2.txt",
            "temp/cache.tmp",
            "debug.log"
        };

        // Act
        var result = matcher.FilterExcluded(paths).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("file1.txt", result);
        Assert.Contains("file2.txt", result);
        Assert.DoesNotContain("error.log", result);
        Assert.DoesNotContain("temp/cache.tmp", result);
        Assert.DoesNotContain("debug.log", result);
    }

    [Fact]
    public void FilterExcluded_WithNullPaths_ThrowsArgumentNullException() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log" });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => matcher.FilterExcluded(null!));
    }

    [Fact]
    public void FilterExcluded_WithNoPatternsMatching_ReturnsAllPaths() {
        // Arrange
        var matcher = new ExclusionPatternMatcher(new[] { "*.log" });
        var paths = new[] { "file1.txt", "file2.txt", "file3.txt" };

        // Act
        var result = matcher.FilterExcluded(paths).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(paths, result);
    }

    #endregion

    #region CreateWithDefaults Tests

    [Fact]
    public void CreateWithDefaults_WithNoCustomPatterns_UsesOnlyDefaultPatterns() {
        // Act
        var matcher = ExclusionPatternMatcher.CreateWithDefaults();

        // Assert
        Assert.NotNull(matcher);
        Assert.Equal(ExclusionPatternMatcher.DefaultExclusionPatterns.Length, matcher.Patterns.Count);
    }

    [Fact]
    public void CreateWithDefaults_WithCustomPatterns_CombinesDefaultAndCustom() {
        // Arrange
        var customPatterns = new[] { "*.custom", "custom/**" };

        // Act
        var matcher = ExclusionPatternMatcher.CreateWithDefaults(customPatterns);

        // Assert
        Assert.NotNull(matcher);
        Assert.Equal(
            ExclusionPatternMatcher.DefaultExclusionPatterns.Length + 2,
            matcher.Patterns.Count);
        Assert.Contains("*.custom", matcher.Patterns);
        Assert.Contains("custom/**", matcher.Patterns);
        Assert.Contains("App_Data/**", matcher.Patterns); // Default pattern
    }

    #endregion

    #region IsValidPattern Tests

    [Theory]
    [InlineData("*.log", true)]
    [InlineData("temp/**", true)]
    [InlineData("App_Data/**", true)]
    [InlineData("file?.txt", true)]
    [InlineData("appsettings.*.json", true)]
    public void IsValidPattern_WithValidPatterns_ReturnsTrue(string pattern, bool expected) {
        // Act
        var result = ExclusionPatternMatcher.IsValidPattern(pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidPattern_WithInvalidPatterns_ReturnsFalse(string pattern, bool expected) {
        // Act
        var result = ExclusionPatternMatcher.IsValidPattern(pattern);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Multiple Patterns Tests

    [Fact]
    public void IsExcluded_WithMultiplePatterns_MatchesAnyPattern() {
        // Arrange
        var patterns = new[] { "*.log", "*.pdb", "temp/**", "App_Data/**" };
        var matcher = new ExclusionPatternMatcher(patterns);

        // Act & Assert
        Assert.True(matcher.IsExcluded("error.log"));
        Assert.True(matcher.IsExcluded("assembly.pdb"));
        Assert.True(matcher.IsExcluded("temp/cache.tmp"));
        Assert.True(matcher.IsExcluded("App_Data/users.db"));
        Assert.False(matcher.IsExcluded("file.txt"));
    }

    #endregion
}
