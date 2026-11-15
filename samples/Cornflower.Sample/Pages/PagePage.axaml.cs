using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 page and Frame: a <see cref="NavigationPage"/> of <see cref="ContentPage"/>s, each going forward with the
/// turnstile, the slide or the continuum, and back with the transition it came with. The platform's back request goes
/// back a page while there is one, before the shell's Back takes it: the hardware back button is the app's.
/// </summary>
[SamplePage("page", Order = 120)]
public partial class PagePage : UserControl
{
    // The transitions hold no state: one of each does for every page.
    private static readonly IPageTransition Slide = new SlideTransition();
    private static readonly IPageTransition Continuum = new ContinuumTransition();

    private TopLevel? _top;

    public PagePage()
    {
        InitializeComponent();
        _ = Navigation.PushAsync(MakePage(1));
    }

    /// <summary>The pages on the stack: the first, and every one gone forward to.</summary>
    internal int Depth => Navigation.StackDepth;

    /// <summary>Goes forward to a new page with <paramref name="transition"/>, or the theme's turnstile when it is null.</summary>
    internal async Task GoForward(IPageTransition? transition)
    {
        // NavigationPage drops a navigation asked for while one runs: don't make a page for nothing.
        if (Navigation.IsNavigating)
            return;
        var page = MakePage(Navigation.StackDepth + 1);
        await (transition is null ? Navigation.PushAsync(page) : Navigation.PushAsync(page, transition));
    }

    /// <summary>
    /// Goes back a page; false on the first page or mid-transition. The theme's turnstile, going back, plays the
    /// transition the page came with, as the phone's Frame.GoBack does, so the page names none.
    /// </summary>
    internal bool GoBack()
    {
        if (Navigation.StackDepth <= 1 || Navigation.IsNavigating)
            return false;
        _ = Navigation.PopAsync();
        return true;
    }

    private ContentPage MakePage(int number)
    {
        var buttons = new StackPanel { Spacing = 9.6 };
        buttons.Children.Add(Forward("turnstile", null));
        buttons.Children.Add(Forward("slide", Slide));
        buttons.Children.Add(Forward("continuum", Continuum));
        if (number > 1)
        {
            var back = new Button { Content = "back", HorizontalAlignment = HorizontalAlignment.Stretch };
            back.Click += (_, _) => GoBack();
            buttons.Children.Add(back);
        }

        var heading = new TextBlock { Text = $"page {number}" };
        heading.Bind(ThemeProperty, heading.GetResourceObservable("SubheaderTextBlockStyle"));
        var note = new TextBlock
        {
            Text = number == 1 ? "go forward to a new page" : $"{number - 1} back to the first",
            Margin = new(0, 6, 0, 12),
        };
        note.Bind(ThemeProperty, note.GetResourceObservable("ControlContextualInfoTextBlockStyle"));

        return new ContentPage
        {
            Content = new StackPanel
            {
                Margin = new(19, 12, 19, 24),
                Children = { new SamplePageHeader(), heading, note, buttons },
            },
            BottomCommandBar = AppBar(number),
        };
    }

    /// <summary>
    /// The page's app bar: compact on odd pages, minimal on even ones, so the content gives up 62.92 or 26.25 at its
    /// foot, and keeps it free while the bar is open. Its commands go forward too.
    /// </summary>
    private CommandBar AppBar(int number)
    {
        var bar = new CommandBar();
        CommandBarOptions.SetClosedDisplayMode(bar, number % 2 == 0 ? AppBarClosedDisplayMode.Minimal : AppBarClosedDisplayMode.Compact);
        var add = new CommandBarButton { Label = "new page", Icon = new LucideIcon { Kind = LucideIconKind.Plus } };
        add.Click += async (_, _) => await GoForward(null);
        bar.PrimaryCommands.Add(add);
        var slide = new CommandBarButton { Label = "slide to a new page" };
        slide.Click += async (_, _) => await GoForward(Slide);
        bar.SecondaryCommands.Add(slide);
        var continuum = new CommandBarButton { Label = "continuum to a new page" };
        continuum.Click += async (_, _) => await GoForward(Continuum);
        bar.SecondaryCommands.Add(continuum);
        return bar;
    }

    private Button Forward(string name, IPageTransition? transition)
    {
        var button = new Button { Content = name, HorizontalAlignment = HorizontalAlignment.Stretch };
        button.Click += async (_, _) => await GoForward(transition);
        return button;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        if (_top is not null)
            _top.BackRequested += OnBackRequested;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_top is not null)
            _top.BackRequested -= OnBackRequested;
        _top = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Back a page while there is one. A press mid-transition is taken and dropped, as NavigationPage drops a navigation
    /// asked for while one runs: it is the page's, not the shell's, so it doesn't leave the page.
    /// </summary>
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled || Navigation.StackDepth <= 1)
            return;
        e.Handled = true;
        GoBack();
    }
}
