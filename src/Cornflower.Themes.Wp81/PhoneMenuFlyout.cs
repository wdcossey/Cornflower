using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's MenuFlyout, on Avalonia's <see cref="MenuFlyout"/>: turned on by the MenuFlyoutPresenter theme's own
/// setter, it puts the presenter's popup in a <see cref="PhonePopupHost"/> (the window's overlay layer, the window's
/// width, Back), places the menu against its target, runs the template's opening storyboard on
/// <see cref="CornflowerClock"/>, and gives the presenter's own items the theme's item themes.
/// </summary>
/// <remarks>
/// <para><b>Where it sits</b> (#150, <c>tools/refapp/README.md</c>, "MenuFlyout"): the window's width; directly below its
/// target (the menu's top at the target's bottom) if it fits there by the window's bottom, else directly above it (its
/// bottom at the target's top), kept inside the window; a menu as tall as the window or taller fills it, whatever the
/// target. Placed again as the window or the menu changes size.</para>
/// <para><b>Opening</b> (gap register A8; <c>tools/refapp/README.md</c>, "MenuFlyout, from the app's values"): the
/// template's <c>TopPortrait</c> (the menu above its target) or <c>BottomPortrait</c> (below) storyboard, as written:
/// <c>OuterBorder</c>'s <c>ScaleX</c> 0 to 1 linearly over <see cref="OuterOpenLength"/>, about its top centre, then
/// <c>InnerBorder</c>'s <c>ScaleY</c> 0 to 1 linearly until <see cref="OpenLength"/>, from the edge by the target; until
/// then both borders show only that edge's line. Time 0 is the first frame, which shows the start.</para>
/// <para><b>Closing</b> has no motion (#150): the menu goes at once and <c>Closed</c> comes in the same frame, after an
/// item's <c>Click</c>, a tap outside (Avalonia's light dismiss: the phone's full-window layer), <c>Hide()</c> or Back.
/// The flyout's own closing stands; nothing is held.</para>
/// <para><b>The host</b> is the Flyout's (<see cref="PhonePopupHost"/>), with two things worked round until it takes a
/// popup that has a motion of its own (PR "Proposed for shared files"): its band motion is given a stand-in that is
/// never drawn, so it turns no band of the menu, and Back, which the host turns into its top popup's closing, closes the
/// menu at once from a handler that runs after the host's.</para>
/// <para><b>The items.</b> Avalonia has no <c>MenuFlyoutItem</c>: the items are <see cref="MenuItem"/>s and
/// <see cref="Separator"/>s. The presenter's theme names their themes (<see cref="ItemThemeProperty"/>,
/// <see cref="SeparatorThemeProperty"/>), and they are given to the presenter's own containers at style priority, so a
/// desktop <c>Menu</c>'s items aren't touched and an app's own <c>Theme</c> or <c>ItemContainerTheme</c> wins.</para>
/// <para>A presenter outside a popup, or under a theme that doesn't turn this on, is Avalonia's own.</para>
/// </remarks>
internal static class PhoneMenuFlyout
{
    /// <summary>The <c>OuterBorder</c>'s <c>ScaleX</c> goes from 0 to 1 over this: the storyboard's 0.38 s.</summary>
    internal static readonly TimeSpan OuterOpenLength = TimeSpan.FromMilliseconds(380);

    /// <summary>The <c>InnerBorder</c>'s <c>ScaleY</c> reaches 1, and the borders their rest, at this: the storyboard's 0.5 s.</summary>
    internal static readonly TimeSpan OpenLength = TimeSpan.FromMilliseconds(500);

    /// <summary>Set by Wp81's MenuFlyoutPresenter theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutPresenter, bool>("IsEnabled", typeof(PhoneMenuFlyout));

    /// <summary>The theme the presenter's own <see cref="MenuItem"/>s get: the phone's MenuFlyoutItem.</summary>
    public static readonly AttachedProperty<ControlTheme?> ItemThemeProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutPresenter, ControlTheme?>("ItemTheme", typeof(PhoneMenuFlyout));

    /// <summary>The theme the presenter's own <see cref="Separator"/>s get: the phone's MenuFlyoutSeparator.</summary>
    public static readonly AttachedProperty<ControlTheme?> SeparatorThemeProperty =
        AvaloniaProperty.RegisterAttached<MenuFlyoutPresenter, ControlTheme?>("SeparatorTheme", typeof(PhoneMenuFlyout));

    private static readonly ConditionalWeakTable<MenuFlyoutPresenter, Binder> Binders = new();

    /// <summary>The binders let go of, until they have given their popups back.</summary>
    private static readonly ConditionalWeakTable<MenuFlyoutPresenter, Binder> Leaving = new();

    static PhoneMenuFlyout()
    {
        IsEnabledProperty.Changed.AddClassHandler<MenuFlyoutPresenter>((presenter, change) =>
        {
            if (Binders.TryGetValue(presenter, out var old))
            {
                // From the dispatcher, as PhoneFlyout does: a theme going unsets this first (CLAUDE.md, the Avalonia facts).
                // Turned on again meanwhile, the new binder comes after the old has given the popup back what its host
                // set, so the new host doesn't take those values for the popup's own.
                Binders.Remove(presenter);
                Leaving.Add(presenter, old);
                Dispatcher.UIThread.Post(() =>
                {
                    old.Dispose();
                    Leaving.Remove(presenter);
                    if (GetIsEnabled(presenter) && !Binders.TryGetValue(presenter, out _) && presenter.Parent is Popup again)
                        Binders.Add(presenter, new Binder(presenter, again));
                });
                return;
            }

            if (change.GetNewValue<bool>() && !Leaving.TryGetValue(presenter, out _) && presenter.Parent is Popup popup)
                Binders.Add(presenter, new Binder(presenter, popup));
        });
        // From the dispatcher too: a theme going unsets these, and re-styling the items in the middle of that would be
        // the same trouble.
        ItemThemeProperty.Changed.AddClassHandler<MenuFlyoutPresenter>((presenter, _) => Dispatcher.UIThread.Post(() => Retheme(presenter)));
        SeparatorThemeProperty.Changed.AddClassHandler<MenuFlyoutPresenter>((presenter, _) => Dispatcher.UIThread.Post(() => Retheme(presenter)));
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<MenuFlyout>((flyout, change) =>
        {
            if (change.GetNewValue<bool>() && flyout.Popup.Child is MenuFlyoutPresenter presenter
                && Binders.TryGetValue(presenter, out var binder))
                binder.Bind(flyout);
        });
    }

    public static bool GetIsEnabled(MenuFlyoutPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(MenuFlyoutPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    public static ControlTheme? GetItemTheme(MenuFlyoutPresenter presenter) => presenter.GetValue(ItemThemeProperty);

    public static void SetItemTheme(MenuFlyoutPresenter presenter, ControlTheme? value) => presenter.SetValue(ItemThemeProperty, value);

    public static ControlTheme? GetSeparatorTheme(MenuFlyoutPresenter presenter) => presenter.GetValue(SeparatorThemeProperty);

    public static void SetSeparatorTheme(MenuFlyoutPresenter presenter, ControlTheme? value) =>
        presenter.SetValue(SeparatorThemeProperty, value);

    /// <summary>The host of one presenter's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(MenuFlyoutPresenter presenter) =>
        Binders.TryGetValue(presenter, out var binder) ? binder.Host : null;

    /// <summary>Whether the menu's opening motion is running.</summary>
    internal static bool IsOpening(MenuFlyoutPresenter presenter) =>
        Binders.TryGetValue(presenter, out var binder) && binder.IsOpening;

    private static void Retheme(MenuFlyoutPresenter presenter)
    {
        if (Binders.TryGetValue(presenter, out var binder))
            binder.GiveThemes();
    }

    private sealed class Binder : IDisposable
    {
        private readonly MenuFlyoutPresenter _presenter;
        private readonly Popup _popup;
        private readonly TemplateParts _parts;
        private readonly Dictionary<Control, (ControlTheme Theme, IDisposable Given)> _themed = [];
        private readonly ScaleTransform _outerScale = new();
        private readonly ScaleTransform _innerScale = new();
        private readonly List<IDisposable> _moving = [];
        private MenuFlyout? _flyout;
        private TopLevel? _top;
        private IInsetsManager? _insets;
        private Border? _outer;
        private Border? _center;
        private Control? _inner;
        private IDisposable? _frame;
        private TimeSpan? _start;
        private bool _above;

        public Binder(MenuFlyoutPresenter presenter, Popup popup)
        {
            _presenter = presenter;
            _popup = popup;
            // The host's band motion runs on a stand-in that is never drawn, so it turns no band of the menu: the menu's
            // motion is its template's own (PR "Proposed for shared files": a host option for that).
            Host = new PhonePopupHost(popup, new Control(), Close);
            _popup.Opened += OnOpened;
            _popup.Closed += OnClosed;
            _presenter.ContainerPrepared += OnContainerPrepared;
            _presenter.ContainerClearing += OnContainerClearing;
            _presenter.SizeChanged += OnSizeChanged;
            _parts = TemplateParts.Watch(presenter, Take, Release);
            GiveThemes();
            if (_popup.IsOpen)
                OnOpened(null, EventArgs.Empty);
        }

        public PhonePopupHost Host { get; }

        public bool IsOpening => _frame is not null;

        /// <summary>
        /// Lets go of the menu: the host given back, the motion stopped, the item themes taken off. One the host was
        /// closing closes now.
        /// </summary>
        public void Dispose()
        {
            var closing = Host.IsClosing;
            _popup.Opened -= OnOpened;
            _popup.Closed -= OnClosed;
            _presenter.ContainerPrepared -= OnContainerPrepared;
            _presenter.ContainerClearing -= OnContainerClearing;
            _presenter.SizeChanged -= OnSizeChanged;
            OnClosed(null, EventArgs.Empty);
            Host.Dispose();
            _parts.Dispose();
            foreach (var (_, given) in _themed.Values)
                given.Dispose();
            _themed.Clear();
            if (closing)
                Close();
            _flyout = null;
        }

        /// <summary>
        /// The flyout opening this popup, whose target it is placed against. Its tap outside is Avalonia's own, as
        /// <see cref="PhonePopupDismissal"/> has it: a flyout's popup is light-dismissed, and the closing tap passes
        /// through only if the app sets <c>OverlayDismissEventPassThrough</c>.
        /// </summary>
        public void Bind(MenuFlyout flyout)
        {
            _flyout = flyout;
            if (_top is not null)
                Place();
        }

        /// <summary>The theme's item themes on every container the presenter has now.</summary>
        public void GiveThemes()
        {
            foreach (var container in _presenter.GetRealizedContainers().ToList())
                GiveTheme(container);
        }

        private void GiveTheme(Control container)
        {
            var theme = container switch
            {
                MenuItem => GetItemTheme(_presenter),
                Separator => GetSeparatorTheme(_presenter),
                _ => null,
            };
            // An app's ItemContainerTheme (the flyout's, given to the presenter) is set as the container is prepared, at
            // no priority of its own that a style's value wouldn't replace: it is left alone.
            var appTheme = _presenter.ItemContainerTheme is { TargetType: { } type } && type.IsAssignableFrom(container.StyleKey)
                ? _presenter.ItemContainerTheme
                : null;
            if (appTheme is not null)
                theme = null;
            if (_themed.TryGetValue(container, out var old))
            {
                // The same theme again (a container prepared again, or the other item theme changed): kept, not re-styled.
                if (old.Theme == theme)
                    return;
                _themed.Remove(container);
                old.Given.Dispose();
                // Prepared with ours on it, the container didn't take the app's ItemContainerTheme: given as Avalonia gives it.
                if (appTheme is not null && !container.IsSet(StyledElement.ThemeProperty))
                    container.SetCurrentValue(StyledElement.ThemeProperty, appTheme);
            }

            // At style priority: an app's own Theme wins.
            if (theme is not null && container.SetValue(StyledElement.ThemeProperty, theme, BindingPriority.Style) is { } given)
                _themed[container] = (theme, given);
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e) => GiveTheme(e.Container);

        private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
        {
            if (_themed.Remove(e.Container, out var given))
                given.Given.Dispose();
        }

        private void Take(Func<string, Control?> part)
        {
            _outer = part("OuterBorder") as Border;
            _center = part("CenterBorder") as Border;
            _inner = part("InnerBorder");
            if (IsOpening)
                Move();
        }

        private void Release()
        {
            Unset();
            _outer = null;
            _center = null;
            _inner = null;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (_top is not null)
                return;
            // The host has placed the popup at the window's top left, the window's width, as tall as the menu (its
            // handler is first: it was added first); the menu is moved down to its target from there.
            _top = TopLevel.GetTopLevel(_popup.PlacementTarget ?? _presenter);
            if (_top is null)
                return;
            _top.PropertyChanged += OnTopPropertyChanged;
            _top.AddHandler(TopLevel.BackRequestedEvent, OnBackRequested, handledEventsToo: true);
            _insets = _top.InsetsManager;
            if (_insets is not null)
                _insets.SafeAreaChanged += OnSafeAreaChanged;
            Place();
            _start = null;
            // The start at once, so no frame shows the menu at rest first; time 0 is still the first frame.
            Move();
            _frame = CornflowerClock.RequestFrame(_presenter, Step);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_top is null)
                return;
            _top.PropertyChanged -= OnTopPropertyChanged;
            _top.RemoveHandler(TopLevel.BackRequestedEvent, OnBackRequested);
            if (_insets is not null)
                _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _top = null;
            _insets = null;
            StopMoving();
        }

        /// <summary>
        /// Back, after the host's own handler has taken it and begun its top popup's closing: the menu, which has no
        /// closing motion, closes now.
        /// </summary>
        private void OnBackRequested(object? sender, RoutedEventArgs e)
        {
            if (Host.IsClosing)
                Close();
        }

        private void OnTopPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            // After the host's, which sizes the popup to the window again.
            if (e.Property == TopLevel.ClientSizeProperty)
                Place();
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Place();

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Laid out already: no layout pass of its own.
            if (e.HeightChanged && _top is not null)
                Place(layout: false);
        }

        /// <summary>
        /// The menu below its target if it fits there by the window's bottom, else above it; as tall as the window or
        /// taller, at the window's top. In the window's area inside the safe area, the host's.
        /// </summary>
        private void Place(bool layout = true)
        {
            if (_top is null || _flyout?.Target is not { } target)
                return;
            var area = new Rect(_top.ClientSize).Deflate(_insets?.SafeAreaPadding ?? default);
            // Laid out at the size the host has just given the popup.
            if (layout)
                _presenter.UpdateLayout();
            var height = _presenter.Bounds.Height;
            var targetTop = target.TranslatePoint(default, _top)?.Y ?? area.Y;
            var targetBottom = targetTop + target.Bounds.Height;
            var above = _above;
            double top;
            if (height >= area.Height)
            {
                top = area.Y;
                _above = false;
            }
            else if (targetBottom + height <= area.Bottom)
            {
                top = Math.Max(area.Y, targetBottom);
                _above = false;
            }
            else
            {
                // Kept inside the window, a target past its bottom too.
                top = Math.Clamp(targetTop - height, area.Y, area.Bottom - height);
                _above = true;
            }

            _popup.VerticalOffset = top - area.Y;
            if (above != _above && IsOpening)
            {
                // The edge by the target, and the inner border's origin, are the other side's.
                Unset();
                Move();
            }
        }

        /// <summary>One frame of the opening.</summary>
        private void Step()
        {
            _frame = null;
            if (_top is null)
                return;
            var start = _start ??= CornflowerClock.Now;
            if (CornflowerClock.Now - start >= OpenLength)
            {
                StopMoving();
                return;
            }

            _frame = CornflowerClock.RequestFrame(_presenter, Step);
            Move();
        }

        /// <summary>The storyboard's values at this time into it (0 before the first frame), at animation priority.</summary>
        private void Move()
        {
            if (_outer is null || _center is null || _inner is null)
                return;
            var time = _start is { } start ? CornflowerClock.Now - start : TimeSpan.Zero;
            _outerScale.ScaleX = Math.Clamp(time / OuterOpenLength, 0, 1);
            _innerScale.ScaleY = Math.Clamp((time - OuterOpenLength) / (OpenLength - OuterOpenLength), 0, 1);
            if (_moving.Count > 0)
                return;
            // Only the edge by the target shows a line until the end: the rest's thickness on that side alone.
            var rest = _presenter.BorderThickness;
            var edge = _above ? new Thickness(0, 0, 0, rest.Bottom) : new Thickness(0, rest.Top, 0, 0);
            _moving.Add(_outer.SetValue(Visual.RenderTransformProperty, _outerScale, BindingPriority.Animation)!);
            _moving.Add(_outer.SetValue(Visual.RenderTransformOriginProperty, new RelativePoint(0.5, 0, RelativeUnit.Relative), BindingPriority.Animation)!);
            _moving.Add(_inner.SetValue(Visual.RenderTransformProperty, _innerScale, BindingPriority.Animation)!);
            _moving.Add(_inner.SetValue(Visual.RenderTransformOriginProperty, new RelativePoint(0, _above ? 1 : 0, RelativeUnit.Relative), BindingPriority.Animation)!);
            _moving.Add(_outer.SetValue(Border.BorderThicknessProperty, edge, BindingPriority.Animation)!);
            _moving.Add(_center.SetValue(Border.BorderThicknessProperty, edge, BindingPriority.Animation)!);
        }

        /// <summary>The menu at rest: the storyboard's values gone, its frames stopped.</summary>
        private void StopMoving()
        {
            _frame?.Dispose();
            _frame = null;
            Unset();
        }

        /// <summary>The storyboard's values gone from the parts.</summary>
        private void Unset()
        {
            foreach (var value in _moving)
                value.Dispose();
            _moving.Clear();
        }

        /// <summary>The menu closes: the flyout, or the popup if no flyout opened it.</summary>
        private void Close()
        {
            if (_flyout is not null)
                _flyout.Hide();
            else
                _popup.Close();
        }
    }
}
