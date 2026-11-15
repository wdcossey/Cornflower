using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// An open app bar closes when one of its commands is invoked, primary or secondary, unless it is sticky: what
/// Avalonia documents for <c>IsSticky</c> ("stays open after a command is invoked") and its <c>CommandBar</c> doesn't
/// do (only its light dismiss closes it). Turned on by the CommandBar theme's own setter: a <c>Click</c> of one of the
/// bar's own <c>PrimaryCommands</c> or <c>SecondaryCommands</c> bubbling through it clears <c>IsOpen</c> with
/// <c>SetCurrentValue</c>, posted, so the click's handlers and its bound <c>Command</c> (which Avalonia's
/// <c>Button</c> runs after <c>Click</c>) see the bar still open. A command with a <c>Flyout</c> leaves the bar open:
/// Avalonia opens the flyout against the command before <c>Click</c>, and closing would move or hide what it points
/// at. The bar's ellipsis isn't a command, so it still opens and closes the bar as Avalonia's does.
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
        if (sender is not CommandBar { IsOpen: true, IsSticky: false } bar || e.Source is not ICommandBarElement command)
            return;
        if (command is Button { Flyout: not null })
            return;
        if (!bar.PrimaryCommands.Contains(command) && !bar.SecondaryCommands.Contains(command))
            return;
        Dispatcher.UIThread.Post(() =>
        {
            if (!bar.IsSticky)
                bar.SetCurrentValue(CommandBar.IsOpenProperty, false);
        });
    }
}
