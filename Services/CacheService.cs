using System.Collections.Concurrent;
using CodexBarWin.Models;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.Services;

/// <summary>
/// Service for caching usage data in memory and on disk.
/// </summary>
public class CacheService : ICacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly ISettingsService? _settingsService;
    private readonly string _cachePath;
    private readonly ConcurrentDictionary<string, UsageData> _cache = new();

    private TimeSpan Expiry => _settingsService != null
        ? TimeSpan.FromMinutes(_settingsService.Settings.CacheExpiryMinutes)
        : TimeSpan.FromMinutes(5);

    public CacheService(ILogger<CacheService> logger, ISettingsService? settingsService = null)
    {
        _logger = logger;
        _settingsService = settingsService;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "CodexBarWin");
        Directory.CreateDirectory(appFolder);
        _cachePath = Path.Combine(appFolder, "cache.json");
    }

    public UsageData? Get(string provider)
    {
        if (_cache.TryGetValue(provider, out var data))
        {
            if (DateTime.UtcNow - data.FetchedAt < Expiry)
            {
                return data;
            }

            _logger.LogDebug("Cache expired for {Provider}", provider);
        }

        return null;
    }

    public void Set(string provider, UsageData data)
    {
        _cache[provider] = data;
        _logger.LogDebug("Cached data for {Provider}", provider);
    }

    public void Clear()
    {
        _cache.Clear();
        _logger.LogDebug("Cache cleared");
    }

    public IReadOnlyDictionary<string, UsageData> GetAll()
    {
        return _cache
            .Where(kv => DateTime.UtcNow - kv.Value.FetchedAt < Expiry)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public async Task SaveAsync()
    {
        try
        {
            var cacheData = new CacheData
            {
                Timestamp = DateTime.UtcNow,
                Items = _cache.ToDictionary(kv => kv.Key, kv => kv.Value)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(cacheData, AppJsonSerializerContext.Default.CacheData);
            await File.WriteAllTextAsync(_cachePath, json);
            _logger.LogDebug("Cache saved to {Path}", _cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cache");
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_cachePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(_cachePath);
            var cacheData = System.Text.Json.JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.CacheData);

            if (cacheData == null)
            {
                return;
            }

            if (DateTime.UtcNow - cacheData.Timestamp > Expiry)
            {
                _logger.LogDebug("Cache file expired, ignoring");
                return;
            }

            foreach (var (provider, data) in cacheData.Items)
            {
                _cache[provider] = data;
            }

            _logger.LogDebug("Cache loaded from {Path}", _cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cache");
        }
    }
}
