using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain NumericUpDown (ADR 0007, #235): Wp81's TextBox, with the number keyboard, inside Wp81's ButtonSpinner with
/// its box cleared, the spin buttons beside the TextBox's box. Simple's puts Simple's TextBox in its own spinner (and,
/// before this theme, HoleParts kept it Simple's), so these fail without the theme.
/// </summary>
public class NumericUpDownTests
{
    private static NumericUpDown Specimen() => new() { Value = 3, Minimum = 0, Maximum = 10, Width = 300 };

    private static TextBox Box(NumericUpDown control) => Part<TextBox>(control, "PART_TextBox");

    private static ButtonSpinner Spinner(NumericUpDown control) => Part<ButtonSpinner>(control, "PART_Spinner");

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_The_Phones_TextBox_With_Spin_Buttons_Beside_It(string variant)
    {
        var control = Specimen();
        using var scene = new Scene(new StackPanel { Children = { control } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Equal(Resource<Thickness>(control, "TextControlMarginThemeThickness"), control.Margin);
        var box = Box(control);
        Assert.Null(box.Theme);                                  // Wp81's TextBox, by type
        Assert.Contains(box.GetVisualDescendants(), v => v is Control { Name: "BorderElement" });
        Assert.Equal(default, box.Margin);
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(box));
        Assert.False(box.AcceptsReturn);
        Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        Assert.Equal("3", box.Text);

        var spinner = Spinner(control);
        Assert.Equal(default, spinner.BorderThickness);
        Assert.Equal(default, spinner.Padding);
        Assert.Equal(Brushes.Transparent, spinner.Background);
        var increase = Part<RepeatButton>(spinner, "PART_IncreaseButton");
        var drawn = Part<Border>(box, "BorderElement");
        Assert.True(Math.Abs(drawn.Bounds.Height - increase.Bounds.Height) < 0.5, $"box {drawn.Bounds.Height}, button {increase.Bounds.Height}");
        Assert.True(box.TranslatePoint(new Point(box.Bounds.Width, 0), control)!.Value.X <= increase.TranslatePoint(default, control)!.Value.X);
    }

    [AvaloniaFact]
    public void A_Spin_Button_Changes_The_Value_And_The_Text()
    {
        var control = Specimen();
        using var scene = new Scene(new StackPanel { Children = { control } });

        var at = Press(scene, Part<RepeatButton>(Spinner(control), "PART_IncreaseButton"));
        Release(scene, at);

        Assert.Equal(4m, control.Value);
        Assert.Equal(control.Text, Box(control).Text);       // "4.0": Avalonia formats the sum with the Increment's scale
        Assert.Equal(4m, decimal.Parse(Box(control).Text!, System.Globalization.CultureInfo.CurrentCulture));
    }

    // Typed into the box, the text is the control's, and so the value.
    [AvaloniaFact]
    public void Text_Typed_Into_The_Box_Sets_The_Value()
    {
        var control = Specimen();
        using var scene = new Scene(new StackPanel { Children = { control } });

        Box(control).Text = "7";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("7", control.Text);
        Assert.Equal(7m, control.Value);
    }

    // The spinner's and the box's values are the control's.
    [AvaloniaFact]
    public void The_Controls_Values_Reach_The_Spinner_And_The_Box()
    {
        var control = Specimen();
        control.AllowSpin = false;
        control.ShowButtonSpinner = false;
        control.ButtonSpinnerLocation = Location.Left;
        control.PlaceholderText = "copies";
        control.IsReadOnly = true;
        control.TextAlignment = TextAlignment.Right;
        using var scene = new Scene(new StackPanel { Children = { control } });

        var spinner = Spinner(control);
        Assert.False(spinner.AllowSpin);
        Assert.False(spinner.ShowButtonSpinner);
        Assert.Equal(Location.Left, spinner.ButtonSpinnerLocation);
        var box = Box(control);
        Assert.Equal("copies", box.PlaceholderText);
        Assert.True(box.IsReadOnly);
        Assert.Equal(TextAlignment.Right, box.TextAlignment);
    }

    // Disabled, the box is Wp81's disabled TextBox and the spin buttons' glyphs PhoneDisabledBrush.
    [AvaloniaFact]
    public void Disabled_The_Box_And_Buttons_Are_Dimmed()
    {
        var control = Specimen();
        control.IsEnabled = false;
        using var scene = new Scene(new StackPanel { Children = { control } });
        Dispatcher.UIThread.RunJobs();

        Assert.Same(Resource<IBrush>(control, "TextBoxDisabledBorderThemeBrush"), Part<Border>(Box(control), "BorderElement").BorderBrush);
        var increase = Part<RepeatButton>(Spinner(control), "PART_IncreaseButton");
        Assert.Same(Resource<IBrush>(control, "PhoneDisabledBrush"), Part<ContentPresenter>(increase, "PART_ContentPresenter").Foreground);
    }
}
