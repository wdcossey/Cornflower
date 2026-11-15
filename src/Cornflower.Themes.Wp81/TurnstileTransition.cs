using Avalonia;
using Avalonia.Animation;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Windows Phone 8.1 turnstile: the page transition a <c>Frame</c> runs by default, and with
/// <c>CommonNavigationTransitionInfo</c>. <c>Wp81Theme</c> sets it as every <see cref="Avalonia.Controls.NavigationPage"/>'s
/// <see cref="Avalonia.Controls.NavigationPage.PageTransition"/>; an app can pass it to
/// <see cref="Avalonia.Controls.NavigationPage.PushAsync(Avalonia.Controls.Page, IPageTransition?)"/> too.
/// </summary>
/// <remarks>
/// As measured on the emulator (<c>tools/refapp/README.md</c>, "Page transitions" and "The turnstile's forward start";
/// <c>tools/motion/turnstile.py</c>): the page going out goes at once, and only the page coming in moves. It turns about
/// a vertical axis 44 px left of its left edge and 99 px behind the screen, seen through the camera that fits the
/// phone's <c>PlaneProjection</c>, 1,000 px in front of the page's centre. Forward it comes in with its right side away,
/// from −67.1° over 425 ms; back with its right side nearer, from +41.97° over 430 ms; both on an
/// <c>ExponentialEase</c> with exponent 6, easing out. The times are animation time, the emulator's frames counted.
/// </remarks>
public sealed class TurnstileTransition : IPageTransition
{
    /// <inheritdoc />
    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken) =>
        PhonePageRun.Start(Motion.Instance, from, to, forward, cancellationToken);

    /// <summary>The angle forward starts from, in degrees: the ease's start, which the first frame shows (−67.0° seen).</summary>
    internal const double ForwardFrom = -67.10;

    /// <summary>How long forward runs.</summary>
    internal static readonly TimeSpan ForwardLength = TimeSpan.FromMilliseconds(425);

    /// <summary>The angle back starts from, in degrees.</summary>
    internal const double BackFrom = 41.97;

    /// <summary>How long back runs.</summary>
    internal static readonly TimeSpan BackLength = TimeSpan.FromMilliseconds(430);

    /// <summary>The ease's exponent, both ways.</summary>
    internal const double Exponent = 6;

    /// <summary>The axis's distance left of the page's left edge, on the reference's 450 px wide page: a fraction of the width.</summary>
    internal const double AxisX = -44.0 / 450;

    /// <summary>The axis's distance behind the screen, in view px.</summary>
    internal const double AxisZ = -99;

    /// <summary>The reference's page width, in view px: wider, the axis's depth and the camera's distance grow with the width.</summary>
    internal const double ReferenceWidth = 450;

    /// <summary>The camera's distance in front of the screen, in view px, as the turnstile's angles were solved with it.</summary>
    internal const double EyeDistance = 1000;

    /// <summary>The page's angle <paramref name="time"/> after the first frame, in degrees: 0 at rest.</summary>
    internal static double AngleAt(TimeSpan time, bool forward) => forward
        ? PhoneEase.Remaining(ForwardFrom, Exponent, time, ForwardLength)
        : PhoneEase.Remaining(BackFrom, Exponent, time, BackLength);

    /// <summary>
    /// Where a point of a page of <paramref name="size"/> is drawn, turned by <paramref name="degrees"/> about the axis
    /// (a positive angle brings the right side nearer) and seen from the camera over its centre, as a perspective matrix
    /// on the page's own space.
    /// </summary>
    internal static Matrix ToMatrix(double degrees, Size size)
    {
        var r = degrees * Math.PI / 180;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        // Wider than the reference's 450, the axis's depth and the camera's distance grow with the width, so the turn is
        // the reference's turn scaled: with them fixed, a page about 1,400 px wide would bring its near edge to the eye.
        var wider = Math.Max(1, size.Width / ReferenceWidth);
        double ax = AxisX * size.Width, axisZ = AxisZ * wider, depth = -axisZ, d = EyeDistance * wider;
        double ex = size.Width / 2, ey = size.Height / 2;

        // A point (x, y) of the page is u = x − ax from the axis and `depth` in front of it. Turned, it is at
        // X = ax + u cos − depth sin, Z = axisZ + u sin + depth cos, and is drawn at eye + (P − eye) × d / (d − Z).
        // With W = (d − Z) / d, the drawn point times W is eye × W + P − eye: linear in x and y.
        var wx = -sin / d;
        var w0 = (d - axisZ - depth * cos + ax * sin) / d;
        return new Matrix(
            ex * wx + cos, ey * wx, wx,
            0, 1, 0,
            ex * w0 + ax - ax * cos - depth * sin - ex, ey * w0 - ey, w0);
    }

    private sealed class Motion : IPhonePageMotion
    {
        public static readonly Motion Instance = new();

        public bool GoesBackAsThePageCame => true;

        public TimeSpan Length(bool forward) => forward ? ForwardLength : BackLength;

        public (PageLook From, PageLook To) LookAt(TimeSpan time, bool forward, Size size) =>
            (PageLook.Hidden, new PageLook(1, ToMatrix(AngleAt(time, forward), size)));
    }
}
