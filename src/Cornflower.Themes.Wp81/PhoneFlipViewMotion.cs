using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's FlipView motion on a Carousel, as #211 read it from the 8.1 emulator (runs <c>20261118-2213-dev11</c>
/// and <c>20261118-2216-dev11</c>, <c>tools/refapp/README.md</c>, "Tier 4's reads", FlipView; <c>tools/refapp/flip.py</c>),
/// run frame by frame on <see cref="CornflowerClock"/> at <see cref="CornflowerConfig.ControlSpeed"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The position</b> (<see cref="Position"/>) is where the strip of items is, in pages from item 0: item k at rest is k
/// pages along, and a page is the item, the FlipView less its 2.5 margin each side. A finger moves it one for one, by
/// the Pivot's and Hub's recogniser (<see cref="SidewaysSwipeGestureRecognizer"/>), up to a page either way of the
/// selected item. Past the first item it follows <see cref="FirstEndFollow"/> of the finger, past the last
/// <see cref="LastEndFollow"/>, and it never goes round.
/// </para>
/// <para>
/// <b>Let go</b>, it goes on or back by the Pivot's rule (<see cref="PivotMotion.ReleaseLookAhead"/>,
/// <see cref="PivotMotion.ReleaseReach"/>), on by the Pivot's swipe curve, <see cref="PivotMotion.SwipeCurve"/>, back by
/// <see cref="BackCurve"/>; the selection changes on the snap's last frame. A press while it moves stops it where it is;
/// a drag goes on from there, and let go without one, the rule is read again from where it stopped.
/// </para>
/// <para>
/// <b>SelectedIndex from code</b>, or Avalonia's keys: a change of one moves by <see cref="CodeCurve"/>; of more, or from
/// no selection, the strip jumps.
/// </para>
/// </remarks>
internal sealed class PhoneFlipViewMotion : ISidewaysSwipeTarget
{
    /// <summary>The snap back after a release short of the rule's reach, and from past an end: 41 frames, fitted to 15 reads to 6.6 px rms.</summary>
    /// <remarks>
    /// Y1 is below 0, so it first carries on a little the way it was dragged, as the phone's does (10 to 28 px in its
    /// first 125 ms), and then comes back.
    /// </remarks>
    public static readonly KeySplineCurve BackCurve = new(0.43, -0.47, 0.25, 1);

    /// <inheritdoc cref="BackCurve"/>
    public static readonly TimeSpan BackDuration = TimeSpan.FromMilliseconds(640.625);

    /// <summary>A change of one to SelectedIndex from code: 33 frames, fitted to the one read (2 to 1) to 1.56 px rms of 445.</summary>
    public static readonly KeySplineCurve CodeCurve = new(0.22, 0.06, 0, 1);

    /// <inheritdoc cref="CodeCurve"/>
    public static readonly TimeSpan CodeDuration = TimeSpan.FromMilliseconds(515.625);

    /// <summary>A drag past the first item moves the items this share of the finger's travel: 139.7 px for a 150 px drag.</summary>
    public const double FirstEndFollow = 139.7 / 150;

    /// <summary>A drag past the last item moves the items this share of the finger's travel: 98.1 and 100.6 px for 150 px.</summary>
    public const double LastEndFollow = 99.35 / 150;

    private readonly Carousel _carousel;
    private readonly SidewaysSwipeGestureRecognizer _recognizer;
    private IDisposable? _frame;
    private int _generation;

    // The move under way: from _from to _to (pages), by _curve over _duration from _start, at _speed.
    private double _from;
    private double _to;
    private KeySplineCurve _curve = PivotMotion.SwipeCurve;
    private TimeSpan _duration;
    private TimeSpan _start;
    private double _speed = 1;
    private bool _selectAtEnd;
    private bool _gesture;

    private double _dragFrom;
    private bool _selecting;
    private object? _selectedItem;

    public PhoneFlipViewMotion(Carousel carousel)
    {
        _carousel = carousel;
        _recognizer = SidewaysSwipeGestureRecognizer.For(carousel, this);
        _recognizer.Vertical = PhoneFlipView.IsVertical(carousel);
        carousel.GestureRecognizers.Add(_recognizer);
        carousel.PropertyChanged += OnCarouselPropertyChanged;
        carousel.ItemsView.CollectionChanged += OnItemsChanged;
        Position = Math.Max(0, carousel.SelectedIndex);
        _selectedItem = carousel.SelectedItem;
    }

    /// <summary>Where the strip is, in pages from item 0.</summary>
    public double Position { get; private set; }

    /// <summary>Whether a finger is dragging the items.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>Whether a snap or a move from code is under way.</summary>
    public bool IsMoving => _frame is not null;

    private bool On => PhoneFlipView.GetIsEnabled(_carousel);

    /// <summary>A page, in view pixels: the theme's items panel along the axis; 0 under any other panel.</summary>
    private double Page => _carousel.ItemsPanelRoot is PhoneFlipViewPanel panel ? (_recognizer.Vertical ? panel.Bounds.Height : panel.Bounds.Width) : 0;

    private int Selected => Math.Max(0, _carousel.SelectedIndex);

    bool ISidewaysSwipeTarget.CanSwipe
    {
        get
        {
            // The axis is read again at each press: a PageSlide's Orientation can change without the property changing.
            var vertical = PhoneFlipView.IsVertical(_carousel);
            if (vertical != _recognizer.Vertical)
            {
                _recognizer.Vertical = vertical;
                _carousel.ItemsPanelRoot?.InvalidateArrange();
            }

            return On && _carousel.IsSwipeEnabled && _carousel.IsEffectivelyEnabled && _carousel.ItemCount > 0 && Page > 0;
        }
    }

    bool ISidewaysSwipeTarget.IsInSwipeArea(Point point) => true;

    /// <summary>A press holds a snap where it is (#211); a move from code, whose selection has changed, it doesn't.</summary>
    bool ISidewaysSwipeTarget.HoldsAPress => IsMoving && _gesture;

    void ISidewaysSwipeTarget.BeginDrag()
    {
        // A press while a snap moves stops it dead (#211); a drag during a move from code ends that move at its target,
        // as the Pivot's does.
        var finish = IsMoving && !_gesture;
        StopFrames();
        if (finish)
            Position = _to;
        IsDragging = true;
        _dragFrom = Position;
    }

    void ISidewaysSwipeTarget.Drag(double travel)
    {
        if (!IsDragging || Page <= 0) return;
        var selected = Selected;
        var last = _carousel.ItemCount - 1;
        var at = _dragFrom + travel / Page;
        if (at < 0) at *= FirstEndFollow;
        else if (at > last) at = last + (at - last) * LastEndFollow;
        Position = Math.Clamp(at, selected - 1, selected + 1);
        Arrange();
    }

    void ISidewaysSwipeTarget.Release(double velocity)
    {
        if (!IsDragging) return;
        IsDragging = false;
        var page = Page;
        var selected = Selected;
        var offset = Position - selected;
        if (page <= 0 || offset == 0)
        {
            Rest();
            return;
        }

        // The Pivot's rule (#148): on if the offset plus its speed the way the drag went, over the look-ahead, passes
        // the reach; back if not, or if the release heads back.
        var heading = offset - velocity / page * PivotMotion.ReleaseLookAhead.TotalMilliseconds;
        var steps = Math.Sign(heading) == Math.Sign(offset) && Math.Abs(heading) >= PivotMotion.ReleaseReach ? Math.Sign(offset) : 0;
        var target = selected + steps;
        if (target < 0 || target >= _carousel.ItemCount)
            target = selected;

        if (target == selected)
            Start(selected, BackCurve, BackDuration, selectAtEnd: false, gesture: true);
        else
            Start(target, PivotMotion.SwipeCurve, PivotMotion.SwipeDuration, selectAtEnd: true, gesture: true);
    }

    /// <summary>Puts the strip at rest at the selected item, stopping any move or drag.</summary>
    public void Rest()
    {
        StopFrames();
        IsDragging = false;
        Position = Selected;
        Arrange();
    }

    private void OnCarouselPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Carousel.PageTransitionProperty)
        {
            _recognizer.Vertical = PhoneFlipView.IsVertical(_carousel);
            _carousel.ItemsPanelRoot?.InvalidateArrange();
        }
        else if (change.Property == Carousel.SelectedIndexProperty)
        {
            var (from, to) = change.GetOldAndNewValue<int>();
            var item = to >= 0 && to < _carousel.ItemCount ? _carousel.ItemsView[to] : null;
            var moved = !ReferenceEquals(item, _selectedItem);  // an index shifted by a change to the items isn't a move
            _selectedItem = item;
            if (_selecting)
                return;
            if (On && moved && from >= 0 && to >= 0 && Math.Abs(to - from) == 1 && Page > 0)
            {
                IsDragging = false;
                Start(to, CodeCurve, CodeDuration, selectAtEnd: false, gesture: false);
            }
            else
            {
                Rest();                                   // a change of two or more jumps (#211)
            }
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rest();

    private void Start(int target, KeySplineCurve curve, TimeSpan duration, bool selectAtEnd, bool gesture)
    {
        StopFrames();
        _gesture = gesture;
        _from = Position;
        _to = target;
        _curve = curve;
        _duration = duration;
        _selectAtEnd = selectAtEnd;
        _start = CornflowerClock.Now;
        _speed = CornflowerConfig.SpeedOf(MotionArea.Control);
        var generation = ++_generation;
        _frame = CornflowerClock.RequestFrame(_carousel, () => Step(generation));
    }

    private void Step(int generation)
    {
        if (generation != _generation) return;
        var t = CornflowerClock.Since(_start, _speed);
        if (t < _duration)
        {
            Position = _from + (_to - _from) * _curve.Ease(t / _duration);
            Arrange();
            _frame = CornflowerClock.RequestFrame(_carousel, () => Step(generation));
            return;
        }

        StopFrames();
        Position = _to;
        Arrange();
        if (_selectAtEnd)
        {
            _selecting = true;                            // the selection changes with the snap's last frame (#211)
            try { _carousel.SelectedIndex = (int)_to; }
            finally { _selecting = false; }
        }
    }

    private void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
        _generation++;
    }

    private void Arrange() => (_carousel.ItemsPanelRoot as PhoneFlipViewPanel)?.Place();
}
