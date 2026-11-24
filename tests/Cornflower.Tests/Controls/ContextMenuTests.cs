using Avalonia;
using Avalonia.Controls;
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
/// The plain ContextMenu (ADR 0007, answer 7; <c>Controls/ContextMenu.axaml</c>): the MenuFlyout's look and motion, on the
/// MenuFlyout's binder (<see cref="PhoneContextMenu"/>). No reference: these hold the plain rules and the MenuFlyout's
/// own values, which <see cref="MenuFlyoutTests"/> holds to the phone.
/// </summary>
public class ContextMenuTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>A page with a Button that owns the menu, at <paramref name="targetTop"/>.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(double targetTop = 200, ThemeVariant? variant = null, IEnumerable<IStyle>? themes = null, params object[] items)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Menu = new ContextMenu();
            foreach (var item in items.Length > 0 ? items : [new MenuItem { Header = "copy" }, new MenuItem { Header = "paste" }])
                Menu.Items.Add(item);
            Button = new Button
            {
                Content = "hold",
                ContextMenu = Menu,
                Margin = new Thickness(19, targetTop, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            try
            {
                Scene = new Scene(new Panel { Children = { Button } }, variant, themes: themes);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public ContextMenu Menu { get; }

        public Button Button { get; }

        public Scene Scene { get; }

        public Popup Popup => (Popup)Menu.Parent!;

        public MenuItem Item(int index) => (MenuItem)Menu.Items[index]!;

        public void Open()
        {
            Menu.Open(Button);
            Dispatcher.UIThread.RunJobs();
        }

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
            var host = Menu.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
            return new Rect(Canvas.GetLeft(host), Canvas.GetTop(host), Menu.Bounds.Width, Menu.Bounds.Height);
        }

        public T Part<T>(string name) where T : Control => PlainParts.Part<T>(Menu, name);

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    // The MenuFlyout's setters, by the same keys, and its template's parts, in both variants; no ScrollViewer.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void It_Is_Templated_With_The_MenuFlyouts_Look(string variant)
    {
        using var stage = new Stage(variant: VariantOf(variant));
        stage.OpenAtRest();
        var menu = stage.Menu;

        var theme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(typeof(ContextMenu)));
        var template = theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, menu.Template);
        Assert.True(PhoneContextMenu.GetIsEnabled(menu));
        Assert.Same(PlainParts.Resource<IBrush>(menu, "MenuFlyoutBackgroundThemeBrush"), menu.Background);
        Assert.Same(PlainParts.Resource<IBrush>(menu, "MenuFlyoutBorderThemeBrush"), menu.BorderBrush);
        Assert.Same(PlainParts.Resource<IBrush>(menu, "PhoneForegroundBrush"), menu.Foreground);
        Assert.Equal(PlainParts.Resource<Thickness>(menu, "MenuFlyoutPortraitBorderThemeThickness"), menu.BorderThickness);
        Assert.Equal(PlainParts.Resource<Thickness>(menu, "MenuFlyoutPortraitThemePadding"), menu.Padding);
        Assert.Equal(PlainParts.Resource<double>(menu, "FlyoutThemeMinHeight"), menu.MinHeight);
        Assert.Same(PlainParts.Resource<FontFamily>(menu, "PhoneFontFamilySemiLight"), stage.Item(0).FontFamily);
        Assert.Equal(25.5, stage.Item(0).FontSize);
        Assert.Equal(-15 * 25.5 / 1000, stage.Item(0).LetterSpacing, 6);
        Assert.True(menu.Focusable);
        // As tall as its items and padding, the borders taking from the bottom padding (PhoneMenuFlyoutRoot).
        var tall = stage.Item(0).Bounds.Height + stage.Item(1).Bounds.Height + menu.Padding.Top + menu.Padding.Bottom;
        Assert.InRange(menu.Bounds.Height, tall - 1 / Scene.ReferenceScaling, tall + 1 / Scene.ReferenceScaling);
        Assert.NotNull(stage.Part<Border>("OuterBorder"));
        Assert.NotNull(stage.Part<Border>("CenterBorder"));
        Assert.True(stage.Part<StackPanel>("InnerBorder").ClipToBounds);
        Assert.DoesNotContain(menu.GetVisualDescendants(), d => d is ScrollViewer);
        // Its items are the plain MenuItem's, by type.
        Assert.Null(stage.Item(0).Theme);
        var itemTheme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(typeof(MenuItem)));
        Assert.Same(itemTheme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value, stage.Item(0).Template);
    }

    // As a MenuFlyout: the window's own overlay layer, the window's width, below its target if it fits there.
    [AvaloniaFact]
    public void It_Is_In_The_Windows_Overlay_Layer_The_Windows_Width_Below_Its_Target()
    {
        using var stage = new Stage(targetTop: 200);
        stage.OpenAtRest();

        Assert.True(stage.Popup.IsUsingOverlayLayer);
        Assert.Same(stage.Scene.Window, TopLevel.GetTopLevel(stage.Menu));
        var bounds = stage.Bounds();
        Assert.Equal(0, bounds.X);
        Assert.Equal(Scene.ReferenceWidth, bounds.Width);
        var targetBottom = stage.Button.TranslatePoint(new Point(0, stage.Button.Bounds.Height), stage.Scene.Window)!.Value.Y;
        Assert.Equal(targetBottom, bounds.Y, 3);
    }

    [AvaloniaFact]
    public void Above_Its_Target_If_It_Doesnt_Fit_Below()
    {
        using var stage = new Stage(targetTop: 700);
        stage.OpenAtRest();

        var bounds = stage.Bounds();
        var targetTop = stage.Button.TranslatePoint(default, stage.Scene.Window)!.Value.Y;
        Assert.Equal(targetTop, bounds.Bottom, 3);
    }

    // The MenuFlyout's opening: the line below the target spreads over 380 ms, then the menu unfolds down until 500 ms.
    [AvaloniaFact]
    public void It_Opens_With_The_MenuFlyouts_Motion()
    {
        using var stage = new Stage();
        stage.Open();
        var outer = stage.Part<Border>("OuterBorder");
        var inner = stage.Part<StackPanel>("InnerBorder");

        Assert.True(PhoneContextMenu.IsOpening(stage.Menu));
        Assert.Equal(0, ((ScaleTransform)outer.RenderTransform!).ScaleX);
        Assert.Equal(new Thickness(0, 1.5, 0, 0), outer.BorderThickness);
        var first = stage.Clock.Now.TotalMilliseconds + Tick;
        foreach (var ms in new[] { 10 * Tick, 25 * Tick, 28 * Tick })
        {
            stage.At(first + ms);
            Assert.Equal(Math.Min(1, ms / OuterOpenMs), ((ScaleTransform)outer.RenderTransform!).ScaleX, 6);
            Assert.Equal(Math.Clamp((ms - OuterOpenMs) / (OpenMs - OuterOpenMs), 0, 1), ((ScaleTransform)inner.RenderTransform!).ScaleY, 6);
        }

        stage.At(first + OpenMs);
        Assert.Null(outer.RenderTransform);
        Assert.Equal(new Thickness(0, 1.5), outer.BorderThickness);
        Assert.False(PhoneContextMenu.IsOpening(stage.Menu));
    }

    [AvaloniaFact]
    public void With_Animated_Off_It_Opens_At_Rest()
    {
        using var config = new ConfigScope();
        CornflowerConfig.Animated = false;
        using var stage = new Stage();
        stage.Open();

        Assert.False(PhoneContextMenu.IsOpening(stage.Menu));
        Assert.Null(stage.Part<Border>("OuterBorder").RenderTransform);
    }

    [AvaloniaFact]
    public void A_Tap_On_An_Item_Raises_Its_Click_And_Closes_It_At_Once()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var clicked = 0;
        var closed = 0;
        stage.Item(0).Click += (_, _) => clicked++;
        stage.Menu.Closed += (_, _) => closed++;

        var at = PlainParts.Press(stage.Scene, stage.Item(0));
        PlainParts.Release(stage.Scene, at);

        Assert.Equal(1, clicked);
        Assert.Equal(1, closed);
        Assert.False(stage.Popup.IsOpen);
        Assert.False(PhoneContextMenu.HostOf(stage.Menu)!.IsOpen);
    }

    // Back closes it at once, as the MenuFlyout, and is taken.
    [AvaloniaFact]
    public void Back_Closes_It_At_Once()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(request);

        Assert.True(request.Handled);
        Assert.False(stage.Menu.IsOpen);
        Assert.False(stage.Popup.IsOpen);
    }

    // Its own Separators are the MenuFlyout's: nothing drawn, 0 tall.
    [AvaloniaFact]
    public void Its_Separators_Are_The_MenuFlyouts_Nothing()
    {
        var separator = new Separator();
        using var stage = new Stage(200, null, null, new MenuItem { Header = "copy" }, separator, new MenuItem { Header = "paste" });
        stage.OpenAtRest();

        Assert.Same(stage.Scene.Window.FindResource("Wp81MenuFlyoutSeparatorTheme"), separator.Theme);
        Assert.Equal(0, separator.Bounds.Height);
        Assert.Empty(separator.GetVisualChildren());
    }

    // It opens again, placed against its target and moving, as the first time.
    [AvaloniaFact]
    public void It_Opens_Again_Placed_And_Moving()
    {
        using var stage = new Stage(targetTop: 200);
        stage.OpenAtRest();
        stage.Menu.Close();
        Dispatcher.UIThread.RunJobs();

        stage.Open();
        Assert.True(PhoneContextMenu.IsOpening(stage.Menu));
        stage.At(stage.Clock.Now.TotalMilliseconds + 600);
        var targetBottom = stage.Button.TranslatePoint(new Point(0, stage.Button.Bounds.Height), stage.Scene.Window)!.Value.Y;
        Assert.Equal(targetBottom, stage.Bounds().Y, 3);
        Assert.True(stage.Popup.IsUsingOverlayLayer);
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own_ContextMenu()
    {
        using var stage = new Stage(themes: [new SimpleTheme()]);
        stage.Open();

        Assert.False(PhoneContextMenu.GetIsEnabled(stage.Menu));
        Assert.Null(PhoneContextMenu.HostOf(stage.Menu));
        Assert.Contains(stage.Menu.GetVisualDescendants(), d => d is ScrollViewer);
        stage.Menu.Close();
    }
}
