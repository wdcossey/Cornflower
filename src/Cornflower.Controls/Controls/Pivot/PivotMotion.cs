namespace Cornflower.Controls;

/// <summary>
/// The phone's Pivot motion, as #128 and #136 read it from the 8.1 emulator (runs <c>20261115-2056-dev11</c>,
/// <c>20261115-2136-dev11</c>, <c>20261115-2339-dev11</c> and <c>20261115-2343-dev11</c>, <c>tools/refapp/README.md</c>,
/// "Pivot's header strip" and "Pivot: wide headers, two items, and a tap on a header cut by the edge";
/// <c>tools/motion/pivot.py</c>), run frame by frame on <see cref="CornflowerClock"/>.
/// </summary>
/// <remarks>
/// <para>
/// The motion is the phone's control's own, not its template's: its <c>ScrollViewer</c>'s offset (DirectManipulation's),
/// the header strip's translate and the items' translate, which the Pivot's code moves. So it lives with the control,
/// as the tilt does, and no theme has to run it.
/// </para>
/// <para>
/// The <b>offset</b> is how far the content has moved forward from the item it started at, in view pixels: a page is
/// the view's width. A finger moves it one for one. Let go, it goes on to the next or previous page, or back, by one of
/// the phone's three curves, each a <see cref="KeySplineCurve"/> fitted to the phone's offsets frame by frame
/// (<c>docs/controls/Pivot/PLAN.md</c> has the fits): the swipe's from the release, the same shape whatever the
/// release's speed; a header tap's, or a change from code, from rest; and the way back from a drag let go.
/// </para>
/// <para>
/// The <b>header strip</b> moves in a straight line with the offset, a page of offset moving it one header's pitch
/// (<see cref="Primitives.PivotHeaderPanel.Pitch"/>): the pitch of the header leaving going forward, of the header
/// coming in going back. When the new item comes in, the strip is laid out again from it, so the headers already
/// passed go to the end, as on the phone.
/// </para>
/// <para>
/// The <b>content</b> isn't the offset's. The item shown goes with the finger. Going forward, from the frame after the
/// release it also slides out ahead of the offset, by <see cref="Slide"/>; then the new item comes in 450 px (a page)
/// beyond where it will settle and slides in on its own, one frame there and then <see cref="Slide"/>, an
/// <c>ExponentialEase</c> of exponent 4 over 400 ms. It comes in at the selection change for a swipe; after a tap or a
/// change from code, which change the selection at once, on the frame after the item leaving has gone off the screen.
/// Going back the new item comes in at once, from the other side.
/// </para>
/// <para>
/// The <b>selection</b> changes 5 frames (78.125 ms) after the release of a swipe forward and a frame (15.625 ms) after
/// one back, as the phone's does (its 188 to 241 ms and 152 to 174 ms after the press, for the probes' 150 ms swipe),
/// and at once for a tap or a change from code.
/// </para>
/// </remarks>
internal sealed class PivotMotion
{
    /// <summary>The emulator's frame, on which every time the phone's motion was read is counted.</summary>
    public static readonly TimeSpan PhoneFrame = TimeSpan.FromMilliseconds(15.625);

    /// <summary>The offset after a swipe's release: 33 frames, fitted to 20 swipes to 1.07 px rms of 450.</summary>
    public static readonly KeySplineCurve SwipeCurve = new(0.24, 0.60, 0.22, 1);

    /// <inheritdoc cref="SwipeCurve"/>
    public static readonly TimeSpan SwipeDuration = TimeSpan.FromMilliseconds(515.625);

    /// <summary>The offset after a header tap's release, or a change from code: 32 frames, fitted to 6 to 0.74 px rms.</summary>
    public static readonly KeySplineCurve TapCurve = new(0.20, 0.08, 0.02, 1);

    /// <inheritdoc cref="TapCurve"/>
    public static readonly TimeSpan TapDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>The offset going back after a drag let go short: 35 frames, fitted to the one read to 0.12 px rms of 109.</summary>
    public static readonly KeySplineCurve ReturnCurve = new(0.25, 0, 0.20, 1);

    /// <inheritdoc cref="ReturnCurve"/>
    public static readonly TimeSpan ReturnDuration = TimeSpan.FromMilliseconds(546.875);

    /// <summary>The items' slide, out and in: an <c>ExponentialEase</c> (exponent 4, out) over this.</summary>
    public static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(400);

    /// <summary>From a swipe's release to the selection change, forward (5 frames) and back (1).</summary>
    public static readonly TimeSpan ForwardSelectionDelay = TimeSpan.FromMilliseconds(78.125);

    /// <inheritdoc cref="ForwardSelectionDelay"/>
    public static readonly TimeSpan BackSelectionDelay = TimeSpan.FromMilliseconds(15.625);

    /// <summary>
    /// A release moving at least this fast, in view pixels a millisecond, the way the drag went, goes on to the next
    /// page; slower, it goes on only past half a page. Not read on the phone (owed); the probes' swipes, 200 px in
    /// 150 ms, go on, and a drag of 100 px held still goes back.
    /// </summary>
    public const double FlickVelocity = 0.3;

    private const double SlideExponent = 4;

    private readonly Pivot _pivot;
    private IDisposable? _frame;

    // The move under way: from the item at _origin, _steps pages (forward positive) to _target.
    private TimeSpan _start;
    private double _from;
    private int _steps;
    private int _origin;
    private int _target;
    private KeySplineCurve _curve = SwipeCurve;
    private TimeSpan _duration;
    private TimeSpan? _selectAt;
    private bool _gesture;
    private bool _jumped;
    private TimeSpan _jumpAt;
    private bool _oldGone;

    public PivotMotion(Pivot pivot) => _pivot = pivot;

    /// <summary>Whether a finger is dragging the content.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>Whether a move is under way after a release, a tap or a change from code.</summary>
    public bool IsMoving => _frame is not null;

    /// <summary>The offset from the item the strip and content are laid out from, in view pixels, forward positive.</summary>
    public double Offset { get; private set; }

    /// <summary>The item the strip is laid out from and whose content is shown.</summary>
    public int Lead { get; private set; }

    /// <summary>Where the shown item is drawn, in view pixels from its place at rest.</summary>
    public double ItemX { get; private set; }

    /// <summary>Puts everything at rest at <paramref name="index"/>, stopping any move, and draws it.</summary>
    public void Rest(int index)
    {
        StopFrames();
        IsDragging = false;
        Lead = index;
        Offset = 0;
        ItemX = 0;
        _pivot.ApplyMotion();
    }

    /// <summary>A finger has taken the content: from now on <see cref="Drag"/> moves it.</summary>
    public void BeginDrag()
    {
        if (IsMoving)
            Finish();
        IsDragging = true;
    }

    /// <summary>The finger is <paramref name="travel"/> view pixels left of where it pressed (right is negative).</summary>
    public void Drag(double travel)
    {
        if (!IsDragging) return;
        var page = _pivot.PageWidth;
        Offset = Math.Clamp(travel, -page, page);
        ItemX = -Offset;
        _pivot.ApplyMotion();
    }

    /// <summary>
    /// The finger has let go, moving at <paramref name="velocity"/> view pixels a millisecond (right positive): goes on
    /// to the next page, the previous one, or back.
    /// </summary>
    public void Release(double velocity)
    {
        if (!IsDragging) return;
        IsDragging = false;
        var page = _pivot.PageWidth;
        var forward = -velocity;
        var steps = 0;
        if (_pivot.ItemCount > 1 && Offset != 0)
        {
            if (Math.Abs(forward) >= FlickVelocity && Math.Sign(forward) == Math.Sign(Offset))
                steps = Math.Sign(forward);
            else if (Math.Abs(Offset) >= page / 2)
                steps = Math.Sign(Offset);
        }

        if (steps == 0 && Offset == 0)
        {
            Rest(Lead);
            return;
        }

        Start(steps, steps == 0 ? ReturnCurve : SwipeCurve, steps == 0 ? ReturnDuration : SwipeDuration, gesture: true,
            selectAfter: steps > 0 ? ForwardSelectionDelay : steps < 0 ? BackSelectionDelay : null);
    }

    /// <summary>
    /// Moves <paramref name="steps"/> pages from the item at rest, for a header tap or a change from code, whose
    /// selection has already changed: forward unless <paramref name="steps"/> is negative.
    /// </summary>
    public void MoveTo(int from, int steps)
    {
        StopFrames();
        IsDragging = false;
        Lead = from;
        Offset = 0;
        ItemX = 0;
        Start(steps, TapCurve, TapDuration, gesture: false, selectAfter: null);
    }

    /// <summary>Ends the move under way at once, at its target, as if it had run its course.</summary>
    public void Finish()
    {
        if (!IsMoving) return;
        if (_selectAt is not null)
        {
            _selectAt = null;
            _pivot.SelectFromMotion(_target);
        }

        if (!_jumped && _steps != 0)
            _pivot.OnItemCameIn(_origin, _target);
        Rest(_target);
    }

    /// <summary>Stops any frames without moving anything.</summary>
    public void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
    }

    /// <summary>
    /// The strip's slide, in view pixels to the left, for <paramref name="pages"/> of offset from the item at
    /// <paramref name="lead"/>: a page forward moves it the leading header's pitch, a page back the pitch of the header
    /// before it, and so on, in a straight line between.
    /// </summary>
    public static double StripTravel(Func<int, double> pitch, int lead, double pages)
    {
        if (pages >= 0)
        {
            var whole = (int)Math.Floor(pages);
            var sum = 0.0;
            for (var j = 0; j < whole; j++) sum += pitch(lead + j);
            return sum + (pages - whole) * pitch(lead + whole);
        }
        else
        {
            var back = -pages;
            var whole = (int)Math.Floor(back);
            var sum = 0.0;
            for (var j = 1; j <= whole; j++) sum += pitch(lead - j);
            return -(sum + (back - whole) * pitch(lead - whole - 1));
        }
    }

    /// <summary>
    /// The items' slide at <paramref name="elapsed"/> from its start: WinRT's <c>ExponentialEase</c>, exponent 4,
    /// <c>EaseOut</c>, over <see cref="SlideDuration"/>, from 0 to 1. Its first frames on the phone leave 0.8526,
    /// 0.7266, 0.6188 and 0.5266 of the way to go.
    /// </summary>
    public static double Slide(TimeSpan elapsed)
    {
        var x = Math.Clamp(elapsed / SlideDuration, 0, 1);
        return 1 - (Math.Exp(SlideExponent * (1 - x)) - 1) / (Math.Exp(SlideExponent) - 1);
    }

    private void Start(int steps, KeySplineCurve curve, TimeSpan duration, bool gesture, TimeSpan? selectAfter)
    {
        StopFrames();
        var count = _pivot.ItemCount;
        _start = CornflowerClock.Now;
        _from = Offset;
        _steps = steps;
        _origin = Lead;
        _target = Primitives.PivotHeaderPanel.Wrap(Lead + steps, count);
        _curve = curve;
        _duration = duration;
        _gesture = gesture;
        _selectAt = selectAfter;
        _jumped = false;
        _oldGone = false;
        _frame = CornflowerClock.RequestFrame(_pivot, Step);
    }

    private void Step()
    {
        _frame = null;
        var page = _pivot.PageWidth;
        var t = CornflowerClock.Now - _start;
        var offset = _from + (_steps * page - _from) * _curve.Ease(t / _duration);

        if (_selectAt is { } selectAt && t >= selectAt)
        {
            _selectAt = null;
            _pivot.SelectFromMotion(_target);
            if (_gesture || _steps < 0)
                Jump(t);
        }
        else if (!_jumped && _steps < 0 && !_gesture)
        {
            Jump(t);                                      // back, from code: the new item comes in at once
        }
        else if (!_jumped && _steps > 0 && !_gesture && _oldGone)
        {
            Jump(t);                                      // forward, after a tap or from code: once the old item is gone
        }

        double itemX;
        if (_jumped)
        {
            var toGo = 1 - Slide(t - _jumpAt - PhoneFrame);
            itemX = _steps * page - offset + Math.Sign(_steps) * page * toGo;
        }
        else
        {
            itemX = -offset - (_steps > 0 ? page * Slide(t - PhoneFrame) : 0);
            if (_steps > 0 && itemX <= -page)
                _oldGone = true;
        }

        Lead = _jumped ? _target : _origin;
        Offset = _jumped ? offset - _steps * page : offset;
        ItemX = itemX;

        var done = t >= _duration && (_steps == 0 || (_jumped && t - _jumpAt - PhoneFrame >= SlideDuration));
        if (done)
        {
            if (!_jumped && _steps != 0)
                _pivot.OnItemCameIn(_origin, _target);
            Rest(_steps == 0 ? _origin : _target);
            return;
        }

        _pivot.ApplyMotion();
        _frame = CornflowerClock.RequestFrame(_pivot, Step);
    }

    private void Jump(TimeSpan t)
    {
        _jumped = true;
        _jumpAt = t;
        _pivot.OnItemCameIn(_origin, _target);
    }
}
