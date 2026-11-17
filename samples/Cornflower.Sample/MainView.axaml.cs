using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornflower.Controls;

namespace Cornflower.Sample;

/// <summary>
/// The theme page: the accent, the 20 named accents, the type ramp and the font families,
/// each read through its key, and what the device reports; then <see cref="CornflowerConfig"/>'s motion settings,
/// shown as they are and changed at runtime. It has no timing code: <see cref="SampleShell"/> times it,
/// as it does every page, and writes its ready line. First in the catalogue, under the title it shows.
/// </summary>
[SamplePage("theme", Order = 0)]
public partial class MainView : UserControl
{
    internal static readonly string[] NamedAccentNames =
    [
        "Lime", "Green", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
        "Crimson", "Red", "Orange", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe",
    ];

    internal static readonly string[] FontSizeKeys =
    [
        "TextStyleSmallFontSize", "TextStyleMediumFontSize", "TextStyleLargeFontSize",
        "ContentControlFontSize", "TextStyleExtraLargeFontSize", "TextStyleExtraLargePlusFontSize",
        "TextStyleExtraExtraLargeFontSize",
    ];

    internal static readonly string[] FontFamilyKeys =
    [
        "PhoneFontFamilyLight", "PhoneFontFamilySemiLight", "PhoneFontFamilyNormal", "PhoneFontFamilySemiBold",
    ];

    /// <summary>The speeds the page offers: 1 is the phone's.</summary>
    internal static readonly double[] Speeds = [0.5, 1, 2, 4];

    /// <summary>The areas with a speed of their own, by the name the page shows, and the setting each changes.</summary>
    internal static readonly (string Name, Func<double?> Get, Action<double?> Set)[] Areas =
    [
        ("popups", () => CornflowerConfig.PopupSpeed, v => CornflowerConfig.PopupSpeed = v),
        ("pages", () => CornflowerConfig.PageSpeed, v => CornflowerConfig.PageSpeed = v),
        ("tilt", () => CornflowerConfig.TiltSpeed, v => CornflowerConfig.TiltSpeed = v),
        ("pivot and hub", () => CornflowerConfig.PivotAndHubSpeed, v => CornflowerConfig.PivotAndHubSpeed = v),
        ("other controls", () => CornflowerConfig.ControlSpeed, v => CornflowerConfig.ControlSpeed = v),
    ];

    private readonly List<ComboBox> _areaBoxes = [];
    private bool _showing;
    private TopLevel? _top;
    private IInsetsManager? _insets;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);

        var accents = this.GetControl<WrapPanel>("NamedAccents");
        foreach (var name in NamedAccentNames)
            accents.Children.Add(Swatch(name));

        var ramp = this.GetControl<StackPanel>("TypeRamp");
        foreach (var key in FontSizeKeys)
        {
            var line = new TextBlock { Name = key, Text = key };
            line.Bind(TextBlock.FontSizeProperty, this.GetResourceObservable(key));
            ramp.Children.Add(line);
        }

        var families = this.GetControl<StackPanel>("FontFamilies");
        foreach (var key in FontFamilyKeys)
        {
            var line = new TextBlock { Name = key, Text = key };
            line.Bind(TextBlock.FontFamilyProperty, this.GetResourceObservable(key));
            families.Children.Add(line);
        }

        this.GetControl<ComboBox>("MotionSpeedBox").ItemsSource = Speeds.Select(SpeedName).ToArray();
        var areas = this.GetControl<StackPanel>("AreaSpeeds");
        foreach (var (name, _, set) in Areas)
        {
            // The first choice is none of its own: the area follows the motion speed.
            var box = new ComboBox { Name = $"{name.Replace(" ", "")}SpeedBox", ItemsSource = Speeds.Select(SpeedName).Prepend("the motion speed").ToArray() };
            ControlHeader.SetHeader(box, $"{name} speed");
            box.SelectionChanged += (_, _) =>
            {
                if (!_showing && box.SelectedIndex >= 0)
                    set(box.SelectedIndex == 0 ? null : Speeds[box.SelectedIndex - 1]);
            };
            _areaBoxes.Add(box);
            areas.Children.Add(box);
        }

        ShowMotion();
    }

    private static string SpeedName(double speed) =>
        speed == 1 ? "1, the phone's" : speed.ToString(CultureInfo.InvariantCulture);

    /// <summary>The settings as <see cref="CornflowerConfig"/> has them now: when the page is made and placed, and after "the phone's".</summary>
    private void ShowMotion()
    {
        _showing = true;
        this.GetControl<ToggleSwitch>("AnimatedSwitch").IsChecked = CornflowerConfig.Animated;
        this.GetControl<ComboBox>("MotionSpeedBox").SelectedIndex = Array.IndexOf(Speeds, CornflowerConfig.MotionSpeed);
        for (var i = 0; i < Areas.Length; i++)
            // A speed the page doesn't offer (an app's own) shows as none chosen, so any choice changes it.
            _areaBoxes[i].SelectedIndex = Areas[i].Get() is { } own ? Array.IndexOf(Speeds, own) is >= 0 and var at ? at + 1 : -1 : 0;
        this.GetControl<ToggleSwitch>("SingleStepSwitch").IsChecked = CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen;
        _showing = false;
    }

    private void OnAnimatedChanged(object? sender, RoutedEventArgs e)
    {
        if (!_showing && sender is ToggleSwitch { IsChecked: { } on })
            CornflowerConfig.Animated = on;
    }

    private void OnSpeedChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_showing && sender is ComboBox { SelectedIndex: >= 0 and var index })
            CornflowerConfig.MotionSpeed = Speeds[index];
    }

    private void OnSingleStepChanged(object? sender, RoutedEventArgs e)
    {
        if (!_showing && sender is ToggleSwitch { IsChecked: { } on })
            CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen = on;
    }

    /// <summary>Every setting back to the phone's: <see cref="CornflowerConfig"/>'s defaults.</summary>
    private void OnPhoneMotion(object? sender, RoutedEventArgs e)
    {
        CornflowerConfig.Reset();
        ShowMotion();
    }

    /// <summary>What the device reports, as shown on the page (<see cref="Diagnostics.DeviceFacts"/>).</summary>
    internal string DeviceFacts { get; private set; } = "";

    private Border Swatch(string name)
    {
        var brush = new SolidColorBrush();
        brush.Bind(SolidColorBrush.ColorProperty, this.GetResourceObservable($"CornflowerAccent{name}Color"));
        return new Border
        {
            Name = $"Accent{name}",
            Width = 96,
            Height = 96,
            Margin = new Thickness(0, 0, 9.6, 9.6),
            Background = brush,
            Child = new TextBlock
            {
                Text = name.ToLowerInvariant(),
                Margin = new Thickness(6, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Bottom,
                FontSize = 12,
            },
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        _top = top;
        _insets = top.InsetsManager;
        if (_insets is not null)
            _insets.SafeAreaChanged += OnSafeAreaChanged;
        top.ScalingChanged += OnScalingChanged;
        UpdateFacts();
        ShowMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null)
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
        _insets = null;
        if (_top is not null)
            _top.ScalingChanged -= OnScalingChanged;
        _top = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateFacts();
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => UpdateFacts();

    private void OnScalingChanged(object? sender, EventArgs e) => UpdateFacts();

    private void UpdateFacts()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;

        DeviceFacts = Diagnostics.DeviceFacts.Of(top, this);
        this.GetControl<TextBlock>("DeviceFactsText").Text = DeviceFacts.Replace(' ', '\n');
    }
}
