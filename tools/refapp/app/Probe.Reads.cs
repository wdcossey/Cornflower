using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// The reads Tier 2 still owed (#148), run on a schedule after a start tap on the header band, like the shell's probes
    /// (<see cref="RunScheduled"/>): where a Pivot's or a Hub's release goes on or back, a press while either still moves,
    /// the Pivot's item events, a drag right at the Hub's first section, <c>ScrollToSection</c>, the Hub's entrance and its
    /// background (a plain colour, an image narrower than the screen, each <c>Stretch</c>), a long list of secondary
    /// commands in the page's app bar, and <c>ItemClick</c> in each <c>SelectionMode</c>.
    /// </summary>
    internal sealed partial class Probe
    {
        /// <summary>The ms a release's touch moves at the given speed (px a ms) over the given distance (px).</summary>
        private static double MoveMs(double distance, double speed) => Math.Abs(distance) / speed;

        /// <summary>
        /// A setup that sets a ScrollViewer's horizontal offset from code at once, clamped to the content (double.MaxValue
        /// for the end), and logs it as "offset N": <see cref="Offset"/> across.
        /// </summary>
        private Func<Task> OffsetX(ScrollViewer sv, double offset)
        {
            return () =>
            {
                var target = Math.Min(offset, sv.ScrollableWidth);
                // ChangeView returns false when the offset is already there.
                var ok = sv.ChangeView(target, null, null, true);
                Log("setup", "offset " + Num(target) + (ok ? "" : " (ChangeView returned false)"));
                return Task.FromResult(true);
            };
        }

        /// <summary>
        /// A second touch inside the touch just planned, with no setup of its own (so no step cuts the log between them):
        /// pressed at (x, y), held still for <paramref name="holdMs"/>, released; <paramref name="gapMs"/> before the next.
        /// Planned straight after a touch whose own gap is how long after its release this one presses.
        /// </summary>
        private void Then(UIElement target, double x, double y, double holdMs, double gapMs)
        {
            _touches++;
            Step("down", x, y, target);
            Wait(holdMs);
            Step("up", x, y);
            Wait(gapMs);
        }

        /// <summary>
        /// A touch along a path of (x, y, ms) points: pressed at the first, then moved to each next one in a straight line
        /// over its ms, a step about every 10 ms (a point where the last one was is a hold for its ms), and released at the
        /// last; <paramref name="gapMs"/> before the next press. The setup runs 1.2 s before the press.
        /// </summary>
        private void PathTouch(UIElement target, string label, Func<Task> setup, double gapMs, params double[][] points)
        {
            _touches++;
            _schedule.Add(new Scheduled { At = _plannedMs - 1200, Label = label, Action = setup, Touch = true });
            Step("down", points[0][0], points[0][1], target);
            for (var k = 1; k < points.Length; k++)
            {
                double x0 = points[k - 1][0], y0 = points[k - 1][1], x1 = points[k][0], y1 = points[k][1], ms = points[k][2];
                if (x0 == x1 && y0 == y1)
                {
                    Wait(ms);
                    continue;
                }

                var n = Math.Max(2, (int)Math.Round(ms / 10));
                for (var i = 1; i <= n; i++)
                {
                    Wait(ms / n);
                    Step("move", x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n);
                }
            }

            var last = points[points.Length - 1];
            Step("up", last[0], last[1]);
            Wait(gapMs);
        }

        /// <summary>
        /// Where a Pivot's release goes, its item events, and a press while it moves (#148). A Pivot of four items, item 0
        /// selected from code before each touch:
        /// - drags left of 100 to 300 px at 0.5 px a ms, held still 400 ms and let go (a release with no speed), and right
        ///   of 200 and 250 px;
        /// - drags left of 60 px let go while moving, at 0.1 to 1 px a ms, and right at 0.2 and 0.6;
        /// - drags left of 120 px let go while moving at 0.1, 0.2 and 0.3 px a ms;
        /// - a drag of 250 px left then 60 px back right in 100 ms, let go moving right; and one of 100 px left then 60 px
        ///   on left in 100 ms;
        /// - a swipe left (200 px in 150 ms) and, 30, 100, 200 or 350 ms after its release, a second press held still 600
        ///   ms; and one tapped (80 ms) 100 ms after;
        /// - a tap on the header "two", and SelectedIndex set to 2 from code, for the item events.
        /// The item events are logged by <see cref="WatchPivot"/>.
        /// </summary>
        private async Task PivotReleaseProbe(Grid host)
        {
            var parts = await PlacePivot(host, new[] { "one", "two", "three", "four" });
            if (parts == null)
            {
                return;
            }

            var pivot = parts.Pivot;
            const double y = 450, gap = 3000;
            var first = Select(pivot, 0);
            foreach (var d in new[] { 100.0, 150, 200, 215, 225, 235, 250, 300 })
            {
                Swipe(pivot, "slow left " + Num(d) + " px, held", first, 400, y, -d, MoveMs(d, 0.5), 400, gap);
            }

            foreach (var d in new[] { 200.0, 250 })
            {
                Swipe(pivot, "slow right " + Num(d) + " px, held", first, 50, y, d, MoveMs(d, 0.5), 400, gap);
            }

            foreach (var v in new[] { 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.6, 1.0 })
            {
                Swipe(pivot, "left 60 px let go at " + Num(v) + " px/ms", first, 400, y, -60, MoveMs(60, v), 0, gap);
            }

            foreach (var v in new[] { 0.2, 0.6 })
            {
                Swipe(pivot, "right 60 px let go at " + Num(v) + " px/ms", first, 50, y, 60, MoveMs(60, v), 0, gap);
            }

            foreach (var v in new[] { 0.1, 0.2, 0.3 })
            {
                Swipe(pivot, "left 120 px let go at " + Num(v) + " px/ms", first, 400, y, -120, MoveMs(120, v), 0, gap);
            }

            PathTouch(pivot, "left 250 px, then back 60 px in 100 ms, let go moving right", first, gap,
                new[] { 400, y, 0 }, new[] { 150, y, 500 }, new[] { 210, y, 100 });
            PathTouch(pivot, "left 100 px, then on 60 px in 100 ms, let go moving left", first, gap,
                new[] { 400, y, 0 }, new[] { 300, y, 500 }, new[] { 240, y, 100 });

            foreach (var after in new[] { 30.0, 100, 200, 350 })
            {
                Swipe(pivot, "swipe left, then a press held 600 ms " + Num(after) + " ms after the release", first, 380, y, -200, 150, 0, after);
                Then(pivot, 225, y, 600, gap);
            }

            Swipe(pivot, "swipe left, then a tap 100 ms after the release", first, 380, y, -200, 150, 0, 100);
            Then(pivot, 225, y, 80, gap);

            Rect two;
            if (parts.AtRest[0].TryGetValue("two", out two))
            {
                Drag(pivot, "tap header two from 0", first, two.X + two.Width / 2, two.Y + two.Height / 2, two.Y + two.Height / 2, 0, 80, gap);
            }

            Code("SelectedIndex 0 from code", first, 3000);
            Code("SelectedIndex 2 from code", Select(pivot, 2), 3000);
            await RunScheduled(parts.Page, parts.Band);
        }

        /// <summary>
        /// Where a Hub's release goes, a press while it moves, a drag right at its first section, and ScrollToSection
        /// (#148). The probes' Hub (<see cref="NewHub"/>), its offset set from code before each touch:
        /// - from offset 0, drags left of 100 to 250 px at 0.5 px a ms, held still 400 ms and let go;
        /// - drags left of 60 px let go while moving, at 0.1 to 1 px a ms;
        /// - at offset 0 (the first section): a drag right of 150 px held and let go, a swipe right (200 px in 150 ms), and
        ///   a drag right of 60 px let go at 0.2 px a ms; at offset 405 (the second section), a drag right of 60 px let go
        ///   at 0.6 and one at 0.2;
        /// - a swipe left and, 30, 100, 200 or 350 ms after its release, a second press held still 600 ms; and one tapped
        ///   100 ms after;
        /// - with no touch, from offset 0, ScrollToSection to the third section, the second, the fourth and the first, 3 s
        ///   apart, the offset logged before and after each call.
        /// </summary>
        private async Task HubReleaseProbe(Grid host)
        {
            var hub = NewHub();
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
            // The layout lines once the entrance is over, not under its projection.
            await Task.Delay(1500);
            WatchHub(hub, sv, headerHost, new FrameworkElement[0]);
            Func<double, Func<Task>> at = offset => OffsetX(sv, offset);
            var start = at(0);
            const double y = 500, gap = 3000;
            foreach (var d in new[] { 100.0, 150, 190, 200, 205, 215, 250 })
            {
                Swipe(hub, "slow left " + Num(d) + " px, held", start, 400, y, -d, MoveMs(d, 0.5), 400, gap);
            }

            foreach (var v in new[] { 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.6, 1.0 })
            {
                Swipe(hub, "left 60 px let go at " + Num(v) + " px/ms", start, 400, y, -60, MoveMs(60, v), 0, gap);
            }

            Swipe(hub, "first section: right 150 px, held", start, 50, y, 150, 300, 400, gap);
            Swipe(hub, "first section: swipe right", start, 70, y, 200, 150, 0, gap);
            Swipe(hub, "first section: right 60 px let go at 0.2 px/ms", start, 50, y, 60, MoveMs(60, 0.2), 0, gap);
            foreach (var v in new[] { 0.6, 0.2 })
            {
                Swipe(hub, "second section: right 60 px let go at " + Num(v) + " px/ms", at(405), 50, y, 60, MoveMs(60, v), 0, gap);
            }

            foreach (var after in new[] { 30.0, 100, 200, 350 })
            {
                Swipe(hub, "swipe left, then a press held 600 ms " + Num(after) + " ms after the release", start, 380, y, -200, 150, 0, after);
                Then(hub, 225, y, 600, gap);
            }

            Swipe(hub, "swipe left, then a tap 100 ms after the release", start, 380, y, -200, 150, 0, 100);
            Then(hub, 225, y, 80, gap);

            Code("ScrollToSection: offset 0 first", start, 2000);
            foreach (var k in new[] { 2, 1, 3, 0 })
            {
                var index = k;
                Code("ScrollToSection " + (index + 1), () =>
                {
                    var before = sv.HorizontalOffset;
                    hub.ScrollToSection(hub.Sections[index]);
                    Log("setup", "ScrollToSection " + (index + 1) + ", offset " + Num(before) + " before the call, " + Num(sv.HorizontalOffset) + " after");
                    return Task.FromResult(true);
                }, 3000);
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// The Hub's entrance, and its background as a plain colour (#148), made to be recorded. The page holds only the
        /// header band at first; then, each from code: the probes' Hub with no background placed and, 3.5 s later, taken
        /// away; the same with a plain colour background, swiped left and back right before it goes; and with a 1600 x
        /// 800 image (<see cref="StripeImage"/>). Every frame from before each Hub is placed, the log has the Hub's root's
        /// projection (the template's EntranceAnimationProjection) and opacity, the header's left edge and its fly-in
        /// translate, the first section's left edge and the background grid's opacity, size and left edge; and each Hub's
        /// visual states and its background parts' sizes once the entrance is over.
        /// </summary>
        private async Task HubEntranceProbe(Grid host)
        {
            var header = Header();
            var page = await Page(host, header);
            Hub current = null;
            FrameworkElement root = null, headerHost = null, bg = null;
            HubSection firstSection = null;
            WatchFrames(() =>
            {
                if (current == null)
                {
                    return new[] { double.NaN };
                }

                if (root == null && VisualTreeHelper.GetChildrenCount(current) > 0)
                {
                    root = VisualTreeHelper.GetChild(current, 0) as FrameworkElement;
                    headerHost = Find(current, "HeaderHost");
                    bg = Find(current, "Background");
                }

                var p = root != null ? root.Projection as PlaneProjection : null;
                var group = headerHost != null ? headerHost.RenderTransform as TransformGroup : null;
                var fly = group != null ? group.Children.OfType<TranslateTransform>().FirstOrDefault() : null;
                return new[]
                {
                    1,
                    p != null ? p.RotationX : double.NaN, p != null ? p.RotationY : double.NaN, p != null ? p.RotationZ : double.NaN,
                    p != null ? p.CenterOfRotationX : double.NaN, p != null ? p.LocalOffsetX : double.NaN,
                    p != null ? p.GlobalOffsetX : double.NaN, p != null ? p.GlobalOffsetY : double.NaN, p != null ? p.GlobalOffsetZ : double.NaN,
                    root != null ? root.Opacity : double.NaN, current.Opacity,
                    headerHost != null ? Left(headerHost) : double.NaN, fly != null ? fly.X : double.NaN,
                    firstSection != null ? Left(firstSection) : double.NaN,
                    bg != null ? bg.Opacity : double.NaN, bg != null ? bg.ActualWidth : double.NaN, bg != null ? bg.ActualHeight : double.NaN, bg != null ? Left(bg) : double.NaN,
                };
            }, v => v.Length == 1
                ? "hub none"
                : "hub proj rot " + Num(v[1]) + " " + Num(v[2]) + " " + Num(v[3]) + " centreX " + Num(v[4]) + " localX " + Num(v[5]) + " global " + Num(v[6]) + " " + Num(v[7]) + " " + Num(v[8])
                  + " | op " + Num(v[9]) + " hubop " + Num(v[10]) + " | header " + Num(v[11]) + " flyin " + Num(v[12]) + " | section1 " + Num(v[13])
                  + " | bg op " + Num(v[14]) + " " + Num(v[15]) + " x " + Num(v[16]) + " at " + Num(v[17]));

            Func<Brush, string, Func<Task>> place = (background, what) => () =>
            {
                var hub = NewHub();
                if (background != null)
                {
                    hub.Background = background;
                }

                hub.Loaded += (s, e) =>
                {
                    Log("hub", "Loaded, " + what);
                    WatchStates(hub);
                };
                root = headerHost = bg = null;
                firstSection = hub.Sections[0];
                current = hub;
                page.Children.Add(hub);
                Log("setup", "placed a Hub, " + what);
                return Task.FromResult(true);
            };
            Func<Task> after = () =>
            {
                if (current != null && bg != null)
                {
                    var sv = Find(current, "ScrollViewer") as ScrollViewer;
                    var main = Find(current, "MainBackground");
                    var wrap = Find(current, "WrapBackground");
                    Log("layout", "hub " + Num(current.ActualWidth) + " x " + Num(current.ActualHeight) + ", Background " + Num(bg.ActualWidth) + " x " + Num(bg.ActualHeight) + " op " + Num(bg.Opacity)
                        + (main != null ? ", MainBackground " + Num(main.ActualWidth) + " x " + Num(main.ActualHeight) : "")
                        + (wrap != null ? ", WrapBackground " + Num(wrap.ActualWidth) + " x " + Num(wrap.ActualHeight) + " " + wrap.Visibility : "")
                        + (sv != null ? ", offset " + Num(sv.HorizontalOffset) : ""));
                }

                return Task.FromResult(true);
            };
            Func<Task> remove = () =>
            {
                if (current != null)
                {
                    page.Children.Remove(current);
                    current = null;
                    Log("setup", "took the Hub away");
                }

                return Task.FromResult(true);
            };
            Func<Task> none = () => Task.FromResult(true);

            Code("place a Hub, no background", place(null, "no background"), 2500);
            Code("its parts after the entrance, no background", after, 1500);
            Code("take it away", remove, 1500);
            Code("place a Hub, a plain colour background", place(new SolidColorBrush(Color.FromArgb(255, 0x60, 0x30, 0x90)), "background #FF603090"), 2500);
            Code("its parts after the entrance, plain colour", after, 1500);
            Swipe(null, "swipe left on the coloured Hub", none, 380, 500, -200, 150, 0, 3000);
            Swipe(null, "swipe right on the coloured Hub", none, 70, 500, 200, 150, 0, 3000);
            Code("take it away again", remove, 1500);
            var image = await StripeImage(1600, 800);
            Code("place a Hub, a 1600 x 800 image", place(new ImageBrush { ImageSource = image }, "a 1600 x 800 image"), 2500);
            Code("its parts after the entrance, image", after, 1500);
            Code("take it away, last", remove, 1500);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// The Hub's background image at each Stretch, and narrower than the screen (#148). The probes' Hub, given in turn
        /// an ImageBrush of a 900 x 400 picture (<see cref="StripeImage"/>) with Stretch Fill (the default, #136's), None,
        /// Uniform and UniformToFill, then of a 300 x 400 one with Fill and None, each a new brush. For each, with no
        /// touch, the offset is set from code to 0, 200, 400, 585, 800, 1000, 1170 and the end, and at 0, 585 and the end
        /// the page is rendered to stretch-&lt;n&gt;-&lt;offset&gt;.png. The background's parts are read every frame, as in
        /// -Probe hubimage, and their sizes logged at each offset.
        /// </summary>
        private async Task HubStretchProbe(Grid host)
        {
            var hub = NewHub();
            var header = Header();
            var page = await Page(host, header, hub);
            var sv = Find(hub, "ScrollViewer") as ScrollViewer;
            var headerHost = Find(hub, "HeaderHost");
            var bg = Find(hub, "Background");
            var wrap = Find(hub, "WrapBackground");
            var main = Find(hub, "MainBackground");
            if (sv == null || headerHost == null || bg == null || wrap == null || main == null)
            {
                await Finish("the Hub's template lacks a part: ScrollViewer, HeaderHost, Background, WrapBackground or MainBackground");
                return;
            }

            WatchStates(hub);
            var parts = new[] { bg, wrap, main };
            // The layout lines once the entrance is over, not under its projection.
            await Task.Delay(1500);
            WatchHub(hub, sv, headerHost, parts);
            var configs = new[]
            {
                new { Name = "fill-900", Stretch = Stretch.Fill, Width = 900 },
                new { Name = "none-900", Stretch = Stretch.None, Width = 900 },
                new { Name = "uniform-900", Stretch = Stretch.Uniform, Width = 900 },
                new { Name = "uniformtofill-900", Stretch = Stretch.UniformToFill, Width = 900 },
                new { Name = "fill-300", Stretch = Stretch.Fill, Width = 300 },
                new { Name = "none-300", Stretch = Stretch.None, Width = 300 },
            };
            foreach (var config in configs)
            {
                var c = config;
                Code("background " + c.Name, async () =>
                {
                    // A new picture for each brush, so that each loads afresh: a BitmapImage already opened raises no
                    // ImageOpened on the next brush it is given to.
                    var image = await StripeImage(c.Width, 400, "hub-" + c.Name + ".png");
                    var brush = new ImageBrush { Stretch = c.Stretch };
                    brush.ImageOpened += (s, e) => Log("image", "opened, " + c.Name + ", background opacity " + Num(bg.Opacity));
                    brush.ImageFailed += (s, e) => Log("image", "failed, " + c.Name + ": " + e.ErrorMessage);
                    brush.ImageSource = image;
                    hub.Background = brush;
                    Log("setup", "background " + c.Name + ", Stretch " + c.Stretch);
                    await OffsetX(sv, 0)();
                }, 2000);
                foreach (var offset in new[] { 0.0, 200, 400, 585, 800, 1000, 1170, double.MaxValue })
                {
                    var o = offset;
                    var shot = o == 0 || o == 585 || o == double.MaxValue;
                    Code(c.Name + ": offset " + (o == double.MaxValue ? "end" : Num(o)), async () =>
                    {
                        await OffsetX(sv, o)();
                        await Task.Delay(400);
                        var line = new StringBuilder("parts at offset " + Num(sv.HorizontalOffset) + ":");
                        foreach (var part in parts)
                        {
                            line.Append(' ').Append(part.Name).Append(' ').Append(Num(Left(part))).Append(' ').Append(Num(part.ActualWidth)).Append(" x ").Append(Num(part.ActualHeight)).Append(';');
                        }

                        Log("layout", line.ToString());
                        if (shot)
                        {
                            var name = "stretch-" + c.Name + "-" + (o == double.MaxValue ? "end" : Num(o)) + ".png";
                            var rtb = await TreeDump.RenderPngAsync(_out, name, page);
                            Log("snapshot", name + " " + rtb.PixelWidth + " x " + rtb.PixelHeight);
                        }
                    }, 1500);
                }
            }

            await RunScheduled(page, header);
        }

        /// <summary>
        /// A long list of secondary commands in the page's app bar (#148), made to be recorded: does it scroll? The bar of
        /// <see cref="AppBarProbe"/> with twelve secondary commands, "menu item 1" to "menu item 12", open (its top at 381.25,
        /// the lines' text from 485.8, 59.58 apart, the sixth running off the screen). On the open bar: a drag up of 250 px
        /// from the fifth line over 400 ms, held 300 ms and let go, then a tap where the second line was at rest (555); the
        /// bar closed and opened again, a flick up of 300 px in 100 ms from the fifth line, then the same tap; opened again,
        /// the same tap with nothing before it; opened again, a drag down of 200 px from the first line, held and let go,
        /// then the same tap. The line a tap invokes is in its Click.
        /// </summary>
        private async Task AppBarLongProbe(Grid host)
        {
            var labels = Enumerable.Range(1, 12).Select(i => "menu item " + i).ToArray();
            await WithAppBar(host, labels, false, AppBarLongTouches);
        }

        /// <summary><see cref="AppBarLongProbe"/>'s page and touches, with the bar on the page.</summary>
        private async Task AppBarLongTouches(Grid host, CommandBar bar, bool light)
        {
            var header = Header();
            var page = await AppBarPage(host, header, bar);
            var state = AppBarState(bar);
            var closed = state(AppBarClosedDisplayMode.Compact, false);
            var open = state(AppBarClosedDisplayMode.Compact, true);
            // Opened afresh, so a list scrolled by the touch before is back at its top.
            Func<Task> reopen = async () =>
            {
                if (bar.IsOpen)
                {
                    bar.IsOpen = false;
                    await Task.Delay(600);
                }

                bar.IsOpen = true;
                Log("setup", "Compact, opened afresh");
            };
            Func<Task> keep = () =>
            {
                Log("setup", "as it is, bar open " + bar.IsOpen);
                return Task.FromResult(true);
            };
            const double x = 150, line2 = 555, gap = 2500;
            Press(null, "drag the list up 250 px over 400 ms, held", open, x, 734, 0, 0, -250, 400, 300, gap);
            Press(null, "tap the second line's place, after the drag up", keep, x, line2, 0, 0, 0, 0, 80, gap);
            Press(null, "flick the list up 300 px in 100 ms", reopen, x, 734, 0, 0, -300, 100, 0, gap);
            Press(null, "tap the second line's place, after the flick", keep, x, line2, 0, 0, 0, 0, 80, gap);
            Press(null, "tap the second line's place, list at rest", reopen, x, line2, 0, 0, 0, 0, 80, gap);
            Press(null, "drag the list down 200 px over 400 ms, held", reopen, x, 496, 0, 0, 200, 400, 300, gap);
            Press(null, "tap the second line's place, after the drag down", keep, x, line2, 0, 0, 0, 0, 80, gap);
            Code("closed again", closed, 2000);
            await RunScheduled(page, header);
        }

        /// <summary>
        /// ItemClick in every SelectionMode (#148): <see cref="ListCheck"/>'s list with IsItemClickEnabled. In Multiple (the
        /// check mode): taps on item 1's box and its text, on item 2's text, and a press held 600 ms on item 3. In
        /// Extended: taps on items 1, 2 and 2 again. In None: a tap on item 3. In Single: taps on item 4 and again. The
        /// list is at its top before each. The log has every ItemClick, selection change, state and tilt.
        /// </summary>
        private async Task ListClickProbe(Grid host)
        {
            var parts = await PlaceCheckList(host, true);
            if (parts == null)
            {
                return;
            }

            var items = parts.Items;
            var mode = parts.Mode;
            var at = parts.At;
            var multiple = mode(ListViewSelectionMode.Multiple);
            var p = parts.BoxAt(1);
            Drag(items[1], "Multiple: tap item1 on the box", multiple, p.X, p.Y, p.Y, 0, 80, ListGap);
            p = at(1, 0.5, 0.5);
            Drag(items[1], "Multiple: tap item1 at its text", multiple, p.X, p.Y, p.Y, 0, 80, ListGap);
            p = at(2, 0.5, 0.5);
            Drag(items[2], "Multiple: tap item2 at its text", multiple, p.X, p.Y, p.Y, 0, 80, ListGap);
            p = at(3, 0.5, 0.5);
            Drag(items[3], "Multiple: hold item3", multiple, p.X, p.Y, p.Y, 0, 600, ListGap);
            var extended = mode(ListViewSelectionMode.Extended);
            foreach (var i in new[] { 1, 2, 2 })
            {
                p = at(i, 0.5, 0.5);
                Drag(items[i], "Extended: tap item" + i, extended, p.X, p.Y, p.Y, 0, 80, ListGap);
            }

            p = at(3, 0.5, 0.5);
            Drag(items[3], "None: tap item3", mode(ListViewSelectionMode.None), p.X, p.Y, p.Y, 0, 80, ListGap);
            var single = mode(ListViewSelectionMode.Single);
            p = at(4, 0.5, 0.5);
            Drag(items[4], "Single: tap item4", single, p.X, p.Y, p.Y, 0, 80, ListGap);
            Drag(items[4], "Single: tap item4 again", single, p.X, p.Y, p.Y, 0, 80, ListGap);
            await RunScheduled(parts.Page, parts.Band);
        }
    }
}
