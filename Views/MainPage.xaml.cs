using CodexBarWin.Controls;
using CodexBarWin.Models;
using CodexBarWin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CodexBarWin.Views;

/// <summary>
/// Main page displaying provider usage data.
/// </summary>
public sealed partial class MainPage : Page
{
    public event EventHandler? SettingsRequested;
    public event EventHandler<UsageData>? ProviderDetailRequested;

    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateUI();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void UpdateUI()
    {
        // Update error panel
        if (!string.IsNullOrEmpty(ViewModel.ErrorMessage) && ViewModel.Providers.Count == 0)
        {
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorMessageText.Text = ViewModel.ErrorMessage;
        }
        else
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        // Update last updated text
        LastUpdatedText.Text = ViewModel.LastUpdatedText;
    }

    private void OnProviderCardLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ProviderCard card)
        {
            // Remove existing handler to avoid duplicates, then add
            card.DetailRequested -= OnProviderDetailRequested;
            card.DetailRequested += OnProviderDetailRequested;
        }
    }

    private void OnProviderDetailRequested(object? sender, UsageData data)
    {
        ProviderDetailRequested?.Invoke(this, data);
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            var logger = App.Services.GetRequiredService<ILogger<MainPage>>();
            logger.LogError(ex, "OnRefreshClicked failed");
        }
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
