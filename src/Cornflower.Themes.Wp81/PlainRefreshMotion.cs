using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The plain RefreshVisualizer's moves (ADR 0007, #241): Avalonia's RefreshContainer gives the visualizer's and the
/// content ScrollViewer's composition visuals implicit animations, so the band and the list ease over 150 ms after the
/// finger, and slide back when a refresh ends. With no motion of the theme's own (rule 8) they follow the pull as it
/// is: at each change of the visualizer's state, before Avalonia moves them, their implicit animations are taken away.
/// Every pull begins with a state change (Idle to Interacting or Peeking), so no move of a pull is ever eased.
/// </summary>
internal static class PlainRefreshMotion
{
    /// <summary>Set by the plain RefreshVisualizer theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<RefreshVisualizer, bool>("IsEnabled", typeof(PlainRefreshMotion));

    static PlainRefreshMotion()
    {
        // Raised before the visualizer updates its content and offsets (its state's setter raises, then updates).
        RefreshVisualizer.RefreshVisualizerStateProperty.Changed.AddClassHandler<RefreshVisualizer>((visualizer, _) => Still(visualizer));
    }

    public static bool GetIsEnabled(RefreshVisualizer visualizer) => visualizer.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(RefreshVisualizer visualizer, bool value) => visualizer.SetValue(IsEnabledProperty, value);

    private static void Still(RefreshVisualizer visualizer)
    {
        if (!visualizer.GetValue(IsEnabledProperty))
            return;
        Unease(visualizer);
        // The ScrollViewer Avalonia's adapter moves: the first in the container's content.
        if (visualizer.FindAncestorOfType<RefreshContainer>() is { } container
            && container.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is { } scroller)
            Unease(scroller);
    }

    private static void Unease(Visual visual)
    {
        if (ElementComposition.GetElementVisual(visual) is { ImplicitAnimations: not null } composition)
            composition.ImplicitAnimations = null;
    }
}
