using Avalonia;
using Avalonia.Animation;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Windows Phone 8.1 slide: the page transition a <c>Frame</c> runs with <c>SlideNavigationTransitionInfo</c>.
/// Pass it to <see cref="Avalonia.Controls.NavigationPage.PushAsync(Avalonia.Controls.Page, IPageTransition?)"/> and
/// <see cref="Avalonia.Controls.NavigationPage.PopAsync(IPageTransition?)"/>. <c>Wp81Theme</c> sets it as every
/// <see cref="Avalonia.Controls.NavigationPage"/>'s <see cref="Avalonia.Controls.NavigationPage.ModalTransition"/>.
/// </summary>
/// <remarks>
/// As measured on the emulator (<c>tools/refapp/README.md</c>, "Page transitions"; <c>tools/motion/slide.py</c>), in
/// animation time. Forward, the page going out goes at once, and the new page rises from 200 px below on an
/// <c>ExponentialEase</c> with exponent 6, easing out, over 350 ms. Back, the page going out slides down, faster each
/// frame (an exponential ease-in, exponent 4.28, reaching 135.9 px in 12 frames, 187.5 ms), and on the next frame the
/// page under it shows at once.
/// </remarks>
public sealed class SlideTransition : IPageTransition
{
    /// <inheritdoc />
    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken) =>
        PhonePageRun.Start(Motion.Instance, from, to, forward, cancellationToken);

    /// <summary>How far below its place the new page starts, in view px.</summary>
    internal const double InFrom = 200;

    /// <summary>How long the new page takes to rise.</summary>
    internal static readonly TimeSpan InLength = TimeSpan.FromMilliseconds(350);

    /// <summary>The rise's exponent, easing out.</summary>
    internal const double InExponent = 6;

    /// <summary>How far the page going out has slid when it goes, in view px.</summary>
    internal const double OutTo = 135.9;

    /// <summary>How long the page going out slides: 12 of the emulator's frames, the last showing it <see cref="OutTo"/> down.</summary>
    internal static readonly TimeSpan OutLength = TimeSpan.FromMilliseconds(187.5);

    /// <summary>When the page under it shows: a frame after the slide's last, one of the emulator's frames, 15.625 ms.</summary>
    internal static readonly TimeSpan OutShownUntil = OutLength + TimeSpan.FromMilliseconds(15.625);

    /// <summary>The slide out's exponent, easing in.</summary>
    internal const double OutExponent = 4.28;

    /// <summary>How far the new page is below its place <paramref name="time"/> after the first frame, forward.</summary>
    internal static double InOffsetAt(TimeSpan time) => PhoneEase.Remaining(InFrom, InExponent, time, InLength);

    /// <summary>How far the page going out has slid <paramref name="time"/> after the first frame, back.</summary>
    internal static double OutOffsetAt(TimeSpan time) => OutTo * PhoneEase.ExponentialIn(OutExponent, time / OutLength);

    private sealed class Motion : IPhonePageMotion
    {
        public static readonly Motion Instance = new();

        public bool GoesBackAsThePageCame => false;

        public TimeSpan Length(bool forward) => forward ? InLength : OutShownUntil;

        public (PageLook From, PageLook To) LookAt(TimeSpan time, bool forward, Size size) => forward
            ? (PageLook.Hidden, PageLook.Down(InOffsetAt(time)))
            : (PageLook.Down(OutOffsetAt(time)), PageLook.Hidden);
    }
}
