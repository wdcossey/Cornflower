using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Cornflower.Controls;

/// <summary>
/// A <see cref="Pivot"/>'s swipe: a touch or a pen pressed on the Pivot's header strip or content that moves sideways
/// further than Avalonia's own <see cref="ScrollGestureRecognizer"/> waits for, and more sideways than up or down, is
/// the Pivot's from then on, and drags its content until it lifts.
/// </summary>
/// <remarks>
/// <para>
/// A gesture recogniser, not pointer handlers, so the Pivot takes part in Avalonia's gesture arbitration: a
/// <c>ScrollViewer</c> inside an item, deeper in the tree, sees the press first and takes a drag up or down past its
/// own threshold; a control that asks for no gesture on its press (the phone's <c>ManipulationMode="None"</c>, as Wp81's
/// Slider and ToggleSwitch set) keeps its drag; and once this one captures the pointer, nothing under it takes it.
/// </para>
/// <para>
/// Touch and pen only, as Avalonia's <see cref="ScrollGestureRecognizer"/>: a mouse drags nothing, and selects by
/// the header strip.
/// </para>
/// </remarks>
internal sealed class PivotSwipeGestureRecognizer : GestureRecognizer
{
    /// <summary>The distance a finger moves before it is a drag: Avalonia's <see cref="ScrollGestureRecognizer"/>'s.</summary>
    internal static readonly double StartDistance = new ScrollGestureRecognizer().ScrollStartDistance;

    /// <summary>The moves the release's speed is read over.</summary>
    private static readonly TimeSpan VelocityWindow = TimeSpan.FromMilliseconds(50);

    private readonly Pivot _pivot;
    private readonly List<(TimeSpan Time, double X)> _moves = [];
    private IPointer? _tracking;
    private Point _press;
    private bool _dragging;

    public PivotSwipeGestureRecognizer(Pivot pivot) => _pivot = pivot;

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_tracking is not null || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen))
            return;
        if (!_pivot.CanSwipe || !_pivot.IsInSwipeArea(e.GetPosition(_pivot)))
            return;

        _tracking = e.Pointer;
        _press = e.GetPosition(_pivot);
        _dragging = false;
        _moves.Clear();
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        var point = e.GetPosition(_pivot);
        var delta = point - _press;
        if (!_dragging)
        {
            if (Math.Abs(delta.X) <= StartDistance || Math.Abs(delta.X) <= Math.Abs(delta.Y) || !_pivot.CanSwipe)
                return;

            _dragging = true;
            Capture(e.Pointer);
            _pivot.BeginSwipe();
        }

        Track(point.X);
        _pivot.Swipe(_press.X - point.X);
        e.Handled = true;
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        if (_dragging)
        {
            Track(e.GetPosition(_pivot).X);
            _pivot.EndSwipe(Velocity());
            e.Handled = true;
        }

        Reset();
    }

    protected override void PointerCaptureLost(IPointer pointer)
    {
        if (pointer != _tracking)
            return;

        if (_dragging)
            _pivot.EndSwipe(0);
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
        _moves.Clear();
    }
}
