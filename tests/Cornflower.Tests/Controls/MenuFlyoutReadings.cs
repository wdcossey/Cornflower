using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 MenuFlyout as the emulator draws it: a <c>MenuFlyout</c> shown with <c>ShowAt</c> on a Button, on the reference
/// profile (device 11, 1080 × 1920 at 2.4). Read by #150 from the screenshots of refapp runs <c>20261116-1934-dev11</c>
/// and <c>-1940</c>, committed in <c>ref/wp81/MenuFlyout/</c>, the popup dumps of runs <c>20261108-1705-dev11</c> and
/// <c>-1734</c>, the dismissal runs <c>20261116-1951-dev11</c> and <c>-1955</c>, and the app's values of runs
/// <c>20261110-0745-dev11</c> and <c>-0753</c> (<c>tools/refapp/README.md</c>, "MenuFlyout", "How each popup is dismissed"
/// and "MenuFlyout, from the app's values"; gap register A8). View pixels unless named raw.
/// </summary>
internal static class MenuFlyoutReadings
{
    public const double Scale = 2.4;

    /// <summary>The menu is the window's width.</summary>
    public const double Width = 450;

    /// <summary>The target in the placement shots: a Button 57.5 tall.</summary>
    public const double TargetHeight = 57.5;

    /// <summary>A four-item menu's height: its items and padding, its borders not added.</summary>
    public const double FourItemHeight = 244.58;

    /// <summary>The targets' tops in <c>at-300</c>, <c>at-500</c>, <c>at-640</c> and <c>at-720</c>, and the menu's top in each.</summary>
    public static readonly (double TargetTop, double MenuTop)[] Placements =
    [
        (312.08, 369.58), // below: its top at the target's bottom
        (512.08, 267.5), // above: its bottom at the target's top
        (652.08, 407.5),
        (732.08, 487.5),
    ];

    /// <summary>Items are 51.25 tall and 51.25 apart, the first 16.67 below the menu's top, the text from x 19.17.</summary>
    public const double ItemPitch = 51.25;

    public const double FirstItemTop = 16.67;

    /// <summary>
    /// A recorded deviation: our item is one raw pixel shorter than the phone's, 122 raw rows (50.83) to its 123. Its
    /// text is Segoe WP's 1.33 em line at 25.5 px (33.92) with the 8.5 padding above and below, 50.92; the phone's
    /// SemiLight line at that size is a raw pixel taller (ADR 0003).
    /// </summary>
    public const double ItemShortfall = 1 / Scale;

    public const double TextLeft = 19.17;

    /// <summary>The top and bottom lines, 1.5 each, drawn as 4 raw rows.</summary>
    public const int LineRows = 4;

    /// <summary>The menu, <c>MenuFlyoutBackgroundThemeBrush</c>, and its lines, <c>MenuFlyoutBorderThemeBrush</c>.</summary>
    public static Color Background(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);

    public static Color Line(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    /// <summary>A disabled item's text, <c>MenuFlyoutItemDisabledForegroundThemeBrush</c> (<c>PhoneDisabledColor</c>).</summary>
    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x66FFFFFFu : 0x66000000u);

    /// <summary>The long menu: 24 items fill the window, 0 to 800.</summary>
    public const int LongItems = 24;

    public static readonly Rect FullWindow = new(0, 0, 450, 800);

    /// <summary>
    /// The opening, the template's storyboard as written (A8): <c>OuterBorder</c>'s <c>ScaleX</c> 0 to 1 linearly over
    /// 380 ms about (0.5, 0); <c>InnerBorder</c>'s <c>ScaleY</c> 0 until 380 ms, then linearly to 1 at 500 ms, from
    /// (0, 1) above the target (<c>TopPortrait</c>) or (0, 0) below it (<c>BottomPortrait</c>). The borders show only
    /// the line by the target (<c>0,0,0,1.5</c> or <c>0,1.5,0,0</c>) until 500 ms.
    /// </summary>
    public const double OuterOpenMs = 380;

    public const double OpenMs = 500;

    public const double EdgeLine = 1.5;

    /// <summary>The emulator's animation clock: 64 ticks a second.</summary>
    public const double Tick = 15.625;
}
