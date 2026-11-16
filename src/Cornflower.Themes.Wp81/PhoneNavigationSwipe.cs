using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.GestureRecognizers;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's Frame has no edge swipe, and its NavigationPage theme turns Avalonia's off (<c>IsGestureEnabled</c>
/// false). But Avalonia 12.1.4's <see cref="NavigationPage"/> keeps its own <see cref="SwipeGestureRecognizer"/> on all
/// the same, sideways and enabled, and checks <c>IsGestureEnabled</c> only when the swipe it recognised comes to go back.
/// The recogniser still takes a finger that moves sideways, from whatever on a page pressed it: a RepeatButton held and
/// slid off and back lost its press, and its repeats never came back (the real-window run of #147). So, turned on by
/// the theme's setter, this keeps the recogniser's own <c>IsEnabled</c> to <c>IsGestureEnabled</c>, and gives it back
/// on when turned off.
/// </summary>
internal static class PhoneNavigationSwipe
{
    /// <summary>Set by Wp81's NavigationPage theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<NavigationPage, bool>("IsEnabled", typeof(PhoneNavigationSwipe));

    static PhoneNavigationSwipe()
    {
        IsEnabledProperty.Changed.AddClassHandler<NavigationPage>((page, _) => Follow(page));
        NavigationPage.IsGestureEnabledProperty.Changed.AddClassHandler<NavigationPage>((page, _) => Follow(page));
    }

    public static bool GetIsEnabled(NavigationPage page) => page.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(NavigationPage page, bool value) => page.SetValue(IsEnabledProperty, value);

    /// <summary>The page's swipe recognisers, enabled as its <c>IsGestureEnabled</c> says while this is on; else enabled.</summary>
    private static void Follow(NavigationPage page)
    {
        var enabled = !page.GetValue(IsEnabledProperty) || page.IsGestureEnabled;
        foreach (var recogniser in page.GestureRecognizers.OfType<SwipeGestureRecognizer>())
            recogniser.IsEnabled = enabled;
    }
}
