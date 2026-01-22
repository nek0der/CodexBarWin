using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.Tests.Unit.Mocks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodexBarWin.Tests.Unit.Services;

[TestClass]
public class SetupCheckerTests
{
    private MockWslService _mockWslService = null!;
    private MockCodexBarService _mockCodexBarService = null!;
    private Mock<ILogger<SetupChecker>> _mockLogger = null!;
    private SetupChecker _setupChecker = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockWslService = new MockWslService();
        _mockCodexBarService = new MockCodexBarService();
        _mockLogger = new Mock<ILogger<SetupChecker>>();

        _setupChecker = new SetupChecker(
            _mockWslService,
            _mockCodexBarService,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task CheckAsync_WslNotInstalled_ReturnsNotInstalled()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = false,
            Error = WslErrorType.NotInstalled
        });

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeFalse();
        result.WslError.Should().Be(WslErrorType.NotInstalled);
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_WslNotRunning_ReturnsNotRunning()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = false,
            Error = WslErrorType.NotRunning
        });

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeFalse();
        result.WslError.Should().Be(WslErrorType.NotRunning);
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_NoDistros_ReturnsEmptyDistros()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros([]);

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeTrue();
        result.Distros.Should().BeEmpty();
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_CodexBarNotInstalled_ReturnsNotInstalled()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(false);

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeTrue();
        result.Distros.Should().Contain("Ubuntu");
        result.CodexBarInstalled.Should().BeFalse();
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_CodexBarOldVersion_ReturnsNotCompatible()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("0.16.0"); // Older than minimum 0.17.0

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeTrue();
        result.CodexBarInstalled.Should().BeTrue();
        result.CodexBarVersion.Should().Be("0.16.0");
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_AllReady_ReturnsIsReady()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("0.17.0");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeTrue();
        result.Distros.Should().NotBeEmpty();
        result.CodexBarInstalled.Should().BeTrue();
        result.CodexBarVersion.Should().Be("0.17.0");
        result.IsReady.Should().BeTrue();
    }

    [TestMethod]
    public async Task CheckAsync_CodexBarNewerVersion_ReturnsIsReady()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("1.0.0");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.IsReady.Should().BeTrue();
    }

    [TestMethod]
    public async Task CheckAsync_UnknownVersion_TreatedAsCompatible()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("unknown");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.IsReady.Should().BeTrue();
    }

    [TestMethod]
    public async Task CheckAsync_MultipleDistros_ReturnsAll()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu", "Debian", "Alpine"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("0.17.0");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.Distros.Should().HaveCount(3);
        result.Distros.Should().Contain(["Ubuntu", "Debian", "Alpine"]);
    }

    [TestMethod]
    public async Task CheckAsync_WslTimeout_ReturnsTimeoutError()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = false,
            Error = WslErrorType.Timeout,
            ErrorMessage = "WSL status check timed out."
        });

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.WslInstalled.Should().BeTrue();
        result.WslRunning.Should().BeFalse();
        result.WslError.Should().Be(WslErrorType.Timeout);
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("0.17.0", true)]
    [DataRow("0.17.1", true)]
    [DataRow("0.18.0", true)]
    [DataRow("1.0.0", true)]
    [DataRow("0.16.9", false)]
    [DataRow("0.16.0", false)]
    [DataRow("0.1.0", false)]
    public async Task CheckAsync_VersionComparison_ReturnsExpected(string version, bool expectedReady)
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion(version);

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.IsReady.Should().Be(expectedReady);
    }

    [TestMethod]
    public async Task CheckAsync_VersionWithPrefix_ParsesCorrectly()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("codexbar 0.17.0");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.IsReady.Should().BeTrue();
    }

    [TestMethod]
    public async Task CheckAsync_NullVersion_ReturnsNotCompatible()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion(null);

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.CodexBarInstalled.Should().BeTrue();
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckAsync_EmptyVersion_ReturnsNotCompatible()
    {
        // Arrange
        _mockWslService.SetStatusResult(new WslStatusResult
        {
            IsInstalled = true,
            IsRunning = true
        });
        _mockWslService.SetDistros(["Ubuntu"]);
        _mockCodexBarService.SetAvailable(true);
        _mockCodexBarService.SetVersion("");

        // Act
        var result = await _setupChecker.CheckAsync();

        // Assert
        result.CodexBarInstalled.Should().BeTrue();
        result.IsReady.Should().BeFalse();
    }

    [TestMethod]
    public void MinCodexBarVersion_IsExpectedValue()
    {
        // Assert
        SetupChecker.MinCodexBarVersion.Should().Be(new Version("0.17.0"));
    }
}
