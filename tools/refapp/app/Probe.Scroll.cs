using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RefApp
{
    /// <summary>
    /// The scrolling probes (#113, B8): a ScrollViewer's deceleration after a flick, its end-of-content compression and
    /// its panning indicator, and a ListView item's press. A ScrollViewer takes the touch over once it pans: its pointer
    /// events end in PointerCaptureLost, not PointerReleased, so these probes don't count releases. They run on a
    /// schedule instead. The first touch is a tap on the page's header band, which logs "go" and flashes the band white
    /// for 250 ms, a mark for the recording. Every later touch is planned at a known time after it, and the app runs
    /// each touch's setup (a scroll offset set from code, a theme) 1.2 s before its touch is due, and logs it as a step.
    /// The run finishes 3 s after the last touch's planned end.
    ///
    /// Every frame, from CompositionTarget.Rendering, the log has the frame's RenderingTime (the animation clock) with
    /// the ScrollViewer's VerticalOffset and its vertical ScrollBar's panning indicator (the root's opacity, the thumb's
    /// bounds in the window, the ScrollBar's IndicatorMode) whenever one of them changes; and each ViewChanging (NextView,
    /// FinalView, IsInertial) and ViewChanged (IsIntermediate) as it comes.
    /// </summary>
    internal sealed partial class Probe
    {
        /// <summary>The header band the start tap lands on, above the probe's control.</summary>
        private const double HeaderHeight = 60;

        /// <summary>
        /// The gap after a list press or tap: the next touch's step comes 1.2 s before its press, so this leaves 800 ms
        /// for the tilt's return (352 to 371 ms) to end before the step cuts the log.
        /// </summary>
        private const double ListGap = 2000;

        /// <summary>What runs at a time after the start tap: a touch's setup, or something during a touch.</summary>
        private sealed class Scheduled
        {
            public double At;
            public string Label;
            public Func<Task> Action;

            /// <summary>A touch's setup, logged as a "step" (which the scripts cut the log at); else an "action" during one.</summary>
            public bool Touch;
        }

        private readonly List<Scheduled> _schedule = new List<Scheduled>();
        private int _touches;

        /// <summary>
        /// Deceleration: a ScrollViewer 740 view px tall over 100,000 px of content, flicked up (the finger moves up, the
        /// offset grows) 240 px in 60, 80, 120, 200 and 400 ms, and 120 px in 400 ms, three times each, from offset 2,000;
        /// then dragged 240 px in 400 ms and held 500 ms before the release. The content is a Grid of bars 100 px apart,
        /// which gives no snap points. Then the same as a StackPanel of 1,000 rows of 100 px, whose children are snap
        /// points (the first run, 20261115-1933-dev11, ended every flick on a multiple of 100): 240 px in 120 and in 400 ms,
        /// twice each. Each touch has 7 s to settle.
        /// </summary>
        private async Task Scroll(Grid host)
        {
            var sv = ScrollPage(1000);
            var stack = Bars(1000, true);
            var header = Header();
            var page = await Page(host, header, sv);
            if (!await WatchScroll(sv))
            {
                return;
            }

            const double x = 225;
            foreach (var flick in new[] { new[] { 240.0, 60 }, new[] { 240.0, 80 }, new[] { 240.0, 120 }, new[] { 240.0, 200 }, new[] { 240.0, 400 }, new[] { 120.0, 400 } })
            {
                for (var i = 0; i < 3; i++)
                {
                    var label = "flick " + Num(flick[0]) + " px in " + Num(flick[1]) + " ms, " + (i + 1);
                    Drag(sv, label, Offset(sv, 2000), x, 650, 650 - flick[0], flick[1], 0, 7000);
                }
            }

            Drag(sv, "drag 240 px in 400 ms, held 500 ms", Offset(sv, 2000), x, 650, 410, 400, 500, 7000);
            var toStack = Offset(sv, 2000);
            Func<Task> swap = async () =>
            {
                sv.Content = stack;
                await Task.Delay(300);
                sv.UpdateLayout();
                Log("setup", "content StackPanel, extent " + Num(sv.ExtentHeight));
                await toStack();
            };
            var first = true;
            foreach (var flick in new[] { new[] { 240.0, 120 }, new[] { 240.0, 400 } })
            {
                for (var i = 0; i < 2; i++)
                {
                    Drag(sv, "stackpanel flick " + Num(flick[0]) + " px in " + Num(flick[1]) + " ms, " + (i + 1), first ? swap : toStack, x, 650, 650 - flick[0], flick[1], 0, 7000);
                    first = false;
                }
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// The end of the content, and the indicator: a ScrollViewer 740 view px tall over 20 rows of 100 px (2,000 px, so
        /// 1,260 px of scrolling). First the indicator while a drag is held still, dark: the page is rendered 1.5 s into
        /// the hold (snapshot-dark.png and its tree). Then pulls past the top (the finger moves down) of 100, 200, 300 and
        /// 400 px in 500 ms, held 1.5 s; a pull of 200 px past the bottom; flicks of 240 px in 60 ms into the top and
        /// into the bottom, twice each. Then the page turns light and the held drag is rendered again (snapshot-light.png).
        /// Made to be recorded (run.ps1 -Record): the compression is drawn by DirectManipulation, which the app's values
        /// may not show.
        /// </summary>
        private async Task ScrollEnd(Grid host)
        {
            var sv = ScrollPage(20);
            var header = Header();
            var page = await Page(host, header, sv);
            if (!await WatchScroll(sv))
            {
                return;
            }

            const double x = 225;
            HeldSnapshot(sv, page, "dark");
            foreach (var pull in new[] { 100.0, 200, 300, 400 })
            {
                Drag(sv, "pull " + Num(pull) + " px past the top", Offset(sv, 0), x, 200, 200 + pull, 500, 1500, 3500);
            }

            Drag(sv, "pull 200 px past the bottom", Offset(sv, double.MaxValue), x, 650, 450, 500, 1500, 3500);
            for (var i = 0; i < 2; i++)
            {
                Drag(sv, "flick into the top, " + (i + 1), Offset(sv, 600), x, 300, 540, 60, 0, 4000);
            }

            for (var i = 0; i < 2; i++)
            {
                Drag(sv, "flick into the bottom, " + (i + 1), Offset(sv, double.MaxValue, -600), x, 650, 410, 60, 0, 4000);
            }

            HeldSnapshot(sv, page, "light");
            await RunScheduled(page, header);
        }

        /// <summary>
        /// A drag of 150 px up in 500 ms from offset 300, held still for 2.5 s; 1.5 s into the hold the page is rendered
        /// to snapshot-&lt;theme&gt;.png, with the ScrollViewer's tree beside it. For "light" the setup turns the page light
        /// first.
        /// </summary>
        private void HeldSnapshot(ScrollViewer sv, Grid page, string theme)
        {
            var setOffset = Offset(sv, 300);
            Func<Task> setup = async () =>
            {
                if (theme == "light")
                {
                    page.RequestedTheme = ElementTheme.Light;
                    page.Background = new SolidColorBrush(Colors.White);
                }

                await setOffset();
            };
            var start = _plannedMs;
            Drag(sv, "held drag, " + theme, setup, 225, 600, 450, 500, 2500, 3500);
            _schedule.Add(new Scheduled { At = start + 500 + 1500, Label = "snapshot " + theme, Action = () => Snapshot(page, sv, "snapshot-" + theme) });
        }

        /// <summary>
        /// The ListView item's press (#113): a ListView of 40 items, below the header band, with IsItemClickEnabled. Each
        /// item's TiltContainer (the template's Pressed state runs a PointerDownThemeAnimation on it) is read every frame
        /// for its projection and transform, and the items' states, ItemClick, SelectionChanged and gesture events are
        /// logged. Presses held 600 ms at the centre of items 1 to 3; at 5%, 25%, 75% and 95% across item 2, and 10% and
        /// 90% down it; taps of 80 ms, three, and of 150 ms, two, on item 4; presses held 300 ms on item 3 then dragged 80 px
        /// up in 200 ms (the list pans), twice, and once down (past the top); presses on item 3 flicked 80 px up in 150
        /// ms, twice; a press on item 2 moved 10 px right over 300 ms and released. The list is put back at its top before each.
        /// </summary>
        private async Task ListPress(Grid host)
        {
            var list = new ListView
            {
                Name = "list",
                IsItemClickEnabled = true,
                Margin = new Thickness(19, HeaderHeight + 20, 19, 0),
                ItemTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"{Binding}\" Style=\"{ThemeResource ListViewItemTextBlockStyle}\" /></DataTemplate>"),
            };
            // 40 items, about 1,570 px: more than the list's height, so that it pans.
            for (var i = 0; i < 40; i++)
            {
                list.Items.Add("item " + i);
            }

            var header = Header();
            var page = await Page(host, header, list);
            var items = Enumerable.Range(0, 7).Select(i => list.ContainerFromIndex(i) as ListViewItem).ToList();
            if (items.Any(item => item == null))
            {
                await Finish("the ListView made no container for one of its first 7 items");
                return;
            }

            var sv = Find(list, "ScrollViewer") as ScrollViewer;
            LogPointers(list, "list");
            list.ItemClick += (s, e) => Log("itemclick", e.ClickedItem as string ?? "?");
            list.SelectionChanged += (s, e) => Log("selection", "added " + string.Join(",", e.AddedItems) + " removed " + string.Join(",", e.RemovedItems));
            LogGestures(list, "list");
            foreach (var item in items)
            {
                var index = list.IndexFromContainer(item);
                item.Name = "item" + index;
                var tilt = Find(item, "TiltContainer");
                if (tilt == null)
                {
                    await Finish("no TiltContainer in item " + index + "'s template");
                    return;
                }

                var b = Bounds(item);
                var tb = Bounds(tilt);
                Log("layout", "item" + index + " " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " tilt " + Num(tb.X) + " " + Num(tb.Y) + " " + Num(tb.Width) + " " + Num(tb.Height));
                WatchStates(item);
                var t = tilt;
                WatchValue("projection item" + index, sample => Read(t.Projection, sample));
                WatchValue("transform item" + index, sample => Read(t.RenderTransform, sample));
            }

            Func<Task> top = () =>
            {
                if (sv != null)
                {
                    sv.ChangeView(null, 0, null, true);
                }

                return Task.FromResult(true);
            };
            Func<int, double, double, Windows.Foundation.Point> at = (i, fx, fy) =>
            {
                var b = Bounds(items[i]);
                return new Windows.Foundation.Point(b.X + b.Width * fx, b.Y + b.Height * fy);
            };

            for (var i = 1; i <= 3; i++)
            {
                var p = at(i, 0.5, 0.5);
                Drag(items[i], "hold item" + i + " centre", top, p.X, p.Y, p.Y, 0, 600, ListGap);
            }

            foreach (var f in new[] { new[] { 0.05, 0.5 }, new[] { 0.25, 0.5 }, new[] { 0.75, 0.5 }, new[] { 0.95, 0.5 }, new[] { 0.5, 0.1 }, new[] { 0.5, 0.9 } })
            {
                var p = at(2, f[0], f[1]);
                Drag(items[2], "hold item2 at " + Num(f[0]) + " " + Num(f[1]), top, p.X, p.Y, p.Y, 0, 600, ListGap);
            }

            for (var i = 0; i < 3; i++)
            {
                var p = at(4, 0.5, 0.5);
                Drag(items[4], "tap item4 80 ms, " + (i + 1), top, p.X, p.Y, p.Y, 0, 80, ListGap);
            }

            for (var i = 0; i < 2; i++)
            {
                var p = at(4, 0.5, 0.5);
                Drag(items[4], "tap item4 150 ms, " + (i + 1), top, p.X, p.Y, p.Y, 0, 150, ListGap);
            }

            for (var i = 0; i < 2; i++)
            {
                var p = at(3, 0.5, 0.5);
                Press(items[3], "hold item3 300 ms, then drag 80 px up (the list pans), " + (i + 1), top, p.X, p.Y, 300, 0, -80, 200, 300, 4000);
            }

            {
                var p = at(3, 0.5, 0.5);
                Press(items[3], "hold item3 300 ms, then drag 80 px down (past the top)", top, p.X, p.Y, 300, 0, 80, 200, 300, 4000);
            }

            for (var i = 0; i < 2; i++)
            {
                var p = at(3, 0.5, 0.5);
                Drag(items[3], "flick item3 80 px up in 150 ms, " + (i + 1), top, p.X, p.Y, p.Y - 80, 150, 0, 5000);
            }

            {
                var p = at(2, 0.5, 0.5);
                Press(items[2], "item2 moved 10 px right over 300 ms", top, p.X, p.Y, 0, 10, 0, 300, 300, 2500);
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// A vertical ScrollViewer below the header band, over <see cref="Bars"/> in a Grid, with no snap points.
        /// </summary>
        private static ScrollViewer ScrollPage(int rows)
        {
            return new ScrollViewer { Name = "scroll", Margin = new Thickness(0, HeaderHeight, 0, 0), Content = Bars(rows, false) };
        }

        /// <summary>
        /// Content of rows of 100 view px, each a grey bar 8 px tall at its top, from 19 px in to 60 px short of the right
        /// edge (the indicator runs down the right edge, clear of them): a StackPanel of rows, or one Grid with the bars
        /// placed by their margins, which has no snap points.
        /// </summary>
        private static Panel Bars(int rows, bool stack)
        {
            var brush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
            if (!stack)
            {
                var grid = new Grid { Height = rows * 100.0, VerticalAlignment = VerticalAlignment.Top };
                for (var i = 0; i < rows; i++)
                {
                    grid.Children.Add(new Rectangle { Height = 8, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(19, i * 100.0, 60, 0), Fill = brush });
                }

                return grid;
            }

            var content = new StackPanel();
            for (var i = 0; i < rows; i++)
            {
                var row = new Grid { Height = 100 };
                row.Children.Add(new Rectangle { Height = 8, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(19, 0, 60, 0), Fill = brush });
                content.Children.Add(row);
            }

            return content;
        }

        private static Border Header()
        {
            return new Border { Name = "header", Height = HeaderHeight, VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Colors.Black) };
        }

        /// <summary>
        /// A setup that sets the offset from code at once: the offset clamped to the content (double.MaxValue for the end),
        /// plus <paramref name="less"/>, so a negative one starts that far short of it.
        /// </summary>
        private Func<Task> Offset(ScrollViewer sv, double offset, double less = 0)
        {
            return () =>
            {
                var target = Math.Min(offset, sv.ScrollableHeight) + less;
                // ChangeView returns false when the offset is already there.
                var ok = sv.ChangeView(null, target, null, true);
                Log("setup", "offset " + Num(target) + (ok ? "" : " (ChangeView returned false)"));
                return Task.FromResult(true);
            };
        }

        /// <summary>
        /// A touch from (x, y0) to (x, y1) over moveMs, a step about every 10 ms, held still for holdMs at the end, then
        /// released; gapMs after the release before the next touch's press. A press with no move gives moveMs 0 and y1
        /// y0. The setup runs 1.2 s before the press.
        /// </summary>
        private void Drag(UIElement target, string label, Func<Task> setup, double x, double y0, double y1, double moveMs, double holdMs, double gapMs)
        {
            Press(target, label, setup, x, y0, 0, 0, y1 - y0, moveMs, holdMs, gapMs);
        }

        /// <summary>
        /// A touch at (x, y), held still for stillMs, then moved by (dx, dy) over moveMs, a step about every 10 ms, held
        /// for holdMs and released; gapMs after the release before the next press. The setup runs 1.2 s before the press.
        /// </summary>
        private void Press(UIElement target, string label, Func<Task> setup, double x, double y, double stillMs, double dx, double dy, double moveMs, double holdMs, double gapMs)
        {
            _touches++;
            var at = _plannedMs;
            _schedule.Add(new Scheduled { At = at - 1200, Label = label, Action = setup, Touch = true });
            Step("down", x, y, target);
            if (stillMs > 0)
            {
                Wait(stillMs);
            }

            if (moveMs > 0)
            {
                var n = Math.Max(2, (int)Math.Round(moveMs / 10));
                for (var i = 1; i <= n; i++)
                {
                    Wait(moveMs / n);
                    Step("move", x + dx * i / n, y + dy * i / n);
                }
            }

            if (holdMs > 0)
            {
                Wait(holdMs);
            }

            Step("up", x + dx, y + dy);
            Wait(gapMs);
        }

        /// <summary>
        /// Logs the ScrollViewer's pointer events, ViewChanging and ViewChanged, its states and its vertical ScrollBar's,
        /// and every frame its offset and indicator (see the class summary). False, with the run finished, when the
        /// template has no VerticalScrollBar or the ScrollBar's has no VerticalPanningRoot.
        /// </summary>
        private async Task<bool> WatchScroll(ScrollViewer sv)
        {
            var bar = Find(sv, "VerticalScrollBar") as ScrollBar;
            var root = bar != null ? Find(bar, "VerticalPanningRoot") : null;
            var thumb = bar != null ? Find(bar, "VerticalPanningThumb") : null;
            if (root == null || thumb == null)
            {
                await Finish("no " + (bar == null ? "VerticalScrollBar" : root == null ? "VerticalPanningRoot" : "VerticalPanningThumb") + " in the ScrollViewer's template");
                return false;
            }

            var b = Bounds(sv);
            Log("layout", "scroll " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " extent " + Num(sv.ExtentHeight) + " viewport " + Num(sv.ViewportHeight) + " scrollable " + Num(sv.ScrollableHeight));
            Log("layout", "snap points " + sv.VerticalSnapPointsType + " " + sv.VerticalSnapPointsAlignment + ", inertia " + sv.IsScrollInertiaEnabled + ", rail " + sv.IsVerticalRailEnabled + ", content " + (sv.Content != null ? sv.Content.GetType().Name : "none"));
            var bb = Bounds(bar);
            var fill = (thumb as Shape) != null ? ((Shape)thumb).Fill as SolidColorBrush : null;
            Log("layout", "bar " + Num(bb.X) + " " + Num(bb.Y) + " " + Num(bb.Width) + " " + Num(bb.Height) + " thumb fill " + (fill != null ? TreeDump.Colour(fill.Color) : "?") + " opacity " + Num(thumb.Opacity));
            LogPointers(sv, "scroll");
            LogGestures(sv, "scroll");
            WatchStates(sv);
            WatchStates(bar);
            sv.ViewChanging += (s, e) => Log("viewchanging", "next " + Num(e.NextView.VerticalOffset) + " final " + Num(e.FinalView.VerticalOffset) + " inertial " + e.IsInertial + (e.NextView.ZoomFactor != 1 ? " zoom " + Num(e.NextView.ZoomFactor) : ""));
            sv.ViewChanged += (s, e) => Log("viewchanged", Num(sv.VerticalOffset) + " intermediate " + e.IsIntermediate);

            // The values are compared as they are read, every frame; the line is built from them only when one changed.
            WatchFrames(() =>
            {
                try
                {
                    var tb = Bounds(thumb);
                    return new[] { sv.VerticalOffset, root.Opacity, bar.Opacity, tb.X, tb.Y, tb.Width, tb.Height, (int)bar.IndicatorMode };
                }
                catch (ArgumentException)
                {
                    return new[] { sv.VerticalOffset };
                }
            }, now => "offset " + Num(now[0]) + (now.Length == 1 ? " thumb not in the tree" : " root " + Num(now[1]) + " bar " + Num(now[2]) + " thumb " + Num(now[3]) + " " + Num(now[4]) + " " + Num(now[5]) + " " + Num(now[6]) + " mode " + (ScrollingIndicatorMode)(int)now[7]));
            return true;
        }

        /// <summary>Logs an element's pointer events, with handled ones too, by position in the window and timestamp, under a tag.</summary>
        private void LogPointers(UIElement element, string tag)
        {
            Action<string, PointerRoutedEventArgs> log = (what, e) =>
            {
                var point = e.GetCurrentPoint(null);
                Log(what, tag + " " + Num(point.Position.X) + " " + Num(point.Position.Y) + (e.Handled ? " handled" : "") + " ts " + point.Timestamp.ToString(CultureInfo.InvariantCulture));
            };
            element.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((s, e) => log("pressed", e)), true);
            element.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((s, e) => log("moved", e)), true);
            element.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((s, e) => log("released", e)), true);
            element.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((s, e) => log("capturelost", e)), true);
            element.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler((s, e) => log("canceled", e)), true);
        }

        private void LogGestures(UIElement element, string tag)
        {
            element.AddHandler(UIElement.TappedEvent, new TappedEventHandler((s, e) => Log("gesture", tag + " Tapped" + (e.Handled ? " handled" : ""))), true);
            element.AddHandler(UIElement.HoldingEvent, new HoldingEventHandler((s, e) => Log("gesture", tag + " Holding " + e.HoldingState + (e.Handled ? " handled" : ""))), true);
        }

        /// <summary>
        /// Plays the planned touches after a start tap on the header (see the class summary): the tap first, then each
        /// scheduled action at its time after the tap's press; finishes 3 s after the last touch's planned end. Gives up 3
        /// minutes after ready if the tap never comes.
        /// </summary>
        private async Task RunScheduled(Grid page, Border header)
        {
            // The start tap goes first in the plan: everything planned so far moves later by its length.
            const double tapMs = 80, afterTapMs = 2000;
            var shift = tapMs + afterTapMs;
            var touches = new List<Planned>(_plan);
            _plan.Clear();
            var planned = _plannedMs;
            _plannedMs = 0;
            Step("down", 30, HeaderHeight / 2, header);
            Wait(tapMs);
            Step("up", 30, HeaderHeight / 2);
            Wait(afterTapMs);
            _plan.AddRange(touches);
            _plannedMs += planned;

            var t0 = double.NaN;
            var go = new TaskCompletionSource<bool>();
            header.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(async (s, e) =>
            {
                if (!double.IsNaN(t0))
                {
                    return;
                }

                t0 = _clock.Elapsed.TotalMilliseconds;
                Log("go", "tap at " + Num(e.GetCurrentPoint(null).Position.X) + " " + Num(e.GetCurrentPoint(null).Position.Y));
                header.Background = new SolidColorBrush(Colors.White);
                Log("flash", "on");
                go.TrySetResult(true);
                await Task.Delay(250);
                header.Background = new SolidColorBrush(Colors.Black);
                Log("flash", "off");
            }), true);

            var timeout = TimeSpan.FromMinutes(3).TotalMilliseconds + _plannedMs + 10000;
            if (!await StartTouches("scheduled: a start tap, then " + _touches + " touches over " + Num(_plannedMs) + " ms;", _touches + 1, timeout))
            {
                return;
            }

            if (await Task.WhenAny(go.Task, Task.Delay(TimeSpan.FromMinutes(3))) != go.Task)
            {
                await Finish("timed out: the start tap never came");
                return;
            }

            foreach (var item in _schedule.OrderBy(s => s.At))
            {
                var wait = t0 + shift + item.At - _clock.Elapsed.TotalMilliseconds;
                if (wait > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(wait));
                }

                Log(item.Touch ? "step" : "action", item.Label);
                try
                {
                    await item.Action();
                }
                catch (Exception ex)
                {
                    Log("error", item.Label + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            var end = t0 + _plannedMs + 3000 - _clock.Elapsed.TotalMilliseconds;
            if (end > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(end));
            }

            await Finish("sequence done");
        }

        /// <summary>Renders the page to &lt;name&gt;.png and writes the ScrollViewer's tree, relative to the page, to &lt;name&gt;.json.</summary>
        private async Task Snapshot(Grid page, ScrollViewer sv, string name)
        {
            var rtb = await TreeDump.RenderPngAsync(_out, name + ".png", page);
            var j = new Json().Obj();
            j.Prop("file", name + ".png");
            j.Key("pixels").Arr().Val(rtb.PixelWidth).Val(rtb.PixelHeight).EndArr();
            j.Prop("verticalOffset", sv.VerticalOffset);
            TreeDump.Bounds(j, sv, page, "elementInStage");
            j.Key("tree");
            TreeDump.Write(j, sv, page);
            j.EndObj();
            await Write(name + ".json", j.ToString());
            Log("snapshot", name + ".png " + rtb.PixelWidth + " x " + rtb.PixelHeight + " offset " + Num(sv.VerticalOffset));
        }
    }
}
