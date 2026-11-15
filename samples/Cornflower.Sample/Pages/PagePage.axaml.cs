using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
    /// <summary>
    /// The transition each page came with, to go back with: none for the theme's turnstile. Kept by page, not as a stack
    /// beside Avalonia's, as NavigationPage also goes back on its own (Alt+Left on desktop).
    /// </summary>
    private readonly Dictionary<Page, IPageTransition> _cameWith = new();

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
        if (transition is not null)
            _cameWith[page] = transition;
        await (transition is null ? Navigation.PushAsync(page) : Navigation.PushAsync(page, transition));
        if (!ReferenceEquals(Navigation.CurrentPage, page))
            _cameWith.Remove(page);
    }

    /// <summary>Goes back a page, with the transition it came with; false on the first page or mid-transition.</summary>
    internal bool GoBack()
    {
        if (Navigation.StackDepth <= 1 || Navigation.IsNavigating || Navigation.CurrentPage is not { } current)
            return false;
        _ = _cameWith.Remove(current, out var transition) ? Navigation.PopAsync(transition) : Navigation.PopAsync();
        return true;
    }

    private ContentPage MakePage(int number)
    {
        var buttons = new StackPanel { Spacing = 9.6 };
        buttons.Children.Add(Forward("turnstile", null));
        buttons.Children.Add(Forward("slide", new SlideTransition()));
        buttons.Children.Add(Forward("continuum", new ContinuumTransition()));
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
        };
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

    /// <summary>Back a page while there is one, even mid-transition (the press is the page's, not the shell's).</summary>
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled || Navigation.StackDepth <= 1)
            return;
        e.Handled = true;
        GoBack();
    }
}
