namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Pivot as the emulator showed it, written out for the tests (the dumps and logs stay outside the repository,
/// ADR 0002). docs/controls/Pivot/PLAN.md quotes each with its source.
/// </summary>
internal static class PivotReadings
{
    // The catalogue's capture (run 20261108-1443-dev11, Pivot two-items, dark and light): a Pivot titled "CORNFLOWER",
    // items "one" and "two", full width on a 450 x 320 stage. Bounds in view pixels from the Pivot's top left.
    public const double StageWidth = 450;
    public const double StageHeight = 320;
    public const double TitleLeft = 19.1667;
    public const double TitleTop = 37.9167;
    public const double TitleHeight = 24.1667;
    public const double ScrollViewerTop = 62.0833;
    public const double HeaderPanelHeight = 75.8333;
    public const double HeaderLeft = 15.8438;
    public const double HeaderTop = 55.4167;
    public const double HeaderHeight = 82.5;
    public const double HeaderTextHeight = 75.8133;
    public const double ItemsPresenterTop = 137.9167;
    public const double ItemLeft = 19.1719;
    public const double ItemTop = 160.4167;
    public const double ItemWidth = 412.0833;
    public const double TitleLandscapeTop = 25;

    /// <summary>The gap from a header's right edge to the next's left at the reference profile: 16 on the raw grid (#128).</summary>
    public const double HeaderGap = 15.8333;

    // The headers' colours (the dumps' foregrounds).
    public const uint DarkSelected = 0xFFFFFFFF;
    public const uint DarkUnselected = 0x66FFFFFF;
    public const uint LightSelected = 0xFF000000;
    public const uint LightUnselected = 0x66000000;

    // The motion (#128, runs 20261115-2056-dev11 and 20261115-2136-dev11; #136, 20261115-2339-dev11 and -2343-dev11).
    /// <summary>The emulator's frame.</summary>
    public const double Frame = 15.625;

    /// <summary>The probes' swipe: 15 moves of 13.333 px, 10 ms apart, from x 380 (left) or 70 (right), y 450.</summary>
    public const int SwipeMoves = 15;
    public const double SwipeStep = 200.0 / 15;
    public const double SwipeMoveMs = 10;

    /// <summary>Selection after the press, a swipe left (#128: 188 to 241 ms; #136: 200 to 235) and right (152 to 174).</summary>
    public const double SelectionLeftMin = 188;
    public const double SelectionLeftMax = 241;
    public const double SelectionRightMin = 152;
    public const double SelectionRightMax = 174;

    /// <summary>A swipe's offset settles this long after the first frame after the press (#128: 609 to 672 ms).</summary>
    public const double SwipeSettleMin = 609;
    public const double SwipeSettleMax = 672;

    /// <summary>A tap's motion settles this long after the release's frame (#128 and #136: 516 to 531 ms).</summary>
    public const double TapSettleMin = 515.625;
    public const double TapSettleMax = 531.25;

    /// <summary>
    /// The new item's slide in: its distance still to go, as a fraction of a page, on the frames after the one it comes
    /// in on at a page: ExponentialEase 4 over 400 ms, the same on every swipe, wrap and tap (#128, to four places).
    /// </summary>
    public static readonly double[] SlideIn = [1, 0.8526, 0.7266, 0.6188, 0.5266];

    /// <summary>
    /// The old item's slide out after a swipe's release, beyond the offset, as a fraction of a page, frame by frame from
    /// the release's frame (run 20261115-2136-dev11, swipe 1: the items' translate 0, 0, -66.31, -123.03, -171.53).
    /// </summary>
    public static readonly double[] SlideOut = [0, 0, 0.1474, 0.2734, 0.3812];

    /// <summary>
    /// The swipe's offset after its release, the median of 20 swipes' fractions still to go, one a frame from the frame
    /// after the release (runs 20261115-2136-dev11, -2339-, -2343-; scratch fit in the PR).
    /// </summary>
    public static readonly double[] SwipeToGo =
    [
        0.9171, 0.8383, 0.7524, 0.6656, 0.5823, 0.5001, 0.4268, 0.3666, 0.3088, 0.2636, 0.2240, 0.1919, 0.1637, 0.1404,
        0.1203, 0.1027, 0.0877, 0.0740, 0.0626, 0.0526, 0.0439, 0.0363, 0.0298, 0.0242, 0.0193, 0.0151, 0.0114, 0.0085,
        0.0060, 0.0041, 0.0025, 0.0014, 0.0006,
    ];

    /// <summary>A tap's offset, the median of 6 taps' fractions still to go, from the second frame after the release's.</summary>
    public static readonly double[] TapToGo =
    [
        0.9266, 0.8422, 0.7334, 0.6143, 0.5190, 0.4461, 0.3823, 0.3309, 0.2864, 0.2490, 0.2151, 0.1868, 0.1611, 0.1388,
        0.1190, 0.1010, 0.0859, 0.0719, 0.0598, 0.0494, 0.0395, 0.0316, 0.0247, 0.0187, 0.0136, 0.0094, 0.0061, 0.0035,
        0.0016, 0.0005, 0,
    ];

    /// <summary>A drag of 109.05 px held and let go, its offset still to go, from the frame after the release (run 20261115-2136-dev11).</summary>
    public static readonly double[] ReturnToGo =
    [
        0.9953, 0.9792, 0.9507, 0.9135, 0.8569, 0.7911, 0.7232, 0.6510, 0.5793, 0.5177, 0.4584, 0.4039, 0.3534, 0.3128,
        0.2728, 0.2402, 0.2082, 0.1785, 0.1552, 0.1327, 0.1119, 0.0949, 0.0782, 0.0628, 0.0517, 0.0413, 0.0318, 0.0235,
        0.0172, 0.0113, 0.0075, 0.0040, 0.0017, 0.0004, 0,
    ];
}
