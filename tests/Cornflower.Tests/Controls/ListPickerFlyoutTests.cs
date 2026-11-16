using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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
using static Cornflower.Tests.Controls.ListPickerFlyoutReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's ListPickerFlyout and PickerFlyout (Cornflower's, <c>Controls/Pickers/</c>), Wp81's themes for their
/// presenters (<c>Controls/ListPickerFlyoutPresenter.axaml</c>, <c>Controls/PickerFlyoutPresenter.axaml</c>), the
/// pickers' bar (<c>Controls/PickerBar.axaml</c>) and the popup round them (<see cref="PhonePickerFlyout"/>), against
/// the emulator's dumps, grabs and probes (<see cref="ListPickerFlyoutReadings"/>). docs/controls/ListPickerFlyout/PLAN.md
/// has each reading.
/// </summary>
public class ListPickerFlyoutTests
{
    /// <summary>The reference app's items.</summary>
    internal static readonly string[] Items = ["one", "two", "three", "four", "five"];

    /// <summary>The reference app's stage: a page with the "open" Button, a picker shown on it.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(PickerFlyoutBase picker, ThemeVariant? variant = null, double width = Scene.ReferenceWidth,
            double height = Scene.ReferenceHeight, IEnumerable<IStyle>? themes = null, Control? root = null)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Picker = picker;
            Button = new Button
            {
                Content = "open",
                Margin = new Thickness(19, 377.08, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            try
            {
                Scene = new Scene(root ?? new Panel { Children = { Button } }, variant, width, height, themes: themes);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public PickerFlyoutBase Picker { get; }

        public Button Button { get; }

        public Scene Scene { get; }

        public PickerPresenterBase Presenter => (PickerPresenterBase)Picker.Popup.Child!;

        public PhonePopupBands? Bands => PhonePickerFlyout.HostOf(Presenter)?.Bands;

        public double Now => Clock.Now.TotalMilliseconds;

        /// <summary>The motion's first frame: the next tick after the call.</summary>
        public double FirstFrameAfter(double call) => (Math.Floor(call / Tick) + 1) * Tick;

        public void Open()
        {
            Picker.ShowAt(Button);
            Dispatcher.UIThread.RunJobs();
        }

        public void OpenAtRest()
        {
            Open();
            At(Now + 500);
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        public T Part<T>(string name) where T : Control =>
            Presenter.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        /// <summary>Where a part is laid out in the window, without the motion.</summary>
        public Rect InWindow(Visual part)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var at = part.TranslatePoint(default, Scene.Window)!.Value;
            return new Rect(at, part.Bounds.Size);
        }

        public IReadOnlyList<ListBoxItem> ListItems() =>
            Part<ListBox>("PART_ItemsHost").GetVisualDescendants().OfType<ListBoxItem>().ToList();

        /// <summary>A mouse press and release at <paramref name="at"/>, the jobs run after each.</summary>
        public void Tap(Point at)
        {
            Scene.Window.MouseDown(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Scene.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        public void Tap(Visual control) => Tap(CentreOf(control));

        public Point CentreOf(Visual control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Scene.Window)!.Value;

        public void Back()
        {
            Scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    internal static ListPickerFlyout ListPicker(ListPickerFlyoutSelectionMode mode = ListPickerFlyoutSelectionMode.Single,
        string? title = "LIST PICKER")
    {
        var picker = new ListPickerFlyout { ItemsSource = Items, SelectionMode = mode };
        PickerFlyoutBase.SetTitle(picker, title);
        return picker;
    }

    internal static PickerFlyout Picker(bool buttons = true) => new()
    {
        Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) },
        ConfirmationButtonsVisible = buttons,
    };

    // ---- The presenters and where things are ----

    [AvaloniaFact]
    public void The_Presenters_Are_Templated_By_The_Theme_And_Hosted_By_The_Phones_Popup()
    {
        foreach (PickerFlyoutBase picker in new PickerFlyoutBase[] { ListPicker(), Picker() })
        {
            using var stage = new Stage(picker);
            stage.OpenAtRest();

            var theme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(stage.Presenter.GetType()));
            Assert.Same(theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value,
                stage.Presenter.Template);
            Assert.True(PhonePickerFlyout.GetIsEnabled(stage.Presenter));
            Assert.NotNull(PhonePickerFlyout.HostOf(stage.Presenter));
            Assert.True(stage.Picker.Popup.ShouldUseOverlayLayer);
            // Nothing outside it to tap: Back and its own buttons close it.
            Assert.False(stage.Picker.Popup.IsLightDismissEnabled);
            Assert.Same(stage.Scene.Window, TopLevel.GetTopLevel(stage.Presenter));
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Picker_Fills_The_Window_In_The_Chrome(bool dark)
    {
        using var stage = new Stage(ListPicker(), dark ? ThemeVariant.Dark : ThemeVariant.Light);
        stage.OpenAtRest();

        Assert.Equal(Screen, stage.InWindow(stage.Presenter));
        using var frame = stage.Scene.Capture();
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1000, 5));
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1000, 1915));
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 5, 1000));
    }

    [AvaloniaFact]
    public void The_Title_Is_The_Pickers_Title_Style_At_The_Content_Panels_Top_Left()
    {
        using var stage = new Stage(ListPicker());
        stage.OpenAtRest();

        var panel = stage.Part<Grid>("ContentPanel");
        // 411.67 wide to the phone's 412.08: layout rounds each margin up to the raw grid (ADR 0003).
        Assert.Equal(ContentPanel, stage.InWindow(panel), new RectComparer(1 / Scene.ReferenceScaling + 0.001));
        var title = stage.Part<TextBlock>("TitlePresenter");
        Assert.Equal("LIST PICKER", title.Text);
        Assert.True(title.IsVisible);
        Assert.Same(stage.Scene.Window.FindResource("FlyoutPickerTitleTextBlockStyle"), title.Theme);
        Assert.Equal(18.14, title.FontSize, 0.001);
        Assert.Equal(FontWeight.SemiBold, title.FontWeight);
        Assert.Equal(Title, stage.InWindow(title).Position, new PointComparer(0.001));
        Assert.Same(stage.Scene.Window.FindResource(ThemeVariant.Dark, "PhoneForegroundBrush"), title.Foreground);
    }

    [AvaloniaFact]
    public void The_Items_Are_Listed_Below_The_Title_58_33_Apart_In_The_Large_SemiLight_Text()
    {
        using var stage = new Stage(ListPicker());
        stage.OpenAtRest();

        var list = stage.Part<ListBox>("PART_ItemsHost");
        Assert.Equal(ItemsHost, stage.InWindow(list), new RectComparer(1 / Scene.ReferenceScaling + 0.001));
        var items = stage.ListItems();
        Assert.Equal(Items.Length, items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var bounds = stage.InWindow(items[i]);
            Assert.Equal(ItemsHost.X, bounds.X, 0.001);
            Assert.Equal(ItemsHost.Y + i * ItemPitch, bounds.Y, 0.01);
            Assert.Equal(ItemHeight, bounds.Height, 0.01);
            Assert.Equal(ItemFontSize, items[i].FontSize);
            Assert.Equal(stage.Scene.Window.FindResource("PhoneFontFamilySemiLight"), items[i].FontFamily);
        }
    }

    [AvaloniaFact]
    public void In_Single_There_Are_No_Check_Boxes_No_Earmark_And_No_Bar()
    {
        var picker = ListPicker();
        picker.SelectedIndex = 1;
        using var stage = new Stage(picker);
        stage.OpenAtRest();

        Assert.False(stage.Part<CommandBar>("PickerBar").IsVisible);
        Assert.Equal(ContentPanel.Height, stage.Part<Grid>("ContentPanel").Bounds.Height, 0.001);
        var text = stage.ListItems()[1].GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "two");
        Assert.Equal(ItemsHost.X, stage.InWindow(text).X, 0.001);
        Assert.False(PhoneListMultiSelect.GetIsOn(stage.Part<ListBox>("PART_ItemsHost")));
        using var frame = stage.Scene.Capture();
        // The selected item's earmark corner is the chrome: Single draws no selection.
        Assert.Equal(Chrome(dark: true), Frames.PixelAt(frame, (int)(428 * 2.4), (int)(156 * 2.4)));
    }

    [AvaloniaFact]
    public void In_Multiple_Each_Item_Has_A_Check_Box_Its_Text_Beside_It_And_The_Bar_Is_Shown()
    {
        var picker = ListPicker(ListPickerFlyoutSelectionMode.Multiple);
        picker.SelectedItems.Add("two");
        picker.SelectedItems.Add("four");
        using var stage = new Stage(picker);
        stage.OpenAtRest();

        Assert.True(stage.Part<CommandBar>("PickerBar").IsVisible);
        Assert.Equal(ContentPanelWithBarHeight, stage.Part<Grid>("ContentPanel").Bounds.Height, 0.01);
        var items = stage.ListItems();
        var box = items[0].GetVisualDescendants().OfType<Rectangle>().First(r => r.Name == "NormalRectangle");
        Assert.Equal(FirstCheckBox.Position, stage.InWindow(box).Position, new PointComparer(0.01));
        Assert.Equal(FirstCheckBox.Width, box.Bounds.Width, 1 / Scene.ReferenceScaling);
        var text = items[0].GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "one");
        Assert.Equal(MultipleTextLeft, text.TranslatePoint(default, stage.Scene.Window)!.Value.X, 0.01);
        var ticks = items.Select(i => i.GetVisualDescendants().OfType<Path>().First(p => p.Name == "CheckGlyph").Opacity).ToList();
        Assert.Equal([0d, 1, 0, 1, 0], ticks);
    }

    [AvaloniaFact]
    public void A_PickerFlyouts_Content_Is_Below_Its_Title_And_With_No_Title_At_The_Panels_Top()
    {
        using var stage = new Stage(Picker());
        stage.OpenAtRest();

        var title = stage.Part<TextBlock>("TitlePresenter");
        Assert.False(title.IsVisible);
        Assert.Same(stage.Scene.Window.FindResource("TitleTextBlockStyle"), title.Theme);
        var text = stage.Presenter.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "picker flyout content");
        // The dump: the content's TextBlock, with its 19 margin, at 38.33, 56.25.
        Assert.Equal(new Point(38.3333, 56.25), stage.InWindow(text).Position, new PointComparer(0.01));
        Assert.Equal(ContentPanelWithBarHeight, stage.Part<Grid>("ContentPanel").Bounds.Height, 0.01);

        stage.Picker.Hide();
        stage.At(stage.Now + 500);
        PickerFlyoutBase.SetTitle(stage.Picker, "PICKER");
        stage.OpenAtRest();
        Assert.True(title.IsVisible);
        Assert.Equal("PICKER", title.Text);
        Assert.Equal(Title.Y + 23.63 + 32.5, stage.InWindow(text).Y - 19, 1.5);
    }

    [AvaloniaFact]
    public void A_PickerFlyout_Without_Its_Buttons_Has_No_Bar_And_Its_Content_Goes_To_The_Foot()
    {
        using var stage = new Stage(Picker(buttons: false));
        stage.OpenAtRest();

        Assert.False(stage.Part<CommandBar>("PickerBar").IsVisible);
        Assert.Equal(ContentPanel.Height, stage.Part<Grid>("ContentPanel").Bounds.Height, 0.001);
    }

    [AvaloniaFact]
    public void In_Landscape_The_Content_Panel_Takes_The_Landscape_Margin()
    {
        using var stage = new Stage(ListPicker(), width: 800, height: 450);
        stage.OpenAtRest();

        Assert.True(PhonePopupHost.GetIsLandscape(stage.Presenter));
        Assert.Equal(new Thickness(19, 19, 19, 0), stage.Part<Grid>("ContentPanel").Margin);
        Assert.Equal(new Rect(0, 0, 800, 450), stage.InWindow(stage.Presenter));
    }

    // ---- The pickers' bar ----

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Bar_Is_The_Pages_Compact_App_Bar_With_Accept_And_Cancel_Where_The_Shells_Are(bool dark)
    {
        using var stage = new Stage(Picker(), dark ? ThemeVariant.Dark : ThemeVariant.Light);
        stage.OpenAtRest();

        var bar = stage.Part<CommandBar>("PickerBar");
        var bounds = stage.InWindow(bar);
        Assert.Equal(BarTop, bounds.Y, 0.01);
        Assert.Equal(BarHeight, bounds.Height, 0.01);
        Assert.Equal(AppBarClosedDisplayMode.Compact, CommandBarOptions.GetClosedDisplayMode(bar));
        Assert.False(bar.IsOpen);
        var accept = stage.Part<CommandBarButton>("PART_AcceptButton");
        var cancel = stage.Part<CommandBarButton>("PART_DismissButton");
        Assert.Equal("done", accept.Label);
        Assert.Equal("cancel", cancel.Label);
        foreach (var (button, centre) in new[] { (accept, AcceptCentre), (cancel, CancelCentre) })
        {
            var ring = button.GetVisualDescendants().OfType<Ellipse>().Single(e => e.Name == "PART_Ellipse");
            var at = stage.CentreOf(ring);
            // The CommandBar's: a raw px left of the shell's centres, 0.43 above.
            Assert.Equal(centre.X, at.X, 1 / Scene.ReferenceScaling + 0.01);
            Assert.Equal(centre.Y, at.Y, 0.5);
            Assert.Equal(RingDiameter, ring.Bounds.Width, 0.06);
        }
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Accept_And_Cancel_Glyphs_Ink_The_Shells_Boxes_At_Their_Places(bool dark)
    {
        using var stage = new Stage(Picker(), dark ? ThemeVariant.Dark : ThemeVariant.Light);
        stage.OpenAtRest();
        using var frame = stage.Scene.Capture();

        // From each ring's own centre: the bar puts the rings a raw px from the shell's (above).
        var acceptRing = stage.CentreOf(stage.Part<CommandBarButton>("PART_AcceptButton").GetVisualDescendants().OfType<Ellipse>().Single());
        var cancelRing = stage.CentreOf(stage.Part<CommandBarButton>("PART_DismissButton").GetVisualDescendants().OfType<Ellipse>().Single());
        var check = InkBox(frame, acceptRing, Foreground(dark), Chrome(dark));
        // The check: as wide, 1.1 shorter (its round ends and corner), centred within 0.4 (the phone's 0.13).
        Assert.Equal(CheckInk.Width, check.Width, 0.5);
        Assert.Equal(CheckInk.Height, check.Height, 1.25);
        Assert.Equal(acceptRing.X, check.Center.X, 0.4);
        Assert.Equal(acceptRing.Y, check.Center.Y, 0.4);
        var cross = InkBox(frame, cancelRing, Foreground(dark), Chrome(dark));
        Assert.Equal(CrossInk.Width, cross.Width, 0.6);
        Assert.Equal(CrossInk.Height, cross.Height, 0.6);
        Assert.Equal(cancelRing.X + CrossOffset.X, cross.Center.X, 0.5);
        Assert.Equal(cancelRing.Y + CrossOffset.Y, cross.Center.Y, 0.5);
    }

    // ---- Picking, accepting, cancelling and Back ----

    /// <summary>What a picker raised, in order, with the clock's time.</summary>
    private sealed class Log
    {
        private readonly Stage _stage;

        public Log(Stage stage)
        {
            _stage = stage;
            stage.Picker.Opening += (_, _) => Add("Opening");
            stage.Picker.Opened += (_, _) => Add("Opened");
            stage.Picker.Closed += (_, _) => Add("Closed");
            if (stage.Picker is ListPickerFlyout list)
                list.ItemsPicked += (_, e) => Add($"ItemsPicked +[{string.Join(",", e.AddedItems)}] -[{string.Join(",", e.RemovedItems)}]");
            if (stage.Picker is PickerFlyout picker)
                picker.Confirmed += (_, _) => Add("Confirmed");
        }

        public List<(string Event, double At)> Events { get; } = [];

        public void Add(string name) => Events.Add((name, _stage.Now));

        public IEnumerable<string> Names => Events.Select(e => e.Event);

        public double At(string name) => Events.Single(e => e.Event.StartsWith(name, StringComparison.Ordinal)).At;
    }

    [AvaloniaFact]
    public void Opening_And_Opened_Come_In_The_Calls_Frame()
    {
        using var stage = new Stage(ListPicker());
        var log = new Log(stage);
        var at = stage.Now;

        stage.Open();

        Assert.Equal(["Opening", "Opened"], log.Names);
        Assert.All(log.Events, e => Assert.Equal(at, e.At));
    }

    [AvaloniaFact]
    public void In_Single_A_Tap_On_An_Item_Picks_It_The_Result_And_ItemsPicked_At_Once_And_Closed_After_The_Motion()
    {
        var picker = ListPicker();
        picker.SelectedIndex = 1;
        using var stage = new Stage(picker);
        var log = new Log(stage);
        var result = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);
        var tap = stage.Now;

        stage.Tap(stage.ListItems()[3]);

        // As the probe read it: added "four", removed "two", SelectedIndex 3, the result [four], in the tap's frame.
        Assert.Equal("ItemsPicked +[four] -[two]", log.Names.Last());
        Assert.Equal(tap, log.At("ItemsPicked"));
        Assert.True(result.IsCompleted);
        Assert.Equal(["four"], result.Result!);
        Assert.Equal(3, picker.SelectedIndex);
        Assert.Equal("four", picker.SelectedItem);
        Assert.Equal(["four"], picker.SelectedItems);
        // Closed when the closing motion has run: the full popup's, 22 ticks (the phone's list picker: 447 ms).
        Assert.True(picker.Popup.IsOpen);
        stage.At(stage.FirstFrameAfter(tap) + 22 * Tick);
        Assert.False(picker.Popup.IsOpen);
        Assert.Equal("Closed", log.Names.Last());
    }

    [AvaloniaFact]
    public void In_Multiple_A_Tap_Checks_An_Item_And_Raises_Nothing_And_Accept_Picks_Those_Checked()
    {
        var picker = ListPicker(ListPickerFlyoutSelectionMode.Multiple);
        picker.SelectedItems.Add("two");
        using var stage = new Stage(picker);
        var log = new Log(stage);
        var result = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);

        var items = stage.ListItems();
        stage.Tap(items[0]);
        stage.Tap(items[1]);
        Assert.Equal(["Opening", "Opened"], log.Names);
        Assert.True(items[0].IsSelected);
        Assert.False(items[1].IsSelected);
        Assert.Equal(["two"], picker.SelectedItems);

        var accept = stage.Now;
        stage.Tap(stage.Part<CommandBarButton>("PART_AcceptButton"));

        // The probe: ticking "one" and unticking "two" gave added [one], removed [two], SelectedItems [one],
        // SelectedIndex 0, and the result [one], in one frame.
        Assert.Equal("ItemsPicked +[one] -[two]", log.Names.Last());
        Assert.Equal(accept, log.At("ItemsPicked"));
        Assert.True(result.IsCompleted);
        Assert.Equal(["one"], result.Result!);
        Assert.Equal(["one"], picker.SelectedItems);
        Assert.Equal(0, picker.SelectedIndex);
        Assert.Equal("one", picker.SelectedItem);
        stage.At(stage.FirstFrameAfter(accept) + 22 * Tick);
        Assert.False(picker.Popup.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData("cancel")]
    [InlineData("back")]
    [InlineData("hide")]
    public void Cancel_Back_And_Hide_Close_With_No_Pick_The_Result_Null_In_Closeds_Frame(string how)
    {
        var picker = ListPicker(ListPickerFlyoutSelectionMode.Multiple);
        picker.SelectedItems.Add("two");
        using var stage = new Stage(picker);
        var log = new Log(stage);
        var result = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);
        stage.Tap(stage.ListItems()[0]);

        var at = stage.Now;
        switch (how)
        {
            case "cancel":
                stage.Tap(stage.Part<CommandBarButton>("PART_DismissButton"));
                break;
            case "back":
                stage.Back();
                break;
            default:
                picker.Hide();
                Dispatcher.UIThread.RunJobs();
                break;
        }

        Assert.False(result.IsCompleted);
        stage.At(stage.FirstFrameAfter(at) + 22 * Tick);
        Assert.False(picker.Popup.IsOpen);
        Assert.True(result.IsCompleted);
        Assert.Null(result.Result);
        Assert.DoesNotContain(log.Names, n => n.StartsWith("ItemsPicked", StringComparison.Ordinal));
        Assert.Equal(["two"], picker.SelectedItems);
        Assert.Equal(1, picker.SelectedIndex);
    }

    [AvaloniaFact]
    public void A_Picker_Opened_Again_Shows_What_Was_Picked_Checked()
    {
        var picker = ListPicker(ListPickerFlyoutSelectionMode.Multiple);
        using var stage = new Stage(picker);
        stage.OpenAtRest();
        stage.Tap(stage.ListItems()[2]);
        stage.Tap(stage.Part<CommandBarButton>("PART_AcceptButton"));
        stage.At(stage.Now + 500);
        Assert.False(picker.Popup.IsOpen);

        stage.OpenAtRest();

        Assert.Equal([false, false, true, false, false], stage.ListItems().Select(i => i.IsSelected));
    }

    [AvaloniaTheory]
    [InlineData("accept")]
    [InlineData("cancel")]
    [InlineData("back")]
    public void PickerFlyouts_Accept_Raises_Confirmed_With_True_At_Once_And_Cancel_And_Back_Give_False(string how)
    {
        var picker = Picker();
        using var stage = new Stage(picker);
        var log = new Log(stage);
        var result = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);
        var at = stage.Now;

        if (how == "back")
            stage.Back();
        else
            stage.Tap(stage.Part<CommandBarButton>(how == "accept" ? "PART_AcceptButton" : "PART_DismissButton"));

        if (how == "accept")
        {
            Assert.Equal("Confirmed", log.Names.Last());
            Assert.Equal(at, log.At("Confirmed"));
            Assert.True(result.IsCompleted);
            Assert.True(result.Result);
        }
        else
        {
            Assert.False(result.IsCompleted);
        }

        stage.At(stage.FirstFrameAfter(at) + 22 * Tick);
        Assert.False(picker.Popup.IsOpen);
        Assert.True(result.IsCompleted);
        Assert.Equal(how == "accept", result.Result);
        Assert.Equal(how == "accept", log.Names.Contains("Confirmed"));
    }

    [AvaloniaFact]
    public void ShowAtAsync_Gives_Its_Empty_Result_At_Once_When_The_Picker_Doesnt_Open()
    {
        var list = new RefusingListPicker { ItemsSource = Items };
        var picker = new RefusingPicker();
        using var stage = new Stage(list);

        var listed = list.ShowAtAsync(stage.Button);
        var picked = picker.ShowAtAsync(stage.Button);

        Assert.True(listed.IsCompleted);
        Assert.Null(listed.Result);
        Assert.True(picked.IsCompleted);
        Assert.False(picked.Result);
    }

    [AvaloniaFact]
    public void A_Second_ShowAtAsync_Ends_The_First_With_No_Result()
    {
        var picker = ListPicker();
        using var stage = new Stage(picker);
        var first = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();

        var second = picker.ShowAtAsync(stage.Button);

        Assert.True(first.IsCompleted);
        Assert.Null(first.Result);
        Assert.False(second.IsCompleted);
    }

    private sealed class RefusingListPicker : ListPickerFlyout
    {
        protected override void OnOpening(System.ComponentModel.CancelEventArgs args) => args.Cancel = true;
    }

    private sealed class RefusingPicker : PickerFlyout
    {
        protected override void OnOpening(System.ComponentModel.CancelEventArgs args) => args.Cancel = true;
    }

    // ---- The list's data ----

    [AvaloniaFact]
    public void SelectedIndex_SelectedItem_And_SelectedItems_Follow_One_Another()
    {
        var picker = ListPicker();

        picker.SelectedIndex = 2;
        Assert.Equal("three", picker.SelectedItem);
        Assert.Equal(["three"], picker.SelectedItems);

        picker.SelectedItem = "five";
        Assert.Equal(4, picker.SelectedIndex);
        Assert.Equal(["five"], picker.SelectedItems);

        picker.SelectedItems.Clear();
        Assert.Equal(-1, picker.SelectedIndex);
        Assert.Null(picker.SelectedItem);

        picker.SelectedItems.Add("four");
        picker.SelectedItems.Add("one");
        Assert.Equal(3, picker.SelectedIndex);
        Assert.Equal("four", picker.SelectedItem);
    }

    [AvaloniaFact]
    public void The_Items_Are_Shown_By_Their_ItemTemplate_Or_DisplayMemberBinding()
    {
        var people = new[] { new Person("Ann"), new Person("Bo") };
        var shown = new ListPickerFlyout { ItemsSource = people, DisplayMemberBinding = new Binding(nameof(Person.Name)) };
        using (var stage = new Stage(shown))
        {
            stage.OpenAtRest();
            Assert.Contains(stage.ListItems()[1].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Bo");
        }

        var templated = new ListPickerFlyout
        {
            ItemsSource = people,
            ItemTemplate = new FuncDataTemplate<Person>((p, _) => new TextBlock { Text = p.Name.ToUpperInvariant() }),
        };
        using (var stage = new Stage(templated))
        {
            stage.OpenAtRest();
            Assert.Contains(stage.ListItems()[0].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "ANN");
        }
    }

    public sealed record Person(string Name);

    // ---- The motion ----

    [AvaloniaFact]
    public void A_Picker_Opens_And_Closes_As_The_Full_Flyout_Does()
    {
        using var stage = new Stage(ListPicker());
        stage.Open();
        var first = stage.FirstFrameAfter(0);

        stage.At(first);
        var bands = Assert.IsType<PhonePopupBands>(stage.Bands);
        Assert.Equal(8, bands.Bands.Count);
        Assert.All(bands.Angles.Take(4), a => Assert.Equal(80, a, 1e-9));
        stage.At(first + 6 * Tick);
        Assert.Equal(80 - 12.61 * 6, stage.Bands!.Angles[0], 1e-9);

        stage.At(stage.Now + 500);
        Assert.Null(stage.Bands);
        var hidden = stage.Now;
        stage.Picker.Hide();
        stage.At(stage.FirstFrameAfter(hidden) + 4 * Tick);
        Assert.Equal(-10.95 * 4, stage.Bands!.Angles[0], 1e-9);
        Assert.Equal(-10.95 * 2, stage.Bands.Angles[1], 1e-9);
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own_Popup_And_The_Picker_Still_Closes()
    {
        var picker = ListPicker();
        using var stage = new Stage(picker, themes: [new SimpleTheme()]);
        var result = picker.ShowAtAsync(stage.Button);
        Dispatcher.UIThread.RunJobs();

        Assert.False(PhonePickerFlyout.GetIsEnabled(stage.Presenter));
        Assert.Null(PhonePickerFlyout.HostOf(stage.Presenter));
        picker.Hide();
        Assert.False(picker.Popup.IsOpen);
        Assert.True(result.IsCompleted);
        Assert.Null(result.Result);
    }

    [AvaloniaFact]
    public void A_Picker_Whose_Theme_Goes_Mid_Close_Closes()
    {
        using var stage = new Stage(ListPicker());
        stage.OpenAtRest();

        stage.Picker.Hide();
        Assert.True(stage.Picker.Popup.IsOpen);
        PhonePickerFlyout.SetIsEnabled(stage.Presenter, false);
        Dispatcher.UIThread.RunJobs();

        Assert.False(stage.Picker.Popup.IsOpen);
        Assert.False(stage.Picker.IsOpen);
    }

    /// <summary>
    /// The box of the ink at half way from <paramref name="chrome"/> to <paramref name="ink"/> inside the ring about
    /// <paramref name="centre"/> (within 15 view px of it, clear of the ring), in view px.
    /// </summary>
    internal static Rect InkBox(Avalonia.Media.Imaging.WriteableBitmap frame, Point centre, Color ink, Color chrome)
    {
        const double scale = Scene.ReferenceScaling;
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        var cx = centre.X * scale;
        var cy = centre.Y * scale;
        var r = 15 * scale;
        for (var y = (int)(cy - r); y <= (int)(cy + r); y++)
        for (var x = (int)(cx - r); x <= (int)(cx + r); x++)
        {
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r)
                continue;
            var p = Frames.PixelAt(frame, x, y);
            var amount = Math.Abs(p.R - chrome.R) / (double)Math.Max(1, Math.Abs(ink.R - chrome.R));
            if (amount < 0.5)
                continue;
            left = Math.Min(left, x);
            right = Math.Max(right, x);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);
        }

        return new Rect(left / scale, top / scale, (right - left + 1) / scale, (bottom - top + 1) / scale);
    }
}

internal sealed class RectComparer(double tolerance) : IEqualityComparer<Rect>
{
    public bool Equals(Rect a, Rect b) =>
        Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance
        && Math.Abs(a.Width - b.Width) <= tolerance && Math.Abs(a.Height - b.Height) <= tolerance;

    public int GetHashCode(Rect rect) => 0;
}

internal sealed class PointComparer(double tolerance) : IEqualityComparer<Point>
{
    public bool Equals(Point a, Point b) => Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance;

    public int GetHashCode(Point point) => 0;
}
