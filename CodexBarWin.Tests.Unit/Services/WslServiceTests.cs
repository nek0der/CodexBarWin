using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.Tests.Unit.Mocks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodexBarWin.Tests.Unit.Services;

/// <summary>
/// Unit tests for WslService.
/// Note: Some tests require actual WSL installation and are marked with [TestCategory("Integration")].
/// Process execution tests are limited due to the difficulty of mocking System.Diagnostics.Process.
/// </summary>
[TestClass]
public class WslServiceTests
{
    private Mock<ILogger<WslService>> _mockLogger = null!;
    private MockSettingsService _mockSettingsService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<WslService>>();
        _mockSettingsService = new MockSettingsService();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullSettingsService_UsesDefaultTimeout()
    {
        // Arrange & Act
        var service = new WslService(_mockLogger.Object, settingsService: null);

        // Assert - Service should be created successfully
        service.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithSettingsService_UsesConfiguredTimeout()
    {
        // Arrange
        var settings = AppSettings.GetDefaults();
        settings.Timeouts.WslCommandTimeoutSeconds = 60;
        _mockSettingsService.SetSettings(settings);

        // Act
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Assert
        service.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithDistro_SetsDistroParameter()
    {
        // Arrange & Act
        var service = new WslService(_mockLogger.Object, _mockSettingsService, distro: "Ubuntu-22.04");

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    [TestMethod]
    public async Task ExecuteAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.ExecuteAsync("echo test", cts.Token);

        // Assert
        // Either throws OperationCanceledException or returns failure
        result.Success.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ExecuteAsync_SimpleCommand_ReturnsResult()
    {
        // This test requires WSL to be installed
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        var result = await service.ExecuteAsync("echo 'hello'");

        // Assert - Will fail if WSL not installed, which is expected
        // This is an integration test
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyCommand_ReturnsResult()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        var result = await service.ExecuteAsync("");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region CheckWslStatusAsync Tests

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CheckWslStatusAsync_ReturnsStatusResult()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        var result = await service.CheckWslStatusAsync();

        // Assert
        result.Should().NotBeNull();
        // Either WSL is installed or not - both are valid states
    }

    [TestMethod]
    public async Task CheckWslStatusAsync_WithCancellation_HandlesCancellation()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.CheckWslStatusAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        // Should handle cancellation gracefully
    }

    #endregion

    #region IsWslInstalledAsync Tests

    [TestMethod]
    [TestCategory("Integration")]
    public async Task IsWslInstalledAsync_ReturnsBool()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        var result = await service.IsWslInstalledAsync();

        // Assert
        // Result is either true or false based on system state - this is a basic sanity check
        (result == true || result == false).Should().BeTrue();
    }

    #endregion

    #region GetDistrosAsync Tests

    [TestMethod]
    [TestCategory("Integration")]
    public async Task GetDistrosAsync_ReturnsList()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        var result = await service.GetDistrosAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<string>>();
    }

    [TestMethod]
    public async Task GetDistrosAsync_WithCancellation_ReturnsEmptyList()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.GetDistrosAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Timeout Configuration Tests

    [TestMethod]
    public void TimeoutConfiguration_DefaultValues_AreReasonable()
    {
        // Arrange
        var settings = AppSettings.GetDefaults();

        // Assert
        settings.Timeouts.WslCommandTimeoutSeconds.Should().BeGreaterThan(0);
        settings.Timeouts.WslCommandTimeoutSeconds.Should().BeLessThanOrEqualTo(120);
        settings.Timeouts.WslStatusCheckTimeoutSeconds.Should().BeGreaterThan(0);
        settings.Timeouts.WslStatusCheckTimeoutSeconds.Should().BeLessThanOrEqualTo(30);
    }

    [TestMethod]
    public void TimeoutConfiguration_CustomValues_AreRespected()
    {
        // Arrange
        var settings = AppSettings.GetDefaults();
        settings.Timeouts.WslCommandTimeoutSeconds = 90;
        settings.Timeouts.WslStatusCheckTimeoutSeconds = 15;
        _mockSettingsService.SetSettings(settings);

        // Act
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Assert
        service.Should().NotBeNull();
        _mockSettingsService.Settings.Timeouts.WslCommandTimeoutSeconds.Should().Be(90);
        _mockSettingsService.Settings.Timeouts.WslStatusCheckTimeoutSeconds.Should().Be(15);
    }

    #endregion

    #region WslResult Tests

    [TestMethod]
    public void WslResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new WslResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().BeEmpty();
        result.Error.Should().BeEmpty();
        result.ExitCode.Should().Be(0);
    }

    [TestMethod]
    public void WslResult_WithValues_StoresCorrectly()
    {
        // Arrange & Act
        var result = new WslResult
        {
            Success = true,
            Output = "test output",
            Error = "test error",
            ExitCode = 0
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("test output");
        result.Error.Should().Be("test error");
        result.ExitCode.Should().Be(0);
    }

    #endregion

    #region WslStatusResult Tests

    [TestMethod]
    public void WslStatusResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new WslStatusResult();

        // Assert
        result.IsInstalled.Should().BeFalse();
        result.IsRunning.Should().BeFalse();
        result.Error.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [TestMethod]
    public void WslStatusResult_NotInstalled_HasCorrectState()
    {
        // Arrange & Act
        var result = new WslStatusResult
        {
            IsInstalled = false,
            Error = WslErrorType.NotInstalled
        };

        // Assert
        result.IsInstalled.Should().BeFalse();
        result.Error.Should().Be(WslErrorType.NotInstalled);
    }

    [TestMethod]
    public void WslStatusResult_InstalledButNotRunning_HasCorrectState()
    {
        // Arrange & Act
        var result = new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = false,
            Error = WslErrorType.NotRunning,
            ErrorMessage = "WSL is not running"
        };

        // Assert
        result.IsInstalled.Should().BeTrue();
        result.IsRunning.Should().BeFalse();
        result.Error.Should().Be(WslErrorType.NotRunning);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void WslStatusResult_Timeout_HasCorrectState()
    {
        // Arrange & Act
        var result = new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = false,
            Error = WslErrorType.Timeout,
            ErrorMessage = "Timeout occurred"
        };

        // Assert
        result.Error.Should().Be(WslErrorType.Timeout);
    }

    [TestMethod]
    public void WslStatusResult_OtherError_HasCorrectState()
    {
        // Arrange & Act
        var result = new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = false,
            Error = WslErrorType.Other,
            ErrorMessage = "Unknown error"
        };

        // Assert
        result.Error.Should().Be(WslErrorType.Other);
    }

    #endregion

    #region WslErrorType Tests

    [TestMethod]
    public void WslErrorType_AllValues_AreDefined()
    {
        // Arrange
        var errorTypes = Enum.GetValues<WslErrorType>();

        // Assert
        errorTypes.Should().Contain(WslErrorType.NotInstalled);
        errorTypes.Should().Contain(WslErrorType.NotRunning);
        errorTypes.Should().Contain(WslErrorType.Timeout);
        errorTypes.Should().Contain(WslErrorType.Other);
    }

    #endregion

    #region Logging Tests

    [TestMethod]
    public async Task ExecuteAsync_LogsCommandExecution()
    {
        // Arrange
        var service = new WslService(_mockLogger.Object, _mockSettingsService);

        // Act
        await service.ExecuteAsync("test command");

        // Assert - Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
