using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain ItemsControl (ADR 0007, #234): themed by type in <c>Wp81Theme</c>, a bare ItemsPresenter with the
/// Padding as its margin, drawing nothing of its own. Simple's draws a Border round the presenter with the control's
/// Background and border, so these fail without the theme.
/// </summary>
public class ItemsControlTests
{
    private static ItemsPresenter PresenterOf(ItemsControl control) =>
        Assert.IsType<ItemsPresenter>(Assert.Single(control.GetVisualChildren()));

    // The template is the presenter alone, its margin the Padding, the items in it at their own size, in the panel.
    [AvaloniaFact]
    public void The_Template_Is_A_Bare_Presenter_Inside_The_Padding()
    {
        var control = new ItemsControl { ItemsSource = new[] { "first", "second" }, Padding = new Thickness(10, 5, 0, 0) };
        using var scene = new Scene(new StackPanel { Children = { control } });

        Assert.Null(control.Theme);
        var presenter = PresenterOf(control);
        Assert.Equal("PART_ItemsPresenter", presenter.Name);
        Assert.Equal(new Thickness(10, 5, 0, 0), presenter.Margin);
        var items = presenter.Panel!.Children;
        Assert.Equal(2, items.Count);
        Assert.Equal(new Point(10, 5), items[0].TranslatePoint(default, control));
        Assert.IsType<StackPanel>(presenter.Panel);
    }

    // An app's ItemsPanel lays the items out.
    [AvaloniaFact]
    public void An_Apps_ItemsPanel_Lays_Out_The_Items()
    {
        var control = new ItemsControl
        {
            ItemsSource = new[] { "first", "second" },
            ItemsPanel = new Avalonia.Controls.Templates.FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal }),
        };
        using var scene = new Scene(new StackPanel { Children = { control } });

        var items = PresenterOf(control).Panel!.Children;
        Assert.Equal(items[0].Bounds.Top, items[1].Bounds.Top);
        Assert.True(items[1].Bounds.Left >= items[0].Bounds.Right);
    }

    // No Background, BorderBrush or BorderThickness is drawn, dark or light: the padding shows the page.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Background_And_Border_Draw_Nothing(string variant)
    {
        var control = new ItemsControl
        {
            Width = 200,
            Height = 100,
            Padding = new Thickness(40),
            Background = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u)),
            BorderBrush = new SolidColorBrush(Color.FromUInt32(0xFF00FF00u)),
            BorderThickness = new Thickness(5),
        };
        var canvas = new Canvas { Children = { control } };
        Canvas.SetLeft(control, 100);
        Canvas.SetTop(control, 100);
        using var scene = new Scene(canvas, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        using var frame = scene.Capture();
        var page = Frames.PixelAt(frame, 50, 50);
        foreach (var (x, y) in new[] { (102.0, 102.0), (150.0, 120.0), (298.0, 198.0), (200.0, 150.0) })
            Assert.Equal(page, Frames.PixelAt(frame, (int)(x * Scene.ReferenceScaling), (int)(y * Scene.ReferenceScaling)));
    }
}
