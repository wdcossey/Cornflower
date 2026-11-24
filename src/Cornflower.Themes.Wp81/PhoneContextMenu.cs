using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// A plain ContextMenu with the MenuFlyout's look and motion (ADR 0007, answer 7): turned on by the ContextMenu theme's
/// own setter, it puts the menu's popup on the MenuFlyout's binder (<see cref="PhoneMenuFlyout.Binder{TFlyout}"/>): the
/// phone's popup host (the window's overlay layer, the window's width, Back), the menu below its target if it fits there,
/// else above it, and the template's opening storyboard on <see cref="Cornflower.Controls.CornflowerClock"/>, as a
/// MenuFlyout opens. The ContextMenu isn't the phone's, so this is a plain theme's behaviour, not a reproduction.
/// </summary>
/// <remarks>
/// <para>The target is the menu's own <c>PlacementTarget</c>, else the control that owns it (the popup's parent), as a
/// MenuFlyout is placed against the element it is shown at: not the element a press or a hold came from, which Avalonia
/// places it by (a Button's text, say), so the menu is below or above the whole control.</para>
/// <para>Its items keep their own themes (the plain MenuItem's, by type); its Separators get the MenuFlyout's. Closing is the
/// ContextMenu's own <c>Close()</c>, at once, as the MenuFlyout closes; a tap outside is Avalonia's light dismiss with
/// the ContextMenu's own pass-through.</para>
/// </remarks>
internal static class PhoneContextMenu
{
    /// <summary>Set by Wp81's ContextMenu theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ContextMenu, bool>("IsEnabled", typeof(PhoneContextMenu));

    /// <summary>One binder a menu, made and let go of as the MenuFlyout's are; no flyout ever opens one.</summary>
    private static readonly PhonePopupBinders<ContextMenu, NoFlyout, PhoneMenuFlyout.Binder<NoFlyout>> Binders =
        new(IsEnabledProperty, (menu, popup) => new PhoneMenuFlyout.Binder<NoFlyout>(menu, popup,
            _ => menu.PlacementTarget ?? popup.Parent as Control, SeparatorTheme, menu.Close));

    /// <summary>
    /// A Separator of the menu's own gets the MenuFlyout's, which draws nothing and is 0 tall (#150), as the menu takes the
    /// MenuFlyout's look; its MenuItems keep the plain theme by type.
    /// </summary>
    private static ControlTheme? SeparatorTheme(Control container) =>
        container is Separator ? container.FindResource(container.ActualThemeVariant, "Wp81MenuFlyoutSeparatorTheme") as ControlTheme : null;

    public static bool GetIsEnabled(ContextMenu menu) => menu.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ContextMenu menu, bool value) => menu.SetValue(IsEnabledProperty, value);

    /// <summary>The host of one menu's popup, if the theme has turned this on for it.</summary>
    internal static PhonePopupHost? HostOf(ContextMenu menu) => Binders.Of(menu)?.Host;

    /// <summary>Whether the menu's opening motion is running.</summary>
    internal static bool IsOpening(ContextMenu menu) => Binders.Of(menu)?.IsOpening == true;

    /// <summary>The binders' flyout kind for a menu no flyout opens: never made, so its <c>IsOpen</c> never changes.</summary>
    internal sealed class NoFlyout : PopupFlyoutBase
    {
        private NoFlyout()
        {
        }

        protected override Control CreatePresenter() => throw new NotSupportedException();
    }
}
