using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's AutoSuggestBox list on Avalonia's <see cref="AutoCompleteBox"/>, turned on by the AutoCompleteBox theme
/// (<c>Controls/AutoCompleteBox.axaml</c>): where the list hangs, how tall it may be, and what closes it, as #150 read
/// them (tools/refapp/README.md, "AutoSuggestBox", and docs/controls/AutoSuggestBox/PLAN.md).
/// </summary>
/// <remarks>
/// <para><b>Where.</b> The list's top border lies on the box's bottom border: as the list opens, the template's
/// <c>PART_Popup</c> is moved up by the <c>PART_TextBox</c>'s bottom border (2.5), so the list's top is the box's bottom
/// less 2.5 (63.33 on the phone, its box from 26.67 to 65.83).</para>
/// <para><b>How tall.</b> As tall as its suggestions, up to the room below the box: down to 2.5 above the keyboard's top
/// while the keyboard is up (twelve suggestions run to 450.83, the keyboard's top 453.33), or above the window's bottom,
/// inside its safe area, when there is none; and never over <see cref="AutoCompleteBox.MaxDropDownHeight"/> (WinRT's
/// <c>MaxSuggestionListHeight</c>). Worked out as it opens, and again while it is open when the keyboard, the window,
/// its safe area or the page's layout changes, the list placed again under the box if the page moved it.</para>
/// <para><b>Back and the keyboard.</b> On the phone one Back hides the keyboard, closes the list and takes the focus from
/// the box, in one frame, and the next Back reaches the app (#150). Here the phone popups' Back handler
/// (<see cref="PhonePopupHost.TakeBackFirst"/>, <see cref="PhonePopupDismissal.BackTakesFocusFirst"/>) does it for a
/// box that has the focus, before any popup: it closes the list, clears the focus and marks Back handled, so neither a
/// popup the box is in nor the page's <c>NavigationPage</c> takes it. On Android the keyboard takes the first Back
/// itself and Avalonia never sees it; so the keyboard going down while the box has the focus does the same.</para>
/// <para><b>A tap on a suggestion</b> closes the list and leaves the focus in the box, so the keyboard stays, as on the
/// phone: the template's list and its items take no focus, and the list is closed here as the release selects.</para>
/// <para>The page moves up to put the box at the window's top while the keyboard is up, as the phone's does
/// (<see cref="PhoneKeyboardPan"/>, the TextBox part's); the list is hung and sized again as it moves.</para>
/// </remarks>
internal static class PhoneSuggestionList
{
    /// <summary>Set by the AutoCompleteBox theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<AutoCompleteBox, bool>("IsEnabled", typeof(PhoneSuggestionList));

    /// <summary>The list's bottom border stops this far above the keyboard's top, or the window's bottom (#150: 450.83 against 453.33).</summary>
    internal const double KeyboardGap = 2.5;

    /// <summary>
    /// The list's least height, one suggestion's (#150: 53.75). A keyboard that leaves less is passed over; with less
    /// room than this at all, the list closes rather than stay open as a sliver.
    /// </summary>
    internal const double OneSuggestion = 53.75;

    /// <summary>
    /// The platform's keyboard of a top level: Avalonia's <see cref="TopLevel.InputPane"/>, or none. A test replaces it,
    /// as no headless top level has one.
    /// </summary>
    internal static Func<TopLevel, IPhoneKeyboard?> KeyboardOf = top => top.InputPane is { } pane ? new PaneKeyboard(pane) : null;

    private static readonly AttachedProperty<Hold?> HoldProperty =
        AvaloniaProperty.RegisterAttached<AutoCompleteBox, Hold?>("Hold", typeof(PhoneSuggestionList));

    static PhoneSuggestionList()
    {
        AutoCompleteBox.MaxDropDownHeightProperty.Changed.AddClassHandler<AutoCompleteBox>((box, _) => box.GetValue(HoldProperty)?.Replace());
        IsEnabledProperty.Changed.AddClassHandler<AutoCompleteBox>((box, change) =>
        {
            box.GetValue(HoldProperty)?.Dispose();
            box.ClearValue(HoldProperty);
            if (change.GetNewValue<bool>())
                box.SetValue(HoldProperty, new Hold(box));
        });
        PhonePopupHost.TakeBackFirst(TakesBack);
    }

    public static bool GetIsEnabled(AutoCompleteBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(AutoCompleteBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// Back while a box of ours has the focus, in the topmost popup if one is open: close its list and take its focus;
    /// Back is taken. A box under an open popup leaves Back to the popup.
    /// </summary>
    private static bool TakesBack(TopLevel top, PhonePopupHost? over)
    {
        if (BoxWithFocus(top) is not { } box || (over is not null && !over.Holds(box)))
            return false;
        LetGo(box);
        return true;
    }

    /// <summary>The box of ours that has the focus in the top level, if one has.</summary>
    internal static AutoCompleteBox? BoxWithFocus(TopLevel top) =>
        (top.FocusManager?.GetFocusedElement() as Visual)?.GetSelfAndVisualAncestors()
            .OfType<AutoCompleteBox>().FirstOrDefault(GetIsEnabled);

    /// <summary>
    /// What the phone does on Back or when its keyboard goes: the box loses the focus, and so its list closes (Avalonia's
    /// box closes it as it loses the focus).
    /// </summary>
    private static void LetGo(AutoCompleteBox box)
    {
        TopLevel.GetTopLevel(box)?.FocusManager?.Focus(null);
    }

    /// <summary>One box's hold on its template's parts, its window and the keyboard.</summary>
    private sealed class Hold : IDisposable
    {
        private readonly AutoCompleteBox _box;
        private Popup? _popup;
        private TextBox? _textBox;
        private Control? _list;
        private Point? _placedAt;
        private TopLevel? _top;
        private IPhoneKeyboard? _keyboard;
        private bool _keyboardWasOpen;
        private IDisposable? _clientSize;

        public Hold(AutoCompleteBox box)
        {
            _box = box;
            box.TemplateApplied += OnTemplateApplied;
            box.AttachedToVisualTree += OnAttached;
            box.DetachedFromVisualTree += OnDetached;
            if (TopLevel.GetTopLevel(box) is not null)
                Attach();
            // Turned on after the template was applied (a theme swapped in): its parts as they are.
            var parts = box.GetVisualDescendants().OfType<Control>().Where(c => c.TemplatedParent == box).ToList();
            if (parts.Count > 0)
                Take(parts.OfType<Popup>().FirstOrDefault(p => p.Name == "PART_Popup"),
                    parts.OfType<TextBox>().FirstOrDefault(t => t.Name == "PART_TextBox"));
        }

        /// <summary>The list's room again, for a new <see cref="AutoCompleteBox.MaxDropDownHeight"/>.</summary>
        public void Replace() => Place();

        public void Dispose()
        {
            _box.TemplateApplied -= OnTemplateApplied;
            _box.AttachedToVisualTree -= OnAttached;
            _box.DetachedFromVisualTree -= OnDetached;
            Detach();
            Take(null, null);
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) =>
            Take(e.NameScope.Find<Popup>("PART_Popup"), e.NameScope.Find<TextBox>("PART_TextBox"));

        private void Take(Popup? popup, TextBox? textBox)
        {
            if (_popup is not null)
            {
                _popup.Opened -= OnOpened;
                _popup.Closed -= OnClosed;
            }
            if (_textBox is not null)
                _textBox.LayoutUpdated -= OnLaidOut;
            _list?.RemoveHandler(InputElement.PointerReleasedEvent, OnListReleased);
            _popup = popup;
            _textBox = textBox;
            _list = popup?.Child as Control;
            if (_popup is not null)
            {
                _popup.Opened += OnOpened;
                _popup.Closed += OnClosed;
            }
            _list?.AddHandler(InputElement.PointerReleasedEvent, OnListReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        /// <summary>
        /// A suggestion tapped: the list closes, the box keeping the focus. The template's list and items take no focus,
        /// so Avalonia's own close, when the box loses it, doesn't come; and its commit on release is for a mouse alone.
        /// </summary>
        private void OnListReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left
                && (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault() is { IsSelected: true })
                _box.SetCurrentValue(AutoCompleteBox.IsDropDownOpenProperty, false);
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Attach();

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Detach();

        private void Attach()
        {
            Detach();
            _top = TopLevel.GetTopLevel(_box);
            if (_top is null)
                return;
            _clientSize = _top.GetObservable(TopLevel.ClientSizeProperty).Subscribe(new AnonymousObserver<Size>(_ => Place()));
            if (_top.InsetsManager is { } insets)
                insets.SafeAreaChanged += OnSafeAreaChanged;
            PhoneKeyboardPan.Moved += OnPanMoved;
            _keyboard = KeyboardOf(_top);
            if (_keyboard is not null)
            {
                _keyboardWasOpen = _keyboard.IsOpen;
                _keyboard.Changed += OnKeyboardChanged;
            }
        }

        private void Detach()
        {
            if (_top is not null)
            {
                if (_top.InsetsManager is { } insets)
                    insets.SafeAreaChanged -= OnSafeAreaChanged;
                PhoneKeyboardPan.Moved -= OnPanMoved;
            }
            if (_keyboard is not null)
            {
                _keyboard.Changed -= OnKeyboardChanged;
                _keyboard.Dispose();
            }
            _clientSize?.Dispose();
            _clientSize = null;
            _top = null;
            _keyboard = null;
        }


        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Place();

        /// <summary>The page moved under the open list (a transform, which lays nothing out): hung and sized again.</summary>
        private void OnPanMoved(TopLevel top)
        {
            if (top == _top)
                Place();
        }

        /// <summary>While the list is open, it follows the box as the page lays it out (a scroll, a resize).</summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (_textBox is not null)
                _textBox.LayoutUpdated += OnLaidOut;
            Place();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _placedAt = null;
            if (_textBox is not null)
                _textBox.LayoutUpdated -= OnLaidOut;
        }

        private void OnLaidOut(object? sender, EventArgs e) => Place();

        private void OnKeyboardChanged(object? sender, EventArgs e)
        {
            var open = _keyboard?.IsOpen == true;
            var went = _keyboardWasOpen && !open;
            _keyboardWasOpen = open;
            if (went && _top is not null && BoxWithFocus(_top) == _box)
                LetGo(_box);
            else
                Place();
        }

        /// <summary>
        /// Hangs the open list from the box's bottom border and gives it the room below, down to the keyboard. A keyboard
        /// that covers nothing, or leaves less than one suggestion's room, is passed over (the list then runs under it to
        /// the window's bottom: a box in a popup, which the page's move doesn't take up); with less room than that at all, the
        /// list closes rather than stay open and unseen.
        /// </summary>
        private void Place()
        {
            if (_popup is not { IsOpen: true } || _textBox is null || _top is null || _list is not { } list)
                return;
            if (_textBox.TranslatePoint(new Point(0, _textBox.Bounds.Height), _top) is not { } below)
                return;
            var offset = -_textBox.BorderThickness.Bottom;
            // Avalonia places an open popup again only when its target's own bounds change, not when the page moves the
            // target (a scroll, a margin); an offset change places it again, so the list stays hung from the box.
            if (_placedAt is { } was && was != below)
                _popup.VerticalOffset = offset - 1;
            _popup.VerticalOffset = offset;
            _placedAt = below;
            var top = below.Y + offset;
            var bottom = PhonePopupHost.AreaOf(_top).Bottom;
            if (_keyboard is { IsOpen: true } keyboard && keyboard.Occluded.Height > 0
                && keyboard.Occluded.Top - KeyboardGap - top >= OneSuggestion)
                bottom = Math.Min(bottom, keyboard.Occluded.Top);
            var room = bottom - KeyboardGap - top;
            if (room < OneSuggestion)
            {
                _box.SetCurrentValue(AutoCompleteBox.IsDropDownOpenProperty, false);
                return;
            }
            list.MaxHeight = Math.Min(room, _box.MaxDropDownHeight);
        }
    }

    /// <summary>The platform keyboard, as the list needs it: up or not, what it covers, and when that changes.</summary>
    internal interface IPhoneKeyboard : IDisposable
    {
        bool IsOpen { get; }

        /// <summary>What it covers, in the top level's coordinates.</summary>
        Rect Occluded { get; }

        event EventHandler? Changed;
    }

    /// <summary>Avalonia's input pane as an <see cref="IPhoneKeyboard"/>.</summary>
    private sealed class PaneKeyboard : IPhoneKeyboard
    {
        private readonly IInputPane _pane;
        private Rect _end;

        public PaneKeyboard(IInputPane pane)
        {
            _pane = pane;
            _pane.StateChanged += OnStateChanged;
        }

        public bool IsOpen => _pane.State == InputPaneState.Open;

        /// <summary>
        /// Where an opening keyboard ends, as Avalonia reports the change when the pane's animation starts and Android's
        /// <see cref="IInputPane.OccludedRect"/> can still be empty then (Avonic's <c>KeyboardAssist</c>); else the pane's.
        /// </summary>
        public Rect Occluded => IsOpen && _end.Height > 0 ? _end : _pane.OccludedRect;

        public event EventHandler? Changed;

        public void Dispose() => _pane.StateChanged -= OnStateChanged;

        private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            _end = e.NewState == InputPaneState.Open ? e.EndRect : default;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
