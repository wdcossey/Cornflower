using Avalonia;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// How a phone popup moves as it opens and closes: the one place its curve lives, for <see cref="PhonePopupHost"/> and
/// every popup that copies the Flyout's (docs/controls/Flyout/README.md, "What the popups share").
/// </summary>
/// <remarks>
/// <para><b>What is known</b> (tools/refapp/README.md, "Flyouts, pickers and the dialog", run <c>20261110-0819-dev11</c>,
/// the recording's clock, ±25 ms): the top Flyout swings down from its top edge, its bottom edge at 46, 55, 69, then 77
/// of 78 px on successive recorded frames, and is 50%, 90% and 99% of the way there 50, 100 and 142 ms after its first
/// changed frame; it closes in one frame, about 100 ms after <c>Hide()</c>. The full Flyout sweeps in top to bottom
/// (its top 100 px band half there at 74 ms, its bottom band at 208), 141, 208 and 208 ms; it sweeps out top to bottom
/// (117 to 275 ms), 200, 291 and 334 ms. Those times are the lengths below.</para>
/// <para><b>PENDING #149: the curves.</b> The recording gives times, not curves. Until #149 reads them per frame, the
/// angle's start (<see cref="OpenFrom"/>, <see cref="CloseTo"/>) and ease (<see cref="OpenExponent"/>,
/// <see cref="CloseExponent"/>) are placeholders, not fits, and the full Flyout's close swings about its top edge as
/// its opening does, though the recording's "top to bottom" doesn't fit a top hinge (its top band would change last).
/// Drop #149's numbers in here; nothing else holds them.</para>
/// <para>The swing is a turn about the popup's top edge, the bottom going away from the eye, seen from a camera
/// <see cref="EyeDistance"/> in front of the screen over the popup's centre, as the phone's <c>PlaneProjection</c> was
/// measured (tools/refapp/README.md, "Motion": the camera). Time 0 is the motion's first frame, as with the app bar's.
/// The recording's milliseconds are taken as this clock's (ADR 0005: whether the recording runs with the phone's
/// stopwatch or with its animation time is open, up to 4%).</para>
/// </remarks>
internal static class PhonePopupMotion
{
    /// <summary>The camera's distance in front of the screen, in view px (the projections probe, 999).</summary>
    internal const double EyeDistance = 999;

    /// <summary>The top popup's opening: at rest by 150 ms (99% of the way at 142 ms, ±25).</summary>
    internal static readonly TimeSpan TopOpenLength = TimeSpan.FromMilliseconds(150);

    /// <summary>The full popup's opening: at rest by 208 ms (99% of the way at 208 ms).</summary>
    internal static readonly TimeSpan FullOpenLength = TimeSpan.FromMilliseconds(208);

    /// <summary>The top popup's closing: it stays at rest this long after it is asked to close, then goes in one frame.</summary>
    internal static readonly TimeSpan TopCloseDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>The full popup's closing: gone 334 ms after it starts (99% of the way at 334 ms).</summary>
    internal static readonly TimeSpan FullCloseLength = TimeSpan.FromMilliseconds(334);

    /// <summary>PENDING #149. The angle the opening swings from, in degrees: a placeholder.</summary>
    internal const double OpenFrom = 60;

    /// <summary>PENDING #149. The opening's ease, WinRT's <c>ExponentialEase</c> out: a placeholder.</summary>
    internal const double OpenExponent = 3;

    /// <summary>PENDING #149. The angle the full popup's closing swings to, in degrees: a placeholder.</summary>
    internal const double CloseTo = 60;

    /// <summary>PENDING #149. The full popup's closing ease, WinRT's <c>ExponentialEase</c> in: a placeholder.</summary>
    internal const double CloseExponent = 3;

    /// <summary>How long a popup placed so takes to open.</summary>
    internal static TimeSpan OpenLength(PhonePopupPlacement placement) =>
        placement == PhonePopupPlacement.Full ? FullOpenLength : TopOpenLength;

    /// <summary>The angle <paramref name="time"/> into the opening, in degrees: 0 at rest.</summary>
    internal static double OpeningAngle(PhonePopupPlacement placement, TimeSpan time) =>
        PhoneEase.Remaining(OpenFrom, OpenExponent, time, OpenLength(placement));

    /// <summary>
    /// The angle <paramref name="time"/> into the full popup's closing, in degrees, from <paramref name="from"/>: the
    /// angle it was at when asked to close (0 at rest; mid-opening, wherever the opening had got to).
    /// </summary>
    internal static double ClosingAngle(double from, TimeSpan time) =>
        from + (CloseTo - from) * PhoneEase.ExponentialIn(CloseExponent, time / FullCloseLength);

    /// <summary>
    /// Where a point of a popup of <paramref name="size"/> is drawn, turned <paramref name="degrees"/> about its top edge
    /// (positive takes the bottom away from the eye) and seen from the camera over its centre: a perspective matrix on
    /// the popup's own space, its top edge fixed.
    /// </summary>
    internal static Matrix Swing(double degrees, Size size)
    {
        var r = degrees * Math.PI / 180;
        double cos = Math.Cos(r), sin = Math.Sin(r), d = EyeDistance;
        double ex = size.Width / 2, ey = size.Height / 2;
        // A point (x, y) turned is at (x, y cos, −y sin), and is drawn at eye + (P − eye) × d / (d − Z). With
        // W = 1 + y sin / d, the drawn point times W is (x + ex y sin / d, y cos + ey y sin / d): linear in x and y.
        return new Matrix(
            1, 0, 0,
            ex * sin / d, cos + ey * sin / d, sin / d,
            0, 0, 1);
    }
}
