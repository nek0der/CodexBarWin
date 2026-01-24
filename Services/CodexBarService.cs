using System.Text.Json;
using CodexBarWin.Models;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.Services;

/// <summary>
/// Service for interacting with codexbar CLI via WSL.
/// </summary>
public class CodexBarService : ICodexBarService
{
    private readonly IWslService _wslService;
    private readonly ICacheService _cacheService;
    private readonly ISettingsService _settingsService;
    private readonly ISampleDataLoader _sampleDataLoader;
    private readonly ILogger<CodexBarService> _logger;


    public CodexBarService(
        IWslService wslService,
        ICacheService cacheService,
        ISettingsService settingsService,
        ISampleDataLoader sampleDataLoader,
        ILogger<CodexBarService> logger)
    {
        _wslService = wslService;
        _cacheService = cacheService;
        _settingsService = settingsService;
        _sampleDataLoader = sampleDataLoader;
        _logger = logger;
    }

    public async Task<UsageData?> GetUsageAsync(string provider, CancellationToken ct = default)
    {
        try
        {
            var normalizedProvider = ProviderConstants.ValidateAndNormalize(provider);
            var source = ProviderConstants.GetSource(normalizedProvider);
            var result = await _wslService.ExecuteAsync($"codexbar --provider {normalizedProvider} --format json --source {source}", ct);

            if (!result.Success)
            {
                _logger.LogWarning("codexbar command failed for {Provider}: {Error}", provider, result.Error);
                return _cacheService.Get(provider);
            }

            var data = ParseUsageJson(result.Output, provider);

            if (data != null)
            {
                _cacheService.Set(provider, data);
            }

            return data ?? _cacheService.Get(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage for {Provider}", provider);
            return _cacheService.Get(provider);
        }
    }

    public async Task<IReadOnlyList<UsageData>> GetAllUsageAsync(CancellationToken ct = default)
    {
        var enabledProviders = _settingsService.Settings.Providers
            .Where(p => p.IsEnabled && ProviderConstants.IsValidProvider(p.Id))
            .Select(p => p.Id)
            .ToList();

        if (enabledProviders.Count == 0)
        {
            return [];
        }

        // Fetch providers sequentially to avoid WSL conflicts
        var results = new List<UsageData>();
        foreach (var id in enabledProviders)
        {
            var source = ProviderConstants.GetSource(id);
            results.Add(await FetchProviderAsync(id, source, ct));
        }

        Interlocked.Exchange(ref _isFirstFetch, 0);

        // All results are non-null now (may contain error info)
        return results.ToList();
    }

    public async IAsyncEnumerable<UsageData> GetAllUsageStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var enabledProviders = _settingsService.Settings.Providers
            .Where(p => p.IsEnabled && ProviderConstants.IsValidProvider(p.Id))
            .Select(p => p.Id)
            .ToList();

        if (enabledProviders.Count == 0)
        {
            yield break;
        }

        // Start all fetches in parallel
        var tasks = enabledProviders
            .Select(id => FetchProviderAsync(id, ProviderConstants.GetSource(id), ct))
            .ToList();

        // Yield results as they complete
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            var result = await completedTask;
            yield return result;
        }

        Interlocked.Exchange(ref _isFirstFetch, 0);
    }

    // Use int for thread-safe atomic operations with Interlocked
    // 1 = first fetch, 0 = subsequent fetches
    private int _isFirstFetch = 1;

    private async Task<UsageData> FetchProviderAsync(string provider, string source, CancellationToken ct)
    {
        try
        {
            // Validate provider (defense in depth)
            var normalizedProvider = ProviderConstants.ValidateAndNormalize(provider);

            // Developer mode: Use sample data instead of WSL
            if (_settingsService.Settings.DeveloperModeEnabled)
            {
                _logger.LogDebug("Developer mode: Loading sample data for {Provider}", normalizedProvider);
                
                var sampleJson = _sampleDataLoader.LoadSampleJson(normalizedProvider);
                if (!string.IsNullOrWhiteSpace(sampleJson))
                {
                    var dataList = ParseUsageJsonArray(sampleJson);
                    if (dataList.Count > 0)
                    {
                        var data = dataList[0];
                        _cacheService.Set(data.Provider, data);
                        _logger.LogDebug("Loaded sample data for {Provider}", normalizedProvider);
                        return data;
                    }
                }

                // Fallback: Sample data not available
                _logger.LogWarning("Sample data not available for {Provider} (Developer mode)", normalizedProvider);
                return new UsageData
                {
                    Provider = provider,
                    Error = "Sample data not available (Developer mode)",
                    FetchedAt = DateTime.UtcNow
                };
            }

            // Use timeout settings from configuration
            // CLI-based providers (codex, gemini) are slower, especially on first fetch
            var timeoutSettings = _settingsService.Settings.Timeouts;
            // Read _isFirstFetch atomically for thread-safety
            var isFirstFetch = Interlocked.CompareExchange(ref _isFirstFetch, 1, 1) == 1;
            var timeout = source == "cli"
                ? (isFirstFetch
                    ? TimeSpan.FromSeconds(timeoutSettings.CliProviderFirstFetchTimeoutSeconds)
                    : TimeSpan.FromSeconds(timeoutSettings.CliProviderTimeoutSeconds))
                : (isFirstFetch
                    ? TimeSpan.FromSeconds(timeoutSettings.StandardProviderFirstFetchTimeoutSeconds)
                    : TimeSpan.FromSeconds(timeoutSettings.StandardProviderTimeoutSeconds));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            // Workaround: gemini crashes without --verbose (codexbar bug)
            var verboseFlag = normalizedProvider == "gemini" ? " --verbose" : "";
            var result = await _wslService.ExecuteAsync(
                $"codexbar usage --provider {normalizedProvider} --format json --source {source}{verboseFlag}", cts.Token);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                var dataList = ParseUsageJsonArray(result.Output);
                if (dataList.Count > 0)
                {
                    var data = dataList[0];
                    _cacheService.Set(data.Provider, data);
                    _logger.LogDebug("Fetched {Provider} successfully", provider);
                    return data;
                }

                // Empty response from codexbar
                _logger.LogDebug("{Provider} returned empty data", provider);
                return new UsageData
                {
                    Provider = provider,
                    Error = "No data available",
                    FetchedAt = DateTime.UtcNow
                };
            }

            // Command failed
            _logger.LogDebug("{Provider} failed: {Error}", provider, result.Error);
            return new UsageData
            {
                Provider = provider,
                Error = string.IsNullOrWhiteSpace(result.Error)
                    ? $"Command failed (exit code {result.ExitCode})"
                    : result.Error.Trim(),
                FetchedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("{Provider} timed out", provider);
            return new UsageData
            {
                Provider = provider,
                Error = "Request timed out",
                FetchedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Provider}", provider);
            return new UsageData
            {
                Provider = provider,
                Error = "Unexpected error occurred",
                FetchedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<string?> GetVersionAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _wslService.ExecuteAsync("codexbar --version", ct);
            return result.Success ? result.Output.Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get codexbar version");
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _wslService.ExecuteAsync("which codexbar", ct);
            return result.Success && !string.IsNullOrWhiteSpace(result.Output);
        }
        catch
        {
            return false;
        }
    }

    private UsageData? ParseUsageJson(string json, string provider)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var dto = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.UsageDataDto);
            return dto?.ToUsageData() ?? new UsageData { Provider = provider };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse usage JSON for {Provider}", provider);
            return null;
        }
    }

    private List<UsageData> ParseUsageJsonArray(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            // Try array first
            if (json.TrimStart().StartsWith('['))
            {
                var dtos = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListUsageDataDto);
                return dtos?.Select(d => d.ToUsageData()).ToList() ?? [];
            }

            // Try single object
            var dto = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.UsageDataDto);
            return dto != null ? [dto.ToUsageData()] : [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse usage JSON array");
            return [];
        }
    }
}
