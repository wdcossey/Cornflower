using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
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
/// 45° to 0 on WinRT's ExponentialEase, out, at rest at tick 9: the host's, given the dialog's swing
/// (<see cref="PhonePopupMotion.DialogSwing"/>).</para>
/// <para><b>Closing</b>: the dialog's events and result come at once, in the call's frame (#150); it is drawn as it is,
/// taking no taps, and goes in one frame 107.5 ms later (#149: 95 to 120 ms), the dim layer with it, as the host closes
/// the popup.</para>
/// <para><b>Back</b> hides the dialog (<see cref="ContentDialog.Hide"/>, so <c>Closing</c> may keep it open) when it is its
/// window's topmost phone popup, and is marked handled: the host gives Back to the dialog
/// (<see cref="PhonePopupHost.Back"/>) rather than close the popup without asking it.</para>
/// <para>The keyboard's Back is the platform's: on Android the input method takes the first Back to hide itself, as
/// the phone's does (#150), and the next reaches the dialog.</para>
/// </remarks>
internal static class PhoneContentDialog
{
    /// <summary>Set by Wp81's ContentDialog theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContentDialog, bool>("IsEnabled", typeof(PhoneContentDialog));

    /// <summary>
    /// Set by Wp81's ContentDialog theme on the title's and the content's presenters: the text a presenter makes of a
    /// string gets the phone's line, as <see cref="PhoneContentLine"/> gives an <c>AccessText</c>'s, with no access keys,
    /// so an underscore in the text shows as written, as on the phone.
    /// </summary>
    public static readonly AttachedProperty<bool> TextLineProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, bool>("TextLine", typeof(PhoneContentDialog));

    /// <summary>The <c>MaxHeight</c> line height that gives the phone's own line, 1.33 em (as PhoneContentLine's).</summary>
    private const double PhoneLine = double.Epsilon;

    private const string DimKey = "ContentDialogDimmingThemeBrush";

    /// <summary>The text each presenter's line was put on, to take it back.</summary>
    private static readonly ConditionalWeakTable<ContentPresenter, TextBlock> Lined = new();

    private static readonly ConditionalWeakTable<ContentDialog, Binder> Binders = new();

    static PhoneContentDialog()
    {
        IsEnabledProperty.Changed.AddClassHandler<ContentDialog>((dialog, _) => Update(dialog));
        StyledElement.ParentProperty.Changed.AddClassHandler<ContentDialog>((dialog, _) => Update(dialog));
        TextLineProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => UpdateLine(presenter));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => UpdateLine(presenter));
    }

    public static bool GetTextLine(ContentPresenter presenter) => presenter.GetValue(TextLineProperty);

    public static void SetTextLine(ContentPresenter presenter, bool value) => presenter.SetValue(TextLineProperty, value);

    /// <summary>
    /// The phone's line on the text the presenter made of content that isn't an element, with no template; taken back
    /// off text it no longer shows. An app's own template whose root is a TextBlock, for a string, would get it too.
    /// </summary>
    private static void UpdateLine(ContentPresenter presenter)
    {
        var made = presenter.GetValue(TextLineProperty) && presenter.Child is TextBlock text &&
                   presenter.ContentTemplate is null && presenter.Content is not (null or Control)
            ? text
            : null;
        if (Lined.TryGetValue(presenter, out var previous) && !ReferenceEquals(previous, made))
        {
            previous.ClearValue(PhoneTextLine.MaxHeightLineHeightProperty);
            Lined.Remove(presenter);
        }

        if (made is not null && !Lined.TryGetValue(presenter, out _))
        {
            made.SetValue(PhoneTextLine.MaxHeightLineHeightProperty, PhoneLine);
            Lined.Add(presenter, made);
        }
    }

    public static bool GetIsEnabled(ContentDialog dialog) => dialog.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContentDialog dialog, bool value) => dialog.SetValue(IsEnabledProperty, value);

    /// <summary>The host of a dialog's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(ContentDialog dialog) =>
        Binders.TryGetValue(dialog, out var binder) ? binder.Host : null;

    private static Popup? PopupOf(ContentDialog dialog) => GetIsEnabled(dialog) ? dialog.Parent as Popup : null;

    private static void Update(ContentDialog dialog)
    {
        var popup = PopupOf(dialog);
        if (Binders.TryGetValue(dialog, out var old))
        {
            if (old.Popup == popup)
                return;
            if (popup is null)
            {
                // A theme going unsets this first: the binder lets go from the dispatcher (CLAUDE.md, the Avalonia facts),
                // and not at all if the theme has come back meanwhile, so no second host is made on the popup.
                Dispatcher.UIThread.Post(() =>
                {
                    if (PopupOf(dialog) != old.Popup && Binders.TryGetValue(dialog, out var still) && still == old)
                    {
                        Binders.Remove(dialog);
                        old.Dispose();
                    }
                });
                return;
            }

            Binders.Remove(dialog);
            old.Dispose();
        }

        if (popup is not null)
            Binders.Add(dialog, new Binder(dialog, popup));
    }

    private sealed class Binder : IDisposable
    {
        /// <summary>The dim layer's brush, its colour the theme's, followed while it is shown.</summary>
        private readonly SolidColorBrush _dim = new();
        private IDisposable? _untouchable;

        public Binder(ContentDialog dialog, Popup popup)
        {
            Dialog = dialog;
            Popup = popup;
            Host = new PhonePopupHost(popup, dialog, Close)
            {
                Placement = PhonePopupPlacement.Top,
                Swing = PhonePopupMotion.DialogSwing,
                Back = dialog.Hide,
            };
            OnThemeChanged(null, EventArgs.Empty);
            popup.Opened += OnOpened;
            popup.Closed += OnClosed;
            dialog.ActualThemeVariantChanged += OnThemeChanged;
            dialog.CloseMotion = Go;
        }

        public ContentDialog Dialog { get; }

        public Popup Popup { get; }

        public PhonePopupHost Host { get; }

        /// <summary>Lets go of the dialog; one still going is closed now.</summary>
        public void Dispose()
        {
            Popup.Opened -= OnOpened;
            Popup.Closed -= OnClosed;
            Dialog.ActualThemeVariantChanged -= OnThemeChanged;
            if (Dialog.CloseMotion == Go)
                Dialog.CloseMotion = null;
            var going = Host.IsClosing;
            Touchable();
            Host.Dispose();
            if (going)
                Popup.IsOpen = false;
        }

        /// <summary>The dim's colour the theme's; none without the key.</summary>
        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (Dialog.TryFindResource(DimKey, Dialog.ActualThemeVariant, out var found) && found is ISolidColorBrush brush)
            {
                _dim.Color = brush.Color;
                Host.Dim = _dim;
            }
            else
            {
                Host.Dim = null;
            }
        }

        private void OnOpened(object? sender, EventArgs e) => Touchable();

        private void OnClosed(object? sender, EventArgs e) => Touchable();

        /// <summary>
        /// The dialog has closed: drawn as it is, then gone in one frame, its dim layer with it (the host's close, which
        /// takes Back and does nothing meanwhile, and shows the dialog at rest if it was still opening).
        /// </summary>
        private void Go()
        {
            Host.Dismiss();
            // Closed: it takes no taps while it is still drawn.
            _untouchable ??= Dialog.SetValue(InputElement.IsHitTestVisibleProperty, false, BindingPriority.Animation);
        }

        private void Close() => Popup.IsOpen = false;

        private void Touchable()
        {
            _untouchable?.Dispose();
            _untouchable = null;
        }
    }
}
