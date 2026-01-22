using CodexBarWin.Models;

namespace CodexBarWin.Services;

/// <summary>
/// Interface for CodexBar CLI service.
/// </summary>
public interface ICodexBarService
{
    /// <summary>
    /// Gets usage data for a specific provider.
    /// </summary>
    Task<UsageData?> GetUsageAsync(string provider, CancellationToken ct = default);

    /// <summary>
    /// Gets usage data for all enabled providers.
    /// </summary>
    Task<IReadOnlyList<UsageData>> GetAllUsageAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets usage data for all enabled providers as a stream (completed items yielded as they finish).
    /// </summary>
    IAsyncEnumerable<UsageData> GetAllUsageStreamAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the version of codexbar CLI.
    /// </summary>
    Task<string?> GetVersionAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if codexbar CLI is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
