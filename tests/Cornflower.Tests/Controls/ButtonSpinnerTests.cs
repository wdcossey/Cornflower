using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain ButtonSpinner (ADR 0007, #235): a 2.5 box with its content and two spin buttons side by side, each in the
/// spin button part theme (Wp81PlainSpinnerButtonTheme). Simple's stacks two small arrows in its own brushes, so these
/// fail without the theme.
/// </summary>
public class ButtonSpinnerTests
{
    private static ButtonSpinner Specimen() => new() { Content = "3 copies", Width = 300 };

    private static RepeatButton Decrease(ButtonSpinner spinner) => Part<RepeatButton>(spinner, "PART_DecreaseButton");

    private static RepeatButton Increase(ButtonSpinner spinner) => Part<RepeatButton>(spinner, "PART_IncreaseButton");

    private static double X(Visual part, Visual host) => part.TranslatePoint(default, host)!.Value.X;

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_A_Box_With_Two_Spin_Buttons_After_Its_Content(string variant)
    {
        var spinner = Specimen();
        using var scene = new Scene(new StackPanel { Children = { spinner } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var foreground = Resource<IBrush>(spinner, "PhoneForegroundBrush");
        Assert.Same(foreground, spinner.BorderBrush);
        Assert.Same(foreground, spinner.Foreground);
        Assert.Equal(Brushes.Transparent, spinner.Background);
        Assert.Equal(new Thickness(2.5), spinner.BorderThickness);
        Assert.Equal(Resource<FontFamily>(spinner, "PhoneFontFamilyNormal"), spinner.FontFamily);
        Assert.Equal(Resource<double>(spinner, "ContentControlFontSize"), spinner.FontSize);
        Assert.Equal(Resource<Thickness>(spinner, "TextControlThemePadding"), spinner.Padding);
        Assert.Equal(VerticalAlignment.Center, spinner.VerticalContentAlignment);
        Assert.Null(spinner.FocusAdorner);
        Assert.True(spinner.Focusable);
        Assert.False(Tilt.GetIsEnabled(spinner));
        Assert.Equal(Resource<double>(spinner, "TextControlThemeMinHeight"), spinner.MinHeight);
        Assert.Equal(34, spinner.Bounds.Height, 0.5);

        var theme = Resource<ControlTheme>(spinner, "Wp81PlainSpinnerButtonTheme");
        var content = Part<ContentPresenter>(spinner, "PART_ContentPresenter");
        foreach (var (button, kind) in new[] { (Decrease(spinner), LucideIconKind.Minus), (Increase(spinner), LucideIconKind.Plus) })
        {
            Assert.Same(theme, button.Theme);
            Assert.Equal(kind, Assert.IsType<LucideIcon>(button.Content).Kind);
            Assert.Equal(40, button.Bounds.Width, 0.5);
            Assert.Equal(34 - 5, button.Bounds.Height, 0.5);
        }
        Assert.True(X(content, spinner) < X(Decrease(spinner), spinner));
        Assert.True(X(Decrease(spinner), spinner) < X(Increase(spinner), spinner));
        Assert.Equal(300 - 2.5, X(Increase(spinner), spinner) + 40, 0.5);
    }

    [AvaloniaFact]
    public void On_The_Left_The_Buttons_Come_First()
    {
        var spinner = Specimen();
        spinner.ButtonSpinnerLocation = Location.Left;
        using var scene = new Scene(new StackPanel { Children = { spinner } });

        Assert.Equal(2.5, X(Decrease(spinner), spinner), 0.5);
        Assert.True(X(Decrease(spinner), spinner) < X(Increase(spinner), spinner));
        Assert.True(X(Increase(spinner), spinner) < X(Part<ContentPresenter>(spinner, "PART_ContentPresenter"), spinner));
    }

    [AvaloniaFact]
    public void ShowButtonSpinner_Off_Hides_The_Buttons()
    {
        var spinner = Specimen();
        spinner.ShowButtonSpinner = false;
        using var scene = new Scene(new StackPanel { Children = { spinner } });

        Assert.False(Decrease(spinner).IsVisible);
        Assert.False(Increase(spinner).IsVisible);
    }

    // The spin button part theme: nothing drawn at rest, the phone's RepeatButton's repeat, not focusable, PhonePress on.
    [AvaloniaFact]
    public void Its_Buttons_Repeat_As_The_Phones_RepeatButton()
    {
        var spinner = Specimen();
        using var scene = new Scene(new StackPanel { Children = { spinner } });

        var button = Increase(spinner);
        Assert.Equal(ClickMode.Press, button.ClickMode);
        Assert.Equal(500, button.Delay);
        Assert.Equal(33, button.Interval);
        Assert.False(button.Focusable);
        Assert.Null(button.FocusAdorner);
        Assert.True(PhonePress.GetIsEnabled(button));
        Assert.Equal(Brushes.Transparent, button.Background);
        Assert.Equal(Brushes.Transparent, Part<Border>(button, "Border").Background);
        Assert.Equal(default, button.BorderThickness);
        Assert.Equal(default, button.Padding);
        Assert.Same(spinner.Foreground, button.Foreground);
        Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
    }

    // A press spins at once (ClickMode Press) and fills the button with the accent, its glyph over it.
    [AvaloniaTheory]
    [InlineData("PART_IncreaseButton", SpinDirection.Increase)]
    [InlineData("PART_DecreaseButton", SpinDirection.Decrease)]
    public void A_Press_Spins_And_Is_The_Accent(string name, SpinDirection direction)
    {
        var spinner = Specimen();
        var spins = new List<SpinDirection>();
        spinner.Spin += (_, e) => spins.Add(e.Direction);
        using var scene = new Scene(new StackPanel { Children = { spinner } });
        var button = Part<RepeatButton>(spinner, name);

        var at = Press(scene, button);
        Assert.Equal([direction], spins);
        Assert.Same(Resource<IBrush>(spinner, "PhoneAccentBrush"), Part<Border>(button, "Border").Background);
        Assert.Same(Resource<IBrush>(spinner, "PhoneButtonBasePressedForegroundBrush"), Part<ContentPresenter>(button, "PART_ContentPresenter").Foreground);
        Release(scene, at);
        Assert.Equal(Brushes.Transparent, Part<Border>(button, "Border").Background);
    }

    [AvaloniaFact]
    public void Disabled_The_Box_And_Glyphs_Are_PhoneDisabledBrush()
    {
        var spinner = Specimen();
        spinner.IsEnabled = false;
        using var scene = new Scene(new StackPanel { Children = { spinner } });

        var dim = Resource<IBrush>(spinner, "PhoneDisabledBrush");
        Assert.Same(dim, Part<Border>(spinner, "Border").BorderBrush);
        Assert.Same(dim, Part<ContentPresenter>(Increase(spinner), "PART_ContentPresenter").Foreground);
        Assert.Same(dim, Part<ContentPresenter>(spinner, "PART_ContentPresenter").Foreground);
    }

    // Focused, it spins on the arrow keys, Avalonia's Spinner's, as Simple's does: the theme makes it focusable.
    [AvaloniaFact]
    public void Focused_It_Spins_On_The_Arrow_Keys()
    {
        var spinner = Specimen();
        var spins = new List<SpinDirection>();
        spinner.Spin += (_, e) => spins.Add(e.Direction);
        using var scene = new Scene(new StackPanel { Children = { spinner } });

        Assert.True(spinner.Focus());
        scene.Window.KeyPressQwerty(Avalonia.Input.PhysicalKey.ArrowUp, Avalonia.Input.RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([SpinDirection.Increase], spins);
    }
}
