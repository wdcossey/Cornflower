namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Hub as the emulator showed it, written out for the tests (the dumps and logs stay outside the repository,
/// ADR 0002). docs/controls/Hub/PLAN.md quotes each with its source.
/// </summary>
internal static class HubReadings
{
    // The probes' Hub (#128 and #137; tools/refapp/app/Probe.Shell.cs): the header "hub" and four sections "section 1" to
    // "section 4", each a 30 px TextBlock, below a 60 px band, so the Hub is 450 x 740 in the 450 x 800 view. Layout in
    // view pixels from the Hub's top left (run 20261116-0009-dev11's layout lines, after the entrance).
    public const double ProbeTop = 60;
    public const double ProbeHeight = 740;
    public const double HeaderLeft = 15;
    public const double HeaderTop = 1;
    public const double HeaderHeight = 103.75;
    public const double SectionWidth = 405;

    /// <summary>The section's width on the other two profiles' catalogues (#137's dumps): the Hub's width, then the section's.</summary>
    public static readonly (double Hub, double Section)[] SectionWidths = [(400, 360), (450, 405), (490.9091, 441.8182)];

    /// <summary>The section's template, from its style and its keys: the inner Grid's margin, the header's margin.</summary>
    public const double SectionInset = 19;
    public const double SectionHeaderLeftMargin = -1;
    public const double SectionHeaderTopMargin = 5;
    public const double SectionHeaderBottomMargin = 31.5;

    /// <summary>The recording's frames at rest (run 20261115-2059-dev11, 1.2 window px per view px): ink, in view px from the Hub.</summary>
    public const double PeekHeaderInkLeft = 424.17;

    /// <summary>The header's speed against the sections': 1/18 (#128, worst 0.0005 px over 270 frames).</summary>
    public const double HeaderRatio = 1.0 / 18;

    // The background image (#137, runs 20261116-0009-dev11 and -0025- at 1600 x 800 px, -0001- at 900 x 400): drawn at
    // its pixel width by the window's height, its left edge against the offset.
    public const double ViewHeight = 800;
    public const double Rate1600 = 0.982906;
    public const double Rate900 = 0.384615;

    /// <summary>The background's left edge at offsets the phone logged (1600 px image, run 20261116-0009-dev11).</summary>
    public static readonly (double Offset, double Left)[] Background1600 =
        [(22.103, -21.725), (205.045, -201.54), (603.562, -593.245), (810, -796.154), (1215.417, -1195.316)];

    /// <summary>The same, 900 px image (run 20261116-0001-dev11).</summary>
    public static readonly (double Offset, double Left)[] Background900 = [(599.066, -230.41), (810, -311.538), (1215.417, -495.32)];

    /// <summary>The copy flips from W − 1 left to W − 1 right between these offsets (three runs; (L − V) / 2 is 585).</summary>
    public const double WrapFlipAbove = 582.872;
    public const double WrapFlipBelow = 586.321;

    // The motion. Every per-frame value is on the emulator's RenderingTime, 15.625 ms a frame.
    public const double Frame = 15.625;

    /// <summary>The probes' swipe: 15 moves of 13.333 px, 10 ms apart, from x 380 (left) or 70 (right), y 500.</summary>
    public const int SwipeMoves = 15;
    public const double SwipeStep = 200.0 / 15;
    public const double SwipeMoveMs = 10;

    /// <summary>
    /// A swipe's offset after its release, frame by frame, as a fraction of the way from where the finger left it to
    /// the next section's start: the median of 20 swipes (15 left, 5 right; runs 20261115-2059-dev11, 20261116-0001-,
    /// -0007-, -0009-, -0025-). Frame 1 is the first after the release.
    /// </summary>
    public static readonly double[] SwipeFractions =
    [
        0.1025, 0.1907, 0.2725, 0.3461, 0.4162, 0.4782, 0.5340, 0.5847, 0.6295, 0.6689, 0.7056, 0.7371, 0.7658, 0.7916,
        0.8143, 0.8348, 0.8527, 0.8692, 0.8837, 0.8966, 0.9083, 0.9188, 0.9282, 0.9367, 0.9440, 0.9508, 0.9568, 0.9621,
        0.9670, 0.9712, 0.9751, 0.9785, 0.9815, 0.9842, 0.9865, 0.9886, 0.9904, 0.9920, 0.9934, 0.9946, 0.9956, 0.9965,
        0.9972, 0.9979, 0.9984, 0.9988, 0.9992, 0.9995,
    ];

    /// <summary>A drag of 150 px held 300 ms and let go goes back: the median of 5, the same way (frame 1 after the release).</summary>
    public static readonly double[] ReturnFractions =
    [
        0.1155, 0.2182, 0.3102, 0.3701, 0.4454, 0.5045, 0.5577, 0.6103, 0.6510, 0.6890, 0.7211, 0.7519, 0.7785, 0.8030,
        0.8246, 0.8434, 0.8617, 0.8768, 0.8909, 0.9023, 0.9135, 0.9236, 0.9324, 0.9401, 0.9473, 0.9538, 0.9594, 0.9644,
        0.9689, 0.9731, 0.9768, 0.9799, 0.9827, 0.9852, 0.9875, 0.9894, 0.9912, 0.9927, 0.9940, 0.9951, 0.9961, 0.9968,
        0.9976, 0.9981,
    ];

    /// <summary>
    /// A swipe left at the end: the sections' way from where the finger left them to the first section's place after the
    /// last (L), frame by frame from the release, the median of 4 loops (runs 20261116-0001-, -0007-, -0009-,
    /// -0025-dev11). On frame 19 the offset is 0 again; the phone stops 1.42 px short of L.
    /// </summary>
    public static readonly double[] LoopFractions =
    [
        0.2753, 0.4702, 0.6170, 0.7259, 0.8020, 0.8583, 0.8978, 0.9266, 0.9474, 0.9630, 0.9738, 0.9813, 0.9864, 0.9901,
        0.9923, 0.9936, 0.9942, 0.9945,
    ];

    /// <summary>The frame on which the offset is 0 again, after the loop's release (all four).</summary>
    public const int LoopResetFrame = 19;

    /// <summary>The header after the loop's release: held on frames 0 to 2, then 16.04 px further left each frame.</summary>
    public const int HeaderHoldFrames = 2;
    public const double HeaderFlyOutPerFrame = 16.04;

    /// <summary>The header jumps on frame 6 after the release (three loops; one on 7), to 433 to 459, and eases in.</summary>
    public const int HeaderJumpFrame = 6;
    public const double HeaderJumpMin = 433;
    public const double HeaderJumpMax = 465;

    /// <summary>The fly-in's frame-to-frame ratio of the way left to go, early on: 0.7699 to 0.7708 (four loops).</summary>
    public const double FlyInRatioMin = 0.7695;
    public const double FlyInRatioMax = 0.7712;

    /// <summary>The header is at its place 27 frames (421.875 ms) after the jump.</summary>
    public const int FlyInFrames = 27;
}
