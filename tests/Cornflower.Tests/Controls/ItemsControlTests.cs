using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain ItemsControl (ADR 0007, #234): themed by type in <c>Wp81Theme</c>, a bare ItemsPresenter in a Border bound
/// to the control's Background, BorderBrush, BorderThickness, CornerRadius and Padding, all unset, so nothing is drawn
/// at rest. Simple's sets the presenter's name and the Border too, so the tests that fail without the theme are the
/// ones on Wp81's own: the presenter's place, and that a <c>Wp81Theme</c> scene finds a theme of Wp81's for the type.
/// </summary>
public class ItemsControlTests
{
    private static Border BorderOf(ItemsControl control) =>
        Assert.IsType<Border>(Assert.Single(control.GetVisualChildren()));

    private static ItemsPresenter PresenterOf(ItemsControl control) =>
        Assert.IsType<ItemsPresenter>(BorderOf(control).Child);

    // The template: the Border, its padding the control's, round the presenter, the items in it at their own size, in
    // the panel. The theme by type is Wp81's, dark and light (Holes.txt no longer lists ItemsControl).
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Template_Is_A_Presenter_Inside_The_Padding(string variant)
    {
        var control = new ItemsControl { ItemsSource = new[] { "first", "second" }, Padding = new Thickness(10, 5, 0, 0) };
        using var scene = new Scene(new StackPanel { Children = { control } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Null(control.Theme);
        var theme = Assert.IsType<ControlTheme>(scene.Window.FindResource(scene.Window.ActualThemeVariant, typeof(ItemsControl)));
        var wp81 = Assert.IsType<Cornflower.Themes.Wp81.Wp81Theme>(scene.Window.Styles[1]);
        Assert.True(wp81.TryGetResource(typeof(ItemsControl), scene.Window.ActualThemeVariant, out var own));
        Assert.Same(own, theme);
        var border = BorderOf(control);
        Assert.Equal("PART_Border", border.Name);
        Assert.Equal(new Thickness(10, 5, 0, 0), border.Padding);
        Assert.Null(border.Background);
        Assert.Equal(default, border.BorderThickness);
        var presenter = PresenterOf(control);
        Assert.Equal("PART_ItemsPresenter", presenter.Name);
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
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal }),
        };
        using var scene = new Scene(new StackPanel { Children = { control } });

        var items = PresenterOf(control).Panel!.Children;
        Assert.Equal(items[0].Bounds.Top, items[1].Bounds.Top);
        Assert.True(items[1].Bounds.Left >= items[0].Bounds.Right);
    }

    // Nothing is drawn at rest, dark or light: the padding shows the page. An app's background and border are drawn, and
    // a transparent background takes the taps between items, as Avalonia's own.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Nothing_Is_Drawn_At_Rest_And_An_Apps_Background_And_Border_Are(string variant)
    {
        var control = new ItemsControl { Width = 200, Height = 100, Padding = new Thickness(40) };
        var canvas = new Canvas { Children = { control } };
        Canvas.SetLeft(control, 100);
        Canvas.SetTop(control, 100);
        using var scene = new Scene(canvas, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        static PixelPoint At(double x, double y) => new((int)(x * Scene.ReferenceScaling), (int)(y * Scene.ReferenceScaling));

        Color page, inside;
        using (var frame = scene.Capture())
        {
            page = Frames.PixelAt(frame, 50, 50);
            inside = Frames.PixelAt(frame, At(150, 120).X, At(150, 120).Y);
        }
        Assert.Equal(page, inside);
        var missed = scene.Window.InputHitTest(new Point(150, 120));
        Assert.True(missed is null || !ReferenceEquals(missed, control) && !control.IsVisualAncestorOf((Visual)missed), $"{missed} took the tap at rest");

        var red = Color.FromUInt32(0xFFFF0000u);
        var green = Color.FromUInt32(0xFF00FF00u);
        control.Background = new SolidColorBrush(red);
        control.BorderBrush = new SolidColorBrush(green);
        control.BorderThickness = new Thickness(5);
        using (var frame = scene.Capture())
        {
            Assert.Equal(red, Frames.PixelAt(frame, At(150, 120).X, At(150, 120).Y));
            Assert.Equal(green, Frames.PixelAt(frame, At(102, 150).X, At(102, 150).Y));
        }

        control.Background = Brushes.Transparent;
        var hit = scene.Window.InputHitTest(new Point(150, 120));
        Assert.True(ReferenceEquals(hit, control) || hit is Visual visual && control.IsVisualAncestorOf(visual), $"{hit} took the tap");
    }
}
