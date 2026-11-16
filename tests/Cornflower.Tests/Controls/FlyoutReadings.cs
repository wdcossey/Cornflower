using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Flyout as the emulator draws it: a <c>Flyout</c> shown on a Button, its content a wrapping
/// <c>TextBlock</c>, "flyout content", with <c>Placement</c> Top and Full, dark and light, on the reference profile
/// (device 11, 1080 × 1920 at 2.4). Read from the screenshots of refapp run <c>20261108-2254-dev11</c> (the same to the
/// byte as <c>-2248</c>), committed in <c>ref/wp81/Flyout/</c>, the popup dumps of runs <c>20261108-1705-dev11</c>,
/// <c>-1734</c> and <c>-2254</c>, the style (<c>flyoutpresenter.json</c>), and #149's reading of the recording of run
/// <c>20261116-1835-dev11</c> (<c>tools/refapp/README.md</c>, "Popups on the reference profile" and "Flyouts, pickers and
/// the dialog"). View pixels unless named raw.
/// </summary>
internal static class FlyoutReadings
{
    public const double Scale = 2.4;

    /// <summary>The top flyout's presenter in the window: at the top left, the window's width.</summary>
    public static readonly Rect Top = new(0, 0, 450, 78.3333);

    /// <summary>The top flyout's height in the screenshots: 188 raw rows of the chrome.</summary>
    public const int TopRows = 188;

    /// <summary>The full flyout's presenter: the whole window.</summary>
    public static readonly Rect Full = new(0, 0, 450, 799.9999);

    /// <summary>
    /// The ScrollViewer part's top in the presenter, <c>FlyoutContentPanelPortraitThemeMargin</c>'s 25.5 on the raw grid
    /// (61 raw px), and its height in the top flyout, its ScrollBar's panning root's <c>MinHeight</c>, 53, on the grid.
    /// </summary>
    public const double ContentTop = 25.4167;

    public const double TopContentHeight = 52.9167;

    /// <summary>The style's <c>MinHeight</c>, <c>FlyoutThemeMinHeight</c>.</summary>
    public const double MinHeight = 43;

    /// <summary>The chrome, <c>FlyoutBackgroundThemeBrush</c> (<c>PhoneChromeColor</c>), and the page under it, not dimmed.</summary>
    public static Color Chrome(bool dark) => Color.FromUInt32(dark ? 0xFF1F1F1Fu : 0xFFDDDDDDu);

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);

    /// <summary>The content's ink in the top flyout's screenshot: from raw row 66 to 90, from raw column 0.</summary>
    public const int InkTopRow = 66;

    public const int InkLeftColumn = 0;

    // The motion, as #149 read it (run 20261116-1835-dev11, tools/refapp/README.md, "The popups' bands"), in ticks of the
    // phone's 64 Hz clock. Each band turns about its own centre line, one camera for the screen, 999 view px in front of
    // it over y 400; positive brings the lower edge nearer. Top: one band, 80° to 0 at 9.31° a tick; closing, no turn,
    // gone in one frame 98 to 122 ms after Hide(), Closed 9 ticks after. Full: 100 px bands, all from 80°, bands 0 to 2
    // at 12.61, 9.57 and 7.58° a tick, none below 400 px turning, the whole at rest 208 to 248 ms; closing, band k 2k
    // ticks after Hide(), the lower edge back, 10.95° a tick to 90°, Closed 22 ticks after.
    public const double Tick = 15.625;

    public const double EyeDistance = 999;

    public const double EyeY = 400;

    public const double OpenFrom = 80;

    public const double TopOpenRate = 9.31;

    public static readonly double[] FullOpenRates = [12.61, 9.57, 7.58];

    public const double FullBand = 100;

    public const double FullStillFrom = 400;

    // #150's reading of the same recording (PR #172): below its turning bands (400 px) nothing turns; the popup is drawn at rest
    // downwards, to 428, 551, 673 and then 800 px at 119, 144, 206 and 269 ms (call 1).
    public static readonly (double At, double To)[] Reveal = [(119, 428), (144, 551), (206, 673), (269, 800)];

    // Bands 0 to 2 take 6.34, 8.36 and 10.55 ticks, about 2.1 more each band down.
    public const double BandSlowing = 2.1;

    public const double CloseRate = 10.95;

    public const double CloseStagger = 2;

    public const double CloseEdgeOn = 90;

    public const double TopGoneFrom = 98;

    public const double TopGoneBy = 122;

    public const double TopClosedAfterHide = 9 * Tick;

    public const double FullClosedAfterHide = 22 * Tick;
}
