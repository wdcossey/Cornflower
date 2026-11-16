using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// WinRT's <c>CharacterSpacing</c> for a theme: in thousandths of an em, held by resource key, and the
/// <c>LetterSpacing</c> it makes, n × <c>FontSize</c> / 1000, through a binding the theme sets on the element, which
/// every text in it inherits, as WinRT's does. <c>Tokens/</c> can't hold a value worked out from the element's own
/// <c>FontSize</c>. Set by the Pivot's header theme (−25) and the Hub's two headers (−22 and −10).
/// </summary>
internal static class PhoneCharacterSpacing
{
    /// <summary>WinRT's <c>CharacterSpacing</c>, in thousandths of an em: data, for <see cref="LetterSpacing"/>.</summary>
    public static readonly AttachedProperty<int> CharacterSpacingProperty =
        AvaloniaProperty.RegisterAttached<Visual, int>("CharacterSpacing", typeof(PhoneCharacterSpacing));

    /// <summary>
    /// Makes <c>LetterSpacing</c>, in view pixels, from a <c>FontSize</c> and a <c>CharacterSpacing</c>, in that order:
    /// n × size / 1000.
    /// </summary>
    public static readonly IMultiValueConverter LetterSpacing = new LetterSpacingConverter();

    public static int GetCharacterSpacing(Visual visual) => visual.GetValue(CharacterSpacingProperty);

    public static void SetCharacterSpacing(Visual visual, int value) => visual.SetValue(CharacterSpacingProperty, value);

    private sealed class LetterSpacingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [double size, int spacing] ? spacing * size / 1000 : 0.0;
    }
}
