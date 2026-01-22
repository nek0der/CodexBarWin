using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodexBarWin.Helpers;
using CodexBarWin.Models;
using CodexBarWin.Services;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ICodexBarService _codexBarService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MainViewModel> _logger;
    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _timerCts;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<UsageData> _providers = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private DateTime _lastUpdated;

    [ObservableProperty]
    private string _lastUpdatedText = "Never updated";

    public MainViewModel(
        ICodexBarService codexBarService,
        ISettingsService settingsService,
        ILogger<MainViewModel> logger)
    {
        _codexBarService = codexBarService;
        _settingsService = settingsService;
        _logger = logger;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    /// <summary>
    /// Starts the periodic refresh.
    /// </summary>
    public async Task StartAsync()
    {
        // Dispose existing CTSs if any (prevents leaks on restart)
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _refreshTimer?.Dispose();

        _refreshCts = new CancellationTokenSource();
        _timerCts = new CancellationTokenSource();

        // Create timer with current interval from settings
        var interval = TimeSpan.FromSeconds(_settingsService.Settings.RefreshIntervalSeconds);
        _refreshTimer = new PeriodicTimer(interval);

        // Initial refresh
        await RefreshAsync();

        // Start periodic refresh
        RunPeriodicRefreshAsync(_timerCts.Token).SafeFireAndForget(
            onError: ex => _logger.LogError(ex, "Periodic refresh task failed"));
    }

    /// <summary>
    /// Stops the periodic refresh.
    /// </summary>
    public void Stop()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        // Check if this is the first load (no existing data)
        bool isFirstLoad = Providers.Count == 0;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Refreshing usage data...");

            // Get enabled providers from settings (sorted by Order)
            var enabledProviders = _settingsService.Settings.Providers
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Order)
                .ToList();

            if (enabledProviders.Count == 0)
            {
                Providers.Clear();
                ErrorMessage = "No providers enabled. Go to Settings to enable providers.";
                return;
            }

            if (isFirstLoad)
            {
                // First load: add placeholder items
                foreach (var config in enabledProviders)
                {
                    Providers.Add(new UsageData
                    {
                        Provider = config.Id,
                        IsLoading = true,
                        FetchedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                // Refresh: set loading state on existing items
                for (int i = 0; i < Providers.Count; i++)
                {
                    Providers[i] = Providers[i] with { IsLoading = true };
                }
            }

            // Stream results and update as they complete
            await foreach (var data in _codexBarService.GetAllUsageStreamAsync())
            {
                // Find the index of this provider
                var index = -1;
                for (int i = 0; i < Providers.Count; i++)
                {
                    if (Providers[i].Provider.Equals(data.Provider, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    Providers[index] = data;
                }
                else if (isFirstLoad)
                {
                    // Provider not in list yet (shouldn't happen normally)
                    Providers.Add(data);
                }
            }

            LastUpdated = DateTime.Now;
            UpdateLastUpdatedText();

            _logger.LogInformation("Usage data refreshed, {Count} providers", Providers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh usage data");

            // Only show error state on first load, otherwise keep existing data
            if (isFirstLoad)
            {
                var enabledProviders = _settingsService.Settings.Providers.Where(p => p.IsEnabled);
                Providers.Clear();
                foreach (var config in enabledProviders)
                {
                    Providers.Add(new UsageData
                    {
                        Provider = config.Id,
                        Error = "Connection error. Check WSL status.",
                        FetchedAt = DateTime.UtcNow
                    });
                }
            }
            ErrorMessage = "Failed to refresh data. Check connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        _logger.LogInformation("Exit requested");
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private async Task RunPeriodicRefreshAsync(CancellationToken ct)
    {
        if (_refreshTimer == null)
        {
            return;
        }

        try
        {
            while (await _refreshTimer.WaitForNextTickAsync(ct))
            {
                await RefreshAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Periodic refresh stopped");
        }
    }

    private void UpdateLastUpdatedText()
    {
        var elapsed = DateTime.Now - LastUpdated;

        LastUpdatedText = elapsed.TotalSeconds switch
        {
            < 5 => "Updated just now",
            < 60 => $"Updated {elapsed.Seconds} sec ago",
            < 3600 => $"Updated {elapsed.Minutes} min ago",
            _ => $"Updated {elapsed.Hours} hours ago"
        };
    }

    private async void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogDebug("Settings changed, recreating timer and refreshing data");
            RecreateTimer();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle settings change");
        }
    }

    /// <summary>
    /// Recreates the periodic timer with the current refresh interval from settings.
    /// </summary>
    private void RecreateTimer()
    {
        var interval = TimeSpan.FromSeconds(_settingsService.Settings.RefreshIntervalSeconds);

        // Stop and dispose old timer and CTS
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _refreshTimer?.Dispose();

        // Create new timer
        _refreshTimer = new PeriodicTimer(interval);
        _timerCts = new CancellationTokenSource();

        // Restart periodic refresh
        RunPeriodicRefreshAsync(_timerCts.Token).SafeFireAndForget(
            onError: ex => _logger.LogError(ex, "Periodic refresh task failed"));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Stop();
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _refreshTimer?.Dispose();
            _settingsService.SettingsChanged -= OnSettingsChanged;
        }

        _disposed = true;
    }
}
