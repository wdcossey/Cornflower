using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What a Pivot header's theme needs that a style can't say, besides its character spacing
/// (<see cref="PhoneCharacterSpacing"/>): the X of the phone's <c>ContentPresenterTranslateTransform</c>, which its
/// <c>UnselectedLocked</c> state sets to <c>PhonePivotLockedTranslation</c> (40), as a value a style can set by key and a
/// <c>DoubleTransition</c> can run, for the phone's 0.33 s generated transition; Avalonia's styles can't reach a
/// transform's own properties. It sets the element's <c>RenderTransform</c> to a <see cref="TranslateTransform"/> of
/// that X, and takes it away at 0.
/// </summary>
internal static class PhonePivotHeader
{
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

    public static double GetTranslation(Visual visual) => visual.GetValue(TranslationProperty);

    public static void SetTranslation(Visual visual, double value) => visual.SetValue(TranslationProperty, value);
}
