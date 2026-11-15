using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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
            var parts = await PlacePivot(host, new[] { "one", "two", "three", "four" });
            if (parts == null)
            {
                return;
            }

            var pivot = parts.Pivot;
            var headerPanel = parts.HeaderPanel;
            var y = 450.0;
            Func<Task> none = () => Task.FromResult(true);
            var first = Select(pivot, 0);
            for (var round = 1; round <= 2; round++)
            {
                for (var i = 0; i < 4; i++)
                {
                    Swipe(pivot, "swipe left, round " + round + ", from " + i, i == 0 ? first : none, 380, y, dx: -200, moveMs: 150, holdMs: 0, gapMs: 3000);
                }
            }

            Swipe(pivot, "swipe right from 0", first, 70, y, dx: 200, moveMs: 150, holdMs: 0, gapMs: 3000);
            var headers = HeaderItems(headerPanel).ToList();
            foreach (var k in new[] { 1, 2 })
            {
                if (k < headers.Count)
                {
                    var b = Bounds(headers[k]);
                    Drag(headers[k], "tap header " + headers[k].Content + " from 0", first, b.X + b.Width / 2, b.Y + b.Height / 2, b.Y + b.Height / 2, 0, 80, 3000);
                }
            }

            Swipe(pivot, "drag 100 px left, held 300 ms", first, 300, y, dx: -100, moveMs: 400, holdMs: 300, gapMs: 3000);
            await RunScheduled(parts.Page, parts.Band);
        }

        /// <summary>
        /// Headers wider than the screen (#136, for #121): a Pivot of four items whose headers come to far more than the
        /// 450 px view, the last ("supercalifragilisticexpialidocious") wider than the view on its own. Swipes left from
        /// item 0, four times, the last wrapping; a swipe right from item 0, to the wide one; then, from each item in
        /// turn, a tap on the header that runs off the right edge (on the part on the screen), and from the wide item a
        /// tap on its own header, which runs off the screen.
        /// </summary>
        private async Task PivotWideProbe(Grid host)
        {
            var parts = await PlacePivot(host, new[] { "favourites", "everything", "recent", "supercalifragilisticexpialidocious" });
            if (parts == null)
            {
                return;
            }

            var pivot = parts.Pivot;
            var y = 450.0;
            Func<Task> none = () => Task.FromResult(true);
            for (var i = 0; i < 4; i++)
            {
                Swipe(pivot, "swipe left from " + i, i == 0 ? Select(pivot, 0) : none, 380, y, dx: -200, moveMs: 150, holdMs: 0, gapMs: 3000);
            }

            Swipe(pivot, "swipe right from 0", Select(pivot, 0), 70, y, dx: 200, moveMs: 150, holdMs: 0, gapMs: 3000);
            for (var k = 0; k < parts.AtRest.Count; k++)
            {
                // The header that starts on the screen and ends past its right edge, with item k selected.
                var cut = parts.AtRest[k].FirstOrDefault(h => h.Value.X < 440 && h.Value.X + h.Value.Width > 450);
                if (cut.Key != null)
                {
                    var b = cut.Value;
                    var x = (b.X + 449) / 2;
                    // Item k isn't selected when the touches are checked, so the press is checked against the Pivot.
                    Drag(pivot, "tap header " + cut.Key + ", partly off the screen, from " + k + " at " + Num(x), Select(pivot, k), x, b.Y + b.Height / 2, b.Y + b.Height / 2, 0, 80, 3000);
                }
            }

            Rect wide;
            if (parts.AtRest[3].TryGetValue((string)((PivotItem)pivot.Items[3]).Header, out wide))
            {
                Drag(pivot, "tap the selected header, wider than the screen, from 3", Select(pivot, 3), 300, wide.Y + wide.Height / 2, wide.Y + wide.Height / 2, 0, 80, 3000);
            }

            await RunScheduled(parts.Page, parts.Band);
        }

        /// <summary>
        /// A Pivot of two items (#136, for #121): does it still wrap? Swipes left from item 0 and then from item 1, twice
        /// round; a swipe right from item 0 and from item 1; and from each item a tap on the other's header, where it is
        /// with that item selected.
        /// </summary>
        private async Task PivotTwoProbe(Grid host)
        {
            var parts = await PlacePivot(host, new[] { "one", "two" });
            if (parts == null)
            {
                return;
            }

            var pivot = parts.Pivot;
            var y = 450.0;
            Func<Task> none = () => Task.FromResult(true);
            for (var round = 1; round <= 2; round++)
            {
                for (var i = 0; i < 2; i++)
                {
                    Swipe(pivot, "swipe left, round " + round + ", from " + i, i == 0 ? Select(pivot, 0) : none, 380, y, dx: -200, moveMs: 150, holdMs: 0, gapMs: 3000);
                }
            }

            for (var i = 0; i < 2; i++)
            {
                Swipe(pivot, "swipe right from " + i, Select(pivot, i), 70, y, dx: 200, moveMs: 150, holdMs: 0, gapMs: 3000);
            }

            var names = pivot.Items.OfType<PivotItem>().Select(p => (string)p.Header).ToList();
            for (var k = 0; k < 2; k++)
            {
                var other = names[1 - k];
                Rect b;
                if (parts.AtRest[k].TryGetValue(other, out b))
                {
                    var x = b.X + (Math.Min(b.X + b.Width, 449) - b.X) / 2;
                    Drag(pivot, "tap header " + other + " from " + k, Select(pivot, k), x, b.Y + b.Height / 2, b.Y + b.Height / 2, 0, 80, 3000);
                }
            }

            await RunScheduled(parts.Page, parts.Band);
        }

        /// <summary>What <see cref="PlacePivot"/> placed: the Pivot, its parts, the page and its header band.</summary>
        private sealed class PivotParts
        {
            public Pivot Pivot;
            public ScrollViewer ScrollViewer;
            public FrameworkElement HeaderPanel;
            public FrameworkElement Items;
            public Grid Page;
            public Border Band;

            /// <summary>For each item selected from code and settled, each header's bounds in the window, by its text.</summary>
            public readonly List<Dictionary<string, Rect>> AtRest = new List<Dictionary<string, Rect>>();
        }

        /// <summary>A setup that selects item <paramref name="index"/> from code.</summary>
        private Func<Task> Select(Pivot pivot, int index)
        {
            return () =>
            {
                pivot.SelectedIndex = index;
                Log("setup", "SelectedIndex " + index);
                return Task.FromResult(true);
            };
        }

        /// <summary>
        /// A Pivot with an item for each name, below the header band: placed; its headers' places read with each item
        /// selected from code in turn, 1.5 s after each (logged as "rest" lines); item 0 selected again; then its pointer
        /// events, selection and view changes logged and every frame watched (see the class summary). Null, with the run
        /// finished, when its template lacks a part.
        /// </summary>
        private async Task<PivotParts> PlacePivot(Grid host, string[] names)
        {
            var pivot = new Pivot { Name = "pivot", Title = "CORNFLOWER", Margin = new Thickness(0, HeaderHeight, 0, 0) };
            foreach (var name in names)
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
                return null;
            }

            var parts = new PivotParts { Pivot = pivot, ScrollViewer = sv, HeaderPanel = headerPanel, Items = items, Page = page, Band = header };
            for (var k = 0; k < names.Length; k++)
            {
                pivot.SelectedIndex = k;
                await Task.Delay(1500);
                page.UpdateLayout();
                var rest = new Dictionary<string, Rect>();
                var line = new StringBuilder("selected " + k + ", panel " + Value(headerPanel.RenderTransform) + ", headers in the panel's order:");
                foreach (var item in HeaderItems(headerPanel))
                {
                    var b = Bounds(item);
                    var text = item.Content as string ?? "?";
                    // A header shown twice keeps its first place.
                    if (!rest.ContainsKey(text))
                    {
                        rest[text] = b;
                    }

                    line.Append(' ').Append(text).Append(' ').Append(Num(b.X)).Append(' ').Append(Num(b.Y)).Append(' ').Append(Num(b.Width)).Append(' ').Append(Num(b.Height)).Append(" op ").Append(Num(item.Opacity)).Append(';');
                }

                Log("rest", line.ToString());
                parts.AtRest.Add(rest);
            }

            pivot.SelectedIndex = 0;
            await Task.Delay(1500);
            page.UpdateLayout();
            var hp = Bounds(headerPanel);
            Log("layout", "panel " + Num(hp.X) + " " + Num(hp.Y) + " " + Num(hp.Width) + " " + Num(hp.Height) + " clip " + (headerPanel.Clip != null ? "yes" : "none"));
            WatchPivot(parts);
            return parts;
        }

        /// <summary>Logs the Pivot's pointer events, gestures, selection and view changes and its layout, and watches every frame (see the class summary).</summary>
        private void WatchPivot(PivotParts parts)
        {
            var pivot = parts.Pivot;
            var sv = parts.ScrollViewer;
            var headerPanel = parts.HeaderPanel;
            var items = parts.Items;
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

            Log("layout", "items " + string.Join(" ", pivot.Items.OfType<PivotItem>().Select(i => i.Header)));
            var ib = Bounds(items);
            Log("layout", "items " + Num(ib.X) + " " + Num(ib.Y) + " " + Num(ib.Width) + " " + Num(ib.Height) + " scroll extent " + Num(sv.ExtentWidth) + " viewport " + Num(sv.ViewportWidth));
            var presenters = new Dictionary<PivotHeaderItem, FrameworkElement>();
            Func<PivotHeaderItem, FrameworkElement> presenterOf = item =>
            {
                FrameworkElement p;
                if (!presenters.TryGetValue(item, out p))
                {
                    // Kept only once found: an item not yet templated is looked at again next frame.
                    p = Find(item, "ContentPresenter");
                    if (p != null)
                    {
                        presenters[item] = p;
                    }
                }

                return p;
            };
            WatchFrames(() =>
            {
                var key = new List<double> { sv.HorizontalOffset, pivot.SelectedIndex, Left(items) };
                AddKey(key, headerPanel.RenderTransform);
                AddKey(key, items.RenderTransform);
                foreach (var item in HeaderItems(headerPanel))
                {
                    key.Add(Left(item));
                    key.Add(item.Opacity);
                    var presenter = presenterOf(item);
                    AddKey(key, presenter != null ? presenter.RenderTransform : null);
                }

                return key.ToArray();
            }, key =>
            {
                var text = new StringBuilder();
                text.Append("offset ").Append(Num(sv.HorizontalOffset)).Append(" index ").Append(pivot.SelectedIndex);
                text.Append(" panel ").Append(Value(headerPanel.RenderTransform)).Append(" items ").Append(Value(items.RenderTransform));
                text.Append(" itemsAt ").Append(Num(Left(items)));
                foreach (var item in HeaderItems(headerPanel))
                {
                    var presenter = presenterOf(item);
                    text.Append(" | ").Append(item.Content).Append(' ').Append(Num(Left(item))).Append(" op ").Append(Num(item.Opacity));
                    if (presenter != null)
                    {
                        text.Append(" cp ").Append(Value(presenter.RenderTransform));
                    }
                }

                return text.ToString();
            });
        }

        /// <summary>
        /// A Hub with the header "hub" and four sections, below the header band. From offset 0: swipes left (200 px in 150
        /// ms on a section) three times in a row, then a swipe right; then a drag of 150 px left over 500 ms, held 300 ms
        /// and let go. The offset is set to 0 from code before each set.
        ///
        /// With <paramref name="image"/> (-Probe hubimage, #136, for #122) the Hub's Background is an ImageBrush of a
        /// striped picture the probe draws (<see cref="StripeImage"/>), and the log also has, every frame, the template's
        /// Background grid, WrapBackground and MainBackground: their left edges, widths, transforms and the grid's
        /// opacity; and the Hub's visual states (BackgroundImageLoadStates). Then a fourth swipe left, at the end, and a
        /// swipe right from there are added.
        /// </summary>
        private async Task HubProbe(Grid host, bool image = false)
        {
            var hub = new Hub { Name = "hub", Header = "hub", Margin = new Thickness(0, HeaderHeight, 0, 0) };
            if (image)
            {
                hub.Background = new ImageBrush { ImageSource = await StripeImage(1600, 800) };
            }

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

            // The background's parts: none of them read without an image, as the first runs had none.
            var bg = image ? Find(hub, "Background") : null;
            var wrap = image ? Find(hub, "WrapBackground") : null;
            var main = image ? Find(hub, "MainBackground") : null;
            if (image && (bg == null || wrap == null || main == null))
            {
                await Finish("the Hub's template has no " + (bg == null ? "Background" : wrap == null ? "WrapBackground" : "MainBackground"));
                return;
            }

            LogPointers(hub, "hub");
            if (image)
            {
                WatchStates(hub);
                var brush = (ImageBrush)hub.Background;
                brush.ImageOpened += (s, e) => Log("image", "opened");
                brush.ImageFailed += (s, e) => Log("image", "failed: " + e.ErrorMessage);
                Log("layout", "background brush stretch " + brush.Stretch + " align " + brush.AlignmentX + " " + brush.AlignmentY + " opacity " + Num(bg.Opacity));
                foreach (var part in new[] { bg, wrap, main })
                {
                    var b = Bounds(part);
                    Log("layout", part.Name + " " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + " " + Value(part.RenderTransform) + " visibility " + part.Visibility);
                }
            }

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
                var key = new List<double> { sv.HorizontalOffset, Left(headerHost) };
                AddKey(key, headerHost.RenderTransform);
                foreach (var section in hub.Sections)
                {
                    key.Add(Left(section));
                }

                if (image)
                {
                    foreach (var part in new[] { bg, wrap, main })
                    {
                        key.Add(Left(part));
                        key.Add(part.ActualWidth);
                        AddKey(key, part.RenderTransform);
                    }

                    key.Add(bg.Opacity);
                    key.Add((double)wrap.Visibility);
                }

                return key.ToArray();
            }, key =>
            {
                var text = new StringBuilder();
                text.Append("offset ").Append(Num(sv.HorizontalOffset));
                text.Append(" header ").Append(Num(Left(headerHost))).Append(' ').Append(Value(headerHost.RenderTransform));
                foreach (var section in hub.Sections)
                {
                    text.Append(" | ").Append(section.Header).Append(' ').Append(Num(Left(section)));
                }

                if (image)
                {
                    foreach (var part in new[] { bg, wrap, main })
                    {
                        text.Append(" | ").Append(part.Name).Append(' ').Append(Num(Left(part))).Append(" w ").Append(Num(part.ActualWidth)).Append(' ').Append(Value(part.RenderTransform));
                    }

                    text.Append(" | op ").Append(Num(bg.Opacity)).Append(" wrap ").Append(wrap.Visibility);
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
                Swipe(hub, "swipe left " + (i + 1), i == 0 ? start : none, 380, y, dx: -200, moveMs: 150, holdMs: 0, gapMs: 3000);
            }

            Swipe(hub, "swipe right", none, 70, y, dx: 200, moveMs: 150, holdMs: 0, gapMs: 3000);
            Swipe(hub, "drag 150 px left, held 300 ms", start, 300, y, dx: -150, moveMs: 500, holdMs: 300, gapMs: 3000);
            if (image)
            {
                // To the end and past it: the offset set to the end from code, then a swipe left there and one back.
                Func<Task> end = () =>
                {
                    var ok = sv.ChangeView(sv.ScrollableWidth, null, null, true);
                    Log("setup", "offset " + Num(sv.ScrollableWidth) + (ok ? "" : " (ChangeView returned false)"));
                    return Task.FromResult(true);
                };
                Swipe(hub, "swipe left at the end", end, 380, y, dx: -200, moveMs: 150, holdMs: 0, gapMs: 3000);
                Swipe(hub, "swipe right from the end", end, 70, y, dx: 200, moveMs: 150, holdMs: 0, gapMs: 3000);
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// A picture to tell places by, written to the app's folder as hub-background.png and loaded from there: width x
        /// height pixels, its hue going once round the colour wheel from left to right, with a black column every 50 px
        /// and a white one every 250 px, each 4 px wide, and a black row every 100 px.
        /// </summary>
        private async Task<ImageSource> StripeImage(int width, int height)
        {
            var pixels = new byte[width * height * 4];
            for (var x = 0; x < width; x++)
            {
                var h = 6.0 * x / width;
                var f = h - Math.Floor(h);
                var c = new[] { new[] { 1, f, 0 }, new[] { 1 - f, 1, 0 }, new[] { 0, 1, f }, new[] { 0, 1 - f, 1 }, new[] { f, 0, 1 }, new[] { 1, 0, 1 - f } }[(int)h % 6];
                for (var y = 0; y < height; y++)
                {
                    var i = (y * width + x) * 4;
                    var v = x % 250 < 4 ? 255 : x % 50 < 4 || y % 100 < 4 ? 0 : -1;
                    pixels[i] = (byte)(v >= 0 ? v : 64 + 160 * c[2]);
                    pixels[i + 1] = (byte)(v >= 0 ? v : 64 + 160 * c[1]);
                    pixels[i + 2] = (byte)(v >= 0 ? v : 64 + 160 * c[0]);
                    pixels[i + 3] = 255;
                }
            }

            var file = await _out.CreateFileAsync("hub-background.png", CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96, 96, pixels);
                await encoder.FlushAsync();
            }

            Log("layout", "background image " + width + " x " + height + " px, hub-background.png");
            return new BitmapImage(new Uri("ms-appdata:///local/hub-background.png"));
        }

        /// <summary>
        /// The page's app bar under real touches (#136, for #133), made to be recorded: the shell draws the bar, so its
        /// pixels and its motion are the recording's, and the app logs only the bar's events. A CommandBar as the page's
        /// BottomAppBar with three primary commands (add; favorite, a toggle; delete) and two secondary ones ("first menu
        /// item", "second menu item"), compact. The touches land where the 2026-11-09 screenshots draw things (view px):
        /// closed, the circles' centres at 150, 224 and 298 across and 768 down and the ellipsis at 420, 752; open, the
        /// circles at 546 down, the menu lines at 629 and 690 and the ellipsis at 420, 527; the minimal bar 773.75 to 800,
        /// its ellipsis at 420, 786. In order: add held 1 s on the closed bar; a tap on the ellipsis (opens); add held 1 s
        /// on the open bar; the first menu line held 1 s; a tap on favorite on the open bar; a tap on the ellipsis of the
        /// open bar; a tap on the page above the open bar; a tap on add on the closed bar; then minimal: a tap on the
        /// bar away from the ellipsis, a tap on its ellipsis, and a tap on the ellipsis of the open bar. Each touch's setup
        /// puts the bar in the state the touch needs from code (IsOpen, ClosedDisplayMode), 1.2 s before it, and logs it.
        /// </summary>
        private async Task AppBarProbe(Grid host)
        {
            var frame = Window.Current.Content as Frame;
            var shell = frame != null ? frame.Content as Page : null;
            if (shell == null)
            {
                await Finish("the window's content is not a Frame showing a Page");
                return;
            }

            var bar = new CommandBar { ClosedDisplayMode = AppBarClosedDisplayMode.Compact };
            var add = new AppBarButton { Label = "add", Icon = new SymbolIcon(Symbol.Add) };
            var favorite = new AppBarToggleButton { Label = "favorite", Icon = new SymbolIcon(Symbol.Favorite) };
            var delete = new AppBarButton { Label = "delete", Icon = new SymbolIcon(Symbol.Delete) };
            var first = new AppBarButton { Label = "first menu item" };
            var second = new AppBarButton { Label = "second menu item" };
            foreach (var command in new[] { add, delete, first, second })
            {
                var c = command;
                c.Click += (s, e) => Log("click", c.Label + ", bar open " + bar.IsOpen);
            }

            favorite.Click += (s, e) => Log("click", "favorite, checked " + favorite.IsChecked + ", bar open " + bar.IsOpen);
            bar.PrimaryCommands.Add(add);
            bar.PrimaryCommands.Add(favorite);
            bar.PrimaryCommands.Add(delete);
            bar.SecondaryCommands.Add(first);
            bar.SecondaryCommands.Add(second);
            bar.Opened += (s, e) => Log("bar", "Opened");
            bar.Closed += (s, e) => Log("bar", "Closed");
            shell.BottomAppBar = bar;

            var header = Header();
            var page = await Page(host, header);
            LogPointers(page, "page");
            LogGestures(page, "page");
            var b = Bounds(bar);
            Log("layout", "bar " + Num(b.X) + " " + Num(b.Y) + " " + Num(b.Width) + " " + Num(b.Height) + ", page " + Num(page.ActualWidth) + " x " + Num(page.ActualHeight));

            Func<AppBarClosedDisplayMode, bool, Func<Task>> state = (mode, isOpen) => () =>
            {
                if (bar.ClosedDisplayMode != mode)
                {
                    bar.ClosedDisplayMode = mode;
                }

                if (bar.IsOpen != isOpen)
                {
                    bar.IsOpen = isOpen;
                }

                Log("setup", mode + (isOpen ? ", open" : ", closed"));
                return Task.FromResult(true);
            };
            var closed = state(AppBarClosedDisplayMode.Compact, false);
            var open = state(AppBarClosedDisplayMode.Compact, true);
            var minimal = state(AppBarClosedDisplayMode.Minimal, false);
            var minimalOpen = state(AppBarClosedDisplayMode.Minimal, true);
            Func<Task> none = () => Task.FromResult(true);

            // The bar is the shell's, outside the page: the touches are planned with no target, so they aren't hit-tested.
            const double gap = 2500;
            Press(null, "hold add, bar closed", closed, 150, 768, 0, 0, 0, 0, 1000, gap);
            Press(null, "tap the ellipsis, bar closed", closed, 420, 752, 0, 0, 0, 0, 80, gap);
            Press(null, "hold add, bar open", open, 150, 546, 0, 0, 0, 0, 1000, gap);
            Press(null, "hold the first menu item, bar open", open, 150, 629, 0, 0, 0, 0, 1000, gap);
            Press(null, "tap favorite, bar open", open, 224, 546, 0, 0, 0, 0, 80, gap);
            Press(null, "tap the ellipsis, bar open", open, 420, 527, 0, 0, 0, 0, 80, gap);
            Press(null, "tap the page above the open bar", open, 225, 300, 0, 0, 0, 0, 80, gap);
            Press(null, "tap add, bar closed", closed, 150, 768, 0, 0, 0, 0, 80, gap);
            Press(null, "minimal: tap the bar away from the ellipsis", minimal, 100, 788, 0, 0, 0, 0, 80, gap);
            Press(null, "minimal: tap the ellipsis", minimal, 420, 786, 0, 0, 0, 0, 80, gap);
            Press(null, "minimal: tap the ellipsis, bar open", minimalOpen, 420, 527, 0, 0, 0, 0, 80, gap);
            Code("closed again", closed, 2000);
            await RunScheduled(page, header);
            shell.BottomAppBar = null;
        }

        /// <summary>A horizontal touch: <see cref="Press"/> at (x, y), moved by dx over moveMs, held holdMs and let go; gapMs before the next. Its arguments are Press's, in Press's order, without the still time and dy.</summary>
        private void Swipe(UIElement target, string label, Func<Task> setup, double x, double y, double dx, double moveMs, double holdMs, double gapMs)
        {
            Press(target, label, setup, x, y, 0, dx, 0, moveMs, holdMs, gapMs);
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
    }
}
