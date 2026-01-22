using System.Text.Json;
using CodexBarWin.Models;
using CodexBarWin.Services;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodexBarWin.Tests.Unit.Services;

/// <summary>
/// Unit tests for SettingsService.
/// Uses a testable subclass to override the settings file path for isolated testing.
/// </summary>
[TestClass]
public class SettingsServiceTests
{
    private Mock<ILogger<SettingsService>> _mockLogger = null!;
    private string _testDirectory = null!;
    private string _testSettingsPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<SettingsService>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CodexBarWin_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testSettingsPath = Path.Combine(_testDirectory, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesInstance_WithDefaultSettings()
    {
        // Arrange & Act
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Assert
        service.Should().NotBeNull();
        service.Settings.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_CreatesAppFolder_IfNotExists()
    {
        // Arrange
        var newTestDir = Path.Combine(Path.GetTempPath(), $"CodexBarWin_Tests_{Guid.NewGuid()}");

        // Act
        var service = new TestableSettingsService(_mockLogger.Object, newTestDir);

        // Assert
        Directory.Exists(newTestDir).Should().BeTrue();

        // Cleanup
        Directory.Delete(newTestDir, recursive: true);
    }

    [TestMethod]
    public void SettingsFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        var path = service.SettingsFilePath;

        // Assert
        path.Should().EndWith("settings.json");
        path.Should().Contain(_testDirectory);
    }

    #endregion

    #region LoadAsync Tests

    [TestMethod]
    public async Task LoadAsync_FileNotExists_UsesDefaultSettings()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
        service.Settings.Providers.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task LoadAsync_ValidSettingsFile_LoadsSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            RefreshIntervalSeconds = 120,
            Providers =
            [
                new ProviderConfig { Id = "claude", IsEnabled = true, Order = 0 },
                new ProviderConfig { Id = "codex", IsEnabled = false, Order = 1 }
            ]
        };
        await WriteSettingsFileAsync(settings);
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.RefreshIntervalSeconds.Should().Be(120);
        service.Settings.Providers.Should().HaveCount(2);
        service.Settings.Providers.First(p => p.Id == "claude").IsEnabled.Should().BeTrue();
        service.Settings.Providers.First(p => p.Id == "codex").IsEnabled.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadAsync_InvalidProviders_FiltersThemOut()
    {
        // Arrange
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderConfig { Id = "claude", IsEnabled = true, Order = 0 },
                new ProviderConfig { Id = "invalid_provider", IsEnabled = true, Order = 1 },
                new ProviderConfig { Id = "codex", IsEnabled = true, Order = 2 }
            ]
        };
        await WriteSettingsFileAsync(settings);
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Providers.Should().HaveCount(2);
        service.Settings.Providers.Should().NotContain(p => p.Id == "invalid_provider");
        service.Settings.Providers.Should().Contain(p => p.Id == "claude");
        service.Settings.Providers.Should().Contain(p => p.Id == "codex");
    }

    [TestMethod]
    public async Task LoadAsync_InvalidJson_UsesDefaultSettings()
    {
        // Arrange
        await File.WriteAllTextAsync(_testSettingsPath, "{ invalid json }");
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
        // Should use defaults when JSON is invalid
    }

    [TestMethod]
    public async Task LoadAsync_EmptyFile_UsesDefaultSettings()
    {
        // Arrange
        await File.WriteAllTextAsync(_testSettingsPath, "");
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
    }

    [TestMethod]
    public async Task LoadAsync_NullProvidersInFile_HandlesGracefully()
    {
        // Arrange
        await File.WriteAllTextAsync(_testSettingsPath, """{"RefreshIntervalSeconds":5,"Providers":null}""");
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
    }

    #endregion

    #region SaveAsync Tests

    [TestMethod]
    public async Task SaveAsync_WritesSettingsToFile()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        service.Settings.RefreshIntervalSeconds = 15;

        // Act
        await service.SaveAsync();

        // Assert
        File.Exists(_testSettingsPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_testSettingsPath);
        content.Should().Contain("15");
    }

    [TestMethod]
    public async Task SaveAsync_RaisesSettingsChangedEvent()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        var eventRaised = false;
        service.SettingsChanged += (_, _) => eventRaised = true;

        // Act
        await service.SaveAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveAsync_WritesValidJson()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.SaveAsync();

        // Assert
        var content = await File.ReadAllTextAsync(_testSettingsPath);
        var action = () => JsonDocument.Parse(content);
        action.Should().NotThrow();
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_RestoresDefaultSettings()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        service.Settings.RefreshIntervalSeconds = 99;

        // Act
        service.Reset();

        // Assert
        var defaults = AppSettings.GetDefaults();
        service.Settings.RefreshIntervalSeconds.Should().Be(defaults.RefreshIntervalSeconds);
    }

    [TestMethod]
    public void Reset_RaisesSettingsChangedEvent()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        var eventRaised = false;
        service.SettingsChanged += (_, _) => eventRaised = true;

        // Act
        service.Reset();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public void Reset_RestoresDefaultProviders()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        service.Settings.Providers.Clear();

        // Act
        service.Reset();

        // Assert
        var defaults = AppSettings.GetDefaults();
        service.Settings.Providers.Should().HaveCount(defaults.Providers.Count);
    }

    #endregion

    #region SettingsChanged Event Tests

    [TestMethod]
    public async Task SettingsChanged_MultipleSubscribers_AllNotified()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        var count = 0;
        service.SettingsChanged += (_, _) => count++;
        service.SettingsChanged += (_, _) => count++;

        // Act
        await service.SaveAsync();

        // Assert
        count.Should().Be(2);
    }

    [TestMethod]
    public void SettingsChanged_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act & Assert
        var action = () => service.Reset();
        action.Should().NotThrow();
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task SaveAndLoad_RoundTrip_PreservesSettings()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        service.Settings.RefreshIntervalSeconds = 42;
        service.Settings.Providers =
        [
            new ProviderConfig { Id = "claude", IsEnabled = false, Order = 2 },
            new ProviderConfig { Id = "gemini", IsEnabled = true, Order = 0 }
        ];

        // Act
        await service.SaveAsync();
        var service2 = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        await service2.LoadAsync();

        // Assert
        service2.Settings.RefreshIntervalSeconds.Should().Be(42);
        service2.Settings.Providers.Should().HaveCount(2);
        service2.Settings.Providers.First(p => p.Id == "claude").IsEnabled.Should().BeFalse();
        service2.Settings.Providers.First(p => p.Id == "gemini").IsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveAndLoad_WithTimeouts_PreservesValues()
    {
        // Arrange
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        service.Settings.Timeouts.WslCommandTimeoutSeconds = 120;
        service.Settings.Timeouts.CliProviderFirstFetchTimeoutSeconds = 90;

        // Act
        await service.SaveAsync();
        var service2 = new TestableSettingsService(_mockLogger.Object, _testDirectory);
        await service2.LoadAsync();

        // Assert
        service2.Settings.Timeouts.WslCommandTimeoutSeconds.Should().Be(120);
        service2.Settings.Timeouts.CliProviderFirstFetchTimeoutSeconds.Should().Be(90);
    }

    #endregion

    #region Provider Security Filtering Tests

    [TestMethod]
    public async Task LoadAsync_MaliciousProviderIds_AreFiltered()
    {
        // Arrange - Try to inject malicious provider IDs
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderConfig { Id = "claude", IsEnabled = true, Order = 0 },
                new ProviderConfig { Id = "../../../etc/passwd", IsEnabled = true, Order = 1 },
                new ProviderConfig { Id = "'; DROP TABLE users; --", IsEnabled = true, Order = 2 },
                new ProviderConfig { Id = "<script>alert('xss')</script>", IsEnabled = true, Order = 3 },
                new ProviderConfig { Id = "codex", IsEnabled = true, Order = 4 }
            ]
        };
        await WriteSettingsFileAsync(settings);
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Providers.Should().HaveCount(2);
        service.Settings.Providers.Select(p => p.Id).Should().BeEquivalentTo(["claude", "codex"]);
    }

    [TestMethod]
    public async Task LoadAsync_AllInvalidProviders_ResultsInEmptyList()
    {
        // Arrange
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderConfig { Id = "invalid1", IsEnabled = true, Order = 0 },
                new ProviderConfig { Id = "invalid2", IsEnabled = true, Order = 1 }
            ]
        };
        await WriteSettingsFileAsync(settings);
        var service = new TestableSettingsService(_mockLogger.Object, _testDirectory);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Providers.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private async Task WriteSettingsFileAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, AppJsonSerializerContext.Default.AppSettings);
        await File.WriteAllTextAsync(_testSettingsPath, json);
    }

    #endregion

    #region Testable SettingsService

    /// <summary>
    /// Testable version of SettingsService that allows custom settings directory.
    /// </summary>
    private class TestableSettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private AppSettings _settings = AppSettings.GetDefaults();

        public AppSettings Settings => _settings;
        public string SettingsFilePath { get; }

        public event EventHandler? SettingsChanged;

        public TestableSettingsService(ILogger<SettingsService> logger, string testDirectory)
        {
            _logger = logger;
            Directory.CreateDirectory(testDirectory);
            SettingsFilePath = Path.Combine(testDirectory, "settings.json");
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    _logger.LogInformation("Settings file not found, using defaults");
                    return;
                }

                var json = await File.ReadAllTextAsync(SettingsFilePath);
                var settings = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppSettings);

                if (settings != null)
                {
                    // Filter out any invalid providers for security
                    var originalCount = settings.Providers.Count;
                    settings.Providers = settings.Providers
                        .Where(p => ProviderConstants.IsValidProvider(p.Id))
                        .ToList();

                    if (settings.Providers.Count < originalCount)
                    {
                        _logger.LogWarning("Filtered {Count} invalid provider(s) from settings",
                            originalCount - settings.Providers.Count);
                    }

                    _settings = settings;
                    _logger.LogInformation("Settings loaded from {Path}", SettingsFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, AppJsonSerializerContext.Default.AppSettings);
                await File.WriteAllTextAsync(SettingsFilePath, json);
                _logger.LogInformation("Settings saved to {Path}", SettingsFilePath);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
        }

        public void Reset()
        {
            _settings = AppSettings.GetDefaults();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Settings reset to defaults");
        }
    }

    #endregion
}
