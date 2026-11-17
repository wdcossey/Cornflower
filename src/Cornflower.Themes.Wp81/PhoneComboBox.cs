using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 phone's two ComboBoxes on Avalonia's own ComboBox, turned on by the Wp81 ComboBox theme's setter: five items
/// or fewer open inline, the box growing in place; six or more open the full-screen <see cref="ListPickerFlyout"/>. By
/// the count, not the room on screen: 5 opened inline and 6 full screen on the 450 × 800 and the 400 × 711 profiles
/// (tools/refapp/README.md, "Where ComboBox flips to full screen", #25).
/// </summary>
/// <remarks>
/// <para><b>Which form.</b> <see cref="IsFullProperty"/> is true with <see cref="FullFrom"/> items or more, kept as the
/// count changes: the template's <c>PresenterStates</c> (Full, or InlineNormal and InlinePlaceholder).</para>
/// <para><b>Inline.</b> The template keeps the items in the box, the selected one showing. <c>IsDropDownOpen</c> going true
/// shows the box open <see cref="InlineOpenDelay"/> later, in one frame (<see cref="IsShownOpenProperty"/>); going false
/// shows it closed at once. That is what the screen shows (#149, "The popups' bands, and the inline ComboBox"): the
/// template's <c>Opened</c> storyboard eases the items' host over 250 ms, but the layout holds the closed size for four
/// frames and jumps open on the fifth, 78 ms after the call; and the box is closed on the first frame after
/// <c>IsDropDownOpen</c> goes false, before the <c>Closed</c> storyboard's values come. While shown open, a tap on an item
/// selects it and closes the box (Avalonia's own selection on the release), so a press on the items is taken before
/// the ComboBox, which would close the box on the press. A press outside the box closes it and goes on to what it was on;
/// Back closes it before any popup under it or the page sees Back.</para>
/// <para><b>Full screen.</b> <c>IsDropDownOpen</c> going true opens a <see cref="ListPickerFlyout"/> at the box, titled
/// with the placeholder text in capitals ("CHOOSE AN ITEM", as #25's dump read it), the selected item in the accent;
/// a pick sets <c>SelectedIndex</c>, and the picker's close sets <c>IsDropDownOpen</c> false.</para>
/// <para>Avalonia's <c>DropDownOpened</c> and <c>DropDownClosed</c> come from the template's <c>PART_Popup</c>, which
/// opens empty and draws nothing.</para>
/// </remarks>
internal static class PhoneComboBox
{
    /// <summary>The count of items from which a ComboBox opens full screen: 6 (#25).</summary>
    internal const int FullFrom = 6;

    /// <summary>
    /// How long after <c>IsDropDownOpen</c> the inline box shows open: five ticks of the 64 Hz clock, 78.1 ms of
    /// RenderingTime (#149, run 20261116-1835-dev11).
    /// </summary>
    internal static readonly TimeSpan InlineOpenDelay = TimeSpan.FromMilliseconds(5 * 15.625);

    /// <summary>Set by the Wp81 ComboBox theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("IsEnabled", typeof(PhoneComboBox));

    /// <summary>Whether the ComboBox has <see cref="FullFrom"/> items or more: its full-screen form. For the template.</summary>
    public static readonly AttachedProperty<bool> IsFullProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("IsFull", typeof(PhoneComboBox));

    /// <summary>
    /// Whether the ComboBox has a header (<see cref="ControlHeader"/>): without one, the phone's header takes its -4.5
    /// margin with it as it collapses. For the template.
    /// </summary>
    public static readonly AttachedProperty<bool> HasHeaderProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("HasHeader", typeof(PhoneComboBox));

    /// <summary>
    /// Whether the inline box shows open: its <c>DropDownStates.Opened</c>. Inherited, so its items see it (the
    /// selected one is drawn in the accent only while open). For the template.
    /// </summary>
    public static readonly AttachedProperty<bool> IsShownOpenProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsShownOpen", typeof(PhoneComboBox), inherits: true);

    private static readonly ConditionalWeakTable<ComboBox, Opener> Openers = new();

    /// <summary>The inline boxes shown open, for Back.</summary>
    private static readonly List<Opener> OpenInline = [];

    static PhoneComboBox()
    {
        IsEnabledProperty.Changed.AddClassHandler<ComboBox>((combo, change) =>
        {
            if (Openers.TryGetValue(combo, out var old))
            {
                old.Detach();
                Openers.Remove(combo);
            }

            if (change.GetNewValue<bool>())
            {
                var opener = new Opener(combo);
                Openers.Add(combo, opener);
                opener.Attach();
            }
        });
        PhonePopupHost.TakeBackFirst(TakesBack);
    }

    public static bool GetIsEnabled(ComboBox combo) => combo.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ComboBox combo, bool value) => combo.SetValue(IsEnabledProperty, value);

    public static bool GetIsFull(ComboBox combo) => combo.GetValue(IsFullProperty);

    public static bool GetHasHeader(ComboBox combo) => combo.GetValue(HasHeaderProperty);

    public static bool GetIsShownOpen(Control control) => control.GetValue(IsShownOpenProperty);

    /// <summary>The picker a full-screen ComboBox opens, once it has opened one. For the tests.</summary>
    internal static ListPickerFlyout? PickerOf(ComboBox combo) => Openers.TryGetValue(combo, out var opener) ? opener.Picker : null;

    /// <summary>
    /// Back: an inline box shown open in the window, not under the popup Back would close, closes, and Back stops there.
    /// </summary>
    private static bool TakesBack(TopLevel top, PhonePopupHost? host)
    {
        for (var i = OpenInline.Count - 1; i >= 0; i--)
        {
            var combo = OpenInline[i].Combo;
            if (TopLevel.GetTopLevel(combo) != top || (host is not null && !host.Holds(combo)))
                continue;
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
            return true;
        }

        return false;
    }

    /// <summary>Opens and closes one ComboBox the phone's way.</summary>
    private sealed class Opener(ComboBox combo)
    {
        private TemplateParts? _parts;
        private Control? _root;
        private Control? _items;
        private IDisposable? _pending;
        private TopLevel? _top;
        private ListPickerFlyout? _picker;
        private ListBox? _pickerList;
        private IDisposable? _accent;
        private Control? _accented;

        public ComboBox Combo => combo;

        public ListPickerFlyout? Picker => _picker;

        public void Attach()
        {
            combo.PropertyChanged += OnChanged;
            combo.SetValue(IsFullProperty, combo.ItemCount >= FullFrom);
            combo.SetValue(HasHeaderProperty, ControlHeader.GetHeader(combo) is not null);
            _parts = TemplateParts.Watch(combo, Hook, Unhook);
            if (combo.IsDropDownOpen)
                Open();
        }

        public void Detach()
        {
            combo.PropertyChanged -= OnChanged;
            _parts?.Dispose();
            Close();
            combo.ClearValue(IsFullProperty);
            combo.ClearValue(HasHeaderProperty);
        }

        private void Hook(Func<string, Control?> find)
        {
            _root = combo.GetVisualChildren().FirstOrDefault() as Control;
            _items = find("ItemsPresenterHost");
            _root?.AddHandler(InputElement.PointerPressedEvent, OnPressed);
        }

        private void Unhook()
        {
            _root?.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _root = null;
            _items = null;
        }

        private void OnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsControl.ItemCountProperty)
                combo.SetValue(IsFullProperty, combo.ItemCount >= FullFrom);
            else if (e.Property == ControlHeader.HeaderProperty)
                combo.SetValue(HasHeaderProperty, e.NewValue is not null);
            else if (e.Property == ComboBox.IsDropDownOpenProperty)
            {
                if (e.GetNewValue<bool>())
                    Open();
                else
                    Close();
            }
        }

        private void Open()
        {
            Close();
            if (combo.GetValue(IsFullProperty))
            {
                OpenPicker();
                return;
            }

            _top = TopLevel.GetTopLevel(combo);
            _top?.AddHandler(InputElement.PointerPressedEvent, OnWindowPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            OpenInline.Add(this);
            _pending = CornflowerClock.Schedule(() =>
            {
                _pending = null;
                if (combo.IsDropDownOpen)
                    combo.SetValue(IsShownOpenProperty, true);
            }, InlineOpenDelay);
        }

        private void Close()
        {
            _pending?.Dispose();
            _pending = null;
            _top?.RemoveHandler(InputElement.PointerPressedEvent, OnWindowPressed);
            _top = null;
            OpenInline.Remove(this);
            combo.ClearValue(IsShownOpenProperty);
            if (_picker is { IsOpen: true } picker)
                picker.Hide();
        }

        /// <summary>
        /// A press on the items of a box shown open is theirs: it is taken here, before the ComboBox, whose own press
        /// would close the box; the item selects on the release.
        /// </summary>
        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (combo.GetValue(IsShownOpenProperty) && _items is { } items && e.Source is Visual source &&
                (source == items || items.IsVisualAncestorOf(source)))
                e.Handled = true;
        }

        /// <summary>A press anywhere off the box closes it, and goes on to what it was on.</summary>
        private void OnWindowPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is Visual source && (source == combo || combo.IsVisualAncestorOf(source)))
                return;
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
        }

        private void OpenPicker()
        {
            if (_picker is null)
            {
                _picker = new ListPickerFlyout();
                _picker.ItemsPicked += (_, _) => combo.SetCurrentValue(ComboBox.SelectedIndexProperty, _picker.SelectedIndex);
                _picker.Opened += (_, _) => FindList();
                _picker.Closed += (_, _) =>
                {
                    Unaccent();
                    combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
                };
            }

            // Items that are controls (a ComboBoxItem in XAML) can't go in a second list: their content goes, or their text.
            var items = combo.Items.Select(item => item switch
            {
                ContentControl { Content: not Control and not null } container => container.Content!,
                Control control => control.ToString() ?? string.Empty,
                _ => item!,
            }).ToList();
            _picker.ItemsSource = items;
            _picker.ItemTemplate = combo.ItemTemplate;
            _picker.DisplayMemberBinding = combo.DisplayMemberBinding;
            _picker.SelectedIndex = combo.SelectedIndex;
            PickerFlyoutBase.SetTitle(_picker, combo.PlaceholderText?.ToUpper(CultureInfo.CurrentCulture));
            _picker.ShowAt(combo);
            if (!_picker.IsOpen)
                combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
        }

        /// <summary>The picker's list, once it has a template: the item selected in the box is drawn in the accent.</summary>
        private void FindList()
        {
            if (_picker?.Popup.Child is not ListPickerFlyoutPresenter { ItemsHost: { } list })
                return;
            if (!ReferenceEquals(list, _pickerList))
            {
                if (_pickerList is not null)
                {
                    _pickerList.ContainerPrepared -= OnContainerPrepared;
                    _pickerList.ContainerClearing -= OnContainerClearing;
                }

                _pickerList = list;
                list.ContainerPrepared += OnContainerPrepared;
                list.ContainerClearing += OnContainerClearing;
            }

            if (list.ContainerFromIndex(combo.SelectedIndex) is { } container)
                Accent(container);
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (_picker is { IsOpen: true } && e.Index == combo.SelectedIndex)
                Accent(e.Container);
        }

        private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            if (e.Container == _accented)
                Unaccent();
        }

        private void Accent(Control container)
        {
            if (container == _accented)
                return;
            Unaccent();
            _accented = container;
            _accent = container.Bind(TemplatedControl.ForegroundProperty,
                container.GetResourceObservable("ComboBoxItemSelectedForegroundThemeBrush"));
        }

        private void Unaccent()
        {
            _accent?.Dispose();
            _accent = null;
            _accented = null;
        }
    }
}
