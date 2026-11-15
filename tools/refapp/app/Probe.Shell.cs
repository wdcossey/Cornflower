using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// The shell's probes (#114): Pivot's header strip and Hub's header against their content, per frame, under swipes and
    /// taps, run on a schedule after a start tap on the header band (<see cref="RunScheduled"/>), made to be recorded
    /// (run.ps1 -Record). Every frame something changed, the log has the frame's RenderingTime with the control's
    /// ScrollViewer's HorizontalOffset and the parts that move: for a Pivot, its SelectedIndex, the header panel's and
    /// the items presenter's transforms, and each header item's left edge in the window and its own transform; for a
    /// Hub, its header's left edge and transform, and each section's left edge. Each ViewChanging comes as it comes.
    /// </summary>
    internal sealed partial class Probe
    {
        /// <summary>
        /// A Pivot of four items ("one" to "four"), below the header band. Swipes left (the finger moves 200 px from
        /// right to left in 150 ms on the content) from item 0, four times, the last wrapping from "four" to "one", then
        /// the same again; a swipe right from "one", which wraps to "four"; a tap on the header "two" and on "three",
        /// each from item 0; and a drag of 100 px left, held 300 ms and let go. SelectedIndex is set to 0 from code
        /// before the sets that start there.
        /// </summary>
        private async Task PivotProbe(Grid host)
        {
            var pivot = new Pivot { Name = "pivot", Title = "CORNFLOWER", Margin = new Thickness(0, HeaderHeight, 0, 0) };
            foreach (var name in new[] { "one", "two", "three", "four" })
            {
                pivot.Items.Add(new PivotItem { Header = name, Content = new TextBlock { Text = name + " pivot item", FontSize = 40 } });
            }

            var header = Header();
            var page = await Page(host, header, pivot);
            var sv = Find(pivot, "ScrollViewer") as ScrollViewer;
            var headerPanel = Find(pivot, "Header");
            var items = Find(pivot, "PivotItemPresenter");
            if (sv == null || headerPanel == null || items == null)
            {
                await Finish("the Pivot's template has no " + (sv == null ? "ScrollViewer" : headerPanel == null ? "Header" : "PivotItemPresenter"));
                return;
            }

            LogPointers(pivot, "pivot");
            LogGestures(pivot, "pivot");
            pivot.SelectionChanged += (s, e) => Log("selection", "index " + pivot.SelectedIndex);
            sv.ViewChanging += (s, e) => Log("viewchanging", "next " + Num(e.NextView.HorizontalOffset) + " final " + Num(e.FinalView.HorizontalOffset) + " inertial " + e.IsInertial);
            sv.ViewChanged += (s, e) => Log("viewchanged", Num(sv.HorizontalOffset) + " intermediate " + e.IsIntermediate);
            foreach (var item in HeaderItems(headerPanel))
            {
                var b = Bounds(item);
                Log("layout", "header " + (item.Content ?? "?") + " " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height));
            }

            var ib = Bounds(items);
            Log("layout", "items " + Num(ib.X) + " " + Num(ib.Y) + " " + Num(ib.Width) + " " + Num(ib.Height) + " scroll extent " + Num(sv.ExtentWidth) + " viewport " + Num(sv.ViewportWidth));
            WatchFrames(() =>
            {
                var text = new StringBuilder();
                text.Append("offset ").Append(Num(sv.HorizontalOffset)).Append(" index ").Append(pivot.SelectedIndex);
                text.Append(" panel ").Append(Value(headerPanel.RenderTransform)).Append(" items ").Append(Value(items.RenderTransform));
                text.Append(" itemsAt ").Append(Num(Left(items)));
                foreach (var item in HeaderItems(headerPanel))
                {
                    var presenter = Find(item, "ContentPresenter");
                    text.Append(" | ").Append(item.Content).Append(' ').Append(Num(Left(item))).Append(" op ").Append(Num(item.Opacity));
                    if (presenter != null)
                    {
                        text.Append(" cp ").Append(Value(presenter.RenderTransform));
                    }
                }

                return text.ToString();
            });

            var y = 450.0;
            Func<Task> none = () => Task.FromResult(true);
            Func<Task> first = () =>
            {
                pivot.SelectedIndex = 0;
                Log("setup", "SelectedIndex 0");
                return Task.FromResult(true);
            };
            for (var round = 1; round <= 2; round++)
            {
                for (var i = 0; i < 4; i++)
                {
                    Swipe(pivot, "swipe left, round " + round + ", from " + i, i == 0 ? first : none, 380, y, 0, 0, 3000, -200, 150);
                }
            }

            Swipe(pivot, "swipe right from 0", first, 70, y, 0, 0, 3000, 200, 150);
            var headers = HeaderItems(headerPanel).ToList();
            foreach (var k in new[] { 1, 2 })
            {
                if (k < headers.Count)
                {
                    var b = Bounds(headers[k]);
                    Drag(headers[k], "tap header " + headers[k].Content + " from 0", first, b.X + b.Width / 2, b.Y + b.Height / 2, b.Y + b.Height / 2, 0, 80, 3000);
                }
            }

            Swipe(pivot, "drag 100 px left, held 300 ms", first, 300, y, 0, 300, 3000, -100, 400);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// A Hub with the header "hub" and four sections, below the header band. From offset 0: swipes left (200 px in 150
        /// ms on a section) three times in a row, then a swipe right; then a drag of 150 px left over 500 ms, held 300 ms
        /// and let go. The offset is set to 0 from code before each set.
        /// </summary>
        private async Task HubProbe(Grid host)
        {
            var hub = new Hub { Name = "hub", Header = "hub", Margin = new Thickness(0, HeaderHeight, 0, 0) };
            for (var i = 1; i <= 4; i++)
            {
                hub.Sections.Add(new HubSection
                {
                    Header = "section " + i,
                    ContentTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"section " + i + " content\" FontSize=\"30\" /></DataTemplate>"),
                });
            }

            var header = Header();
            var page = await Page(host, header, hub);
            var sv = Find(hub, "ScrollViewer") as ScrollViewer;
            var headerHost = Find(hub, "HeaderHost");
            if (sv == null || headerHost == null)
            {
                await Finish("the Hub's template has no " + (sv == null ? "ScrollViewer" : "HeaderHost"));
                return;
            }

            LogPointers(hub, "hub");
            sv.ViewChanging += (s, e) => Log("viewchanging", "next " + Num(e.NextView.HorizontalOffset) + " final " + Num(e.FinalView.HorizontalOffset) + " inertial " + e.IsInertial);
            sv.ViewChanged += (s, e) => Log("viewchanged", Num(sv.HorizontalOffset) + " intermediate " + e.IsIntermediate);
            var hb = Bounds(headerHost);
            Log("layout", "header " + Num(hb.X) + " " + Num(hb.Y) + " " + Num(hb.Width) + " " + Num(hb.Height) + " scroll extent " + Num(sv.ExtentWidth) + " viewport " + Num(sv.ViewportWidth));
            foreach (var section in hub.Sections)
            {
                var b = Bounds(section);
                Log("layout", "section " + section.Header + " " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height));
            }

            WatchFrames(() =>
            {
                var text = new StringBuilder();
                text.Append("offset ").Append(Num(sv.HorizontalOffset));
                text.Append(" header ").Append(Num(Left(headerHost))).Append(' ').Append(Value(headerHost.RenderTransform));
                foreach (var section in hub.Sections)
                {
                    text.Append(" | ").Append(section.Header).Append(' ').Append(Num(Left(section)));
                }

                return text.ToString();
            });

            var y = 500.0;
            Func<Task> start = () =>
            {
                var ok = sv.ChangeView(0, null, null, true);
                Log("setup", "offset 0" + (ok ? "" : " (ChangeView returned false)"));
                return Task.FromResult(true);
            };
            Func<Task> none = () => Task.FromResult(true);
            for (var i = 0; i < 3; i++)
            {
                Swipe(hub, "swipe left " + (i + 1), i == 0 ? start : none, 380, y, 0, 0, 3000, -200, 150);
            }

            Swipe(hub, "swipe right", none, 70, y, 0, 0, 3000, 200, 150);
            Swipe(hub, "drag 150 px left, held 300 ms", start, 300, y, 0, 300, 3000, -150, 500);
            await RunScheduled(page, header);
        }

        /// <summary>A horizontal touch: <see cref="Press"/> at (x, y), still for stillMs, moved by dx over moveMs, held holdMs and let go; gapMs before the next.</summary>
        private void Swipe(UIElement target, string label, Func<Task> setup, double x, double y, double stillMs, double holdMs, double gapMs, double dx, double moveMs)
        {
            Press(target, label, setup, x, y, stillMs, dx, 0, moveMs, holdMs, gapMs);
        }

        /// <summary>The Pivot's header items, in the panel's order.</summary>
        private static IEnumerable<PivotHeaderItem> HeaderItems(DependencyObject panel)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(panel); i++)
            {
                var item = VisualTreeHelper.GetChild(panel, i) as PivotHeaderItem;
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        /// <summary>The element's left edge in the window, or NaN when it isn't in the tree.</summary>
        private static double Left(FrameworkElement element)
        {
            try
            {
                return element.TransformToVisual(null).TransformPoint(new Point(0, 0)).X;
            }
            catch (ArgumentException)
            {
                return double.NaN;
            }
        }

        /// <summary>A transform's values, "none" for none or the identity (<see cref="AnyTransform"/>).</summary>
        private static string Value(Transform transform)
        {
            var t = AnyTransform(transform);
            return t.Length > 0 ? "[" + t + "]" : "none";
        }

        /// <summary>Logs, every frame its text changed, the frame's RenderingTime and the text; stops when the run finishes.</summary>
        private void WatchFrames(Func<string> read)
        {
            string last = null;
            EventHandler<object> frame = null;
            frame = (s, e) =>
            {
                if (_finished)
                {
                    CompositionTarget.Rendering -= frame;
                    return;
                }

                var text = read();
                if (text == last)
                {
                    return;
                }

                last = text;
                var args = e as RenderingEventArgs;
                Log("frame", (args != null ? "rt " + args.RenderingTime.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + " " : "") + text);
            };
            CompositionTarget.Rendering += frame;
        }
    }
}
