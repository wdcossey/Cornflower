using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// WinRT's <c>ScrollingIndicatorMode</c>: which scroll indicator a ScrollViewer shows, and which a ScrollBar draws.
/// </summary>
internal enum PhoneIndicatorMode
{
    /// <summary>No indicator: the phone's <c>NoIndicator</c> state.</summary>
    None,

    /// <summary>The indicator a touch shows: the phone's <c>TouchIndicator</c> state.</summary>
    TouchIndicator,

    /// <summary>The indicator a mouse or a pen shows: the phone's <c>MouseIndicator</c> state.</summary>
    MouseIndicator,
}

/// <summary>
/// The 8.1 ScrollViewer's <c>ScrollingIndicatorStates</c>, chosen from the pointer type as WinRT's ScrollViewer chooses
/// them, on Avalonia's own ScrollViewer. Turned on by the Wp81 ScrollViewer theme's own setter. It sets
/// <see cref="IndicatorModeProperty"/> on the ScrollViewer; the theme's styles pass it to the template's ScrollBars, as
/// the phone's states set the ScrollBars' <c>IndicatorMode</c>, and the ScrollBar theme draws it.
/// </summary>
/// <remarks>
/// <para><b>When.</b> The indicators show whenever the content scrolls (an offset change, once the ScrollViewer is
/// loaded: WinRT's <c>ShowIndicators</c> on a view change), and while a mouse or a pen moves over it. They go again
/// <see cref="StateDuration"/> after they showed, then <see cref="HideKeyTime"/> into the transition to
/// <c>NoIndicator</c>, unless the content scrolled or the mouse moved in between: the phone's template runs the
/// <c>TouchIndicator</c> storyboard for 0.1 s, and its transition to <c>NoIndicator</c> sets <c>None</c> at 0.05 s,
/// and WinRT goes to <c>NoIndicator</c> when the state's storyboard completes. Showing the state it is already in
/// changes nothing, as in WinRT's visual state manager, so during a scroll that changes the offset more often than every
/// 50 ms the indicator stays, and it goes 50 to 150 ms after the last change.</para>
/// <para><b>Which.</b> The mouse's, once a mouse or a pen has entered the ScrollViewer or moved over it (a move that
/// reaches it unhandled, so an outer ScrollViewer shows its own too, as WinRT's routed <c>PointerMoved</c> does), until
/// a touch presses or a key goes down on it; the touch's otherwise (WinRT's <c>m_preferMouseIndicators</c>). A touch
/// never shows an indicator by itself: only the scroll it makes does.</para>
/// <para>The mouse's is a deliberate difference. The phone's ScrollViewer template sets nothing in its
/// <c>MouseIndicator</c> state, so on the phone a mouse would see no indicator at all. Ours passes the state on, so the
/// ScrollBar draws its own <c>MouseIndicator</c> look, and lasts as the touch's does, as the template gives the mouse
/// no timeline of its own.</para>
/// <para>It reads the ScrollViewer's offset and pointer and key events, sets only <see cref="IndicatorModeProperty"/>,
/// and adds no global rule. The time is <see cref="CornflowerClock"/>'s.</para>
/// </remarks>
internal static class PhoneScrollIndicators
{
    /// <summary>How long a shown state lasts before it goes: the <c>TouchIndicator</c> storyboard's 0.1 s (scrollviewer.json).</summary>
    internal static readonly TimeSpan StateDuration = TimeSpan.FromMilliseconds(100);

    /// <summary>When, into the transition to <c>NoIndicator</c>, the ScrollBars' mode is <c>None</c>: 0.05 s (scrollviewer.json).</summary>
    internal static readonly TimeSpan HideKeyTime = TimeSpan.FromMilliseconds(50);

    /// <summary>Set by the Wp81 ScrollViewer theme: the indicators follow the scroll and the pointer.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("IsEnabled", typeof(PhoneScrollIndicators));

    /// <summary>
    /// On a ScrollViewer, its <c>ScrollingIndicatorStates</c> state, which this sets; on a ScrollBar, WinRT's
    /// <c>ScrollBar.IndicatorMode</c>, which the ScrollViewer theme's styles set and the ScrollBar theme draws. A
    /// ScrollBar's default is <see cref="PhoneIndicatorMode.MouseIndicator"/>, so one an app places on its own shows.
    /// </summary>
    public static readonly AttachedProperty<PhoneIndicatorMode> IndicatorModeProperty =
        AvaloniaProperty.RegisterAttached<Control, PhoneIndicatorMode>("IndicatorMode", typeof(PhoneScrollIndicators));

    private static readonly ConditionalWeakTable<ScrollViewer, Indicators> Watched = new();

    static PhoneScrollIndicators()
    {
        // A ScrollBar on its own, which no ScrollViewer sets, draws the mouse's look: shown (PLAN.md, "A ScrollBar on
        // its own"). The ScrollViewer theme sets its own bars' mode, None at rest.
        IndicatorModeProperty.OverrideDefaultValue<ScrollBar>(PhoneIndicatorMode.MouseIndicator);

        IsEnabledProperty.Changed.AddClassHandler<ScrollViewer>((viewer, change) =>
        {
            if (Watched.TryGetValue(viewer, out var old))
            {
                Watched.Remove(viewer);
                old.Dispose();

                // A theme going unsets its values one by one, and a handler that takes a value away inside that pass
                // can stop it (spike 6). Hide sets None rather than clearing the value, so it takes nothing away.
                old.Hide();
            }

            if (change.GetNewValue<bool>())
                Watched.Add(viewer, new Indicators(viewer));
        });

        ScrollViewer.OffsetProperty.Changed.AddClassHandler<ScrollViewer>((viewer, _) =>
        {
            if (Watched.TryGetValue(viewer, out var indicators))
                indicators.OnScrolled();
        });
    }

    public static bool GetIsEnabled(ScrollViewer viewer) => viewer.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ScrollViewer viewer, bool value) => viewer.SetValue(IsEnabledProperty, value);

    public static PhoneIndicatorMode GetIndicatorMode(Control control) => control.GetValue(IndicatorModeProperty);

    public static void SetIndicatorMode(Control control, PhoneIndicatorMode value) => control.SetValue(IndicatorModeProperty, value);

    /// <summary>One ScrollViewer's state, its pointer preference and the timer that ends the state.</summary>
    private sealed class Indicators : IDisposable
    {
        private readonly ScrollViewer _viewer;
        private bool _preferMouse;
        private bool _leaving;
        private IDisposable? _timer;

        public Indicators(ScrollViewer viewer)
        {
            _viewer = viewer;

            // As WinRT's ScrollViewer overrides: the pointer entering it, and a move that reaches it unhandled. A key
            // is taken as it tunnels, before Avalonia's ScrollViewer scrolls for PageUp or PageDown, as WinRT's
            // OnKeyDown clears the mouse's preference before it scrolls. A touch press counts wherever it lands, as
            // WinRT's manipulation starting does.
            viewer.AddHandler(InputElement.PointerEnteredEvent, OnPointer);
            viewer.AddHandler(InputElement.PointerMovedEvent, OnPointer);
            viewer.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            viewer.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            viewer.DetachedFromVisualTree += OnDetached;
        }

        private PhoneIndicatorMode Mode => _viewer.GetValue(IndicatorModeProperty);

        /// <summary>Stops following the ScrollViewer. Its state's value stays until <see cref="Hide"/>.</summary>
        public void Dispose()
        {
            _viewer.RemoveHandler(InputElement.PointerEnteredEvent, OnPointer);
            _viewer.RemoveHandler(InputElement.PointerMovedEvent, OnPointer);
            _viewer.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _viewer.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            _viewer.DetachedFromVisualTree -= OnDetached;
            _timer?.Dispose();
            _timer = null;
        }

        public void OnScrolled()
        {
            if (_viewer.IsLoaded)
                Show(_preferMouse ? PhoneIndicatorMode.MouseIndicator : PhoneIndicatorMode.TouchIndicator);
        }

        private void OnPointer(object? sender, PointerEventArgs e)
        {
            // A move while a control holds the pointer (a mouse dragging a Slider in the content) isn't the mouse over
            // the ScrollViewer: it would keep the page's indicator for the whole drag.
            if (e.Pointer.Type == PointerType.Touch || e.Pointer.Captured is not null)
                return;
            _preferMouse = true;
            Show(PhoneIndicatorMode.MouseIndicator);
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Pointer.Type == PointerType.Touch)
                _preferMouse = false;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) => _preferMouse = false;

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Hide();

        /// <summary>
        /// Goes to <paramref name="mode"/>'s state, unless it is in it already and not leaving it, and starts the
        /// state's timeline.
        /// </summary>
        private void Show(PhoneIndicatorMode mode)
        {
            if (Mode == mode && !_leaving)
                return;

            _timer?.Dispose();
            _leaving = false;
            _viewer.SetValue(IndicatorModeProperty, mode);
            _timer = CornflowerClock.Schedule(Leave, StateDuration);
        }

        /// <summary>The state's storyboard has completed: the transition to <c>NoIndicator</c> starts.</summary>
        private void Leave()
        {
            _leaving = true;
            _timer = CornflowerClock.Schedule(Hide, HideKeyTime);
        }

        /// <summary>The state is <c>NoIndicator</c>.</summary>
        public void Hide()
        {
            _timer?.Dispose();
            _timer = null;
            _leaving = false;
            _viewer.SetValue(IndicatorModeProperty, PhoneIndicatorMode.None);
        }
    }
}
