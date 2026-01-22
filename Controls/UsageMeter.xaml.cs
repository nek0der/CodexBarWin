using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CodexBarWin.Controls;

/// <summary>
/// A meter control for displaying usage percentage.
/// </summary>
public sealed partial class UsageMeter : UserControl
{
    public static readonly DependencyProperty PercentProperty =
        DependencyProperty.Register(
            nameof(Percent),
            typeof(double),
            typeof(UsageMeter),
            new PropertyMetadata(0.0, OnPercentChanged));

    public double Percent
    {
        get => (double)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public UsageMeter()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    private static void OnPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UsageMeter meter)
        {
            meter.UpdateBar();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        var percent = Math.Clamp(Percent, 0, 100);

        UsageBar.Width = ActualWidth * (percent / 100.0);
        UsageBar.Fill = GetBarColor(percent);
    }

    private static SolidColorBrush GetBarColor(double percent) => percent switch
    {
        >= 95 => new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)),   // Red - Critical
        >= 80 => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),   // Yellow - Warning
        _ => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))        // Blue - Normal
    };
}
