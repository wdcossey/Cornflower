using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace RefApp
{
    /// <summary>
    /// A control that opens a popup: a flyout, a dialog, a picker, an open ComboBox. The host is placed on a stage
    /// like any specimen; <see cref="Open"/> opens the popup from it and returns whatever <see cref="Close"/> needs.
    /// </summary>
    internal sealed class PopupSpecimen
    {
        public string Name;
        public string Variant;
        public double Height = 100;
        public int SettleMs = 900;

        /// <summary>
        /// Shown by the system, as a MessageDialog is: outside the popups the app can list, drawn in the phone's
        /// theme, not the stage's (the app sets no theme of its own, so the runs, dark, can't tell the phone's from the
        /// app's), and not closed by cancelling its ShowAsync (2026-11-11). A second can't open while it shows
        /// (E_ACCESSDENIED), so it stays over every later capture: <see cref="Suite"/> runs one only when -Only picks
        /// it and nothing else, once, dark, and leaves it out otherwise.
        /// </summary>
        public bool System;
        public Func<FrameworkElement> MakeHost;
        public Func<FrameworkElement, object> Open;
        public Action<FrameworkElement, object> Close;

        /// <summary>
        /// For a popup the app can't list (<see cref="System"/>): given what <see cref="Open"/> returned, why it isn't
        /// showing, or null when it is. Asked once it has settled and again after the hold.
        /// </summary>
        public Func<object, string> Fault;

        /// <summary>
        /// Opens no XAML popup on purpose (an AutoSuggestBox at rest or with its list closed; the keyboard isn't one): the
        /// run doesn't count "no popup open" as an error for it.
        /// </summary>
        public bool NoPopup;
    }

    /// <summary>What the reference app opens and captures outside the stage. Add a popup by adding lines here.</summary>
    internal static class PopupCatalog
    {
        public static List<PopupSpecimen> All()
        {
            var list = new List<PopupSpecimen>();
            Func<FrameworkElement> button = () => new Button { Content = "open" };

            list.Add(Flyout("Flyout", "default", FlyoutPlacementMode.Top));
            list.Add(Flyout("Flyout", "full", FlyoutPlacementMode.Full));

            list.Add(new PopupSpecimen
            {
                Name = "MenuFlyout",
                Variant = "four-items",
                MakeHost = button,
                Open = host =>
                {
                    var m = new MenuFlyout();
                    m.Items.Add(new MenuFlyoutItem { Text = "first item" });
                    m.Items.Add(new ToggleMenuFlyoutItem { Text = "toggle item", IsChecked = true });
                    m.Items.Add(new MenuFlyoutSeparator());
                    m.Items.Add(new MenuFlyoutItem { Text = "disabled item", IsEnabled = false });
                    m.ShowAt(host);
                    return m;
                },
                Close = (host, h) => ((MenuFlyout)h).Hide(),
            });

            list.Add(Dialog("default", false));
            list.Add(Dialog("full-size", true));

            // Windows.UI.Popups.MessageDialog (B18): a one-line title, and one long enough to wrap; the message wraps in
            // both. One per run, and only when -Only picks it alone (PopupSpecimen.System), so each has a name of its own.
            list.Add(Message("MessageDialog", "default", "message dialog"));
            list.Add(Message("MessageDialogLongTitle", "long-title", "Agjy 0123 a message dialog title long enough to wrap"));

            list.Add(ListPicker("single", ListPickerFlyoutSelectionMode.Single));
            list.Add(ListPicker("multiple", ListPickerFlyoutSelectionMode.Multiple));

            list.Add(new PopupSpecimen
            {
                Name = "PickerFlyout",
                Variant = "confirmation-buttons",
                MakeHost = button,
                Open = host =>
                {
                    var p = new PickerFlyout { Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) }, ConfirmationButtonsVisible = true };
                    var op = p.ShowAtAsync(host);
                    return p;
                },
                Close = (host, h) => ((PickerFlyout)h).Hide(),
            });

            list.Add(new PopupSpecimen
            {
                Name = "DatePickerFlyout",
                Variant = "default",
                MakeHost = button,
                Open = host =>
                {
                    var d = new DatePickerFlyout { Date = new DateTimeOffset(2014, 7, 15, 0, 0, 0, TimeSpan.Zero) };
                    var op = d.ShowAtAsync(host);
                    return d;
                },
                Close = (host, h) => ((DatePickerFlyout)h).Hide(),
            });

            list.Add(new PopupSpecimen
            {
                Name = "TimePickerFlyout",
                Variant = "default",
                MakeHost = button,
                Open = host =>
                {
                    var t = new TimePickerFlyout { Time = new TimeSpan(12, 30, 0) };
                    var op = t.ShowAtAsync(host);
                    return t;
                },
                Close = (host, h) => ((TimePickerFlyout)h).Hide(),
            });

            // The page's app bar (#23): compact and minimal, closed and open. The phone draws it outside the page, so
            // only -Grab sees its pixels.
            list.Add(BottomAppBar("compact", AppBarClosedDisplayMode.Compact, false));
            list.Add(BottomAppBar("compact-open", AppBarClosedDisplayMode.Compact, true));
            list.Add(BottomAppBar("minimal", AppBarClosedDisplayMode.Minimal, false));
            list.Add(BottomAppBar("minimal-open", AppBarClosedDisplayMode.Minimal, true));

            // The open bar with other numbers of secondary commands (#136, for #133): none, one, three, four, five, and ten,
            // more than the room.
            list.Add(BottomAppBar("compact-open-0-secondary", AppBarClosedDisplayMode.Compact, true, 0));
            list.Add(BottomAppBar("compact-open-1-secondary", AppBarClosedDisplayMode.Compact, true, 1));
            list.Add(BottomAppBar("compact-open-3-secondary", AppBarClosedDisplayMode.Compact, true, 3));
            list.Add(BottomAppBar("compact-open-4-secondary", AppBarClosedDisplayMode.Compact, true, 4));
            list.Add(BottomAppBar("compact-open-5-secondary", AppBarClosedDisplayMode.Compact, true, 5));
            list.Add(BottomAppBar("compact-open-10-secondary", AppBarClosedDisplayMode.Compact, true, 10));

            // Every count from 3, which opens inline, to 10, which opens the full-screen picker: where it flips (#25).
            for (var count = 3; count <= 10; count++)
            {
                list.Add(Combo("open-" + count + "-items", count));
            }

            // #150. AutoSuggestBox: at rest, focused, with text, and its suggestion list open with 1, 3 and 12 suggestions.
            // The box is focused from code (its TextBox part); whether the keyboard comes up with that is part of the read.
            list.Add(Suggest("rest", null, 0, false));
            list.Add(Suggest("focused", null, 0, true));
            list.Add(Suggest("text", "ap", 0, true));
            list.Add(Suggest("list-1", "ap", 1, true));
            list.Add(Suggest("list-3", "a", 3, true));
            list.Add(Suggest("list-12", "a", 12, true));

            // #179. How far the page moves for a box the keyboard doesn't cover: an AutoSuggestBox and a TextBox 300 view
            // px down the stage (clear of the keyboard's top: 453.33, a PasswordBox's 513.75), at rest and focused, the box again with
            // AutoMaximizeSuggestionArea false; a TextBox 560 down (under the keyboard), on the page and in a ScrollViewer; a
            // PasswordBox at both places.
            // A "page top" line at the stage's top shows whether the page moved or only the ScrollViewer scrolled.
            list.Add(Panned("AutoSuggestBox", "mid-rest", 300, false, false, () => new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit" }));
            list.Add(Panned("AutoSuggestBox", "mid-focused", 300, true, false, () => new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit" }));
            list.Add(Panned("AutoSuggestBox", "mid-nomax", 300, true, false, () => new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit", AutoMaximizeSuggestionArea = false }));
            list.Add(Panned("TextBox", "mid-rest", 300, false, false, () => new TextBox { Header = "text box", Text = "Agjy" }));
            list.Add(Panned("TextBox", "mid-focused", 300, true, false, () => new TextBox { Header = "text box", Text = "Agjy" }));
            list.Add(Panned("TextBox", "low-rest", 560, false, false, () => new TextBox { Header = "text box", Text = "Agjy" }));
            list.Add(Panned("TextBox", "low-focused", 560, true, false, () => new TextBox { Header = "text box", Text = "Agjy" }));
            list.Add(Panned("TextBox", "scroll-low-focused", 560, true, true, () => new TextBox { Header = "text box", Text = "Agjy" }));
            list.Add(Panned("PasswordBox", "mid-rest", 300, false, false, () => new PasswordBox { Header = "password box", Password = "Agjy" }));
            list.Add(Panned("PasswordBox", "mid-focused", 300, true, false, () => new PasswordBox { Header = "password box", Password = "Agjy" }));
            list.Add(Panned("PasswordBox", "low-rest", 560, false, false, () => new PasswordBox { Header = "password box", Password = "Agjy" }));
            list.Add(Panned("PasswordBox", "low-focused", 560, true, false, () => new PasswordBox { Header = "password box", Password = "Agjy" }));

            // #179's second read: an AutoSuggestBox with AutoMaximizeSuggestionArea false that the keyboard covers, focused and
            // then, 1.2 s later, given "a" and a list of 3 or 12 suggestions; a PasswordBox with its reveal row under the
            // keyboard; the ScrollViewer's rest, and its VerticalOffset (each Panned box writes a collapsed "readout").
            Func<Control> lowSuggest = () => new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit", AutoMaximizeSuggestionArea = false };
            list.Add(Panned("AutoSuggestBox", "low-nomax-focused", 560, true, false, lowSuggest));
            list.Add(Panned("AutoSuggestBox", "low-nomax-list-3", 560, true, false, lowSuggest, Typed(3)));
            list.Add(Panned("AutoSuggestBox", "low-nomax-list-12", 560, true, false, lowSuggest, Typed(12)));
            list.Add(Panned("PasswordBox", "low-reveal-rest", 560, false, false, () => new PasswordBox { Header = "password box", Password = "Agjy", IsPasswordRevealButtonEnabled = true }));
            list.Add(Panned("PasswordBox", "low-reveal-focused", 560, true, false, () => new PasswordBox { Header = "password box", Password = "Agjy", IsPasswordRevealButtonEnabled = true }));
            list.Add(Panned("TextBox", "scroll-low-rest", 560, false, true, () => new TextBox { Header = "text box", Text = "Agjy" }));

            // ContentDialog's layout (#150): no button, the primary or the secondary alone, a long title and content
            // longer than the screen, and a TextBox in the content focused from code (the keyboard).
            list.Add(Dialog("no-buttons", false, null, null));
            list.Add(Dialog("primary-only", false, "ok", null));
            list.Add(Dialog("secondary-only", false, null, "cancel"));
            list.Add(Dialog("long", false, "ok", "cancel", LongTitle, LongBody()));
            list.Add(Dialog("long-full-size", true, "ok", "cancel", LongTitle, LongBody()));
            list.Add(KeyboardDialog());

            // MenuFlyout (#150): every kind of item with the first held Pressed, a menu longer than the screen, and the
            // four-item menu shown at targets lower down the page (where it sits, and when it goes above its target).
            list.Add(MenuStates());
            list.Add(MenuAt("long", 0, 24));
            list.Add(MenuAt("at-300", 300, 4));
            list.Add(MenuAt("at-500", 500, 4));
            list.Add(MenuAt("at-640", 640, 4));
            list.Add(MenuAt("at-720", 720, 4));

            // The pickers' bar (#150): a PickerFlyout without the confirmation buttons, and ListPickerFlyout Multiple
            // with two items checked.
            list.Add(new PopupSpecimen
            {
                Name = "PickerFlyout",
                Variant = "no-confirmation-buttons",
                MakeHost = button,
                Open = host =>
                {
                    var p = new PickerFlyout { Content = new TextBlock { Text = "picker flyout content", Margin = new Thickness(19) }, ConfirmationButtonsVisible = false };
                    var op = p.ShowAtAsync(host);
                    return p;
                },
                Close = (host, h) => ((PickerFlyout)h).Hide(),
            });
            list.Add(ListPicker("multiple-two-checked", ListPickerFlyoutSelectionMode.Multiple, 2));

            return list;
        }

        private static PopupSpecimen Flyout(string name, string variant, FlyoutPlacementMode placement)
        {
            return new PopupSpecimen
            {
                Name = name,
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var f = new Flyout
                    {
                        Placement = placement,
                        Content = new TextBlock { Text = "flyout content", TextWrapping = TextWrapping.Wrap },
                    };
                    f.ShowAt(host);
                    return f;
                },
                Close = (host, h) => ((Flyout)h).Hide(),
            };
        }

        private static PopupSpecimen Dialog(string variant, bool fullSize)
        {
            return Dialog(variant, fullSize, "ok", "cancel");
        }

        private const string LongTitle = "Agjy 0123 a content dialog title long enough to wrap onto a second line";

        /// <summary>Forty numbered lines of body text, longer than the screen.</summary>
        internal static string LongBody()
        {
            var lines = new List<string>();
            for (var i = 1; i <= 40; i++)
            {
                lines.Add("line " + i + " of the dialog's long body");
            }

            return string.Join("\n", lines);
        }

        /// <summary>A ContentDialog; a null button text leaves that button out.</summary>
        private static PopupSpecimen Dialog(string variant, bool fullSize, string primary, string secondary, string title = "content dialog", string body = "Agjy 0123 the dialog's body text, long enough to wrap onto a second line.")
        {
            return new PopupSpecimen
            {
                Name = "ContentDialog",
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var d = new ContentDialog
                    {
                        Title = title,
                        Content = body,
                        FullSizeDesired = fullSize,
                    };
                    if (primary != null)
                    {
                        d.PrimaryButtonText = primary;
                    }

                    if (secondary != null)
                    {
                        d.SecondaryButtonText = secondary;
                    }

                    var op = d.ShowAsync();
                    return d;
                },
                Close = (host, h) => ((ContentDialog)h).Hide(),
            };
        }

        /// <summary>
        /// A MessageDialog with two commands, shown with ShowAsync. Close cancels that operation, which the API documents
        /// as taking one down from code, but on the phone (2026-11-11, device 11) the dialog stayed open: see
        /// <see cref="PopupSpecimen.System"/>. Fault reads the operation's status.
        /// </summary>
        private static PopupSpecimen Message(string name, string variant, string title)
        {
            return new PopupSpecimen
            {
                Name = name,
                Variant = variant,
                System = true,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var d = new MessageDialog("Agjy 0123 the dialog's message, long enough to wrap onto a second line.", title);
                    d.Commands.Add(new UICommand("ok"));
                    d.Commands.Add(new UICommand("cancel"));
                    return d.ShowAsync();
                },
                Close = (host, h) =>
                {
                    // Null when ShowAsync threw, as it does (E_ACCESSDENIED) while another dialog shows.
                    var op = h as IAsyncOperation<IUICommand>;
                    if (op != null)
                    {
                        op.Cancel();
                    }
                },
                Fault = h =>
                {
                    // Started while the dialog shows; anything else means it closed or never opened.
                    var op = (IAsyncOperation<IUICommand>)h;
                    if (op.Status == AsyncStatus.Started)
                    {
                        return null;
                    }

                    var error = op.Status == AsyncStatus.Error && op.ErrorCode != null ? ", " + op.ErrorCode.GetType().Name + " 0x" + op.ErrorCode.HResult.ToString("X8") + ": " + op.ErrorCode.Message : "";
                    return "the dialog isn't showing: ShowAsync is " + op.Status + error;
                },
            };
        }

        private static PopupSpecimen ListPicker(string variant, ListPickerFlyoutSelectionMode mode, int checkedItems = 0)
        {

            return new PopupSpecimen
            {
                Name = "ListPickerFlyout",
                Variant = variant,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var p = new ListPickerFlyout
                    {
                        ItemsSource = new[] { "one", "two", "three", "four", "five" },
                        SelectionMode = mode,
                    };
                    // Single, or Multiple with none asked for: the second item selected, as before #150. Else items 2, 4...
                    if (checkedItems == 0)
                    {
                        p.SelectedIndex = 1;
                    }

                    // Items 2 and 4 of the five: no more than two can be checked this way; more fails this capture.
                    if (checkedItems > 2)
                    {
                        throw new ArgumentOutOfRangeException(nameof(checkedItems), "the five items give at most two checked, the second and the fourth");
                    }

                    for (var i = 0; i < checkedItems; i++)
                    {
                        p.SelectedItems.Add(((string[])p.ItemsSource)[i * 2 + 1]);
                    }

                    PickerFlyoutBase.SetTitle(p, "LIST PICKER");
                    var op = p.ShowAtAsync(host);
                    return p;
                },
                Close = (host, h) => ((ListPickerFlyout)h).Hide(),
            };
        }

        /// <summary>What <see cref="BottomAppBar"/> opened, for its Close.</summary>
        private sealed class AppBarHandle
        {
            public Page Page;
            public CommandBar Bar;
            public DispatcherTimer OpenTimer;
            public bool WantOpen;
            public bool Opened;
        }

        /// <summary>
        /// A CommandBar as the page's BottomAppBar, with three primary commands (one disabled, one a checked toggle) and
        /// two secondary ones (or <paramref name="secondary"/>: "first menu item", "second menu item", then "menu item
        /// 3" and on), set on the page that holds the stage and taken off again on close. The shell draws it, so only
        /// -Grab sees its pixels.
        /// </summary>
        private static PopupSpecimen BottomAppBar(string variant, AppBarClosedDisplayMode mode, bool open, int secondary = 2)
        {
            return new PopupSpecimen
            {
                Name = "BottomAppBar",
                Variant = variant,
                SettleMs = open ? 2200 : 1500,
                MakeHost = () => new TextBlock { Text = "page with an app bar", Margin = new Thickness(0, 12, 0, 0) },
                Open = host =>
                {
                    var bar = new CommandBar { ClosedDisplayMode = mode };
                    bar.PrimaryCommands.Add(new AppBarButton { Label = "add", Icon = new SymbolIcon(Symbol.Add) });
                    bar.PrimaryCommands.Add(new AppBarToggleButton { Label = "favorite", Icon = new SymbolIcon(Symbol.Favorite), IsChecked = true });
                    bar.PrimaryCommands.Add(new AppBarButton { Label = "delete", Icon = new SymbolIcon(Symbol.Delete), IsEnabled = false });
                    for (var i = 1; i <= secondary; i++)
                    {
                        bar.SecondaryCommands.Add(new AppBarButton { Label = i == 1 ? "first menu item" : i == 2 ? "second menu item" : "menu item " + i });
                    }

                    bar.RequestedTheme = ThemeOf(host);
                    var h = new AppBarHandle { Page = CurrentPage(), Bar = bar, WantOpen = open };
                    bar.Opened += (s, e) => h.Opened = true;
                    h.Page.BottomAppBar = bar;
                    if (open)
                    {
                        // Set as the bar is attached, IsOpen is ignored (2026-11-09): open it once the bar has loaded.
                        h.OpenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                        h.OpenTimer.Tick += (s, e) =>
                        {
                            h.OpenTimer.Stop();
                            bar.IsOpen = true;
                        };
                        bar.Loaded += (s, e) => h.OpenTimer.Start();
                    }

                    return h;
                },
                Close = (host, handle) =>
                {
                    var h = (AppBarHandle)handle;
                    if (h == null)
                    {
                        return;
                    }

                    if (h.OpenTimer != null)
                    {
                        h.OpenTimer.Stop();
                    }

                    // Never take a popup away mid-animation (see the README): close the bar, and detach it once closed.
                    if (h.Bar.IsOpen)
                    {
                        h.Bar.Closed += (s, e) => h.Page.BottomAppBar = null;
                        h.Bar.IsOpen = false;
                    }
                    else
                    {
                        h.Page.BottomAppBar = null;
                    }

                    if (h.WantOpen && !h.Opened)
                    {
                        throw new InvalidOperationException("the app bar never opened");
                    }
                },
            };
        }

        /// <summary>The theme the stage asked for: the first RequestedTheme set above the host.</summary>
        private static ElementTheme ThemeOf(FrameworkElement host)
        {
            for (DependencyObject e = host; e != null; e = VisualTreeHelper.GetParent(e))
            {
                var f = e as FrameworkElement;
                if (f != null && f.RequestedTheme != ElementTheme.Default)
                {
                    return f.RequestedTheme;
                }
            }

            return ElementTheme.Default;
        }

        /// <summary>The page the window's frame shows: the app's only page, MainPage.</summary>
        private static Page CurrentPage()
        {
            var frame = Window.Current.Content as Frame;
            var page = frame != null ? frame.Content as Page : null;
            if (page == null)
            {
                throw new InvalidOperationException("the window's content is not a Frame showing a Page");
            }

            return page;
        }

        /// <summary>
        /// A ContentDialog whose content is a line of text and a TextBox, the TextBox focused from code once the dialog
        /// has opened: the dialog with the keyboard up, if focus from code raises it.
        /// </summary>
        private static PopupSpecimen KeyboardDialog()
        {
            return new PopupSpecimen
            {
                Name = "ContentDialog",
                Variant = "keyboard",
                SettleMs = 1500,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var box = new TextBox { Header = "name", Text = "Agjy" };
                    var panel = new StackPanel();
                    panel.Children.Add(new TextBlock { Text = "Agjy 0123 the dialog's body text.", TextWrapping = TextWrapping.Wrap });
                    panel.Children.Add(box);
                    var d = new ContentDialog { Title = "content dialog", Content = panel, PrimaryButtonText = "ok", SecondaryButtonText = "cancel" };
                    // Focused at once on Opened, it didn't take (20261116-1934-dev11): give the content time to load.
                    d.Opened += async (s, e) =>
                    {
                        await Task.Delay(500);
                        box.Focus(FocusState.Programmatic);
                    };
                    var op = d.ShowAsync();
                    return d;
                },
                Close = (host, h) => ((ContentDialog)h).Hide(),
                Fault = h => FocusManager.GetFocusedElement() is TextBox ? null : "the dialog's TextBox didn't take the focus",
            };
        }

        /// <summary>
        /// An AutoSuggestBox with a header and placeholder; with <paramref name="focus"/> its TextBox part is focused from
        /// code, then <paramref name="text"/> set, then <paramref name="suggestions"/> fruit names given as its items and
        /// the list opened (none: left closed).
        /// </summary>
        private static PopupSpecimen Suggest(string variant, string text, int suggestions, bool focus)
        {
            return new PopupSpecimen
            {
                Name = "AutoSuggestBox",
                Variant = variant,
                Height = 400,
                SettleMs = 1500,
                NoPopup = suggestions == 0,
                MakeHost = () => new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit" },
                Open = host =>
                {
                    var box = (AutoSuggestBox)host;
                    if (focus)
                    {
                        var inner = FindNamed(box, "TextBox") as Control;
                        if (inner == null || !inner.Focus(FocusState.Programmatic))
                        {
                            throw new InvalidOperationException("the AutoSuggestBox's TextBox part didn't take focus");
                        }
                    }

                    if (text != null)
                    {
                        box.Text = text;
                    }

                    if (suggestions > 0)
                    {
                        box.ItemsSource = Fruits.Take(suggestions).ToList();
                        box.IsSuggestionListOpen = true;
                    }

                    return null;
                },
                Close = (host, h) => ((AutoSuggestBox)host).IsSuggestionListOpen = false,
            };
        }

        /// <summary>
        /// A box (<paramref name="make"/>: an AutoSuggestBox, a TextBox or a PasswordBox) <paramref name="top"/> view px below a "page top"
        /// line at the stage's top, focused from code with <paramref name="focus"/> (an AutoSuggestBox's TextBox part); with
        /// <paramref name="scroll"/> the box sits in a ScrollViewer with 800 px of content below it, the line outside it.
        /// The page's move is read from the screen grab: the line's and the box's places at rest against focused.
        /// <paramref name="then"/>, if given, runs on the box 1.2 s after the focus (an AutoSuggestBox's typing). Before the
        /// stage is dumped, a collapsed TextBlock named "readout" is given what the app can read: the ScrollViewer's
        /// VerticalOffset, the box's top and bottom in the window, and the InputPane's OccludedRect.
        /// </summary>
        private static PopupSpecimen Panned(string name, string variant, double top, bool focus, bool scroll, Func<Control> make, Action<Control> then = null)
        {
            var settle = then != null ? 3000 : 1500;
            return new PopupSpecimen
            {
                Name = name,
                Variant = variant,
                Height = 800,
                SettleMs = settle,
                NoPopup = then == null,
                MakeHost = () =>
                {
                    var box = make();
                    box.VerticalAlignment = VerticalAlignment.Top;
                    box.Margin = new Thickness(0, top, 0, 0);
                    var grid = new Grid { Height = 780, VerticalAlignment = VerticalAlignment.Top, Tag = box };
                    grid.Children.Add(new TextBlock { Text = "page top", VerticalAlignment = VerticalAlignment.Top });
                    grid.Children.Add(new TextBlock { Name = "readout", Visibility = Visibility.Collapsed });
                    if (scroll)
                    {
                        var content = new StackPanel();
                        content.Children.Add(box);
                        content.Children.Add(new Border { Height = 800 });
                        grid.Children.Add(new ScrollViewer { Name = "scroller", Content = content });
                    }
                    else
                    {
                        grid.Children.Add(box);
                    }

                    return grid;
                },
                Open = host =>
                {
                    if (focus)
                    {
                        var box = (Control)host.Tag;
                        var target = box is AutoSuggestBox ? FindNamed(box, "TextBox") as Control : box;
                        if (target == null || !target.Focus(FocusState.Programmatic))
                        {
                            throw new InvalidOperationException("the " + name + " didn't take focus");
                        }
                    }

                    if (then != null)
                    {
                        Later(host, 1200, () => then((Control)host.Tag));
                    }

                    Later(host, settle - 300, () => Readout(host));
                    return null;
                },
                Close = (host, h) =>
                {
                    var box = host.Tag as AutoSuggestBox;
                    if (box != null)
                    {
                        box.IsSuggestionListOpen = false;
                    }
                },
            };
        }

        /// <summary>An AutoSuggestBox's typing, for <see cref="Panned"/>: "a", and a list of the first <paramref name="count"/> fruits, opened.</summary>
        private static Action<Control> Typed(int count)
        {
            return c =>
            {
                var box = (AutoSuggestBox)c;
                box.Text = "a";
                box.ItemsSource = Fruits.Take(count).ToList();
                box.IsSuggestionListOpen = true;
            };
        }

        /// <summary>Runs <paramref name="a"/> on the UI thread after <paramref name="ms"/>; a fault goes into the host's readout.</summary>
        private static async void Later(FrameworkElement host, int ms, Action a)
        {
            await Task.Delay(ms);
            try
            {
                a();
            }
            catch (Exception ex)
            {
                var readout = FindNamed(host, "readout") as TextBlock;
                if (readout != null)
                {
                    readout.Text = "fault: " + ex.GetType().Name + ": " + ex.Message;
                }
            }
        }

        /// <summary>Writes what the app can read of the pan into a <see cref="Panned"/> host's readout.</summary>
        private static void Readout(FrameworkElement host)
        {
            var box = (Control)host.Tag;
            var scroller = FindNamed(host, "scroller") as ScrollViewer;
            var r = box.TransformToVisual(null).TransformBounds(new Rect(0, 0, box.ActualWidth, box.ActualHeight));
            var o = Windows.UI.ViewManagement.InputPane.GetForCurrentView().OccludedRect;
            ((TextBlock)FindNamed(host, "readout")).Text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "offset {0}; box {1} to {2}; occluded {3} {4} {5} {6}",
                scroller != null ? scroller.VerticalOffset : double.NaN,
                r.Top,
                r.Bottom,
                o.X,
                o.Y,
                o.Width,
                o.Height);
        }

        /// <summary>Twelve fruit names, the AutoSuggestBox's suggestions.</summary>
        internal static readonly string[] Fruits = { "apple", "apricot", "avocado", "banana", "blackberry", "blueberry", "cherry", "grape", "lemon", "mango", "orange", "peach" };

        /// <summary>
        /// A MenuFlyout of every kind of item: an item, a ToggleMenuFlyoutItem checked and one unchecked, a separator, a
        /// disabled item and a last item; once open, the first item is put in its Pressed state.
        /// </summary>
        private static PopupSpecimen MenuStates()
        {
            return new PopupSpecimen
            {
                Name = "MenuFlyout",
                Variant = "states",
                SettleMs = 1200,
                MakeHost = () => new Button { Content = "open" },
                Open = host =>
                {
                    var m = new MenuFlyout();
                    var first = new MenuFlyoutItem { Text = "pressed item" };
                    m.Items.Add(first);
                    m.Items.Add(new ToggleMenuFlyoutItem { Text = "toggle checked", IsChecked = true });
                    m.Items.Add(new ToggleMenuFlyoutItem { Text = "toggle unchecked", IsChecked = false });
                    m.Items.Add(new MenuFlyoutSeparator());
                    m.Items.Add(new MenuFlyoutItem { Text = "disabled item", IsEnabled = false });
                    m.Items.Add(new MenuFlyoutItem { Text = "last item" });
                    // Put in Pressed on Opened, it didn't show (20261116-1934-dev11): after the menu's 0.5 s storyboard instead.
                    m.Opened += async (s, e) =>
                    {
                        await Task.Delay(700);
                        VisualStateManager.GoToState(first, "Pressed", false);
                    };
                    m.ShowAt(host);
                    return m;
                },
                Close = (host, h) => ((MenuFlyout)h).Hide(),
                Fault = h => IsPressed(((MenuFlyout)h).Items[0]) ? null : "the first item isn't in its Pressed state",
            };
        }

        /// <summary>Whether a control's template is in its CommonStates group's Pressed state.</summary>
        private static bool IsPressed(Control control)
        {
            var root = VisualTreeHelper.GetChildrenCount(control) > 0 ? VisualTreeHelper.GetChild(control, 0) as FrameworkElement : null;
            var group = root != null ? VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g => g.Name == "CommonStates") : null;
            return group != null && group.CurrentState != null && group.CurrentState.Name == "Pressed";
        }

        /// <summary>
        /// A MenuFlyout of <paramref name="items"/> items shown at a Button whose top is <paramref name="top"/> view px
        /// below the stage's top margin: where the menu sits against its target, and when it goes above it.
        /// </summary>
        private static PopupSpecimen MenuAt(string variant, double top, int items)
        {
            return new PopupSpecimen
            {
                Name = "MenuFlyout",
                Variant = variant,
                Height = 800,
                MakeHost = () =>
                {
                    var target = new Button { Content = "open", Margin = new Thickness(0, top, 0, 0), VerticalAlignment = VerticalAlignment.Top };
                    var grid = new Grid { Height = 780, VerticalAlignment = VerticalAlignment.Top, Tag = target };
                    grid.Children.Add(target);
                    return grid;
                },
                Open = host =>
                {
                    var m = new MenuFlyout();
                    for (var i = 1; i <= items; i++)
                    {
                        m.Items.Add(new MenuFlyoutItem { Text = "menu item " + i });
                    }

                    m.ShowAt((FrameworkElement)host.Tag);
                    return m;
                },
                Close = (host, h) => ((MenuFlyout)h).Hide(),
            };
        }

        /// <summary>The first element of that name under root, in the visual tree.</summary>
        internal static FrameworkElement FindNamed(DependencyObject root, string name)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var f = child as FrameworkElement;
                if (f != null && f.Name == name)
                {
                    return f;
                }

                var found = FindNamed(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static PopupSpecimen Combo(string variant, int count)
        {
            return new PopupSpecimen
            {
                Name = "ComboBox",
                Variant = variant,
                Height = 400,
                MakeHost = () =>
                {
                    var c = new ComboBox { Header = "combo box" };
                    for (var i = 1; i <= count; i++)
                    {
                        c.Items.Add("item " + i);
                    }

                    c.SelectedIndex = 0;
                    return c;
                },
                Open = host =>
                {
                    ((ComboBox)host).IsDropDownOpen = true;
                    return null;
                },
                Close = (host, h) => ((ComboBox)host).IsDropDownOpen = false,
            };
        }
    }
}
