using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.Tests.Unit.Mocks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodexBarWin.Tests.Unit.Services;

[TestClass]
public class CacheServiceTests
{
    private Mock<ILogger<CacheService>> _mockLogger = null!;
    private MockSettingsService _mockSettingsService = null!;
    private CacheService _cacheService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CacheService>>();
        _mockSettingsService = new MockSettingsService();
        _cacheService = new CacheService(_mockLogger.Object, _mockSettingsService);
    }

    [TestMethod]
    public void Get_NonExistentProvider_ReturnsNull()
    {
        // Act
        var result = _cacheService.Get("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void Set_ThenGet_ReturnsCachedData()
    {
        // Arrange
        var data = new UsageData
        {
            Provider = "claude",
            Plan = "Pro",
            FetchedAt = DateTime.UtcNow
        };

        // Act
        _cacheService.Set("claude", data);
        var result = _cacheService.Get("claude");

        // Assert
        result.Should().NotBeNull();
        result!.Provider.Should().Be("claude");
        result.Plan.Should().Be("Pro");
    }

    [TestMethod]
    public void Get_ExpiredCache_ReturnsNull()
    {
        // Arrange
        _mockSettingsService.Settings.CacheExpiryMinutes = 5;
        var oldData = new UsageData
        {
            Provider = "claude",
            FetchedAt = DateTime.UtcNow.AddMinutes(-6)
        };

        _cacheService.Set("claude", oldData);

        // Act
        var result = _cacheService.Get("claude");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void Get_NotExpiredCache_ReturnsData()
    {
        // Arrange
        _mockSettingsService.Settings.CacheExpiryMinutes = 5;
        var recentData = new UsageData
        {
            Provider = "claude",
            FetchedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _cacheService.Set("claude", recentData);

        // Act
        var result = _cacheService.Get("claude");

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Clear_RemovesAllCachedData()
    {
        // Arrange
        _cacheService.Set("claude", new UsageData { Provider = "claude", FetchedAt = DateTime.UtcNow });
        _cacheService.Set("codex", new UsageData { Provider = "codex", FetchedAt = DateTime.UtcNow });

        // Act
        _cacheService.Clear();

        // Assert
        _cacheService.Get("claude").Should().BeNull();
        _cacheService.Get("codex").Should().BeNull();
    }

    [TestMethod]
    public void GetAll_ReturnsOnlyNonExpiredData()
    {
        // Arrange
        _mockSettingsService.Settings.CacheExpiryMinutes = 5;

        var recentData = new UsageData { Provider = "claude", FetchedAt = DateTime.UtcNow };
        var oldData = new UsageData { Provider = "codex", FetchedAt = DateTime.UtcNow.AddMinutes(-10) };

        _cacheService.Set("claude", recentData);
        _cacheService.Set("codex", oldData);

        // Act
        var result = _cacheService.GetAll();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("claude");
        result.Should().NotContainKey("codex");
    }

    [TestMethod]
    public void GetAll_EmptyCache_ReturnsEmptyDictionary()
    {
        // Act
        var result = _cacheService.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Set_OverwritesExistingData()
    {
        // Arrange
        var originalData = new UsageData { Provider = "claude", Plan = "Free", FetchedAt = DateTime.UtcNow };
        var updatedData = new UsageData { Provider = "claude", Plan = "Pro", FetchedAt = DateTime.UtcNow };

        // Act
        _cacheService.Set("claude", originalData);
        _cacheService.Set("claude", updatedData);
        var result = _cacheService.Get("claude");

        // Assert
        result.Should().NotBeNull();
        result!.Plan.Should().Be("Pro");
    }

    [TestMethod]
    public void Get_CacheExpiryBoundary_ReturnsDataJustBeforeExpiry()
    {
        // Arrange
        _mockSettingsService.Settings.CacheExpiryMinutes = 5;
        var almostExpiredData = new UsageData
        {
            Provider = "claude",
            FetchedAt = DateTime.UtcNow.AddMinutes(-4).AddSeconds(-59)
        };

        _cacheService.Set("claude", almostExpiredData);

        // Act
        var result = _cacheService.Get("claude");

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Get_NullSettingsService_UsesDefaultExpiry()
    {
        // Arrange
        var cacheServiceWithoutSettings = new CacheService(_mockLogger.Object, settingsService: null);
        var data = new UsageData
        {
            Provider = "claude",
            FetchedAt = DateTime.UtcNow.AddMinutes(-4)
        };

        cacheServiceWithoutSettings.Set("claude", data);

        // Act
        var result = cacheServiceWithoutSettings.Get("claude");

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void GetAll_MultipleProviders_ReturnsAllNonExpired()
    {
        // Arrange
        _mockSettingsService.Settings.CacheExpiryMinutes = 10;

        _cacheService.Set("claude", new UsageData { Provider = "claude", FetchedAt = DateTime.UtcNow });
        _cacheService.Set("codex", new UsageData { Provider = "codex", FetchedAt = DateTime.UtcNow });
        _cacheService.Set("gemini", new UsageData { Provider = "gemini", FetchedAt = DateTime.UtcNow });

        // Act
        var result = _cacheService.GetAll();

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().Contain(["claude", "codex", "gemini"]);
    }
}
