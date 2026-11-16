using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What a Pivot header's theme needs that a style can't say.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CharacterSpacingProperty"/> and <see cref="LetterSpacing"/>: WinRT's <c>CharacterSpacing</c>, in
/// thousandths of an em, held by resource key, and the <c>LetterSpacing</c> it makes, n × <c>FontSize</c> / 1000,
/// through a binding the theme sets on the header, which every text in it inherits, as WinRT's does.
/// </para>
/// <para>
/// <see cref="TranslationProperty"/>: the X of the phone's <c>ContentPresenterTranslateTransform</c>, which its
/// <c>UnselectedLocked</c> state sets to <c>PhonePivotLockedTranslation</c> (40), as a value a style can set by key and a
/// <c>DoubleTransition</c> can run, for the phone's 0.33 s generated transition; Avalonia's styles can't reach a
/// transform's own properties. It sets the element's <c>RenderTransform</c> to a <see cref="TranslateTransform"/> of
/// that X, and takes it away at 0.
/// </para>
/// </remarks>
internal static class PhonePivotHeader
{
    /// <summary>WinRT's <c>CharacterSpacing</c>, in thousandths of an em: data, for <see cref="LetterSpacing"/>.</summary>
    public static readonly AttachedProperty<int> CharacterSpacingProperty =
        AvaloniaProperty.RegisterAttached<Visual, int>("CharacterSpacing", typeof(PhonePivotHeader));

    /// <summary>
    /// Makes <c>LetterSpacing</c>, in view pixels, from a <c>FontSize</c> and a <c>CharacterSpacing</c>, in that order:
    /// n × size / 1000.
    /// </summary>
    public static readonly IMultiValueConverter LetterSpacing = new LetterSpacingConverter();

    /// <summary>The transform each element was given, to move and take back only that one.</summary>
    private static readonly ConditionalWeakTable<Visual, TranslateTransform> Made = new();

    /// <summary>How far right the element is drawn, in view pixels.</summary>
    public static readonly AttachedProperty<double> TranslationProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Translation", typeof(PhonePivotHeader));

    static PhonePivotHeader()
    {
        TranslationProperty.Changed.AddClassHandler<Visual>((visual, change) =>
        {
            // Only the transform this made is moved or taken away: one an app set is left alone.
            var x = change.GetNewValue<double>();
            var own = Made.TryGetValue(visual, out var made) && ReferenceEquals(visual.RenderTransform, made) ? made : null;
            if (x == 0)
            {
                if (own is not null)
                    visual.RenderTransform = null;
            }
            else if (own is not null)
            {
                own.X = x;                                // the transition's frames move the one transform
            }
            else if (visual.RenderTransform is null)
            {
                made = new TranslateTransform(x, 0);
                Made.AddOrUpdate(visual, made);
                visual.RenderTransform = made;
            }
        });
    }

    public static int GetCharacterSpacing(Visual visual) => visual.GetValue(CharacterSpacingProperty);

    public static void SetCharacterSpacing(Visual visual, int value) => visual.SetValue(CharacterSpacingProperty, value);

    public static double GetTranslation(Visual visual) => visual.GetValue(TranslationProperty);

    public static void SetTranslation(Visual visual, double value) => visual.SetValue(TranslationProperty, value);

    private sealed class LetterSpacingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [double size, int spacing] ? spacing * size / 1000 : 0.0;
    }
}
