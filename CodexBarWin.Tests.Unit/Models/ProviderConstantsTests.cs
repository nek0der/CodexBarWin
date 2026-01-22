using CodexBarWin.Models;
using FluentAssertions;

namespace CodexBarWin.Tests.Unit.Models;

[TestClass]
public class ProviderConstantsTests
{
    [TestMethod]
    public void AllowedProviders_ContainsExpectedProviders()
    {
        // Assert
        ProviderConstants.AllowedProviders.Should().Contain("claude");
        ProviderConstants.AllowedProviders.Should().Contain("codex");
        ProviderConstants.AllowedProviders.Should().Contain("gemini");
        ProviderConstants.AllowedProviders.Should().HaveCount(3);
    }

    [TestMethod]
    [DataRow("claude", true)]
    [DataRow("codex", true)]
    [DataRow("gemini", true)]
    [DataRow("CLAUDE", true)]
    [DataRow("Claude", true)]
    [DataRow("invalid", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    [DataRow("  ", false)]
    public void IsValidProvider_ReturnsExpectedResult(string? providerId, bool expected)
    {
        // Act
        var result = ProviderConstants.IsValidProvider(providerId!);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("claude", "claude")]
    [DataRow("CLAUDE", "claude")]
    [DataRow("Claude", "claude")]
    [DataRow("  claude  ", "claude")]
    [DataRow("codex", "codex")]
    [DataRow("gemini", "gemini")]
    public void ValidateAndNormalize_ValidProvider_ReturnsNormalized(string input, string expected)
    {
        // Act
        var result = ProviderConstants.ValidateAndNormalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public void ValidateAndNormalize_NullOrEmpty_ThrowsArgumentException(string? input)
    {
        // Act
        var action = () => ProviderConstants.ValidateAndNormalize(input!);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [TestMethod]
    [DataRow("invalid")]
    [DataRow("unknown")]
    [DataRow("openai")]
    public void ValidateAndNormalize_InvalidProvider_ThrowsArgumentException(string input)
    {
        // Act
        var action = () => ProviderConstants.ValidateAndNormalize(input);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage($"*Invalid provider*{input}*");
    }

    [TestMethod]
    [DataRow("claude", "oauth")]
    [DataRow("codex", "cli")]
    [DataRow("gemini", "cli")]
    public void GetSource_ValidProvider_ReturnsCorrectSource(string providerId, string expectedSource)
    {
        // Act
        var result = ProviderConstants.GetSource(providerId);

        // Assert
        result.Should().Be(expectedSource);
    }

    [TestMethod]
    public void GetSource_InvalidProvider_ThrowsArgumentException()
    {
        // Act
        var action = () => ProviderConstants.GetSource("invalid");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void GetSource_CaseInsensitive()
    {
        // Act & Assert
        ProviderConstants.GetSource("CLAUDE").Should().Be("oauth");
        ProviderConstants.GetSource("Claude").Should().Be("oauth");
        ProviderConstants.GetSource("claude").Should().Be("oauth");
    }
}
