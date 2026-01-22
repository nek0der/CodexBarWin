using CodexBarWin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodexBarWin.Views;

/// <summary>
/// Page for guiding users through the setup process.
/// </summary>
public sealed partial class SetupPage : Page
{
    private SetupViewModel? _viewModel;

    public SetupViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = value;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateUI();
            }
        }
    }

    public event EventHandler? SetupComplete;

    public SetupPage()
    {
        InitializeComponent();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void UpdateUI()
    {
        if (ViewModel == null)
        {
            return;
        }

        HeaderIcon.Text = ViewModel.HeaderIcon;
        HeaderText.Text = ViewModel.HeaderText;
        WslStatusIcon.Text = ViewModel.WslStatusIcon;
        WslStatusText.Text = ViewModel.WslStatusText;
        CodexBarStatusIcon.Text = ViewModel.CodexBarStatusIcon;
        CodexBarStatusText.Text = ViewModel.CodexBarStatusText;
        InstructionText.Text = ViewModel.InstructionText;

        CommandsItemsControl.ItemsSource = ViewModel.Commands;

        StartButton.Visibility = ViewModel.IsReady ? Visibility.Visible : Visibility.Collapsed;
        LoadingOverlay.Visibility = ViewModel.IsChecking ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnRecheckClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel != null)
            {
                await ViewModel.RecheckCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            var logger = App.Services.GetRequiredService<ILogger<SetupPage>>();
            logger.LogError(ex, "OnRecheckClicked failed");
        }
    }

    private async void OnOpenGuideClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel != null)
            {
                await ViewModel.OpenGuideCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            var logger = App.Services.GetRequiredService<ILogger<SetupPage>>();
            logger.LogError(ex, "OnOpenGuideClicked failed");
        }
    }

    private void OnStartClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.StartCommand.Execute(null);
        SetupComplete?.Invoke(this, EventArgs.Empty);
    }
}
