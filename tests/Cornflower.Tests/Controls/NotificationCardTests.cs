using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain NotificationCard (ADR 0007, #241): a banner as wide as its host on the flyout's surface, small type, the
/// type as the line under it, a fingertip close button that takes no focus, and no motion: closed is closed at once.
/// Simple's is a 350-wide card in its own colours that flies in and out, so these fail without the theme.
/// </summary>
public class NotificationCardTests
{
    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static NotificationCard Card(NotificationType type = NotificationType.Information) =>
        new() { Content = new Notification("Saved", "Your changes are in.", type) };

    private static TextBlock Text(NotificationCard card, string name) =>
        card.GetVisualDescendants().OfType<TextBlock>().First(text => text.Name == name);

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Card_Is_A_Banner_On_The_Flyouts_Surface(string variant)
    {
        var card = Card();
        using var scene = new Scene(new StackPanel { Children = { card } }, Variant(variant), width: 411);

        Assert.Same(Resource<IBrush>(card, "FlyoutBackgroundThemeBrush"), card.Background);
        Assert.Same(Resource<IBrush>(card, "FlyoutBackgroundThemeBrush"), Part<Border>(card, "PART_Border").Background);
        Assert.Same(Resource<IBrush>(card, "PhoneForegroundBrush"), card.Foreground);
        Assert.Equal(411, card.Bounds.Width, 0.5);
        Assert.Equal(default, Part<Border>(card, "PART_Border").CornerRadius);

        var title = Text(card, "PART_Title");
        var message = Text(card, "PART_Message");
        Assert.Equal("Saved", title.Text);
        Assert.Equal("Your changes are in.", message.Text);
        Assert.Equal(16, title.FontSize, 2);
        Assert.Equal(16, message.FontSize, 2);
        Assert.Equal(Resource<FontFamily>(card, "PhoneFontFamilySemiBold"), title.FontFamily);
        Assert.Equal(Resource<FontFamily>(card, "PhoneFontFamilyNormal"), message.FontFamily);
        Assert.Equal(TextWrapping.Wrap, message.TextWrapping);
        Assert.Equal(2, message.MaxLines);

        // The text 19 in from the banner's left, as a page's, and 9.5 below its top.
        var at = title.TranslatePoint(default, card)!.Value;
        Assert.Equal(19, at.X, 0.5);
        Assert.Equal(9.5, at.Y, 0.5);
    }

    [AvaloniaTheory]
    [InlineData(NotificationType.Information, null)]
    [InlineData(NotificationType.Success, "#FF60A917")]
    [InlineData(NotificationType.Warning, "#FFF0A30A")]
    [InlineData(NotificationType.Error, "#FFE51400")]
    public void Its_Type_Is_The_Line_Under_It(NotificationType type, string? colour)
    {
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var card = Card(type);
            var information = Card();
            using var scene = new Scene(new StackPanel { Children = { card, information } }, variant, width: 411);
            var border = Part<Border>(card, "PART_Border");

            Assert.Equal(new Thickness(0, 0, 0, 1.5), border.BorderThickness);
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(border.BorderBrush);
            Assert.Equal(colour is null ? Resource<ISolidColorBrush>(card, "FlyoutBackgroundThemeBrush").Color : Color.Parse(colour), brush.Color);
            Assert.Equal(information.Bounds.Height, card.Bounds.Height, 2);
        }
    }

    [AvaloniaFact]
    public void Its_Close_Button_Is_A_Fingertip_At_The_Right_And_Closes_It_At_Once()
    {
        var card = Card();
        using var scene = new Scene(new StackPanel { Children = { card } }, width: 411);
        var close = Part<Button>(card, "PART_CloseButton");
        var closed = 0;
        card.NotificationClosed += (_, _) => closed++;

        Assert.True(close.Bounds.Width >= 57.5 - 0.01 && close.Bounds.Height >= 57.5 - 0.01, $"the close button is {close.Bounds.Size}");
        Assert.Equal(411, close.TranslatePoint(new Point(close.Bounds.Width, 0), card)!.Value.X, 0.5);
        Assert.Equal(LucideIconKind.X, Assert.IsType<LucideIcon>(close.Content).Kind);

        Release(scene, Press(scene, close));

        Assert.True(card.IsClosing);
        Assert.True(card.IsClosed);
        Assert.Equal(1, closed);
    }

    // On Android a focused element's text client raises the keyboard (#254): the close button takes no focus, so a
    // press on it leaves the focus as it was, and asks for no keyboard.
    [AvaloniaFact]
    public void Its_Close_Button_Takes_No_Focus_And_No_Keyboard()
    {
        var card = Card();
        var box = new TextBox();
        using var scene = new Scene(new StackPanel { Children = { box, card } }, width: 411);
        var close = Part<Button>(card, "PART_CloseButton");

        Assert.False(close.Focusable);
        Assert.False(close.IsTabStop);
        Assert.False(InputMethod.GetIsInputMethodEnabled(close));

        Release(scene, Press(scene, close));

        Assert.False(close.IsFocused);
        Assert.Null(TopLevel.GetTopLevel(card)?.FocusManager?.GetFocusedElement());
    }

    // Simple's flies in (opacity, scale, a slide) and out, and sets IsClosed when its animation ends.
    [AvaloniaFact]
    public void It_Has_No_Motion()
    {
        var card = Card();
        using var scene = new Scene(new StackPanel { Children = { card } }, width: 411);

        Assert.Equal(1, card.Opacity);
        Assert.Null(card.RenderTransform);
        card.Close();
        Dispatcher.UIThread.RunJobs();
        Assert.True(card.IsClosed);
        Assert.Equal(1, card.Opacity);
    }

    // On its own, with no manager to take it out, a closed card is hidden: its close button does what it says.
    [AvaloniaFact]
    public void A_Closed_Card_On_Its_Own_Is_Hidden()
    {
        var card = Card();
        using var scene = new Scene(new StackPanel { Children = { card } }, width: 411);

        Release(scene, Press(scene, Part<Button>(card, "PART_CloseButton")));

        Assert.True(card.IsClosed);
        Assert.False(card.IsVisible);
    }

    [AvaloniaFact]
    public void Content_That_Isnt_A_Notification_Is_Shown_As_Its_Own()
    {
        var card = new NotificationCard { Content = "Copied." };
        using var scene = new Scene(new StackPanel { Children = { card } }, width: 411);

        var text = card.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Copied.");
        Assert.Equal(16, text.FontSize, 2);
        Assert.Equal(411, card.Bounds.Width, 0.5);

        // One line of text is shorter than a fingertip: the close button keeps the banner one tall.
        var close = Part<Button>(card, "PART_CloseButton");
        Assert.True(close.Bounds.Height >= 57.5 - 0.01, $"the close button is {close.Bounds.Height} tall");
    }
}
