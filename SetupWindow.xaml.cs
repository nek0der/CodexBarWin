using CodexBarWin.Helpers;
using CodexBarWin.ViewModels;
using CodexBarWin.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace CodexBarWin;

/// <summary>
/// Window for the setup process.
/// </summary>
public sealed partial class SetupWindow : Window
{
    private readonly SetupViewModel _viewModel;
    private readonly SetupPage _setupPage;
    private MicaController? _micaController;
    private SystemBackdropConfiguration? _backdropConfiguration;
    private AppWindow? _appWindow;

    public SetupWindow()
    {
        InitializeComponent();

        // Set up extended title bar with Mica
        SetupTitleBar();
        SetupMicaBackdrop();

        // Set window size
        _appWindow = AppWindow;
        _appWindow.Resize(new Windows.Graphics.SizeInt32(500, 600));

        // Apply theme
        App.ApplyTheme();

        // Get ViewModel from DI
        _viewModel = App.Services.GetRequiredService<SetupViewModel>();
        _viewModel.SetupComplete += OnSetupComplete;

        // Create and configure the setup page
        _setupPage = new SetupPage { ViewModel = _viewModel };
        _setupPage.SetupComplete += OnSetupComplete;

        // Navigate to setup page
        ContentFrame.Content = _setupPage;

        // Start the check
        var logger = App.Services.GetRequiredService<ILogger<SetupWindow>>();
        _viewModel.InitializeAsync().SafeFireAndForget(
            onError: ex => logger.LogError(ex, "SetupViewModel.InitializeAsync failed"));
    }

    private void OnSetupComplete(object? sender, EventArgs e)
    {
        App.Current.OnSetupComplete();
    }

    private void SetupTitleBar()
    {
        // Extend content into title bar
        ExtendsContentIntoTitleBar = true;

        // Set the custom title bar element
        SetTitleBar(AppTitleBar);

        // Get AppWindow and customize title bar
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow.TitleBar != null)
        {
            // Make title bar buttons transparent
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }

    private void SetupMicaBackdrop()
    {
        if (!MicaController.IsSupported()) return;

        _micaController = new MicaController
        {
            Kind = MicaKind.Base
        };

        // Configure backdrop with theme awareness
        _backdropConfiguration = new SystemBackdropConfiguration
        {
            IsInputActive = true
        };
        UpdateBackdropTheme();

        _micaController.SetSystemBackdropConfiguration(_backdropConfiguration);
        _micaController.AddSystemBackdropTarget(
            WinRT.CastExtensions.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>(this));

        // Listen for theme changes on the root element
        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += OnActualThemeChanged;
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateBackdropTheme();
    }

    private void UpdateBackdropTheme()
    {
        if (_backdropConfiguration == null) return;

        var theme = SystemBackdropTheme.Default;
        if (Content is FrameworkElement rootElement)
        {
            theme = rootElement.ActualTheme switch
            {
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                _ => SystemBackdropTheme.Default
            };
        }
        _backdropConfiguration.Theme = theme;
    }
}
