using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Cornflower.Controls;

/// <summary>
/// What a <see cref="SidewaysSwipeGestureRecognizer"/> drags: a <see cref="Pivot"/>'s items, a <see cref="Hub"/>'s
/// sections, or a FlipView's (the Wp81 theme's Carousel's) items. Each implements it explicitly, so none of it is the
/// control's public API.
/// </summary>
internal interface ISidewaysSwipeTarget
{
    /// <summary>Whether a finger can drag it now.</summary>
    bool CanSwipe { get; }

    /// <summary>Whether <paramref name="point"/>, in the control's coordinates, is in the part a swipe starts in.</summary>
    bool IsInSwipeArea(Point point);

    /// <summary>
    /// Whether a finger coming down now holds what is moving where it is, as the phone's DirectManipulation catches a
    /// moving Hub. If it does, the recogniser begins the drag at the press (<see cref="BeginDrag"/>) and doesn't capture
    /// the finger, so that something in the content can still take it. The Pivot holds a swipe's move (#163); a header
    /// tap's or a code change's move it ends when the drag begins.
    /// </summary>
    bool HoldsAPress { get; }

    /// <summary>The finger has moved far enough sideways: the drag begins, from where the content is.</summary>
    void BeginDrag();

    /// <summary>
    /// The finger is <paramref name="travel"/> left of where it came down, in view pixels; up, for a
    /// <see cref="SidewaysSwipeGestureRecognizer.Vertical"/> recogniser.
    /// </summary>
    void Drag(double travel);

    /// <summary>
    /// The finger has lifted, or was taken, at <paramref name="velocity"/> view pixels a millisecond, right positive;
    /// down positive, for a <see cref="SidewaysSwipeGestureRecognizer.Vertical"/> recogniser.
    /// </summary>
    void Release(double velocity);
}

/// <summary>
/// A Pivot's swipe and a Hub's pan: a touch or a pen pressed in the control's swipe area that moves sideways further
/// than Avalonia's own <see cref="ScrollGestureRecognizer"/> waits for, and more sideways than up or down, is the
/// control's from then on, and drags its content until it lifts.
/// </summary>
/// <remarks>
/// <para>
/// A gesture recogniser, not pointer handlers, so the control takes part in Avalonia's gesture arbitration: a
/// <c>ScrollViewer</c> inside an item or a section, deeper in the tree, sees the press first and takes a drag up or
/// down past its own threshold; a control that asks for no gesture on its press (the phone's
/// <c>ManipulationMode="None"</c>, as Wp81's Slider and ToggleSwitch set) keeps its drag; and once this one captures the
/// pointer, nothing under it takes it.
/// </para>
/// <para>
/// Touch and pen only, as Avalonia's <see cref="ScrollGestureRecognizer"/>: a mouse drags nothing, and selects by the
/// Pivot's header strip.
/// </para>
/// <para>
/// A vertical FlipView's is turned on its side (<see cref="Vertical"/>): the same rules, up and down for sideways.
/// </para>
/// </remarks>
internal sealed class SidewaysSwipeGestureRecognizer : GestureRecognizer
{
    /// <summary>The distance a finger moves before it is a drag: Avalonia's <see cref="ScrollGestureRecognizer"/>'s.</summary>
    internal static readonly double StartDistance = new ScrollGestureRecognizer().ScrollStartDistance;

    /// <summary>The moves the release's speed is read over.</summary>
    private static readonly TimeSpan VelocityWindow = TimeSpan.FromMilliseconds(50);

    private readonly Visual _control;
    private readonly ISidewaysSwipeTarget _target;
    private readonly List<(TimeSpan Time, double X)> _moves = [];
    private IPointer? _tracking;
    private Point _press;
    private bool _dragging;
    private bool _held;

    private SidewaysSwipeGestureRecognizer(Visual control, ISidewaysSwipeTarget target)
    {
        _control = control;
        _target = target;
    }

    /// <summary>A recogniser for <paramref name="control"/>, which it reads positions in and drags.</summary>
    public static SidewaysSwipeGestureRecognizer For<T>(T control) where T : Visual, ISidewaysSwipeTarget => new(control, control);

    /// <summary>A recogniser for <paramref name="control"/>, which it reads positions in, dragging <paramref name="target"/>.</summary>
    public static SidewaysSwipeGestureRecognizer For(Visual control, ISidewaysSwipeTarget target) => new(control, target);

    /// <summary>Whether the swipe is up and down instead of sideways: a vertical FlipView's. Off by default.</summary>
    public bool Vertical { get; set; }

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_tracking is not null || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen))
            return;
        if (!_target.CanSwipe || !_target.IsInSwipeArea(e.GetPosition(_control)))
            return;

        _tracking = e.Pointer;
        _press = e.GetPosition(_control);
        _dragging = false;
        _moves.Clear();

        // If something in the content takes the finger, this hears PointerCaptureLost and lets go.
        _held = _target.HoldsAPress;
        if (_held)
            _target.BeginDrag();
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        var point = e.GetPosition(_control);
        var delta = point - _press;
        var along = Along(delta);
        var across = Vertical ? delta.X : delta.Y;
        if (!_dragging)
        {
            if (Math.Abs(along) <= StartDistance || Math.Abs(along) <= Math.Abs(across) || !_target.CanSwipe)
                return;

            _dragging = true;
            if (!_held)
            {
                Capture(e.Pointer);
                _target.BeginDrag();
            }
        }

        Track(Along(point));
        _target.Drag(-along);
        e.Handled = true;
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer != _tracking)
            return;

        if (_dragging)
        {
            Track(Along(e.GetPosition(_control)));
            _target.Release(Velocity());
            e.Handled = true;
        }
        else if (_held)
        {
            _target.Release(0);                       // let go where it was held
        }

        Reset();
    }

    protected override void PointerCaptureLost(IPointer pointer)
    {
        if (pointer != _tracking)
            return;

        if (_dragging || _held)
            _target.Release(0);
        Reset();
    }

    private double Along(Point point) => Vertical ? point.Y : point.X;

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
