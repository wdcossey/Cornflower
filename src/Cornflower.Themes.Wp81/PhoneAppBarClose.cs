using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's open app bar closes when one of its commands is invoked, primary or secondary, unless it is sticky.
/// Avalonia's <c>CommandBar</c> leaves it open (only its light dismiss closes it). Turned on by the CommandBar theme's
/// own setter: a command's <c>Click</c> (a <c>CommandBarButton</c>'s or a <c>CommandBarToggleButton</c>'s) bubbling
/// through the bar clears <c>IsOpen</c> with <c>SetCurrentValue</c>, after the command's own handlers. The bar's
/// ellipsis isn't a command, so it still opens and closes the bar as Avalonia's does.
/// </summary>
internal static class PhoneAppBarClose
{
    /// <summary>Set by a theme: invoking a command closes the open bar.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CommandBar, bool>("IsEnabled", typeof(PhoneAppBarClose));

    static PhoneAppBarClose()
    {
        IsEnabledProperty.Changed.AddClassHandler<CommandBar>((bar, change) =>
        {
            bar.RemoveHandler(Button.ClickEvent, Close);
            if (change.GetNewValue<bool>())
                bar.AddHandler(Button.ClickEvent, Close, RoutingStrategies.Bubble, handledEventsToo: true);
        });
    }

    public static bool GetIsEnabled(CommandBar bar) => bar.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(CommandBar bar, bool value) => bar.SetValue(IsEnabledProperty, value);

    private static void Close(object? sender, RoutedEventArgs e)
    {
        if (sender is CommandBar { IsOpen: true, IsSticky: false } bar && e.Source is ICommandBarElement)
            bar.SetCurrentValue(CommandBar.IsOpenProperty, false);
    }
}
