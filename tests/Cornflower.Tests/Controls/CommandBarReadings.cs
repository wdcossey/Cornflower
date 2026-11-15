using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 page's app bar as the shell draws it: a <c>CommandBar</c> set as the page's <c>BottomAppBar</c>, with three
/// primary commands (add; favorite, a checked toggle; delete, disabled) and two secondary ones ("first menu item",
/// "second menu item"), compact and minimal, closed and open, dark and light, on the reference profile (device 11, 1080
/// × 1920 at 2.4). Read from the screenshots of refapp runs <c>20261109-2124-dev11</c> and <c>20261109-2130-dev11</c>
/// (the same to the pixel row), committed in <c>ref/wp81/CommandBar/</c>, and the API's reading of run
/// <c>20261115-2050-dev11</c> (<c>tools/refapp/README.md</c>, "The page's app bar"). Raw pixels unless named view
/// pixels.
/// </summary>
internal static class CommandBarReadings
{
    public const double Scale = 2.4;

    // The bar's top row and height (view: 737.0833 and 62.9167 compact, 773.75 and 26.25 minimal, 514.5833 and
    // 285.4167 open, either mode, with these commands).
    public const int CompactTop = 1769;
    public const int MinimalTop = 1857;
    public const int OpenTop = 1235;
    public const double CompactHeight = 151 / Scale;
    public const double MinimalHeight = 63 / Scale;
    public const double OpenHeight = 685 / Scale;

    // The circles' centres (the middle one 2 raw px left of the screen's), and their ring: the bounding box 98 raw px
    // across from 25 below the bar's top (the edge's coverage puts it at 25.08), the ring 6.44 raw wide.
    public static readonly int[] CircleCentres = [361, 538, 715];
    public const int CircleTopBelowBar = 25;
    public const int CircleRadius = 49;
    public const int RingWidth = 6;

    // The ellipsis's dots: 12 raw px across, from 1794 on the compact bar (25 below its top); the first dot's left at
    // 976, the last's right at 1030.
    public static readonly (int Left, int Right)[] Dots = [(976, 987), (998, 1009), (1019, 1030)];
    public const int DotTopBelowBar = 25;

    // Open: the labels' baseline 171 raw px below the bar's top (71.25 view), the secondary commands' 302 and 444
    // (125.83 and 185 view), 142 apart; the first word's ink from x 51.
    public const double LabelBaselineBelowBar = 171 / Scale;
    public static readonly double[] MenuBaselinesBelowBar = [302 / Scale, 444 / Scale];
    public const double MenuPitch = 142 / Scale;
    public const double MenuInkLeft = 51 / Scale;

    // The colours, dark then light. The bar: PhoneChromeColor. The page behind it: the page's background.
    public static Color Bar(bool dark) => Color.FromUInt32(dark ? 0xFF1F1F1Fu : 0xFFDDDDDDu);

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);

    // The ring and the checked circle's fill: PhoneForegroundColor.
    public static Color Ring(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);

    // The words, the icons and the dots: PhoneTextHighColor over the bar, #DE000000 over #FFDDDDDD in light.
    public static Color Text(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF1D1D1Du);

    // A disabled command's ring: the ring at 0x73 over the bar.
    public static Color DisabledRing(bool dark) => Color.FromUInt32(dark ? 0xFF848484u : 0xFF797979u);

    // A disabled command's opacity: its ring, icon and label at 0x73 / 0xFF, in both themes.
    public const double DisabledOpacity = 0.451;
}
