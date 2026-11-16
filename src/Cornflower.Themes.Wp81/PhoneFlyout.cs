using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's Flyout, on Avalonia's <see cref="Flyout"/>: turned on by the FlyoutPresenter theme's own setter, it puts
/// the presenter's popup in a <see cref="PhonePopupHost"/>, which hosts, places, moves and takes Back for it, at the top
/// of the window or, with <see cref="FlyoutOptions.IsFullScreenProperty"/>, filling it, whatever the flyout's own
/// <c>Placement</c>; and it holds the flyout's closing (<c>Hide()</c>, a tap outside, Back) until the closing motion
/// has run. A flyout's own events are Avalonia's, in Avalonia's order: <c>Closing</c> is raised when it is asked to close
/// and again when it closes, after the motion (PENDING #150: the phone's order).
/// </summary>
/// <remarks>
/// <para>Avalonia's flyout is no styled element, so the theme can reach only its presenter. The presenter knows its
/// <see cref="Popup"/> (its parent); the flyout is found as it opens, from <see cref="FlyoutBase.IsOpenProperty"/>,
/// which a flyout sets before it opens its popup when shown with <c>ShowAt</c>, and after when opened by
/// <c>IsOpen</c>: then the popup is placed again, before it is drawn.</para>
/// <para>A presenter outside a popup, or under a theme that doesn't turn this on, is Avalonia's own.</para>
/// </remarks>
internal static class PhoneFlyout
{
    /// <summary>Set by Wp81's FlyoutPresenter theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<FlyoutPresenter, bool>("IsEnabled", typeof(PhoneFlyout));

    private static readonly ConditionalWeakTable<FlyoutPresenter, Binder> Binders = new();

    static PhoneFlyout()
    {
        IsEnabledProperty.Changed.AddClassHandler<FlyoutPresenter>((presenter, change) =>
        {
            if (Binders.TryGetValue(presenter, out var old))
            {
                Binders.Remove(presenter);
                old.Dispose();
            }

            if (change.GetNewValue<bool>() && presenter.Parent is Popup popup)
                Binders.Add(presenter, new Binder(presenter, popup));
        });
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<Flyout>((flyout, change) =>
        {
            if (change.GetNewValue<bool>() && flyout.Popup.Child is FlyoutPresenter presenter
                && Binders.TryGetValue(presenter, out var binder))
                binder.Bind(flyout);
        });
    }

    public static bool GetIsEnabled(FlyoutPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(FlyoutPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    /// <summary>The host of one presenter's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(FlyoutPresenter presenter) =>
        Binders.TryGetValue(presenter, out var binder) ? binder.Host : null;

    private sealed class Binder : IDisposable
    {
        private readonly Popup _popup;
        private Flyout? _flyout;
        private bool _closing;

        public Binder(FlyoutPresenter presenter, Popup popup)
        {
            _popup = popup;
            Host = new PhonePopupHost(popup, presenter, Close);
        }

        public PhonePopupHost Host { get; }

        /// <summary>Lets go of the flyout; one asked to close and still in its closing motion closes now.</summary>
        public void Dispose()
        {
            var closing = Host.IsClosing;
            Host.Dispose();
            if (closing)
                Close();
            if (_flyout is not null)
                _flyout.Closing -= OnClosing;
            _flyout = null;
        }

        /// <summary>The flyout opening this popup: its placement, its tap outside, and its closing.</summary>
        public void Bind(Flyout flyout)
        {
            if (_flyout != flyout)
            {
                if (_flyout is not null)
                    _flyout.Closing -= OnClosing;
                _flyout = flyout;
                _flyout.Closing += OnClosing;
            }

            Host.Placement = FlyoutOptions.GetIsFullScreen(flyout) ? PhonePopupPlacement.Full : PhonePopupPlacement.Top;
            _popup.IsLightDismissEnabled = PhonePopupDismissal.FlyoutTapOutsideCloses;
            if (!flyout.IsSet(PopupFlyoutBase.OverlayDismissEventPassThroughProperty))
                _popup.OverlayDismissEventPassThrough = PhonePopupDismissal.TapOutsidePassesThrough;
        }

        /// <summary>Asked to close while open: not yet; the host's closing motion first, then <see cref="Close"/>.</summary>
        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (_closing || e.Cancel || !Host.IsOpen)
                return;
            e.Cancel = true;
            Host.Dismiss();
        }

        /// <summary>The closing motion is over: the flyout closes.</summary>
        private void Close()
        {
            _closing = true;
            try
            {
                if (_flyout is not null)
                    _flyout.Hide();
                else
                    _popup.Close();
            }
            finally
            {
                _closing = false;
            }
        }
    }
}
