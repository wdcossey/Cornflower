namespace Cornflower.Tests.Controls;

/// <summary>
/// What the emulator showed of the page and its transitions, on the reference profile (device 11, 450 × 800 view px),
/// as <c>tools/refapp/README.md</c> has it. docs/controls/Page/PLAN.md names each.
/// </summary>
internal static class PageReadings
{
    /// <summary>One frame of the emulator's animation clock: 64 Hz.</summary>
    public const double Tick = 15.625;

    /// <summary>
    /// <c>-Native</c>, <c>20261111-1759-dev11</c> and <c>20261115-2050-dev11</c>: <c>VisibleBounds</c> with the tray shown
    /// starts 27.92 down; with a compact app bar it ends at 737.08, with a minimal one at 773.75. The page fills the
    /// window in every reading; under <c>UseVisible</c> its content is laid out in <c>VisibleBounds</c>, under
    /// <c>UseCoreWindow</c> it fills the window.
    /// </summary>
    public static class Bounds
    {
        public const double TrayHeight = 27.92;
        public const double CompactBarTop = 737.08;
        public const double MinimalBarTop = 773.75;
    }

    /// <summary>
    /// The turnstile, <c>20261110-0748-dev11</c>, <c>20261110-0810-dev11</c> and <c>20261115-2053-dev11</c>
    /// (<c>tools/motion/turnstile.py</c>): only the page coming in moves, about a vertical axis 44 px left of its left edge
    /// and 99 px behind the screen, seen from 1,000 px over the page's centre (225, 400). Forward, from −67.10° over 425 ms;
    /// back, from +41.97° over 430 ms; both an <c>ExponentialEase</c> with exponent 6, easing out, starting on the first
    /// frame shown.
    /// </summary>
    public static class Turnstile
    {
        public const double AxisX = -44;
        public const double AxisZ = -99;
        public const double Eye = 1000;
        public const double ForwardFrom = -67.10;
        public const double ForwardMs = 425;
        public const double BackFrom = 41.97;
        public const double BackMs = 430;
        public const double Exponent = 6;

        /// <summary>The forward turns' first frames: ticks 0, 1 and 2 of one ladder (−67.0 in 4 runs, −53.8 in 25, −43.1 in 1).</summary>
        public static readonly double[] ForwardFirstTicks = [-67.0, -53.8, -43.1];

        /// <summary>The back turn's first frame.</summary>
        public const double BackFirst = 42.0;

        /// <summary>Within 1° of rest after about 300 ms.</summary>
        public const double WithinADegreeMs = 300;
    }

    /// <summary>
    /// The slide, <c>20261110-0810-dev11</c> (<c>tools/motion/slide.py</c>, run on 2026-11-15): forward, the new page's
    /// top below its place, its distinct offsets on ticks 0, 1, 3, 4 and on; back, the page going out, its offsets read
    /// backwards from the last seen, a tick apart, the last one 12 ticks after its first move.
    /// </summary>
    public static class Slide
    {
        public static readonly (int Tick, double Offset)[] Forward =
            [(0, 200.0), (1, 152.8), (3, 89.2), (4, 68.2), (5, 52.0), (6, 39.7), (7, 30.2), (8, 23.1), (9, 17.5), (10, 13.3)];

        /// <summary>The back slide's offsets, from its last frame (tick 12 of the slide) back.</summary>
        public static readonly double[] BackFromLast = [137.3, 94.2, 64.6, 44.2, 30.2, 20.7, 14.1, 9.4, 6.4, 4.2, 2.7];

        public const int BackLastTick = 12;
    }

    /// <summary>
    /// The continuum, <c>20261110-0810-dev11</c>, by the recording (frames about 31 ms apart): forward, the old page fades
    /// in one or two recorded frames, then the new page grows from 0.941 to 0.963 at its first frame to 1 in 281 to
    /// 312 ms; back, after the slide out, the page under it fades in, first seen at 1 − 153/255 = 0.40, over about 344 ms.
    /// </summary>
    public static class Continuum
    {
        public const double FirstScaleLow = 0.9405;
        public const double FirstScaleHigh = 0.9626;
        public const double GrownByMsLow = 281;
        public const double GrownByMsHigh = 312;
        public const double FadeFramesMs = 2 * 31.25;
        public const double BackFirstOpacity = 1 - 153.0 / 255;
        public const double BackFadeMs = 344;
    }
}
