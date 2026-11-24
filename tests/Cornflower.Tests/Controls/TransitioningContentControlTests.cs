using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain TransitioningContentControl (ADR 0007, #236): two presenters and no transition of its own (ADR 0007's
/// answer 6), where Avalonia's default is a 125 ms cross-fade, so new content shows at once; an app's transition wins.
/// </summary>
public class TransitioningContentControlTests
{
    private static ContentPresenter[] Presenters(TransitioningContentControl control) =>
        [Part<ContentPresenter>(control, "PART_ContentPresenter"), Part<ContentPresenter>(control, "PART_ContentPresenter2")];

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void New_Content_Shows_At_Once(string variant)
    {
        var control = new TransitioningContentControl { Content = "first" };
        using var scene = new Scene(new StackPanel { Children = { control } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Null(control.PageTransition);
        control.Content = "second";
        Dispatcher.UIThread.RunJobs();

        var shown = Assert.Single(Presenters(control), presenter => presenter.IsVisible);
        Assert.Equal("second", shown.Content);
        Assert.All(Presenters(control), presenter => Assert.True(PhoneHeaderLine.GetIsEnabled(presenter)));
    }

    // An app's transition is its own: set, it runs, so both contents show while it does.
    [AvaloniaFact]
    public void An_Apps_Transition_Wins()
    {
        var transition = new SlideTransition();
        var control = new TransitioningContentControl { Content = "first", PageTransition = transition };
        using var scene = new Scene(new StackPanel { Children = { control } });

        control.Content = "second";
        Dispatcher.UIThread.RunJobs();

        Assert.Same(transition, control.PageTransition);
        Assert.Equal(2, Presenters(control).Count(presenter => presenter.IsVisible));
    }

    // What an app sets reaches both presenters.
    [AvaloniaFact]
    public void An_Apps_Values_Reach_Both_Presenters()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var template = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "mine" });
        var control = new TransitioningContentControl
        {
            Content = "first", Background = brush, BorderBrush = brush, BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(3), Padding = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Right, VerticalContentAlignment = VerticalAlignment.Bottom,
            ContentTemplate = template,
        };
        using var scene = new Scene(new StackPanel { Children = { control } });

        Assert.All(Presenters(control), presenter =>
        {
            Assert.Same(brush, presenter.Background);
            Assert.Same(brush, presenter.BorderBrush);
            Assert.Equal(new Thickness(2), presenter.BorderThickness);
            Assert.Equal(new CornerRadius(3), presenter.CornerRadius);
            Assert.Equal(new Thickness(4), presenter.Padding);
            Assert.Equal(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
            Assert.Same(template, presenter.ContentTemplate);
        });
    }
}
