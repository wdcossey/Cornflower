using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// Tier 4's reads (#211), run on a schedule after a start tap on the header band (<see cref="RunScheduled"/>):
    /// FlipView's release, snap, ends, a SelectedIndex set from code and a press while it moves, and a vertical one;
    /// GridView's panel, an item's press and the check mode; grouped lists' headers; and SemanticZoom's jump list.
    /// </summary>
    internal sealed partial class Probe
    {
        private const string XamlNs = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";

        /// <summary>Every descendant of the element of type T, in breadth-first order.</summary>
        private static List<T> All<T>(DependencyObject root) where T : DependencyObject
        {
            var found = new List<T>();
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var t = node as T;
                if (t != null && node != root)
                {
                    found.Add(t);
                }

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return found;
        }

        private static string Box(FrameworkElement e)
        {
            var b = Bounds(e);
            return Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height);
        }

        private static string Brush(Brush brush)
        {
            var solid = brush as SolidColorBrush;
            return solid != null ? solid.Color + (solid.Opacity != 1 ? " op " + Num(solid.Opacity) : "") : brush == null ? "null" : brush.GetType().Name;
        }

        /// <summary>The template's named elements and their types, one log line.</summary>
        private void LogParts(Control control, string tag)
        {
            var named = All<FrameworkElement>(control).Where(e => e.Name.Length > 0).Select(e => e.Name + ":" + e.GetType().Name);
            Log("layout", tag + " parts " + string.Join(" ", named));
        }

        /// <summary>A grey panel for a FlipView item, a shade a step lighter for each index, with its name in big text.</summary>
        private static Grid FlipPanel(int i)
        {
            var v = (byte)(60 + 25 * i);
            return new Grid { Background = new SolidColorBrush(Color.FromArgb(255, v, v, v)), Children = { new TextBlock { Text = "item " + i, FontSize = 40, Margin = new Thickness(12) } } };
        }

        private Func<Task> SelectFlip(FlipView flip, int index)
        {
            return () =>
            {
                flip.SelectedIndex = index;
                Log("setup", "SelectedIndex " + index);
                return Task.FromResult(true);
            };
        }

        /// <summary>A FlipView of five items below the header band, 400 tall, its layout, events and every frame logged.</summary>
        private async Task<FlipView> PlaceFlip(Grid host, bool vertical, Border header)
        {
            var flip = new FlipView { Name = "flip", Height = 400, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, HeaderHeight + 20, 0, 0) };
            if (vertical)
            {
                flip.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate " + XamlNs + "><VirtualizingStackPanel AreScrollSnapPointsRegular=\"True\" Orientation=\"Vertical\" /></ItemsPanelTemplate>");
            }

            for (var i = 0; i < 5; i++)
            {
                flip.Items.Add(FlipPanel(i));
            }

            flip.SelectedIndex = 1;
            await Page(host, header, flip);
            var sv = Find(flip, "ScrollingHost") as ScrollViewer;
            if (sv == null)
            {
                await Finish("the FlipView's template has no ScrollingHost");
                return null;
            }

            LogParts(flip, "flip");
            Log("layout", "flip " + Box(flip) + " sv " + Box(sv) + " extent " + Num(sv.ExtentWidth) + " x " + Num(sv.ExtentHeight) + " viewport " + Num(sv.ViewportWidth) + " x " + Num(sv.ViewportHeight)
                + " hmode " + sv.HorizontalScrollMode + " vmode " + sv.VerticalScrollMode + " hsnap " + sv.HorizontalSnapPointsType + " vsnap " + sv.VerticalSnapPointsType);
            // In release.py's form (tools/motion), the offset along the FlipView's axis.
            Log("layout", "flip scroll extent " + Num(vertical ? sv.ExtentHeight : sv.ExtentWidth) + " viewport " + Num(vertical ? sv.ViewportHeight : sv.ViewportWidth));
            Func<ScrollViewerView, double> along = v => vertical ? v.VerticalOffset : v.HorizontalOffset;
            var containers = Enumerable.Range(0, 5).Select(i => flip.ContainerFromIndex(i) as FlipViewItem).ToList();
            foreach (var c in containers.Where(c => c != null))
            {
                Log("layout", "item " + flip.IndexFromContainer(c) + " " + Box(c) + " margin " + c.Margin + " padding " + c.Padding + " background " + Brush(c.Background));
            }

            LogPointers(flip, "flip");
            LogGestures(flip, "flip");
            flip.SelectionChanged += (s, e) => Log("selection", "index " + flip.SelectedIndex);
            sv.ViewChanging += (s, e) => Log("viewchanging", "next " + Num(along(e.NextView)) + " final " + Num(along(e.FinalView)) + " inertial " + e.IsInertial);
            sv.ViewChanged += (s, e) => Log("viewchanged", Num(vertical ? sv.VerticalOffset : sv.HorizontalOffset) + " intermediate " + e.IsIntermediate);
            Func<FrameworkElement, double> top = el =>
            {
                try
                {
                    return el.TransformToVisual(null).TransformPoint(new Point(0, 0)).Y;
                }
                catch (ArgumentException)
                {
                    return double.NaN;
                }
            };
            WatchFrames(() =>
            {
                var key = new List<double> { vertical ? sv.VerticalOffset : sv.HorizontalOffset, flip.SelectedIndex };
                foreach (var i in new[] { 0, 1, 4 })
                {
                    var c = flip.ContainerFromIndex(i) as FrameworkElement;
                    key.Add(c != null ? Left(c) : double.NaN);
                    key.Add(c != null ? top(c) : double.NaN);
                }

                return key.ToArray();
            }, k => "offset " + Num(k[0]) + " index " + k[1] + " item0 " + Num(k[2]) + "," + Num(k[3]) + " item1 " + Num(k[4]) + "," + Num(k[5]) + " item4 " + Num(k[6]) + "," + Num(k[7]));
            return flip;
        }

        /// <summary>
        /// FlipView (#211): a horizontal FlipView of five items, item 1 selected from code before each touch:
        /// - drags left of 100 to 300 px at 0.5 px a ms, held still 400 ms and let go;
        /// - drags left of 60 px let go while moving at 0.1 to 1 px a ms, and of 120 px at 0.2 and 0.3;
        /// - swipes left (200 px in 150 ms), three times, for the snap frame by frame;
        /// - at item 0, a drag right of 150 px held and let go, and a swipe right; at item 4, the same left;
        /// - a swipe left and a second press held 600 ms 100 ms after its release; and a tap 100 ms after;
        /// - SelectedIndex 1 to 3 and back to 1 from code.
        /// </summary>
        private async Task FlipViewProbe(Grid host)
        {
            var header = Header();
            var flip = await PlaceFlip(host, false, header);
            if (flip == null)
            {
                return;
            }

            var page = _page;
            const double y = 280, gap = 3000;
            var one = SelectFlip(flip, 1);
            foreach (var d in new[] { 100.0, 200, 215, 225, 235, 250, 300 })
            {
                Swipe(flip, "slow left " + Num(d) + " px, held", one, 400, y, -d, MoveMs(d, 0.5), 400, gap);
            }

            foreach (var v in new[] { 0.1, 0.2, 0.3, 0.4, 0.6, 1.0 })
            {
                Swipe(flip, "left 60 px let go at " + Num(v) + " px/ms", one, 400, y, -60, MoveMs(60, v), 0, gap);
            }

            foreach (var v in new[] { 0.2, 0.3 })
            {
                Swipe(flip, "left 120 px let go at " + Num(v) + " px/ms", one, 400, y, -120, MoveMs(120, v), 0, gap);
            }

            for (var k = 0; k < 3; k++)
            {
                Swipe(flip, "swipe left " + k, one, 380, y, -200, 150, 0, gap);
            }

            Swipe(flip, "at item 0, right 150 px, held", SelectFlip(flip, 0), 50, y, 150, 300, 400, gap);
            Swipe(flip, "at item 0, swipe right", SelectFlip(flip, 0), 50, y, 200, 150, 0, gap);
            Swipe(flip, "at item 4, left 150 px, held", SelectFlip(flip, 4), 400, y, -150, 300, 400, gap);
            Swipe(flip, "at item 4, swipe left", SelectFlip(flip, 4), 380, y, -200, 150, 0, gap);
            Swipe(flip, "swipe left, then a press held 600 ms 100 ms after the release", one, 380, y, -200, 150, 0, 100);
            Then(flip, 225, y, 600, gap);
            Swipe(flip, "swipe left, then a tap 100 ms after the release", one, 380, y, -200, 150, 0, 100);
            Then(flip, 225, y, 80, gap);
            Code("SelectedIndex 1 from code", one, 3000);
            Code("SelectedIndex 3 from code", SelectFlip(flip, 3), 3000);
            Code("SelectedIndex 1 from code, again", one, 3000);
            await RunScheduled(page, header);
        }

        /// <summary>FlipView with a vertical panel (#211): item 1 selected before each; drags up held, let go moving, and a swipe up.</summary>
        private async Task FlipVerticalProbe(Grid host)
        {
            var header = Header();
            var flip = await PlaceFlip(host, true, header);
            if (flip == null)
            {
                return;
            }

            var page = _page;
            const double x = 225, gap = 3000;
            var one = SelectFlip(flip, 1);
            foreach (var d in new[] { 100.0, 150, 250 })
            {
                Drag(flip, "slow up " + Num(d) + " px, held", one, x, 440, 440 - d, MoveMs(d, 0.5), 400, gap);
            }

            foreach (var v in new[] { 0.2, 0.6 })
            {
                Drag(flip, "up 60 px let go at " + Num(v) + " px/ms", one, x, 440, 380, MoveMs(60, v), 0, gap);
            }

            Drag(flip, "swipe up", one, x, 440, 240, 150, 0, gap);
            Swipe(flip, "slow left 250 px, held", one, 400, 280, -250, 500, 400, gap);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// GridView (#211): 20 of <see cref="Catalog.Tile"/>'s tiles below the header band. Its panel, the items' places and
        /// every state change of the first six are logged, and item 1's tilt every frame. A tap and a held press on items
        /// in Single; a drag up (the scroll's direction); then Multiple, with taps on items 0 and 3, and snapshots of the
        /// page dark and light; then Single again with item 1 selected, snapshot dark.
        /// </summary>
        private async Task GridViewProbe(Grid host)
        {
            var grid = new GridView { Name = "grid", Margin = new Thickness(19, HeaderHeight + 20, 19, 0) };
            for (var i = 0; i < 20; i++)
            {
                grid.Items.Add(Catalog.Tile("tile " + i));
            }

            var header = Header();
            var page = await Page(host, header, grid);
            var sv = Find(grid, "ScrollViewer") as ScrollViewer;
            var items = Enumerable.Range(0, 6).Select(i => grid.ContainerFromIndex(i) as GridViewItem).ToList();
            if (sv == null || items.Any(c => c == null))
            {
                await Finish("the GridView has no ScrollViewer or made no container for one of its first six items");
                return;
            }

            var panel = grid.ItemsPanelRoot;
            var wrap = panel as ItemsWrapGrid;
            Log("layout", "grid " + Box(grid) + " panel " + (panel != null ? panel.GetType().Name + " " + Box(panel) : "none")
                + (wrap != null ? " orientation " + wrap.Orientation + " max " + wrap.MaximumRowsOrColumns + " itemwidth " + Num(wrap.ItemWidth) + " itemheight " + Num(wrap.ItemHeight) : "")
                + " extent " + Num(sv.ExtentWidth) + " x " + Num(sv.ExtentHeight) + " hmode " + sv.HorizontalScrollMode + " vmode " + sv.VerticalScrollMode);
            for (var i = 0; i < 8; i++)
            {
                var c = grid.ContainerFromIndex(i) as GridViewItem;
                if (c != null)
                {
                    Log("layout", "item " + i + " " + Box(c) + " margin " + c.Margin + " padding " + c.Padding + " hca " + c.HorizontalContentAlignment + " vca " + c.VerticalContentAlignment);
                }
            }

            LogParts(items[0], "item0");
            foreach (var c in items)
            {
                c.Name = "item" + grid.IndexFromContainer(c);
                WatchStates(c);
            }

            var tilt = Find(items[1], "TiltContainer");
            if (tilt != null)
            {
                WatchValue("projection item1", sample => Read(tilt.Projection, sample));
            }

            LogPointers(grid, "grid");
            LogGestures(grid, "grid");
            grid.SelectionChanged += (s, e) => Log("selection", "index " + grid.SelectedIndex + ", " + grid.SelectedItems.Count + " selected");
            sv.ViewChanging += (s, e) => Log("viewchanging", "next " + Num(e.NextView.HorizontalOffset) + "," + Num(e.NextView.VerticalOffset) + " inertial " + e.IsInertial);
            Func<int, Point> at = i =>
            {
                var b = Bounds(items[i]);
                return new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
            };
            Func<ListViewSelectionMode, ElementTheme, Func<Task>> mode = (m, theme) => async () =>
            {
                sv.ChangeView(null, 0, null, true);
                grid.SelectionMode = m;
                page.RequestedTheme = theme;
                page.Background = new SolidColorBrush(theme == ElementTheme.Light ? Colors.White : Colors.Black);
                Log("setup", "offset 0, SelectionMode " + m + ", " + theme);
                await Task.FromResult(true);
            };
            var single = mode(ListViewSelectionMode.Single, ElementTheme.Dark);
            var p1 = at(1);
            var p2 = at(2);
            Drag(grid, "Single: tap item 1", single, p1.X, p1.Y, p1.Y, 0, 80, ListGap);
            Drag(grid, "Single: press item 2 held 600 ms", single, p2.X, p2.Y, p2.Y, 0, 600, ListGap);
            Drag(grid, "Single: press item 1 held 600 ms 6 px from its left", single, p1.X - 54, p1.Y, p1.Y, 0, 600, ListGap);
            Drag(grid, "drag up 200 px", single, p2.X, p2.Y + 200, p2.Y, 400, 400, 3000);
            var multiple = mode(ListViewSelectionMode.Multiple, ElementTheme.Dark);
            var p0 = at(0);
            var p3 = at(3);
            Drag(grid, "Multiple: tap item 0", multiple, p0.X, p0.Y, p0.Y, 0, 80, ListGap);
            Drag(grid, "Multiple: tap item 3", async () => await Task.FromResult(true), p3.X, p3.Y, p3.Y, 0, 80, ListGap);
            Code("snapshot Multiple dark", () => Snapshot(page, sv, "gridview-multiple-dark"), 3000);
            Code("Multiple light", mode(ListViewSelectionMode.Multiple, ElementTheme.Light), 2000);
            Code("snapshot Multiple light", () => Snapshot(page, sv, "gridview-multiple-light"), 3000);
            Code("Single dark, item 1", async () =>
            {
                await single();
                grid.SelectedIndex = 1;
            }, 2000);
            Code("snapshot Single dark", () => Snapshot(page, sv, "gridview-single-dark"), 3000);
            Code("Single light", async () =>
            {
                await mode(ListViewSelectionMode.Single, ElementTheme.Light)();
                grid.SelectedIndex = 1;
            }, 2000);
            Code("snapshot Single light", () => Snapshot(page, sv, "gridview-single-light"), 3000);
            await RunScheduled(page, header);
        }

        /// <summary>A group of the probes' grouped lists: a letter and its items (none for an empty group).</summary>
        private sealed class Letter
        {
            public string Key { get; set; }
            public List<string> Items { get; set; }
        }

        /// <summary>Groups a to h, five items each, with b and e empty (for the jump list's grey tiles).</summary>
        private static List<Letter> Letters()
        {
            return "abcdefgh".Select(ch => new Letter { Key = ch.ToString(), Items = ch == 'b' || ch == 'e' ? new List<string>() : Enumerable.Range(1, 5).Select(i => ch + " item " + i).ToList() }).ToList();
        }

        private static CollectionViewSource Grouped()
        {
            return new CollectionViewSource { Source = Letters(), IsSourceGrouped = true, ItemsPath = new PropertyPath("Items") };
        }

        private static GroupStyle LetterHeaders()
        {
            return new GroupStyle
            {
                HidesIfEmpty = true,
                HeaderTemplate = (DataTemplate)XamlReader.Load("<DataTemplate " + XamlNs + "><TextBlock Text=\"{Binding Key}\" Style=\"{ThemeResource GroupHeaderTextBlockStyle}\" /></DataTemplate>"),
            };
        }

        /// <summary>Logs the list's header items and GroupItems, their places, margins and paddings.</summary>
        private void LogHeaders(ListViewBase list, string tag)
        {
            foreach (var h in All<ContentControl>(list).Where(c => c is ListViewHeaderItem || c is GridViewHeaderItem))
            {
                Log("layout", tag + " header " + h.GetType().Name + " " + (h.Content is Letter ? ((Letter)h.Content).Key : "?") + " " + Box(h) + " margin " + h.Margin + " padding " + h.Padding + " hca " + h.HorizontalContentAlignment);
            }

            Log("layout", tag + " GroupItems " + All<GroupItem>(list).Count + " panel " + (list.ItemsPanelRoot != null ? list.ItemsPanelRoot.GetType().Name : "none"));
            var first = list.ContainerFromIndex(0) as FrameworkElement;
            if (first != null)
            {
                Log("layout", tag + " item 0 " + Box(first));
            }
        }

        /// <summary>
        /// Grouped lists (#211, for #131): a grouped ListView over a grouped GridView, each 330 tall, groups a to h (b and e
        /// empty, hidden). Their header items' places and the first header's states and tilt are logged, and every frame
        /// the first two headers' tops with each list's offset (do they stick?). A press held 600 ms on each list's first
        /// header; a drag up of 150 px held 1 s on each; snapshots dark, scrolled from code (131 and 85) and light. Recorded:
        /// a header's place in the window doesn't follow DirectManipulation's pan.
        /// </summary>
        private async Task GroupedProbe(Grid host)
        {
            var list = new ListView { Name = "list", Height = 330, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(19, HeaderHeight + 10, 19, 0), ItemsSource = Grouped().View };
            list.GroupStyle.Add(LetterHeaders());
            var grid = new GridView { Name = "grid", Height = 330, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(19, HeaderHeight + 350, 19, 0), ItemsSource = Grouped().View };
            grid.GroupStyle.Add(LetterHeaders());
            var header = Header();
            var page = await Page(host, header, list, grid);
            var lsv = Find(list, "ScrollViewer") as ScrollViewer;
            var gsv = Find(grid, "ScrollViewer") as ScrollViewer;
            var lh = All<ListViewHeaderItem>(list);
            var gh = All<GridViewHeaderItem>(grid);
            if (lsv == null || gsv == null || lh.Count < 2 || gh.Count < 2)
            {
                await Finish("missing: a ScrollViewer or two header items (list " + lh.Count + ", grid " + gh.Count + ")");
                return;
            }

            LogHeaders(list, "list");
            LogHeaders(grid, "grid");
            LogParts(lh[0], "listheader0");
            lh[0].Name = "listheader0";
            gh[0].Name = "gridheader0";
            WatchStates(lh[0]);
            WatchStates(gh[0]);
            var lt = Find(lh[0], "TiltContainer");
            var gt = Find(gh[0], "TiltContainer");
            if (lt != null)
            {
                WatchValue("projection listheader0", s => Read(lt.Projection, s));
            }

            if (gt != null)
            {
                WatchValue("projection gridheader0", s => Read(gt.Projection, s));
            }

            Func<FrameworkElement, double> topOf = el =>
            {
                try
                {
                    return el.TransformToVisual(null).TransformPoint(new Point(0, 0)).Y;
                }
                catch (ArgumentException)
                {
                    return double.NaN;
                }
            };
            WatchFrames(() => new[] { lsv.VerticalOffset, topOf(lh[0]), topOf(lh[1]), gsv.VerticalOffset, topOf(gh[0]), topOf(gh[1]) },
                k => "list offset " + Num(k[0]) + " h0 " + Num(k[1]) + " h1 " + Num(k[2]) + " | grid offset " + Num(k[3]) + " h0 " + Num(k[4]) + " h1 " + Num(k[5]));
            LogPointers(list, "list");
            LogPointers(grid, "grid");
            Func<ElementTheme, Func<Task>> reset = theme => () =>
            {
                lsv.ChangeView(null, 0, null, true);
                gsv.ChangeView(null, 0, null, true);
                page.RequestedTheme = theme;
                page.Background = new SolidColorBrush(theme == ElementTheme.Light ? Colors.White : Colors.Black);
                Log("setup", "offsets 0, " + theme);
                return Task.FromResult(true);
            };
            var dark = reset(ElementTheme.Dark);
            var lb = Bounds(lh[0]);
            var gb = Bounds(gh[0]);
            var lc = new Point(lb.X + Math.Min(lb.Width, 60) / 2, lb.Y + lb.Height / 2);
            var gc = new Point(gb.X + Math.Min(gb.Width, 60) / 2, gb.Y + gb.Height / 2);
            Drag(lh[0], "press the list's first header, held 600 ms", dark, lc.X, lc.Y, lc.Y, 0, 600, ListGap);
            Drag(gh[0], "press the grid's first header, held 600 ms", dark, gc.X, gc.Y, gc.Y, 0, 600, ListGap);
            Drag(list, "drag the list up 150 px, held 1 s", dark, 300, lb.Y + 250, lb.Y + 100, 500, 1000, 3000);
            Drag(grid, "drag the grid up 150 px, held 1 s", dark, 300, gb.Y + 250, gb.Y + 100, 500, 1000, 3000);
            Code("snapshot list dark", () => Snapshot(page, lsv, "grouped-dark"), 3000);
            Code("offsets 131 and 85 from code", () =>
            {
                lsv.ChangeView(null, 131, null, true);
                gsv.ChangeView(null, 85, null, true);
                return Task.FromResult(true);
            }, 2000);
            Code("snapshot scrolled dark", () => Snapshot(page, lsv, "grouped-scrolled-dark"), 3000);
            Code("dark, offsets 0", dark, 2000);
            Code("light", reset(ElementTheme.Light), 2000);
            Code("snapshot list light", () => Snapshot(page, lsv, "grouped-light"), 3000);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// The pinned header as the next one arrives (#225), made to be recorded: the grouped ListView alone (330 tall, as
        /// in <see cref="GroupedProbe"/>; header c comes 225.8 under header a). The offset is set from code 1 px at a time
        /// from 180 to 240, each held about 250 ms, logged; then, from 0, a slow drag up of 280 px over 8 s, held 1 s. The
        /// offset is logged every frame.
        /// </summary>
        private async Task GroupedPushProbe(Grid host)
        {
            var list = new ListView { Name = "list", Height = 330, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(19, HeaderHeight + 10, 19, 0), ItemsSource = Grouped().View };
            list.GroupStyle.Add(LetterHeaders());
            var header = Header();
            var page = await Page(host, header, list);
            var lsv = Find(list, "ScrollViewer") as ScrollViewer;
            if (lsv == null)
            {
                await Finish("missing: the list's ScrollViewer");
                return;
            }

            LogHeaders(list, "list");
            WatchFrames(() => new[] { lsv.VerticalOffset }, k => "list offset " + Num(k[0]));
            var top = Bounds(list).Y;
            Code("offsets 180 to 240 from code, 1 px every 250 ms", async () =>
            {
                for (var y = 180; y <= 240; y++)
                {
                    lsv.ChangeView(null, y, null, true);
                    Log("setup", "offset " + y);
                    await Task.Delay(250);
                }
            }, 18000);
            Drag(list, "drag the list up 280 px over 8 s, held 1 s", () =>
            {
                lsv.ChangeView(null, 0, null, true);
                return Task.FromResult(true);
            }, 300, top + 310, top + 30, 8000, 1000, 3000);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// SemanticZoom (#211), made to be recorded: a grouped ListView (groups a to h, b and e empty and hidden) zoomed
        /// in, and a GridView of the groups zoomed out, in the jump list's usual tiles (the converters' brushes, 99 x 99,
        /// as the 8.1 docs' sample). Before the touches the zoomed-out view is opened from code to read where its tiles
        /// are. Then: a tap on header a (opens); a tap on tile d (closes, lands on d); a tap on header d, then Back; the
        /// light theme, a tap on header a, then Back; IsZoomOutButtonEnabled logged. With <paramref name="locked"/>
        /// (-Probe semanticzoomlocked) only CanChangeViews false and a tap on header a. Its events, the popups open and their trees are logged.
        /// </summary>
        private async Task SemanticZoomProbe(Grid host, bool locked = false)
        {
            var cvs = Grouped();
            var list = new ListView { Name = "zoomedin", ItemsSource = cvs.View, Margin = new Thickness(19, 0, 19, 0) };
            list.GroupStyle.Add(LetterHeaders());
            var jump = new GridView { Name = "zoomedout", ItemsSource = cvs.View.CollectionGroups, Margin = new Thickness(19, 0, 19, 0) };
            jump.ItemTemplate = (DataTemplate)XamlReader.Load("<DataTemplate " + XamlNs + "><Grid><Grid.Resources><JumpListItemBackgroundConverter x:Key=\"bg\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" /><JumpListItemForegroundConverter x:Key=\"fg\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" /></Grid.Resources>"
                + "<Border Background=\"{Binding Converter={StaticResource bg}}\" Width=\"99\" Height=\"99\" Margin=\"6\" HorizontalAlignment=\"Left\"><TextBlock Text=\"{Binding Group.Key}\" Foreground=\"{Binding Converter={StaticResource fg}}\" FontSize=\"48\" Padding=\"6\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Bottom\" /></Border></Grid></DataTemplate>");
            var sz = new SemanticZoom { Name = "sz", ZoomedInView = list, ZoomedOutView = jump, Margin = new Thickness(0, HeaderHeight, 0, 0) };
            var header = Header();
            var page = await Page(host, header, sz);
            LogParts(sz, "sz");
            Log("layout", "sz " + Box(sz) + " IsZoomOutButtonEnabled " + sz.IsZoomOutButtonEnabled + " CanChangeViews " + sz.CanChangeViews + " IsZoomedInViewActive " + sz.IsZoomedInViewActive);
            LogHeaders(list, "zoomedin");
            sz.ViewChangeStarted += (s, e) => Log("event", "ViewChangeStarted in-active " + sz.IsZoomedInViewActive + " source " + (e.SourceItem != null ? Describe(e.SourceItem.Item) : "-") + " dest " + (e.DestinationItem != null ? Describe(e.DestinationItem.Item) : "-"));
            sz.ViewChangeCompleted += (s, e) => Log("event", "ViewChangeCompleted in-active " + sz.IsZoomedInViewActive);
            var lsv = Find(list, "ScrollViewer") as ScrollViewer;
            HardwareButtons.BackPressed += (s, e) =>
            {
                Log("back", "BackPressed handled-on-entry " + e.Handled + " in-active " + sz.IsZoomedInViewActive + " popups " + VisualTreeHelper.GetOpenPopups(Window.Current).Count);
                if (sz.IsZoomedInViewActive)
                {
                    e.Handled = true;
                }
            };
            LogPointers(page, "page");
            var popupCount = 0;
            WatchFrames(() =>
            {
                var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                var key = new List<double> { popups.Count, sz.IsZoomedInViewActive ? 1 : 0, lsv != null ? lsv.VerticalOffset : double.NaN, list.Opacity, jump.Opacity };
                foreach (var p in popups.Where(p => p.Child is FrameworkElement))
                {
                    key.Add(p.Child.Opacity);
                    key.Add(Left((FrameworkElement)p.Child));
                    AddKey(key, p.Child.RenderTransform);
                    key.Add(p.Child.Projection != null ? 1 : 0);
                }

                return key.ToArray();
            }, k => "popups " + k[0] + " in-active " + k[1] + " list offset " + Num(k[2]) + " list op " + Num(k[3]) + " jump op " + Num(k[4]) + (k.Length > 5 ? " child op " + Num(k[5]) + " left " + Num(k[6]) : ""));

            // Opened from code once, before the touches, to find the tiles.
            sz.IsZoomedInViewActive = false;
            await Task.Delay(1500);
            LogPopups("opened from code");
            var tiles = new Dictionary<string, Rect>();
            foreach (var c in All<GridViewItem>(jump))
            {
                var g = c.Content as ICollectionViewGroup;
                var l = g != null ? g.Group as Letter : null;
                if (l != null)
                {
                    tiles[l.Key] = Bounds(c);
                }
            }

            sz.IsZoomedInViewActive = true;
            await Task.Delay(1500);
            Log("layout", "tiles " + string.Join("; ", tiles.Select(t => t.Key + " " + Num(t.Value.X) + " " + Num(t.Value.Y) + " " + Num(t.Value.Width) + " " + Num(t.Value.Height))));
            var lh = All<ListViewHeaderItem>(list);
            if (lh.Count == 0 || (!locked && !tiles.ContainsKey("d")))
            {
                await Finish("missing: a header in the zoomed-in list (" + lh.Count + ") or tile d");
                return;
            }

            var hb = Bounds(lh[0]);
            var ha = new Point(hb.X + 20, hb.Y + hb.Height / 2);
            var td = locked ? default(Rect) : tiles["d"];
            Func<ElementTheme, bool, Func<Task>> reset = (theme, can) => () =>
            {
                sz.IsZoomedInViewActive = true;
                if (lsv != null)
                {
                    lsv.ChangeView(null, 0, null, true);
                }

                sz.CanChangeViews = can;
                page.RequestedTheme = theme;
                page.Background = new SolidColorBrush(theme == ElementTheme.Light ? Colors.White : Colors.Black);
                Log("setup", "zoomed in, offset 0, " + theme + ", CanChangeViews " + can);
                return Task.FromResult(true);
            };
            Func<string, Func<Task>> logPopups = what => () =>
            {
                LogPopups(what);
                return Task.FromResult(true);
            };
            if (locked)
            {
                // On its own: in the first run the app closed at this tap (20261118-2231-dev11-partial), so the log was lost.
                Drag(lh[0], "CanChangeViews false: tap header a", reset(ElementTheme.Dark, false), ha.X, ha.Y, ha.Y, 0, 80, 3000);
                Code("after the tap", logPopups("CanChangeViews false, after the tap"), 1500);
                await RunScheduled(page, header);
                return;
            }

            Drag(lh[0], "tap header a", reset(ElementTheme.Dark, true), ha.X, ha.Y, ha.Y, 0, 80, 2500);
            Code("read the jump list", logPopups("open after header a"), 1500);
            Drag(null, "tap tile d", async () => await Task.FromResult(true), td.X + td.Width / 2, td.Y + td.Height / 2, td.Y + td.Height / 2, 0, 80, 3000);
            Code("read the list", () =>
            {
                LogHeaders(list, "after tile d, offset " + (lsv != null ? Num(lsv.VerticalOffset) : "?"));
                return Task.FromResult(true);
            }, 1500);
            Drag(lh[0], "tap header a, then Back", reset(ElementTheme.Dark, true), ha.X, ha.Y, ha.Y, 0, 80, 2500);
            _touches++;
            _plan.Add(new Planned { Verb = "key", X = BackKey });
            Wait(3000);
            Drag(lh[0], "light: tap header a", reset(ElementTheme.Light, true), ha.X, ha.Y, ha.Y, 0, 80, 2500);
            Code("read the jump list, light", logPopups("open after header a, light"), 1500);
            _touches++;
            _plan.Add(new Planned { Verb = "key", X = BackKey });
            Wait(3000);
            await RunScheduled(page, header);
        }

        private static string Describe(object item)
        {
            var g = item as ICollectionViewGroup;
            var l = (g != null ? g.Group : item) as Letter;
            return l != null ? "group " + l.Key : item != null ? item.ToString() : "-";
        }

        /// <summary>Every open popup's tree, in brief: each element's type, name, bounds, and its brushes and font where it has them.</summary>
        private void LogPopups(string what)
        {
            var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
            Log("popups", what + ": " + popups.Count + " open");
            foreach (var p in popups.Where(p => p.Child is FrameworkElement))
            {
                var lines = new StringBuilder();
                foreach (var e in new[] { (FrameworkElement)p.Child }.Concat(All<FrameworkElement>(p.Child)).Take(80))
                {
                    lines.Append(e.GetType().Name).Append(e.Name.Length > 0 ? "#" + e.Name : "").Append(' ').Append(Box(e));
                    var border = e as Border;
                    var panel = e as Panel;
                    var text = e as TextBlock;
                    var control = e as Control;
                    if (border != null)
                    {
                        lines.Append(" bg ").Append(Brush(border.Background));
                    }

                    if (panel != null && panel.Background != null)
                    {
                        lines.Append(" bg ").Append(Brush(panel.Background));
                    }

                    if (control != null && control.Background != null)
                    {
                        lines.Append(" bg ").Append(Brush(control.Background));
                    }

                    if (text != null)
                    {
                        lines.Append(" '").Append(text.Text).Append("' fg ").Append(Brush(text.Foreground)).Append(' ').Append(text.FontFamily.Source).Append(' ').Append(Num(text.FontSize)).Append(' ').Append(text.FontWeight.Weight);
                    }

                    if (e.Opacity != 1)
                    {
                        lines.Append(" op ").Append(Num(e.Opacity));
                    }

                    lines.Append("; ");
                }

                Log("popup", lines.ToString());
            }
        }
    }
}
