using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain WindowNotificationManager (ADR 0007, #241): banners across the whole width at the top, divided by the 1.5
/// line, stacked up from the bottom for a bottom Position; inside the window's safe area in the adorner layer; a closed card leaves
/// at once; showing one never scrolls the page. Simple's stacks 350-wide cards in a corner, 8 in from the edge, and
/// removes a card only when its closing animation ends, so these fail without the theme.
/// </summary>
public class WindowNotificationManagerTests
{
    private static IReadOnlyList<NotificationCard> Cards(WindowNotificationManager manager) =>
        Part<ReversibleStackPanel>(manager, "PART_Items").Children.OfType<NotificationCard>().ToList();

    private static double Top(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value.Y;

    [AvaloniaFact]
    public void Banners_Stack_Across_The_Top_Divided_By_The_Line()
    {
        var manager = new WindowNotificationManager();
        using var scene = new Scene(new Panel { Children = { manager } });

        manager.Show(new Notification("One", "The first."));
        manager.Show(new Notification("Two", "The second.", NotificationType.Error));
        Dispatcher.UIThread.RunJobs();

        var cards = Cards(manager);
        Assert.Equal(2, cards.Count);
        Assert.Equal(0, Top(cards[0], scene.Window), 0.5);
        Assert.Equal(cards[0].Bounds.Height + 1.5, Top(cards[1], scene.Window), 0.5);
        Assert.All(cards, card => Assert.Equal(450, card.Bounds.Width, 0.5));
        Assert.All(cards, card => Assert.Equal(0, card.TranslatePoint(default, scene.Window)!.Value.X, 0.5));
    }

    [AvaloniaFact]
    public void A_Bottom_Position_Stacks_Them_Up_From_The_Bottom()
    {
        var manager = new WindowNotificationManager { Position = NotificationPosition.BottomRight };
        using var scene = new Scene(new Panel { Children = { manager } });

        manager.Show(new Notification("One", "The first."));
        manager.Show(new Notification("Two", "The second."));
        Dispatcher.UIThread.RunJobs();

        var cards = Cards(manager);
        Assert.Equal(800, Top(cards[0], scene.Window) + cards[0].Bounds.Height, 0.5);
        Assert.Equal(Top(cards[0], scene.Window) - 1.5, Top(cards[1], scene.Window) + cards[1].Bounds.Height, 0.5);
        Assert.All(cards, card => Assert.Equal(450, card.Bounds.Width, 0.5));
    }

    [AvaloniaFact]
    public void A_Closed_Card_Leaves_At_Once_And_A_Tap_Runs_Its_OnClick()
    {
        var manager = new WindowNotificationManager();
        using var scene = new Scene(new Panel { Children = { manager } });
        int clicked = 0, closed = 0;

        manager.Show(new Notification("One", "Tap me.", onClick: () => clicked++, onClose: () => closed++));
        manager.Show(new Notification("Two", "Close me.", onClick: () => clicked++, onClose: () => closed++));
        Dispatcher.UIThread.RunJobs();
        var cards = Cards(manager);

        // The close button takes its press: the card closes, and its OnClick doesn't run.
        Release(scene, Press(scene, Part<Button>(cards[1], "PART_CloseButton")));
        Assert.Equal((0, 1), (clicked, closed));
        Assert.Equal([cards[0]], Cards(manager));

        // A tap on the banner runs it, and closes the card.
        Release(scene, Press(scene, Text(cards[0])));
        Assert.Equal((1, 2), (clicked, closed));
        Assert.Empty(Cards(manager));
    }

    private static TextBlock Text(NotificationCard card) => card.GetVisualDescendants().OfType<TextBlock>().First();

    [AvaloniaFact]
    public void In_The_Windows_Adorner_Layer_It_Keeps_Inside_The_Safe_Area()
    {
        var insets = new Insets(new Thickness(0, 24, 0, 48));
        var was = PlainNotificationArea.SafeAreaOf;
        PlainNotificationArea.SafeAreaOf = _ => insets;
        try
        {
            using var scene = new Scene(new Panel());
            var manager = new WindowNotificationManager(scene.Window);
            Dispatcher.UIThread.RunJobs();
            manager.Show(new Notification("One", "Under the status bar."));
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<AdornerLayer>(manager.Parent);
            Assert.Equal(new Thickness(0, 24, 0, 48), manager.Padding);
            Assert.Equal(24, Top(Cards(manager)[0], scene.Window), 0.5);

            insets.Change(new Thickness(0, 30, 0, 0));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(30, Top(Cards(manager)[0], scene.Window), 0.5);

            // An app's own padding wins.
            manager.Padding = new Thickness(0, 4, 0, 0);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(4, Top(Cards(manager)[0], scene.Window), 0.5);
            insets.Change(new Thickness(0, 36, 0, 0));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(4, Top(Cards(manager)[0], scene.Window), 0.5);

            // Out of the window, it lets the safe area go.
            manager.ClearValue(TemplatedControl.PaddingProperty);
            Assert.Equal(new Thickness(0, 36, 0, 0), manager.Padding);
            ((AdornerLayer)manager.Parent!).Children.Remove(manager);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(default, manager.Padding);
        }
        finally
        {
            PlainNotificationArea.SafeAreaOf = was;
        }
    }

    // A manager an app places in its own layout is inside its view's padding already.
    [AvaloniaFact]
    public void In_An_Apps_Layout_It_Takes_No_Safe_Area()
    {
        var was = PlainNotificationArea.SafeAreaOf;
        PlainNotificationArea.SafeAreaOf = _ => new Insets(new Thickness(0, 24, 0, 0));
        try
        {
            var manager = new WindowNotificationManager();
            using var scene = new Scene(new Panel { Children = { manager } });

            Assert.Equal(default, manager.Padding);
        }
        finally
        {
            PlainNotificationArea.SafeAreaOf = was;
        }
    }

    // A notification at load or from code never moves the page (#220, #251, #252).
    [AvaloniaFact]
    public void Showing_One_Doesnt_Scroll_The_Page()
    {
        var page = new ScrollViewer
        {
            Height = 200,
            VerticalAlignment = VerticalAlignment.Top,
            Content = new StackPanel { Children = { new Border { Height = 600 }, new TextBox() } },
        };
        using var scene = new Scene(new Panel { Children = { page } });
        var manager = new WindowNotificationManager(scene.Window);
        Dispatcher.UIThread.RunJobs();

        manager.Show(new Notification("One", "Nothing moves."));
        Dispatcher.UIThread.RunJobs();
        Release(scene, Press(scene, Part<Button>(Cards(manager)[0], "PART_CloseButton")));

        Assert.Equal(0, page.Offset.Y);
    }

    /// <summary>A window's insets, as a phone's: a safe area that changes.</summary>
    private sealed class Insets(Thickness safeArea) : PlainNotificationArea.ISafeArea
    {
        public Thickness Padding { get; private set; } = safeArea;

        public event EventHandler<SafeAreaChangedArgs>? Changed;

        public void Change(Thickness safeArea)
        {
            Padding = safeArea;
            Changed?.Invoke(this, new SafeAreaChangedArgs(safeArea));
        }
    }
}
