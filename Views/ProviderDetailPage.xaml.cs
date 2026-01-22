using CodexBarWin.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace CodexBarWin.Views;

/// <summary>
/// Detail page for a provider's usage data.
/// </summary>
public sealed partial class ProviderDetailPage : Page
{
    private UsageData? _data;

    public event EventHandler? BackRequested;

    public ProviderDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is UsageData data)
        {
            _data = data;
            UpdateUI();
        }
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateUI()
    {
        if (_data == null) return;

        // Header
        ProviderIcon.Data = GetProviderIconGeometry(_data.Provider);
        ProviderName.Text = FormatProviderName(_data.Provider);

        // Update section titles based on provider (e.g., "Session/Weekly" for Claude/Codex, "Pro/Flash" for Gemini)
        SessionSectionTitle.Text = $"{_data.SessionLabel} Usage";
        WeeklySectionTitle.Text = $"{_data.WeeklyLabel} Usage";
        TertiarySectionTitle.Text = _data.TertiaryLabel;

        // Session usage
        if (_data.Session != null)
        {
            SessionSection.Visibility = Visibility.Visible;
            SessionMeter.Percent = _data.Session.Percent;
            SessionPercentText.Text = $"{_data.Session.PercentText} used";
            SessionResetText.Text = !string.IsNullOrEmpty(_data.Session.ResetIn)
                ? $"Resets {_data.Session.ResetIn}"
                : string.Empty;
        }
        else
        {
            SessionSection.Visibility = Visibility.Collapsed;
        }

        // Weekly usage
        if (_data.Weekly != null)
        {
            WeeklySection.Visibility = Visibility.Visible;
            WeeklyMeter.Percent = _data.Weekly.Percent;
            WeeklyPercentText.Text = $"{_data.Weekly.PercentText} used";
            WeeklyResetText.Text = !string.IsNullOrEmpty(_data.Weekly.ResetIn)
                ? $"Resets {_data.Weekly.ResetIn}"
                : string.Empty;
        }
        else
        {
            WeeklySection.Visibility = Visibility.Collapsed;
        }

        // Tertiary usage (e.g., Sonnet Weekly for Claude)
        if (_data.Tertiary != null)
        {
            TertiarySection.Visibility = Visibility.Visible;
            TertiaryMeter.Percent = _data.Tertiary.Percent;
            TertiaryPercentText.Text = $"{_data.Tertiary.PercentText} used";
            TertiaryResetText.Text = !string.IsNullOrEmpty(_data.Tertiary.ResetIn)
                ? $"Resets {_data.Tertiary.ResetIn}"
                : string.Empty;
        }
        else
        {
            TertiarySection.Visibility = Visibility.Collapsed;
        }

        // Details
        if (!string.IsNullOrEmpty(_data.Plan))
        {
            PlanRow.Visibility = Visibility.Visible;
            PlanText.Text = _data.Plan;
        }
        else
        {
            PlanRow.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrEmpty(_data.Status))
        {
            StatusRow.Visibility = Visibility.Visible;
            StatusText.Text = _data.Status;
        }
        else
        {
            StatusRow.Visibility = Visibility.Collapsed;
        }

        FetchedAtText.Text = _data.FetchedAt.ToString("g");

        // Error
        if (_data.HasError)
        {
            ErrorSection.Visibility = Visibility.Visible;
            ErrorTextBox.Text = _data.Error ?? string.Empty;
        }
        else
        {
            ErrorSection.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCopyError(object sender, RoutedEventArgs e)
    {
        if (_data?.Error != null)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(_data.Error);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    // SVG path data for provider icons (from Bootstrap Icons and UXWing)
    private static readonly Dictionary<string, string> ProviderIconPaths = new()
    {
        ["claude"] = "m3.127 10.604 3.135-1.76.053-.153-.053-.085H6.11l-.525-.032-1.791-.048-1.554-.065-1.505-.08-.38-.081L0 7.832l.036-.234.32-.214.455.04 1.009.069 1.513.105 1.097.064 1.626.17h.259l.036-.105-.089-.065-.068-.064-1.566-1.062-1.695-1.121-.887-.646-.48-.327-.243-.306-.104-.67.435-.48.585.04.15.04.593.456 1.267.981 1.654 1.218.242.202.097-.068.012-.049-.109-.181-.9-1.626-.96-1.655-.428-.686-.113-.411a2 2 0 0 1-.068-.484l.496-.674L4.446 0l.662.089.279.242.411.94.666 1.48 1.033 2.014.302.597.162.553.06.17h.105v-.097l.085-1.134.157-1.392.154-1.792.052-.504.25-.605.497-.327.387.186.319.456-.045.294-.19 1.23-.37 1.93-.243 1.29h.142l.161-.16.654-.868 1.097-1.372.484-.545.565-.601.363-.287h.686l.505.751-.226.775-.707.895-.585.759-.839 1.13-.524.904.048.072.125-.012 1.897-.403 1.024-.186 1.223-.21.553.258.06.263-.218.536-1.307.323-1.533.307-2.284.54-.028.02.032.04 1.029.098.44.024h1.077l2.005.15.525.346.315.424-.053.323-.807.411-3.631-.863-.872-.218h-.12v.073l.726.71 1.331 1.202 1.667 1.55.084.383-.214.302-.226-.032-1.464-1.101-.565-.497-1.28-1.077h-.084v.113l.295.432 1.557 2.34.08.718-.112.234-.404.141-.444-.08-.911-1.28-.94-1.44-.759-1.291-.093.053-.448 4.821-.21.246-.484.186-.403-.307-.214-.496.214-.98.258-1.28.21-1.016.19-1.263.112-.42-.008-.028-.092.012-.953 1.307-1.448 1.957-1.146 1.227-.274.109-.477-.247.045-.44.266-.39 1.586-2.018.956-1.25.617-.723-.004-.105h-.036l-4.212 2.736-.75.096-.324-.302.04-.496.154-.162 1.267-.871z",
        ["codex"] = "M14.949 6.547a3.94 3.94 0 0 0-.348-3.273 4.11 4.11 0 0 0-4.4-1.934A4.1 4.1 0 0 0 8.423.2 4.15 4.15 0 0 0 6.305.086a4.1 4.1 0 0 0-1.891.948 4.04 4.04 0 0 0-1.158 1.753 4.1 4.1 0 0 0-1.563.679A4 4 0 0 0 .554 4.72a3.99 3.99 0 0 0 .502 4.731 3.94 3.94 0 0 0 .346 3.274 4.11 4.11 0 0 0 4.402 1.933c.382.425.852.764 1.377.995.526.231 1.095.35 1.67.346 1.78.002 3.358-1.132 3.901-2.804a4.1 4.1 0 0 0 1.563-.68 4 4 0 0 0 1.14-1.253 3.99 3.99 0 0 0-.506-4.716m-6.097 8.406a3.05 3.05 0 0 1-1.945-.694l.096-.054 3.23-1.838a.53.53 0 0 0 .265-.455v-4.49l1.366.778q.02.011.025.035v3.722c-.003 1.653-1.361 2.992-3.037 2.996m-6.53-2.75a2.95 2.95 0 0 1-.36-2.01l.095.057L5.29 12.09a.53.53 0 0 0 .527 0l3.949-2.246v1.555a.05.05 0 0 1-.022.041L6.473 13.3c-1.454.826-3.311.335-4.15-1.098m-.85-6.94A3.02 3.02 0 0 1 3.07 3.949v3.785a.51.51 0 0 0 .262.451l3.93 2.237-1.366.779a.05.05 0 0 1-.048 0L2.585 9.342a2.98 2.98 0 0 1-1.113-4.094zm11.216 2.571L8.747 5.576l1.362-.776a.05.05 0 0 1 .048 0l3.265 1.86a3 3 0 0 1 1.173 1.207 2.96 2.96 0 0 1-.27 3.2 3.05 3.05 0 0 1-1.36.997V8.279a.52.52 0 0 0-.276-.445m1.36-2.015-.097-.057-3.226-1.855a.53.53 0 0 0-.53 0L6.249 6.153V4.598a.04.04 0 0 1 .019-.04L9.533 2.7a3.07 3.07 0 0 1 3.257.139c.474.325.843.778 1.066 1.303.223.526.289 1.103.191 1.664zM5.503 8.575 4.139 7.8a.05.05 0 0 1-.026-.037V4.049c0-.57.166-1.127.476-1.607s.752-.864 1.275-1.105a3.08 3.08 0 0 1 3.234.41l-.096.054-3.23 1.838a.53.53 0 0 0-.265.455zm.742-1.577 1.758-1 1.762 1v2l-1.755 1-1.762-1z",
        ["gemini"] = "M8 0c.167 0 .313.114.354.277a9.58 9.58 0 0 0 .492 1.453c.53 1.23 1.256 2.307 2.179 3.23.923.922 2 1.649 3.23 2.179a9.59 9.59 0 0 0 1.454.492c.162.04.277.186.277.354s-.115.313-.277.354a9.58 9.58 0 0 0-1.453.492c-1.23.53-2.307 1.256-3.23 2.179-.923.922-1.649 2-2.18 3.23a9.59 9.59 0 0 0-.491 1.454.365.365 0 0 1-.354.277c-.168 0-.313-.114-.354-.277a9.58 9.58 0 0 0-.492-1.453c-.53-1.23-1.255-2.307-2.179-3.23-.922-.923-2-1.649-3.23-2.18a9.59 9.59 0 0 0-1.453-.491A.365.365 0 0 1 0 8c0-.168.114-.313.277-.354a9.58 9.58 0 0 0 1.453-.492c1.23-.53 2.308-1.256 3.23-2.179.923-.923 1.65-2 2.18-3.23a9.59 9.59 0 0 0 .491-1.453A.365.365 0 0 1 8 0z"
    };

    private const string DefaultIconPath = "M8 0a8 8 0 1 0 0 16A8 8 0 0 0 8 0";

    private static Geometry GetProviderIconGeometry(string provider)
    {
        var providerId = provider.ToLowerInvariant();
        var pathData = ProviderIconPaths.GetValueOrDefault(providerId, DefaultIconPath);

        try
        {
            return (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                typeof(Geometry), pathData);
        }
        catch
        {
            return (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                typeof(Geometry), DefaultIconPath);
        }
    }

    private static string FormatProviderName(string provider) => provider.ToLowerInvariant() switch
    {
        "claude" => "Claude",
        "codex" => "Codex",
        "gemini" => "Gemini",
        "cursor" => "Cursor",
        "copilot" => "Copilot",
        _ => provider
    };
}
