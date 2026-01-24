namespace CodexBarWin.Services;

/// <summary>
/// Service for loading sample data files for development mode.
/// </summary>
public interface ISampleDataLoader
{
    /// <summary>
    /// Loads sample JSON data for the specified provider.
    /// </summary>
    /// <param name="provider">The provider name (e.g., "claude", "gemini", "codex").</param>
    /// <returns>Sample JSON string, or null if not found.</returns>
    string? LoadSampleJson(string provider);
}
