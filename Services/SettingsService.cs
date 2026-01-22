using System.Text.Json;
using CodexBarWin.Models;
using Microsoft.Extensions.Logging;

namespace CodexBarWin.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private AppSettings _settings = AppSettings.GetDefaults();

    public AppSettings Settings => _settings;
    public string SettingsFilePath { get; }

    public event EventHandler? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "CodexBarWin");
        Directory.CreateDirectory(appFolder);
        SettingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                _logger.LogInformation("Settings file not found, using defaults");
                return;
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings != null)
            {
                // Filter out any invalid providers for security
                var originalCount = settings.Providers.Count;
                settings.Providers = settings.Providers
                    .Where(p => ProviderConstants.IsValidProvider(p.Id))
                    .ToList();

                if (settings.Providers.Count < originalCount)
                {
                    _logger.LogWarning("Filtered {Count} invalid provider(s) from settings",
                        originalCount - settings.Providers.Count);
                }

                _settings = settings;
                _logger.LogInformation("Settings loaded from {Path}", SettingsFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
            _logger.LogInformation("Settings saved to {Path}", SettingsFilePath);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    public void Reset()
    {
        _settings = AppSettings.GetDefaults();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Settings reset to defaults");
    }
}
