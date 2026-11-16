using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ContentDialog as the emulator draws it, on the reference profile (device 11, 1080 × 1920 at 2.4): title
/// "content dialog", a one-line body "Agjy 0123 the dialog's body text, long enough to wrap onto a second line." (a
/// string, so it doesn't wrap), buttons "ok" and "cancel", shown with <c>ShowAsync</c> from a Button "open". Read by
/// #150 from the screen grabs of refapp runs <c>20261116-1934-dev11</c> and <c>-1944</c> (committed in
/// <c>ref/wp81/ContentDialog/</c>) and the dismissal probes <c>20261116-1951-dev11</c> and <c>-1955</c>, and by #149 from
/// the recording of run <c>20261116-1835-dev11</c> (<c>tools/refapp/README.md</c>, "ContentDialog's layout", "How each
/// popup is dismissed" and "The popups' bands"). View pixels unless named raw.
/// </summary>
internal static class ContentDialogReadings
{
    public const double Scale = 2.4;

    /// <summary>One raw pixel, in view pixels: the layout's rounding (ADR 0003).</summary>
    public const double RawPixel = 1 / Scale;

    /// <summary>The dialog with buttons: 0 to 199.17 (478 raw rows of the chrome).</summary>
    public const double Height = 199.1667;

    /// <summary>The dialog with no button: 0 to 153.33, the hosts collapsed, the row its 45.</summary>
    public const double NoButtonsHeight = 153.3333;

    /// <summary>The title's top left, <c>ContentDialogTitleMargin</c> on the raw grid.</summary>
    public static readonly Point TitleAt = new(19.1667, 33.3333);

    /// <summary>The body's top.</summary>
    public const double BodyTop = 84.1667;

    /// <summary>The button row's hosts: from 108.33, 90.83 tall, each half the width.</summary>
    public const double HostsTop = 108.3333;

    /// <summary>A dialog button's outline: 196.67 × 38.33 at x 19.17 (the first host) and 234.58 (the second), y 142.08.</summary>
    public static readonly Rect Outline1 = new(19.1667, 142.0833, 196.6667, 38.3333);

    public static readonly Rect Outline2 = new(234.5833, 142.0833, 196.6667, 38.3333);

    /// <summary>The full dialog's bar buttons, drawn by the shell: 194.17 × 41.67 at x 20.83 and 235.83, y 747.08.</summary>
    public static readonly Rect BarOutline1 = new(20.8333, 747.0833, 194.1667, 41.6667);

    public static readonly Rect BarOutline2 = new(235.8333, 747.0833, 194.1667, 41.6667);

    /// <summary>The page's compact app bar's top, 62.92 above the foot: where the long body is clipped.</summary>
    public const double BarTop = 800 - 62.9167;

    /// <summary>The chrome, <c>ContentDialogBackgroundThemeBrush</c> (<c>PhoneChromeColor</c>).</summary>
    public static Color Chrome(bool dark) => Color.FromUInt32(dark ? 0xFF1F1F1Fu : 0xFFDDDDDDu);

    /// <summary>The layer over the window behind the dialog, <c>ContentDialogDimmingThemeBrush</c>.</summary>
    public static Color Dim(bool dark) => Color.FromUInt32(dark ? 0x99000000u : 0x99FFFFFFu);

    // The motion (#149): one band, the dialog, about its centre line, one camera for the screen 999 px in front over
    // y 400; from 45.0° to 0 at tick 9 (141 ms) on an eased exponential, exponent 1.68 (rms 0.21°). Closing: gone in one
    // frame 95 to 120 ms after Hide(), Closed at once, in the call's frame.
    public const double Tick = 15.625;

    public const double OpenFrom = 45;

    public const double OpenExponent = 1.68;

    public const int OpenTicks = 9;

    public const double GoneFrom = 95;

    public const double GoneBy = 120;
}
