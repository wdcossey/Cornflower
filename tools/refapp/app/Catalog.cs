using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>One control in one configuration. The suite captures it at rest, then in every visual state its template declares.</summary>
    internal sealed class Specimen
    {
        public string Name;
        public string Variant;
        public double Height = 120;
        public bool FullBleed;
        public bool States = true;
        public Func<FrameworkElement> Make;

        /// <summary>
        /// Run on the placed control before its capture, for a state the control's own groups don't hold, such as an
        /// inner part's (#150, B19); its result is the capture's stateApplied. Null for none.
        /// </summary>
        public Func<FrameworkElement, Task<bool>> Prepare;
    }

    /// <summary>
    /// What the reference app captures. Add a control by adding lines here, nothing else. Content strings are
    /// lower case, as the phone's own UI wrote them.
    /// </summary>
    internal static class Catalog
    {
        private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";

        /// <summary>Every TextBlock style key in the phone's generic.xaml and themeresources.xaml (2026-11-07 inventory).</summary>
        private static readonly string[] TextStyles =
        {
            "BaseTextBlockStyle", "HeaderTextBlockStyle", "SubheaderTextBlockStyle", "TitleTextBlockStyle", "BodyTextBlockStyle",
            "GroupHeaderTextBlockStyle", "ControlHeaderTextBlockStyle", "ControlContextualInfoTextBlockStyle",
            "ListViewItemTextBlockStyle", "ListViewItemSubheaderTextBlockStyle", "ListViewItemContentTextBlockStyle",
            "ListViewEmptyStaticTextBlockStyle", "FlyoutPickerTitleTextBlockStyle", "ComboBoxPlaceholderTextBlockStyle",
            "MessageDialogTitleStyle", "MessageDialogContentStyle",
        };

        /// <summary>The styles above that wrap (BaseTextBlockStyle's WrapWholeWords, not set back to NoWrap), which get a sentence wrapped by the stage's width too.</summary>
        private static readonly string[] WrappingTextStyles =
        {
            "BaseTextBlockStyle", "SubheaderTextBlockStyle", "BodyTextBlockStyle", "GroupHeaderTextBlockStyle",
            "ControlHeaderTextBlockStyle", "ControlContextualInfoTextBlockStyle", "ListViewEmptyStaticTextBlockStyle",
            "MessageDialogTitleStyle", "MessageDialogContentStyle",
        };

        private const string Line = "Agjy 0123 cornflower";

        /// <summary>
        /// A TextBlock in a theme style, loaded through XamlReader so the key resolves as the phone's own XAML would. The
        /// content is XAML (inlines); null sets Text to <see cref="Line"/> instead, as the first runs did.
        /// </summary>
        private static Specimen Text(string key, string variant, double height, string content)
        {
            return new Specimen
            {
                Name = "TextBlock",
                Variant = variant,
                Height = height,
                States = false,
                Make = () => (FrameworkElement)XamlReader.Load(content == null
                    ? "<TextBlock " + Ns + " Style=\"{ThemeResource " + key + "}\" Text=\"" + Line + "\" />"
                    : "<TextBlock " + Ns + " Style=\"{ThemeResource " + key + "}\">" + content + "</TextBlock>"),
            };
        }

        /// <summary>
        /// A ScrollViewer 280 tall over 14 lines of body text, 40 apart (560 tall), scrolled to the offset from code once it
        /// has laid out.
        /// </summary>
        private static FrameworkElement Scrolled(double offset)
        {
            var content = new StackPanel();
            for (var i = 1; i <= 14; i++)
            {
                content.Children.Add(new Border { Height = 40, Child = new TextBlock { Text = "line " + i, FontSize = 20 } });
            }

            var sv = new ScrollViewer { Height = 280, Content = content };
            if (offset > 0)
            {
                EventHandler<object> scroll = null;
                scroll = (s, e) =>
                {
                    if (sv.ScrollableHeight > 0)
                    {
                        sv.LayoutUpdated -= scroll;
                        sv.ChangeView(null, offset, null, true);
                    }
                };
                sv.LayoutUpdated += scroll;
            }

            return sv;
        }

        /// <summary>A 200 x 100 slot at the stage's top left, holding the element.</summary>
        private static FrameworkElement Slot(FrameworkElement element)
        {
            return new Grid { Width = 200, Height = 100, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { element } };
        }

        /// <summary>A child smaller than its slot: a 60 x 30 rectangle in the reference's accent.</summary>
        private static FrameworkElement Child()
        {
            return new Windows.UI.Xaml.Shapes.Rectangle { Width = 60, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 0x3E, 0x65, 0xFF)) };
        }

        public static List<Specimen> All()
        {
            var list = new List<Specimen>();
            Action<string, string, double, Func<FrameworkElement>> add = (name, variant, height, make) =>
                list.Add(new Specimen { Name = name, Variant = variant, Height = height, Make = make });

            // One line; the same line twice, broken by a LineBreak, so the second line's ink is the first's moved down
            // by the line pitch; and, for the styles that wrap, a sentence wrapped by the stage's width.
            foreach (var key in TextStyles)
            {
                list.Add(Text(key, key, 120, null));
            }

            foreach (var key in TextStyles)
            {
                list.Add(Text(key, key + "-two-lines", 200, Line + "<LineBreak />" + Line));
            }

            foreach (var key in WrappingTextStyles)
            {
                list.Add(Text(key, key + "-wrapped", 200, Line + ", a sentence long enough to wrap onto the next line"));
            }

            add("Button", "default", 100, () => new Button { Content = "button" });
            add("HyperlinkButton", "default", 100, () => new HyperlinkButton { Content = "hyperlink button" });
            add("RepeatButton", "default", 100, () => new RepeatButton { Content = "repeat button" });
            add("ToggleButton", "unchecked", 100, () => new ToggleButton { Content = "toggle button" });
            add("ToggleButton", "checked", 100, () => new ToggleButton { Content = "toggle button", IsChecked = true });
            add("CheckBox", "unchecked", 100, () => new CheckBox { Content = "check box" });
            add("CheckBox", "checked", 100, () => new CheckBox { Content = "check box", IsChecked = true });
            add("CheckBox", "indeterminate", 100, () => new CheckBox { Content = "check box", IsThreeState = true, IsChecked = null });
            add("RadioButton", "unchecked", 100, () => new RadioButton { Content = "radio button" });
            add("RadioButton", "checked", 100, () => new RadioButton { Content = "radio button", IsChecked = true });
            add("ToggleSwitch", "off", 140, () => new ToggleSwitch { Header = "toggle switch" });
            add("ToggleSwitch", "on", 140, () => new ToggleSwitch { Header = "toggle switch", IsOn = true });
            add("ToggleSwitch", "no-header", 110, () => new ToggleSwitch());
            add("Slider", "value-30", 140, () => new Slider { Header = "slider", Value = 30 });
            add("ProgressBar", "value-40", 60, () => new ProgressBar { Value = 40 });
            add("ProgressBar", "indeterminate", 60, () => new ProgressBar { IsIndeterminate = true });
            add("ProgressRing", "active", 120, () => new ProgressRing { IsActive = true });
            add("TextBox", "empty", 140, () => new TextBox { Header = "text box", PlaceholderText = "placeholder" });
            add("TextBox", "text", 140, () => new TextBox { Header = "text box", Text = "Agjy 0123" });
            add("TextBox", "no-header", 110, () => new TextBox { Text = "Agjy 0123" });
            add("TextBox", "no-header-empty", 110, () => new TextBox { PlaceholderText = "placeholder" });
            add("TextBox", "wrap", 220, () => new TextBox { Header = "text box", Text = "Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123 Agjy 0123", TextWrapping = TextWrapping.Wrap, AcceptsReturn = true });
            add("PasswordBox", "password", 140, () => new PasswordBox { Header = "password box", Password = "secret", IsPasswordRevealButtonEnabled = true });
            add("PasswordBox", "no-header", 110, () => new PasswordBox { Password = "secret" });
            add("PasswordBox", "empty", 140, () => new PasswordBox { Header = "password box", PlaceholderText = "placeholder" });
            add("PasswordBox", "reveal-empty", 140, () => new PasswordBox { Header = "password box", IsPasswordRevealButtonEnabled = true });
            add("PasswordBox", "reveal-no-header", 110, () => new PasswordBox { Password = "secret", IsPasswordRevealButtonEnabled = true });
            add("ComboBox", "closed", 140, () =>
            {
                var c = new ComboBox { Header = "combo box" };
                c.Items.Add("one");
                c.Items.Add("two");
                c.Items.Add("three");
                c.SelectedIndex = 0;
                return c;
            });
            add("DatePicker", "default", 140, () => new DatePicker { Header = "date picker", Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) });
            add("TimePicker", "default", 140, () => new TimePicker { Header = "time picker", Time = new TimeSpan(12, 30, 0) });

            // B19 (#150): the states of DatePicker's and TimePicker's inner FlyoutButton, a Button in the default Button
            // style, forced on it; and the control disabled, which disables the button through its binding.
            foreach (var state in new[] { "PointerOver", "Pressed", "Disabled" })
            {
                var st = state;
                list.Add(new Specimen { Name = "DatePicker", Variant = "flyout-button-" + st.ToLowerInvariant(), Height = 140, States = false, Make = () => new DatePicker { Header = "date picker", Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) }, Prepare = e => InnerState(e, "FlyoutButton", st) });
                list.Add(new Specimen { Name = "TimePicker", Variant = "flyout-button-" + st.ToLowerInvariant(), Height = 140, States = false, Make = () => new TimePicker { Header = "time picker", Time = new TimeSpan(12, 30, 0) }, Prepare = e => InnerState(e, "FlyoutButton", st) });
            }

            add("DatePicker", "disabled", 140, () => new DatePicker { Header = "date picker", Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero), IsEnabled = false });
            add("TimePicker", "disabled", 140, () => new TimePicker { Header = "time picker", Time = new TimeSpan(12, 30, 0), IsEnabled = false });
            add("AppBarButton", "default", 120, () => new AppBarButton { Label = "add", Icon = new SymbolIcon(Symbol.Add) });
            add("AppBarToggleButton", "unchecked", 120, () => new AppBarToggleButton { Label = "pin", Icon = new SymbolIcon(Symbol.Pin) });
            add("AppBarToggleButton", "checked", 120, () => new AppBarToggleButton { Label = "pin", Icon = new SymbolIcon(Symbol.Pin), IsChecked = true });
            add("ListViewItem", "default", 100, () => new ListViewItem { Content = "list view item" });
            add("ListViewItem", "selected", 100, () => new ListViewItem { Content = "list view item", IsSelected = true });
            add("ListView", "three-items", 260, () =>
            {
                var l = new ListView();
                l.Items.Add("one");
                l.Items.Add("two");
                l.Items.Add("three");
                l.SelectedIndex = 1;
                return l;
            });

            // Tier 4 (#211): the items and headers alone, each state forced on a fresh instance, and the lists with items.
            // A GridViewItem gets a 120 x 120 grey tile, so its border and earmark have something to sit on.
            add("ListBoxItem", "default", 100, () => new ListBoxItem { Content = "list box item" });
            add("ListBoxItem", "selected", 100, () => new ListBoxItem { Content = "list box item", IsSelected = true });
            add("ListBox", "three-items", 260, () =>
            {
                var l = new ListBox();
                l.Items.Add("one");
                l.Items.Add("two");
                l.Items.Add("three");
                l.SelectedIndex = 1;
                return l;
            });
            add("GridViewItem", "default", 160, () => new GridViewItem { Content = Tile("one") });
            add("GridViewItem", "selected", 160, () => new GridViewItem { Content = Tile("one"), IsSelected = true });
            add("GridView", "six-items", 300, () =>
            {
                var g = new GridView();
                foreach (var name in new[] { "one", "two", "three", "four", "five", "six" })
                {
                    g.Items.Add(Tile(name));
                }

                g.SelectedIndex = 1;
                return g;
            });
            add("ListViewHeaderItem", "default", 100, () => new ListViewHeaderItem { Content = "list view header" });
            add("GridViewHeaderItem", "default", 100, () => new GridViewHeaderItem { Content = "grid view header" });
            list.Add(new Specimen
            {
                Name = "FlipView",
                Variant = "three-items",
                Height = 300,
                FullBleed = true,
                Make = () =>
                {
                    var f = new FlipView();
                    foreach (var name in new[] { "one", "two", "three" })
                    {
                        f.Items.Add(new Grid { Background = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)), Children = { new TextBlock { Text = name, FontSize = 40 } } });
                    }

                    f.SelectedIndex = 1;
                    return f;
                },
            });

            // The ScrollViewer and ScrollBar (#113, for #116): the indicator shows only while the ScrollBar's IndicatorMode
            // is TouchIndicator, which the ScrollViewer's forced TouchIndicator state sets. A standalone ScrollBar is given
            // round numbers, so its thumb's size can be checked: 200 long, Maximum 100, ViewportSize 50.
            add("ScrollViewer", "rest", 300, () => Scrolled(0));
            add("ScrollViewer", "offset-200", 300, () => Scrolled(200));
            add("ScrollBar", "vertical", 240, () => new ScrollBar { Orientation = Orientation.Vertical, Height = 200, Maximum = 100, ViewportSize = 50, Value = 25, IndicatorMode = ScrollingIndicatorMode.TouchIndicator, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top });
            add("ScrollBar", "horizontal", 60, () => new ScrollBar { Orientation = Orientation.Horizontal, Width = 200, Maximum = 100, ViewportSize = 50, Value = 25, IndicatorMode = ScrollingIndicatorMode.TouchIndicator, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
            add("ScrollBar", "no-indicator", 240, () => new ScrollBar { Orientation = Orientation.Vertical, Height = 200, Maximum = 100, ViewportSize = 50, Value = 25, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top });

            // For #117 (ContentControl): a ContentPresenter placed on a page, outside any template, with a child smaller than
            // its slot, which shows whether the phone's implicit ContentPresenter style (HorizontalAlignment Left,
            // VerticalAlignment Top) reaches it; the same with Stretch set, to compare; and a bare ContentControl with a
            // Background, a Padding and a small child (its template is a lone ContentPresenter, with nothing to draw the
            // Background). Each sits in a 200 x 100 slot; the child is a 60 x 30 rectangle.
            list.Add(new Specimen { Name = "ContentPresenter", Variant = "page", Height = 130, States = false, Make = () => Slot(new ContentPresenter { Content = Child() }) });
            list.Add(new Specimen { Name = "ContentPresenter", Variant = "page-stretch", Height = 130, States = false, Make = () => Slot(new ContentPresenter { Content = Child(), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }) });
            list.Add(new Specimen { Name = "ContentControl", Variant = "background-padding", Height = 130, States = false, Make = () => Slot(new ContentControl { Content = Child(), Background = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)), Padding = new Thickness(12) }) });

            list.Add(new Specimen
            {
                Name = "Pivot",
                Variant = "two-items",
                Height = 320,
                FullBleed = true,
                Make = () =>
                {
                    var p = new Pivot { Title = "CORNFLOWER" };
                    p.Items.Add(new PivotItem { Header = "one", Content = new TextBlock { Text = "first pivot item" } });
                    p.Items.Add(new PivotItem { Header = "two", Content = new TextBlock { Text = "second pivot item" } });
                    return p;
                },
            });
            list.Add(new Specimen
            {
                Name = "Hub",
                Variant = "two-sections",
                Height = 400,
                FullBleed = true,
                Make = () =>
                {
                    var h = new Hub { Header = "hub" };
                    h.Sections.Add(new HubSection { Header = "one" });
                    h.Sections.Add(new HubSection { Header = "two" });
                    return h;
                },
            });

            return list;
        }

        /// <summary>A 120 x 120 grey tile with its name, a GridView item's content (#211).</summary>
        internal static FrameworkElement Tile(string name)
        {
            return new Grid { Width = 120, Height = 120, Background = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)), Children = { new TextBlock { Text = name, FontSize = 20, Margin = new Thickness(6) } } };
        }

        /// <summary>Forces a state on a named control inside the specimen's template, and gives it the suite's settle time.</summary>
        private static async Task<bool> InnerState(FrameworkElement element, string part, string state)
        {
            var inner = PopupCatalog.FindNamed(element, part) as Control;
            var applied = inner != null && VisualStateManager.GoToState(inner, state, false);
            await Task.Delay(Suite.StateSettleMs);
            return applied;
        }
    }
}
