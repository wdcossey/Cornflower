using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's jump list round Cornflower's <see cref="SemanticZoom"/>: turned on by the SemanticZoom theme's own setter,
/// it puts the zoomed-out view's popup in a <see cref="PhonePopupHost"/> (docs/controls/Flyout/README.md, "What the popups
/// share"), which covers the whole window with it, with no dim, and takes Back, and gives it the jump list's motion and
/// the window's theme.
/// </summary>
/// <remarks>
/// <para>As #211 read it (tools/refapp/README.md, "Tier 4's reads", SemanticZoom; runs <c>20261118-2231-dev11-partial</c>
/// and <c>-2237-</c>, the recording at about 36 frames a second): <b>opening</b>, the tiles show <see cref="OpenDelay"/>
/// after the tap, and open in rows, each a tile's height (<see cref="RowHeight"/>, the first tile container's), every row
/// turning from <see cref="PhonePopupMotion.OpenFrom"/> to rest over <see cref="OpenLength"/>. <b>A tile's tap</b>
/// holds the jump list <see cref="TileHold"/> (the tile's tilt), then closes it in rows, the top first, over about
/// 80 ms (<see cref="CloseRate"/>, <see cref="CloseStagger"/>); <b>Back</b> closes it the same way at once. All on
/// <see cref="CornflowerClock"/> at <see cref="CornflowerConfig.PopupSpeed"/>.</para>
/// <para>The popup takes its window's theme, not the page's: the template's <c>AppTheme</c> scope, round the zoomed-out
/// view, asks for the window's <see cref="TopLevel.ActualThemeVariant"/> as it opens.</para>
/// </remarks>
internal static class PhoneSemanticZoom
{
    /// <summary>Set by Wp81's SemanticZoom theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<SemanticZoom, bool>("IsEnabled", typeof(PhoneSemanticZoom));

    /// <summary>The tiles show this long after the tap: the first recorded frame with them, a 4 px band across the first row.</summary>
    internal static readonly TimeSpan OpenDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>The rows open to their full height over this long (6 recorded frames).</summary>
    internal static readonly TimeSpan OpenLength = TimeSpan.FromMilliseconds(170);

    /// <summary>A tile's tap holds the jump list this long before it closes: about 100 ms read, the tap's tilt first.</summary>
    internal static readonly TimeSpan TileHold = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// A closing row's rate, degrees a tick, and the ticks between one row's start and the next one down's: the eight
    /// rows of the reference's 800 (the docs' sample tiles, 111.25) gone in 5 ticks, the read's about 80 ms (3 frames).
    /// </summary>
    internal const double CloseRate = 30;

    /// <inheritdoc cref="CloseRate"/>
    internal const double CloseStagger = 0.3;

    /// <summary>The row height with no tile to measure: the read's container, a 99 tile with a margin of 6 (111.25 laid out).</summary>
    internal const double RowHeight = 111.25;

    private const string ScopePart = "AppTheme";

    private static readonly ConditionalWeakTable<SemanticZoom, Binder> Binders = new();

    static PhoneSemanticZoom()
    {
        IsEnabledProperty.Changed.AddClassHandler<SemanticZoom>((zoom, e) => Update(zoom, e.GetNewValue<bool>()));
    }

    public static bool GetIsEnabled(SemanticZoom zoom) => zoom.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(SemanticZoom zoom, bool value) => zoom.SetValue(IsEnabledProperty, value);

    /// <summary>The host of a SemanticZoom's popup, once its jump list has opened under the theme.</summary>
    internal static PhonePopupHost? HostOf(SemanticZoom zoom) => Binders.TryGetValue(zoom, out var binder) ? binder.Host : null;

    /// <summary>The jump list's rows for tiles of <paramref name="height"/> in a window of <paramref name="window"/>: every row turning, at one rate.</summary>
    internal static PhonePopupRows Rows(double height, double window)
    {
        var rate = PhonePopupMotion.OpenFrom / PhonePopupMotion.Ticks(OpenLength);
        var count = (int)Math.Ceiling(window / height) + 1;
        return new PhonePopupRows(height, Enumerable.Repeat(rate, count).ToArray(), [], default, CloseRate, CloseStagger);
    }

    private static void Update(SemanticZoom zoom, bool on)
    {
        if (on && !Binders.TryGetValue(zoom, out _))
        {
            Binders.Add(zoom, new Binder(zoom));
            return;
        }

        if (!on && Binders.TryGetValue(zoom, out var old))
        {
            // A theme going unsets this first: let go from the dispatcher (CLAUDE.md, the Avalonia facts), and not if the
            // theme has come back meanwhile.
            Dispatcher.UIThread.Post(() =>
            {
                if (!GetIsEnabled(zoom) && Binders.TryGetValue(zoom, out var still) && still == old)
                {
                    Binders.Remove(zoom);
                    old.Dispose();
                }
            });
        }
    }

    private sealed class Binder : IDisposable
    {
        private readonly SemanticZoom _zoom;
        private IDisposable? _pending;

        public Binder(SemanticZoom zoom)
        {
            _zoom = zoom;
            zoom.OpenMotion = Open;
            zoom.CloseMotion = Close;
        }

        public PhonePopupHost? Host { get; private set; }

        private Popup? HostedPopup { get; set; }

        public void Dispose()
        {
            _pending?.Dispose();
            _pending = null;
            if (_zoom.OpenMotion == Open)
                _zoom.OpenMotion = null;
            if (_zoom.CloseMotion == Close)
                _zoom.CloseMotion = null;
            LetGo();
        }

        private static double Speed => CornflowerConfig.SpeedOf(MotionArea.Popup);

        /// <summary>The jump list opens a little after the tap; one still closing goes at once first.</summary>
        private void Open()
        {
            _pending?.Dispose();
            _pending = null;
            if (Host is { IsOpen: true } && HostedPopup is { } going)
                going.IsOpen = false;
            if (CornflowerConfig.Animated)
                _pending = CornflowerClock.Schedule(Show, OpenDelay, Speed);
            else
                Show();
        }

        private void Show()
        {
            _pending = null;
            // Taken out of the window meanwhile, it opens as it comes back (SemanticZoom.OnAttachedToVisualTree).
            if (_zoom.IsZoomedInViewActive || _zoom.Popup is not { } popup || TopLevel.GetTopLevel(_zoom) is null)
                return;
            Hold(popup);
            if (popup.Child is ThemeVariantScope scope && scope.Name == ScopePart && TopLevel.GetTopLevel(_zoom) is { } top)
                scope.RequestedThemeVariant = top.ActualThemeVariant;
            popup.IsOpen = true;
        }

        /// <summary>A tile's tap closes it after the tile's tilt; Back, or a change in code, at once.</summary>
        private void Close(bool byTile)
        {
            _pending?.Dispose();
            _pending = null;
            if (Host is not { IsOpen: true } host)
            {
                // Not shown yet: nothing to move.
                if (_zoom.Popup is { } popup)
                    popup.IsOpen = false;
                return;
            }

            if (byTile && CornflowerConfig.Animated)
                _pending = CornflowerClock.Schedule(Dismiss, TileHold, Speed);
            else
                host.Dismiss();
        }

        private void Dismiss()
        {
            _pending = null;
            Host?.Dismiss();
        }

        /// <summary>A host for the template's popup, made anew for a new template's.</summary>
        private void Hold(Popup popup)
        {
            if (HostedPopup == popup)
                return;
            LetGo();
            HostedPopup = popup;
            var content = popup.Child ?? popup;
            Host = new PhonePopupHost(popup, content, () => popup.IsOpen = false, host =>
            {
                host.Placement = PhonePopupPlacement.Full;
                host.Rows = Rows(RowHeight, TopLevel.GetTopLevel(_zoom) is { } top ? PhonePopupHost.AreaOf(top).Height : 0);
                host.Back = _zoom.ZoomInByBack;
            });
            popup.Opened += OnOpened;
        }

        private void LetGo()
        {
            if (HostedPopup is null)
                return;
            HostedPopup.Opened -= OnOpened;
            var going = Host!.IsClosing;
            Host.Dispose();
            if (going)
                HostedPopup.IsOpen = false;
            Host = null;
            HostedPopup = null;
        }

        /// <summary>The rows are the tiles' own height, measured once the host has laid the popup out at the window's size.</summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (Host is not { } host)
                return;
            var height = TileHeight() is > 0 and var measured ? measured : RowHeight;
            // Enough rows for the longer side, so a turn of the phone mid-opening leaves no row at rest.
            var rows = Rows(height, Math.Max(host.Area.Width, host.Area.Height));
            if (rows.Height != host.Rows.Height || rows.Rates.Length != host.Rows.Rates.Length)
                host.Rows = rows;
        }

        /// <summary>A realized tile's height with its margin, if the zoomed-out view is a list with one laid out.</summary>
        private double TileHeight() =>
            _zoom.ZoomedOutView is ListBox list && list.GetRealizedContainers().FirstOrDefault() is { } tile
                ? tile.Bounds.Height + tile.Margin.Top + tile.Margin.Bottom
                : 0;
    }
}
