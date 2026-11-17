using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The 8.1 ScrollBar's indicator fades, on a panning root (a <see cref="Track"/> in the Wp81 ScrollBar theme), frame by
/// frame on <see cref="CornflowerClock"/>. Turned on by the ScrollBar theme's own setter. It follows the ScrollBar's
/// <see cref="PhoneScrollIndicators.IndicatorModeProperty"/> and <see cref="ScrollBar.AllowAutoHide"/>.
/// </summary>
/// <remarks>
/// <para>As run <c>20261115-1946-dev11</c>'s recording read them (tools/refapp/README.md, "the panning indicator"):
/// <c>TouchIndicator</c>'s <c>FadeInThemeAnimation</c> takes the root from 0 to 1 in <see cref="FadeIn"/>, and
/// <c>NoIndicator</c>'s <c>FadeOutThemeAnimation</c> from 1 to 0 in <see cref="FadeOut"/>, both nearly linear, each
/// starting from wherever the other had got to (an indicator shown only briefly fades out from 55 to 65%). So the
/// opacity moves at a constant rate toward its target. <c>MouseIndicator</c> runs no animation: the root is shown at
/// once; so is it while <c>AllowAutoHide</c> is off.</para>
/// <para>Avalonia's <c>Transitions</c> can't do it: a style that changes the duration with the target sets a new
/// <c>Transitions</c>, which stops the running one, and they run on Avalonia's own clock, which a test can't move.</para>
/// <para>It sets only the root's <c>Opacity</c>, as a local value. Turned off, it leaves the root at 1, setting rather
/// than clearing, so it takes no value away inside a theme's unsetting (spike 6). It asks for frames only while a fade
/// runs.</para>
/// </remarks>
internal static class PhoneIndicatorFade
{
    /// <summary>The fade in: about 370 ms from 0 to 1 (2.7 a second), by the recording's clock.</summary>
    internal static readonly TimeSpan FadeIn = TimeSpan.FromMilliseconds(370);

    /// <summary>The fade out: about 340 ms from 1 to 0 (2.95 a second), by the recording's clock.</summary>
    internal static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(340);

    /// <summary>Set by the Wp81 ScrollBar theme on its panning roots.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Track, bool>("IsEnabled", typeof(PhoneIndicatorFade));

    private static readonly ConditionalWeakTable<Track, Fader> Faders = new();

    /// <summary>Each ScrollBar's fading root: the one of its orientation, which the theme turns on.</summary>
    private static readonly ConditionalWeakTable<ScrollBar, Fader> ByBar = new();

    static PhoneIndicatorFade()
    {
        IsEnabledProperty.Changed.AddClassHandler<Track>((track, change) =>
        {
            if (Faders.TryGetValue(track, out var old))
            {
                Faders.Remove(track);
                if (track.TemplatedParent is ScrollBar oldBar && ByBar.TryGetValue(oldBar, out var held) && held == old)
                    ByBar.Remove(oldBar);
                old.Dispose();
                track.SetValue(Visual.OpacityProperty, 1d);
            }

            if (change.GetNewValue<bool>())
            {
                var fader = new Fader(track);
                Faders.Add(track, fader);
                if (track.TemplatedParent is ScrollBar bar)
                    ByBar.AddOrUpdate(bar, fader);
            }
        });

        PhoneScrollIndicators.IndicatorModeProperty.Changed.AddClassHandler<ScrollBar>((bar, _) => Retarget(bar));
        ScrollBar.AllowAutoHideProperty.Changed.AddClassHandler<ScrollBar>((bar, _) => Retarget(bar));
    }

    public static bool GetIsEnabled(Track track) => track.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Track track, bool value) => track.SetValue(IsEnabledProperty, value);

    private static void Retarget(ScrollBar bar)
    {
        if (ByBar.TryGetValue(bar, out var fader))
            fader.Retarget(animate: true);
    }

    /// <summary>One panning root's fade.</summary>
    private sealed class Fader : IDisposable
    {
        private readonly Track _track;
        private IDisposable? _frame;
        private TimeSpan _last;
        private double _speed = 1;                        // read from CornflowerConfig as the fade starts

        public Fader(Track track)
        {
            _track = track;
            track.DetachedFromVisualTree += OnDetached;
            Retarget(animate: false);
        }

        private ScrollBar? Bar => _track.TemplatedParent as ScrollBar;

        public void Dispose()
        {
            _track.DetachedFromVisualTree -= OnDetached;
            Stop();
        }

        private void Stop()
        {
            _frame?.Dispose();
            _frame = null;
        }

        /// <summary>Out of the window there are no frames to fade on: the root goes straight to where it was going.</summary>
        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Retarget(animate: false);

        /// <summary>Where the root is going, and how long a whole fade there takes; zero for at once.</summary>
        private (double Target, TimeSpan Duration) Goal()
        {
            if (Bar is not { } bar)
                return (1, TimeSpan.Zero);
            if (!bar.AllowAutoHide)
                return (1, TimeSpan.Zero);
            return PhoneScrollIndicators.GetIndicatorMode(bar) switch
            {
                PhoneIndicatorMode.TouchIndicator => (1, FadeIn),
                PhoneIndicatorMode.MouseIndicator => (1, TimeSpan.Zero),
                _ => (0, FadeOut),
            };
        }

        public void Retarget(bool animate)
        {
            var (target, duration) = Goal();
            // With CornflowerConfig.Animated off, at the target at once.
            if (!animate || duration == TimeSpan.Zero || !_track.IsAttachedToVisualTree() || !CornflowerConfig.Animated)
            {
                Stop();
                _track.SetValue(Visual.OpacityProperty, target);
                return;
            }

            if (_frame is null && _track.Opacity != target)
            {
                _last = CornflowerClock.Now;
                _speed = CornflowerClock.SpeedOf(MotionArea.Control);
                _frame = CornflowerClock.RequestFrame(_track, Step);
            }
        }

        private void Step()
        {
            _frame = null;
            var (target, duration) = Goal();
            var now = CornflowerClock.Now;
            var step = duration == TimeSpan.Zero ? 1 : CornflowerClock.AtSpeed(now - _last, _speed).TotalMilliseconds / duration.TotalMilliseconds;
            _last = now;

            var opacity = _track.Opacity;
            opacity = target > opacity ? Math.Min(target, opacity + step) : Math.Max(target, opacity - step);
            _track.SetValue(Visual.OpacityProperty, opacity);

            if (opacity != target)
                _frame = CornflowerClock.RequestFrame(_track, Step);
        }
    }
}
