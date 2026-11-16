using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>Where a phone popup sits: across the top of the window at its own height, or filling the window.</summary>
internal enum PhonePopupPlacement
{
    /// <summary>At the window's top left, the window's width, its own height (WinRT's Top, Bottom, Left and Right).</summary>
    Top,

    /// <summary>The whole window (WinRT's Full).</summary>
    Full,
}

/// <summary>
/// The phone's way of showing a popup over the page, shared by every Tier 3 popup (docs/controls/Flyout/README.md,
/// "What the popups share"): one host per Avalonia <see cref="Popup"/>, which, while the popup is open, keeps it in the
/// window's own overlay layer, places it (<see cref="Placement"/>), dims what is under it (<see cref="Dim"/>), opens and
/// closes it on <see cref="CornflowerClock"/> (<see cref="PhonePopupMotion"/>), and takes the Back key from the page and
/// its <c>NavigationPage</c> (<see cref="PhonePopupDismissal"/>).
/// </summary>
/// <remarks>
/// <para><b>Hosting.</b> The phone has one window and its popups are drawn in it; so the popup is shown in the window's
/// overlay layer (<c>ShouldUseOverlayLayer</c>), on desktop too, never as a window of its own. Set before it opens: the
/// host does it as it is made.</para>
/// <para><b>Placing.</b> When the popup opens, and as the window's size changes, it is anchored to the window's top left,
/// inside the safe area, the window's width, and for <see cref="PhonePopupPlacement.Full"/> its height too, whatever its
/// owner's own placement asked: Avalonia's positioner, given the window as the target and a gravity down and right with
/// no adjustment. A top popup is as tall as it asks, up to the window. <see cref="IsLandscapeProperty"/> is set on the
/// content while the window is wider than tall, for a theme's landscape values.</para>
/// <para><b>Dimming.</b> With a <see cref="Dim"/> brush, a rectangle of it covers the window under the popup while it is
/// open, as the phone's own full-window Rectangle popup does under a MenuFlyout and a ContentDialog: in the same overlay
/// layer, just below the popup. It takes no taps unless <see cref="PhonePopupDismissal.DimPassesTaps"/> says so.</para>
/// <para><b>Moving.</b> Opening, the content swings in about its top edge (<see cref="PhonePopupMotion"/>), drawn by a
/// <c>RenderTransform</c> at animation priority, taken away at rest. Closing goes through <see cref="Dismiss"/>, which
/// runs the closing motion and then calls the owner's close; anything that closes the popup another way ends the
/// motion where it is.</para>
/// <para><b>Back.</b> A class handler on <see cref="TopLevel.BackRequestedEvent"/>, which runs before any handler an
/// element adds (Avalonia's <c>PageNavigationHost</c>'s, which passes Back to its <c>NavigationPage</c>, included),
/// dismisses the window's topmost open host and marks the request handled.</para>
/// <para>To copy: make a host for the popup with its content (the element the motion moves) and the owner's close, set
/// <see cref="Placement"/> and <see cref="Dim"/>, and close through <see cref="Dismiss"/>.</para>
/// </remarks>
internal sealed class PhonePopupHost : IDisposable
{
    /// <summary>Set on the content while its window is wider than tall.</summary>
    public static readonly AttachedProperty<bool> IsLandscapeProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsLandscape", typeof(PhonePopupHost));

    /// <summary>The hosts open now, in the order they opened: Back takes its window's last.</summary>
    private static readonly List<PhonePopupHost> Open = [];

    private readonly Popup _popup;
    private readonly Control _content;
    private readonly Action _close;
    private PhonePopupPlacement _placement;
    private TopLevel? _top;
    private Rectangle? _dim;
    private IDisposable? _frame;
    private IDisposable? _shown;
    private IDisposable? _origin;
    private readonly MatrixTransform _transform = new();
    private TimeSpan? _start;
    private Phase _phase;

    static PhonePopupHost()
    {
        TopLevel.BackRequestedEvent.AddClassHandler<TopLevel>(OnBackRequested);
    }

    /// <param name="popup">The popup.</param>
    /// <param name="content">What the motion moves: the popup's child, or the part of it that is drawn.</param>
    /// <param name="close">The owner's close, which the closing motion ends with.</param>
    public PhonePopupHost(Popup popup, Control content, Action close)
    {
        _popup = popup;
        _content = content;
        _close = close;
        _popup.ShouldUseOverlayLayer = true;
        _popup.Opened += OnOpened;
        _popup.Closed += OnClosed;
        _content.SizeChanged += OnContentSizeChanged;
        if (_popup.IsOpen)
            OnOpened(null, EventArgs.Empty);
    }

    private enum Phase
    {
        Closed,
        Opening,
        Shown,
        Closing,
    }

    /// <summary>Where the popup sits. Set while it is open, it moves there at once.</summary>
    public PhonePopupPlacement Placement
    {
        get => _placement;
        set
        {
            if (_placement == value)
                return;
            _placement = value;
            if (_phase != Phase.Closed)
                Place();
        }
    }

    /// <summary>What dims the window under the popup while it is open; none by default.</summary>
    public IBrush? Dim { get; set; }

    /// <summary>Whether the popup is open, opening or not yet closed.</summary>
    public bool IsOpen => _phase != Phase.Closed;

    /// <summary>Whether its closing motion is running.</summary>
    public bool IsClosing => _phase == Phase.Closing;

    public static bool GetIsLandscape(Control control) => control.GetValue(IsLandscapeProperty);

    public void Dispose()
    {
        _popup.Opened -= OnOpened;
        _popup.Closed -= OnClosed;
        _content.SizeChanged -= OnContentSizeChanged;
        OnClosed(null, EventArgs.Empty);
        _popup.ClearValue(Popup.ShouldUseOverlayLayerProperty);
        _popup.ClearValue(Layoutable.WidthProperty);
        _popup.ClearValue(Layoutable.HeightProperty);
        _popup.ClearValue(Layoutable.MaxHeightProperty);
        _popup.ClearValue(Popup.PlacementRectProperty);
    }

    /// <summary>
    /// Closes the popup the phone's way: its closing motion, then the owner's close. Asked again while closing, or when
    /// it isn't open, it does nothing.
    /// </summary>
    public void Dismiss()
    {
        if (_phase is Phase.Closed or Phase.Closing)
            return;
        _phase = Phase.Closing;
        _frame?.Dispose();
        _frame = null;
        _start = null;
        if (_placement == PhonePopupPlacement.Top)
        {
            // At rest until it goes, in one frame.
            Look(0);
            _frame = CornflowerClock.Schedule(Finish, PhonePopupMotion.TopCloseDelay);
        }
        else
        {
            _frame = CornflowerClock.RequestFrame(_content, Step);
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_phase != Phase.Closed)
            return;
        _top = TopLevel.GetTopLevel(_content) ?? (_popup.PlacementTarget is { } target ? TopLevel.GetTopLevel(target) : null);
        if (_top is null)
            return;
        _phase = Phase.Opening;
        Open.Add(this);
        _top.PropertyChanged += OnTopPropertyChanged;
        Place();
        _start = null;
        Look(PhonePopupMotion.OpeningAngle(_placement, TimeSpan.Zero));
        _frame = CornflowerClock.RequestFrame(_content, Step);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_phase == Phase.Closed)
            return;
        _phase = Phase.Closed;
        Open.Remove(this);
        _frame?.Dispose();
        _frame = null;
        if (_top is not null)
            _top.PropertyChanged -= OnTopPropertyChanged;
        _top = null;
        RemoveDim();
        Rest();
        _content.ClearValue(IsLandscapeProperty);
    }

    private void Step()
    {
        _frame = null;
        if (_phase is not (Phase.Opening or Phase.Closing))
            return;
        var start = _start ??= CornflowerClock.Now;
        var time = CornflowerClock.Now - start;
        if (_phase == Phase.Opening)
        {
            if (time >= PhonePopupMotion.OpenLength(_placement))
            {
                _phase = Phase.Shown;
                Rest();
                return;
            }

            Look(PhonePopupMotion.OpeningAngle(_placement, time));
        }
        else
        {
            if (time >= PhonePopupMotion.FullCloseLength)
            {
                Finish();
                return;
            }

            Look(PhonePopupMotion.ClosingAngle(time));
        }

        _frame = CornflowerClock.RequestFrame(_content, Step);
    }

    /// <summary>The closing motion is over: the owner closes the popup.</summary>
    private void Finish()
    {
        _frame = null;
        if (_phase != Phase.Closing)
            return;
        _close();
        // An owner that didn't close it leaves it shown, at rest.
        if (_phase == Phase.Closing)
        {
            _phase = Phase.Shown;
            Rest();
        }
    }

    /// <summary>The content turned <paramref name="degrees"/> about its top edge; at 0, its own transform back.</summary>
    private void Look(double degrees)
    {
        if (degrees == 0)
        {
            Rest();
            return;
        }

        var size = _content.Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            size = _content.DesiredSize;
        _transform.Matrix = PhonePopupMotion.Swing(degrees, size);
        _origin ??= _content.SetValue(Visual.RenderTransformOriginProperty, RelativePoint.TopLeft, BindingPriority.Animation);
        _shown ??= _content.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
    }

    private void Rest()
    {
        _shown?.Dispose();
        _shown = null;
        _origin?.Dispose();
        _origin = null;
    }

    private void OnContentSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_shown is not null)
            _transform.Matrix = PhonePopupMotion.Swing(Angle(), e.NewSize);
    }

    /// <summary>The angle drawn now.</summary>
    private double Angle()
    {
        var time = _start is { } start ? CornflowerClock.Now - start : TimeSpan.Zero;
        return _phase == Phase.Closing && _placement == PhonePopupPlacement.Full
            ? PhonePopupMotion.ClosingAngle(time)
            : _phase == Phase.Opening ? PhonePopupMotion.OpeningAngle(_placement, time) : 0;
    }

    private void OnTopPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
            Place();
    }

    /// <summary>The window's area for a popup: its client area inside the safe area.</summary>
    private Rect Area()
    {
        if (_top is null)
            return default;
        var area = new Rect(_top.ClientSize);
        return area.Deflate(_top.InsetsManager?.SafeAreaPadding ?? default);
    }

    /// <summary>Anchors the popup to the window's top left, the window's width, and for Full its height.</summary>
    private void Place()
    {
        if (_top is null)
            return;
        var area = Area();
        _popup.Width = area.Width;
        _popup.Height = _placement == PhonePopupPlacement.Full ? area.Height : double.NaN;
        _popup.MaxHeight = area.Height;
        _popup.PlacementTarget = _top;
        _popup.Placement = PlacementMode.AnchorAndGravity;
        _popup.PlacementAnchor = PopupAnchor.TopLeft;
        _popup.PlacementGravity = PopupGravity.BottomRight;
        _popup.PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
        _popup.PlacementRect = area;
        _popup.HorizontalOffset = 0;
        _popup.VerticalOffset = 0;
        _content.SetValue(IsLandscapeProperty, area.Width > area.Height);
        PlaceDim(area);
    }

    /// <summary>The dim rectangle over the area, just below the popup in its overlay layer; none without a brush.</summary>
    private void PlaceDim(Rect area)
    {
        if (Dim is null)
        {
            RemoveDim();
            return;
        }

        var host = _content.GetSelfAndVisualAncestors().OfType<OverlayPopupHost>().FirstOrDefault();
        if (host?.GetVisualParent() is not Panel layer)
            return;
        _dim ??= new Rectangle { IsHitTestVisible = !PhonePopupDismissal.DimPassesTaps };
        _dim.Fill = Dim;
        _dim.Width = area.Width;
        _dim.Height = area.Height;
        Canvas.SetLeft(_dim, area.X);
        Canvas.SetTop(_dim, area.Y);
        if (_dim.Parent is null)
            layer.Children.Insert(layer.Children.IndexOf(host), _dim);
    }

    private void RemoveDim()
    {
        if (_dim?.Parent is Panel layer)
            layer.Children.Remove(_dim);
        _dim = null;
    }

    /// <summary>Back dismisses the window's topmost open popup, ahead of every handler an element adds.</summary>
    private static void OnBackRequested(TopLevel top, RoutedEventArgs e)
    {
        if (e.Handled || !PhonePopupDismissal.BackCloses)
            return;
        var window = WindowOf(top);
        var host = Open.LastOrDefault(h => h._top is { } t && WindowOf(t) == window);
        if (host is null)
            return;
        e.Handled = true;
        host.Dismiss();
    }

    /// <summary>The window a top level is in: for a popup's own window, the one it was opened from.</summary>
    private static TopLevel WindowOf(TopLevel top)
    {
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent && parent != top)
            top = parent;
        return top;
    }
}
