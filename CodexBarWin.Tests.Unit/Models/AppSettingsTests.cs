using CodexBarWin.Models;
using FluentAssertions;

namespace CodexBarWin.Tests.Unit.Models;

[TestClass]
public class AppSettingsTests
{
    [TestMethod]
    public void GetDefaults_ReturnsValidDefaults()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Should().NotBeNull();
        settings.Version.Should().Be(1);
        settings.Providers.Should().HaveCount(3);
        settings.RefreshIntervalSeconds.Should().Be(120);
        settings.StartWithWindows.Should().BeFalse();
        settings.StartMinimized.Should().BeTrue();
        settings.Theme.Should().Be(AppTheme.System);
    }

    [TestMethod]
    public void GetDefaults_IncludesAllProviders()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Providers.Should().Contain(p => p.Id == "claude");
        settings.Providers.Should().Contain(p => p.Id == "codex");
        settings.Providers.Should().Contain(p => p.Id == "gemini");
    }

    [TestMethod]
    public void GetDefaults_ProvidersAreEnabled()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Providers.Should().AllSatisfy(p => p.IsEnabled.Should().BeTrue());
    }

    [TestMethod]
    public void GetDefaults_IncludesTimeoutSettings()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Timeouts.Should().NotBeNull();
        settings.Timeouts.WslCommandTimeoutSeconds.Should().Be(30);
        settings.Timeouts.WslStatusCheckTimeoutSeconds.Should().Be(10);
        settings.Timeouts.CliProviderFirstFetchTimeoutSeconds.Should().Be(60);
        settings.Timeouts.CliProviderTimeoutSeconds.Should().Be(45);
        settings.Timeouts.StandardProviderFirstFetchTimeoutSeconds.Should().Be(20);
        settings.Timeouts.StandardProviderTimeoutSeconds.Should().Be(10);
    }

    [TestMethod]
    public void GetDefaults_IncludesAnimationSettings()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Animation.Should().NotBeNull();
        settings.Animation.ShowDurationMs.Should().Be(150);
        settings.Animation.HideDurationMs.Should().Be(100);
        settings.Animation.Steps.Should().Be(15);
    }

    [TestMethod]
    public void GetDefaults_IncludesCacheExpiry()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.CacheExpiryMinutes.Should().Be(5);
    }

    [TestMethod]
    public void GetDefaults_IncludesGuideUrl()
    {
        // Act
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.CodexBarGuideUrl.Should().NotBeNullOrEmpty();
        settings.CodexBarGuideUrl.Should().StartWith("https://");
    }
}

[TestClass]
public class TimeoutSettingsTests
{
    [TestMethod]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var settings = new TimeoutSettings();

        // Assert
        settings.WslCommandTimeoutSeconds.Should().Be(30);
        settings.WslStatusCheckTimeoutSeconds.Should().Be(10);
        settings.CliProviderFirstFetchTimeoutSeconds.Should().Be(60);
        settings.CliProviderTimeoutSeconds.Should().Be(45);
        settings.StandardProviderFirstFetchTimeoutSeconds.Should().Be(20);
        settings.StandardProviderTimeoutSeconds.Should().Be(10);
    }
}

[TestClass]
public class AnimationSettingsTests
{
    [TestMethod]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var settings = new AnimationSettings();

        // Assert
        settings.ShowDurationMs.Should().Be(150);
        settings.HideDurationMs.Should().Be(100);
        settings.Steps.Should().Be(15);
    }
}
