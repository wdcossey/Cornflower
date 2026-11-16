namespace Cornflower.Themes.Wp81;

/// <summary>
/// Where a phone's LoopingSelector goes when a finger lets go of it, and how, as #149 read it from the 8.1 emulator
/// (runs <c>20261116-1828-dev11</c> and <c>20261116-1904-dev11</c>, <c>-Probe looping</c>; <c>tools/refapp/README.md</c>,
/// "LoopingSelector, and a press on a closed ComboBox"). Pure: it works out offsets from times, and runs nothing.
/// </summary>
/// <remarks>
/// <para>The <b>offset</b> is how far the items have moved up, in view pixels; at rest it is a whole number of items
/// (<c>pitch</c> apart). Where a release goes is decided at the release, in whole items, as on the phone (its first
/// inertial <c>ViewChanging</c>'s <c>FinalView</c>): the item nearest where a coast at the release's speed would stop.
/// A finger held still before it lifts has no speed, so that is the item nearest where it was let go.</para>
/// <para><b>On to an item</b> the selector coasts by the law #113 read in the ScrollViewer's inertia,
/// <c>dv/dt = −a − c v²</c>, its speed at the release whatever lands it on the item. #113's own constants (one pair
/// for its flicks of a plain ScrollViewer) are out by 36 to 53 px rms on the selector's coasts, so the pair is the
/// selector's own (<see cref="Deceleration"/>, <see cref="Drag"/>), fitted to its eight coasts: three flicks of each
/// run and two drags held still and let go past half an item, 1.5 to 11.7 px rms each (under 1% of the flicks' 660 to
/// 2,500 px), at rest within 130 ms of the phone. At low speed the law is close to a constant deceleration of
/// 0.000785 px/ms², where the phone's slow releases read 0.000834 to 0.000967.</para>
/// <para><b>Back to an item</b> (let go short of half an item past it, held still) the phone first drifts on the
/// drag's way, then comes back. No simple ease fits that (#149: within 1.65% at best), so it is two pieces fitted to
/// the five such releases of the two runs, 1.0 to 2.2 px rms each: a drift at <see cref="DriftSpeed"/> for
/// <see cref="DriftTime"/>, then a sine ease-in-out to the item over <see cref="BackScale"/> √d ms, d the way back in
/// view pixels (313 to 397 ms for the 47 to 75 px read).</para>
/// </remarks>
internal sealed class PhoneLoopingSelectorMotion
{
    /// <summary>The law's constant deceleration, <c>a</c>, in view pixels a millisecond squared.</summary>
    public const double Deceleration = 0.000785;

    /// <summary>The law's drag, <c>c</c>, per view pixel: the deceleration grows with the speed squared.</summary>
    public const double Drag = 0.00049;

    /// <summary>How fast the drift goes on the drag's way before the way back, in view pixels a millisecond.</summary>
    public const double DriftSpeed = 0.086;

    /// <summary>How long the drift lasts.</summary>
    public static readonly TimeSpan DriftTime = TimeSpan.FromMilliseconds(98);

    /// <summary>The way back's length in ms for each √px of the way: 46 √d.</summary>
    public const double BackScale = 46;

    private readonly double _from;
    private readonly double _to;
    private readonly double _way;

    // The coast: its time to stop at the release, and √(a c).
    private readonly double _stopTime;

    // The way back: where it starts, and how long it takes.
    private readonly double _backFrom;
    private readonly double _backTime;

    private PhoneLoopingSelectorMotion(double from, double to, double way, bool back)
    {
        _from = from;
        _to = to;
        _way = way;
        IsBack = back;
        if (back)
        {
            _backFrom = from + way * DriftSpeed * DriftTime.TotalMilliseconds;
            _backTime = BackScale * Math.Sqrt(Math.Abs(_to - _backFrom));
            Duration = DriftTime + TimeSpan.FromMilliseconds(_backTime);
        }
        else
        {
            _stopTime = StopTime(Math.Abs(to - from));
            Duration = TimeSpan.FromMilliseconds(_stopTime);
        }
    }

    /// <summary>Where the release goes: a whole number of items.</summary>
    public double Target => _to;

    /// <summary>Whether this is the drift and the way back, rather than a coast on to the target.</summary>
    public bool IsBack { get; }

    /// <summary>How long it takes from the release to rest.</summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// The motion after a release at <paramref name="offset"/>, moving at <paramref name="velocity"/> view pixels a
    /// millisecond (positive as the offset grows), after a drag whose way was <paramref name="way"/> (+1, −1, or 0 for
    /// none). Items are <paramref name="pitch"/> apart, and the target is kept between <paramref name="min"/> and
    /// <paramref name="max"/>. Null if it is at rest on its target already.
    /// </summary>
    public static PhoneLoopingSelectorMotion? Release(double offset, double velocity, int way, double pitch,
        double min = double.NegativeInfinity, double max = double.PositiveInfinity)
    {
        var end = offset + Math.Sign(velocity) * CoastLength(Math.Abs(velocity));
        var target = Math.Clamp(Math.Round(end / pitch) * pitch, min, max);
        if (target == offset)
            return null;

        var toward = Math.Sign(target - offset);
        var back = way != 0 && toward != way && Math.Abs(velocity) * toward <= 0;
        return new PhoneLoopingSelectorMotion(offset, target, way, back);
    }

    /// <summary>
    /// A move on to <paramref name="target"/> from <paramref name="offset"/> by the coast's law, as a release that
    /// decided it would: for a wheel's notch, which the phone never had. Null if it is there already.
    /// </summary>
    public static PhoneLoopingSelectorMotion? CoastTo(double offset, double target) =>
        target == offset ? null : new PhoneLoopingSelectorMotion(offset, target, 0, back: false);

    /// <summary>The offset <paramref name="elapsed"/> after the release.</summary>
    public double OffsetAt(TimeSpan elapsed)
    {
        if (elapsed >= Duration)
            return _to;
        var t = Math.Max(0, elapsed.TotalMilliseconds);
        if (IsBack)
        {
            if (t <= DriftTime.TotalMilliseconds)
                return _from + _way * DriftSpeed * t;
            var s = (t - DriftTime.TotalMilliseconds) / _backTime;
            return _backFrom + (_to - _backFrom) * (1 - Math.Cos(Math.PI * s)) / 2;
        }

        return _to - Math.Sign(_to - _from) * Remaining(_stopTime - t);
    }

    /// <summary>How far a coast at <paramref name="speed"/> goes before it stops: (1/2c) ln(1 + c v² / a).</summary>
    public static double CoastLength(double speed) => Math.Log(1 + Drag * speed * speed / Deceleration) / (2 * Drag);

    /// <summary>The speed a coast needs to go <paramref name="distance"/>.</summary>
    public static double CoastSpeed(double distance) => Math.Sqrt(Deceleration / Drag * (Math.Exp(2 * Drag * distance) - 1));

    /// <summary>How long a coast of <paramref name="distance"/> takes, in ms: acos(e^(−c d)) / √(a c).</summary>
    public static double StopTime(double distance) => Math.Acos(Math.Exp(-Drag * distance)) / Math.Sqrt(Deceleration * Drag);

    /// <summary>How far a coast still has to go with <paramref name="toStop"/> ms left: −ln(cos(√(a c) τ)) / c.</summary>
    private static double Remaining(double toStop) =>
        toStop <= 0 ? 0 : -Math.Log(Math.Cos(Math.Sqrt(Deceleration * Drag) * toStop)) / Drag;
}
