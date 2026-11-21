using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's FlipView on Avalonia's <c>Carousel</c> (#214): turned on by the Wp81 theme, the Carousel's items follow
/// a finger and snap to the next or back as the phone's do, and a change of one to <c>SelectedIndex</c> animates
/// (<see cref="PhoneFlipViewMotion"/>). The items are laid out by <see cref="PhoneFlipViewPanel"/>, the theme's items
/// panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>The axis</b> is Avalonia's: a Carousel whose <c>PageTransition</c> is a <c>PageSlide</c>, or a composite holding
/// one, slides along that <c>PageSlide</c>'s <c>Orientation</c>, as Avalonia's own keys and layout read it; any other,
/// or none, slides sideways. The phone's FlipView is turned up and down by a vertical items panel instead; here the
/// panel is the theme's.
/// </para>
/// <para>
/// <b>The PageTransition itself never runs.</b> Avalonia runs it in its <c>VirtualizingCarouselPanel</c>, which the
/// theme's panel replaces, so it can't fight the motion: only its axis is read.
/// </para>
/// </remarks>
internal static class PhoneFlipView
{
    /// <summary>Set by the theme: the Carousel swipes and snaps as the phone's FlipView does.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Carousel, bool>("IsEnabled", typeof(PhoneFlipView));

    private static readonly AttachedProperty<PhoneFlipViewMotion?> MotionProperty =
        AvaloniaProperty.RegisterAttached<Carousel, PhoneFlipViewMotion?>("Motion", typeof(PhoneFlipView));

    static PhoneFlipView()
    {
        IsEnabledProperty.Changed.AddClassHandler<Carousel>((carousel, change) =>
        {
            if (change.GetNewValue<bool>() && carousel.GetValue(MotionProperty) is null)
                carousel.SetValue(MotionProperty, new PhoneFlipViewMotion(carousel));
            carousel.GetValue(MotionProperty)?.Rest();
        });
    }

    public static bool GetIsEnabled(Carousel carousel) => carousel.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Carousel carousel, bool value) => carousel.SetValue(IsEnabledProperty, value);

    /// <summary>The motion of <paramref name="carousel"/>, or null if the theme has never turned it on.</summary>
    internal static PhoneFlipViewMotion? MotionOf(Carousel carousel) => carousel.GetValue(MotionProperty);

    /// <summary>
    /// Where <paramref name="carousel"/>'s strip is, in pages from item 0: the motion's, while it is on; else the
    /// selected item's.
    /// </summary>
    internal static double PositionOf(Carousel carousel) =>
        GetIsEnabled(carousel) && MotionOf(carousel) is { } motion ? motion.Position : Math.Max(0, carousel.SelectedIndex);

    /// <summary>Whether <paramref name="carousel"/> slides up and down: its <c>PageTransition</c>'s <c>PageSlide</c> is vertical.</summary>
    internal static bool IsVertical(Carousel carousel) => SlideOf(carousel.PageTransition)?.Orientation == PageSlide.SlideAxis.Vertical;

    private static PageSlide? SlideOf(IPageTransition? transition) => transition switch
    {
        PageSlide slide => slide,
        CompositePageTransition composite => composite.PageTransitions.OfType<PageSlide>().FirstOrDefault(),
        _ => null,
    };
}
