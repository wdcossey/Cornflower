using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The emulator's ListPickerFlyout, PickerFlyout and the pickers' bar, as docs/controls/ListPickerFlyout/PLAN.md reads
/// them: run <c>20261116-1934-dev11</c>'s dumps and grabs (#150, tools/refapp/README.md, "The pickers' bar" and
/// "ListPickerFlyout in Multiple"), <c>20261116-1951-dev11</c>'s and <c>-1955-</c>'s dismissal probe ("How each popup is
/// dismissed"), and #149's motion (<c>20261116-1835-dev11</c>, "The popups' bands"). View pixels, in the window.
/// </summary>
internal static class ListPickerFlyoutReadings
{
    /// <summary>One tick of the phone's animation clock, ms.</summary>
    public const double Tick = 15.625;

    /// <summary>The picker fills the window: 0, 0, 450 × 800, both themes.</summary>
    public static readonly Rect Screen = new(0, 0, 450, 800);

    /// <summary>The chrome, <c>FlyoutBackgroundThemeBrush</c>: #FF1F1F1F dark, #FFDDDDDD light.</summary>
    public static Color Chrome(bool dark) => dark ? Color.FromRgb(0x1F, 0x1F, 0x1F) : Color.FromRgb(0xDD, 0xDD, 0xDD);

    /// <summary>The text, <c>PhoneForegroundBrush</c>.</summary>
    public static Color Foreground(bool dark) => dark ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x00, 0x00, 0x00);

    /// <summary>ContentPanel: 19, 37 in; to the window's foot without the bar, to the bar's top (700 tall) with it.</summary>
    public static readonly Rect ContentPanel = new(19.1667, 37.0833, 412.0833, 762.9166);

    public const double ContentPanelWithBarHeight = 700;

    /// <summary>The title: at the content panel's top left, 32.5 above what follows.</summary>
    public static readonly Point Title = new(19.1667, 37.0833);

    /// <summary>The items host and its list: from 93.33, 371.67 tall for five items (291.67 of items, an 80 footer).</summary>
    public static readonly Rect ItemsHost = new(19.1667, 93.3333, 412.0833, 371.6667);

    /// <summary>Each item: 39.17 tall, 58.33 apart (a 19 margin under each).</summary>
    public const double ItemHeight = 39.1667;

    public const double ItemPitch = 58.3333;

    /// <summary>An item's text: 25.5, Segoe WP SemiLight; its line 33.92 (the phone's 1.33 em).</summary>
    public const double ItemFontSize = 25.5;

    /// <summary>In Multiple, the check box: a 25.42 square (25.5 asked) at x 19.17, 9.5 down the item.</summary>
    public static readonly Rect FirstCheckBox = new(19.1667, 102.9167, 25.4167, 25.4167);

    /// <summary>In Multiple, the item's text from x 44.67: its content 25.5 to the right.</summary>
    public const double MultipleTextLeft = 44.6667;

    /// <summary>The pickers' bar: the page's compact app bar, its top at 737.08, 62.92 tall.</summary>
    public const double BarTop = 737.0833;

    public const double BarHeight = 62.9166;

    /// <summary>The accept and cancel rings' centres; each 40.80 across, a 2.68 ring.</summary>
    public static readonly Point AcceptCentre = new(187.50, 768.33);

    public static readonly Point CancelCentre = new(261.25, 768.33);

    public const double RingDiameter = 40.80;

    /// <summary>The check's ink box, dark and light (20.00 × 17.23, 19.92 × 17.12), centred within 0.13 of its ring's.</summary>
    public static readonly Size CheckInk = new(19.96, 17.18);

    /// <summary>The cross's ink box (16.06 × 15.87 dark, 15.95 × 15.76 light), centred +0.19, +0.48 from its ring's.</summary>
    public static readonly Size CrossInk = new(16.01, 15.82);

    public static readonly Vector CrossOffset = new(0.19, 0.48);

    /// <summary>
    /// <c>Closed</c> after a pick or accept: in the tap's frame come the result and the picked event, and <c>Closed</c>
    /// 447 (Single) and 483 ms (Multiple) later; PickerFlyout's accept 323 ms.
    /// </summary>
    public const double SingleClosedAfterPick = 447;

    public const double MultipleClosedAfterAccept = 483;

    public const double PickerClosedAfterAccept = 323;
}
