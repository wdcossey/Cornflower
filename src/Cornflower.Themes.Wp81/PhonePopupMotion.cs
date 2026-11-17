using Avalonia;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// A full popup's bands: <paramref name="Height"/> px rows from its top, the top ones turning as it opens at
/// <paramref name="Rates"/> (degrees a tick, from the top), and below them the popup revealed at rest, downwards, as
/// <paramref name="Reveal"/> has it (none: all of it at once). The Flyout's are <see cref="PhonePopupMotion.FlyoutRows"/>.
/// </summary>
internal sealed record PhonePopupRows(double Height, double[] Rates, (TimeSpan At, double To)[] Reveal);

/// <summary>
/// A top popup's motion, its one band's: <paramref name="Angle"/>, the band's angle a time into the opening, at rest
/// after <paramref name="OpenLength"/>; closing, it shows as it is, is gone in one frame <paramref name="GoneAfter"/>
/// after it is asked to close, and is closed at <paramref name="ClosedAfter"/> (at once as it goes, if no later). The
/// Flyout's is <see cref="PhonePopupMotion.FlyoutSwing"/>, the ContentDialog's <see cref="PhonePopupMotion.DialogSwing"/>.
/// </summary>
internal sealed record PhonePopupSwing(Func<TimeSpan, double> Angle, TimeSpan OpenLength, TimeSpan GoneAfter, TimeSpan ClosedAfter);

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
/// <para>A popup with bands of its own gives its host its rows (<see cref="PhonePopupRows"/>: the list pickers'
/// <see cref="ListPickerRows"/>) or its swing (<see cref="PhonePopupSwing"/>: the ContentDialog's
/// <see cref="DialogSwing"/>); its closing bands turn as the Flyout's do.</para>
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

    /// <summary>The full Flyout's rows, and the PickerFlyout's (#149: the same).</summary>
    internal static readonly PhonePopupRows FlyoutRows = new(BandHeight, FullOpenRates, Reveal);

    /// <summary>
    /// The list pickers' opening rates, degrees a tick, rows 0 to 4 from the top (#149: the header at 12.75 and the
    /// first item at 9.63, then about 8 to 6; docs/controls/ListPickerFlyout/README.md has the 12.5, 9.6, 7.8, 6.6 and
    /// 5.7 its reading gives). Rows past these don't turn: they are revealed at rest.
    /// </summary>
    internal static readonly double[] ListPickerOpenRates = [12.5, 9.6, 7.8, 6.6, 5.7];

    /// <summary>
    /// The ListPickerFlyout's rows: the header and each item, 66.7 px (800 / 12), the top five turning at
    /// <see cref="ListPickerOpenRates"/>, the rest revealed downwards by the Flyout's steps, scaled. Twelve on the
    /// reference, so <c>Closed</c> comes at <see cref="ClosedAfter"/>(12), 30 ticks, as #149 read it.
    /// </summary>
    internal static readonly PhonePopupRows ListPickerRows = new(800.0 / 12, ListPickerOpenRates, Reveal);

    /// <summary>
    /// Where a full popup of <paramref name="height"/> is drawn down to below its turning rows, <paramref name="time"/>
    /// into the opening: the reading's rows on the reference's 800, below the Flyout's turning 400 px, scaled to the
    /// popup's own still part. With no reveal, all of it.
    /// </summary>
    internal static double RevealedTo(TimeSpan time, double height, PhonePopupRows rows)
    {
        const double reference = 800;
        var read = FullOpenRates.Length * BandHeight;
        var still = rows.Rates.Length * rows.Height;
        var to = rows.Reveal.Length > 0 ? still : double.PositiveInfinity;
        foreach (var (at, end) in rows.Reveal)
        {
            if (time >= at)
                to = double.IsPositiveInfinity(end) ? end : still + (end - read) * (height - still) / (reference - read);
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

    /// <summary>How long a full popup of these rows takes to open: until its slowest row is at rest and its last piece revealed.</summary>
    internal static TimeSpan OpenLength(PhonePopupRows rows) => TimeSpan.FromTicks(Math.Max(
        (Tick * (OpenFrom / rows.Rates.Min())).Ticks, rows.Reveal.Length > 0 ? rows.Reveal[^1].At.Ticks : 0));

    /// <summary>A full popup's row <paramref name="band"/>'s angle <paramref name="time"/> into the opening, in degrees: 0 at rest.</summary>
    internal static double OpeningAngle(PhonePopupRows rows, int band, TimeSpan time) =>
        band >= rows.Rates.Length ? 0 : Math.Max(0, OpenFrom - rows.Rates[band] * Ticks(time));

    /// <summary>The top Flyout's angle <paramref name="time"/> into its opening: from <see cref="OpenFrom"/> at <see cref="TopOpenRate"/> a tick.</summary>
    internal static double TopOpeningAngle(TimeSpan time) => Math.Max(0, OpenFrom - TopOpenRate * Ticks(time));

    /// <summary>The top Flyout's swing: 134 ms in, gone at 110 ms and closed at 140.6.</summary>
    internal static readonly PhonePopupSwing FlyoutSwing =
        new(TopOpeningAngle, Tick * (OpenFrom / TopOpenRate), TopGoneAfter, TopClosedAfter);

    /// <summary>The ContentDialog's first angle, in degrees, the lower edge toward the eye.</summary>
    internal const double DialogOpenFrom = 45;

    /// <summary>The ContentDialog's opening ease, WinRT's ExponentialEase out.</summary>
    internal const double DialogOpenExponent = 1.68;

    /// <summary>The ContentDialog's opening is at rest 9 ticks after its first frame: 140.6 ms.</summary>
    internal static readonly TimeSpan DialogOpenLength = Tick * 9;

    /// <summary>The ContentDialog is gone, and closed, this long after it is asked to close: the middle of the read 95 to 120 ms.</summary>
    internal static readonly TimeSpan DialogGoneAfter = TimeSpan.FromMilliseconds(107.5);

    /// <summary>The ContentDialog's angle <paramref name="time"/> into its opening, in degrees: 0 at rest.</summary>
    internal static double DialogOpeningAngle(TimeSpan time) =>
        time >= DialogOpenLength ? 0 : PhoneEase.Remaining(DialogOpenFrom, DialogOpenExponent, time, DialogOpenLength);

    /// <summary>
    /// The ContentDialog's swing (#149, run <c>20261116-1835-dev11</c>): one band, the dialog, from
    /// <see cref="DialogOpenFrom"/> to 0 on WinRT's ExponentialEase, out, exponent <see cref="DialogOpenExponent"/>, at
    /// rest at tick 9; closing, gone in one frame <see cref="DialogGoneAfter"/> later, the dim layer with it, as it closes.
    /// </summary>
    internal static readonly PhonePopupSwing DialogSwing = new(DialogOpeningAngle, DialogOpenLength, DialogGoneAfter, DialogGoneAfter);

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
