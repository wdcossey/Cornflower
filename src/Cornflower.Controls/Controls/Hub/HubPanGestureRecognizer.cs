using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Cornflower.Controls;

/// <summary>
/// A <see cref="Hub"/>'s pan: a touch or a pen pressed on the Hub that moves sideways further than Avalonia's own
/// <see cref="ScrollGestureRecognizer"/> waits for, and more sideways than up or down, is the Hub's from then on, and
/// drags its sections until it lifts.
/// </summary>
/// <remarks>
/// The shape of Pivot's <c>PivotSwipeGestureRecognizer</c>, for the same reasons: a gesture recogniser takes part in
/// Avalonia's arbitration, so a <c>ScrollViewer</c> inside a section keeps a drag up or down, a control that asks for no
/// gesture (Wp81's Slider and ToggleSwitch) keeps its drag, and once this one captures the pointer nothing under it
/// takes it. Touch and pen only, as Avalonia's <see cref="ScrollGestureRecognizer"/>.
/// </remarks>
internal sealed class HubPanGestureRecognizer : GestureRecognizer
{
    /// <summary>The distance a finger moves before it is a drag: Avalonia's <see cref="ScrollGestureRecognizer"/>'s.</summary>
    internal static readonly double StartDistance = new ScrollGestureRecognizer().ScrollStartDistance;

    /// <summary>The moves the release's speed is read over.</summary>
    private static readonly TimeSpan VelocityWindow = TimeSpan.FromMilliseconds(50);

    private readonly Hub _hub;
    private readonly List<(TimeSpan Time, double X)> _moves = [];
    private IPointer? _tracking;
    private Point _press;
    private bool _dragging;
    private bool _held;

    public HubPanGestureRecognizer(Hub hub) => _hub = hub;

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_tracking is not null || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen))
            return;
        if (!_hub.CanPan || !_hub.IsInPanArea(e.GetPosition(_hub)))
            return;

        _tracking = e.Pointer;
        _press = e.GetPosition(_hub);
        _dragging = false;
        _moves.Clear();

        // A finger on moving sections holds them where they are, as the phone's DirectManipulation does. If something
        // in a section takes the finger, this hears PointerCaptureLost and lets them go on.
        _held = _hub.Motion.AreSectionsMoving;
        if (_held)
            _hub.Motion.BeginDrag();
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        var point = e.GetPosition(_hub);
        var delta = point - _press;
        if (!_dragging)
        {
            if (Math.Abs(delta.X) <= StartDistance || Math.Abs(delta.X) <= Math.Abs(delta.Y) || !_hub.CanPan)
                return;

            _dragging = true;
            if (!_held)
            {
                Capture(e.Pointer);
                _hub.Motion.BeginDrag();
            }
        }

        Track(point.X);
        _hub.Motion.Drag(_press.X - point.X);
        e.Handled = true;
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        if (_dragging)
        {
            Track(e.GetPosition(_hub).X);
            _hub.Motion.Release(Velocity());
            e.Handled = true;
        }
        else if (_held)
        {
            _hub.Motion.Release(0);                       // let go where it was held: on to the nearer section
        }

        Reset();
    }

    protected override void PointerCaptureLost(IPointer pointer)
    {
        if (pointer != _tracking)
            return;

        if (_dragging || _held)
            _hub.Motion.Release(0);
        Reset();
    }

    private void Track(double x)
    {
        var now = CornflowerClock.Now;
        _moves.Add((now, x));
        var stale = 0;
        while (now - _moves[stale].Time > VelocityWindow) stale++;
        _moves.RemoveRange(0, stale);
    }

    /// <summary>View pixels a millisecond, right positive, over the last moves; 0 if they all came at one time.</summary>
    private double Velocity()
    {
        if (_moves.Count < 2) return 0;
        var first = _moves[0];
        var last = _moves[^1];
        var ms = (last.Time - first.Time).TotalMilliseconds;
        return ms > 0 ? (last.X - first.X) / ms : 0;
    }

    private void Reset()
    {
        _tracking = null;
        _dragging = false;
        _held = false;
        _moves.Clear();
    }
}
