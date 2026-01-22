using CodexBarWin.Models;
using CodexBarWin.Services;

namespace CodexBarWin.Tests.Unit.Mocks;

/// <summary>
/// Mock implementation of ICacheService for testing.
/// </summary>
public class MockCacheService : ICacheService
{
    private readonly Dictionary<string, UsageData> _cache = new();

    public int GetCallCount { get; private set; }
    public int SetCallCount { get; private set; }
    public int ClearCallCount { get; private set; }

    public UsageData? Get(string provider)
    {
        GetCallCount++;
        return _cache.TryGetValue(provider, out var data) ? data : null;
    }

    public void Set(string provider, UsageData data)
    {
        SetCallCount++;
        _cache[provider] = data;
    }

    public void Clear()
    {
        ClearCallCount++;
        _cache.Clear();
    }

    public IReadOnlyDictionary<string, UsageData> GetAll()
    {
        return _cache;
    }

    public Task SaveAsync()
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    // Test helpers
    public void PreloadCache(string provider, UsageData data)
    {
        _cache[provider] = data;
    }
}
