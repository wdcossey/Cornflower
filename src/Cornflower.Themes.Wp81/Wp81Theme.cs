using System.Runtime.CompilerServices;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Windows Phone 8.1 theme. Add to <c>Application.Styles</c> in place of <c>FluentTheme</c>.
/// It merges every file under <c>Tokens/</c>, <c>Glyphs/</c> and <c>Controls/</c>, found at build time, so a
/// control's theme is added by adding its file.
/// </summary>
/// <example>
/// <code>
/// &lt;Application.Styles&gt;
///     &lt;wp81:Wp81Theme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </example>
public class Wp81Theme : Styles
{
    private static readonly Uri BaseUri = new("avares://Cornflower.Themes.Wp81/");

    public Wp81Theme(IServiceProvider? _ = null)
    {
        // The flyouts' binders hear each flyout open from IsOpen's class handler, which their type's static setup adds.
        // Left to the first presenter the theme styles, that setup would run in the middle of a first flyout opening by
        // IsOpen, after Avalonia had taken its list of handlers: that flyout would be heard by none (#209).
        RuntimeHelpers.RunClassConstructor(typeof(PhoneFlyout).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(PhoneMenuFlyout).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(PhonePickerFlyout).TypeHandle);
        foreach (var path in OrderForMerge(XamlManifest.Paths))
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri(BaseUri, path) });
        }
    }

    /// <summary>Glob order differs between file systems, so merge order is fixed here.</summary>
    internal static IEnumerable<string> OrderForMerge(IEnumerable<string> paths) =>
        paths.Order(StringComparer.Ordinal);
}
