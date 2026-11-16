using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// The reads Tier 3 needs (#149), run on a schedule after a start tap on the header band, like the shell's probes
    /// (<see cref="RunScheduled"/>), whose flash pairs a recording with the log:
    /// - <c>popupcurves</c> (with -Record): each popup opened from code and closed with <c>Hide()</c> eight times, for the
    ///   recording, which shows the shell's swing of each popup that the app can't read; and the inline ComboBox's
    ///   template storyboards, which it can, every frame.
    /// - <c>looping</c>: a press on a closed ComboBox, then a DatePickerFlyout's LoopingSelectors dragged, flicked, tapped
    ///   and caught, with each selector's offset and items every frame.
    /// Every step, event and frame line carries the frame's RenderingTime (the animation clock) as "rt".
    /// </summary>
    internal sealed partial class Probe
    {
        private double _rt = double.NaN;
        private bool _rtWatched;

        /// <summary>The last frame's RenderingTime, kept from CompositionTarget.Rendering once <see cref="WatchRt"/> ran.</summary>
        private void WatchRt()
        {
            if (_rtWatched)
            {
                return;
            }

            _rtWatched = true;
            CompositionTarget.Rendering += (s, e) =>
            {
                var args = e as RenderingEventArgs;
                if (args != null)
                {
                    _rt = args.RenderingTime.TotalMilliseconds;
                }
            };
        }

        private string Rt => "rt " + Num(_rt);

        private T Timed<T>(string name, T flyout) where T : FlyoutBase
        {
            flyout.Opening += (s, e) => Log("event", name + " Opening " + Rt);
            flyout.Opened += (s, e) => Log("event", name + " Opened " + Rt);
            flyout.Closed += (s, e) => Log("event", name + " Closed " + Rt);
            return flyout;
        }

        /// <summary>
        /// The popups' curves (#149, gap register B11 and B12), for the recording: on a black page with two ComboBoxes, the top and full Flyout, the ContentDialog, the ListPickerFlyout (five items), the PickerFlyout,
        /// the DatePickerFlyout, the TimePickerFlyout and a ComboBox of six items (the full-screen picker) are each opened
        /// from code, held 1.5 s, closed with <c>Hide()</c> (the ComboBox with <c>IsDropDownOpen</c>) and given 1.5 s,
        /// eight times; then a ComboBox of five items, which opens inline, the same. The popups are those of
        /// <c>-Probe motion</c> (#66). Each call logs a step and "call rt", and each flyout its events with rt. The inline
        /// ComboBox's <c>ItemsPresenterHost</c> height and <c>ItemsPresenterTranslateTransform</c> are logged every frame
        /// they change, with the control's height.
        /// </summary>
        private async Task PopupCurvesProbe(Grid host)
        {
            WatchRt();
            var header = Header();
            var opener = new Border { Width = 1, Height = 1, Margin = new Thickness(0, HeaderHeight, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var combo6 = Combo("combo6", 6, 200);
            var combo5 = Combo("combo5", 5, 300);
            var page = await Page(host, header, opener, combo6, combo5);
            FrameworkElement hostPart = null;
            TranslateTransform shift = null;
            WatchFrames(() =>
            {
                if (hostPart == null)
                {
                    hostPart = Find(combo5, "ItemsPresenterHost");
                    shift = Find(combo5, "ItemsPresenter") != null ? Find(combo5, "ItemsPresenter").RenderTransform as TranslateTransform : null;
                }

                return new[] { hostPart != null ? hostPart.Height : double.NaN, hostPart != null ? hostPart.ActualHeight : double.NaN, shift != null ? shift.Y : double.NaN, combo5.ActualHeight };
            }, v => "combo5 host height " + Num(v[0]) + " actual " + Num(v[1]) + " shift " + Num(v[2]) + " combo " + Num(v[3]));
            WatchStates(combo5);

            var popups = new List<KeyValuePair<string, Func<Action>>>();
            Action<string, Func<Action>> add = (n, f) => popups.Add(new KeyValuePair<string, Func<Action>>(n, f));
            add("Flyout top", () =>
            {
                var f = Timed("Flyout top", new Flyout { Placement = FlyoutPlacementMode.Top, Content = new TextBlock { Text = "flyout content" } });
                f.ShowAt(opener);
                return f.Hide;
            });
            add("Flyout full", () =>
            {
                var f = Timed("Flyout full", new Flyout { Placement = FlyoutPlacementMode.Full, Content = new TextBlock { Text = "flyout content" } });
                f.ShowAt(opener);
                return f.Hide;
            });
            add("ContentDialog", () =>
            {
                var d = new ContentDialog { Title = "content dialog", Content = "the dialog's body", PrimaryButtonText = "ok", SecondaryButtonText = "cancel" };
                d.Opened += (s, e) => Log("event", "ContentDialog Opened " + Rt);
                d.Closed += (s, e) => Log("event", "ContentDialog Closed " + Rt);
                var op = d.ShowAsync();
                return d.Hide;
            });
            add("ListPickerFlyout", () =>
            {
                var p = new ListPickerFlyout { ItemsSource = new[] { "one", "two", "three", "four", "five" } };
                p.SelectedIndex = 1;
                Timed("ListPickerFlyout", p);
                var op = p.ShowAtAsync(opener);
                return p.Hide;
            });
            add("PickerFlyout", () =>
            {
                var p = Timed("PickerFlyout", new PickerFlyout { Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) }, ConfirmationButtonsVisible = true });
                var op = p.ShowAtAsync(opener);
                return p.Hide;
            });
            add("DatePickerFlyout", () =>
            {
                var d = Timed("DatePickerFlyout", new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) });
                var op = d.ShowAtAsync(opener);
                return d.Hide;
            });
            add("TimePickerFlyout", () =>
            {
                var t = Timed("TimePickerFlyout", new TimePickerFlyout { Time = new TimeSpan(12, 30, 0) });
                var op = t.ShowAtAsync(opener);
                return t.Hide;
            });
            add("ComboBox 6 items", () =>
            {
                combo6.IsDropDownOpen = true;
                return () => combo6.IsDropDownOpen = false;
            });
            add("ComboBox 5 items", () =>
            {
                combo5.IsDropDownOpen = true;
                return () => combo5.IsDropDownOpen = false;
            });

            foreach (var p in popups)
            {
                for (var i = 0; i < 8; i++)
                {
                    Action close = null;
                    var name = p.Key;
                    var open = p.Value;
                    Code(name + " open", () =>
                    {
                        Log("call", name + " open " + Rt);
                        close = open();
                        return Task.FromResult(true);
                    }, 1500);
                    Code(name + " close", () =>
                    {
                        Log("call", name + " close " + Rt);
                        if (close != null)
                        {
                            close();
                        }

                        return Task.FromResult(true);
                    }, 1500);
                }
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// LoopingSelector's physics, and a press on a closed ComboBox (#149). On a dark page a ComboBox of five items is
        /// pressed and held 600 ms, and let go, which opens it inline; it is closed from code. Then a DatePickerFlyout
        /// (15 July 2014) is opened from code and its three LoopingSelectors (month, day, year) touched:
        /// - month: a press held still 600 ms; a drag up of one item and a half at 0.2 px a ms, held 400 ms and let go;
        ///   a drag down of 0.4 of an item, held and let go; a drag up of 0.6 of an item, held and let go;
        /// - day: flicks up of 150 px in 100 ms and down of 300 px in 120 ms; a flick up of 300 px in 100 ms and a press
        ///   300 ms after its release, held 600 ms; a drag down of three items, held 400 ms, let go;
        /// - year: a tap on the item two below the selected one, then two above, and one on the selected item;
        /// - month: three flicks down of 300 px in 80 ms, 1.5 s apart, to wrap.
        /// Where each selector and its items sit is read first, with the flyout opened and closed once, so the touches are
        /// planned against the phone's own layout; they are played with no target, as the popup isn't open when they are
        /// checked. Every frame, each selector's ScrollViewer offset and each realised item's top, opacity and scale are
        /// logged when they change; and its ViewChanging, pointer events and the items' visual states.
        /// </summary>
        private async Task LoopingProbe(Grid host)
        {
            WatchRt();
            var header = Header();
            var opener = new Border { Width = 1, Height = 1, Margin = new Thickness(0, HeaderHeight, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var combo = Combo("combo5", 5, 120);
            var page = await Page(host, header, opener, combo);
            WatchStates(combo);
            WatchTree("combo5", combo);
            LogPointers(combo, "combo5");

            var date = new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) };
            Timed("DatePickerFlyout", date);

            // The layout, read once with the flyout open.
            var op = date.ShowAtAsync(opener);
            List<LoopingSelector> found = null;
            for (var waited = 0; waited < 5000; waited += 100)
            {
                await Task.Delay(100);
                found = Selectors();
                if (found.Count == 3 && found.All(s => s.ActualHeight > 0))
                {
                    break;
                }
            }

            await Task.Delay(800);
            found = Selectors();
            if (found.Count != 3)
            {
                await Finish("the DatePickerFlyout has " + found.Count + " LoopingSelectors, not 3");
                return;
            }

            var boxes = found.Select(Bounds).ToArray();
            var itemHeight = found[0].ItemHeight;
            for (var k = 0; k < found.Count; k++)
            {
                var s = found[k];
                var sv = Find(s, "ScrollViewer") as ScrollViewer;
                Log("layout", "selector " + k + " " + Num(boxes[k].X) + " " + Num(boxes[k].Y) + " " + Num(boxes[k].Width) + " " + Num(boxes[k].Height)
                    + " item " + Num(s.ItemWidth) + " x " + Num(s.ItemHeight) + " loop " + s.ShouldLoop + " items " + (s.Items != null ? s.Items.Count : -1) + " selected " + s.SelectedIndex
                    + (sv != null ? " extent " + Num(sv.ExtentHeight) + " viewport " + Num(sv.ViewportHeight) + " offset " + Num(sv.VerticalOffset) + " snap " + sv.VerticalSnapPointsType + " " + sv.VerticalSnapPointsAlignment : " no ScrollViewer"));
                foreach (var item in Items(s))
                {
                    var b = Bounds(item);
                    Log("layout", "selector " + k + " item " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " opacity " + Num(Root(item) != null ? Root(item).Opacity : double.NaN) + " " + ItemText(item));
                }
            }

            date.Hide();
            for (var waited = 0; waited < 3000 && VisualTreeHelper.GetOpenPopups(Window.Current).Count > 0; waited += 50)
            {
                await Task.Delay(50);
            }

            await Task.Delay(500);

            // The selected item's row: the item nearest the selector's vertical centre.
            var cy = boxes[0].Y + boxes[0].Height / 2;
            var cx = boxes.Select(b => b.X + b.Width / 2).ToArray();
            var h = itemHeight;
            Func<Task> none = () => Task.FromResult(true);

            // The closed ComboBox: a press held, let go (it opens), then closed from code.
            var cb = Bounds(combo);
            Press(combo, "combo press held 600 ms", none, cb.X + 100, cb.Bottom - 20, 600, 0, 0, 0, 0, 1800);
            Code("combo close", () =>
            {
                combo.IsDropDownOpen = false;
                return Task.FromResult(true);
            }, 2000);
            Code("date open", () =>
            {
                Log("call", "DatePickerFlyout open " + Rt);
                var o = date.ShowAtAsync(opener);
                WatchSelectorsSoon();
                return Task.FromResult(true);
            }, 3000);

            Press(null, "month press held 600 ms", none, cx[0], cy, 600, 0, 0, 0, 0, 2500);
            Press(null, "month drag up 1.5 items at 0.2 px/ms, held", none, cx[0], cy, 0, 0, -1.5 * h, MoveMs(1.5 * h, 0.2), 400, 2500);
            Press(null, "month drag down 0.4 items at 0.2 px/ms, held", none, cx[0], cy, 0, 0, 0.4 * h, MoveMs(0.4 * h, 0.2), 400, 2500);
            Press(null, "month drag up 0.6 items at 0.2 px/ms, held", none, cx[0], cy, 0, 0, -0.6 * h, MoveMs(0.6 * h, 0.2), 400, 2500);
            Press(null, "day flick up 150 px in 100 ms", none, cx[1], cy + 100, 0, 0, -150, 100, 0, 4000);
            Press(null, "day flick down 300 px in 120 ms", none, cx[1], cy - 150, 0, 0, 300, 120, 0, 4000);
            Press(null, "day flick up 300 px in 100 ms, then a press 300 ms after", none, cx[1], cy + 150, 0, 0, -300, 100, 0, 300);
            Then(null, cx[1], cy, 600, 3000);
            Press(null, "day drag down 3 items at 0.3 px/ms, held", none, cx[1], cy - 1.5 * h, 0, 0, 3 * h, MoveMs(3 * h, 0.3), 400, 3000);
            Press(null, "year tap two below", none, cx[2], cy + 2 * h, 0, 0, 0, 0, 80, 3000);
            Press(null, "year tap two above", none, cx[2], cy - 2 * h, 0, 0, 0, 0, 80, 3000);
            Press(null, "year tap the selected", none, cx[2], cy, 0, 0, 0, 0, 80, 3000);
            Press(null, "month flick down 300 px in 80 ms (1)", none, cx[0], cy - 150, 0, 0, 300, 80, 0, 1500);
            Press(null, "month flick down 300 px in 80 ms (2)", none, cx[0], cy - 150, 0, 0, 300, 80, 0, 1500);
            Press(null, "month flick down 300 px in 80 ms (3)", none, cx[0], cy - 150, 0, 0, 300, 80, 0, 4000);
            Code("date close", () =>
            {
                Log("call", "DatePickerFlyout close " + Rt);
                date.Hide();
                return Task.FromResult(true);
            }, 2000);

            await RunScheduled(page, header);
        }

        /// <summary>Watches the open DatePickerFlyout's selectors once they are laid out (see <see cref="LoopingProbe"/>).</summary>
        private async void WatchSelectorsSoon()
        {
            List<LoopingSelector> found = null;
            for (var waited = 0; waited < 5000; waited += 20)
            {
                await Task.Delay(20);
                found = Selectors();
                if (found.Count == 3 && found.All(s => s.ActualHeight > 0 && Find(s, "ScrollViewer") != null))
                {
                    break;
                }
            }

            if (found == null || found.Count != 3)
            {
                Log("error", "the opened DatePickerFlyout has no three laid out LoopingSelectors");
                return;
            }

            for (var k = 0; k < found.Count; k++)
            {
                var s = found[k];
                var tag = "sel" + k;
                var sv = (ScrollViewer)Find(s, "ScrollViewer");
                var top = Bounds(s).Y;
                LogPointers(s, tag);
                LogGestures(s, tag);
                s.SelectionChanged += (o, e) => Log("event", tag + " SelectionChanged " + s.SelectedIndex + " " + Rt);
                sv.ViewChanging += (o, e) => Log("viewchanging", "next " + Num(e.NextView.VerticalOffset) + " final " + Num(e.FinalView.VerticalOffset) + " inertial " + e.IsInertial + " " + tag);
                sv.ViewChanged += (o, e) => Log("viewchanged", Num(sv.VerticalOffset) + " intermediate " + e.IsIntermediate + " " + tag);
                var states = new HashSet<LoopingSelectorItem>();
                WatchFrames(() =>
                {
                    var key = new List<double> { sv.VerticalOffset };
                    List<LoopingSelectorItem> items;
                    try
                    {
                        items = Items(s).OrderBy(i => Bounds(i).Y).ToList();
                    }
                    catch (ArgumentException)
                    {
                        return key.ToArray();
                    }

                    foreach (var item in items)
                    {
                        if (states.Add(item))
                        {
                            WatchItemStates(item, tag);
                        }

                        var root = Root(item);
                        var scale = root != null ? root.RenderTransform as ScaleTransform : null;
                        key.Add(Bounds(item).Y - top);
                        key.Add(root != null ? root.Opacity : double.NaN);
                        key.Add(scale != null ? scale.ScaleY : double.NaN);
                        key.Add(ItemNumber(item));
                    }

                    return key.ToArray();
                }, v =>
                {
                    var text = tag + " offset " + Num(v[0]) + " items";
                    for (var i = 1; i + 3 < v.Length + 1; i += 4)
                    {
                        text += " " + Num(v[i + 3]) + "@" + Num(v[i]) + ":" + Num(v[i + 1]) + (v[i + 2] != 1 && !double.IsNaN(v[i + 2]) ? "x" + Num(v[i + 2]) : "");
                    }

                    return text;
                });
            }

            Log("layout", "selectors watched " + Rt);
        }

        private void WatchItemStates(LoopingSelectorItem item, string tag)
        {
            var root = Root(item);
            if (root == null)
            {
                return;
            }

            foreach (var g in VisualStateManager.GetVisualStateGroups(root))
            {
                var group = g;
                group.CurrentStateChanged += (o, e) => Log("state", tag + " item " + Num(ItemNumber(item)) + " " + group.Name + " " + (e.OldState != null ? e.OldState.Name : "-") + " > " + (e.NewState != null ? e.NewState.Name : "-") + " " + Rt);
            }
        }

        /// <summary>The LoopingSelectors in the open popups, left to right.</summary>
        private static List<LoopingSelector> Selectors()
        {
            var list = new List<LoopingSelector>();
            foreach (var p in VisualTreeHelper.GetOpenPopups(Window.Current))
            {
                if (p.Child != null)
                {
                    Collect(p.Child, list);
                }
            }

            try
            {
                return list.Where(s => s.ActualWidth > 0).OrderBy(s => Bounds(s).X).ToList();
            }
            catch (ArgumentException)
            {
                return new List<LoopingSelector>();
            }
        }

        private static void Collect<T>(DependencyObject e, List<T> list) where T : DependencyObject
        {
            var t = e as T;
            if (t != null)
            {
                list.Add(t);
                return;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(e); i++)
            {
                Collect(VisualTreeHelper.GetChild(e, i), list);
            }
        }

        /// <summary>The selector's realised items.</summary>
        private static List<LoopingSelectorItem> Items(LoopingSelector s)
        {
            var list = new List<LoopingSelectorItem>();
            Collect(s, list);
            return list;
        }

        private static FrameworkElement Root(LoopingSelectorItem item)
        {
            return VisualTreeHelper.GetChildrenCount(item) > 0 ? VisualTreeHelper.GetChild(item, 0) as FrameworkElement : null;
        }

        /// <summary>The item's first text, as a number where it is one (the month, the day, the year), else NaN.</summary>
        private static double ItemNumber(LoopingSelectorItem item)
        {
            double n;
            return double.TryParse(ItemText(item), out n) ? n : double.NaN;
        }

        private static string ItemText(LoopingSelectorItem item)
        {
            var texts = new List<TextBlock>();
            Collect(item, texts);
            return texts.Count > 0 ? texts[0].Text : "";
        }
    }
}
