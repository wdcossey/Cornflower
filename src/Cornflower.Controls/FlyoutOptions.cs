using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>Flyout.Placement="Full"</c>, for Avalonia's <see cref="Flyout"/>, whose <c>Placement</c> has no full
/// placement. It holds data only; a theme shows it, as the Windows Phone 8.1 theme does: on the phone a flyout is drawn
/// across the top of the window whatever its <c>Placement</c> (WinRT's Top, Bottom, Left and Right alike), and a full
/// one fills the window. A theme that doesn't read it ignores it.
/// </summary>
/// <example>
/// <code>&lt;Flyout cf:FlyoutOptions.IsFullScreen="True"&gt;...&lt;/Flyout&gt;</code>
/// </example>
public static class FlyoutOptions
{
    /// <summary>Whether the flyout fills the window: WinRT's <c>FlyoutPlacementMode.Full</c>. False by default.</summary>
    public static readonly AttachedProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.RegisterAttached<Flyout, bool>("IsFullScreen", typeof(FlyoutOptions));

    public static bool GetIsFullScreen(Flyout flyout) => flyout.GetValue(IsFullScreenProperty);

    public static void SetIsFullScreen(Flyout flyout, bool value) => flyout.SetValue(IsFullScreenProperty, value);
}
