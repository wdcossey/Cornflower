namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 LoopingSelector as the emulator showed it (device 11, the reference profile): the DatePickerFlyout's three
/// wheels, 15 July 2014, read by #149's <c>-Probe looping</c> (runs <c>20261116-1828-dev11</c> and
/// <c>20261116-1904-dev11</c>; <c>tools/refapp/README.md</c>, "LoopingSelector", and <c>tools/motion/looping.py</c>), and
/// the captures of run <c>20261116-1934-dev11</c> (<c>ref/wp81/LoopingSelector/</c>). View pixels, ms.
/// </summary>
internal static class LoopingSelectorReadings
{
    /// <summary>The emulator's frame.</summary>
    public const double Frame = 15.625;

    /// <summary>Each wheel's box: 138.333 wide and 700 tall from y 37.083.</summary>
    public const double ColumnWidth = 138.333;

    /// <inheritdoc cref="ColumnWidth"/>
    public const double ColumnHeight = 700;

    /// <inheritdoc cref="ColumnWidth"/>
    public const double ColumnTop = 37.083;

    /// <summary>The three wheels' left edges: month, day, year.</summary>
    public static readonly double[] ColumnX = [17.5, 155.833, 294.167];

    /// <summary>The items' box, laid out at 2.4 (123 rounded to the raw pixel: 295 px).</summary>
    public const double ItemSize = 122.917;

    /// <summary>The items' <c>ContentScaleTransform</c>.</summary>
    public const double ItemScale = 1.125;

    /// <summary>How far apart the items are: the median over every frame of the runs.</summary>
    public const double Pitch = 138.375;

    /// <summary>The selected item's top in the month and day wheels (the year's, 317.896).</summary>
    public const double SelectedTop = 317.943;

    /// <summary>The selected tile as the capture draws it: x 54 to 359, y 775 to 1080 raw, so 22.5 and 322.917, 127.083 square.</summary>
    public const double TileLeft = 22.5;

    /// <inheritdoc cref="TileLeft"/>
    public const double TileTop = 322.917;

    /// <inheritdoc cref="TileLeft"/>
    public const double TileSize = 127.083;

    /// <summary>The columns' fade: 0.0469 a frame, a straight line, both ways (21.3 frames, 333 ms).</summary>
    public const double FadePerFrame = 0.0469;

    /// <summary>A drag held still and let go short of half an item: it drifts on 9.1 to 9.5 px over 7 or 8 frames, then goes back.</summary>
    public const double DriftMin = 8.4;

    /// <inheritdoc cref="DriftMin"/>
    public const double DriftMax = 10.6;

    /// <summary>
    /// The 1904 run's drag held still and let go 0.43 of an item past: the offset from the item it goes back to, from
    /// the first inertial frame, every frame to rest (500 ms).
    /// </summary>
    public static readonly (double Ms, double Offset)[] BackFrames =
    [
        (0, 63.4), (16, 64.7), (31, 66.0), (62, 68.5), (78, 69.7), (94, 70.7), (109, 71.9), (125, 72.5), (141, 71.1),
        (156, 68.9), (172, 65.7), (188, 61.6), (203, 57.7), (219, 53.3), (234, 48.8), (250, 44.0), (266, 39.8),
        (281, 35.1), (297, 31.1), (312, 26.9), (328, 22.9), (344, 19.2), (359, 16.1), (375, 13.0), (391, 10.1),
        (406, 7.7), (422, 5.5), (438, 3.7), (453, 2.2), (469, 1.1), (484, 0.4), (500, 0.0),
    ];

    /// <summary>The 1904 run's drag held still and let go 0.52 of an item past: the offset short of the next item, to rest.</summary>
    public static readonly (double Ms, double Offset)[] OnFrames =
    [
        (0, -61.5), (16, -58.0), (31, -54.1), (47, -49.8), (62, -45.8), (78, -41.4), (109, -33.1), (156, -21.9),
        (172, -18.3), (188, -15.1), (203, -12.3), (219, -9.7), (234, -7.4), (250, -5.3), (266, -3.6), (281, -2.2),
        (297, -1.2), (312, -0.4), (328, -0.1),
    ];

    /// <summary>
    /// The 1904 run's flick down, 300 px in 120 ms: 14 items in 1,844 ms; the offset short of where it ends, every
    /// 125 ms or so from the first inertial frame.
    /// </summary>
    public static readonly (double Ms, double Offset)[] FlickFrames =
    [
        (0, 1686.6), (125, 1379.0), (250, 1125.8), (375, 916.2), (500, 739.6), (625, 589.6), (750, 461.5), (859, 365.8),
        (1000, 262.2), (1125, 187.1), (1250, 125.8), (1375, 77.8), (1500, 41.8), (1625, 17.3), (1750, 3.6), (1844, 0.0),
    ];
}
