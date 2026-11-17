using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The emulator's DatePicker and TimePicker, and their pickers, as docs/controls/DatePicker/PLAN.md reads them: the
/// closed controls' dumps and grabs and their inner button forced into its states (run <c>20261116-1934-dev11</c>, #150,
/// B19), the DatePickerFlyout's and TimePickerFlyout's dumps and grabs at rest (<c>20261108-2254-dev11</c>), #149's
/// motion (<c>20261116-1835-dev11</c>) and #150's dismissal (<c>20261116-1951-dev11</c>). View pixels; a closed
/// control's in the control, a picker's in the window.
/// </summary>
internal static class DateTimePickerReadings
{
    /// <summary>One tick of the phone's animation clock, ms.</summary>
    public const double Tick = 15.625;

    /// <summary>The reference app's slot: the control 19.17 in and 12.08 down, 412.08 wide.</summary>
    public static readonly Rect Slot = new(19.1667, 12.0833, 412.0833, 127.9167);

    /// <summary>The header: at the top, one 16 px line (21.67), 3 over the button.</summary>
    public const double HeaderHeight = 21.6667;

    /// <summary>FlyoutButton: 18.75 down, the control's width, 57.5 tall (the touch target).</summary>
    public static readonly Rect FlyoutButton = new(0, 18.75, 412.0833, 57.5);

    /// <summary>Its Border, drawn 9.5 in from the touch target above and below: 28.33 down, 38.33 tall, 2.5 thick.</summary>
    public static readonly Rect ButtonBorder = new(0, 28.3333, 412.0833, 38.3333);

    /// <summary>The text's left: 2.5 of border and 6.5 of padding.</summary>
    public const double TextLeft = 9.1667;

    /// <summary>The text, the culture's short pattern (en-US).</summary>
    public const string DateText = "7/15/2014";

    public const string TimeText = "12:30 PM";

    /// <summary>The button's text: Wp81's Button's, 18.14 SemiBold.</summary>
    public const double TextSize = 18.14;

    /// <summary>The header's colour, every state: #99FFFFFF dark, #73000000 light.</summary>
    public static Color Header(bool dark) => dark ? Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x73, 0, 0, 0);

    /// <summary>The text and the border at rest: <c>PhoneForegroundColor</c>.</summary>
    public static Color Foreground(bool dark) => dark ? Colors.White : Colors.Black;

    /// <summary>Pressed: the border's inside the accent, the text white, the border unchanged.</summary>
    public static readonly Color Accent = Color.FromRgb(0x3E, 0x65, 0xFF);

    public static readonly Color OverAccent = Colors.White;

    /// <summary>Disabled: the border and the text <c>PhoneBaseLowColor</c>, #73FFFFFF dark, #40000000 light.</summary>
    public static Color Disabled(bool dark) => dark ? Color.FromArgb(0x73, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x40, 0, 0, 0);

    /// <summary>The picker fills the window: 0, 0, 450 × 800, in <c>FlyoutBackgroundThemeBrush</c>.</summary>
    public static readonly Rect Screen = new(0, 0, 450, 800);

    public static Color Chrome(bool dark) => dark ? Color.FromRgb(0x1F, 0x1F, 0x1F) : Color.FromRgb(0xDD, 0xDD, 0xDD);

    /// <summary>The three wheels' lefts, 138.333 wide, 700 tall from 37.08: the date's and the time's alike.</summary>
    public static readonly double[] WheelLefts = [17.5, 155.8333, 294.1667];

    public const double WheelTop = 37.0833;

    public const double WheelWidth = 138.3333;

    public const double WheelHeight = 700;

    /// <summary>The bar: the page's compact app bar, from 737.08, 62.92 tall.</summary>
    public const double BarTop = 737.0833;

    /// <summary>The time picker's item text: 43 SemiBold, its top 64.53 into the item (382.47 − 317.94), scaled 1.125.</summary>
    public const double TimeItemTextSize = 43;

    /// <summary>The rows: 160 px, five on the 800.</summary>
    public const double RowHeight = 160;

    /// <summary>The rows' opening rates, degrees a tick, rows 0 to 4 (#149: 12.50, 9.60, 7.84, 6.60, 5.67).</summary>
    public static readonly double[] RowRates = [12.5, 9.6, 7.84, 6.6, 5.67];

    /// <summary>
    /// Closing, band k starts 2k ticks after the call and turns back at 10.95° a tick, gone at 90°: the last, band 4,
    /// is gone 8 + 8.2 ticks in, and the popup closes on the 16th tick. (The DatePickerFlyout's <c>Closed</c> event
    /// came at 17 in all eight calls, a tick later; the bands' fit is the Flyout's, by the recording.)
    /// </summary>
    public const int ClosedTicks = 16;
}
