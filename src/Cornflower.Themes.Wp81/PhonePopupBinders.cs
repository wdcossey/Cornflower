using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Cornflower.Themes.Wp81;

/// <summary>One presenter's hold on its popup for the phone: its <see cref="PhonePopupHost"/>, told which flyout opens it.</summary>
internal interface IPhonePopupBinder<TFlyout> : IDisposable where TFlyout : PopupFlyoutBase
{
    PhonePopupHost Host { get; }

    /// <summary>The flyout it was last told of, if any.</summary>
    TFlyout? Flyout { get; }

    /// <summary>The flyout opening this popup, each time it opens.</summary>
    void Bind(TFlyout flyout);
}

/// <summary>The flyout kinds' binders, set up as the module loads.</summary>
internal static class PhonePopupBinders
{
    /// <summary>
    /// Runs each flyout kind's static setup, which adds the <c>IsOpen</c> class handler its binders hear a flyout open
    /// from. Left to the theme's first styling of a presenter, it ran in the middle of the first flyout of the kind opened
    /// by <c>IsOpen</c>, after Avalonia had taken that change's handlers: that flyout was never bound (#209). A new
    /// binder kind is added here.
    /// </summary>
#pragma warning disable CA2255 // A library's module initializer: the one way to come before a flyout's first opening.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void SetUp()
    {
        RuntimeHelpers.RunClassConstructor(typeof(PhoneFlyout).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(PhoneMenuFlyout).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(PhonePickerFlyout).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(PhoneContextMenu).TypeHandle);
    }
}

/// <summary>
/// The binders of one kind of flyout presenter, for <see cref="PhoneFlyout"/>, <see cref="PhonePickerFlyout"/> and
/// <see cref="PhoneMenuFlyout"/>: one for each presenter in a popup while its theme's setter turns the phone's popup on,
/// and each time a flyout of the kind opens a popup, its presenter's binder told which flyout it is.
/// </summary>
/// <remarks>
/// <para>A binder is let go of from the dispatcher: a theme going unsets its setter first, and taking values off the
/// presenter (and closing its flyout) in the middle of that would stop the rest being unset (CLAUDE.md, the Avalonia
/// facts).</para>
/// <para>Turned off and on again before the dispatcher has run, the new binder is made only after the old one has given
/// the popup back what its host set, from the same job; else the new host would take those values for the popup's own,
/// and the old one's going would then clear them under it (<c>ShouldUseOverlayLayer</c> among them). A flyout still
/// open then is given to the new binder, as no opening will.</para>
/// </remarks>
internal sealed class PhonePopupBinders<TPresenter, TFlyout, TBinder>
    where TPresenter : Control
    where TFlyout : PopupFlyoutBase
    where TBinder : class, IPhonePopupBinder<TFlyout>
{
    private readonly AttachedProperty<bool> _isEnabled;
    private readonly Func<TPresenter, Popup, TBinder> _make;
    private readonly ConditionalWeakTable<TPresenter, TBinder> _binders = new();

    /// <summary>The binders let go of, until they have given their popups back.</summary>
    private readonly ConditionalWeakTable<TPresenter, TBinder> _leaving = new();

    /// <summary>
    /// Flyouts heard opening by <c>IsOpen</c> before their popup's presenter had a binder: this handler can come before
    /// Avalonia's own, which opens the popup. The binder made as the presenter is styled is told then.
    /// </summary>
    private readonly ConditionalWeakTable<Popup, TFlyout> _opening = new();

    /// <param name="isEnabled">The attached property the presenter's theme sets.</param>
    /// <param name="make">A presenter's binder, for its popup.</param>
    public PhonePopupBinders(AttachedProperty<bool> isEnabled, Func<TPresenter, Popup, TBinder> make)
    {
        _isEnabled = isEnabled;
        _make = make;
        isEnabled.Changed.AddClassHandler<TPresenter>((presenter, change) => OnEnabledChanged(presenter, change.GetNewValue<bool>()));
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<TFlyout>((flyout, change) =>
        {
            if (!change.GetNewValue<bool>())
                return;
            if (flyout.Popup.Child is TPresenter presenter && _binders.TryGetValue(presenter, out var binder))
                binder.Bind(flyout);
            else
                _opening.AddOrUpdate(flyout.Popup, flyout);
        });
    }

    /// <summary>A presenter's binder, if its theme has turned the phone's popup on for it.</summary>
    public TBinder? Of(TPresenter presenter) => _binders.TryGetValue(presenter, out var binder) ? binder : null;

    private void OnEnabledChanged(TPresenter presenter, bool enabled)
    {
        if (_binders.TryGetValue(presenter, out var old))
        {
            _binders.Remove(presenter);
            _leaving.Add(presenter, old);
            Dispatcher.UIThread.Post(() =>
            {
                var flyout = old.Flyout;
                old.Dispose();
                _leaving.Remove(presenter);
                if (!presenter.GetValue(_isEnabled) || _binders.TryGetValue(presenter, out _) || presenter.Parent is not Popup again)
                    return;
                var binder = _make(presenter, again);
                _binders.Add(presenter, binder);
                if (flyout is { IsOpen: true } && flyout.Popup == again)
                    binder.Bind(flyout);
            });
            return;
        }

        if (enabled && !_leaving.TryGetValue(presenter, out _) && presenter.Parent is Popup popup)
        {
            var binder = _make(presenter, popup);
            _binders.Add(presenter, binder);
            if (_opening.TryGetValue(popup, out var flyout))
            {
                _opening.Remove(popup);
                if (flyout.IsOpen && flyout.Popup == popup)
                    binder.Bind(flyout);
            }
        }
    }
}

/// <summary>
/// A flyout's popup on a <see cref="PhonePopupHost"/> with its closing held (<see cref="PhoneFlyout"/>'s and
/// <see cref="PhonePickerFlyout"/>'s): asked to close (<c>Hide()</c>, a tap outside, a pick), the flyout's
/// <c>Closing</c> is cancelled and the host's closing motion runs, and then the flyout closes. A flyout's own events are
/// Avalonia's, in Avalonia's order: <c>Closing</c> is raised when it is asked to close and again when it closes.
/// </summary>
internal sealed class PhoneHeldFlyout<TFlyout> : IPhonePopupBinder<TFlyout> where TFlyout : PopupFlyoutBase
{
    private readonly Popup _popup;
    private readonly bool _lightDismiss;
    private readonly Action<PhoneHeldFlyout<TFlyout>, TFlyout> _bind;
    private TFlyout? _flyout;
    private bool _closing;

    /// <param name="presenter">What the host's motion moves.</param>
    /// <param name="popup">The presenter's popup.</param>
    /// <param name="bind">What the owner sets each time a flyout opens the popup: the host's placement, the light dismiss.</param>
    /// <param name="setUp">The host's settings, as <see cref="PhonePopupHost"/> takes them.</param>
    public PhoneHeldFlyout(Control presenter, Popup popup, Action<PhoneHeldFlyout<TFlyout>, TFlyout> bind,
        Action<PhonePopupHost>? setUp = null)
    {
        _popup = popup;
        _bind = bind;
        _lightDismiss = popup.IsLightDismissEnabled;
        Host = new PhonePopupHost(popup, presenter, Close, setUp);
    }

    public PhonePopupHost Host { get; }

    public Popup Popup => _popup;

    public TFlyout? Flyout => _flyout;

    /// <summary>
    /// Lets go of the flyout, giving its popup back its light dismiss; one asked to close and still in its closing
    /// motion closes now.
    /// </summary>
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

    public void Bind(TFlyout flyout)
    {
        if (_flyout != flyout)
        {
            if (_flyout is not null)
                _flyout.Closing -= OnClosing;
            _flyout = flyout;
            _flyout.Closing += OnClosing;
        }

        _bind(this, flyout);
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
