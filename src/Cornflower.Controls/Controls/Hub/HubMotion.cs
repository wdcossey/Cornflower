namespace Cornflower.Controls;

/// <summary>
/// The phone's Hub motion, as #128 and #137 read it from the 8.1 emulator (runs <c>20261115-2059-dev11</c>,
/// <c>20261116-0001-dev11</c>, <c>-0007-</c>, <c>-0009-</c> and <c>-0025-</c>; <c>tools/refapp/README.md</c>, "Hub's
/// header" and "Hub: a background image, and the end of the sections"), and #148 read the loop backwards (run
/// <c>20261116-1751-dev11</c>, "Pivot and Hub: where a release goes"), run frame by frame on
/// <see cref="CornflowerClock"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <b>offset</b> is how far the sections have moved left, in view pixels. A finger moves it one for one, from 0 to
/// past the last section's start (the end), on into the wrap; and at the first section, right of 0, into the loop
/// backwards, as far as the end less the sections' width. At rest it is a section's start: let go, it goes on to the
/// next section's start or back to its own, by one of two curves fitted to the phone's frames
/// (<c>docs/controls/Hub/PLAN.md</c>): the swipe's, after a flick, and the way back's, after a slow release.
/// </para>
/// <para>
/// The <b>header</b> moves left at 1/18 of the offset, as on the phone (<see cref="HeaderRatio"/>).
/// </para>
/// <para>
/// The <b>loop</b>: let go past the end going forward, the sections carry on to where the first section, drawn after the
/// last, is at its place, by <see cref="LoopCurve"/> over <see cref="LoopDuration"/>; then the offset is 0 again. The
/// header holds two frames, flies out left at <see cref="HeaderFlyOutSpeed"/>, and at <see cref="HeaderJump"/> after
/// the release jumps a view's width right of its place and eases in, an <c>ExponentialEase</c> of exponent 7 over
/// 420 ms.
/// </para>
/// <para>
/// The <b>loop backwards</b> mirrors it: let go right of 0 going right, the sections carry on right by the same curve
/// over the same time, to where the end would show them (the end less the sections' width), and the offset becomes the
/// end. Nothing is drawn left of the first section on the way, as on the phone. The header holds two frames, flies out
/// right (<see cref="HeaderBackFlyOutSpeed"/>), and at <see cref="HeaderBackJump"/> is
/// <see cref="HeaderBackJumpDistance"/> left of its place and eases in. A slow release goes back to 0 on the phone's way
/// back from there (<see cref="BackReturnCurve"/>).
/// </para>
/// </remarks>
internal sealed class HubMotion
{
    /// <summary>The emulator's frame, on which every time the phone's motion was read is counted.</summary>
    public static readonly TimeSpan PhoneFrame = TimeSpan.FromMilliseconds(15.625);

    /// <summary>How far the header moves for each pixel the sections move: 1/18 (#128, worst 0.0005 px over 270 frames).</summary>
    public const double HeaderRatio = 1.0 / 18;

    /// <summary>The offset after a flick's release: fitted to 20 swipes to 0.36 px rms (worst 3.3) of a 405 px section.</summary>
    public static readonly KeySplineCurve SwipeCurve = new(0.19, 0.93, 0.40, 0.986);

    /// <inheritdoc cref="SwipeCurve"/>
    public static readonly TimeSpan SwipeDuration = TimeSpan.FromMilliseconds(743.75);

    /// <summary>The offset after a slow release: fitted to 5 drags held and let go to 1.05 px rms (worst 5.3).</summary>
    public static readonly KeySplineCurve ReturnCurve = new(0.18, 1, 0.54, 0.97);

    /// <inheritdoc cref="ReturnCurve"/>
    public static readonly TimeSpan ReturnDuration = TimeSpan.FromMilliseconds(687.5);

    /// <summary>The sections' carry past the end into the wrap: fitted to 4 loops to 0.93 px rms (worst 3.9).</summary>
    public static readonly KeySplineCurve LoopCurve = new(0.17, 1, 0.376, 0.983);

    /// <summary>From the loop's release to the offset's return to 0 (19 frames, all four loops read).</summary>
    public static readonly TimeSpan LoopDuration = TimeSpan.FromMilliseconds(296.875);

    /// <summary>How long the header holds where it was at the loop's release: two frames.</summary>
    public static readonly TimeSpan HeaderHold = TimeSpan.FromMilliseconds(31.25);

    /// <summary>The header's flight out to the left after the hold, in view pixels a millisecond (16.04 a frame).</summary>
    public const double HeaderFlyOutSpeed = 1.0267;

    /// <summary>When the header jumps right, after the loop's release: six frames.</summary>
    public static readonly TimeSpan HeaderJump = TimeSpan.FromMilliseconds(93.75);

    /// <summary>The header's ease back in after the jump: an <c>ExponentialEase</c> of exponent 7, out, over 420 ms.</summary>
    public static readonly TimeSpan HeaderFlyInDuration = TimeSpan.FromMilliseconds(420);

    /// <inheritdoc cref="HeaderFlyInDuration"/>
    public const double HeaderFlyInExponent = 7;

    /// <summary>
    /// The sections' way back to 0 from right of it, after a slow release: fitted to the phone's 50 frames to 0.25 px rms
    /// (worst 1.03) of a 144 px pull (#148, run <c>20261116-1751-dev11</c>, a 150 px drag held and let go).
    /// </summary>
    public static readonly KeySplineCurve BackReturnCurve = new(0.195, 0.859, 0.306, 1);

    /// <inheritdoc cref="BackReturnCurve"/>
    public static readonly TimeSpan BackReturnDuration = TimeSpan.FromMilliseconds(781.25);

    /// <summary>The header's flight out to the right in the loop backwards, in view pixels a millisecond (15.996 a frame).</summary>
    public const double HeaderBackFlyOutSpeed = 1.02374;

    /// <summary>When the header, flown out right, jumps back after the loop backwards's release: 30 frames (#148).</summary>
    public static readonly TimeSpan HeaderBackJump = TimeSpan.FromMilliseconds(468.75);

    /// <summary>How far left of its place the header is drawn on that jump, in view pixels (#148, the one swipe read).</summary>
    public const double HeaderBackJumpDistance = 33.836;

    /// <summary>
    /// The header's ease back in after that jump: an <c>ExponentialEase</c> of exponent 7 (<see cref="HeaderFlyInExponent"/>),
    /// out, over 66 ms, fitted to the phone's four frames to 0.04 px rms.
    /// </summary>
    public static readonly TimeSpan HeaderBackFlyInDuration = TimeSpan.FromMilliseconds(66);

    /// <summary>
    /// A release moving at least this fast, in view pixels a millisecond, goes on the way it went (or, past the end,
    /// loops); slower, the nearer section start. Not read on the phone (owed); the probes' swipes, 200 px in 150 ms,
    /// go on, and a 150 px drag held still goes back. Pivot's.
    /// </summary>
    public const double FlickVelocity = 0.3;

    private readonly Hub _hub;
    private IDisposable? _frame;
    private int _generation;
    private bool _moving;
    private double _dragFrom;

    // The move under way: from _from to _to by _curve over _duration from _start; or the loop.
    private TimeSpan _start;
    private double _from;
    private double _to;
    private KeySplineCurve _curve = SwipeCurve;
    private TimeSpan _duration;

    public HubMotion(Hub hub) => _hub = hub;

    /// <summary>Whether a finger is dragging the sections.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>Whether a move is under way after a release.</summary>
    public bool IsMoving => _moving;

    /// <summary>Whether the move under way is the loop past the end, or the one backwards past the start.</summary>
    public bool IsLooping { get; private set; }

    /// <summary>Whether the loop under way is the one backwards, past the start.</summary>
    public bool IsLoopingBack { get; private set; }

    /// <summary>Whether the sections themselves are moving: not in the loop's last part, where only the header is.</summary>
    public bool AreSectionsMoving => _moving && !(IsLooping && CornflowerClock.Now - _start >= LoopDuration);

    /// <summary>How far the sections have moved left, in view pixels.</summary>
    public double Offset { get; private set; }

    /// <summary>How far right of its place the header is drawn, in view pixels.</summary>
    public double HeaderX { get; private set; }

    /// <summary>Where the move under way ends: a section's start, 0 for the loop, the end for the loop backwards.</summary>
    public double Target => _to;

    /// <summary>Puts everything at rest at <paramref name="offset"/>, stopping any move, and draws it.</summary>
    public void Rest(double offset)
    {
        StopFrames();
        IsDragging = false;
        Offset = offset;
        HeaderX = -offset * HeaderRatio;
        _hub.ApplyMotion();
    }

    /// <summary>
    /// A finger has taken the sections: a move under way stops where it is, and the loop ends at once; from now on
    /// <see cref="Drag"/> moves them.
    /// </summary>
    public void BeginDrag()
    {
        if (IsLooping)
            Finish();
        else
            StopFrames();
        IsDragging = true;
        _dragFrom = Offset;
    }

    /// <summary>The finger is <paramref name="travel"/> view pixels left of where it pressed (right is negative).</summary>
    public void Drag(double travel)
    {
        if (!IsDragging) return;
        Offset = Math.Clamp(_dragFrom + travel, LowestOffset, _hub.SectionsLength);
        HeaderX = -Offset * HeaderRatio;
        _hub.ApplyMotion();
    }

    /// <summary>
    /// The finger has let go, moving at <paramref name="velocity"/> view pixels a millisecond (right positive): goes on
    /// to the next section, back to this one's start, or past the end round the loop.
    /// </summary>
    public void Release(double velocity)
    {
        if (!IsDragging) return;
        IsDragging = false;
        var forward = -velocity;
        var flick = Math.Abs(forward) >= FlickVelocity;
        var starts = _hub.SectionStarts;
        if (starts.Count == 0)
        {
            Rest(0);
            return;
        }

        var end = starts[^1];
        if (Offset < 0)
        {
            // Right of the first section: on round the loop backwards, or back to 0 on the phone's way back from there.
            var pulled = -Offset;
            var span = _hub.SectionsLength - end;
            if (forward <= -FlickVelocity || (forward < FlickVelocity && pulled >= span / 2))
                StartLoopBack(end);
            else
                Start(0, BackReturnCurve, BackReturnDuration);
            return;
        }

        if (Offset > end)
        {
            // Past the end: on round the loop, or back to the end.
            var past = Offset - end;
            var last = _hub.SectionsLength - end;
            if (forward >= FlickVelocity || (forward > -FlickVelocity && past >= last / 2))
                StartLoop();
            else
                Start(end, flick ? SwipeCurve : ReturnCurve, flick ? SwipeDuration : ReturnDuration);
            return;
        }

        var k = 0;
        while (k + 1 < starts.Count && starts[k + 1] <= Offset) k++;
        var here = starts[k];
        var next = k + 1 < starts.Count ? starts[k + 1] : here;
        double target;
        if (forward >= FlickVelocity)
            target = next;
        else if (forward <= -FlickVelocity)
            target = here;
        else
            target = next > here && Offset - here >= (next - here) / 2 ? next : here;

        if (target == Offset)
        {
            Rest(target);
            return;
        }

        Start(target, flick ? SwipeCurve : ReturnCurve, flick ? SwipeDuration : ReturnDuration);
    }

    /// <summary>Ends the move under way at once, where it was going.</summary>
    public void Finish()
    {
        if (!_moving) return;
        Rest(_to);
    }

    /// <summary>
    /// Ends whatever is under way at once: a move where it was going, a drag at the nearer section start (the Hub is
    /// leaving its window, or its sections or template changed).
    /// </summary>
    public void Stop()
    {
        if (_moving)
        {
            Finish();
        }
        else if (IsDragging)
        {
            // The nearest start; past the end, the first section's place after the last (L), which is 0 again; right of
            // the first, the end less L, which is the end.
            var starts = _hub.SectionStarts;
            var nearest = 0.0;
            foreach (var start in starts)
            {
                if (Math.Abs(start - Offset) < Math.Abs(nearest - Offset)) nearest = start;
            }

            var length = _hub.SectionsLength;
            var rest = nearest;
            if (length > 0 && Math.Abs(length - Offset) < Math.Abs(nearest - Offset))
                rest = 0;
            else if (starts.Count > 1 && Math.Abs(starts[^1] - length - Offset) < Math.Abs(nearest - Offset))
                rest = starts[^1];
            Rest(rest);
        }
    }

    /// <summary>Stops any frames without moving anything.</summary>
    public void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
        _moving = false;
        IsLooping = false;
        IsLoopingBack = false;
        _generation++;
    }

    /// <summary>
    /// The header during the loop, <paramref name="elapsed"/> after the release, right of its place: where it was at
    /// the release (<paramref name="from"/>), held, then flying out left; from <see cref="HeaderJump"/>, a view's width
    /// (<paramref name="view"/>) right, easing in.
    /// </summary>
    public static double LoopHeader(TimeSpan elapsed, double from, double view)
    {
        if (elapsed < HeaderJump)
            return from - HeaderFlyOutSpeed * Math.Max(0, (elapsed - HeaderHold).TotalMilliseconds);
        return view * (1 - PhoneEase.ExponentialOut(HeaderFlyInExponent, (elapsed - HeaderJump) / HeaderFlyInDuration));
    }

    /// <summary>
    /// The header during the loop backwards, <paramref name="elapsed"/> after the release, right of its place at 0:
    /// where it was at the release (<paramref name="from"/>), held, then flying out right; from
    /// <see cref="HeaderBackJump"/>, <see cref="HeaderBackJumpDistance"/> left of its place at the end
    /// (<paramref name="end"/>), easing in.
    /// </summary>
    public static double LoopBackHeader(TimeSpan elapsed, double from, double end)
    {
        if (elapsed < HeaderBackJump)
            return from + HeaderBackFlyOutSpeed * Math.Max(0, (elapsed - HeaderHold).TotalMilliseconds);
        return -end * HeaderRatio
               - PhoneEase.Remaining(HeaderBackJumpDistance, HeaderFlyInExponent, elapsed - HeaderBackJump, HeaderBackFlyInDuration);
    }

    /// <summary>How far right a finger can pull the sections: the end less their width, where the loop backwards ends.</summary>
    private double LowestOffset
    {
        get
        {
            var starts = _hub.SectionStarts;
            return starts.Count > 1 ? Math.Min(0, starts[^1] - _hub.SectionsLength) : 0;
        }
    }

    private void Start(double to, KeySplineCurve curve, TimeSpan duration)
    {
        StopFrames();
        _start = CornflowerClock.Now;
        _from = Offset;
        _to = to;
        _curve = curve;
        _duration = duration;
        _moving = true;
        var generation = _generation;
        _frame = CornflowerClock.RequestFrame(_hub, () => Step(generation));
    }

    private void StartLoop()
    {
        StopFrames();
        _start = CornflowerClock.Now;
        _from = Offset;
        _to = 0;
        _moving = true;
        IsLooping = true;
        var generation = _generation;
        _frame = CornflowerClock.RequestFrame(_hub, () => Step(generation));
    }

    private void StartLoopBack(double end)
    {
        StartLoop();
        _to = end;
        IsLoopingBack = true;
    }

    private void Step(int generation)
    {
        if (generation != _generation) return;
        var t = CornflowerClock.Now - _start;
        if (IsLoopingBack)
        {
            if (t >= HeaderBackJump + HeaderBackFlyInDuration)
            {
                Rest(_to);
                return;
            }

            // The sections carry on right to where the end would show them, the end less L; then the offset is the end.
            var carry = _to - _hub.SectionsLength;
            Offset = t < LoopDuration ? _from + (carry - _from) * LoopCurve.Ease(t / LoopDuration) : _to;
            HeaderX = LoopBackHeader(t, -_from * HeaderRatio, _to);
        }
        else if (IsLooping)
        {
            if (t >= HeaderJump + HeaderFlyInDuration)
            {
                Rest(0);
                return;
            }

            var length = _hub.SectionsLength;
            Offset = t < LoopDuration ? _from + (length - _from) * LoopCurve.Ease(t / LoopDuration) : 0;
            HeaderX = LoopHeader(t, -_from * HeaderRatio, _hub.ViewWidth);
        }
        else
        {
            if (t >= _duration)
            {
                Rest(_to);
                return;
            }

            Offset = _from + (_to - _from) * _curve.Ease(t / _duration);
            HeaderX = -Offset * HeaderRatio;
        }

        _hub.ApplyMotion();
        if (generation != _generation) return;            // a handler of the change started something else
        _frame = CornflowerClock.RequestFrame(_hub, () => Step(generation));
    }
}
