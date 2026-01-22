using CodexBarWin.Models;
using CodexBarWin.Services;

namespace CodexBarWin.Tests.Unit.Mocks;

/// <summary>
/// Mock implementation of ISettingsService for testing.
/// </summary>
public class MockSettingsService : ISettingsService
{
    private AppSettings _settings;

    public MockSettingsService()
    {
        _settings = AppSettings.GetDefaults();
    }

    public AppSettings Settings => _settings;

    public string SettingsFilePath => "test-settings.json";

    public event EventHandler? SettingsChanged;

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _settings = AppSettings.GetDefaults();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    // Test helpers
    public void SetSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public void SetProviders(List<ProviderConfig> providers)
    {
        _settings.Providers = providers;
    }

    public void RaiseSettingsChanged()
    {
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
