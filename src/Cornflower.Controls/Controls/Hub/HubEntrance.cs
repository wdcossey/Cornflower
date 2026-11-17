using Avalonia;
using Avalonia.Data;
using Avalonia.Media;

namespace Cornflower.Controls;

/// <summary>
/// The phone's Hub entrance, as #148 read it from the 8.1 emulator (runs <c>20261116-1721-dev11</c> and
/// <c>-1811-</c>, <c>tools/motion/entrance.py</c>; <c>tools/refapp/README.md</c>, "Hub: the entrance"), run frame by
/// frame on <see cref="CornflowerClock"/>: each time the Hub is placed, its template's root turns in about a vertical
/// axis half the Hub's width left of its left edge, from <see cref="From"/> to 0 over <see cref="Duration"/>.
/// </summary>
/// <remarks>
/// The phone's is the template's <c>EntranceAnimationProjection</c>, a <c>PlaneProjection</c> on <c>HubRoot</c>:
/// <c>RotationY</c> −120° on the first frame, 0 on the 64th tick after (1,000 ms of animation time), an
/// <c>ExponentialEase</c> out of exponent 4.61 as <c>entrance.py</c> prints it (ln 100, 4.605, fits the logged angles to
/// their last place, 0.0005°); <c>CenterOfRotationX</c> −0.5,
/// nothing else, and no fade. Ours draws it with a <see cref="MatrixTransform"/> on the root, at animation priority over
/// the root's own, seen through <c>PlaneProjection</c>'s camera (<see cref="TiltModel.EyeDistance"/>, over the root's
/// centre), which #24 read. A root wider than the phone's 450 px is seen from further away, as the turnstile's page is,
/// so a desktop window's turn looks as the phone's does.
/// </remarks>
internal sealed class HubEntrance
{
    /// <summary>The angle on the first frame, in degrees: the right side away.</summary>
    public const double From = -120;

    /// <summary>The ease's exponent: ln 100, 4.605 (the first 12 ticks to 0.0005°, the log's last place; 4.61 is 0.04° off).</summary>
    public static readonly double Exponent = Math.Log(100);

    /// <summary>How long the turn takes, in animation time.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(1000);

    /// <summary>The axis, as a fraction of the root's width from its left edge: <c>CenterOfRotationX</c>.</summary>
    public const double CenterOfRotationX = -0.5;

    /// <summary>The phone's width, in view px: wider, the camera's distance grows with the width, as the turnstile's.</summary>
    public const double ReferenceWidth = 450;

    private readonly Visual _owner;
    private readonly MatrixTransform _transform = new();
    private Visual? _root;
    private IDisposable? _shown;
    private IDisposable? _origin;
    private IDisposable? _frame;
    private TimeSpan _start;
    private double _speed = 1;                            // read from CornflowerConfig as the motion starts
    private int _generation;

    public HubEntrance(Visual owner) => _owner = owner;

    /// <summary>Whether the turn is under way.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>The angle about Y now, in degrees: 0 when the turn isn't under way.</summary>
    public double Angle => IsRunning ? AngleAt(CornflowerClock.Since(_start, _speed)) : 0;

    /// <summary>The angle <paramref name="time"/> after the first frame, in degrees.</summary>
    public static double AngleAt(TimeSpan time) =>
        time >= Duration ? 0 : PhoneEase.Remaining(From, Exponent, time, Duration);

    /// <summary>
    /// Where a point of a root of <paramref name="size"/> is drawn, turned by <paramref name="degrees"/> about the axis
    /// (a positive angle brings the right side nearer, as <c>PlaneProjection</c>'s <c>RotationY</c>) and seen from the
    /// camera over its centre, as a perspective matrix on the root's own space, about its top left.
    /// </summary>
    public static Matrix ToMatrix(double degrees, Size size)
    {
        var r = degrees * Math.PI / 180;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        double ax = CenterOfRotationX * size.Width, d = TiltModel.EyeDistance * Math.Max(1, size.Width / ReferenceWidth);
        double ex = size.Width / 2, ey = size.Height / 2;

        // A point (x, y) on the screen's plane is u = x − ax from the axis. Turned, it is at X = ax + u cos, Z = u sin, and
        // is drawn at eye + (P − eye) × d / (d − Z). With W = (d − Z) / d, the drawn point times W is eye × W + P − eye:
        // linear in x and y.
        var wx = -sin / d;
        var w0 = 1 + ax * sin / d;
        return new Matrix(
            cos + ex * wx, ey * wx, wx,
            0, 1, 0,
            ax * (1 - cos) + ex * (w0 - 1), ey * (w0 - 1), w0);
    }

    /// <summary>Starts the turn on <paramref name="root"/> from its first frame, now.</summary>
    public void Begin(Visual? root)
    {
        Finish();
        // With CornflowerConfig.Animated off there is no turn: the root is at rest at once.
        if (root is null || !CornflowerConfig.Animated) return;
        _root = root;
        _start = CornflowerClock.Now;
        _speed = CornflowerClock.SpeedOf(MotionArea.PivotAndHub);
        IsRunning = true;
        _origin = root.SetValue(Visual.RenderTransformOriginProperty, RelativePoint.TopLeft, BindingPriority.Animation);
        _shown = root.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
        Apply();
        var generation = _generation;
        _frame = CornflowerClock.RequestFrame(_owner, () => Step(generation));
    }

    /// <summary>Draws the turn for the root's size now: call when it changes.</summary>
    public void Apply()
    {
        if (!IsRunning || _root is null) return;
        var size = _root.Bounds.Size;
        _transform.Matrix = size.Width > 0 ? ToMatrix(Angle, size) : Matrix.Identity;
    }

    /// <summary>Ends the turn at once, the root at rest.</summary>
    public void Finish()
    {
        _frame?.Dispose();
        _frame = null;
        _generation++;
        _shown?.Dispose();                                // the root's own transform and origin show again
        _origin?.Dispose();
        _shown = null;
        _origin = null;

        _root = null;
        IsRunning = false;
    }

    private void Step(int generation)
    {
        if (generation != _generation) return;
        if (CornflowerClock.Since(_start, _speed) >= Duration)
        {
            Finish();
            return;
        }

        Apply();
        _frame = CornflowerClock.RequestFrame(_owner, () => Step(generation));
    }
}
