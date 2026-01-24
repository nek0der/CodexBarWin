using CodexBarWin.Services;

namespace CodexBarWin.Tests.Unit.Mocks;

/// <summary>
/// Mock implementation of ISampleDataLoader for testing.
/// </summary>
public class MockSampleDataLoader : ISampleDataLoader
{
    private readonly Dictionary<string, string?> _sampleData = new();

    /// <summary>
    /// Preloads sample data for a provider.
    /// </summary>
    public void SetSampleData(string provider, string? json)
    {
        _sampleData[provider.ToLowerInvariant()] = json;
    }

    /// <inheritdoc />
    public string? LoadSampleJson(string provider)
    {
        var normalizedProvider = provider.ToLowerInvariant();
        return _sampleData.TryGetValue(normalizedProvider, out var json) ? json : null;
    }

    /// <summary>
    /// Clears all preloaded sample data.
    /// </summary>
    public void Clear()
    {
        _sampleData.Clear();
    }
}
