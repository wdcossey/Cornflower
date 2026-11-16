using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Reactive;
using Avalonia.Threading;
using Cornflower.Sample.Diagnostics;

namespace Cornflower.Sample;

/// <summary>
/// The sample's catalogue: the list of <see cref="SamplePageAttribute"/> pages, a page, and Back.
/// <see cref="MainWindow"/> holds one on desktop, and <see cref="App"/> shows one on a phone. Back is the Back button,
/// or Avalonia's back request (Alt+Left on desktop, the back button or gesture on a phone) once nothing else on its
/// route has taken it.
/// <para>
/// The pages are a <see cref="NavigationPage"/>'s, Wp81's Frame: the list is its first page, and each sample page is
/// pushed onto it in a <see cref="ContentPage"/>, so it comes in with the theme's turnstile and Back turns back to the
/// list. The navigation page drops a navigation asked for while another runs, so the shell asks again once it ends:
/// whatever was asked for last is where it goes.
/// </para>
/// <para>
/// The shell times every page it opens, from navigation to the page's first drawn frame, mid-turn, and then, once the
/// turn is over and the page is drawn at rest, writes the page's <c>CORNFLOWER ready</c> line to standard output
/// (logcat's <c>DOTNET</c> tag on Android), which <c>tools/android/run.ps1</c> and <c>tools/real-window/drive.ps1</c>
/// read, so a frame they take after it is the page at rest. A page has no timing code of its own. In a Debug build it
/// also writes what the page cost to open, to its first frame, a <c>CORNFLOWER cost</c> line, and shows it up the rail
/// (<see cref="PerformanceMonitor"/>).
/// </para>
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>What every ready line starts with.</summary>
    internal const string ReadyMarker = "CORNFLOWER ready";

    /// <summary>What the Debug line starts with, written after the page's ready line.</summary>
    internal const string CostMarker = "CORNFLOWER cost";

    private static IReadOnlyList<SamplePageEntry>? _pages;

    /// <summary>
    /// Every page in the sample, found by its attribute. Found on first use rather than in a static
    /// initialiser, so that a page that can't be made fails with its own message, not a type initialiser's.
    /// </summary>
    internal static IReadOnlyList<SamplePageEntry> Pages => _pages ??= SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private TopLevel? _top;
    private IDisposable? _backRequests;

    /// <summary>The page the shell shows, or is turning to: a sample page's <see cref="ContentPage"/>; null for the list.</summary>
    private ContentPage? _wanted;

    /// <summary>Whether the shell is taking <see cref="Host"/> to <see cref="_wanted"/>, one navigation after another.</summary>
    private bool _navigating;

    /// <summary>
    /// The page being opened, from its navigation until its ready line is written, or until the shell leaves it or its
    /// window; then null.
    /// </summary>
    private Opening? _opening;

    /// <summary>The sample's catalogue, <see cref="Pages"/>.</summary>
    public SampleShell()
        : this(Pages)
    {
    }

    /// <summary>A catalogue of <paramref name="catalogue"/>, for a test's own pages.</summary>
    internal SampleShell(IReadOnlyList<SamplePageEntry> catalogue)
    {
        Catalogue = catalogue;
        InitializeComponent();
        // The navigation page's own way back (Alt+Left in a page) goes to the list without the shell: it follows, as the
        // turn back begins.
        Host.PropertyChanged += (_, change) =>
        {
            if (change.Property == Page.CurrentPageProperty)
                Followed();
        };
        foreach (var page in catalogue)
        {
            var open = new Button { Content = page.Title, HorizontalAlignment = HorizontalAlignment.Stretch };
            open.Click += (_, _) => Open(page);
            PageList.Children.Add(open);
        }
    }

    /// <summary>The pages on the list.</summary>
    internal IReadOnlyList<SamplePageEntry> Catalogue { get; }

    /// <summary>Where each ready line, and in Debug each cost line, goes. Standard output; a test replaces it.</summary>
    internal Action<string> Report { get; set; } = Console.WriteLine;

    /// <summary>
    /// Whether Back has a rail of its own beside the page, as on desktop; on by default. Off, as on a phone, the page
    /// has the shell's whole width, and the platform's back request is Back.
    /// </summary>
    public bool HasBackRail
    {
        get => Rail.IsVisible;
        set => Rail.IsVisible = value;
    }

    /// <summary>Whether a page is shown, rather than the list, or being turned to.</summary>
    public bool IsShowingPage => _wanted is not null;

    /// <summary>The page shown, or the list: what the shell shows, or is turning to.</summary>
    internal object? Shown => _wanted is { } wanted ? wanted.Content : Index;

    /// <summary>Shows the list.</summary>
    public void ShowIndex()
    {
        Abandon();
        HidePerformance();
        BackButton.IsVisible = false;
        _wanted = null;
        Navigate();
    }

    /// <summary>
    /// Makes the page called <paramref name="title"/> and shows it, as its button on the list does. Titles are matched
    /// without regard to case, as the catalogue already keeps apart titles that differ only in case.
    /// </summary>
    /// <exception cref="ArgumentException">No page has the title; the message lists those there are.</exception>
    public void Open(string title)
    {
        var page = Catalogue.FirstOrDefault(entry => string.Equals(entry.Title, title, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                $"The sample has no page titled '{title}'. Its pages are: {string.Join(", ", Catalogue.Select(entry => $"'{entry.Title}'"))}.",
                nameof(title));
        Open(page);
    }

    /// <summary>Makes <paramref name="page"/> and shows it, timed from before it's made, because styles apply as soon as it's attached.</summary>
    public void Open(SamplePageEntry page)
    {
        var started = Stopwatch.GetTimestamp();
        Show(page.Create(), page.Title, started);
    }

    /// <summary>Shows <paramref name="page"/>, made by a test, under <paramref name="title"/>, timed from now.</summary>
    internal void ShowPage(Control page, string title) => Show(page, title, Stopwatch.GetTimestamp());

    /// <summary>The ready line: the time to open, what the device reports, and the page's title, last, as it can have spaces.</summary>
    internal static string ReadyLine(TimeSpan opened, string facts, string title) =>
        string.Create(CultureInfo.InvariantCulture, $"{ReadyMarker} open={opened.TotalMilliseconds:0}ms {facts} title={title}");

    private void Show(Control page, string title, long started)
    {
        Abandon();
        HidePerformance();
        BackButton.IsVisible = true;
        _wanted = new ContentPage
        {
            Content = page,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        _opening = new Opening(page, _wanted, title, started);
        Navigate();
        AwaitFirstFrame();
    }

    /// <summary>Takes <see cref="Host"/> to <see cref="_wanted"/>, unless it is already on its way.</summary>
    private void Navigate()
    {
        if (!_navigating)
            _ = NavigateAsync();
    }

    /// <summary>
    /// One navigation after another until <see cref="Host"/> is at the page asked for last: pushed from the list, put
    /// in place of another page, or popped back to the list. Each waits for the one before, as the navigation page
    /// drops one asked for while another runs.
    /// </summary>
    private async Task NavigateAsync()
    {
        _navigating = true;
        try
        {
            while (true)
            {
                Page wanted = (Page?)_wanted ?? IndexPage;
                var current = Host.CurrentPage;
                if (ReferenceEquals(current, wanted))
                    break;
                if (Host.IsNavigating)
                {
                    await NavigationEnds();
                    continue;
                }

                var step = ReferenceEquals(wanted, IndexPage) ? Host.PopToRootAsync()
                    : ReferenceEquals(current, IndexPage) ? Host.PushAsync(wanted)
                    : Host.ReplaceAsync(wanted);
                try
                {
                    await step;
                }
                catch (OperationCanceledException)
                {
                    // Given up for another navigation: the loop goes on from wherever the host is.
                }

                // A host that didn't move and isn't moving won't: stop rather than ask for ever.
                if (ReferenceEquals(Host.CurrentPage, current) && !Host.IsNavigating)
                    break;
            }
        }
        catch (Exception e)
        {
            // Not lost on an unobserved task: thrown on the UI thread, where the app sees it.
            Dispatcher.UIThread.Post(() => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(e));
        }
        finally
        {
            _navigating = false;
        }

        if (_opening is { } opening && ReferenceEquals(Host.CurrentPage, opening.Holder))
            Arrived(opening);
    }

    /// <summary>Completes when <see cref="Host"/>'s navigation in progress ends.</summary>
    private Task NavigationEnds()
    {
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var watch = Host.GetObservable(NavigationPage.IsNavigatingProperty)
            .Subscribe(new AnonymousObserver<bool>(navigating =>
            {
                if (!navigating)
                    ended.TrySetResult();
            }));
        _ = ended.Task.ContinueWith(_ => watch.Dispose(), TaskScheduler.FromCurrentSynchronizationContext());
        return ended.Task;
    }

    /// <summary>The navigation page is going back to the list itself, as on Alt+Left in a page: the shell shows the list.</summary>
    private void Followed()
    {
        if (_wanted is not null && !_navigating && ReferenceEquals(Host.CurrentPage, IndexPage))
            ShowIndex();
    }

    /// <summary>Leaves the page being opened unreported, and stops measuring it.</summary>
    private void Abandon()
    {
        _opening?.Dispose();
        _opening = null;
    }

    /// <summary>
    /// Asks for the frame after the one that first draws the page being opened, if the shell is in a window; a shell
    /// that isn't in one yet, as on a phone at start, asks once it is.
    /// </summary>
    private void AwaitFirstFrame()
    {
        if (_opening is not { } opening || _top is not { } top)
            return;

#if DEBUG
        // Only now, in a window, so that a shell never shown has no listener; layout and render can't come before.
        opening.Measure();
#endif
        // The callback runs before the next frame is drawn; the one it asks for runs after it.
        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ => Drawn(opening, top)));
    }

    /// <summary>
    /// The page's first frame has drawn. An animation-frame callback runs at the start of a render pass, before that
    /// pass's layout, so a view or safe area that changed since (as a phone's does while it starts) would be in the
    /// facts but not yet in the page's size. So the line waits for this pass to finish: a job just below the render
    /// pass's priority runs once its layout and frame are done. A change that comes before the job is laid out before
    /// it too, as Avalonia queues the layout it needs as a render pass, at the higher priority
    /// (<c>MediaContext.BeginInvokeOnRender</c>, 12.1.4).
    /// </summary>
    private void Drawn(Opening opening, TopLevel top)
    {
        if (IsCurrent(opening, top))
            Dispatcher.UIThread.Post(() => Settled(opening, top), DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The pass after the page's first frame is done: the page has opened, and the time is that frame's. The line waits
    /// for the turn, unless it is already over.
    /// </summary>
    private void Settled(Opening opening, TopLevel top)
    {
        if (!IsCurrent(opening, top))
            return;

        opening.Opened(Stopwatch.GetElapsedTime(opening.Started));
        if (opening.HasArrived)
            Ready(opening, top);
    }

    /// <summary>
    /// The turn into the page is over. Once the page's first frame has been timed, the line waits for the next frame,
    /// the page at rest, and the end of its pass, as the first frame's did.
    /// </summary>
    private void Arrived(Opening opening)
    {
        opening.HasArrived = true;
        if (opening.OpenedIn is null || _top is not { } top)
            return;

        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ =>
        {
            if (IsCurrent(opening, top))
                Dispatcher.UIThread.Post(() => Ready(opening, top), DispatcherPriority.Loaded);
        }));
    }

    /// <summary>The ready line, with the time to the page's first frame and the facts of the frame at rest.</summary>
    private void Ready(Opening opening, TopLevel top)
    {
        if (!IsCurrent(opening, top))
            return;

        _opening = null;
        try
        {
            Report(ReadyLine(opening.OpenedIn!.Value, DeviceFacts.Of(top, opening.Page), opening.Title));
#if DEBUG
            Report($"{CostMarker} {opening.Cost}");
            PerformanceLine.Text = opening.Cost;
            PerformanceLine.IsVisible = true;
#endif
        }
        finally
        {
            opening.Dispose();
        }
    }

    /// <summary>
    /// Whether <paramref name="opening"/> is still the page being opened, in <paramref name="top"/>: frame requests and
    /// posted jobs can't be cancelled, and a page left, or a shell that has left its window since, says nothing.
    /// </summary>
    private bool IsCurrent(Opening opening, TopLevel top) => ReferenceEquals(opening, _opening) && ReferenceEquals(top, _top);

    private void HidePerformance()
    {
        PerformanceLine.IsVisible = false;
        PerformanceLine.Text = null;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        // After every handler on the route, so that anything a page shows over itself goes back first.
        _backRequests = TopLevel.BackRequestedEvent.RouteFinished.Subscribe(new AnonymousObserver<RoutedEventArgs>(OnBackRequestFinished));
        AwaitFirstFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _backRequests?.Dispose();
        _backRequests = null;
        _top = null;
        // A page still opening when its shell leaves the window is never reported: its time would include the gap.
        Abandon();
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// What's left of a back request once everything else has seen it: from a page, the list. On the
    /// list it's left alone, so that a phone leaves the app.
    /// </summary>
    private void OnBackRequestFinished(RoutedEventArgs e)
    {
        // RouteFinished is for every top level's request, so only this one's.
        if (e.Handled || !IsShowingPage || _top is null || e.Source is not Visual source || WindowOf(source) != _top)
            return;

        e.Handled = true;
        ShowIndex();
    }

    /// <summary>The window <paramref name="source"/> is in: for a popup, the window it was opened from.</summary>
    private static TopLevel? WindowOf(Visual source)
    {
        var top = TopLevel.GetTopLevel(source);
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent && parent != top)
            top = parent;
        return top;
    }

    /// <summary>
    /// A page being opened: what it is, the page that holds it, what it's called, when its navigation began, how long it
    /// took to its first frame, whether the turn into it is over, and in Debug its cost.
    /// </summary>
    private sealed class Opening(Control page, ContentPage holder, string title, long started) : IDisposable
    {
        public Control Page { get; } = page;

        public ContentPage Holder { get; } = holder;

        public string Title { get; } = title;

        public long Started { get; } = started;

        /// <summary>From its navigation to its first drawn frame, once that is drawn.</summary>
        public TimeSpan? OpenedIn { get; private set; }

        /// <summary>Whether the navigation page has finished turning to it.</summary>
        public bool HasArrived { get; set; }

#if DEBUG
        /// <summary>Avalonia's layout and render times for this page; a process-wide listener, so disposed with it.</summary>
        private PerformanceMonitor? _performance;

        /// <summary>What the page cost to its first frame, read then.</summary>
        public string? Cost { get; private set; }

        public void Measure() => _performance ??= new PerformanceMonitor();

        /// <summary>The first frame is drawn: its time, and the cost to it, after which the monitor stops listening.</summary>
        public void Opened(TimeSpan opened)
        {
            OpenedIn ??= opened;
            if (_performance is not null)
            {
                Cost = _performance.Read(Title, Page, opened);
                _performance.Dispose();
                _performance = null;
            }
        }

        public void Dispose()
        {
            _performance?.Dispose();
            _performance = null;
        }
#else
        public void Opened(TimeSpan opened) => OpenedIn ??= opened;

        public void Dispose()
        {
        }
#endif
    }
}
