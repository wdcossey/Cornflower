using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.MenuFlyoutReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's MenuFlyout: Wp81's MenuFlyoutPresenter theme and its item themes (<c>Controls/MenuFlyoutPresenter.axaml</c>)
/// and the popup round it (<see cref="PhoneMenuFlyout"/>, on <see cref="PhonePopupHost"/>), against the emulator's
/// screenshots and #150's readings (<see cref="MenuFlyoutReadings"/>). docs/controls/MenuFlyout/PLAN.md has each reading.
/// </summary>
public class MenuFlyoutTests
{
    /// <summary>The reference app's stage: a page with the "open" Button, the menu shown on it.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(double targetTop = 312.08, int items = 4, ThemeVariant? variant = null, Control? root = null,
            IEnumerable<IStyle>? themes = null, IEnumerable<object>? menu = null, IBrush? page = null)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Flyout = new MenuFlyout();
            foreach (var item in menu ?? Enumerable.Range(1, items).Select(i => (object)new MenuItem { Header = $"item {i}" }))
                Flyout.Items.Add(item);
            Button = new Button
            {
                Content = "open",
                Margin = new Thickness(19, targetTop, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            Page = new Panel { Background = page ?? Brushes.Transparent, Children = { Button } };
            try
            {
                Scene = new Scene(root ?? Page, variant, themes: themes);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public MenuFlyout Flyout { get; }

        public Button Button { get; }

        public Panel Page { get; }

        public Scene Scene { get; }

        public MenuFlyoutPresenter Presenter => (MenuFlyoutPresenter)Flyout.Popup.Child!;

        public MenuItem Item(int index) => (MenuItem)Flyout.Items[index]!;

        public void Open()
        {
            Flyout.ShowAt(Button);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Opens the menu and lets its motion end.</summary>
        public void OpenAtRest()
        {
            Open();
            At(Clock.Now.TotalMilliseconds + 600);
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>The menu's laid-out bounds in the window, without the motion's transforms.</summary>
        public Rect Bounds()
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var host = Presenter.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
            return new Rect(Canvas.GetLeft(host), Canvas.GetTop(host), Presenter.Bounds.Width, Presenter.Bounds.Height);
        }

        public T Part<T>(string name) where T : Control =>
            Presenter.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

        public void Tap(Point at)
        {
            Scene.Window.MouseDown(at, MouseButton.Left);
            Scene.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        public Point CentreOf(Visual visual) =>
            visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), Scene.Window)!.Value;

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    /// <summary>Within <paramref name="raw"/> raw pixels.</summary>
    private static void Near(double expected, double actual, int raw = 1) =>
        Assert.InRange(actual, expected - raw / Scale - 0.001, expected + raw / Scale + 0.001);

    [AvaloniaFact]
    public void The_Presenter_And_Its_Items_Are_Templated_By_The_Theme()
    {
        using var stage = new Stage(menu: [new MenuItem { Header = "one" }, new Separator(), new MenuItem { Header = "two" }]);
        stage.OpenAtRest();

        var theme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(typeof(MenuFlyoutPresenter)));
        var template = theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, stage.Presenter.Template);
        Assert.True(PhoneMenuFlyout.GetIsEnabled(stage.Presenter));
        Assert.NotNull(stage.Part<Border>("OuterBorder"));
        Assert.NotNull(stage.Part<Border>("CenterBorder"));
        Assert.True(stage.Part<StackPanel>("InnerBorder").ClipToBounds);
        Assert.DoesNotContain(stage.Presenter.GetVisualDescendants(), d => d is ScrollViewer);

        Assert.Same(stage.Scene.Window.FindResource("Wp81MenuFlyoutItemTheme"), stage.Item(0).Theme);
        Assert.Same(stage.Scene.Window.FindResource("Wp81MenuFlyoutItemTheme"), stage.Item(2).Theme);
        Assert.Same(stage.Scene.Window.FindResource("Wp81MenuFlyoutSeparatorTheme"), ((Separator)stage.Flyout.Items[1]!).Theme);
        Assert.Equal(25.5, stage.Item(0).FontSize);
        Assert.Equal(-15 * 25.5 / 1000, stage.Item(0).LetterSpacing, 6);
        Assert.Same(stage.Presenter.FindResource(stage.Presenter.ActualThemeVariant, "PhoneForegroundBrush"), stage.Presenter.Foreground);
    }

    [AvaloniaFact]
    public void A_Desktop_Menus_Items_Keep_Their_Own_Theme()
    {
        // The item theme is keyed and given only to a MenuFlyoutPresenter's own items: a desktop Menu (Tier 4) isn't caught.
        using var clock = new TestClock();
        var item = new MenuItem { Header = "file" };
        using var scene = new Scene(new Menu { Items = { item } });

        Assert.NotSame(scene.Window.FindResource("Wp81MenuFlyoutItemTheme"), item.Theme);
        Assert.NotSame(scene.Window.FindResource("Wp81MenuFlyoutItemTheme"), item.GetValue(StyledElement.ThemeProperty));
        Assert.DoesNotContain(item.GetVisualDescendants(), d => d is Border { Name: "InnerBorder" });
    }

    [AvaloniaFact]
    public void An_Apps_Own_Item_Theme_Wins()
    {
        var own = new ControlTheme(typeof(MenuItem));
        var containers = new ControlTheme(typeof(MenuItem));
        using var stage = new Stage(menu: [new MenuItem { Header = "own", Theme = own }, new MenuItem { Header = "plain" }]);
        stage.Flyout.ItemContainerTheme = containers;
        stage.OpenAtRest();

        Assert.Same(own, stage.Item(0).Theme);
        Assert.Same(containers, stage.Item(1).Theme);
    }

    [AvaloniaFact]
    public void The_Menu_Is_In_The_Windows_Own_Overlay_Layer_The_Windows_Width()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        Assert.True(stage.Flyout.Popup.IsUsingOverlayLayer);
        Assert.Same(stage.Scene.Window, TopLevel.GetTopLevel(stage.Presenter));
        Assert.Equal(0, stage.Bounds().X);
        Assert.Equal(Width, stage.Bounds().Width);
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void The_Menu_Is_Below_Its_Target_If_It_Fits_Else_Above(int shot)
    {
        var (targetTop, menuTop) = Placements[shot];
        using var stage = new Stage(targetTop);
        stage.OpenAtRest();

        Near(targetTop, stage.Button.TranslatePoint(default, stage.Scene.Window)!.Value.Y);
        Near(TargetHeight, stage.Button.Bounds.Height);
        var bounds = stage.Bounds();
        // Below, the menu's top at the target's bottom; above, its bottom at the target's top. Our four items are 4 raw
        // pixels shorter than the phone's (ItemShortfall), so a menu above its target is that much lower at its top.
        var below = menuTop > targetTop;
        Near(FourItemHeight - 4 * ItemShortfall, bounds.Height);
        if (below)
            Near(menuTop, bounds.Y);
        else
            Near(menuTop + FourItemHeight, bounds.Bottom);
    }

    [AvaloniaFact]
    public void A_Menu_Longer_Than_The_Window_Fills_It()
    {
        using var stage = new Stage(targetTop: 312.08, items: LongItems);
        stage.Open();
        // Chosen (the phone's isn't read): it unfolds downwards from the line at its top.
        Assert.Equal(new RelativePoint(0, 0, RelativeUnit.Relative), stage.Part<StackPanel>("InnerBorder").RenderTransformOrigin);
        stage.At(stage.Clock.Now.TotalMilliseconds + 600);

        Assert.Equal(FullWindow, stage.Bounds());
        Near(FirstItemTop, stage.Item(0).TranslatePoint(default, stage.Presenter)!.Value.Y);
    }

    [AvaloniaFact]
    public void A_Drag_On_A_Long_Menu_Doesnt_Move_It_Or_Reach_The_Page()
    {
        using var stage = new Stage(items: LongItems);
        stage.OpenAtRest();
        var pressed = 0;
        stage.Page.AddHandler(InputElement.PointerPressedEvent, (_, _) => pressed++, handledEventsToo: true);
        var first = stage.Item(0).TranslatePoint(default, stage.Scene.Window)!.Value;

        stage.Scene.Window.MouseDown(new Point(225, 600), MouseButton.Left);
        stage.Scene.Window.MouseMove(new Point(225, 400));
        stage.Scene.Window.MouseMove(new Point(225, 200));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(first, stage.Item(0).TranslatePoint(default, stage.Scene.Window)!.Value);
        Assert.Equal(0, pressed);
    }

    [AvaloniaFact]
    public void The_Items_Are_51_25_Apart_From_16_67_The_Text_At_19_17()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        for (var i = 0; i < 4; i++)
        {
            var item = stage.Item(i);
            Near(FirstItemTop + i * (ItemPitch - ItemShortfall), item.TranslatePoint(default, stage.Presenter)!.Value.Y);
            Near(ItemPitch - ItemShortfall, item.Bounds.Height);
            var text = item.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "TextBlock");
            Near(TextLeft, text.TranslatePoint(default, stage.Presenter)!.Value.X);
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Menu_Is_The_Background_With_A_Line_At_Its_Top_And_Bottom(bool dark)
    {
        using var stage = new Stage(variant: dark ? ThemeVariant.Dark : ThemeVariant.Light);
        stage.OpenAtRest();
        var bounds = stage.Bounds();
        using var frame = stage.Scene.Capture();
        var top = (int)Math.Round(bounds.Y * Scale);
        var bottom = (int)Math.Round(bounds.Bottom * Scale);

        // The emulator's: 4 raw rows of the line at the top and the bottom, the background between, the page around.
        for (var row = 0; row < LineRows - 1; row++)
        {
            Assert.Equal(Line(dark), Frames.PixelAt(frame, 1000, top + row));
            Assert.Equal(Line(dark), Frames.PixelAt(frame, 1000, bottom - 1 - row));
        }

        Assert.Equal(Background(dark), Frames.PixelAt(frame, 1000, top + LineRows + 2));
        Assert.Equal(Background(dark), Frames.PixelAt(frame, 1000, bottom - LineRows - 2));
    }

    [AvaloniaFact]
    public void A_Toggle_Item_Draws_The_Same_Checked_And_Unchecked()
    {
        using var stage = new Stage(menu: [new MenuItem { Header = "toggle", ToggleType = MenuItemToggleType.CheckBox }]);
        stage.OpenAtRest();
        using var unchecked_ = stage.Scene.Capture();

        stage.Item(0).IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        using var checked_ = stage.Scene.Capture();

        Assert.True(Frames.SameAs(unchecked_, checked_));
    }

    [AvaloniaFact]
    public void A_Separator_Draws_Nothing_And_Is_0_Tall()
    {
        var separator = new Separator();
        var dash = new MenuItem { Header = "-" };
        using var stage = new Stage(menu: [new MenuItem { Header = "one" }, separator, dash, new MenuItem { Header = "two" }]);
        stage.OpenAtRest();

        Assert.Equal(0, separator.Bounds.Height);
        Assert.Equal(0, dash.Bounds.Height);
        Assert.Empty(separator.GetVisualChildren());
        Near(FirstItemTop + ItemPitch - ItemShortfall, stage.Item(3).TranslatePoint(default, stage.Presenter)!.Value.Y);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Disabled_Items_Text_Is_The_Disabled_Brush_And_A_Tap_On_It_Does_Nothing(bool dark)
    {
        var clicks = 0;
        var disabled = new MenuItem { Header = "disabled", IsEnabled = false };
        disabled.Click += (_, _) => clicks++;
        using var stage = new Stage(variant: dark ? ThemeVariant.Dark : ThemeVariant.Light, menu: [new MenuItem { Header = "one" }, disabled]);
        stage.OpenAtRest();

        var text = (ContentPresenter)disabled.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "TextBlock");
        Assert.Equal(Disabled(dark), ((ISolidColorBrush)text.Foreground!).Color);

        stage.Tap(stage.CentreOf(disabled));
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.Equal(0, clicks);
        Assert.True(stage.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void A_Pressed_Item_Only_Tilts()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var item = stage.Item(1);
        var inner = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "InnerBorder");
        Assert.True(Tilt.GetIsEnabled(item));
        Assert.True(Tilt.GetIsTarget(inner));
        using var before = stage.Scene.Capture();

        stage.Scene.Window.MouseDown(stage.CentreOf(item), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Clock.Now.TotalMilliseconds + 100);

        // Tilted, and no colour of its own: the phone's Pressed is PointerDownThemeAnimation on InnerBorder alone.
        Assert.NotEqual(TiltState.Rest, Tilt.Current(item));
        var layoutRoot = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "LayoutRoot");
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)layoutRoot.Background!).Color);
        Assert.Null(inner.Background);
        stage.Scene.Window.MouseUp(stage.CentreOf(item), MouseButton.Left);
    }

    [AvaloniaFact]
    public void The_Opening_Spreads_The_Line_Below_The_Target_Then_Unfolds_The_Menu_Down()
    {
        using var stage = new Stage();
        stage.Open();
        var outer = stage.Part<Border>("OuterBorder");
        var center = stage.Part<Border>("CenterBorder");
        var inner = stage.Part<StackPanel>("InnerBorder");

        // The first frame, the call's: nothing spread yet; the menu's top line alone, the line by the target below it.
        Assert.Equal(0, ((ScaleTransform)outer.RenderTransform!).ScaleX);
        Assert.Equal(new RelativePoint(0.5, 0, RelativeUnit.Relative), outer.RenderTransformOrigin);
        Assert.Equal(0, ((ScaleTransform)inner.RenderTransform!).ScaleY);
        Assert.Equal(new RelativePoint(0, 0, RelativeUnit.Relative), inner.RenderTransformOrigin);
        Assert.Equal(new Thickness(0, EdgeLine, 0, 0), outer.BorderThickness);
        Assert.Equal(new Thickness(0, EdgeLine, 0, 0), center.BorderThickness);

        var first = stage.Clock.Now.TotalMilliseconds + Tick;
        foreach (var ms in new[] { 0, 10 * Tick, 20 * Tick, 23 * Tick, 25 * Tick, 30 * Tick, 31 * Tick })
        {
            stage.At(first + ms);
            Assert.Equal(Math.Min(1, ms / OuterOpenMs), ((ScaleTransform)outer.RenderTransform!).ScaleX, 6);
            Assert.Equal(Math.Clamp((ms - OuterOpenMs) / (OpenMs - OuterOpenMs), 0, 1), ((ScaleTransform)inner.RenderTransform!).ScaleY, 6);
            Assert.Equal(new Thickness(0, EdgeLine, 0, 0), outer.BorderThickness);
        }

        // At 500 ms, at rest: no transform, both lines.
        stage.At(first + OpenMs);
        Assert.Null(outer.RenderTransform);
        Assert.Null(inner.RenderTransform);
        Assert.Equal(new Thickness(0, EdgeLine), outer.BorderThickness);
        Assert.Equal(new Thickness(0, EdgeLine), center.BorderThickness);
        Assert.False(PhoneMenuFlyout.IsOpening(stage.Presenter));
    }

    [AvaloniaFact]
    public void Above_Its_Target_The_Menu_Unfolds_Up_From_The_Line_At_Its_Bottom()
    {
        using var stage = new Stage(targetTop: Placements[1].TargetTop);
        stage.Open();
        var outer = stage.Part<Border>("OuterBorder");
        var inner = stage.Part<StackPanel>("InnerBorder");

        Assert.Equal(new RelativePoint(0, 1, RelativeUnit.Relative), inner.RenderTransformOrigin);
        Assert.Equal(new Thickness(0, 0, 0, EdgeLine), outer.BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, EdgeLine), stage.Part<Border>("CenterBorder").BorderThickness);
    }

    [AvaloniaFact]
    public void The_Menu_Turns_No_Band()
    {
        // The host's band motion isn't the menu's: no picture of it is turned, and it is never hidden.
        using var stage = new Stage();
        stage.Open();
        var host = PhoneMenuFlyout.HostOf(stage.Presenter)!;
        for (var ms = 0.0; ms < 300; ms += Tick)
        {
            stage.At(stage.Clock.Now.TotalMilliseconds + Tick);
            Assert.Null(host.Bands);
            Assert.Equal(1, stage.Presenter.Opacity);
            Assert.True(stage.Presenter.IsHitTestVisible);
        }
    }

    [AvaloniaFact]
    public void A_Tap_On_An_Item_Raises_Its_Click_Then_Closed_In_One_Frame()
    {
        var events = new List<string>();
        var toggle = new MenuItem { Header = "toggle", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
        toggle.Click += (_, _) => events.Add($"click {toggle.IsChecked}");
        using var stage = new Stage(menu: [new MenuItem { Header = "one" }, toggle]);
        stage.Flyout.Closed += (_, _) => events.Add("closed");
        stage.OpenAtRest();

        stage.Tap(stage.CentreOf(toggle));

        // The toggle's IsChecked flips before its Click; the menu is gone in the same frame.
        Assert.Equal(["click False", "closed"], events);
        Assert.False(stage.Flyout.Popup.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData(200)]
    [InlineData(750)]
    public void A_Tap_Above_Or_Below_The_Menu_Closes_It_At_Once_And_Doesnt_Reach_The_Page(double y)
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var pressed = 0;
        stage.Page.AddHandler(InputElement.PointerPressedEvent, (_, _) => pressed++, handledEventsToo: true);
        var closed = 0;
        stage.Flyout.Closed += (_, _) => closed++;

        stage.Tap(new Point(225, y));

        Assert.Equal(1, closed);
        Assert.False(stage.Flyout.Popup.IsOpen);
        Assert.Equal(0, pressed);
    }

    [AvaloniaFact]
    public void Back_Closes_The_Menu_At_Once_Before_The_NavigationPage_Goes_Back()
    {
        var first = new ContentPage();
        var second = new ContentPage();
        var nav = new NavigationPage();
        using var stage = new Stage(root: new PageNavigationHost { Page = nav });
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();
        second.Content = stage.Page;
        Dispatcher.UIThread.RunJobs();
        stage.OpenAtRest();
        var closed = 0;
        stage.Flyout.Closed += (_, _) => closed++;

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(request);

        // #150: the menu's Closed comes at once, in Back's own frame.
        Assert.True(request.Handled);
        Assert.Equal(1, closed);
        Assert.False(stage.Flyout.Popup.IsOpen);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(second, nav.CurrentPage);
        Assert.False(PhoneMenuFlyout.HostOf(stage.Presenter)!.IsOpen);

        var next = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(next);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(first, nav.CurrentPage);
    }

    [AvaloniaFact]
    public void Hide_Closes_It_At_Once()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        stage.Flyout.Hide();

        Assert.False(stage.Flyout.Popup.IsOpen);
    }

    [AvaloniaFact]
    public void It_Opens_Again_Placed_And_Moving()
    {
        using var stage = new Stage(targetTop: Placements[1].TargetTop);
        stage.OpenAtRest();
        stage.Flyout.Hide();
        stage.Button.Margin = new Thickness(19, Placements[0].TargetTop, 0, 0);
        Dispatcher.UIThread.RunJobs();

        stage.Open();
        Assert.True(PhoneMenuFlyout.IsOpening(stage.Presenter));
        Assert.Equal(new Thickness(0, EdgeLine, 0, 0), stage.Part<Border>("OuterBorder").BorderThickness);
        stage.At(stage.Clock.Now.TotalMilliseconds + 600);
        Near(Placements[0].MenuTop, stage.Bounds().Y);
    }

    [AvaloniaFact]
    public void Opened_By_IsOpen_It_Is_Placed_Against_Its_Target_Too()
    {
        // Opened by IsOpen, the flyout opens its popup before it says it is open: the menu is placed again then.
        using var stage = new Stage(targetTop: Placements[1].TargetTop);
        stage.Button.Flyout = stage.Flyout;

        stage.Flyout.IsOpen = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0, 0, 0, EdgeLine), stage.Part<Border>("OuterBorder").BorderThickness);
        stage.At(stage.Clock.Now.TotalMilliseconds + 600);
        Assert.True(stage.Flyout.Popup.IsOpen);
        Near(Placements[1].TargetTop, stage.Bounds().Bottom);
    }

    [AvaloniaFact]
    public void The_Menu_Is_Placed_Again_As_The_Window_Changes_Size()
    {
        using var stage = new Stage(targetTop: Placements[0].TargetTop);
        stage.OpenAtRest();

        stage.Scene.Window.Height = 600;
        Dispatcher.UIThread.RunJobs();

        // 369.58 + 244.58 is past 600: above the target now.
        Near(Placements[0].TargetTop, stage.Bounds().Bottom);
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own_MenuFlyout()
    {
        using var stage = new Stage(themes: [new SimpleTheme()]);
        stage.Open();

        Assert.False(PhoneMenuFlyout.GetIsEnabled(stage.Presenter));
        Assert.Null(PhoneMenuFlyout.HostOf(stage.Presenter));
        Assert.Contains(stage.Presenter.GetVisualDescendants(), d => d is ScrollViewer);
        stage.Flyout.Hide();
        Assert.False(stage.Flyout.Popup.IsOpen);
    }

    [AvaloniaFact]
    public void A_Presenter_Given_Another_Theme_Loses_Every_Wp81_Value()
    {
        using var stage = new Stage();
        stage.Open();
        stage.At(stage.Clock.Now.TotalMilliseconds + 2 * Tick);
        Assert.True(PhoneMenuFlyout.IsOpening(stage.Presenter));

        stage.Presenter.Theme = new ControlTheme(typeof(MenuFlyoutPresenter));
        Dispatcher.UIThread.RunJobs();

        Assert.False(PhoneMenuFlyout.GetIsEnabled(stage.Presenter));
        Assert.Null(PhoneMenuFlyout.HostOf(stage.Presenter));
        Assert.Null(stage.Presenter.Background);
        Assert.False(stage.Flyout.Popup.ShouldUseOverlayLayer);
        Assert.False(stage.Item(0).IsSet(StyledElement.ThemeProperty));
        Assert.Equal(0, stage.Flyout.Popup.VerticalOffset);
    }

    [AvaloniaFact]
    public void Turned_Off_It_Takes_The_Item_Themes_Back()
    {
        using var stage = new Stage(menu: [new MenuItem { Header = "one" }, new Separator()]);
        stage.OpenAtRest();
        Assert.True(stage.Item(0).IsSet(StyledElement.ThemeProperty));

        PhoneMenuFlyout.SetIsEnabled(stage.Presenter, false);
        Dispatcher.UIThread.RunJobs();

        Assert.False(stage.Item(0).IsSet(StyledElement.ThemeProperty));
        Assert.False(((Separator)stage.Flyout.Items[1]!).IsSet(StyledElement.ThemeProperty));
        Assert.Null(PhoneMenuFlyout.HostOf(stage.Presenter));
    }

    [AvaloniaFact]
    public void A_Menu_Above_Its_Target_Stays_On_It_As_Items_Come()
    {
        using var stage = new Stage(targetTop: Placements[1].TargetTop);
        stage.OpenAtRest();

        stage.Flyout.Items.Add(new MenuItem { Header = "another" });
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Bounds().Height > FourItemHeight);
        Near(Placements[1].TargetTop, stage.Bounds().Bottom);
    }

    [AvaloniaFact]
    public void A_Target_Past_The_Windows_Bottom_Keeps_The_Menu_In_The_Window()
    {
        using var stage = new Stage(targetTop: 850);
        stage.OpenAtRest();

        Near(FullWindow.Bottom, stage.Bounds().Bottom);
    }

    [AvaloniaFact]
    public void An_ItemContainerTheme_Set_After_The_Menu_Has_Opened_Wins()
    {
        var containers = new ControlTheme(typeof(MenuItem));
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Flyout.Hide();
        Assert.Same(stage.Scene.Window.FindResource("Wp81MenuFlyoutItemTheme"), stage.Item(0).Theme);

        stage.Flyout.ItemContainerTheme = containers;
        stage.OpenAtRest();

        Assert.Same(containers, stage.Item(0).Theme);
    }

    [AvaloniaFact]
    public void Back_Kept_Open_By_The_Apps_Closing_Asks_Once_And_Leaves_The_Menu_Shown()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var closings = 0;
        stage.Flyout.Closing += (_, e) =>
        {
            closings++;
            e.Cancel = true;
        };

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(request);
        Assert.True(request.Handled);
        Assert.Equal(1, closings);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);

        // Asked once: the host isn't left closing, to close the menu again later.
        Assert.Equal(1, closings);
        Assert.True(stage.Flyout.IsOpen);
        Assert.False(PhoneMenuFlyout.HostOf(stage.Presenter)!.IsClosing);
    }

    [AvaloniaFact]
    public void Turned_Off_And_On_While_Open_The_New_Host_Turns_No_Band_Of_The_Menu()
    {
        // The new host is given Moves before it starts the open menu's opening.
        using var stage = new Stage();
        stage.OpenAtRest();

        PhoneMenuFlyout.SetIsEnabled(stage.Presenter, false);
        PhoneMenuFlyout.SetIsEnabled(stage.Presenter, true);
        Dispatcher.UIThread.RunJobs();

        var host = PhoneMenuFlyout.HostOf(stage.Presenter)!;
        Assert.True(host.IsOpen);
        Assert.Null(host.Bands);
        Assert.Equal(1, stage.Presenter.Opacity);
    }

    [AvaloniaFact]
    public void Turned_Off_And_On_Again_It_Keeps_The_Menu_Hosted()
    {
        // The old binder lets go from the dispatcher: the new one is made after it has given the popup back what its
        // host set, so the new host doesn't take those values for the popup's own and lose them.
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Flyout.Hide();

        PhoneMenuFlyout.SetIsEnabled(stage.Presenter, false);
        PhoneMenuFlyout.SetIsEnabled(stage.Presenter, true);
        Dispatcher.UIThread.RunJobs();
        stage.Open();

        Assert.NotNull(PhoneMenuFlyout.HostOf(stage.Presenter));
        Assert.True(stage.Flyout.Popup.ShouldUseOverlayLayer);
        stage.At(stage.Clock.Now.TotalMilliseconds + 600);
        Near(Placements[0].MenuTop, stage.Bounds().Y);
    }
}
