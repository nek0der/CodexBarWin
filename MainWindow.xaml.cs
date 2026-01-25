using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using CodexBarWin.Models;
using CodexBarWin.Services;
using CodexBarWin.ViewModels;
using CodexBarWin.Views;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace CodexBarWin;

/// <summary>
/// Native API for window customization and mouse hooks.
/// </summary>
internal static class NativeMethods
{
    // DWM constants
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    // Mouse hook constants
    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_MBUTTONDOWN = 0x0207;

    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// Apply rounded corners to the window.
    /// </summary>
    public static void ApplyRoundedCorners(IntPtr hwnd)
    {
        int cornerPreference = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, Marshal.SizeOf<int>());
    }
}

/// <summary>
/// Main flyout window for displaying usage data.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly UISettings _uiSettings;
    private TaskbarIcon? _trayIcon;
    private const int WindowWidth = 400;
    private const int WindowHeight = 480;
    private const int WindowMarginRight = 7;  // Adjusted for window shadow (target: 15px from edge)
    private const int WindowMarginBottom = 2; // Adjusted for window shadow (target: 10px from taskbar)
    private bool _isWindowVisible;
    private bool _isAnimating;
    private bool _isShowingAnimation;  // Track animation direction
    private DispatcherTimer? _animationTimer;
    private int _animationStep;
    private int _startX;
    private int _targetX;
    private int _fixedY;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private GCHandle _mouseProcHandle;
    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _backdropConfiguration;

    // Animation settings (from configuration)
    private int ShowAnimationDurationMs => _settingsService.Settings.Animation.ShowDurationMs;
    private int HideAnimationDurationMs => _settingsService.Settings.Animation.HideDurationMs;
    private int AnimationSteps => _settingsService.Settings.Animation.Steps;

    public MainWindow()
    {
        InitializeComponent();

        // Set up Acrylic backdrop (like OS system tray)
        SetupAcrylicBackdrop();

        // Get services from DI
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();

        // Initialize UISettings for theme detection
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnSystemColorValuesChanged;

        // Configure window for flyout style (borderless)
        ConfigureWindow();

        // Handle window keyboard events
        ContentFrame.KeyDown += OnKeyDown;

        // Cleanup on close
        Closed += OnWindowClosed;

        // Navigate to main page
        ContentFrame.Navigated += OnFrameNavigated;
        ContentFrame.Navigate(typeof(MainPage));

        // Create tray icon after window is loaded
        ContentFrame.Loaded += OnContentLoaded;
    }

    #region Light Dismiss Mouse Hook

    private void InstallMouseHook()
    {
        if (_mouseHookId != IntPtr.Zero) return;

        _mouseProc = MouseHookCallback;
        // Pin the delegate to prevent GC from collecting it while the hook is active
        _mouseProcHandle = GCHandle.Alloc(_mouseProc);

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(curModule?.ModuleName ?? string.Empty);
        _mouseHookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
    }

    private void UninstallMouseHook()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        // Free the pinned delegate
        if (_mouseProcHandle.IsAllocated)
        {
            _mouseProcHandle.Free();
        }
        _mouseProc = null;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isWindowVisible && !_isAnimating)
        {
            var msg = (int)wParam;
            if (msg == NativeMethods.WM_LBUTTONDOWN ||
                msg == NativeMethods.WM_RBUTTONDOWN ||
                msg == NativeMethods.WM_MBUTTONDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                var clickPoint = new System.Drawing.Point(hookStruct.pt.x, hookStruct.pt.y);

                // Get window bounds
                var windowPos = AppWindow.Position;
                var windowSize = AppWindow.Size;
                var windowRect = new System.Drawing.Rectangle(
                    windowPos.X, windowPos.Y,
                    windowSize.Width, windowSize.Height);

                // If click is outside the window, hide it
                if (!windowRect.Contains(clickPoint))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_isWindowVisible && !_isAnimating)
                        {
                            HideWindow();
                        }
                    });
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    #endregion

    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        if (e.Content is MainPage mainPage)
        {
            mainPage.SettingsRequested += OnSettingsRequested;
            mainPage.ProviderDetailRequested += OnProviderDetailRequested;
        }
        else if (e.Content is ProviderDetailPage detailPage)
        {
            detailPage.BackRequested += OnBackRequested;
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        // Check if settings window is already open
        var existingWindow = App.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            return;
        }

        // Create and show new settings window
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    private void OnProviderDetailRequested(object? sender, UsageData data)
    {
        ContentFrame.Navigate(typeof(ProviderDetailPage), data,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void OnBackRequested(object? sender, EventArgs e)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack(new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }

    private void OnContentLoaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Loaded -= OnContentLoaded;

        // Create and configure tray icon
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "CodexBarWin - AI Usage Monitor",
            NoLeftClickDelay = true,
            LeftClickCommand = new RelayCommand(ToggleWindow),
            ContextMenuMode = ContextMenuMode.SecondWindow
        };

        // Create context menu (SecondWindow mode for dark mode support)
        var presenterStyle = new Style(typeof(MenuFlyoutPresenter));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 150.0));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 100.0));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, double.NaN));
        presenterStyle.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden));
        presenterStyle.Setters.Add(new Setter(ScrollViewer.VerticalScrollModeProperty, ScrollMode.Disabled));

        var contextMenu = new MenuFlyout
        {
            MenuFlyoutPresenterStyle = presenterStyle
        };

        var settingsItem = new MenuFlyoutItem
        {
            Text = "Settings",
            Icon = new FontIcon { Glyph = "\uE713" }
        };
        settingsItem.Click += (_, _) => OnSettingsRequested(null, EventArgs.Empty);
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new MenuFlyoutSeparator());

        var quitItem = new MenuFlyoutItem
        {
            Text = "Quit",
            Icon = new FontIcon { Glyph = "\uE7E8" }
        };
        quitItem.Click += (_, _) => Application.Current.Exit();
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextFlyout = contextMenu;

        // Set the icon using System.Drawing generated icon
        UpdateTrayIcon();

        // Initialize tray icon for this window
        _trayIcon.ForceCreate();

        // Start hidden - user clicks tray to show
        HideWindow();
    }

    /// <summary>
    /// Updates the tray icon based on current theme.
    /// </summary>
    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;

        var icon = CreateTrayIcon();
        _trayIcon.UpdateIcon(icon);
    }

    /// <summary>
    /// Creates a tray icon based on the taskbar theme.
    /// Uses dark icon for light taskbar, white icon for dark taskbar.
    /// </summary>
    private static Icon CreateTrayIcon()
    {
        var isLightTaskbar = IsTaskbarLightTheme();
        var iconName = isLightTaskbar ? "app-dark.ico" : "app.ico";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconName);
        return new Icon(iconPath);
    }

    /// <summary>
    /// Detects if the Windows taskbar is using a light theme.
    /// </summary>
    private static bool IsTaskbarLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("SystemUsesLightTheme");
            return value is int intValue && intValue == 1;
        }
        catch
        {
            return false; // Default to dark theme on error
        }
    }

    /// <summary>
    /// Sets up Acrylic backdrop (like OS system tray flyout).
    /// </summary>
    private void SetupAcrylicBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported()) return;

        _acrylicController = new DesktopAcrylicController();

        // Configure backdrop with theme awareness
        _backdropConfiguration = new SystemBackdropConfiguration
        {
            IsInputActive = true
        };
        UpdateBackdropTheme();

        _acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
        _acrylicController.AddSystemBackdropTarget(
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

        // Get the actual theme from the root element or system
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

    /// <summary>
    /// Called when the system color values change (theme change).
    /// </summary>
    private void OnSystemColorValuesChanged(UISettings sender, object args)
    {
        // Update icon on UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateTrayIcon();
        });
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged -= OnActualThemeChanged;
        }
        StopAnimationTimer();
        UninstallMouseHook();
        _acrylicController?.Dispose();
        _trayIcon?.Dispose();
    }

    private void ToggleWindow()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isWindowVisible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }
        });
    }

    private void ShowWindow()
    {
        if (_isAnimating) return;

        // Calculate positions
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;

        var workArea = displayArea.WorkArea;
        var finalX = workArea.X + workArea.Width - WindowWidth - WindowMarginRight;
        _fixedY = workArea.Y + workArea.Height - WindowHeight - WindowMarginBottom;

        // Start from right edge (off-screen)
        _startX = workArea.X + workArea.Width;
        _targetX = finalX;

        // Position window at start location (off-screen right)
        AppWindow.Move(new PointInt32(_startX, _fixedY));

        WindowExtensions.Show(this);
        Activate();
        _isWindowVisible = true;

        // Install mouse hook for light dismiss
        InstallMouseHook();

        // Animate window sliding left
        PlaySlideAnimation(isShowing: true);
    }

    private void HideWindow()
    {
        if (_isAnimating) return;
        if (!_isWindowVisible) return;

        // Uninstall mouse hook
        UninstallMouseHook();

        // Get work area to determine target position (off-screen right)
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workAreaRight = displayArea?.WorkArea.X + displayArea?.WorkArea.Width ?? AppWindow.Position.X + WindowWidth;

        // Animate window sliding right (off-screen)
        _startX = AppWindow.Position.X;
        _targetX = workAreaRight;

        PlaySlideAnimation(isShowing: false);
    }

    private void PlaySlideAnimation(bool isShowing)
    {
        _isAnimating = true;
        _isShowingAnimation = isShowing;
        _animationStep = 0;

        // Fluent Design: show=150ms, hide=100ms
        var duration = isShowing ? ShowAnimationDurationMs : HideAnimationDurationMs;
        var interval = duration / AnimationSteps;

        // Stop and clean up existing timer
        StopAnimationTimer();

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(interval)
        };

        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, object e)
    {
        if (_animationTimer == null) return;

        var isShowing = _isShowingAnimation;

        _animationStep++;
        var progress = (double)_animationStep / AnimationSteps;

        // Fluent Design easing functions
        // Show: Decelerate (Fast Out, Slow In)
        // Hide: Accelerate (Slow Out, Fast In)
        var easedProgress = isShowing
            ? 1 - Math.Pow(1 - progress, 3)  // Decelerate: ease-out
            : Math.Pow(progress, 3);          // Accelerate: ease-in

        var currentX = (int)(_startX + (_targetX - _startX) * easedProgress);
        AppWindow.Move(new PointInt32(currentX, _fixedY));

        if (_animationStep >= AnimationSteps)
        {
            StopAnimationTimer();
            _isAnimating = false;

            if (!isShowing)
            {
                WindowExtensions.Hide(this);
                _isWindowVisible = false;
            }
        }
    }

    private void StopAnimationTimer()
    {
        if (_animationTimer != null)
        {
            _animationTimer.Stop();
            _animationTimer.Tick -= OnAnimationTick;
            _animationTimer = null;
        }
    }

    private void ConfigureWindow()
    {
        var appWindow = AppWindow;
        var presenter = appWindow.Presenter as OverlappedPresenter;

        if (presenter != null)
        {
            // Configure as borderless popup-style window
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true; // Above other apps (animation stays above taskbar)

            // Remove title bar completely (keep border for shadow)
            presenter.SetBorderAndTitleBar(true, false);
        }

        // Hide from taskbar and Alt+Tab
        appWindow.IsShownInSwitchers = false;

        // Get window handle and apply rounded corners
        var hwnd = WindowNative.GetWindowHandle(this);
        NativeMethods.ApplyRoundedCorners(hwnd);

        appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
        appWindow.Title = string.Empty;

        // Position window off-screen initially (right edge for horizontal slide animation)
        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var initialX = workArea.X + workArea.Width; // Off-screen right
            var initialY = workArea.Y + workArea.Height - WindowHeight - WindowMarginBottom;
            appWindow.Move(new PointInt32(initialX, initialY));
        }
    }

    public void ShowFlyout()
    {
        ShowWindow();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
            else
            {
                HideWindow();
            }
        }
    }
}
