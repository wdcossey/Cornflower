using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's list check mode: WinRT's <c>MultiSelectStates</c>, which Avalonia's <see cref="ListBoxItem"/> doesn't
/// raise, and its <c>CheckboxPressed</c> state. A WinRT ListView in <c>SelectionMode="Multiple"</c> puts every item in
/// <c>ListMultiSelect</c>: its check box slides in from the left and its content 40.5 to the right, and a tap checks or
/// unchecks it. The ListView theme sets <see cref="IsOnProperty"/> on a ListBox whose <see cref="ListBox.SelectionMode"/>
/// has both <see cref="SelectionMode.Multiple"/> and <see cref="SelectionMode.Toggle"/>, and its items inherit it, for
/// the ListViewItem theme's selectors.
/// </summary>
/// <remarks>
/// <para>With <see cref="SelectionMode.Multiple"/> and <see cref="SelectionMode.Toggle"/>, a tap checks or unchecks:
/// WinRT's <c>Multiple</c>. <see cref="SelectionMode.Multiple"/> alone is WinRT's <c>Extended</c>, which the phone shows
/// with no check boxes, a tap selecting one item as in <c>Single</c> (run <c>20261116-0040-dev11</c>, #136): so it isn't
/// the check mode. <see cref="SelectionMode.Toggle"/> alone toggles one item, with no boxes.</para>
/// <para>Inherited, so every container the list holds has it, whether it makes it, reuses it, keeps it out of view or
/// the item is its own container, with no bookkeeping; a theme of the app's own on the list takes it away with Wp81's.
/// A nested Wp81 list sets its own. An item outside a list never has it. It costs the ListView theme one style rule,
/// and no global rule.</para>
/// <para><b>A press on the box</b> (<c>CheckboxPressed</c>, read in run <c>20261116-0040-dev11</c>): the item's own
/// press, the Button's tilt on its content (<see cref="Tilt"/>), is put back at rest in the same input event, and the
/// box tilts instead, by <see cref="TiltModel"/>'s rule about the box's own centre, with the eye the item's: the phone
/// moves the box by the item's offset to the screen's centre, not by its own. On release, or when the pointer is taken
/// (a pan), the box is at rest at once, with no hold and no return, and <see cref="IsBoxPressedProperty"/>, which fills
/// the box with the accent, goes. A press anywhere else on the item is the item's press, as in <c>Single</c>: the
/// content tilts and the box stays flat. A move of the finger doesn't re-aim the box's tilt (not read). The screen's
/// centre is taken into the item's space as <c>TranslatePoint</c> takes it, through any tilt an ancestor shows, which
/// <see cref="Tilt"/> leaves out: a list inside a pressed, tilting control isn't the phone's case. It costs a class
/// handler per pointer press, release and capture lost on an item, which does nothing out of the check mode.</para>
/// </remarks>
internal static class PhoneListMultiSelect
{
    /// <summary>Whether the list is in the check mode: set on a list by the ListView theme, inherited by its items.</summary>
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsOn", typeof(PhoneListMultiSelect), inherits: true);

    /// <summary>Whether an item in the check mode is pressed on its box: WinRT's <c>CheckboxPressed</c>. For the theme's selectors.</summary>
    public static readonly AttachedProperty<bool> IsBoxPressedProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, bool>("IsBoxPressed", typeof(PhoneListMultiSelect));

    /// <summary>The box's press, while there is one.</summary>
    private static readonly AttachedProperty<BoxPress?> PressProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, BoxPress?>("Press", typeof(PhoneListMultiSelect));

    /// <summary>The template's part that the box's press tilts, the phone's name for it.</summary>
    private const string BoxPart = "CheckboxTiltContainer";

    static PhoneListMultiSelect()
    {
        // Bubbling, so after the item's own tilt (a tunnelling handler) has taken the press: it is then put back.
        InputElement.PointerPressedEvent.AddClassHandler<ListBoxItem>(OnPressed, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<ListBoxItem>(OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerCaptureLostEvent.AddClassHandler<ListBoxItem>(OnCaptureLost, handledEventsToo: true);
        IsOnProperty.Changed.AddClassHandler<ListBoxItem>((item, e) =>
        {
            if (!e.GetNewValue<bool>()) Release(item);
        });
    }

    public static bool GetIsOn(Control control) => control.GetValue(IsOnProperty);

    /// <summary>The box's tilt showing on <paramref name="item"/> now, or <see cref="TiltState.Rest"/>. For tests.</summary>
    internal static TiltState BoxTilt(ListBoxItem item) => item.GetValue(PressProperty)?.State ?? TiltState.Rest;

    /// <summary>The item's own box, the part its <c>CheckboxPressed</c> tilts.</summary>
    internal static Control? BoxOf(ListBoxItem item) =>
        item.GetVisualDescendants().OfType<Control>()
            .FirstOrDefault(c => c.Name == BoxPart && ReferenceEquals(c.TemplatedParent, item));

    /// <summary>
    /// A press on the item: on its box, in the check mode, by a touch, a pen or the left button, from inside the item's
    /// own tree (not a popup it owns) and the first. Anything else is left to the item.
    /// </summary>
    private static void OnPressed(ListBoxItem item, PointerPressedEventArgs e)
    {
        if (!GetIsOn(item) || !item.IsEffectivelyEnabled || item.IsSet(PressProperty) ||
            !e.GetCurrentPoint(item).Properties.IsLeftButtonPressed ||
            e.Source is not Visual hit || !(ReferenceEquals(hit, item) || item.IsVisualAncestorOf(hit)) ||
            BoxOf(item) is not { } box || box.GetVisualParent() is not { } parent ||
            box.Bounds is not { Width: > 0, Height: > 0 } bounds)
            return;

        var at = e.GetPosition(parent);
        if (!bounds.Contains(at))
            return;

        // The item's tilt took the press first; it goes back to rest with nothing drawn, its press forgotten.
        if (Tilt.GetIsEnabled(item))
            item.SetValue(Tilt.IsEnabledProperty, false, BindingPriority.Animation)?.Dispose();

        var press = new BoxPress(item, box, e.Pointer, at - bounds.Position);
        item.SetValue(PressProperty, press);
        item.SetValue(IsBoxPressedProperty, true);
        press.Step();
    }

    private static void OnReleased(ListBoxItem item, PointerReleasedEventArgs e)
    {
        if (item.GetValue(PressProperty) is { } press && ReferenceEquals(press.Pointer, e.Pointer))
            Release(item);
    }

    private static void OnCaptureLost(ListBoxItem item, PointerCaptureLostEventArgs e)
    {
        if (item.GetValue(PressProperty) is { } press && ReferenceEquals(press.Pointer, e.Pointer))
            Release(item);
    }

    /// <summary>The box back at rest at once, with no hold and no return, as the phone's <c>CheckboxPressed</c> has none.</summary>
    private static void Release(ListBoxItem item)
    {
        if (item.GetValue(PressProperty) is not { } press)
            return;

        press.Stop();
        item.ClearValue(PressProperty);
        item.ClearValue(IsBoxPressedProperty);
    }

    /// <summary>One press on an item's box: its frames, and the tilt drawn on the box.</summary>
    private sealed class BoxPress
    {
        private readonly ListBoxItem _item;
        private readonly Control _box;
        private readonly Point _press;                          // in the box's own space
        private readonly MatrixTransform _transform = new();
        private IDisposable? _shown;
        private IDisposable? _pending;
        private int _steps;

        public BoxPress(ListBoxItem item, Control box, IPointer pointer, Point press)
        {
            _item = item;
            _box = box;
            _press = press;
            Pointer = pointer;
            _item.DetachedFromVisualTree += OnDetached;
        }

        public IPointer Pointer { get; }

        public TiltState State { get; private set; }

        /// <summary>
        /// One frame: the tilt for the finger as the box's current tilt shows it, stepping each frame until it settles,
        /// or for <see cref="TiltModel.MaxSteps"/> frames, as the item's does (<see cref="Tilt"/>).
        /// </summary>
        public void Step()
        {
            _pending = null;
            var size = _box.Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0 || Eye(size) is not { } eye)
                return;

            var last = ++_steps >= TiltModel.MaxSteps;
            var next = TiltModel.Step(last ? TiltState.Rest : State, size, _press, eye);
            var moved = Math.Abs(next.RotationX - State.RotationX) + Math.Abs(next.RotationY - State.RotationY) +
                        Math.Abs(next.OffsetZ - State.OffsetZ);
            State = next;

            // The model's matrix is about the box's top left; Avalonia draws a RenderTransform about its origin.
            var origin = _box.RenderTransformOrigin.ToPixels(size);
            _transform.Matrix = Matrix.CreateTranslation(origin) * TiltModel.ToMatrix(next, size, eye) *
                                Matrix.CreateTranslation(-origin);
            _shown ??= _box.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);

            if (moved > TiltModel.Settled && !last)
                _pending = CornflowerClock.RequestFrame(_item, Step);
        }

        /// <summary>The box's own transform back, nothing pending.</summary>
        public void Stop()
        {
            _item.DetachedFromVisualTree -= OnDetached;
            _pending?.Dispose();
            _pending = null;
            _shown?.Dispose();
            _shown = null;
            State = TiltState.Rest;
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Release(_item);

        /// <summary>
        /// The point under the eye, in the box's space: <see cref="TiltModel.Eye"/> for the box's size, but from the
        /// item's centre to the screen's (the <see cref="TopLevel"/>'s), as the phone moves the box by the item's offset.
        /// </summary>
        private Point? Eye(Size size)
        {
            if (TopLevel.GetTopLevel(_item) is not { } top ||
                top.TranslatePoint(new Point(top.ClientSize.Width / 2, top.ClientSize.Height / 2), _item) is not { } centre)
                return null;

            return TiltModel.Eye(size, centre - new Rect(_item.Bounds.Size).Center);
        }
    }
}
