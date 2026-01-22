using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.Tests.Unit.Mocks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodexBarWin.Tests.Unit.Services;

[TestClass]
public class CodexBarServiceTests
{
    private MockWslService _mockWslService = null!;
    private MockCacheService _mockCacheService = null!;
    private MockSettingsService _mockSettingsService = null!;
    private Mock<ILogger<CodexBarService>> _mockLogger = null!;
    private CodexBarService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockWslService = new MockWslService();
        _mockCacheService = new MockCacheService();
        _mockSettingsService = new MockSettingsService();
        _mockLogger = new Mock<ILogger<CodexBarService>>();

        _service = new CodexBarService(
            _mockWslService,
            _mockCacheService,
            _mockSettingsService,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetUsageAsync_ValidProvider_ExecutesCommand()
    {
        // Arrange
        // ParseUsageJson expects a single object, not an array
        var jsonResponse = """{"provider":"claude","usage":{"loginMethod":"test","primary":{"usedPercent":50,"windowMinutes":60}}}""";
        _mockWslService.SetCommandResult(
            "codexbar --provider claude --format json --source oauth",
            new WslResult { Success = true, Output = jsonResponse, ExitCode = 0 });

        // Act
        var result = await _service.GetUsageAsync("claude");

        // Assert
        result.Should().NotBeNull();
        _mockWslService.ExecutedCommands.Should().ContainSingle()
            .Which.Should().Contain("--provider claude");
    }

    [TestMethod]
    public async Task GetUsageAsync_InvalidProvider_ReturnsNull()
    {
        // The service catches all exceptions and returns cached/null data
        // rather than throwing to avoid crashing the UI

        // Act
        var result = await _service.GetUsageAsync("invalid");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetUsageAsync_CommandFails_ReturnsCachedData()
    {
        // Arrange
        var cachedData = new UsageData
        {
            Provider = "claude",
            Plan = "Pro",
            FetchedAt = DateTime.UtcNow
        };
        _mockCacheService.PreloadCache("claude", cachedData);

        _mockWslService.SetCommandResult(
            "codexbar --provider claude --format json --source oauth",
            new WslResult { Success = false, Error = "Command failed", ExitCode = 1 });

        // Act
        var result = await _service.GetUsageAsync("claude");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("claude");
    }

    [TestMethod]
    public async Task GetAllUsageAsync_FiltersInvalidProviders()
    {
        // Arrange
        _mockSettingsService.SetProviders(new List<ProviderConfig>
        {
            new() { Id = "claude", IsEnabled = true },
            new() { Id = "invalid", IsEnabled = true },  // Should be filtered
            new() { Id = "codex", IsEnabled = true }
        });

        // Act
        var results = await _service.GetAllUsageAsync();

        // Assert
        // Only valid providers should be fetched
        _mockWslService.ExecutedCommands.Should().HaveCount(2);
        _mockWslService.ExecutedCommands.Any(c => c.Contains("invalid")).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetAllUsageAsync_SkipsDisabledProviders()
    {
        // Arrange
        _mockSettingsService.SetProviders(new List<ProviderConfig>
        {
            new() { Id = "claude", IsEnabled = true },
            new() { Id = "codex", IsEnabled = false },
            new() { Id = "gemini", IsEnabled = true }
        });

        // Act
        var results = await _service.GetAllUsageAsync();

        // Assert
        _mockWslService.ExecutedCommands.Should().HaveCount(2);
        // Note: "codexbar" contains "codex", so check for "--provider codex" specifically
        _mockWslService.ExecutedCommands.Any(c => c.Contains("--provider codex")).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetAllUsageAsync_NoEnabledProviders_ReturnsEmpty()
    {
        // Arrange
        _mockSettingsService.SetProviders(new List<ProviderConfig>
        {
            new() { Id = "claude", IsEnabled = false },
            new() { Id = "codex", IsEnabled = false }
        });

        // Act
        var results = await _service.GetAllUsageAsync();

        // Assert
        results.Should().BeEmpty();
        _mockWslService.ExecutedCommands.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllUsageStreamAsync_YieldsResultsAsTheyComplete()
    {
        // Arrange
        _mockSettingsService.SetProviders(new List<ProviderConfig>
        {
            new() { Id = "claude", IsEnabled = true },
            new() { Id = "gemini", IsEnabled = true }
        });

        // Act
        var results = new List<UsageData>();
        await foreach (var data in _service.GetAllUsageStreamAsync())
        {
            results.Add(data);
        }

        // Assert
        results.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetVersionAsync_Success_ReturnsVersion()
    {
        // Arrange
        _mockWslService.SetCommandResult(
            "codexbar --version",
            new WslResult { Success = true, Output = "1.0.0", ExitCode = 0 });

        // Act
        var result = await _service.GetVersionAsync();

        // Assert
        result.Should().Be("1.0.0");
    }

    [TestMethod]
    public async Task GetVersionAsync_Failure_ReturnsNull()
    {
        // Arrange
        _mockWslService.SetCommandResult(
            "codexbar --version",
            new WslResult { Success = false, Error = "Not found", ExitCode = 1 });

        // Act
        var result = await _service.GetVersionAsync();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task IsAvailableAsync_CodexBarExists_ReturnsTrue()
    {
        // Arrange
        _mockWslService.SetCommandResult(
            "which codexbar",
            new WslResult { Success = true, Output = "/usr/local/bin/codexbar", ExitCode = 0 });

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsAvailableAsync_CodexBarNotFound_ReturnsFalse()
    {
        // Arrange
        _mockWslService.SetCommandResult(
            "which codexbar",
            new WslResult { Success = false, Output = "", ExitCode = 1 });

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetUsageAsync_SuccessfulParse_CachesData()
    {
        // Arrange
        // ParseUsageJson expects a single object, not an array
        var jsonResponse = """{"provider":"claude","usage":{"primary":{"usedPercent":75,"windowMinutes":60}}}""";
        _mockWslService.SetCommandResult(
            "codexbar --provider claude --format json --source oauth",
            new WslResult { Success = true, Output = jsonResponse, ExitCode = 0 });

        // Act
        await _service.GetUsageAsync("claude");

        // Assert
        _mockCacheService.SetCallCount.Should().Be(1);
    }
}
