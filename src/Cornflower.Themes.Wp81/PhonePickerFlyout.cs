using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's full-screen pickers, on Cornflower's <see cref="PickerFlyoutBase"/> (<see cref="ListPickerFlyout"/>,
/// <see cref="PickerFlyout"/>): turned on by their presenters' themes, it puts the presenter's popup in a
/// <see cref="PhonePopupHost"/>, which hosts it filling the window, moves it in bands and takes Back for it; and it
/// holds the picker's closing (a pick, accept, cancel, <c>Hide()</c>) until the closing motion has run: the Flyout's
/// binder (<see cref="PhoneHeldFlyout{TFlyout}"/>), as docs/controls/Flyout/README.md ("What the popups share") has it.
/// </summary>
/// <remarks>
/// <para>The pickers fill the window whatever their own <c>Placement</c>: the phone's are drawn at 0, 0, 450 × 800 on
/// the reference. There is nothing outside them to tap, so their popup's light dismiss is off; Back and their own
/// buttons close them.</para>
/// <para>The PickerFlyout's motion is the full Flyout's (<see cref="PhonePopupMotion.FlyoutRows"/>): #149 read it as the
/// same. The ListPickerFlyout's bands are its rows, 66.7 px, the top five turning more slowly down the list
/// (<see cref="PhonePopupMotion.ListPickerRows"/>): <c>Closed</c> comes at 30 ticks.</para>
/// </remarks>
internal static class PhonePickerFlyout
{
    /// <summary>Set by Wp81's ListPickerFlyoutPresenter and PickerFlyoutPresenter themes.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<PickerPresenterBase, bool>("IsEnabled", typeof(PhonePickerFlyout));

    private static readonly PhonePopupBinders<PickerPresenterBase, PickerFlyoutBase, PhoneHeldFlyout<PickerFlyoutBase>> Binders =
        new(IsEnabledProperty, (presenter, popup) => new PhoneHeldFlyout<PickerFlyoutBase>(presenter, popup, Bind,
            host => host.Placement = PhonePopupPlacement.Full));

    public static bool GetIsEnabled(PickerPresenterBase presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(PickerPresenterBase presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    /// <summary>The host of one presenter's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(PickerPresenterBase presenter) => Binders.Of(presenter)?.Host;

    /// <summary>The picker opening its popup: its rows, and no light dismiss.</summary>
    private static void Bind(PhoneHeldFlyout<PickerFlyoutBase> binder, PickerFlyoutBase flyout)
    {
        binder.Host.Rows = flyout is ListPickerFlyout ? PhonePopupMotion.ListPickerRows : PhonePopupMotion.FlyoutRows;
        binder.Popup.IsLightDismissEnabled = false;
    }
}
