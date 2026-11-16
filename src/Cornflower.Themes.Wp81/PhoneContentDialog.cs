using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's ContentDialog round Cornflower's <see cref="ContentDialog"/>: turned on by the ContentDialog theme's own
/// setter, it puts the dialog's popup in a <see cref="PhonePopupHost"/> (docs/controls/Flyout/README.md, "What the popups
/// share"), which hosts it at the top of the window over the <c>ContentDialogDimmingThemeBrush</c> layer, and gives it
/// the dialog's own motion and Back.
/// </summary>
/// <remarks>
/// <para><b>Opening</b> (#149, run <c>20261116-1835-dev11</c>): one band, the dialog, turned about its centre line from
/// <see cref="OpenFrom"/> to 0 on WinRT's ExponentialEase, out, exponent <see cref="OpenExponent"/>, over
/// <see cref="OpenLength"/> (tick 9, 141 ms). The host turns a top popup the Flyout's way, linearly from 80°, so the
/// dialog's angle is set over the host's in each frame, after it; the host's band rests at 134.4 ms, between the
/// phone's ticks 8 and 9, where this curve has under 0.8° to go.</para>
/// <para><b>Closing</b>: the dialog's events and result come at once, in the call's frame (#150); it is drawn as it is
/// and goes in one frame <see cref="GoneAfter"/> later (#149: 95 to 120 ms), the dim layer with it, when the popup is
/// closed. The host's own close (gone at 110 ms, closed at 140.6) is pre-empted.</para>
/// <para><b>Back</b> hides the dialog (<see cref="ContentDialog.Hide"/>, so <c>Closing</c> may keep it open) when it is its
/// window's topmost popup, and is marked handled. It must come before the host's own Back, which would close the
/// dialog's popup without asking the dialog: so its class handler is added as the module loads
/// (<see cref="TakeBack"/>), before any host's.</para>
/// <para>The keyboard's Back is the platform's: on Android the input method takes the first Back to hide itself, as
/// the phone's does (#150), and the next reaches the dialog.</para>
/// </remarks>
internal static class PhoneContentDialog
{
    /// <summary>Set by Wp81's ContentDialog theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContentDialog, bool>("IsEnabled", typeof(PhoneContentDialog));

    /// <summary>The opening's first angle, in degrees, the lower edge toward the eye.</summary>
    internal const double OpenFrom = 45;

    /// <summary>The opening's ease, WinRT's ExponentialEase out.</summary>
    internal const double OpenExponent = 1.68;

    /// <summary>The opening is at rest 9 ticks of the phone's clock after its first frame: 140.6 ms.</summary>
    internal static readonly TimeSpan OpenLength = PhonePopupMotion.Tick * 9;

    /// <summary>The dialog is gone this long after it is asked to close: the middle of the read 95 to 120 ms.</summary>
    internal static readonly TimeSpan GoneAfter = TimeSpan.FromMilliseconds(107.5);

    private const string DimKey = "ContentDialogDimmingThemeBrush";

    private static readonly ConditionalWeakTable<ContentDialog, Binder> Binders = new();

    /// <summary>The dialogs shown now, each with its binder.</summary>
    private static readonly List<Binder> Shown = [];

    /// <summary>
    /// Adds the Back handler as the module loads: a class handler runs in the order it was added, and the hosts' is added
    /// when the first host is made.
    /// </summary>
#pragma warning disable CA2255 // A library's module initializer: the one way to come before PhonePopupHost's class handler.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void TakeBack() => TopLevel.BackRequestedEvent.AddClassHandler<TopLevel>(OnBackRequested);

    static PhoneContentDialog()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentDialog>((dialog, _) => Update(dialog));
        StyledElement.ParentProperty.Changed.AddClassHandler<ContentDialog>((dialog, _) => Update(dialog));
    }

    public static bool GetIsEnabled(ContentDialog dialog) => dialog.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContentDialog dialog, bool value) => dialog.SetValue(IsEnabledProperty, value);

    /// <summary>The host of a dialog's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(ContentDialog dialog) =>
        Binders.TryGetValue(dialog, out var binder) ? binder.Host : null;

    /// <summary>The dialog's angle <paramref name="time"/> into its opening, in degrees: 0 at rest.</summary>
    internal static double OpeningAngle(TimeSpan time) =>
        time >= OpenLength ? 0 : PhoneEase.Remaining(OpenFrom, OpenExponent, time, OpenLength);

    private static void Update(ContentDialog dialog)
    {
        var popup = GetIsEnabled(dialog) ? dialog.Parent as Popup : null;
        if (Binders.TryGetValue(dialog, out var old))
        {
            if (old.Popup == popup)
                return;
            Binders.Remove(dialog);
            // A theme going unsets this first: what the binder gives back is given from the dispatcher, unless the popup
            // is closed and nothing of the dialog's is touched (CLAUDE.md, the Avalonia facts).
            if (old.Popup.IsOpen)
                Dispatcher.UIThread.Post(old.Dispose);
            else
                old.Dispose();
        }

        if (popup is not null)
            Binders.Add(dialog, new Binder(dialog, popup));
    }

    /// <summary>Back hides the window's topmost dialog, ahead of the hosts' own handler; taken while it is still drawn.</summary>
    private static void OnBackRequested(TopLevel top, RoutedEventArgs e)
    {
        if (e.Handled || !PhonePopupDismissal.BackCloses)
            return;
        var window = WindowOf(top);
        var binder = Shown.LastOrDefault(b => b.Popup.IsOpen && b.Window == window && b.IsTopmost);
        if (binder is null)
            return;
        e.Handled = true;
        binder.Dialog.Hide();
    }

    /// <summary>The window a top level is in: for a popup's own window, the one it was opened from.</summary>
    private static TopLevel WindowOf(TopLevel top)
    {
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent && parent != top)
            top = parent;
        return top;
    }

    private sealed class Binder : IDisposable
    {
        private IDisposable? _frame;
        private IDisposable? _going;
        private TimeSpan? _start;
        private int _uncut;

        public Binder(ContentDialog dialog, Popup popup)
        {
            Dialog = dialog;
            Popup = popup;
            Host = new PhonePopupHost(popup, dialog, Close) { Placement = PhonePopupPlacement.Top, Dim = Dim() };
            // After the host's: it has placed and dimmed the popup, and cut its band.
            popup.Opened += OnOpened;
            popup.Closed += OnClosed;
            dialog.ActualThemeVariantChanged += OnThemeChanged;
            dialog.CloseMotion = Go;
            if (popup.IsOpen)
                Shown.Add(this);
        }

        public ContentDialog Dialog { get; }

        public Popup Popup { get; }

        public PhonePopupHost Host { get; }

        /// <summary>The window the dialog is shown over.</summary>
        public TopLevel? Window => Popup.PlacementTarget is { } target && TopLevel.GetTopLevel(target) is { } top
            ? WindowOf(top)
            : null;

        /// <summary>Whether no other popup is open above the dialog's in its window's overlay layer.</summary>
        public bool IsTopmost
        {
            get
            {
                var host = Dialog.GetVisualAncestors().OfType<OverlayPopupHost>().FirstOrDefault();
                if (host?.GetVisualParent() is not Panel layer)
                    return true;
                return !layer.Children.Skip(layer.Children.IndexOf(host) + 1).OfType<OverlayPopupHost>().Any(h => h.IsVisible);
            }
        }

        /// <summary>Lets go of the dialog; one still going is closed now.</summary>
        public void Dispose()
        {
            Popup.Opened -= OnOpened;
            Popup.Closed -= OnClosed;
            Dialog.ActualThemeVariantChanged -= OnThemeChanged;
            if (Dialog.CloseMotion == Go)
                Dialog.CloseMotion = null;
            var going = _going is not null;
            Stop();
            Shown.Remove(this);
            Host.Dispose();
            if (going)
                Popup.IsOpen = false;
        }

        private IBrush? Dim() =>
            Dialog.TryFindResource(DimKey, Dialog.ActualThemeVariant, out var brush) ? brush as IBrush : null;

        private void OnThemeChanged(object? sender, EventArgs e) => Host.Dim = Dim();

        private void OnOpened(object? sender, EventArgs e)
        {
            Stop();
            Shown.Remove(this);
            Shown.Add(this);
            _start = null;
            _uncut = 0;
            // The first angle at once, over the host's, so no frame shows the Flyout's.
            if (Host.Bands is { } bands)
                Turn(bands, TimeSpan.Zero);
            _frame = CornflowerClock.RequestFrame(Dialog, Step);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Stop();
            Shown.Remove(this);
        }

        /// <summary>A frame of the opening, after the host's: the dialog's angle over the Flyout's.</summary>
        private void Step()
        {
            _frame = null;
            if (Host.Bands is not { } bands)
            {
                // Not cut yet, as the host waits a few frames for the popup to be laid out; or the host is at rest.
                if (_start is null && ++_uncut < 5)
                    _frame = CornflowerClock.RequestFrame(Dialog, Step);
                return;
            }

            var start = _start ??= CornflowerClock.Now;
            var time = CornflowerClock.Now - start;
            Turn(bands, time);
            if (time < OpenLength)
                _frame = CornflowerClock.RequestFrame(Dialog, Step);
        }

        private static void Turn(PhonePopupBands bands, TimeSpan time)
        {
            bands.Angles[0] = OpeningAngle(time);
            bands.InvalidateVisual();
        }

        /// <summary>The dialog has closed: it is drawn as it is, then gone in one frame, its dim layer with it.</summary>
        private void Go()
        {
            Stop();
            // The host takes Back and does nothing meanwhile, and shows the dialog at rest if it was still opening.
            Host.Dismiss();
            _going = CornflowerClock.Schedule(Close, GoneAfter);
        }

        private void Close()
        {
            _going = null;
            Popup.IsOpen = false;
        }

        private void Stop()
        {
            _frame?.Dispose();
            _frame = null;
            _going?.Dispose();
            _going = null;
        }
    }
}
