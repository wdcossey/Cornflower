using Avalonia;
using Avalonia.Media;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 GridView and GridViewItem as the phone has them: the values of their default styles (the 8.1 Update 1
/// runtime's <c>gridview.json</c> and <c>gridviewitem.json</c>, which equal the SDK's), what the reference app drew of a
/// <c>GridViewItem</c> holding a 120 × 120 grey tile (<c>Catalog.Tile</c>) on its own, in <c>GridMultiSelect</c> and
/// selected, and of a GridView of six tiles, run <c>20261118-2210-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs
/// are in <c>ref/wp81/GridView/</c>; and the probe of run <c>20261118-2219-dev11</c> (<c>tools/refapp/README.md</c>,
/// "Tier 4's reads", GridView). Values only: the JSON and the logs stay in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class GridViewReadings
{
    // The stage (Suite.Place): 450 × 160 for an item, 450 × 300 for the grid, the slot 19 in and 12 down.
    public const double ItemStageHeight = 160;
    public const double GridStageHeight = 300;

    // Catalog.Tile: 120 × 120, #FF808080.
    public const double Tile = 120;
    public static readonly Color TileGrey = Color.FromUInt32(0xFF808080);

    // GridViewItemMargin, and no padding: the items abut. The phone's GridViewItem style sets neither content alignment,
    // so WinRT's Center stands (the ListViewItem's sets Left and Top).
    public static readonly Thickness ItemMargin = new(0);

    // The six-items capture: the grey runs from raw (46, 29) to (909, 604), three tiles a row, two rows, each 288 raw
    // (120 view) after the last. The item on its own: the tile from raw (396, 62) to (683, 349), centred in the item.
    public static readonly PixelRect SixTiles = new(46, 29, 864, 576);
    public static readonly PixelRect ItemTile = new(396, 62, 288, 288);

    // GridMultiSelect on the item on its own (the item raw 46 to 1034 across, 29 to 383 down): SelectedBorder's 2.5 is
    // 6 raw pixels of accent on each side, 5 on the top (29 to 34, 35 the page's). Offsets in raw pixels from the item's
    // edges, each with what the capture draws there.
    public const int BorderIn = 4;      // inside the border: accent, from every edge
    public const int PastBorder = 7;    // past it: the page

    // SelectedCheckMark on a selected item, in raw pixels from the item's right edge (exclusive) and top: the earmark in
    // the accent and the tick in ListViewItemCheckThemeBrush, white in both variants. The capture's points:
    // (1015, 50) and (992, 54) inside the tick's arms, (1003, 55) the notch between them, (1027, 100) the earmark below
    // the tick, (990, 90) below the earmark's cut. The phone's item ends at raw 1035; ours at 1034 (ADR 0003), so the
    // points are taken from the right edge.
    public static readonly (int FromRight, int FromTop)[] TickPoints = [(20, 21), (43, 25)];
    public static readonly (int FromRight, int FromTop)[] EarmarkPoints = [(32, 26), (8, 71)];
    public static readonly (int FromRight, int FromTop) BelowTheCut = (45, 61);

    // The template's sizes for the check mark: 34 square, at the top right inside the border; the tick 17 × 14.5,
    // 1 in from the corner. Laid out at 2.4: 34.1667 and 17.0833 × 14.5833.
    public const double CheckMark = 34.1667;
    public static readonly Thickness TickMargin = new(0, 1, 1, 0);
    public static readonly Size TickSize = new(17, 14.5);
    public const double BorderThickness = 2.5;   // GridViewItemMultiselectBorderThickness

    // The probe's page: the GridView 19 in and 80 down the 450 × 800 view, 20 tiles. Three a row in the 412-wide list,
    // from its left edge; seven rows, 840 tall in a 720 view, so a drag up scrolls it to 120, its end.
    public static readonly Thickness ProbeGridMargin = new(19, 80, 19, 0);
    public const int ProbeTiles = 20;
    public const double ProbeExtent = 840;
    public const double ProbeViewport = 720;

    // The probe's presses on tile 1, the middle of the first row: at its centre the first frame is depth alone, GlobalOffsetZ -24.95; 6 px from its
    // left edge the tilt settles at RotationY +8.05° (a ListView item's +8.09° at 5% across).
    public const int PressedTile = 1;
    public static readonly TiltState CentreFirstFrame = new(0, 0, -24.95);
    public const double EdgePressX = 6;
    public const double EdgeRotationY = 8.05;
}
