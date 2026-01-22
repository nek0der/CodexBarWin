using CodexBarWin.Models;
using CodexBarWin.Services;

namespace CodexBarWin.Tests.Unit.Mocks;

/// <summary>
/// Mock implementation of ICodexBarService for testing.
/// </summary>
public class MockCodexBarService : ICodexBarService
{
    private readonly Dictionary<string, UsageData?> _usageData = new();
    private string? _version = "1.0.0";
    private bool _isAvailable = true;

    public int GetUsageCallCount { get; private set; }
    public int GetAllUsageCallCount { get; private set; }
    public int GetVersionCallCount { get; private set; }
    public int IsAvailableCallCount { get; private set; }

    public void SetUsageData(string provider, UsageData? data)
    {
        _usageData[provider] = data;
    }

    public void SetVersion(string? version)
    {
        _version = version;
    }

    public void SetAvailable(bool isAvailable)
    {
        _isAvailable = isAvailable;
    }

    public Task<UsageData?> GetUsageAsync(string provider, CancellationToken ct = default)
    {
        GetUsageCallCount++;
        return Task.FromResult(_usageData.TryGetValue(provider, out var data) ? data : null);
    }

    public Task<IReadOnlyList<UsageData>> GetAllUsageAsync(CancellationToken ct = default)
    {
        GetAllUsageCallCount++;
        var results = _usageData.Values.Where(d => d != null).Cast<UsageData>().ToList();
        return Task.FromResult<IReadOnlyList<UsageData>>(results);
    }

    public async IAsyncEnumerable<UsageData> GetAllUsageStreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var data in _usageData.Values.Where(d => d != null))
        {
            yield return data!;
            await Task.Yield();
        }
    }

    public Task<string?> GetVersionAsync(CancellationToken ct = default)
    {
        GetVersionCallCount++;
        return Task.FromResult(_version);
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        IsAvailableCallCount++;
        return Task.FromResult(_isAvailable);
    }
}
