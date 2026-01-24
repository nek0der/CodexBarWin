using CodexBarWin.Helpers;
using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace CodexBarWin;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "CodexBarWin_SingleInstance";

    private MainWindow? _mainWindow;
    private Window? _setupWindow;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets all active windows.
    /// </summary>
    public static List<Window> Windows { get; } = [];

    /// <summary>
    /// Gets the current App instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single instance check
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            Exit();
            return;
        }

        // Load settings
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        // Load cache
        var cacheService = Services.GetRequiredService<ICacheService>();
        await cacheService.LoadAsync();

        // Check setup status
        var setupChecker = Services.GetRequiredService<ISetupChecker>();
        var status = await setupChecker.CheckAsync();

        if (status.IsReady)
        {
            // Normal startup - show main window
            StartMainWindow();
        }
        else
        {
            // Setup required - show setup window
            ShowSetupWindow();
        }
    }

    private void StartMainWindow()
    {
        var logger = Services.GetRequiredService<ILogger<App>>();
        try
        {
            _mainWindow = new MainWindow();
            Windows.Add(_mainWindow);
            _mainWindow.Closed += (s, e) =>
            {
                Windows.Remove(_mainWindow);
                ReleaseMutex();
            };
            _mainWindow.Activate();

            // Apply theme
            ApplyTheme();

            // Start the main view model
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            mainViewModel.StartAsync().SafeFireAndForget(
                onError: ex => logger.LogError(ex, "MainViewModel.StartAsync failed"));

            // Tray icon is now handled in MainWindow.xaml
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StartMainWindow failed");
            throw;
        }
    }

    private void ShowSetupWindow()
    {
        _setupWindow = new SetupWindow();
        Windows.Add(_setupWindow);
        _setupWindow.Closed += (s, e) => Windows.Remove(_setupWindow!);
        _setupWindow.Activate();

        // Apply theme
        ApplyTheme();
    }

    /// <summary>
    /// Applies the saved theme to all windows.
    /// </summary>
    public static void ApplyTheme()
    {
        var settingsService = Services.GetRequiredService<ISettingsService>();
        var theme = settingsService.Settings.Theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        foreach (var window in Windows)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }
    }

    /// <summary>
    /// Called when setup is complete to transition to main window.
    /// </summary>
    public void OnSetupComplete()
    {
        _setupWindow?.Close();
        _setupWindow = null;
        StartMainWindow();
    }

    /// <summary>
    /// Releases and disposes the single-instance mutex.
    /// </summary>
    private static void ReleaseMutex()
    {
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex was not owned by this thread (already released)
            }
            _mutex.Dispose();
            _mutex = null;
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
#if DEBUG
            builder.AddDebug();  // Output to VS Debug window
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
        });

        // Services
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ICodexBarService, CodexBarService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISetupChecker, SetupChecker>();
        services.AddSingleton<ISampleDataLoader, SampleDataLoader>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SetupViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
