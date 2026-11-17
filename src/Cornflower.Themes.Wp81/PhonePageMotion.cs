using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>How a page looks at one moment of a transition: its opacity, and where it is drawn.</summary>
/// <param name="Opacity">0 hides the page; 1 is its own.</param>
/// <param name="Transform">
/// Where a point of the page is drawn, in its own space with the origin at its top left (Avalonia's row-vector
/// convention, perspective included); null for where the page is at rest.
/// </param>
internal readonly record struct PageLook(double Opacity, Matrix? Transform)
{
    /// <summary>The page not drawn.</summary>
    public static readonly PageLook Hidden = new(0, null);

    /// <summary>The page at rest but at <paramref name="opacity"/>.</summary>
    public static PageLook Faded(double opacity) => new(opacity, null);

    /// <summary>The page moved down by <paramref name="offset"/>.</summary>
    public static PageLook Down(double offset) => new(1, Matrix.CreateTranslation(0, offset));

    /// <summary>The page scaled by <paramref name="scale"/> about the centre of <paramref name="size"/>.</summary>
    public static PageLook Scaled(double scale, Size size) =>
        new(1, Matrix.CreateTranslation(-size.Width / 2, -size.Height / 2) * Matrix.CreateScale(scale, scale) *
               Matrix.CreateTranslation(size.Width / 2, size.Height / 2));
}

/// <summary>One of the phone's page transitions: how long it runs, and how its two pages look at each moment.</summary>
internal interface IPhonePageMotion
{
    /// <summary>How long the transition runs, forward (a new page) or back.</summary>
    TimeSpan Length(bool forward);

    /// <summary>
    /// The page going out and the page coming in, <paramref name="time"/> after the first frame, for pages of
    /// <paramref name="size"/>. Not asked at or after <see cref="Length"/>: then both are at rest and the page going out is
    /// hidden.
    /// </summary>
    (PageLook From, PageLook To) LookAt(TimeSpan time, bool forward, Size size);

    /// <summary>
    /// Whether, run back, this transition gives way to the one the page going out came with: the default's, the
    /// turnstile's, as the phone's <c>Frame.GoBack</c> plays the reverse of the page's own transition.
    /// </summary>
    bool GoesBackAsThePageCame { get; }
}

/// <summary>
/// Runs a <see cref="IPhonePageMotion"/> between two pages, frame by frame on <see cref="CornflowerClock"/>, as
/// Avalonia's <see cref="IPageTransition"/> contract has it: the page coming in is made visible at the start, and the
/// page going out is hidden at the end. Each page's look is set at animation priority, over whatever its host or the app
/// sets, and taken away at the end or when the transition is cancelled, so neither page keeps any of it.
/// </summary>
/// <remarks>
/// Time 0 is the first frame: the first frame drawn shows the transition's first values, as the emulator's first frame
/// of each transition showed (the turnstile's −67.0° and +42.0°, the slide's 200 px; <c>tools/refapp/README.md</c>,
/// "Page transitions"). The pages are sized as their host is (both of a <see cref="NavigationPage"/>'s presenters fill
/// it), as the page coming in has its content but hasn't been laid out yet when the transition starts.
/// </remarks>
internal sealed class PhonePageRun
{
    private readonly IPhonePageMotion _motion;
    private readonly Visual? _from;
    private readonly Visual? _to;
    private readonly bool _forward;
    // Its continuations run on later, not inside a frame callback or inside the token's Cancel, where a host such as
    // NavigationPage may be part-way through leaving its window.
    private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Shown _fromShown = new(goingOut: true);
    private readonly Shown _toShown = new(goingOut: false);
    private readonly CancellationTokenRegistration _cancel;
    private IDisposable? _pending;
    private TimeSpan? _start;
    // Read from CornflowerConfig as the transition starts.
    private readonly double _speed = CornflowerConfig.SpeedOf(MotionArea.Page);
    private bool _over;

    private PhonePageRun(IPhonePageMotion motion, Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        _motion = motion;
        _from = from;
        _to = to;
        _forward = forward;
        if (_to is not null)
        {
            _to.IsVisible = true;
        }

        Show(TimeSpan.Zero);
        _pending = CornflowerClock.RequestFrame(_to ?? _from!, Step);
        // A token cancels on the thread that cancels it: Avalonia's NavigationPage cancels on the UI thread, before it
        // starts the next transition on the same presenters, so the looks are gone before that one sets its own.
        _cancel = cancellationToken.Register(Cancel);
    }

    /// <summary>Starts <paramref name="motion"/> from <paramref name="from"/> to <paramref name="to"/>; the task ends with it.</summary>
    public static Task Start(IPhonePageMotion motion, Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (from is null && to is null)
        {
            return Task.CompletedTask;
        }

        if (forward)
        {
            // The page coming in remembers how it came, for its way back.
            PageOf(to)?.SetValue(CameWithProperty, motion);
        }
        else if (motion.GoesBackAsThePageCame && PageOf(from)?.GetValue(CameWithProperty) is { } cameWith)
        {
            motion = cameWith;
        }

        // With CornflowerConfig.Animated off, the end at once: the page coming in shown, the one going out hidden.
        if (!CornflowerConfig.Animated)
        {
            if (to is not null)
                to.IsVisible = true;
            if (from is not null)
                from.IsVisible = false;
            return Task.CompletedTask;
        }

        return new PhonePageRun(motion, from, to, forward, cancellationToken)._done.Task;
    }

    /// <summary>
    /// The transition a page came with, on the page: so the default going back (NavigationPage's <c>PopAsync()</c>, its
    /// back button, Alt+Left, a back request through a <c>PageNavigationHost</c>) goes back as it came.
    /// </summary>
    internal static readonly AttachedProperty<IPhonePageMotion?> CameWithProperty =
        AvaloniaProperty.RegisterAttached<PhonePageRun, AvaloniaObject, IPhonePageMotion?>("CameWith");

    /// <summary>The page a transition moves: a presenter's content (NavigationPage's), or the visual itself.</summary>
    private static AvaloniaObject? PageOf(Visual? visual) =>
        visual is ContentPresenter { Content: AvaloniaObject content } ? content : visual;

    private Size Size
    {
        get
        {
            var page = _to ?? _from!;
            var host = page.GetVisualParent();
            return host is { Bounds.Size: { Width: > 0, Height: > 0 } size } ? size : page.Bounds.Size;
        }
    }

    private void Step()
    {
        _pending = null;
        if (_over)
        {
            return;
        }

        var now = CornflowerClock.Now;
        _start ??= now;
        var time = CornflowerClock.AtSpeed(now - _start.Value, _speed);
        if (time >= _motion.Length(_forward))
        {
            Finish();
            return;
        }

        Show(time);
        _pending = CornflowerClock.RequestFrame(_to ?? _from!, Step);
    }

    private void Show(TimeSpan time)
    {
        var (from, to) = _motion.LookAt(time, _forward, Size);
        _fromShown.Show(_from, from);
        _toShown.Show(_to, to);
    }

    /// <summary>The end: <see cref="Stop"/>, the task done.</summary>
    private void Finish() => Stop(cancelled: false);

    /// <summary>
    /// Given up: <see cref="Stop"/>, the task cancelled. A token can be cancelled on any thread; the pages are the UI
    /// thread's, so the stop is posted there unless it is already on it.
    /// </summary>
    private void Cancel()
    {
        if (Dispatcher.UIThread.CheckAccess())
            Stop(cancelled: true);
        else
            Dispatcher.UIThread.Post(() => Stop(cancelled: true));
    }

    /// <summary>
    /// The end, or given up: the page going out hidden, as Avalonia's own transitions leave it (NavigationPage doesn't
    /// hide it after a cancel, and on a pop it is on top), then both pages without their looks.
    /// </summary>
    private void Stop(bool cancelled)
    {
        if (_over)
        {
            return;
        }

        End();
        if (_from is not null)
        {
            _from.IsVisible = false;
        }

        _fromShown.Clear();
        _toShown.Clear();
        if (cancelled)
            _done.TrySetCanceled();
        else
            _done.TrySetResult();
    }

    private void End()
    {
        _over = true;
        _pending?.Dispose();
        _pending = null;
        _cancel.Dispose();
    }

    /// <summary>
    /// One page's look, held at animation priority: its opacity, and one transform changed in place. The page going out
    /// takes no input from the start (it is leaving, and fading or dropping it may still show); the page coming in takes
    /// none while it is hidden (Avalonia's hit test doesn't look at opacity, and a pop's page going out is on top).
    /// </summary>
    private sealed class Shown(bool goingOut)
    {
        private readonly MatrixTransform _transform = new();
        private IDisposable? _opacity;
        private IDisposable? _unhit;
        private double? _opacityShown;
        private IDisposable? _transformShown;
        private IDisposable? _origin;

        public void Show(Visual? page, PageLook look)
        {
            if (page is null)
            {
                return;
            }

            if (look.Opacity != _opacityShown)
            {
                _opacity?.Dispose();
                _opacity = look.Opacity == 1 ? null : page.SetValue(Visual.OpacityProperty, look.Opacity, BindingPriority.Animation);
                _opacityShown = look.Opacity;
                var takesNoInput = goingOut || look.Opacity == 0;
                if (takesNoInput != _unhit is not null)
                {
                    _unhit?.Dispose();
                    _unhit = takesNoInput && page is InputElement input
                        ? input.SetValue(InputElement.IsHitTestVisibleProperty, false, BindingPriority.Animation)
                        : null;
                }
            }

            if (look.Transform is { } matrix)
            {
                // The look is about the page's top left; drawn about its origin, which is set there too.
                _transform.Matrix = matrix;
                _transformShown ??= page.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
                _origin ??= page.SetValue(Visual.RenderTransformOriginProperty, RelativePoint.TopLeft, BindingPriority.Animation);
            }
            else
            {
                ClearTransform();
            }
        }

        public void Clear()
        {
            _opacity?.Dispose();
            _opacity = null;
            _unhit?.Dispose();
            _unhit = null;
            _opacityShown = null;
            ClearTransform();
        }

        private void ClearTransform()
        {
            _transformShown?.Dispose();
            _transformShown = null;
            _origin?.Dispose();
            _origin = null;
        }
    }
}
