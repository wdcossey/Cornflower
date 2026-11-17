using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's full-screen pickers, on Cornflower's <see cref="PickerFlyoutBase"/> (<see cref="ListPickerFlyout"/>,
/// <see cref="PickerFlyout"/>): turned on by their presenters' themes, it puts the presenter's popup in a
/// <see cref="PhonePopupHost"/>, which hosts it filling the window, moves it in bands and takes Back for it; and it
/// holds the picker's closing (a pick, accept, cancel, <c>Hide()</c>) until the closing motion has run. Copied from
/// <see cref="PhoneFlyout"/>, as docs/controls/Flyout/README.md ("What the popups share") says a picker does.
/// </summary>
/// <remarks>
/// <para>The pickers fill the window whatever their own <c>Placement</c>: the phone's are drawn at 0, 0, 450 × 800 on
/// the reference. There is nothing outside them to tap, so their popup's light dismiss is off; Back and their own
/// buttons close them.</para>
/// <para>Their motion is the full Flyout's (<see cref="PhonePopupMotion"/>): #149 read the PickerFlyout's as the same.
/// The ListPickerFlyout's bands are 66.7 px rows and slower ones (docs/controls/ListPickerFlyout/README.md, "Known
/// gaps"); the host has one band motion for a full popup, so it takes the Flyout's until the host takes a picker's.</para>
/// </remarks>
internal static class PhonePickerFlyout
{
    /// <summary>Set by Wp81's ListPickerFlyoutPresenter and PickerFlyoutPresenter themes.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<PickerPresenterBase, bool>("IsEnabled", typeof(PhonePickerFlyout));

    private static readonly ConditionalWeakTable<PickerPresenterBase, Binder> Binders = new();

    static PhonePickerFlyout()
    {
        IsEnabledProperty.Changed.AddClassHandler<PickerPresenterBase>((presenter, change) =>
        {
            if (Binders.TryGetValue(presenter, out var old))
            {
                // From the dispatcher, as PhoneFlyout's: a theme going unsets this first (CLAUDE.md, the Avalonia facts).
                Binders.Remove(presenter);
                Dispatcher.UIThread.Post(old.Dispose);
            }

            if (change.GetNewValue<bool>() && presenter.Parent is Popup popup)
                Binders.Add(presenter, new Binder(presenter, popup));
        });
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<PickerFlyoutBase>((flyout, change) =>
        {
            if (change.GetNewValue<bool>() && flyout.Popup.Child is PickerPresenterBase presenter
                && Binders.TryGetValue(presenter, out var binder))
                binder.Bind(flyout);
        });
    }

    public static bool GetIsEnabled(PickerPresenterBase presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(PickerPresenterBase presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    /// <summary>The host of one presenter's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(PickerPresenterBase presenter) =>
        Binders.TryGetValue(presenter, out var binder) ? binder.Host : null;

    private sealed class Binder : IDisposable
    {
        private readonly Popup _popup;
        private readonly bool _lightDismiss;
        private PickerFlyoutBase? _flyout;
        private bool _closing;

        public Binder(PickerPresenterBase presenter, Popup popup)
        {
            _popup = popup;
            _lightDismiss = popup.IsLightDismissEnabled;
            Host = new PhonePopupHost(popup, presenter, Close) { Placement = PhonePopupPlacement.Full };
        }

        public PhonePopupHost Host { get; }

        /// <summary>Lets go of the picker, giving its popup back its light dismiss; one in its closing motion closes now.</summary>
        public void Dispose()
        {
            var closing = Host.IsClosing;
            Host.Dispose();
            _popup.IsLightDismissEnabled = _lightDismiss;
            if (closing)
                Close();
            if (_flyout is not null)
                _flyout.Closing -= OnClosing;
            _flyout = null;
        }

        /// <summary>The picker opening this popup: no light dismiss, and its closing held.</summary>
        public void Bind(PickerFlyoutBase flyout)
        {
            if (_flyout != flyout)
            {
                if (_flyout is not null)
                    _flyout.Closing -= OnClosing;
                _flyout = flyout;
                _flyout.Closing += OnClosing;
            }

            _popup.IsLightDismissEnabled = false;
        }

        /// <summary>Asked to close while open: not yet; the host's closing motion first, then <see cref="Close"/>.</summary>
        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (_closing || e.Cancel || !Host.IsOpen)
                return;
            e.Cancel = true;
            Host.Dismiss();
        }

        /// <summary>The closing motion is over: the picker closes.</summary>
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
