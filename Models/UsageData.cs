namespace CodexBarWin.Models;

/// <summary>
/// Represents usage data for a provider.
/// </summary>
public record UsageData
{
    public string Provider { get; init; } = string.Empty;
    public string? Plan { get; init; }
    public UsageWindow? Session { get; init; }
    public UsageWindow? Weekly { get; init; }
    public UsageWindow? Tertiary { get; init; }
    public CreditsInfo? Credits { get; init; }
    public DateTime FetchedAt { get; init; } = DateTime.UtcNow;
    public bool IsStale => DateTime.UtcNow - FetchedAt > TimeSpan.FromMinutes(5);
    public string? Status { get; init; }
    public string? Error { get; init; }
    public bool IsLoading { get; init; }

    public bool HasWeekly => Weekly != null;
    public bool HasTertiary => Tertiary != null;
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>
    /// Gets the display label for the session usage (e.g., "Session" for Claude/Codex, "Pro" for Gemini).
    /// </summary>
    public string SessionLabel => Provider.ToLowerInvariant() switch
    {
        "gemini" => "Pro",
        _ => "Session"
    };

    /// <summary>
    /// Gets the display label for the weekly usage (e.g., "Weekly" for Claude/Codex, "Flash" for Gemini).
    /// </summary>
    public string WeeklyLabel => Provider.ToLowerInvariant() switch
    {
        "gemini" => "Flash",
        _ => "Weekly"
    };

    /// <summary>
    /// Gets the display label for the tertiary usage (e.g., "Sonnet Weekly" for Claude).
    /// </summary>
    public string TertiaryLabel => Provider.ToLowerInvariant() switch
    {
        "claude" => "Current week (Sonnet)",
        _ => "Additional"
    };
}

/// <summary>
/// Represents a usage window (session or weekly).
/// </summary>
public record UsageWindow
{
    public int Used { get; init; }
    public int Limit { get; init; }
    public double Percent => Limit > 0 ? (double)Used / Limit * 100 : 0;
    public string PercentText => $"{Percent:F0}%";
    public DateTime? ResetAt { get; init; }
    public string? ResetIn { get; init; }
    public TimeSpan? TimeUntilReset => ResetAt.HasValue ? ResetAt.Value - DateTime.UtcNow : null;
}

/// <summary>
/// Represents credit/cost information.
/// </summary>
public record CreditsInfo
{
    public decimal Used { get; init; }
    public decimal Limit { get; init; }
    public decimal Remaining => Limit - Used;
}
