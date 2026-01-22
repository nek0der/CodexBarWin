using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ILogger<CodexBarService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public CodexBarService(
        IWslService wslService,
        ICacheService cacheService,
        ISettingsService settingsService,
        ILogger<CodexBarService> logger)
    {
        _wslService = wslService;
        _cacheService = cacheService;
        _settingsService = settingsService;
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

        _isFirstFetch = false;

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

        _isFirstFetch = false;
    }

    private bool _isFirstFetch = true;

    private async Task<UsageData> FetchProviderAsync(string provider, string source, CancellationToken ct)
    {
        try
        {
            // Validate provider (defense in depth)
            var normalizedProvider = ProviderConstants.ValidateAndNormalize(provider);

            // Use timeout settings from configuration
            // CLI-based providers (codex, gemini) are slower, especially on first fetch
            var timeoutSettings = _settingsService.Settings.Timeouts;
            var timeout = source == "cli"
                ? (_isFirstFetch
                    ? TimeSpan.FromSeconds(timeoutSettings.CliProviderFirstFetchTimeoutSeconds)
                    : TimeSpan.FromSeconds(timeoutSettings.CliProviderTimeoutSeconds))
                : (_isFirstFetch
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

            var dto = JsonSerializer.Deserialize<UsageDataDto>(json, JsonOptions);
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
                var dtos = JsonSerializer.Deserialize<List<UsageDataDto>>(json, JsonOptions);
                return dtos?.Select(d => d.ToUsageData()).ToList() ?? [];
            }

            // Try single object
            var dto = JsonSerializer.Deserialize<UsageDataDto>(json, JsonOptions);
            return dto != null ? [dto.ToUsageData()] : [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse usage JSON array");
            return [];
        }
    }

    /// <summary>
    /// DTO for deserializing codexbar JSON output.
    /// </summary>
    private record UsageDataDto
    {
        public string Provider { get; init; } = string.Empty;
        public string? Source { get; init; }
        public UsageDto? Usage { get; init; }
        public string? Error { get; init; }

        public UsageData ToUsageData() => new()
        {
            Provider = Provider,
            Plan = Usage?.LoginMethod,
            Session = Usage?.Primary?.ToUsageWindow(),
            Weekly = Usage?.Secondary?.ToUsageWindow(),
            Tertiary = Usage?.Tertiary?.ToUsageWindow(),
            Status = Source,
            Error = Error,
            FetchedAt = DateTime.UtcNow
        };
    }

    private record UsageDto
    {
        public string? LoginMethod { get; init; }
        public UsageWindowDto? Primary { get; init; }
        public UsageWindowDto? Secondary { get; init; }
        public UsageWindowDto? Tertiary { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }

    private record UsageWindowDto
    {
        public double UsedPercent { get; init; }
        public int WindowMinutes { get; init; }
        public DateTime? ResetsAt { get; init; }
        public string? ResetDescription { get; init; }

        public UsageWindow ToUsageWindow() => new()
        {
            Used = (int)UsedPercent,
            Limit = 100,
            ResetAt = ResetsAt,
            ResetIn = ResetDescription
        };
    }
}
