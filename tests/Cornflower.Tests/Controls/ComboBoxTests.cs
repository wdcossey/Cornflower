using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ComboBoxReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ComboBox and ComboBoxItem (<c>Controls/ComboBox.axaml</c>, <see cref="PhoneComboBox"/>) against the phone's
/// style and the reference app's dumps and probes (<see cref="ComboBoxReadings"/>). docs/controls/ComboBox has the plan
/// and the comparison.
/// </summary>
public class ComboBoxTests
{
    /// <summary>The reference app's stage: the ComboBox in a slot 19 in and 12 down, on the page's background.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(int items, int selected = 0, ThemeVariant? variant = null, double height = OpenStageHeight, string? header = "combo box")
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Combo = new ComboBox();
            if (header is not null)
                ControlHeader.SetHeader(Combo, header);
            for (var i = 1; i <= items; i++)
                Combo.Items.Add(items <= 3 && height == ClosedStageHeight ? Words[i - 1] : "item " + i);
            Combo.SelectedIndex = selected;
            Below = new Button { Content = "below", VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left };
            var stage = new Grid { Children = { new Grid { Margin = Slot, Children = { Combo } }, Below } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            try
            {
                Scene = new Scene(stage, variant, width: StageWidth, height: height);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        private static readonly string[] Words = ["one", "two", "three"];

        public TestClock Clock { get; }

        public ComboBox Combo { get; }

        public Button Below { get; }

        public Scene Scene { get; }

        public double Now => Clock.Now.TotalMilliseconds;

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
            Layout();
        }

        public void Layout()
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public void Open()
        {
            Combo.IsDropDownOpen = true;
            Layout();
        }

        public void OpenAtRest()
        {
            Open();
            At(Now + 200);
        }

        public T Part<T>(string name) where T : Control =>
            Combo.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        public Border Box => Part<Border>("Background");

        /// <summary>Where a visual is in the ComboBox.</summary>
        public Rect InCombo(Visual visual) => new(visual.TranslatePoint(default, Combo)!.Value, visual.Bounds.Size);

        public IReadOnlyList<ComboBoxItem> Items => Combo.GetVisualDescendants().OfType<ComboBoxItem>().ToList();

        public ContentPresenter TextOf(ComboBoxItem item) => item.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");

        public Point CentreOf(Visual visual) =>
            visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), Scene.Window)!.Value;

        public void Tap(Point at)
        {
            Scene.Window.MouseDown(at, MouseButton.Left);
            Layout();
            Scene.Window.MouseUp(at, MouseButton.Left);
            Layout();
        }

        public void Back()
        {
            Scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Layout();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color ForegroundOf(Visual visual) => ColourOf(visual.GetValue(TextElement.ForegroundProperty));

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static void Near(double expected, double actual, double within = 0.2) =>
        Assert.True(Math.Abs(expected - actual) <= within, $"Expected {expected}, got {actual}.");

    // The closed box, as the dump laid it out: the header, the box 4.5 into its overhang, the selected item's text in it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Closed_It_Is_Laid_Out_And_Coloured_As_The_Phones(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(3, variant: VariantOf(variant), height: ClosedStageHeight);
        stage.Layout();

        Near(ControlWidth, stage.Combo.Bounds.Width, 0.5);   // 19 in from each side: the phone rounds the right edge the other way
        Near(HeaderHeight, stage.Part<ContentPresenter>("HeaderContentPresenter").Bounds.Height);
        var box = stage.InCombo(stage.Box);
        Near(BoxBounds.Y, box.Y);
        Near(BoxBounds.Height, box.Height);
        Near(BoxBounds.Width, box.Width, 0.5);
        Assert.Equal(new Thickness(2.5), stage.Box.BorderThickness);

        var one = stage.TextOf(stage.Items[0]);
        var text = stage.InCombo(one);
        Near(TextLeft, text.X);
        Near(InlineTextTop, text.Y, 0.25);   // the lift is laid out on the pixel; the phone's transform isn't
        Near(ItemTextHeight, text.Height);
        Assert.Equal(20.26, stage.Combo.FontSize);

        Assert.Equal(Foreground(dark), ColourOf(stage.Box.BorderBrush));
        Assert.Equal(Transparent, ColourOf(stage.Box.Background));
        Assert.Equal(Foreground(dark), ForegroundOf(one));
        Assert.Equal(Header(dark), ForegroundOf(stage.Part<ContentPresenter>("HeaderContentPresenter")));
        // The box shows the selected item alone: the next is under the box's foot, clipped.
        Assert.True(stage.InCombo(stage.TextOf(stage.Items[1])).Y >= box.Bottom - 2.5);
    }

    // DropDownOffset: closed, the items are lifted so the selected one is in the box (-8.5 less its index times the step).
    [AvaloniaFact]
    public void Closed_The_Selected_Item_Shows_In_The_Box()
    {
        using var stage = new Stage(5, selected: 3);
        stage.Layout();
        var host = stage.Part<PhoneComboBoxItemsHost>("ItemsPresenterHost");
        Near(8.5 + 3 * ItemStep, host.Lift, 0.05 + 3 * StepShort);
        Near(InlineTextTop, stage.InCombo(stage.TextOf(stage.Items[3])).Y, 0.25);

        stage.Combo.SelectedIndex = 1;
        stage.Layout();
        Near(InlineTextTop, stage.InCombo(stage.TextOf(stage.Items[1])).Y, 0.25);
    }

    // #149: the storyboard's ease doesn't show; the box holds its closed size four frames and is open on the fifth, 78 ms.
    [AvaloniaTheory]
    [InlineData(3)]
    [InlineData(5)]
    public void Inline_It_Opens_In_One_Frame_78_Ms_After_The_Call(int count)
    {
        using var stage = new Stage(count, selected: 0);
        stage.Layout();
        stage.Open();
        stage.At(4 * Tick);
        Near(BoxBounds.Height, stage.Box.Bounds.Height);
        Assert.False(PhoneComboBox.GetIsShownOpen(stage.Combo));

        stage.At(OpenAfter);
        Assert.True(PhoneComboBox.GetIsShownOpen(stage.Combo));
        // Each item is 0.42 short of the phone's step (a known gap: Avalonia rounds each side of the padding to the pixel).
        Near(OpenBoxHeight(count), stage.Box.Bounds.Height, count * StepShort + 0.01);
        for (var i = 0; i < count; i++)
            Near(InlineTextTop + 8.5 + i * ItemStep, stage.InCombo(stage.TextOf(stage.Items[i])).Y, 0.2 + i * StepShort);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Open_The_Box_Is_White_With_The_Accent_Border_And_The_Selected_Item_In_The_Accent(string variant)
    {
        using var stage = new Stage(5, selected: 0, variant: VariantOf(variant));
        stage.OpenAtRest();
        Assert.Equal(OpenBackground, ColourOf(stage.Box.Background));
        Assert.Equal(Accent, ColourOf(stage.Box.BorderBrush));
        Assert.Equal(Accent, ForegroundOf(stage.TextOf(stage.Items[0])));
        Assert.Equal(OpenForeground, ForegroundOf(stage.TextOf(stage.Items[1])));
    }

    // Closing shows at once, on the first frame (#149).
    [AvaloniaFact]
    public void Inline_It_Closes_At_Once()
    {
        using var stage = new Stage(5);
        stage.OpenAtRest();
        stage.Combo.IsDropDownOpen = false;
        stage.Layout();
        Near(BoxBounds.Height, stage.Box.Bounds.Height);
        Assert.Equal(Transparent, ColourOf(stage.Box.Background));
    }

    [AvaloniaFact]
    public void A_Tap_On_An_Item_Selects_It_And_Closes_The_Box_With_Avalonias_Events()
    {
        using var stage = new Stage(5, selected: 0);
        var opened = 0;
        var closed = 0;
        stage.Combo.DropDownOpened += (_, _) => opened++;
        stage.Combo.DropDownClosed += (_, _) => closed++;
        stage.Layout();
        stage.Tap(stage.CentreOf(stage.Box));
        Assert.True(stage.Combo.IsDropDownOpen);
        Assert.Equal(1, opened);
        stage.At(stage.Now + 100);

        // The press is the item's: the box stays open until the release (Avalonia's own press would close it).
        var item = stage.CentreOf(stage.Items[2]);
        stage.Scene.Window.MouseDown(item, MouseButton.Left);
        stage.Layout();
        Assert.True(PhoneComboBox.GetIsShownOpen(stage.Combo));
        Assert.Equal(0, stage.Combo.SelectedIndex);
        stage.Scene.Window.MouseUp(item, MouseButton.Left);
        stage.Layout();
        Assert.Equal(2, stage.Combo.SelectedIndex);
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.Equal(1, closed);
        stage.At(stage.Now + 400);   // the item's tilt gone
        Near(InlineTextTop, stage.InCombo(stage.TextOf(stage.Items[2])).Y, 0.25);
    }

    [AvaloniaFact]
    public void A_Tap_Outside_Closes_The_Box_And_Reaches_What_It_Was_On()
    {
        using var stage = new Stage(5);
        var clicked = 0;
        stage.Below.Click += (_, _) => clicked++;
        stage.OpenAtRest();
        stage.Tap(stage.CentreOf(stage.Below));
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.Equal(1, clicked);
        Assert.Equal(0, stage.Combo.SelectedIndex);
    }

    [AvaloniaFact]
    public void Back_Closes_The_Box()
    {
        using var stage = new Stage(5);
        stage.OpenAtRest();
        var reached = false;
        stage.Scene.Window.AddHandler(TopLevel.BackRequestedEvent, (_, _) => reached = true);
        stage.Back();
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.False(reached);
    }

    // #149: the press on the closed box is the Button's tilt on Background, with the accent fill; the release opens it
    // and the tilt is gone on the next frame.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Box_In_The_Accent_And_The_Release_Opens_It_With_The_Tilt_Gone()
    {
        using var stage = new Stage(3);
        stage.Layout();
        var at = stage.CentreOf(stage.Box) + new Vector(120, 0);
        stage.Scene.Window.MouseDown(at, MouseButton.Left);
        stage.At(stage.Now + 160);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(stage.Combo));
        Assert.Same(stage.Box, CfTilt.TargetOf(stage.Combo));
        Assert.Equal(Accent, ColourOf(stage.Box.Background));
        Assert.Equal(OverAccent, ColourOf(stage.Combo.Foreground));

        stage.Scene.Window.MouseUp(at, MouseButton.Left);
        stage.Layout();
        Assert.True(stage.Combo.IsDropDownOpen);
        Assert.Equal(TiltState.Rest, CfTilt.Current(stage.Combo));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_It_Dims_The_Header_The_Border_And_The_Text(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(3, variant: VariantOf(variant), height: ClosedStageHeight);
        stage.Combo.IsEnabled = false;
        stage.Layout();
        Assert.Equal(Disabled(dark), ColourOf(stage.Box.BorderBrush));
        Assert.Equal(Disabled(dark), ForegroundOf(stage.TextOf(stage.Items[0])));
        Assert.Equal(1, stage.TextOf(stage.Items[0]).Opacity);   // dimmed once, by the colour: the item isn't disabled itself
        Assert.Equal(Disabled(dark), ForegroundOf(stage.Part<ContentPresenter>("HeaderContentPresenter")));
    }

    // InlinePlaceholder: nothing selected, the phone's "choose an item" in PhoneMidBrush, the items hidden.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Inline_With_Nothing_Selected_It_Shows_The_Placeholder(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new Stage(3, selected: -1, variant: VariantOf(variant), height: ClosedStageHeight);
        stage.Layout();
        var placeholder = stage.Part<ContentPresenter>("PlaceholderTextContentPresenter");
        Assert.Equal("choose an item", placeholder.Content);
        Assert.Equal(1, placeholder.Opacity);
        Assert.Equal(Header(dark), ForegroundOf(placeholder));
        Near(PlaceholderLeft, stage.InCombo(placeholder).X);
        Near(PlaceholderTop, stage.InCombo(placeholder).Y);
        Assert.Equal(0, stage.Part<ItemsPresenter>("PART_ItemsPresenter").Opacity);
    }

    // #25: by the count. 5 open inline; 6 open the full-screen picker.
    [AvaloniaTheory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(10, true)]
    public void Five_Items_Open_Inline_And_Six_Full_Screen(int count, bool full)
    {
        using var stage = new Stage(count);
        stage.OpenAtRest();
        Assert.Equal(full, PhoneComboBox.GetIsFull(stage.Combo));
        Assert.Equal(full, PhoneComboBox.PickerOf(stage.Combo)?.IsOpen == true);
        Assert.Equal(!full, PhoneComboBox.GetIsShownOpen(stage.Combo));
    }

    [AvaloniaFact]
    public void Full_The_Box_Shows_The_Selected_Item_Or_The_Placeholder()
    {
        using var stage = new Stage(6, selected: 1);
        stage.Layout();
        var presenter = stage.Part<ContentPresenter>("ContentPresenter");
        Assert.True(stage.Part<Panel>("FlyoutContent").IsVisible);
        Assert.False(stage.Part<Panel>("ShortList").IsVisible);
        Assert.Equal("item 2", presenter.Content);
        Near(TextLeft, stage.InCombo(presenter).X);
        Near(FullTextTop, stage.InCombo(presenter).Y);
        Near(BoxBounds.Height, stage.Box.Bounds.Height);

        stage.Combo.SelectedIndex = -1;
        stage.Layout();
        var placeholder = stage.Part<TextBlock>("PlaceholderTextBlock");
        Assert.True(placeholder.IsVisible);
        Assert.Equal("choose an item", placeholder.Text);
        Assert.Equal(FullPlaceholderOpacity(true), placeholder.Opacity, 3);
    }

    // The picker: titled with the placeholder in capitals, the selected item in the accent; a tap picks and closes.
    [AvaloniaFact]
    public void Full_It_Opens_The_List_Picker_Which_Picks_And_Closes_The_Drop_Down()
    {
        using var stage = new Stage(7, selected: 0);
        var closed = 0;
        stage.Combo.DropDownClosed += (_, _) => closed++;
        stage.Open();
        stage.At(stage.Now + 600);   // the picker's rows at rest
        var picker = Assert.IsType<ListPickerFlyout>(PhoneComboBox.PickerOf(stage.Combo));
        Assert.True(picker.IsOpen);
        Assert.Equal("CHOOSE AN ITEM", PickerFlyoutBase.GetTitle(picker));
        var presenter = Assert.IsType<ListPickerFlyoutPresenter>(picker.Popup.Child);
        var list = Assert.IsAssignableFrom<ListBox>(presenter.ItemsHost);
        Assert.Equal(7, list.ItemCount);
        Assert.Equal(Accent, ColourOf(list.ContainerFromIndex(0)!.GetValue(TextElement.ForegroundProperty)));
        Assert.NotEqual(Accent, ColourOf(list.ContainerFromIndex(1)!.GetValue(TextElement.ForegroundProperty)));

        stage.Tap(stage.CentreOf(list.ContainerFromIndex(4)!));
        Assert.Equal(4, stage.Combo.SelectedIndex);
        Assert.Equal("item 5", stage.Combo.SelectionBoxItem);
        stage.At(stage.Now + 600);
        Assert.False(picker.IsOpen);
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.Equal(1, closed);
    }

    [AvaloniaFact]
    public void Full_Back_Closes_The_Picker_With_The_Selection_Kept()
    {
        using var stage = new Stage(6, selected: 2);
        stage.OpenAtRest();
        stage.Back();
        stage.At(stage.Now + 600);
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.Equal(2, stage.Combo.SelectedIndex);
    }

    // ComboBoxItem's own Disabled: ComboBoxItemDisabledThemeOpacity, 0.4.
    [AvaloniaFact]
    public void A_Disabled_Item_Is_Dimmed_To_0_4()
    {
        using var stage = new Stage(3);
        stage.Layout();
        stage.Items[1].IsEnabled = false;
        stage.Layout();
        Assert.Equal(0.4, stage.TextOf(stage.Items[1]).Opacity, 3);
        Assert.Equal(1, stage.TextOf(stage.Items[0]).Opacity);
    }

    [AvaloniaFact]
    public void With_No_Header_The_Header_Is_Collapsed()
    {
        using var stage = new Stage(3, header: null, height: ClosedStageHeight);
        stage.Layout();
        Assert.False(stage.Part<ContentPresenter>("HeaderContentPresenter").IsVisible);
        Near(9.5, stage.InCombo(stage.Box).Y);
    }

    // The hidden placeholder over the first item doesn't take its tap.
    [AvaloniaFact]
    public void A_Tap_On_The_First_Item_Selects_It_Under_The_Hidden_Placeholder()
    {
        using var stage = new Stage(5, selected: 3);
        stage.OpenAtRest();
        var text = stage.TextOf(stage.Items[0]);
        stage.Tap(text.TranslatePoint(new Point(10, text.Bounds.Height / 2), stage.Scene.Window)!.Value);
        Assert.Equal(0, stage.Combo.SelectedIndex);
    }

    // Of two equal items, the picker picks the one tapped, as the inline box does.
    [AvaloniaFact]
    public void Full_Of_Equal_Items_The_One_Tapped_Is_Picked()
    {
        using var stage = new Stage(0);
        foreach (var name in new[] { "ann", "bob", "ann", "cy", "dee", "eve" })
            stage.Combo.Items.Add(name);
        stage.Combo.SelectedIndex = 1;
        stage.Open();
        stage.At(stage.Now + 600);
        var list = Assert.IsType<ListPickerFlyoutPresenter>(PhoneComboBox.PickerOf(stage.Combo)!.Popup.Child).ItemsHost!;
        stage.Tap(stage.CentreOf(list.ContainerFromIndex(2)!));
        Assert.Equal(2, stage.Combo.SelectedIndex);
    }

    // An item disabled in the box is disabled in the picker; a ComboBoxItem's text content shows as its text.
    [AvaloniaFact]
    public void Full_A_Disabled_Item_Cant_Be_Picked_And_Items_Written_As_Controls_Show_Their_Text()
    {
        using var stage = new Stage(0);
        for (var i = 0; i < 6; i++)
            stage.Combo.Items.Add(new ComboBoxItem { Content = new TextBlock { Text = "row " + i }, IsEnabled = i != 3 });
        stage.Combo.SelectedIndex = 0;
        stage.Open();
        stage.At(stage.Now + 600);
        var list = Assert.IsType<ListPickerFlyoutPresenter>(PhoneComboBox.PickerOf(stage.Combo)!.Popup.Child).ItemsHost!;
        Assert.Equal("row 3", list.Items[3]);
        Assert.False(list.ContainerFromIndex(3)!.IsEnabled);
        stage.Tap(stage.CentreOf(list.ContainerFromIndex(3)!));
        Assert.Equal(0, stage.Combo.SelectedIndex);
    }

    // Closed and opened again while its picker is still closing, it opens once the close has run.
    [AvaloniaFact]
    public void Full_Opened_Again_While_Closing_It_Opens_After_The_Close()
    {
        using var stage = new Stage(6);
        stage.Open();
        stage.At(stage.Now + 600);
        stage.Combo.IsDropDownOpen = false;
        stage.Layout();
        stage.Combo.IsDropDownOpen = true;
        stage.Layout();
        stage.At(stage.Now + 600);
        Assert.True(stage.Combo.IsDropDownOpen);
        Assert.True(PhoneComboBox.PickerOf(stage.Combo)!.IsOpen);
    }

    // Opened before it is in a window, it opens there, and a tap outside closes it.
    [AvaloniaFact]
    public void Opened_Before_It_Is_In_A_Window_It_Opens_There()
    {
        using var clock = new TestClock();
        var combo = new ComboBox { Items = { "one", "two" }, SelectedIndex = 0, IsDropDownOpen = true, VerticalAlignment = VerticalAlignment.Top };
        var below = new Button { Content = "below", VerticalAlignment = VerticalAlignment.Bottom };
        using var scene = new Scene(new Grid { Children = { combo, below } });
        clock.At(200);
        Dispatcher.UIThread.RunJobs();
        Assert.True(PhoneComboBox.GetIsShownOpen(combo));
        var at = below.TranslatePoint(new Point(5, 5), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(combo.IsDropDownOpen);
    }

    // The count crossing 6 while open closes it: neither form is left half open.
    [AvaloniaFact]
    public void A_Sixth_Item_Added_While_Open_Closes_It()
    {
        using var stage = new Stage(5);
        stage.OpenAtRest();
        stage.Combo.Items.Add("item 6");
        stage.Layout();
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.False(PhoneComboBox.GetIsShownOpen(stage.Combo));
        Assert.True(PhoneComboBox.GetIsFull(stage.Combo));
    }

    // Not stretched, the box fits its widest item.
    [AvaloniaFact]
    public void Not_Stretched_The_Box_Fits_Its_Items()
    {
        using var stage = new Stage(0);
        stage.Combo.HorizontalAlignment = HorizontalAlignment.Left;
        stage.Combo.Items.Add("an item far longer than the header");
        stage.Combo.SelectedIndex = 0;
        stage.Layout();
        var text = stage.TextOf(stage.Items[0]);
        Assert.True(stage.Box.Bounds.Width >= text.Bounds.Width + 6.5 + 5, $"the box is {stage.Box.Bounds.Width}, the text {text.Bounds.Width}");
    }

    // Turned off while open, it lets go: the inline box shows closed, and the picker's close, still running, leaves the box
    // to the behaviour turned on again.
    [AvaloniaFact]
    public void Turned_Off_While_Open_It_Lets_Go()
    {
        using var inline = new Stage(5);
        inline.OpenAtRest();
        PhoneComboBox.SetIsEnabled(inline.Combo, false);
        Assert.False(PhoneComboBox.GetIsShownOpen(inline.Combo));
        Assert.False(PhoneComboBox.GetIsFull(inline.Combo));
        inline.Dispose();

        using var full = new Stage(6);
        full.Open();
        full.At(full.Now + 600);
        var picker = PhoneComboBox.PickerOf(full.Combo)!;
        PhoneComboBox.SetIsEnabled(full.Combo, false);
        full.Combo.ClearValue(PhoneComboBox.IsEnabledProperty);   // the theme's again: a new behaviour, the box still open
        full.At(full.Now + 600);
        Assert.False(picker.IsOpen);
        Assert.True(full.Combo.IsDropDownOpen);
        Assert.True(PhoneComboBox.PickerOf(full.Combo)!.IsOpen);
    }

    // A Button just below the open box: the tap outside reaches it before the box closes and moves it up.
    [AvaloniaFact]
    public void A_Tap_On_A_Button_Just_Below_The_Open_Box_Clicks_It()
    {
        using var clock = new TestClock();
        var combo = new ComboBox { Items = { "a", "b", "c", "d", "e" }, SelectedIndex = 0 };
        var below = new Button { Content = "below" };
        var clicked = 0;
        below.Click += (_, _) => clicked++;
        using var scene = new Scene(new StackPanel { Children = { combo, below } });
        combo.IsDropDownOpen = true;
        clock.At(200);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var at = below.TranslatePoint(new Point(20, below.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicked);
        Assert.False(combo.IsDropDownOpen);
    }

    // The items changing under the open picker close it with no pick: its rows are a copy.
    [AvaloniaFact]
    public void Full_The_Items_Changing_Under_The_Picker_Close_It()
    {
        using var stage = new Stage(7, selected: 1);
        stage.Open();
        stage.At(stage.Now + 600);
        stage.Combo.Items.Insert(0, "new");
        stage.At(stage.Now + 600);
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.False(PhoneComboBox.PickerOf(stage.Combo)!.IsOpen);
        Assert.Equal("item 2", stage.Combo.SelectedItem);
    }

    // A ComboBox in an item of another doesn't show open with it.
    [AvaloniaFact]
    public void A_ComboBox_Inside_An_Open_One_Doesnt_Show_Open()
    {
        using var stage = new Stage(0);
        var inner = new ComboBox { Items = { "x", "y" }, SelectedIndex = 0 };
        stage.Combo.Items.Add(new ComboBoxItem { Content = inner });
        stage.Combo.Items.Add("two");
        stage.Combo.SelectedIndex = 1;
        stage.OpenAtRest();
        Assert.True(PhoneComboBox.GetIsShownOpen(stage.Combo));
        Assert.False(PhoneComboBox.GetIsShownOpen(inner));
    }

    // Taken out of the window while open, it closes (Avalonia's own popup closes it; the box shows closed).
    [AvaloniaFact]
    public void Taken_Out_Of_The_Window_Open_It_Closes()
    {
        using var stage = new Stage(5);
        stage.OpenAtRest();
        ((Panel)stage.Combo.Parent!).Children.Remove(stage.Combo);
        Dispatcher.UIThread.RunJobs();
        Assert.False(stage.Combo.IsDropDownOpen);
        Assert.False(PhoneComboBox.GetIsShownOpen(stage.Combo));
    }
}
