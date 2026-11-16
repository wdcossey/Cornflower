using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// How each popup is dismissed, and the AutoSuggestBox's events (#150), run on a schedule after a start tap on the
    /// header band (<see cref="RunScheduled"/>). Each case opens a popup from code 1.2 s before its touch, then the
    /// touch: the Back key (F1, which the emulator maps to the phone's Back), a tap outside the popup (on the page's
    /// "under" Button, below every popup that doesn't fill the screen), a tap on the popup itself, or on one of its
    /// parts (an item, a dialog's button, the pickers' accept and cancel). Then a step closes whatever is still open.
    /// Every flyout's Opening, Opened and Closed, every dialog's and picker's events and results, the "under" Button's
    /// pointer events, Tapped and Click, the page's presses, HardwareButtons.BackPressed (with whether it was handled
    /// when the app's handler ran, and the open popups) and the keyboard's Showing and Hiding are logged with the frame's
    /// RenderingTime. The app's BackPressed handler marks Back handled, so that Back with nothing open doesn't leave the
    /// app, except in the last case, which leaves a top Flyout's Back to the phone. The log is written after every case.
    /// </summary>
    internal sealed partial class Probe
    {
        /// <summary>The virtual-key code of F1, the emulator's Back.</summary>
        private const int BackKey = 0x70;

        /// <summary>Closes the case's popup, if it is still open; set by each case's setup.</summary>
        private Action _closeCase;
        private bool _backHandled = true;

        /// <summary>
        /// Where a tap outside lands: on the "under" Button, below the top Flyout and over the dialog's dim layer. Not off
        /// the menu, whose last item reaches 642.5 at its target (20261116-1951-dev11): the menu's cases tap above and
        /// below it instead.
        /// </summary>
        private static readonly Point Outside = new Point(225, 630);

        /// <summary>The pickers' accept and cancel, where the shell draws them on the reference profile (pickerbar.py, 20261108-2254-dev11).</summary>
        private static readonly Point Accept = new Point(187.5, 768.3);
        private static readonly Point Cancel = new Point(261.25, 768.3);

        private static int OpenPopups => VisualTreeHelper.GetOpenPopups(Window.Current).Count;

        private async Task DismissProbe(Grid host, bool again = false)
        {
            WatchRt();
            var header = Header();
            var opener = new Border { Width = 1, Height = 1, Margin = new Thickness(0, HeaderHeight, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var suggest = new AutoSuggestBox { Name = "suggest", Header = "auto suggest box", PlaceholderText = "type a fruit", Margin = new Thickness(19, 70, 19, 0), VerticalAlignment = VerticalAlignment.Top };
            var menuTarget = new Button { Name = "menutarget", Content = "menu target", Margin = new Thickness(19, 312, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var under = new Button { Name = "under", Content = "under", Margin = new Thickness(19, 560, 19, 0), Height = 140, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
            var page = await Page(host, header, opener, suggest, menuTarget, under);

            LogPointers(under, "under");
            LogGestures(under, "under");
            under.Click += (s, e) => Log("event", "under Click " + Rt);
            page.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((s, e) => Log("pressed", "page " + Num(e.GetCurrentPoint(null).Position.X) + " " + Num(e.GetCurrentPoint(null).Position.Y) + " on " + Name(e.OriginalSource as UIElement ?? page) + (e.Handled ? " handled" : "") + " popups " + OpenPopups)), true);
            HardwareButtons.BackPressed += (s, e) =>
            {
                Log("back", "BackPressed handled-on-entry " + e.Handled + " popups " + OpenPopups + " " + Rt + (_backHandled ? "; marked handled" : "; left to the phone"));
                if (_backHandled)
                {
                    e.Handled = true;
                }
            };
            var pane = InputPane.GetForCurrentView();
            pane.Showing += (s, e) => Log("keyboard", "Showing occluded " + Describe(e.OccludedRect) + " " + Rt);
            pane.Hiding += (s, e) => Log("keyboard", "Hiding occluded " + Describe(e.OccludedRect) + " " + Rt);

            // The menu's items, read once with it open at its target, so the taps on them are planned against the phone's layout.
            var items = new List<Rect>();
            var probeMenu = StatesMenu("menu");
            probeMenu.ShowAt(menuTarget);
            await Task.Delay(1500);
            foreach (var item in MenuItems())
            {
                var b = Bounds(item);
                items.Add(b);
                Log("layout", "menu item " + Describe(b) + " " + item.GetType().Name + " '" + ((MenuFlyoutItem)item).Text + "'");
            }

            probeMenu.Hide();
            await Task.Delay(1500);
            if (items.Count != 5)
            {
                await Finish("the menu showed " + items.Count + " items, not 5");
                return;
            }

            // The first item of a long menu, and the long dialog's content, every frame, for the drags on them.
            WatchFrames(() =>
            {
                try
                {
                    return new[] { _watchItem != null ? Bounds(_watchItem).Y : double.NaN };
                }
                catch (ArgumentException)
                {
                    return new[] { double.NaN };
                }
            }, v => "watched " + (_watchName ?? "-") + " y " + Num(v[0]));

            if (again)
            {
                await DismissAgain(page, header, menuTarget, under, suggest);
                return;
            }

            // Where a menu of four items (244.58 tall on the reference) sits against targets lower and lower down: the
            // target's top set so the menu, shown below it, would end at each of these window y's.
            foreach (var end in new[] { 700, 760, 790, 799, 801, 810, 830 })
            {
                menuTarget.Margin = new Thickness(19, end - 244.58 - 57.5, 0, 0);
                await Task.Delay(300);
                var t = Bounds(menuTarget);
                var four = new MenuFlyout();
                for (var i = 1; i <= 4; i++)
                {
                    four.Items.Add(new MenuFlyoutItem { Text = "menu item " + i });
                }

                four.ShowAt(menuTarget);
                await Task.Delay(1200);
                var shown = VisualTreeHelper.GetOpenPopups(Window.Current).Select(pp => pp.Child as FrameworkElement).Where(c => c != null && c.ActualHeight < 790).Select(Bounds).ToList();
                Log("layout", "menu of four at a target " + Describe(t) + ": " + (shown.Count == 1 ? Describe(shown[0]) + (shown[0].Y >= t.Bottom - 0.5 ? " below" : shown[0].Bottom <= t.Y + 0.5 ? " above" : " over") : shown.Count + " popups"));
                four.Hide();
                await Task.Delay(800);
            }

            menuTarget.Margin = new Thickness(19, 312, 0, 0);
            await Task.Delay(300);

            Func<Rect, Point> mid = r => new Point(r.X + r.Width / 2, r.Y + r.Height / 2);

            // Flyout, top and full.
            Case("flyout top: Back", () => OpenFlyout("flyout top", FlyoutPlacementMode.Top, opener), null);
            Case("flyout top: tap outside", () => OpenFlyout("flyout top", FlyoutPlacementMode.Top, opener), Outside);
            Case("flyout top: tap on it", () => OpenFlyout("flyout top", FlyoutPlacementMode.Top, opener), new Point(225, 40));
            Case("flyout full: Back", () => OpenFlyout("flyout full", FlyoutPlacementMode.Full, opener), null);
            Case("flyout full: tap on it", () => OpenFlyout("flyout full", FlyoutPlacementMode.Full, opener), Outside);

            // MenuFlyout.
            Func<Task> menu = () => OpenMenu(menuTarget);
            Case("menu: Back", menu, null);
            // Off the menu above and below it (20261116-1951-dev11 tapped at 630, which was on its last item).
            Case("menu: tap above it", menu, new Point(225, 150));
            Case("menu: tap below it", menu, new Point(225, 740));
            Case("menu: tap an item", menu, mid(items[0]));
            Case("menu: tap the checked toggle", menu, mid(items[1]));
            Case("menu: tap the disabled item", menu, mid(items[3]));
            DragCase("long menu: drag up 300", () => OpenLongMenu(menuTarget), 225, 600, 300);
            Case("long menu: then Back", null, null);

            // ContentDialog: Back, the dim layer, its body, then each button.
            Func<Task> dialog = () => OpenDialog();
            Case("dialog: Back", dialog, null);
            Case("dialog: tap the dim layer", dialog, Outside);
            Case("dialog: tap its body", dialog, new Point(225, 96));
            Case("dialog: tap the primary button", dialog, new Point(117.5, 161));
            Case("dialog: tap the secondary button", dialog, new Point(333, 161));
            // The dialog with a TextBox (its box at y 117.9 to 183.8 in 20261116-1934-dev11's dump): tapped, then Back twice.
            Case("dialog with a TextBox: tap the TextBox", () => OpenDialog(true), new Point(225, 165), keep: true);
            Case("dialog with a TextBox: Back", null, null, keep: true);
            Case("dialog with a TextBox: Back again", null, null);
            // Forty lines of content, longer than the screen: does it scroll under a drag?
            // The long dialog's drag is in DismissAgain: opening it moves the focus to the page's AutoSuggestBox.

            // ListPickerFlyout: items 58.33 apart from y 93.33, 39.17 tall (20261108-2254-dev11's dumps).
            Func<int, Point> row = k => new Point(225, 93.33 + 58.33 * k + 19.6);
            Case("list single: Back", () => OpenList(ListPickerFlyoutSelectionMode.Single, opener), null);
            Case("list single: tap four", () => OpenList(ListPickerFlyoutSelectionMode.Single, opener), row(3));
            Case("list multiple: tap one", () => OpenList(ListPickerFlyoutSelectionMode.Multiple, opener), row(0), keep: true);
            Case("list multiple: then Back", null, null);
            Case("list multiple: tap one", () => OpenList(ListPickerFlyoutSelectionMode.Multiple, opener), row(0), keep: true);
            Case("list multiple: tap two", null, row(1), keep: true);
            Case("list multiple: then accept", null, Accept);
            Case("list multiple: cancel", () => OpenList(ListPickerFlyoutSelectionMode.Multiple, opener), Cancel);

            // PickerFlyout, DatePickerFlyout, TimePickerFlyout.
            Case("picker: Back", () => OpenPicker(opener), null);
            Case("picker: accept", () => OpenPicker(opener), Accept);
            Case("picker: cancel", () => OpenPicker(opener), Cancel);
            Case("date: Back", () => OpenDate(opener), null);
            Case("date: accept unchanged", () => OpenDate(opener), Accept);
            Case("date: cancel", () => OpenDate(opener), Cancel);
            // The month column (#149: x 17.5 to 155.8, the selected item's centre at y 379.4) dragged up one item (138.375).
            DragCase("date: month up one item", () => OpenDate(opener), 86, 379.4, 241);
            Case("date: then accept", null, Accept);
            Case("time: Back", () => OpenTime(opener), null);
            Case("time: accept unchanged", () => OpenTime(opener), Accept);

            // AutoSuggestBox: the keyboard's keys are planned from the reference's keyboard (README, "AutoSuggestBox").
            SuggestCases(suggest, under);

            // Last: Back on a top Flyout, left to the phone. If the flyout doesn't take it, the app leaves the screen; the
            // log written after the previous case still holds everything before it.
            Code("back left to the phone", () =>
            {
                _backHandled = false;
                return Task.FromResult(true);
            }, 1300);
            Case("flyout top: Back left to the phone", () => OpenFlyout("flyout top", FlyoutPlacementMode.Top, opener), null);

            await RunScheduled(page, header);
        }

        /// <summary>
        /// One case: <paramref name="open"/> (null: nothing new) runs 1.2 s before the touch, which is Back for a null
        /// point and a 90 ms tap there otherwise; 1.5 s later a step logs the open popups and, unless the case keeps
        /// its popup for the next one, closes what is still open and writes the log so far.
        /// </summary>
        private void Case(string label, Func<Task> open, Point? at, bool keep = false)
        {
            Func<Task> setup = async () =>
            {
                Log("case", label + ", popups open " + OpenPopups + " " + Rt);
                if (open != null)
                {
                    await open();
                }
            };
            if (at == null)
            {
                _touches++;
                _schedule.Add(new Scheduled { At = _plannedMs - 1200, Label = label, Action = setup, Touch = true });
                _plan.Add(new Planned { Verb = "key", X = BackKey });
                Wait(1500);
            }
            else
            {
                var p = at.Value;
                PathTouch(null, label, setup, 1500, new[] { p.X, p.Y, 0 }, new[] { p.X, p.Y, 90 });
            }

            AfterCase(label, keep);
        }

        /// <summary>A case whose touch is a drag from (x, y0) up or down to y1 over 600 ms, held 300 ms and let go.</summary>
        private void DragCase(string label, Func<Task> open, double x, double y0, double y1)
        {
            Func<Task> setup = async () =>
            {
                Log("case", label + ", popups open " + OpenPopups + " " + Rt);
                await open();
            };
            PathTouch(null, label, setup, 1500, new[] { x, y0, 0 }, new[] { x, y1, 600 }, new[] { x, y1, 300 });
            AfterCase(label, true);
        }

        private void AfterCase(string label, bool keep)
        {
            Code(label + ": after", async () =>
            {
                Log("after", label + ", popups open " + OpenPopups + " " + Rt);
                if (keep)
                {
                    return;
                }

                if (_closeCase != null && OpenPopups > 0)
                {
                    Log("after", "closing from code");
                    _closeCase();
                }

                _closeCase = null;
                for (var waited = 0; waited < 800 && OpenPopups > 0; waited += 50)
                {
                    await Task.Delay(50);
                }

                await Write("probe.txt", _log.ToString());
            }, keep ? 1300 : 2000);
        }

        private Task OpenFlyout(string name, FlyoutPlacementMode placement, FrameworkElement at)
        {
            var f = Timed(name, new Flyout { Placement = placement, Content = new TextBlock { Text = "flyout content" } });
            Log("call", name + " ShowAt " + Rt);
            f.ShowAt(at);
            _closeCase = f.Hide;
            return Task.FromResult(true);
        }

        /// <summary>The probe's menu: an item, a checked and an unchecked ToggleMenuFlyoutItem, a separator, a disabled item and a last item.</summary>
        private MenuFlyout StatesMenu(string name)
        {
            var m = Timed(name, new MenuFlyout());
            var labels = new[] { "first item", "toggle checked", "toggle unchecked", null, "disabled item", "last item" };
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null)
                {
                    m.Items.Add(new MenuFlyoutSeparator());
                    continue;
                }

                var item = i == 1 || i == 2 ? new ToggleMenuFlyoutItem { Text = labels[i], IsChecked = i == 1 } : new MenuFlyoutItem { Text = labels[i], IsEnabled = labels[i] != "disabled item" };
                var it = item;
                it.Click += (s, e) => Log("event", name + " item '" + it.Text + "' Click" + (it is ToggleMenuFlyoutItem ? " IsChecked " + ((ToggleMenuFlyoutItem)it).IsChecked : "") + " " + Rt);
                m.Items.Add(item);
            }

            return m;
        }

        /// <summary>The open menu's items, separators left out.</summary>
        private static List<MenuFlyoutItem> MenuItems()
        {
            var found = new List<MenuFlyoutItem>();
            foreach (var popup in VisualTreeHelper.GetOpenPopups(Window.Current).Where(p => p.Child != null))
            {
                Collect(popup.Child, found);
            }

            return found;
        }

        private Task OpenMenu(FrameworkElement at)
        {
            var m = StatesMenu("menu");
            Log("call", "menu ShowAt " + Rt);
            m.ShowAt(at);
            _closeCase = m.Hide;
            return Task.FromResult(true);
        }

        /// <summary>The case's watched element, logged every frame it moves (see DismissProbe), and its name.</summary>
        private FrameworkElement _watchItem;
        private string _watchName;

        private Task OpenLongMenu(FrameworkElement at)
        {
            var m = Timed("long menu", new MenuFlyout());
            for (var i = 1; i <= 24; i++)
            {
                m.Items.Add(new MenuFlyoutItem { Text = "menu item " + i });
            }

            m.Opened += (s, e) =>
            {
                _watchItem = m.Items[0];
                _watchName = "long menu item 1";
                LogOpenPopups("long menu");
            };
            m.Closed += (s, e) => _watchItem = null;
            Log("call", "long menu ShowAt " + Rt);
            m.ShowAt(at);
            _closeCase = m.Hide;
            return Task.FromResult(true);
        }

        private Task OpenDialog(bool textBox = false, bool longBody = false)
        {
            object content = "Agjy 0123 the dialog's body text, long enough to wrap onto a second line.";
            TextBox box = null;
            if (textBox)
            {
                box = new TextBox { Header = "name", Text = "Agjy" };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock { Text = "Agjy 0123 the dialog's body text.", TextWrapping = TextWrapping.Wrap });
                panel.Children.Add(box);
                content = panel;
            }
            else if (longBody)
            {
                content = PopupCatalog.LongBody();
            }

            var d = new ContentDialog { Title = "content dialog", Content = content, PrimaryButtonText = "ok", SecondaryButtonText = "cancel" };
            d.Opened += (s, e) =>
            {
                // The dialog's own panel (its BackgroundElement) for the TextBox, its content for the long body.
                _watchItem = textBox ? Find(d, "BackgroundElement") : longBody ? Find(d, "Content") : null;
                _watchName = textBox ? "dialog background" : "dialog content";
            };
            d.Closed += (s, e) => _watchItem = null;
            if (box != null)
            {
                box.GotFocus += (s, e) => Log("event", "dialog TextBox GotFocus " + Rt);
                box.LostFocus += (s, e) => Log("event", "dialog TextBox LostFocus " + Rt);
            }

            d.Opened += (s, e) => Log("event", "dialog Opened " + Rt);
            d.Closing += (s, e) => Log("event", "dialog Closing result " + e.Result + " cancel " + e.Cancel + " " + Rt);
            d.Closed += (s, e) => Log("event", "dialog Closed result " + e.Result + " " + Rt);
            d.PrimaryButtonClick += (s, e) => Log("event", "dialog PrimaryButtonClick cancel " + e.Cancel + " " + Rt);
            d.SecondaryButtonClick += (s, e) => Log("event", "dialog SecondaryButtonClick cancel " + e.Cancel + " " + Rt);
            Log("call", "dialog ShowAsync " + Rt);
            Result("dialog", d.ShowAsync(), r => r.ToString());
            _closeCase = d.Hide;
            return Task.FromResult(true);
        }

        private Task OpenList(ListPickerFlyoutSelectionMode mode, FrameworkElement at)
        {
            var name = "list " + mode.ToString().ToLowerInvariant();
            var p = Timed(name, new ListPickerFlyout { ItemsSource = new[] { "one", "two", "three", "four", "five" }, SelectionMode = mode });
            if (mode == ListPickerFlyoutSelectionMode.Single)
            {
                p.SelectedIndex = 1;
            }
            else
            {
                p.SelectedItems.Add("two");
            }

            PickerFlyoutBase.SetTitle(p, "LIST PICKER");
            p.ItemsPicked += (s, e) => Log("event", name + " ItemsPicked added [" + string.Join(", ", e.AddedItems) + "] removed [" + string.Join(", ", e.RemovedItems) + "] selected [" + string.Join(", ", s.SelectedItems) + "] index " + s.SelectedIndex + " " + Rt);
            Log("call", name + " ShowAtAsync " + Rt);
            Result(name, p.ShowAtAsync(at), r => "[" + string.Join(", ", r) + "]");
            _closeCase = p.Hide;
            return Task.FromResult(true);
        }

        private Task OpenPicker(FrameworkElement at)
        {
            var p = Timed("picker", new PickerFlyout { Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) }, ConfirmationButtonsVisible = true });
            p.Confirmed += (s, e) => Log("event", "picker Confirmed " + Rt);
            Log("call", "picker ShowAtAsync " + Rt);
            Result("picker", p.ShowAtAsync(at), r => r.ToString());
            _closeCase = p.Hide;
            return Task.FromResult(true);
        }

        private Task OpenDate(FrameworkElement at)
        {
            var d = Timed("date", new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) });
            d.DatePicked += (s, e) => Log("event", "date DatePicked old " + e.OldDate.ToString("yyyy-MM-dd") + " new " + e.NewDate.ToString("yyyy-MM-dd") + " " + Rt);
            Log("call", "date ShowAtAsync " + Rt);
            Result("date", d.ShowAtAsync(at), r => r.HasValue ? r.Value.ToString("yyyy-MM-dd") : "null");
            _closeCase = d.Hide;
            return Task.FromResult(true);
        }

        private Task OpenTime(FrameworkElement at)
        {
            var t = Timed("time", new TimePickerFlyout { Time = new TimeSpan(12, 30, 0) });
            t.TimePicked += (s, e) => Log("event", "time TimePicked old " + e.OldTime + " new " + e.NewTime + " " + Rt);
            Log("call", "time ShowAtAsync " + Rt);
            Result("time", t.ShowAtAsync(at), r => r.HasValue ? r.Value.ToString() : "null");
            _closeCase = t.Hide;
            return Task.FromResult(true);
        }

        /// <summary>Logs what the operation returns, when it does, with the frame's RenderingTime.</summary>
        private async void Result<T>(string name, IAsyncOperation<T> op, Func<T, string> describe)
        {
            try
            {
                var r = await op;
                Log("result", name + " " + describe(r) + " " + Rt);
            }
            catch (Exception ex)
            {
                Log("result", name + " " + ex.GetType().Name + ": " + ex.Message + " " + Rt);
            }
        }

        private static string Describe(Rect r) => Num(r.X) + " " + Num(r.Y) + " " + Num(r.Width) + " " + Num(r.Height);

        /// <summary>Logs the pointer events of every open popup's child, under a tag: where a touch on a popup goes.</summary>
        private void LogOpenPopups(string tag)
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopups(Window.Current).Where(p => p.Child != null))
            {
                LogPointers(popup.Child, tag);
            }
        }

        /// <summary>
        /// <c>-Probe dismissagain</c>: what the first run (20261116-1951-dev11) missed. A tap off the menu above and below
        /// it (that run's "outside" landed on the menu's last item); the long menu dragged, with the popups' pointer
        /// events; the long dialog dragged with the focus put on the page's Button first (in that run the AutoSuggestBox
        /// took focus and the drag typed on the keyboard); and the AutoSuggestBox from an empty box: a key and Back twice,
        /// then two keys, a tap on a suggestion, Enter and Back twice.
        /// </summary>
        private async Task DismissAgain(Grid page, Border header, Button menuTarget, Button under, AutoSuggestBox suggest)
        {
            Func<Task> menu = () => OpenMenu(menuTarget);
            Case("menu: tap above it", menu, new Point(225, 150));
            Case("menu: tap below it", menu, new Point(225, 740));
            DragCase("long menu: drag up 300", () => OpenLongMenu(menuTarget), 225, 600, 300);
            Case("long menu: then Back", null, null);
            DragCase("long dialog: drag up 300", async () =>
            {
                under.Focus(FocusState.Programmatic);
                await OpenDialog(false, true);
                // ShowAsync puts the dialog's popups up a moment later: wait for them before logging their presses.
                await Task.Delay(500);
                LogOpenPopups("long dialog");
            }, 225, 600, 300);
            Case("long dialog: then Back", null, null);
            SuggestCases(suggest, under, true);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// The AutoSuggestBox: a tap on its box (focus, the keyboard), keys typed on the keyboard, each TextChanged
        /// answered with the fruits starting with the text, a tap on a suggestion, Text set from code, the keyboard's
        /// Enter, Back with the keyboard up, then typing again and a tap outside. Each TextChanged (reason, text, the
        /// list open), SuggestionChosen, the TextBox's focus and keys, and every frame the box's, the list's and the
        /// keyboard's places are logged.
        /// </summary>
        private void SuggestCases(AutoSuggestBox box, Button under, bool again = false)
        {
            var inner = Find(box, "TextBox") as TextBox;
            var popup = Find(box, "SuggestionsPopup") as Popup;
            if (inner == null || popup == null)
            {
                Log("error", "the AutoSuggestBox has no TextBox or SuggestionsPopup part");
                return;
            }

            box.TextChanged += (s, e) =>
            {
                Log("event", "suggest TextChanged " + e.Reason + " text '" + s.Text + "' list open " + s.IsSuggestionListOpen + " " + Rt);
                if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    var t = s.Text;
                    s.ItemsSource = PopupCatalog.Fruits.Where(f => f.StartsWith(t, StringComparison.OrdinalIgnoreCase)).ToList();
                    Log("event", "suggest items " + ((List<string>)s.ItemsSource).Count + " list open " + s.IsSuggestionListOpen);
                }
            };
            box.SuggestionChosen += (s, e) => Log("event", "suggest SuggestionChosen '" + e.SelectedItem + "' text '" + s.Text + "' list open " + s.IsSuggestionListOpen + " " + Rt);
            inner.GotFocus += (s, e) => Log("event", "suggest GotFocus " + Rt);
            inner.LostFocus += (s, e) => Log("event", "suggest LostFocus " + Rt);
            inner.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((s, e) => Log("event", "suggest KeyDown " + e.Key + (e.Handled ? " handled" : "") + " " + Rt)), true);
            LogPointers(box, "suggest");
            var pane = InputPane.GetForCurrentView();
            WatchFrames(() =>
            {
                var b = Bounds(inner);
                var child = popup.Child as FrameworkElement;
                var c = child != null && popup.IsOpen && child.ActualHeight > 0 ? child.TransformToVisual(null).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight)) : Rect.Empty;
                var o = pane.OccludedRect;
                return new[] { b.X, b.Y, b.Width, b.Height, popup.IsOpen ? 1 : 0, c.IsEmpty ? -1 : c.X, c.IsEmpty ? -1 : c.Y, c.IsEmpty ? -1 : c.Width, c.IsEmpty ? -1 : c.Height, o.Y, o.Height };
            }, v => "suggest box " + Num(v[0]) + " " + Num(v[1]) + " " + Num(v[2]) + " " + Num(v[3]) + " popup " + (v[4] > 0 ? "open" : "closed") + " list " + Num(v[5]) + " " + Num(v[6]) + " " + Num(v[7]) + " " + Num(v[8]) + " keyboard " + Num(v[9]) + " " + Num(v[10]));

            var field = Bounds(inner);
            var tapBox = new Point(field.X + 40, field.Bottom - 20);
            Func<Task> none = () => Task.FromResult(true);
            if (again)
            {
                Func<Task> empty = () =>
                {
                    _closeCase = null;
                    box.Text = "";
                    return none();
                };
                Case("suggest: tap the empty box", empty, tapBox, keep: true);
                Case("suggest: key a", null, Keys['a'], keep: true);
                Case("suggest: Back with the list and the keyboard up", null, null, keep: true);
                Case("suggest: Back again", null, null, keep: true);
                Case("suggest: tap the empty box again", empty, tapBox, keep: true);
                Case("suggest: key a", null, Keys['a'], keep: true);
                Case("suggest: key p", null, Keys['p'], keep: true);
                Case("suggest: tap the second suggestion", null, SuggestionRow, keep: true);
                Case("suggest: Enter", null, Keys['\n'], keep: true);
                Case("suggest: Back", null, null, keep: true);
                Case("suggest: Back again", null, null);
                return;
            }

            Case("suggest: tap the box", () =>
            {
                _closeCase = null;
                return none();
            }, tapBox, keep: true);
            foreach (var k in "ap")
            {
                Case("suggest: key " + k, null, Keys[k], keep: true);
            }

            Case("suggest: tap a suggestion", null, SuggestionRow, keep: true);
            Case("suggest: Text from code", () =>
            {
                box.Text = "cherry";
                return none();
            }, Keys['\n'], keep: true);
            Case("suggest: Back with the keyboard up", null, null, keep: true);
            Case("suggest: tap the box again", () =>
            {
                box.Text = "";
                return none();
            }, tapBox, keep: true);
            Case("suggest: key b", null, Keys['b'], keep: true);
            // Above the keyboard (its top at 453.3) and below a list of three (it ends at 214.6).
            Case("suggest: tap outside", null, new Point(225, 380), keep: true);
            Case("suggest: Back", null, null);
        }

        /// <summary>
        /// The keyboard's keys used, their centres in view px on the reference profile, read from the screen grab of the
        /// focused box (20261116-1944-dev11; README, "AutoSuggestBox"): keys 39.2 x 65 on rows 70.8 apart.
        /// </summary>
        private static readonly Dictionary<char, Point> Keys = new Dictionary<char, Point>
        {
            { 'a', new Point(45, 622.5) },
            { 'p', new Point(427.5, 551.7) },
            { 'b', new Point(270, 693.3) },
            { '\n', new Point(416.2, 764.4) },
        };

        /// <summary>
        /// The open list's second suggestion: with the box moved to the window's top, the list's top is at 63.33 and its
        /// rows 48.75 apart from 2.5 + 12.08 in, 39.17 tall (20261116-1944-dev11's dumps).
        /// </summary>
        private static readonly Point SuggestionRow = new Point(225, 63.33 + 2.5 + 60.83 + 19.6);
    }
}
