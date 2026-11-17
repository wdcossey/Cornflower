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

    // The checked circle's ring: white in both themes. In light the black circle has a white ring, 6 raw px wide
    // (x 489..495 of 489..587 on row 1843; #168), where #118 read only its fill.
    public static readonly Color CheckedRing = Colors.White;

    // The words, the icons and the dots: PhoneTextHighColor over the bar, #DE000000 over #FFDDDDDD in light.
    public static Color Text(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF1D1D1Du);

    // A disabled command's ring: the ring at 0x73 over the bar.
    public static Color DisabledRing(bool dark) => Color.FromUInt32(dark ? 0xFF848484u : 0xFF797979u);

    // A disabled command's opacity: its ring, icon and label at 0x73 / 0xFF, in both themes.
    public const double DisabledOpacity = 0.451;

    // Read for #136 (tools/refapp/README.md, "The app bar under touches, and with other numbers of secondary
    // commands"): the screenshots of run 20261116-0005-dev11 and the recording of 20261115-2354-dev11.

    // The open bar's height by the lines of its list, 0 to 4 (4 for 4 or more): 214, 402, 685 (2 and 3) and 1005 raw.
    public static readonly double[] OpenHeights = [214 / Scale, 402 / Scale, 685 / Scale, 685 / Scale, 1005 / Scale];

    // The accent the recording shows a pressed command in (#3E65FE recorded): PhoneAccentBrush, Cobalt by default.
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FFu);

    // A pressed command, from the recording's stills (540 × 960, 2 raw px a recording px): the disc's ink 45.85
    // recording px across from its area, where the circle's is 48.9 at rest; it and the icon 2.54 recording px higher
    // on the closed bar (0.99 on the open), the icon its own size. On the closed bar the disc's ink is x 158..203 and
    // y 896..942 recording px: raw 316..407 and 1792..1885, 23 below the bar's top.
    public const double PressedCircleScale = 0.9376;
    public const double PressedDiscDiameter = 2 * CircleRadius * PressedCircleScale;
    public const int PressedRise = 5;
    public static readonly (int Left, int Right) PressedDiscColumns = (316, 407);
    public const int PressedDiscTopBelowBar = 1792 - CompactTop;
    public const int PressedDiscBottomBelowBar = 1885 - CompactTop;

    // Read for #148 (tools/refapp/README.md, "The app bar: a long list of secondary commands, and a press in the light
    // theme"): the recording of run 20261116-1724-dev11, the bar light. A pressed command is the checked look: the
    // circle black, its ring white, the icon the bar's colour, at its own size (49.1 recording px across from its
    // area; the checked circle at rest 49.2), 2.5 recording px higher. On the closed bar its ink is x 157..204 and
    // y 895..943 recording px: raw 314..409 and 1790..1887. A checked favorite held draws the same.
    public static readonly (int Left, int Right) LightPressedDiscColumns = (314, 409);
    public const int LightPressedDiscTopBelowBar = 1790 - CompactTop;
    public const int LightPressedDiscBottomBelowBar = 1887 - CompactTop;

    // The motion, by the recording's clock, over fourteen openings and fourteen closings (two runs): the time constant
    // of the rest of the way, and when the top is 90% there and at rest, each from the last frame at rest, in ms.
    public static readonly (double Low, double High) OpenTimeConstant = (76, 92);
    public static readonly (double Low, double High) OpenNinety = (145, 255);
    public static readonly (double Low, double High) OpenAtRest = (384, 455);
    public static readonly (double Low, double High) CloseTimeConstant = (27, 52);
    public static readonly (double Low, double High) CloseNinety = (67, 127);
    public static readonly (double Low, double High) CloseAtRest = (163, 233);
}
