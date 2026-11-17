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
/// and again when it closes, after the motion; 8.1's flyout has no <c>Closing</c> (#150).
/// </summary>
/// <remarks>
/// <para>Avalonia's flyout is no styled element, so the theme can reach only its presenter. The presenter knows its
/// <see cref="Popup"/> (its parent); the flyout is found as it opens, from <see cref="FlyoutBase.IsOpenProperty"/>,
/// which a flyout sets before it opens its popup when shown with <c>ShowAt</c>, and after when opened by
/// <c>IsOpen</c>: then the popup is placed again, before it is drawn.</para>
/// <para>The binder is <see cref="PhoneHeldFlyout{TFlyout}"/>, made and let go of by <see cref="PhonePopupBinders{TPresenter, TFlyout, TBinder}"/>,
/// which a picker shares (<see cref="PhonePickerFlyout"/>).</para>
/// <para>A presenter outside a popup, or under a theme that doesn't turn this on, is Avalonia's own.</para>
/// </remarks>
internal static class PhoneFlyout
{
    /// <summary>Set by Wp81's FlyoutPresenter theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<FlyoutPresenter, bool>("IsEnabled", typeof(PhoneFlyout));

    private static readonly PhonePopupBinders<FlyoutPresenter, Flyout, PhoneHeldFlyout<Flyout>> Binders =
        new(IsEnabledProperty, (presenter, popup) => new PhoneHeldFlyout<Flyout>(presenter, popup, Bind));

    public static bool GetIsEnabled(FlyoutPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(FlyoutPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    /// <summary>The host of one presenter's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(FlyoutPresenter presenter) => Binders.Of(presenter)?.Host;

    /// <summary>The flyout opening its popup: its placement and its tap outside.</summary>
    private static void Bind(PhoneHeldFlyout<Flyout> binder, Flyout flyout)
    {
        binder.Host.Placement = FlyoutOptions.GetIsFullScreen(flyout) ? PhonePopupPlacement.Full : PhonePopupPlacement.Top;
        binder.Popup.IsLightDismissEnabled = PhonePopupDismissal.FlyoutTapOutsideCloses;
        if (!flyout.IsSet(PopupFlyoutBase.OverlayDismissEventPassThroughProperty))
            binder.Popup.OverlayDismissEventPassThrough = PhonePopupDismissal.TapOutsidePassesThrough;
    }
}
