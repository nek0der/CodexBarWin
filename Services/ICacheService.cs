using CodexBarWin.Models;

namespace CodexBarWin.Services;

/// <summary>
/// Interface for caching usage data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets cached usage data for a provider.
    /// </summary>
    UsageData? Get(string provider);

    /// <summary>
    /// Sets cached usage data for a provider.
    /// </summary>
    void Set(string provider, UsageData data);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all cached usage data.
    /// </summary>
    IReadOnlyDictionary<string, UsageData> GetAll();

    /// <summary>
    /// Saves cache to file.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Loads cache from file.
    /// </summary>
    Task LoadAsync();
}
