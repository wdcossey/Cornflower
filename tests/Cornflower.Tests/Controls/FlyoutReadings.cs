using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Flyout as the emulator draws it: a <c>Flyout</c> shown on a Button, its content a wrapping
/// <c>TextBlock</c>, "flyout content", with <c>Placement</c> Top and Full, dark and light, on the reference profile
/// (device 11, 1080 × 1920 at 2.4). Read from the screenshots of refapp run <c>20261108-2254-dev11</c> (the same to the
/// byte as <c>-2248</c>), committed in <c>ref/wp81/Flyout/</c>, the popup dumps of runs <c>20261108-1705-dev11</c>,
/// <c>-1734</c> and <c>-2254</c>, the style (<c>flyoutpresenter.json</c>), and the recording of run
/// <c>20261110-0819-dev11</c> (<c>tools/refapp/README.md</c>, "Popups on the reference profile" and "Flyouts, pickers and
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

    // The recording (±25 ms, the recording's clock). Top: 50 / 100 / 142 ms to 50%, 90% and 99% of the way; the bottom
    // edge at 46, 55, 69 then 77 of 78 px on successive recorded frames; gone in one frame about 100 ms after Hide().
    // Full: 141 / 208 / 208 ms in, 200 / 291 / 334 ms out.
    public const double TopOpenAt99 = 142;

    public const double TopGoneAfterHide = 100;

    public const double FullOpenAt99 = 208;

    public const double FullCloseAt99 = 334;

    public const double RecordingTolerance = 25;
}
