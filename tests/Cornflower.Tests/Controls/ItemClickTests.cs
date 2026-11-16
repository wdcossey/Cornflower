using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.ListViewReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// WinRT's <c>IsItemClickEnabled</c> and <c>ItemClick</c> on Avalonia's ListBox (<see cref="ListBoxOptions"/>), against
/// #113's probes of a ListView with item click on, runs <c>20261115-1941-dev11</c> (12 items) and
/// <c>20261115-1948-dev11</c> (40, so the list pans), in the phone's default <c>SelectionMode</c>, Single: each of the
/// 15 taps and held presses in each run raised one <c>ItemClick</c>, with the release, for the item under the finger,
/// and none raised <c>SelectionChanged</c> or changed an item's <c>SelectionStates</c>; a pan and a press dragged off
/// the item raised neither. docs/controls/ItemClick has the plan and the comparison.
/// </summary>
public class ItemClickTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>The probes' page: a ListView with item click on, 19 in and 80 down the 450 × 800 view, its items strings.</summary>
    private sealed class ClickPage : IDisposable
    {
        private readonly Scene _scene;

        public ClickPage(int count = 12, bool enabled = true, IEnumerable<Avalonia.Styling.IStyle>? themes = null)
        {
            List = new ListBox
            {
                Margin = ProbeListMargin,
                ItemsSource = Enumerable.Range(0, count).Select(i => $"item {i}").ToList(),
            };
            ListBoxOptions.SetIsItemClickEnabled(List, enabled);
            List.AddHandler(ListBoxOptions.ItemClickEvent, (sender, e) =>
            {
                Assert.Same(List, sender);
                Assert.Same(List, e.Source);
                Clicked.Add(e.ClickedItem);
            });
            List.SelectionChanged += (_, _) => SelectionChanges++;
            _scene = new Scene(List, themes: themes);
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public List<object?> Clicked { get; } = [];

        public int SelectionChanges { get; private set; }

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        /// <summary>A point of an item, in the window.</summary>
        public Point On(int index, double x = 200, double y = 20) =>
            Item(index).TranslatePoint(new Point(x, y), Window)!.Value;

        public void Tap(Point at)
        {
            var touch = Window.TouchBegin(at);
            Window.TouchEnd(touch, at);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => _scene.Dispose();
    }

    // ----- A tap -----

    // The probes: a tap (80 ms) presses and tilts the item, and its release raises ItemClick with the item, as the
    // phone's came with the release; nothing is selected.
    [AvaloniaFact]
    public void A_Tap_Tilts_Then_Clicks_The_Item_On_Its_Release_And_Selects_Nothing()
    {
        using var clock = new TestClock();
        using var page = new ClickPage();
        var item = page.Item(4);
        var at = Presses[^1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(80);
        Assert.Contains(":pressed", item.Classes);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(item));
        Assert.Empty(page.Clicked);
        page.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["item 4"], page.Clicked);
        Assert.DoesNotContain(":pressed", item.Classes);
        Assert.Equal(-1, page.List.SelectedIndex);
        Assert.False(item.IsSelected);
        Assert.Equal(0, page.SelectionChanges);
        clock.At(clock.Now.TotalMilliseconds + 400);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // Each of the probes' held presses (centres, 5% to 95% across, 10% and 90% down) clicks the item under the finger on
    // its release, and none selects.
    public static TheoryData<int> PressIndexes => new(Enumerable.Range(0, Presses.Length));

    [AvaloniaTheory]
    [MemberData(nameof(PressIndexes))]
    public void Each_Probed_Press_Clicks_Its_Item_And_Selects_Nothing(int index)
    {
        var (_, press, itemIndex, _) = Presses[index];
        using var clock = new TestClock();
        using var page = new ClickPage();

        var touch = page.Window.TouchBegin(press);
        clock.At(600);
        page.Window.TouchEnd(touch, press);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([$"item {itemIndex}"], page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // Off, as WinRT's IsItemClickEnabled is by default, a tap selects and clicks nothing: Avalonia's ListBox as before.
    [AvaloniaFact]
    public void Off_A_Tap_Selects_And_Clicks_Nothing()
    {
        using var page = new ClickPage(enabled: false);

        page.Tap(page.On(2));

        Assert.Empty(page.Clicked);
        Assert.Equal(2, page.List.SelectedIndex);
    }

    // Turned off again, the list selects again and clicks nothing.
    [AvaloniaFact]
    public void Turned_Off_The_List_Selects_Again()
    {
        using var page = new ClickPage();
        page.Tap(page.On(1));
        ListBoxOptions.SetIsItemClickEnabled(page.List, false);

        page.Tap(page.On(2));

        Assert.Equal(["item 1"], page.Clicked);
        Assert.Equal(2, page.List.SelectedIndex);
    }

    // A selection set in code stands: a tap on another item clicks it and leaves the selection as it was.
    [AvaloniaFact]
    public void A_Selection_Set_In_Code_Stands()
    {
        using var page = new ClickPage();
        page.List.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();

        page.Tap(page.On(1));
        page.Tap(page.On(3));

        Assert.Equal(["item 1", "item 3"], page.Clicked);
        Assert.Equal(3, page.List.SelectedIndex);
        Assert.Equal(1, page.SelectionChanges);
    }

    // ----- What isn't a tap -----

    // The 40-item probe: a press held 300 ms and dragged up pans the list, which takes the pointer from the item; no
    // ItemClick, nothing selected.
    [AvaloniaFact]
    public void A_Pan_Clicks_Nothing()
    {
        using var clock = new TestClock();
        using var page = new ClickPage(40);
        var item = page.Item(3);
        var at = Presses[2].Press;
        var lost = false;
        item.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => lost = true, RoutingStrategies.Direct, handledEventsToo: true);

        var touch = page.Window.TouchBegin(at);
        clock.At(300);
        for (var step = 1; step <= 8; step++)
        {
            page.Window.TouchMove(touch, at - new Point(0, 10 * step));
            clock.At(300 + 25 * step);
        }

        page.Window.TouchEnd(touch, at - new Point(0, 80));
        Dispatcher.UIThread.RunJobs();

        Assert.True(lost, "the pan didn't take the pointer");
        Assert.True(page.List.Scroll!.Offset.Y > 0, "the drag didn't pan the list");
        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // The 12-item probe: a press dragged off its item, where the list can't pan, and let go there clicks nothing.
    [AvaloniaFact]
    public void Dragged_Off_The_Item_Clicks_Nothing()
    {
        using var clock = new TestClock();
        using var page = new ClickPage();
        var at = Presses[1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(300);
        page.Window.TouchMove(touch, at + new Point(0, 60));
        clock.At(500);
        page.Window.TouchEnd(touch, at + new Point(0, 60));
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
        Assert.Equal(0, page.List.Scroll!.Offset.Y);
    }

    // The probes: a press moved 10 px sideways over 300 ms is still a tap, and clicks.
    [AvaloniaFact]
    public void A_Press_Moved_Ten_Pixels_Sideways_Still_Clicks()
    {
        using var clock = new TestClock();
        using var page = new ClickPage(40);
        var at = Presses[1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(100);
        for (var step = 1; step <= 10; step++)
        {
            page.Window.TouchMove(touch, at + new Point(step, 0));
            clock.At(100 + 30 * step);
        }

        page.Window.TouchEnd(touch, at + new Point(10, 0));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["item 2"], page.Clicked);
        Assert.Equal(-1, page.List.SelectedIndex);
    }

    // ----- The check mode -----

    // In the check mode, Multiple and Toggle (WinRT's Multiple), a tap checks the item as before and clicks nothing.
    [AvaloniaTheory]
    [InlineData(SelectionMode.Multiple | SelectionMode.Toggle)]
    [InlineData(SelectionMode.Multiple | SelectionMode.Toggle | SelectionMode.AlwaysSelected)]
    public void In_The_Check_Mode_A_Tap_Checks_And_Clicks_Nothing(SelectionMode mode)
    {
        using var page = new ClickPage();
        page.List.SelectionMode = mode;
        Dispatcher.UIThread.RunJobs();

        page.Tap(page.On(1));

        Assert.Empty(page.Clicked);
        Assert.True(page.Item(1).IsSelected);
    }

    // Single's other flags keep item click: Toggle and AlwaysSelected without Multiple.
    [AvaloniaTheory]
    [InlineData(SelectionMode.Single | SelectionMode.Toggle)]
    [InlineData(SelectionMode.Single | SelectionMode.AlwaysSelected)]
    public void Without_Multiple_A_Tap_Clicks(SelectionMode mode)
    {
        using var page = new ClickPage();
        page.List.SelectionMode = mode;
        Dispatcher.UIThread.RunJobs();
        var before = page.List.SelectedIndex;

        page.Tap(page.On(1));

        Assert.Equal(["item 1"], page.Clicked);
        Assert.Equal(before, page.List.SelectedIndex);
    }

    // Multiple without Toggle, WinRT's Extended, isn't the check mode (#142: the phone shows no boxes, and a tap is
    // Single's): with item click on, a tap clicks and selects nothing, and a selection set in code stands.
    [AvaloniaTheory]
    [InlineData(SelectionMode.Multiple)]
    [InlineData(SelectionMode.Multiple | SelectionMode.AlwaysSelected)]
    public void Multiple_Without_Toggle_A_Tap_Clicks(SelectionMode mode)
    {
        using var page = new ClickPage();
        page.List.SelectionMode = mode;
        page.List.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();

        page.Tap(page.On(1));

        Assert.Equal(["item 1"], page.Clicked);
        Assert.Equal([(object)"item 3"], page.List.SelectedItems!.Cast<object>());
    }

    // Multiple without Toggle, by the mouse and the keys: a Ctrl or Shift click clicks the item and selects nothing;
    // Space clicks the focused item; Ctrl+A, which Avalonia's ListBox takes to select every item in any mode with
    // Multiple, selects nothing. In the check mode Ctrl+A still selects every item, as Avalonia's does.
    [AvaloniaFact]
    public void Multiple_Without_Toggle_The_Mouse_And_Keys_Select_Nothing()
    {
        using var page = new ClickPage();
        page.List.SelectionMode = SelectionMode.Multiple;
        Dispatcher.UIThread.RunJobs();

        page.Window.MouseDown(page.On(1), MouseButton.Left, RawInputModifiers.Control);
        page.Window.MouseUp(page.On(1), MouseButton.Left, RawInputModifiers.Control);
        page.Window.MouseDown(page.On(3), MouseButton.Left, RawInputModifiers.Shift);
        page.Window.MouseUp(page.On(3), MouseButton.Left, RawInputModifiers.Shift);
        Assert.True(page.Item(2).Focus());
        page.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        page.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        page.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        page.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["item 1", "item 3", "item 2"], page.Clicked);
        Assert.Empty(page.List.SelectedItems!);

        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        page.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        page.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(12, page.List.SelectedItems!.Count);
    }

    // The select-all keys in a TextBox in an item are the TextBox's, in a list that clicks: its text is selected.
    [AvaloniaFact]
    public void Select_All_In_A_TextBox_In_An_Item_Is_The_TextBoxs()
    {
        var box = new TextBox { Text = "some text" };
        var list = new ListBox { SelectionMode = SelectionMode.Multiple, Items = { new ListBoxItem { Content = box } } };
        ListBoxOptions.SetIsItemClickEnabled(list, true);
        using var scene = new Scene(list);
        Assert.True(box.Focus());
        Dispatcher.UIThread.RunJobs();

        scene.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        scene.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("some text", box.SelectedText);
        Assert.Empty(list.SelectedItems!);
    }

    // ----- Mouse and keys -----

    // The mouse: Avalonia's item selects on the left or right button's press; with item click on, neither selects, and
    // the left button's release on the item clicks it, as a Button's. The right button clicks nothing.
    [AvaloniaFact]
    public void A_Mouse_Clicks_On_The_Left_Buttons_Release_And_Selects_Nothing()
    {
        using var page = new ClickPage();
        var at = page.On(2);

        page.Window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":pressed", page.Item(2).Classes);
        Assert.Equal(-1, page.List.SelectedIndex);
        Assert.Empty(page.Clicked);
        page.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["item 2"], page.Clicked);

        page.Window.MouseDown(page.On(3), MouseButton.Right);
        page.Window.MouseUp(page.On(3), MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["item 2"], page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // The mouse pressed on an item and let go off it clicks nothing.
    [AvaloniaFact]
    public void A_Mouse_Let_Go_Off_The_Item_Clicks_Nothing()
    {
        using var page = new ClickPage();

        page.Window.MouseDown(page.On(2), MouseButton.Left);
        page.Window.MouseMove(page.On(4));
        page.Window.MouseUp(page.On(4), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // With hold on the mouse (Avalonia's IsHoldWithMouseEnabled), Avalonia's item selects on a button's release, not
    // its press; neither the left button's nor the right's selects, and the left's clicks.
    [AvaloniaFact]
    public void With_Hold_On_The_Mouse_Neither_Button_Selects()
    {
        using var page = new ClickPage();
        foreach (var item in page.List.GetRealizedContainers())
        {
            InputElement.SetIsHoldWithMouseEnabled(item, true);
        }

        page.Window.MouseDown(page.On(3), MouseButton.Right);
        page.Window.MouseUp(page.On(3), MouseButton.Right);
        page.Window.MouseDown(page.On(2), MouseButton.Left);
        page.Window.MouseUp(page.On(2), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["item 2"], page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // Space and Enter, which select the focused item in Avalonia's ListBox, click it instead, once, as the key is let
    // go: a held key, whose press repeats, clicks once.
    [AvaloniaTheory]
    [InlineData(Key.Enter, PhysicalKey.Enter)]
    [InlineData(Key.Space, PhysicalKey.Space)]
    public void Space_And_Enter_Click_The_Focused_Item_Once_As_They_Are_Let_Go(Key key, PhysicalKey physical)
    {
        using var page = new ClickPage();
        var item = page.Item(2);
        Assert.True(item.Focus());
        Dispatcher.UIThread.RunJobs();

        page.Window.KeyPress(key, RawInputModifiers.None, physical, null);
        page.Window.KeyPress(key, RawInputModifiers.None, physical, null);
        page.Window.KeyPress(key, RawInputModifiers.None, physical, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(page.Clicked);
        page.Window.KeyRelease(key, RawInputModifiers.None, physical, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["item 2"], page.Clicked);
        Assert.Equal(-1, page.List.SelectedIndex);
    }

    // A key let go on an item whose press it didn't take (the key pressed elsewhere) clicks nothing.
    [AvaloniaFact]
    public void A_Key_Let_Go_Without_Its_Press_Clicks_Nothing()
    {
        using var page = new ClickPage();
        Assert.True(page.Item(2).Focus());
        Dispatcher.UIThread.RunJobs();

        page.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
    }

    // Space on a focused Button in an item is the Button's: it clicks, and the item doesn't.
    [AvaloniaFact]
    public void A_Key_On_A_Control_In_The_Item_Is_The_Controls()
    {
        using var page = new ClickPage();
        var buttonClicks = 0;
        page.List.ItemTemplate = new FuncDataTemplate<string>((text, _) =>
        {
            var button = new Button { Content = text };
            button.Click += (_, _) => buttonClicks++;
            return button;
        });
        Dispatcher.UIThread.RunJobs();
        Assert.True(page.Item(1).GetVisualDescendants().OfType<Button>().Single().Focus());
        Dispatcher.UIThread.RunJobs();

        page.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        page.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, buttonClicks);
        Assert.Empty(page.Clicked);
    }

    // A key pressed on an item that loses the focus before the key is let go is forgotten: let go on the item later,
    // after the focus comes back, it clicks nothing.
    [AvaloniaFact]
    public void A_Key_Pressed_Before_The_Focus_Left_Clicks_Nothing()
    {
        using var page = new ClickPage();
        var item = page.Item(2);
        Assert.True(item.Focus());
        Dispatcher.UIThread.RunJobs();

        page.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.True(page.Item(3).Focus());
        Assert.True(item.Focus());
        page.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
    }

    // ----- In and round the item -----

    // A Button in an item takes its own tap: the Button clicks, the item doesn't.
    [AvaloniaFact]
    public void A_Button_In_An_Item_Takes_Its_Own_Tap()
    {
        using var page = new ClickPage();
        var buttonClicks = 0;
        page.List.ItemTemplate = new FuncDataTemplate<string>((text, _) =>
        {
            var button = new Button { Content = text };
            button.Click += (_, _) => buttonClicks++;
            return button;
        });
        Dispatcher.UIThread.RunJobs();
        var button = page.Item(1).GetVisualDescendants().OfType<Button>().Single();
        var at = button.TranslatePoint(new Point(10, 10), page.Window)!.Value;

        page.Tap(at);

        Assert.Equal(1, buttonClicks);
        Assert.Empty(page.Clicked);
        Assert.Equal(-1, page.List.SelectedIndex);
    }

    // A list in an item of a list clicks its own item alone: ItemClick is the list's own, as WinRT's, so the outer
    // list hears nothing, and neither list selects.
    [AvaloniaFact]
    public void A_List_In_An_Item_Clicks_Its_Own_Item_Alone()
    {
        var inner = new ListBox { ItemsSource = new[] { "a", "b" } };
        ListBoxOptions.SetIsItemClickEnabled(inner, true);
        var outer = new ListBox { Margin = ProbeListMargin, ItemsSource = new object[] { "first", inner } };
        ListBoxOptions.SetIsItemClickEnabled(outer, true);
        var innerHeard = new List<object?>();
        var outerHeard = new List<object?>();
        inner.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => innerHeard.Add(e.ClickedItem));
        outer.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => outerHeard.Add(e.ClickedItem), RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        using var scene = new Scene(outer);
        var b = (ListBoxItem)inner.ContainerFromIndex(1)!;
        var at = b.TranslatePoint(new Point(20, 10), scene.Window)!.Value;

        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["b"], innerHeard);
        Assert.Empty(outerHeard);
        Assert.Equal(-1, outer.SelectedIndex);
        Assert.Equal(-1, inner.SelectedIndex);
    }

    // A press something in the item took (here a plain list's item, which selects on the mouse's press) clicks nothing
    // when it is let go, though the release reaches the outer item: the mouse and a touch agree.
    [AvaloniaFact]
    public void A_Press_Something_In_The_Item_Took_Clicks_Nothing()
    {
        var inner = new ListBox { ItemsSource = new[] { "a", "b" } };
        var outer = new ListBox { Margin = ProbeListMargin, ItemsSource = new object[] { "first", inner } };
        ListBoxOptions.SetIsItemClickEnabled(outer, true);
        var heard = new List<object?>();
        outer.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => heard.Add(e.ClickedItem));
        using var scene = new Scene(outer);
        var b = (ListBoxItem)inner.ContainerFromIndex(1)!;
        var at = b.TranslatePoint(new Point(20, 10), scene.Window)!.Value;

        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, inner.SelectedIndex);
        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(heard);
        Assert.Equal(-1, outer.SelectedIndex);
    }

    // The right button pressed and let go while the left is held on the item clicks nothing.
    [AvaloniaFact]
    public void A_Right_Click_While_The_Left_Is_Held_Clicks_Nothing()
    {
        using var page = new ClickPage();
        var at = page.On(2);

        page.Window.MouseDown(at, MouseButton.Left);
        page.Window.MouseDown(at, MouseButton.Right);
        page.Window.MouseUp(at, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
        page.Window.MouseUp(at, MouseButton.Left);
    }

    // A press something in the item took forgets an earlier press of the same pointer on the item, whose release never
    // came back to it: letting go then clicks nothing.
    [AvaloniaFact]
    public void A_Press_Something_In_The_Item_Took_Forgets_An_Earlier_Press()
    {
        using var page = new ClickPage();
        page.List.ItemTemplate = new FuncDataTemplate<string>((text, _) =>
        {
            var grabber = new Border { Name = "Grabber", Height = 20, Background = Avalonia.Media.Brushes.Transparent };
            grabber.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Handled = true);
            return new StackPanel { Children = { new TextBlock { Text = text }, grabber } };
        });
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var text = item.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "item 1");
        var grabber = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Grabber");
        EventHandler<PointerPressedEventArgs> letGo = (_, e) => e.Pointer.Capture(null);
        page.Window.AddHandler(InputElement.PointerPressedEvent, letGo, RoutingStrategies.Bubble, handledEventsToo: true);

        page.Window.MouseDown(text.TranslatePoint(new Point(5, 5), page.Window)!.Value, MouseButton.Left);
        page.Window.MouseMove(page.On(4));
        page.Window.MouseUp(page.On(4), MouseButton.Left);
        page.Window.RemoveHandler(InputElement.PointerPressedEvent, letGo);
        var onGrabber = grabber.TranslatePoint(new Point(5, 5), page.Window)!.Value;
        page.Window.MouseDown(onGrabber, MouseButton.Left);
        page.Window.MouseUp(onGrabber, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
    }

    // A press whose pointer is taken from the item (as a pan takes it) is forgotten: let go back on the item, it clicks
    // nothing.
    [AvaloniaFact]
    public void A_Press_Whose_Pointer_Is_Taken_Clicks_Nothing()
    {
        using var page = new ClickPage();
        IPointer? pointer = null;
        page.Window.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);

        page.Window.MouseDown(page.On(2), MouseButton.Left);
        pointer!.Capture(null);
        page.Window.MouseUp(page.On(2), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // A release something in the item handled forgets the press too, so a later release of the same pointer, with no
    // press on the item, clicks nothing.
    [AvaloniaFact]
    public void A_Release_Something_In_The_Item_Took_Forgets_The_Press()
    {
        using var page = new ClickPage();
        page.List.ItemTemplate = new FuncDataTemplate<string>((text, _) =>
        {
            var eater = new Border { Name = "Eater", Height = 20, Background = Brushes.Transparent };
            eater.AddHandler(InputElement.PointerReleasedEvent, (_, e) => e.Handled = true);
            return new StackPanel { Children = { new TextBlock { Text = text }, eater } };
        });
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var text = item.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "item 1");
        var onEater = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Eater").TranslatePoint(new Point(5, 5), page.Window)!.Value;

        page.Window.MouseDown(onEater, MouseButton.Left);
        page.Window.MouseUp(onEater, MouseButton.Left);
        EventHandler<PointerPressedEventArgs> letGo = (_, e) => e.Pointer.Capture(null);
        page.Window.AddHandler(InputElement.PointerPressedEvent, letGo, RoutingStrategies.Bubble, handledEventsToo: true);
        page.Window.MouseDown(page.On(3), MouseButton.Left);
        var onText = text.TranslatePoint(new Point(5, 5), page.Window)!.Value;
        page.Window.MouseMove(onText);
        page.Window.MouseUp(onText, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
    }

    // A release with no press on the item (the mouse pressed off it, its capture let go, and let go on the item) clicks
    // nothing.
    [AvaloniaFact]
    public void A_Release_Without_A_Press_On_The_Item_Clicks_Nothing()
    {
        using var page = new ClickPage();
        page.Window.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Pointer.Capture(null), RoutingStrategies.Bubble, handledEventsToo: true);

        page.Window.MouseDown(page.On(2), MouseButton.Left);
        page.Window.MouseMove(page.On(4));
        page.Window.MouseUp(page.On(4), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(page.Clicked);
        Assert.Equal(0, page.SelectionChanges);
    }

    // A tap in a popup a control in the item owns is routed through the item but isn't on it: it clicks nothing, though
    // the popup is drawn over the item.
    [AvaloniaFact]
    public void A_Tap_In_A_Popup_Over_The_Item_Clicks_Nothing()
    {
        using var page = new ClickPage();
        var inPopup = new Border { Width = 200, Height = 30, Background = Brushes.Transparent };
        var popup = new Popup { Child = inPopup, Placement = PlacementMode.Bottom };
        page.List.ItemTemplate = new FuncDataTemplate<string>((text, _) =>
            new Panel { Children = { new TextBlock { Text = text }, text == "item 1" ? popup : new Panel() } });
        Dispatcher.UIThread.RunJobs();
        popup.IsOpen = true;
        Dispatcher.UIThread.RunJobs();
        var popupRoot = TopLevel.GetTopLevel(inPopup)!;
        var at = inPopup.TranslatePoint(new Point(20, 5), popupRoot)!.Value;
        var routed = false;
        page.Item(1).AddHandler(InputElement.PointerReleasedEvent, (_, _) => routed = true, RoutingStrategies.Tunnel, handledEventsToo: true);

        var touch = popupRoot.TouchBegin(at);
        popupRoot.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
        popup.IsOpen = false;

        Assert.True(routed, "the popup's release wasn't routed through the item");
        Assert.Empty(page.Clicked);
    }

    // A held touch is a hold, which opens a context menu, not a click: let go, it clicks nothing. Avalonia's hold is on
    // a real timer, so this waits for it in real time.
    [AvaloniaFact]
    public void A_Hold_Clicks_Nothing()
    {
        using var page = new ClickPage();
        var item = page.Item(2);
        var held = false;
        item.AddHandler(InputElement.HoldingEvent, (_, e) => held |= e.HoldingState == HoldingState.Started, RoutingStrategies.Bubble, handledEventsToo: true);
        var at = page.On(2);

        var touch = page.Window.TouchBegin(at);
        var frame = new DispatcherFrame();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var poll = new DispatcherTimer(TimeSpan.FromMilliseconds(5), DispatcherPriority.Background, (_, _) =>
        {
            if (held || watch.ElapsedMilliseconds > 3000)
            {
                frame.Continue = false;
            }
        });
        poll.Start();
        Dispatcher.UIThread.PushFrame(frame);
        poll.Stop();
        page.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.True(held, "no hold came");
        Assert.Empty(page.Clicked);
    }

    // Behaviour only: under Simple's ListBox, with no Cornflower theme, a tap clicks and selects nothing.
    [AvaloniaFact]
    public void It_Works_Under_Another_Theme()
    {
        using var page = new ClickPage(themes: [new SimpleTheme()]);
        Assert.False(CfTilt.GetIsEnabled(page.Item(1)));

        page.Tap(page.On(1, 20, 5));

        Assert.Equal(["item 1"], page.Clicked);
        Assert.Equal(-1, page.List.SelectedIndex);
    }

    // An item that is its own container clicks as itself: ClickedItem is the ListBoxItem, the list's item.
    [AvaloniaFact]
    public void An_Item_That_Is_Its_Own_Container_Is_Clicked_As_Itself()
    {
        var own = new ListBoxItem { Content = "own" };
        var list = new ListBox { Margin = ProbeListMargin, Items = { own } };
        ListBoxOptions.SetIsItemClickEnabled(list, true);
        var clicked = new List<object?>();
        list.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => clicked.Add(e.ClickedItem));
        using var scene = new Scene(list);
        var at = own.TranslatePoint(new Point(200, 20), scene.Window)!.Value;

        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([own], clicked);
        Assert.False(own.IsSelected);
    }
}
