namespace CodexBarWin.Models;

/// <summary>
/// Configuration for a provider (user settings only).
/// </summary>
public record ProviderConfig
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? IconPath { get; init; }
    public int Order { get; set; }

    /// <summary>
    /// Gets the default providers for MVP.
    /// </summary>
    public static IReadOnlyList<ProviderConfig> GetDefaults() =>
    [
        new ProviderConfig { Id = "claude", DisplayName = "Claude", IsEnabled = true, Order = 0 },
        new ProviderConfig { Id = "codex", DisplayName = "Codex", IsEnabled = true, Order = 1 },
        new ProviderConfig { Id = "gemini", DisplayName = "Gemini", IsEnabled = true, Order = 2 }
    ];
}

/// <summary>
/// Application constants for providers (code-managed, not user settings).
/// </summary>
public static class ProviderConstants
{
    /// <summary>
    /// Set of allowed provider IDs for security validation.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedProviders =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "claude", "codex", "gemini" };

    /// <summary>
    /// Checks if a provider ID is valid (exists in the allowed list).
    /// </summary>
    public static bool IsValidProvider(string providerId)
        => !string.IsNullOrWhiteSpace(providerId) && AllowedProviders.Contains(providerId);

    /// <summary>
    /// Validates and normalizes a provider ID.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the provider ID is invalid.</exception>
    public static string ValidateAndNormalize(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

        var normalized = providerId.Trim().ToLowerInvariant();
        if (!AllowedProviders.Contains(normalized))
            throw new ArgumentException($"Invalid provider: '{providerId}'", nameof(providerId));

        return normalized;
    }

    /// <summary>
    /// Gets the source type for fetching usage data.
    /// This is determined by codexbar CLI requirements and should not be user-configurable.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the provider ID is invalid.</exception>
    public static string GetSource(string providerId)
    {
        var normalized = ValidateAndNormalize(providerId);
        return normalized switch
        {
            "claude" => "oauth",
            "codex" => "cli",
            "gemini" => "cli",
            _ => throw new InvalidOperationException($"Unhandled provider: {normalized}")
        };
    }
}
