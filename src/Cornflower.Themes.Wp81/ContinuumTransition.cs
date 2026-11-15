using Avalonia;
using Avalonia.Animation;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Windows Phone 8.1 continuum, as a whole page: the page transition a <c>Frame</c> runs with
/// <c>ContinuumNavigationTransitionInfo</c>. Pass it to
/// <see cref="Avalonia.Controls.NavigationPage.PushAsync(Avalonia.Controls.Page, IPageTransition?)"/> and
/// <see cref="Avalonia.Controls.NavigationPage.PopAsync(IPageTransition?)"/>.
/// </summary>
/// <remarks>
/// As measured on the emulator's recording (<c>tools/refapp/README.md</c>, "Page transitions"; <c>tools/motion/slide.py</c>,
/// and the fits in <c>docs/controls/Page/PLAN.md</c>), by the recording's clock, its frames about 31 ms apart. Forward,
/// the page going out fades in 62.5 ms, then the new page grows about its centre from a scale of 0.943 to 1 on an
/// exponential ease-out (exponent 4.61) over 359 ms. Back, the page going out slides down as in the slide's back, then
/// the page under it fades in from 0.393 on an exponential ease-out (exponent 3.85) over 344 ms. The element the
/// phone's continuum flies out of the old page (<c>ContinuumNavigationTransitionInfo.ExitElement</c>) isn't measured,
/// and isn't here.
/// </remarks>
public sealed class ContinuumTransition : IPageTransition
{
    /// <inheritdoc />
    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken) =>
        PhonePageRun.Start(Motion.Instance, from, to, forward, cancellationToken);

    /// <summary>How long the page going out takes to fade, forward.</summary>
    internal static readonly TimeSpan FadeLength = TimeSpan.FromMilliseconds(62.5);

    /// <summary>The scale the new page grows from, forward.</summary>
    internal const double GrowFrom = 0.9427;

    /// <summary>How long the new page grows, forward.</summary>
    internal static readonly TimeSpan GrowLength = TimeSpan.FromMilliseconds(359);

    /// <summary>The growth's exponent, easing out.</summary>
    internal const double GrowExponent = 4.61;

    /// <summary>The opacity the page under it fades in from, back.</summary>
    internal const double FadeInFrom = 0.3933;

    /// <summary>How long the page under it fades in, back.</summary>
    internal static readonly TimeSpan FadeInLength = TimeSpan.FromMilliseconds(344);

    /// <summary>The fade's exponent, easing out.</summary>
    internal const double FadeInExponent = 3.85;

    /// <summary>The page going out's opacity <paramref name="time"/> after the first frame, forward.</summary>
    internal static double FadeOutAt(TimeSpan time) => Math.Max(0, 1 - time / FadeLength);

    /// <summary>The new page's scale <paramref name="time"/> after the first frame, forward; null while it isn't shown.</summary>
    internal static double? ScaleAt(TimeSpan time) => time < FadeLength
        ? null
        : 1 - PhoneEase.Remaining(1 - GrowFrom, GrowExponent, time - FadeLength, GrowLength);

    /// <summary>The page under it's opacity <paramref name="time"/> after the first frame, back; null while it isn't shown.</summary>
    internal static double? FadeInAt(TimeSpan time) => time < SlideTransition.OutShownUntil
        ? null
        : 1 - PhoneEase.Remaining(1 - FadeInFrom, FadeInExponent, time - SlideTransition.OutShownUntil, FadeInLength);

    private sealed class Motion : IPhonePageMotion
    {
        public static readonly Motion Instance = new();

        public TimeSpan Length(bool forward) => forward ? FadeLength + GrowLength : SlideTransition.OutShownUntil + FadeInLength;

        public (PageLook From, PageLook To) LookAt(TimeSpan time, bool forward, Size size)
        {
            if (forward)
            {
                return ScaleAt(time) is { } scale
                    ? (PageLook.Hidden, PageLook.Scaled(scale, size))
                    : (PageLook.Faded(FadeOutAt(time)), PageLook.Hidden);
            }

            return FadeInAt(time) is { } opacity
                ? (PageLook.Hidden, PageLook.Faded(opacity))
                : (PageLook.Down(SlideTransition.OutOffsetAt(time)), PageLook.Hidden);
        }
    }
}
