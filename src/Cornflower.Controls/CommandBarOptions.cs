using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>AppBarClosedDisplayMode</c>: how much of a closed app bar shows. The Windows Phone 8.1 members, with
/// their values.
/// </summary>
public enum AppBarClosedDisplayMode
{
    /// <summary>The commands' icons, without their labels. The default.</summary>
    Compact = 0,

    /// <summary>A thin bar with the ellipsis alone.</summary>
    Minimal = 1,
}

/// <summary>
/// WinRT's <c>AppBar.ClosedDisplayMode</c>, for Avalonia's <see cref="CommandBar"/>, which lacks it. It holds data
/// only; a theme's template shows it, as the Windows Phone 8.1 theme's <c>CommandBar</c> does: compact, the closed bar
/// shows the commands' icons; minimal, a thin bar with the ellipsis alone. Opened, the bar is the same in both. A
/// theme that doesn't read it ignores it.
/// </summary>
/// <example>
/// <code>&lt;CommandBar cf:CommandBarOptions.ClosedDisplayMode="Minimal" /&gt;</code>
/// </example>
public static class CommandBarOptions
{
    /// <summary>How much of the closed bar shows: WinRT's <c>ClosedDisplayMode</c>. Compact by default, as WinRT's.</summary>
    public static readonly AttachedProperty<AppBarClosedDisplayMode> ClosedDisplayModeProperty =
        AvaloniaProperty.RegisterAttached<CommandBar, AppBarClosedDisplayMode>(
            "ClosedDisplayMode", typeof(CommandBarOptions), AppBarClosedDisplayMode.Compact);

    public static AppBarClosedDisplayMode GetClosedDisplayMode(CommandBar bar) => bar.GetValue(ClosedDisplayModeProperty);

    public static void SetClosedDisplayMode(CommandBar bar, AppBarClosedDisplayMode value) =>
        bar.SetValue(ClosedDisplayModeProperty, value);
}
