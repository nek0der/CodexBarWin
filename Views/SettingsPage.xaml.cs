using CodexBarWin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodexBarWin.Views;

/// <summary>
/// Page for application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private SettingsViewModel? _viewModel;

    public SettingsViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;

            if (_viewModel != null)
            {
                LoadSettings();
            }
        }
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public SettingsPage()
    {
        InitializeComponent();
    }

    private void LoadSettings()
    {
        if (ViewModel == null)
        {
            return;
        }

        ViewModel.Load();

        ProvidersItemsControl.ItemsSource = ViewModel.Providers;

        // Set theme
        ThemeComboBox.SelectedIndex = ViewModel.SelectedThemeIndex;

        // Set refresh interval
        foreach (ComboBoxItem item in RefreshIntervalComboBox.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out var seconds) && seconds == ViewModel.RefreshInterval)
            {
                RefreshIntervalComboBox.SelectedItem = item;
                break;
            }
        }

        StartWithWindowsToggle.IsOn = ViewModel.StartWithWindows;
        StartMinimizedToggle.IsOn = ViewModel.StartMinimized;
    }

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
            {
                return;
            }

            // Update ViewModel from UI
            ViewModel.SelectedThemeIndex = ThemeComboBox.SelectedIndex;

            if (RefreshIntervalComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out var seconds))
            {
                ViewModel.RefreshInterval = seconds;
            }

            ViewModel.StartWithWindows = StartWithWindowsToggle.IsOn;
            ViewModel.StartMinimized = StartMinimizedToggle.IsOn;

            await ViewModel.SaveCommand.ExecuteAsync(null);

            // Apply theme change
            App.ApplyTheme();

            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            var logger = App.Services.GetRequiredService<ILogger<SettingsPage>>();
            logger.LogError(ex, "OnSaveClicked failed");
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.CancelCommand.Execute(null);
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMoveUpClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProviderConfigViewModel provider)
        {
            ViewModel?.MoveUpCommand.Execute(provider);
        }
    }

    private void OnMoveDownClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProviderConfigViewModel provider)
        {
            ViewModel?.MoveDownCommand.Execute(provider);
        }
    }
}
