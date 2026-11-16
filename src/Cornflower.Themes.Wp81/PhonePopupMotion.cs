using Avalonia;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// How a phone popup moves as it opens and closes: the one place its motion lives, for <see cref="PhonePopupHost"/> and
/// every popup that copies the Flyout's (docs/controls/Flyout/README.md, "What the popups share").
/// </summary>
/// <remarks>
/// <para>As #149 read it (tools/refapp/README.md, "The popups' bands, and the inline ComboBox", run
/// <c>20261116-1835-dev11</c>, the stills in <c>ref/wp81/Flyout/motion/</c>). The shell cuts a popup into horizontal
/// bands and turns each about its own horizontal centre line, which stays on the screen's plane, seen by one camera for
/// the whole screen, <see cref="EyeDistance"/> in front of it, over the window's centre (y 400 on the reference). An
/// angle is positive when the band's lower edge is nearer the eye. Every band's turn is linear in animation time (the
/// phone shows it one step a tick of its 64 Hz clock, <see cref="Tick"/>; ours at whatever frames come).</para>
/// <list type="bullet">
/// <item>The <b>top</b> popup is one band. Opening, it turns from <see cref="OpenFrom"/> to 0 at
/// <see cref="TopOpenRate"/> a tick (134 ms). Closing, it doesn't turn: it shows unchanged, is gone in one frame
/// <see cref="TopGoneAfter"/> after it is asked to close (98 to 122 ms read), and is closed at
/// <see cref="TopClosedAfter"/>.</item>
/// <item>The <b>full</b> popup is <see cref="BandHeight"/> px bands. Opening, every band starts at
/// <see cref="OpenFrom"/> at once and goes to 0 at its own rate, the lower ones slower (<see cref="FullOpenRates"/>);
/// from the fifth band (400 px) down nothing turns: it is revealed at rest, downwards (<see cref="Reveal"/>). Closing, band k starts <see cref="CloseStagger"/> × k ticks
/// after the call and turns the other way (the lower edge going back) at <see cref="CloseRate"/> a tick until it is
/// edge-on at <see cref="CloseTo"/>, gone; the popup is closed when its last band is (<see cref="ClosedAfter"/>, 22
/// ticks).</item>
/// </list>
/// <para>Time 0 is the motion's first frame, which shows the start angle, as the phone's is the call's frame.</para>
/// </remarks>
internal static class PhonePopupMotion
{
    /// <summary>One tick of the phone's animation clock: 64 a second.</summary>
    internal static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(15.625);

    /// <summary>The camera's distance in front of the screen, in view px: the tilt's, the projections probe's 999.</summary>
    internal const double EyeDistance = TiltModel.EyeDistance;

    /// <summary>A full popup's band: 100 px slices from its top.</summary>
    internal const double BandHeight = 100;

    /// <summary>Every opening band's first angle, in degrees, the lower edge toward the eye.</summary>
    internal const double OpenFrom = 80;

    /// <summary>The top popup's opening, degrees a tick: 80° in 8.6 ticks, 134 ms.</summary>
    internal const double TopOpenRate = 9.31;

    /// <summary>
    /// The full popup's opening bands' rates, degrees a tick, from the top: 12.61, 9.57 and 7.58 read for bands 0 to
    /// 2. Band 3's isn't read: each band down takes about 2.1 ticks more than the one above (6.34, 8.36, 10.55), so its
    /// 12.65 ticks. Bands past these don't turn: below 400 px the popup is revealed at rest (<see cref="Reveal"/>).
    /// </summary>
    internal static readonly double[] FullOpenRates = [12.61, 9.57, 7.58, OpenFrom / 12.65];

    /// <summary>
    /// The full popup's opening below its turning bands: drawn at rest, a piece at a time, downwards (#150's reading of
    /// #149's recording, call 1): nothing below 400 px, then down to 428, 551 and 673 px at 119, 144 and 206 ms, and all
    /// of it at 269 ms.
    /// </summary>
    internal static readonly (TimeSpan At, double To)[] Reveal =
    [
        (TimeSpan.FromMilliseconds(119), 428),
        (TimeSpan.FromMilliseconds(144), 551),
        (TimeSpan.FromMilliseconds(206), 673),
        (TimeSpan.FromMilliseconds(269), double.PositiveInfinity),
    ];

    /// <summary>Where the full popup's still part is drawn down to, <paramref name="time"/> into the opening.</summary>
    internal static double RevealedTo(TimeSpan time)
    {
        var to = FullOpenRates.Length * BandHeight;
        foreach (var (at, end) in Reveal)
        {
            if (time >= at)
                to = end;
        }

        return to;
    }

    /// <summary>A closing band's rate, degrees a tick, the lower edge going back.</summary>
    internal const double CloseRate = 10.95;

    /// <summary>A closing band is gone at this angle, edge-on.</summary>
    internal const double CloseTo = 90;

    /// <summary>The ticks between one closing band's start and the next one down's.</summary>
    internal const double CloseStagger = 2;

    /// <summary>The top popup is gone this long after it is asked to close: the middle of the read 98 to 122 ms.</summary>
    internal static readonly TimeSpan TopGoneAfter = TimeSpan.FromMilliseconds(110);

    /// <summary>The top popup is closed (<c>Closed</c>) 9 ticks after it is asked to close.</summary>
    internal static readonly TimeSpan TopClosedAfter = Tick * 9;

    /// <summary>
    /// A full popup of <paramref name="bands"/> bands is closed (<c>Closed</c>) on the last whole tick before its last
    /// band is gone: 22 ticks for the reference's 8 (14 + 8.2), as read.
    /// </summary>
    internal static TimeSpan ClosedAfter(int bands) =>
        Tick * Math.Floor(CloseStagger * Math.Max(0, bands - 1) + CloseTo / CloseRate);

    /// <summary>Ticks of the phone's clock in <paramref name="time"/>.</summary>
    internal static double Ticks(TimeSpan time) => time / Tick;

    /// <summary>How long a popup placed so takes to open: until its slowest band is at rest.</summary>
    internal static TimeSpan OpenLength(PhonePopupPlacement placement) => placement == PhonePopupPlacement.Full
        ? TimeSpan.FromTicks(Math.Max((Tick * (OpenFrom / FullOpenRates.Min())).Ticks, Reveal[^1].At.Ticks))
        : Tick * (OpenFrom / TopOpenRate);

    /// <summary>Band <paramref name="band"/>'s angle <paramref name="time"/> into the opening, in degrees: 0 at rest.</summary>
    internal static double OpeningAngle(PhonePopupPlacement placement, int band, TimeSpan time)
    {
        if (placement == PhonePopupPlacement.Full && band >= FullOpenRates.Length)
            return 0;
        var rate = placement == PhonePopupPlacement.Top ? TopOpenRate : FullOpenRates[band];
        return Math.Max(0, OpenFrom - rate * Ticks(time));
    }

    /// <summary>
    /// Band <paramref name="band"/>'s angle <paramref name="time"/> into a full popup's closing, from
    /// <paramref name="from"/>, its angle when it was asked to close (0 at rest; mid-opening, wherever it had got to).
    /// At <see cref="CloseTo"/> back, it is gone.
    /// </summary>
    internal static double ClosingAngle(int band, double from, TimeSpan time) =>
        Math.Max(-CloseTo, from - CloseRate * Math.Max(0, Ticks(time) - CloseStagger * band));

    /// <summary>
    /// Where a point of a band is drawn, the band turned <paramref name="degrees"/> about its horizontal centre line at
    /// <paramref name="centreY"/> (positive brings the lower edge nearer the eye), seen from the camera over
    /// <paramref name="eye"/>, all in the popup's own space: a perspective matrix, the centre line fixed.
    /// </summary>
    internal static Matrix Band(double degrees, double centreY, Point eye)
    {
        var r = degrees * Math.PI / 180;
        double cos = Math.Cos(r), k = Math.Sin(r) / EyeDistance, c = centreY;
        // A point (x, y), u = y − c from the line, turned is at (x, c + u cos, u sin), and is drawn at
        // eye + (P − eye) × d / (d − Z). With W = 1 − u sin / d, the drawn point times W is linear in x and y.
        return new Matrix(
            1, 0, 0,
            -eye.X * k, cos - eye.Y * k, -k,
            eye.X * c * k, c * (1 - cos) + eye.Y * c * k, 1 + c * k);
    }
}
