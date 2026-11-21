using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cornflower.Controls;

/// <summary>
/// A jump list tile's background: WinRT's <c>JumpListItemBackgroundConverter</c>. Given a group, an
/// <see cref="IEnumerable"/> of its items as <see cref="ListBoxOptions.GroupedItemsSource"/> takes, it gives
/// <see cref="Enabled"/> for a group with items and <see cref="Disabled"/> for an empty one (or null).
/// </summary>
/// <remarks>
/// Left unset, each brush is the app's resource of WinRT's name, <c>JumpListDefaultEnabledBackground</c> or
/// <c>JumpListDefaultDisabledBackground</c>, in the app's theme (<see cref="Application.ActualThemeVariant"/>), not the
/// page's, as on the phone: a theme defines them (Wp81: the accent, and #FF1F1F1F dark, #FFDEDEDE light). A brush is
/// read as the tile is bound, so a change of the app's theme shows on tiles bound after it.
/// </remarks>
/// <example>
/// <code>&lt;Border Background="{Binding Converter={StaticResource JumpListItemBackgroundConverter}}" /&gt;</code>
/// </example>
public sealed class JumpListItemBackgroundConverter : IValueConverter
{
    /// <summary>A group with items: the app's <c>JumpListDefaultEnabledBackground</c> unless set.</summary>
    public IBrush? Enabled { get; set; }

    /// <summary>An empty group: the app's <c>JumpListDefaultDisabledBackground</c> unless set.</summary>
    public IBrush? Disabled { get; set; }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        JumpList.HasItems(value)
            ? Enabled ?? JumpList.AppBrush("JumpListDefaultEnabledBackground")
            : Disabled ?? JumpList.AppBrush("JumpListDefaultDisabledBackground");

    /// <summary>Not supported, as WinRT's.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// A jump list tile's text: WinRT's <c>JumpListItemForegroundConverter</c>. Given a group, it gives <see cref="Enabled"/>
/// for a group with items and <see cref="Disabled"/> for an empty one (or null).
/// </summary>
/// <remarks>
/// Left unset, each brush is the app's <c>JumpListDefaultEnabledForeground</c> or <c>JumpListDefaultDisabledForeground</c>,
/// in the app's theme, as <see cref="JumpListItemBackgroundConverter"/>'s are (Wp81: white on both, dark; light, white on
/// the accent and #FFA5A5A5 on an empty tile).
/// </remarks>
/// <example>
/// <code>&lt;TextBlock Foreground="{Binding Converter={StaticResource JumpListItemForegroundConverter}}" /&gt;</code>
/// </example>
public sealed class JumpListItemForegroundConverter : IValueConverter
{
    /// <summary>A group with items: the app's <c>JumpListDefaultEnabledForeground</c> unless set.</summary>
    public IBrush? Enabled { get; set; }

    /// <summary>An empty group: the app's <c>JumpListDefaultDisabledForeground</c> unless set.</summary>
    public IBrush? Disabled { get; set; }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        JumpList.HasItems(value)
            ? Enabled ?? JumpList.AppBrush("JumpListDefaultEnabledForeground")
            : Disabled ?? JumpList.AppBrush("JumpListDefaultDisabledForeground");

    /// <summary>Not supported, as WinRT's.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>What the jump list's converters and <see cref="SemanticZoom"/> share.</summary>
internal static class JumpList
{
    /// <summary>
    /// Whether a jump list's item is a group with items: an <see cref="IEnumerable"/> (not a string, a value) with one,
    /// or anything that isn't a group. Null and an empty group aren't.
    /// </summary>
    public static bool HasItems(object? value) => value switch
    {
        null => false,
        string => true,
        IEnumerable items => items.GetEnumerator() is var e && Advance(e),
        _ => true,
    };

    /// <summary>The app's brush of this key, in the app's theme; none if it has none.</summary>
    public static IBrush? AppBrush(string key) =>
        Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var found) && found is IBrush brush
            ? brush
            : null;

    private static bool Advance(IEnumerator e)
    {
        try
        {
            return e.MoveNext();
        }
        finally
        {
            (e as IDisposable)?.Dispose();
        }
    }
}
