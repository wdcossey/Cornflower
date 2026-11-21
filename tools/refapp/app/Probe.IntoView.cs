using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// #220: does a selected list low on a page in a ScrollViewer move the page? Read from the app alone, after a start
    /// tap. For a ListView of 30 items and a GridView of 30 tiles, each given a height of 300 (its own ScrollViewer
    /// scrolls) and left free in the page's ScrollViewer (only the page scrolls), item 20 selected before it loads, below
    /// a 900-tall spacer: the page's and the list's offsets 1.5 s after the load; then the same with the page laid out
    /// 60 tall first and its full height 0.5 s later (as Avalonia lays a page out on Android). After a plain load, with
    /// both put back to 0 before each: SelectedIndex 25, ScrollIntoView of item 28 and of item 2, and focus on the
    /// selected container from code and on item 27 as from the keyboard, each read 1.2 s later.
    /// </summary>
    internal sealed partial class Probe
    {
        private async Task ListIntoView(Grid host, bool focused = false)
        {
            var stage = new Grid();
            var page = await Page(host, stage);
            Func<Task> sequence = async () =>
            {
                foreach (var grid in new[] { false, true })
                {
                    foreach (var free in new[] { false, true })
                    {
                        if (focused)
                        {
                            await FocusedCase(stage, grid, free);
                            continue;
                        }

                        foreach (var shortFirst in new[] { false, true })
                        {
                            await IntoViewCase(stage, grid, free, shortFirst);
                        }
                    }
                }
            };
            await RunAfterTap(page, sequence);
        }

        /// <summary>
        /// <c>listintoviewfocus</c>: the same lists, plain loads, with the focus in the list: item 0 focused from code, both
        /// offsets put back to 0, then SelectedIndex 25; item 0 focused as from the keyboard, both put back, then
        /// SelectedIndex 27. Each read 1.2 s later.
        /// </summary>
        private async Task FocusedCase(Grid stage, bool grid, bool free)
        {
            var tag = (grid ? "grid" : "list") + (free ? " free" : " height");
            ListViewBase list = grid ? (ListViewBase)new GridView() : new ListView();
            for (var i = 0; i < 30; i++)
            {
                list.Items.Add(grid ? (object)Catalog.Tile("tile " + i) : "item " + i);
            }

            if (!free)
            {
                list.Height = 300;
            }

            list.SelectedIndex = 20;
            var outer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalAlignment = VerticalAlignment.Top };
            var column = new StackPanel();
            column.Children.Add(new Border { Height = 900, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40)) });
            column.Children.Add(list);
            column.Children.Add(new Border { Height = 400 });
            outer.Content = column;
            stage.Children.Clear();
            stage.Children.Add(outer);
            await Task.Delay(1500);
            foreach (var step in new[] { new KeyValuePair<FocusState, int>(FocusState.Programmatic, 25), new KeyValuePair<FocusState, int>(FocusState.Keyboard, 27) })
            {
                Log("focused", tag + " " + Focus(list, 0, step.Key) + " " + step.Key);
                await Task.Delay(800);
                Log("focus", tag + " " + step.Key + ": " + Offsets(outer, list));
                outer.ChangeView(null, 0, null, true);
                var inner = Find(list, "ScrollViewer") as ScrollViewer;
                if (inner != null)
                {
                    inner.ChangeView(null, 0, null, true);
                }

                await Task.Delay(800);
                Log("before", tag + " " + step.Key + " SelectedIndex " + step.Value + ": " + Offsets(outer, list) + "; focus " + FocusName(list));
                list.SelectedIndex = step.Value;
                await Task.Delay(1200);
                Log("after", tag + " " + step.Key + " SelectedIndex " + step.Value + ": " + Offsets(outer, list) + "; focus " + FocusName(list));
            }
        }

        private async Task IntoViewCase(Grid stage, bool grid, bool free, bool shortFirst)
        {
            var tag = (grid ? "grid" : "list") + (free ? " free" : " height") + (shortFirst ? " short-first" : "");
            ListViewBase list = grid ? (ListViewBase)new GridView() : new ListView();
            var items = new List<object>();
            for (var i = 0; i < 30; i++)
            {
                object item = grid ? (object)Catalog.Tile("tile " + i) : "item " + i;
                items.Add(item);
                list.Items.Add(item);
            }

            if (!free)
            {
                list.Height = 300;
            }

            list.SelectedIndex = 20;
            var outer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalAlignment = VerticalAlignment.Top };
            var column = new StackPanel();
            column.Children.Add(new Border { Height = 900, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40)) });
            column.Children.Add(list);
            column.Children.Add(new Border { Height = 400 });
            outer.Content = column;
            if (shortFirst)
            {
                outer.Height = 60;
            }

            stage.Children.Clear();
            stage.Children.Add(outer);
            if (shortFirst)
            {
                await Task.Delay(500);
                Log("short", tag + ": " + Offsets(outer, list));
                outer.ClearValue(FrameworkElement.HeightProperty);
            }

            await Task.Delay(1500);
            Log("load", tag + ": " + Offsets(outer, list));
            if (shortFirst)
            {
                return;
            }

            var steps = new List<KeyValuePair<string, Action>>
            {
                new KeyValuePair<string, Action>("SelectedIndex 25", () => list.SelectedIndex = 25),
                new KeyValuePair<string, Action>("ScrollIntoView 28", () => list.ScrollIntoView(items[28])),
                new KeyValuePair<string, Action>("ScrollIntoView 2", () => list.ScrollIntoView(items[2])),
                new KeyValuePair<string, Action>("focus selected programmatic", () => Log("focused", tag + " " + Focus(list, list.SelectedIndex, FocusState.Programmatic))),
                new KeyValuePair<string, Action>("focus 27 keyboard", () => Log("focused", tag + " " + Focus(list, 27, FocusState.Keyboard))),
            };
            foreach (var step in steps)
            {
                outer.ChangeView(null, 0, null, true);
                var inner = Find(list, "ScrollViewer") as ScrollViewer;
                if (inner != null)
                {
                    inner.ChangeView(null, 0, null, true);
                }

                await Task.Delay(800);
                Log("before", tag + " " + step.Key + ": " + Offsets(outer, list));
                step.Value();
                await Task.Delay(1200);
                Log("after", tag + " " + step.Key + ": " + Offsets(outer, list));
            }
        }

        /// <summary>The focused element: an item of the list by its index, or its type.</summary>
        private static string FocusName(ListViewBase list)
        {
            var f = FocusManager.GetFocusedElement() as DependencyObject;
            var index = f != null ? list.IndexFromContainer(f) : -1;
            return f == null ? "none" : index >= 0 ? "item " + index : f.GetType().Name;
        }

        private static string Focus(ListViewBase list, int index, FocusState state)
        {
            var c = list.ContainerFromIndex(index) as Control;
            return "item " + index + (c == null ? " no container" : " " + c.Focus(state));
        }

        private static string Offsets(ScrollViewer outer, ListViewBase list)
        {
            var inner = Find(list, "ScrollViewer") as ScrollViewer;
            var selected = list.SelectedIndex >= 0 ? list.ContainerFromIndex(list.SelectedIndex) as FrameworkElement : null;
            return "page " + Num(outer.VerticalOffset) + " of " + Num(outer.ExtentHeight) + " viewport " + Num(outer.ViewportHeight)
                + "; list " + (inner == null ? "no ScrollViewer" : Num(inner.VerticalOffset) + " of " + Num(inner.ExtentHeight) + " viewport " + Num(inner.ViewportHeight))
                + "; list at " + Box(list) + "; selected " + list.SelectedIndex + (selected == null ? " no container" : " at " + Box(selected));
        }
    }
}
