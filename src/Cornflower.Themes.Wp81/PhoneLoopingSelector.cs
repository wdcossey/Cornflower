using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's LoopingSelector, the date and time pickers' wheel, on Avalonia's <see cref="DateTimePickerPanel"/>:
/// a finger drags its items, and a release coasts on to an item or drifts and comes back to one, frame by frame on
/// <see cref="CornflowerClock"/> (<see cref="PhoneLoopingSelectorMotion"/>); a tap only makes it the active wheel; the
/// active wheel shows every item, and the others only their selected one. Turned on by the
/// <c>Wp81LoopingSelectorTheme</c>'s own setter.
/// </summary>
/// <remarks>
/// <para>The phone's <c>LoopingSelector</c>, <c>LoopingSelectorItem</c> and <c>LoopingSelectorPanel</c> can't be made by
/// an app (no constructor in the 8.1 <c>Windows.winmd</c>): they are the DatePickerFlyout's and TimePickerFlyout's
/// parts. Avalonia's <c>DatePickerPresenter</c> and <c>TimePickerPresenter</c> require a
/// <see cref="DateTimePickerPanel"/> for each column, so the wheel is that panel, driven (docs/controls/LoopingSelector).</para>
/// <para>The panel's own ways are kept from the finger: a drag is this recogniser's (deeper than any ScrollViewer's, so
/// it sees the press first), and a tap on an item, which the panel would select, is handled at the item. The panel
/// isn't in a <see cref="ScrollViewer"/>: its presenter clamps the offset to the extent less the viewport, and the
/// panel's extent for a range that doesn't loop is the items alone, so its last items could never be reached.</para>
/// <para>What it sets: the panel's <c>Offset</c> while it moves and its <c>SelectedValue</c> at rest (which puts a
/// looping panel back in the middle of its extent, as the phone recentres); <see cref="IsMovingProperty"/> and
/// <see cref="ExpansionProperty"/> on the panel, which its items inherit and the item theme reads; and each item's
/// <c>Theme</c>, the one <see cref="ItemThemeProperty"/> names.</para>
/// </remarks>
internal static class PhoneLoopingSelector
{
    /// <summary>The fade between the active wheel's look and the others': the template's generated 0.33 s, a straight line.</summary>
    public static readonly TimeSpan ExpansionTime = TimeSpan.FromMilliseconds(330);

    /// <summary>Set by the <c>Wp81LoopingSelectorTheme</c>.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DateTimePickerPanel, bool>("IsEnabled", typeof(PhoneLoopingSelector));

    /// <summary>The theme the panel's items take: the phone's <c>LoopingSelectorItem</c>.</summary>
    public static readonly AttachedProperty<ControlTheme?> ItemThemeProperty =
        AvaloniaProperty.RegisterAttached<DateTimePickerPanel, ControlTheme?>("ItemTheme", typeof(PhoneLoopingSelector));

    /// <summary>
    /// Whether the wheel is being dragged or is moving after a release: no item shows as selected then, as the phone's
    /// selected item goes from <c>Selected</c> to <c>Expanded</c> as a drag starts, and the new one to <c>Selected</c> at
    /// rest. Inherited by the items.
    /// </summary>
    public static readonly AttachedProperty<bool> IsMovingProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsMoving", typeof(PhoneLoopingSelector), inherits: true);

    /// <summary>
    /// How far the wheel shows its unselected items, 0 to 1: 1 for the active wheel (<c>Expanded</c>), 0 for the
    /// others (<c>Normal</c>), between while it fades. Inherited by the items.
    /// </summary>
    public static readonly AttachedProperty<double> ExpansionProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("Expansion", typeof(PhoneLoopingSelector), inherits: true);

    /// <summary>An item's first line: what the item template shows large (the phone's <c>PrimaryText</c>).</summary>
    public static readonly IValueConverter PrimaryText = new LineConverter(primary: true);

    /// <summary>An item's other lines: what it shows small below (the phone's <c>SecondaryText</c>).</summary>
    public static readonly IValueConverter SecondaryText = new LineConverter(primary: false);

    private static readonly ConditionalWeakTable<DateTimePickerPanel, Wheel> Wheels = new();

    /// <summary>The wheels in a window, for the one active wheel of a group.</summary>
    private static readonly List<Wheel> Shown = [];

    static PhoneLoopingSelector()
    {
        IsEnabledProperty.Changed.AddClassHandler<DateTimePickerPanel>((panel, change) =>
        {
            if (Wheels.TryGetValue(panel, out var old))
            {
                Wheels.Remove(panel);
                old.Dispose();
            }

            if (change.GetNewValue<bool>())
                Wheels.Add(panel, new Wheel(panel));
        });

        ItemThemeProperty.Changed.AddClassHandler<DateTimePickerPanel>((panel, _) =>
        {
            if (Wheels.TryGetValue(panel, out var wheel))
                wheel.ThemeItems();
        });
    }

    public static bool GetIsEnabled(DateTimePickerPanel panel) => panel.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DateTimePickerPanel panel, bool value) => panel.SetValue(IsEnabledProperty, value);

    public static ControlTheme? GetItemTheme(DateTimePickerPanel panel) => panel.GetValue(ItemThemeProperty);

    public static void SetItemTheme(DateTimePickerPanel panel, ControlTheme? value) => panel.SetValue(ItemThemeProperty, value);

    public static bool GetIsMoving(Control control) => control.GetValue(IsMovingProperty);

    public static void SetIsMoving(Control control, bool value) => control.SetValue(IsMovingProperty, value);

    public static double GetExpansion(Control control) => control.GetValue(ExpansionProperty);

    public static void SetExpansion(Control control, double value) => control.SetValue(ExpansionProperty, value);

    /// <summary>The wheel <paramref name="panel"/> is, if the behaviour is on: for tests.</summary>
    internal static Wheel? Of(DateTimePickerPanel panel) => Wheels.TryGetValue(panel, out var wheel) ? wheel : null;

    /// <summary>One panel's wheel.</summary>
    internal sealed class Wheel : IDisposable
    {
        private readonly DateTimePickerPanel _panel;
        private readonly WheelGestureRecognizer _recognizer;

        private PhoneLoopingSelectorMotion? _motion;
        private TimeSpan _motionStart;
        private IDisposable? _frame;
        private int _generation;

        private double _dragFrom;
        private int _way;

        // Where this last put the panel, to see the panel's owner move it; a move this held with a press; the mouse
        // wheel's steps short of a notch.
        private double _lastSet;
        private bool _held;
        private double _notches;

        private ControlTheme? _itemTheme;

        private bool _expandedBefore;
        private double _expansionTarget;
        private TimeSpan _expansionLast;
        private IDisposable? _expansionFrame;

        public Wheel(DateTimePickerPanel panel)
        {
            _panel = panel;
            _recognizer = new WheelGestureRecognizer(this);
            panel.GestureRecognizers.Add(_recognizer);
            panel.Children.CollectionChanged += OnChildrenChanged;
            panel.AttachedToVisualTree += OnAttached;
            panel.DetachedFromVisualTree += OnDetached;
            panel.PointerWheelChanged += OnWheel;
            ThemeItems();
            if (panel.IsAttachedToVisualTree())
                Shown.Add(this);
        }

        internal DateTimePickerPanel Panel => _panel;

        /// <summary>Whether a finger is dragging it.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>Whether it is moving after a release, or a notch of the mouse wheel.</summary>
        public bool IsCoasting => _motion is not null;

        /// <summary>The move under way, if any.</summary>
        internal PhoneLoopingSelectorMotion? Motion => _motion;

        /// <summary>Whether this is the active wheel of its group.</summary>
        public bool IsActive => _expansionTarget == 1;

        private double Pitch => _panel.ItemHeight;

        private int Count => (int)Math.Ceiling((_panel.MaximumValue - _panel.MinimumValue + 1) / (double)_panel.Increment);

        private double Offset => _panel.Offset.Y;

        public void Dispose()
        {
            Settle();
            StopExpansion();
            _panel.GestureRecognizers.Remove(_recognizer);
            _panel.Children.CollectionChanged -= OnChildrenChanged;
            _panel.AttachedToVisualTree -= OnAttached;
            _panel.DetachedFromVisualTree -= OnDetached;
            _panel.PointerWheelChanged -= OnWheel;
            foreach (var child in _panel.Children)
                Unhook(child);
            // Set back rather than cleared: this can run while the panel's theme is being unset (spike 6).
            _panel.SetValue(IsMovingProperty, false);
            _panel.SetValue(ExpansionProperty, 0d);
            Shown.Remove(this);
        }

        // ---- The items ----

        public void ThemeItems()
        {
            foreach (var child in _panel.Children)
                Hook(child);
            _itemTheme = GetItemTheme(_panel);
        }

        private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // The panel moves its items round as it scrolls, most frames: they are the same items, hooked already.
            if (e.Action == NotifyCollectionChangedAction.Move)
                return;
            if (e.OldItems is not null)
                foreach (Control child in e.OldItems)
                    Unhook(child);
            if (e.NewItems is not null)
                foreach (Control child in e.NewItems)
                    Hook(child);
        }

        private void Hook(Control child)
        {
            if (child.Theme == _itemTheme)
                child.ClearValue(StyledElement.ThemeProperty);
            if (GetItemTheme(_panel) is { } theme)
                child.Theme = theme;
            child.RemoveHandler(InputElement.TappedEvent, OnItemTapped);
            child.AddHandler(InputElement.TappedEvent, OnItemTapped);
        }

        /// <summary>The item is Avalonia's again: no tap handler, and the theme it had before, if this gave it one.</summary>
        private void Unhook(Control child)
        {
            child.RemoveHandler(InputElement.TappedEvent, OnItemTapped);
            if (_itemTheme is not null && child.Theme == _itemTheme)
                child.ClearValue(StyledElement.ThemeProperty);
        }

        /// <summary>The phone's tap selects nothing and moves nothing: it only makes the wheel the active one.</summary>
        private void OnItemTapped(object? sender, TappedEventArgs e)
        {
            e.Handled = true;
            Activate();
        }

        // ---- The finger ----

        /// <summary>A finger came down: a move under way stops where it is, within the frame, as the phone's does.</summary>
        public void Press()
        {
            if (_motion is null)
                return;
            _way = Math.Sign(_motion.Target - Offset);
            StopMotion();
            _held = true;
        }

        /// <summary>The finger has moved far enough up or down: the drag begins, from where the items are.</summary>
        public void BeginDrag()
        {
            StopMotion();
            _held = false;
            IsDragging = true;
            _way = 0;
            _dragFrom = Offset;
            _lastSet = Offset;
            _panel.SetValue(IsMovingProperty, true);
            Activate();
        }

        /// <summary>The finger is <paramref name="travel"/> view pixels above where the drag began (below is negative).</summary>
        public void Drag(double travel)
        {
            if (!IsDragging)
                return;

            // The panel's owner moved it under the finger (a presenter changing its range): go on from there.
            _dragFrom += Offset - _lastSet;
            var offset = Clamp(_dragFrom + travel);
            if (offset != Offset)
                _way = Math.Sign(offset - Offset);
            SetOffset(offset);
        }

        /// <summary>
        /// The finger has let go, moving at <paramref name="velocity"/> view pixels a millisecond (up positive), or a
        /// finger that held a move has lifted (no speed): on to the item the release decides.
        /// </summary>
        public void Release(double velocity)
        {
            IsDragging = false;
            _held = false;
            var (min, max) = Range();
            Start(PhoneLoopingSelectorMotion.Release(Offset, velocity, _way, Pitch, min, max));
        }

        /// <summary>A finger lifted with no drag: the wheel is the active one, and a move it held goes on to an item.</summary>
        public void Lift()
        {
            Activate();
            if (_held || (!IsCoasting && !IsAtRest()))
                Release(0);
        }

        /// <summary>The finger was taken before it dragged: a move it held goes on to an item.</summary>
        public void LetGo()
        {
            if (_held)
                Release(0);
        }

        /// <summary>
        /// The mouse wheel: an item a notch, coasting on to it (the phone has no mouse; a desktop does). A finer wheel's
        /// small steps add up to a notch. At a wheel's end the scroll is left to what holds it.
        /// </summary>
        private void OnWheel(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y == 0 || IsDragging)
                return;
            var from = _motion?.Target ?? Math.Round(Offset / Pitch) * Pitch;
            var (min, max) = Range();
            if (Math.Clamp(from - Math.Sign(e.Delta.Y) * Pitch, min, max) == from)
            {
                _notches = 0;
                return;
            }

            _notches += e.Delta.Y;
            var notches = Math.Truncate(_notches);
            _notches -= notches;
            var target = Math.Clamp(from - notches * Pitch, min, max);
            e.Handled = true;
            if (notches == 0)
                return;
            Activate();
            Start(PhoneLoopingSelectorMotion.CoastTo(Offset, target));
        }

        // ---- The motion ----

        private void Start(PhoneLoopingSelectorMotion? motion)
        {
            StopMotion();
            if (motion is null)
            {
                Rest(Offset);
                return;
            }

            _motion = motion;
            _motionStart = CornflowerClock.Now;
            _lastSet = Offset;
            _panel.SetValue(IsMovingProperty, true);
            var generation = _generation;
            _frame = CornflowerClock.RequestFrame(_panel, () => Step(generation));
        }

        private void Step(int generation)
        {
            if (generation != _generation || _motion is not { } motion)
                return;
            // The panel's owner moved it under the move (a presenter changing its range): it rests where the panel is.
            if (Offset != _lastSet)
            {
                StopMotion();
                Rest(Offset);
                return;
            }

            var elapsed = CornflowerClock.Now - _motionStart;
            if (elapsed >= motion.Duration)
            {
                StopMotion();
                Rest(motion.Target);
                return;
            }

            SetOffset(motion.OffsetAt(elapsed));
            if (generation != _generation)
                return;
            _frame = CornflowerClock.RequestFrame(_panel, () => Step(generation));
        }

        private void StopMotion()
        {
            _frame?.Dispose();
            _frame = null;
            _motion = null;
            _generation++;
        }

        /// <summary>At rest on the item at <paramref name="offset"/>: it is selected, and shows as selected.</summary>
        private void Rest(double offset)
        {
            _held = false;
            var count = Count;
            var index = (int)Math.Round(offset / Pitch);
            index = _panel.ShouldLoop ? ((index % count) + count) % count : Math.Clamp(index, 0, count - 1);
            _panel.SelectedValue = _panel.MinimumValue + index * _panel.Increment;
            _panel.SetValue(IsMovingProperty, false);
        }

        /// <summary>Ends a drag or a move at once, on the item it was nearest or going to.</summary>
        private void Settle()
        {
            if (_motion is { } motion)
            {
                StopMotion();
                Rest(motion.Target);
            }
            else if (IsDragging || _held)
            {
                IsDragging = false;
                Rest(Offset);
            }
        }

        private bool IsAtRest() => Math.Abs(Offset / Pitch - Math.Round(Offset / Pitch)) < 1e-9;

        private void SetOffset(double offset)
        {
            if (offset != Offset)
                _panel.Offset = new Vector(0, offset);

            // What was asked for, so a jump the panel made of it, or its owner made inside it, shows next time.
            _lastSet = offset;
        }

        /// <summary>Where the offset can go: anywhere for a looping wheel, the first item to the last for one that doesn't.</summary>
        private (double Min, double Max) Range() =>
            _panel.ShouldLoop ? (double.NegativeInfinity, double.PositiveInfinity) : (0, (Count - 1) * Pitch);

        private double Clamp(double offset)
        {
            var (min, max) = Range();
            return Math.Clamp(offset, min, max);
        }

        // ---- Which wheel is active ----

        /// <summary>
        /// Makes this the active wheel of its group (the wheels in the same template, or with none, the same window):
        /// its items fade in, and the others' fade out.
        /// </summary>
        public void Activate()
        {
            foreach (var other in Shown.ToArray())
            {
                if (other != this && other.Group == Group)
                    other.Expand(false);
            }

            Expand(true);
        }

        private object? Group => (object?)_panel.TemplatedParent ?? TopLevel.GetTopLevel(_panel);

        private void Expand(bool expand)
        {
            var target = expand ? 1d : 0d;
            if (_expansionTarget == target && (_expansionFrame is not null || GetExpansion(_panel) == target))
                return;
            _expansionTarget = target;

            // The first time a wheel expands it has no state to come from, and is there at once.
            if (expand && !_expandedBefore)
            {
                _expandedBefore = true;
                StopExpansion();
                _panel.SetValue(ExpansionProperty, 1d);
                return;
            }

            if (_expansionFrame is null)
            {
                _expansionLast = CornflowerClock.Now;
                _expansionFrame = CornflowerClock.RequestFrame(_panel, StepExpansion);
            }
        }

        private void StepExpansion()
        {
            _expansionFrame = null;
            var now = CornflowerClock.Now;
            var step = (now - _expansionLast) / ExpansionTime;
            _expansionLast = now;
            var value = GetExpansion(_panel);
            value = value < _expansionTarget ? Math.Min(_expansionTarget, value + step) : Math.Max(_expansionTarget, value - step);
            _panel.SetValue(ExpansionProperty, value);
            if (value != _expansionTarget)
                _expansionFrame = CornflowerClock.RequestFrame(_panel, StepExpansion);
        }

        private void StopExpansion()
        {
            _expansionFrame?.Dispose();
            _expansionFrame = null;
        }

        // ---- The window ----

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (!Shown.Contains(this))
                Shown.Add(this);
        }

        /// <summary>Out of the window there are no frames: a move ends where it was going, a fade where it was going.</summary>
        /// <remarks>
        /// A flyout that closes takes its wheels out of the window and can show them again: they come back as the phone
        /// opens its wheels, none active, each with no state, and no finger on them.
        /// </remarks>
        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Shown.Remove(this);
            _recognizer.Cancel();
            Settle();
            StopExpansion();
            _expansionTarget = 0;
            _expandedBefore = false;
            _panel.SetValue(ExpansionProperty, 0d);
        }
    }

    /// <summary>
    /// A wheel's drag: a touch or a pen that moves up or down further than Avalonia's own
    /// <see cref="ScrollGestureRecognizer"/> waits for, and more up or down than sideways, is the wheel's from then on.
    /// A press on a moving wheel holds it. Touch and pen only, as Avalonia's recogniser; a mouse turns the wheel by its
    /// wheel.
    /// </summary>
    internal sealed class WheelGestureRecognizer : GestureRecognizer
    {
        /// <summary>The distance a finger moves before it is a drag: Avalonia's <see cref="ScrollGestureRecognizer"/>'s.</summary>
        internal static readonly double StartDistance = new ScrollGestureRecognizer().ScrollStartDistance;

        /// <summary>The moves the release's speed is read over.</summary>
        private static readonly TimeSpan VelocityWindow = TimeSpan.FromMilliseconds(50);

        private readonly Wheel _wheel;
        private readonly List<(TimeSpan Time, double Y)> _moves = [];
        private IPointer? _tracking;
        private Point _press;
        private double _from;
        private bool _dragging;

        public WheelGestureRecognizer(Wheel wheel) => _wheel = wheel;

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (_tracking is not null || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen))
                return;
            _tracking = e.Pointer;
            _press = e.GetPosition(_wheel.Panel);
            _dragging = false;
            _moves.Clear();
            _wheel.Press();
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (e.Pointer != _tracking)
                return;

            var point = e.GetPosition(_wheel.Panel);
            if (!_dragging)
            {
                var delta = point - _press;
                if (Math.Abs(delta.Y) <= StartDistance || Math.Abs(delta.Y) <= Math.Abs(delta.X))
                    return;
                _dragging = true;
                _from = point.Y;
                Capture(e.Pointer);
                _wheel.BeginDrag();
            }

            Track(point.Y);
            _wheel.Drag(_from - point.Y);
            e.Handled = true;
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (e.Pointer != _tracking)
                return;

            if (_dragging)
            {
                Track(e.GetPosition(_wheel.Panel).Y);
                _wheel.Release(Velocity());
                e.Handled = true;
            }
            else
            {
                _wheel.Lift();
            }

            Reset();
        }

        protected override void PointerCaptureLost(IPointer pointer)
        {
            if (pointer != _tracking)
                return;
            if (_dragging)
                _wheel.Release(0);
            else
                _wheel.LetGo();
            Reset();
        }

        private void Track(double y)
        {
            var now = CornflowerClock.Now;
            _moves.Add((now, y));
            var stale = 0;
            while (now - _moves[stale].Time > VelocityWindow) stale++;
            _moves.RemoveRange(0, stale);
        }

        /// <summary>View pixels a millisecond, up positive, over the last moves; 0 if they all came at one time.</summary>
        private double Velocity()
        {
            if (_moves.Count < 2) return 0;
            var first = _moves[0];
            var last = _moves[^1];
            var ms = (last.Time - first.Time).TotalMilliseconds;
            return ms > 0 ? (first.Y - last.Y) / ms : 0;
        }

        /// <summary>Forgets the finger: its wheel left the window under it.</summary>
        public void Cancel() => Reset();

        private void Reset()
        {
            _tracking = null;
            _dragging = false;
            _moves.Clear();
        }
    }

    /// <summary>The first line of an item's text, or the rest.</summary>
    private sealed class LineConverter(bool primary) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value?.ToString() ?? "";
            var newline = text.IndexOf('\n');
            if (newline < 0)
                return primary ? text : "";
            return primary ? text[..newline] : text[(newline + 1)..];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
