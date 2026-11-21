using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>Grouping: WinRT's grouped <c>CollectionViewSource</c> and <c>GroupStyle</c>, for Avalonia's ListBox.</summary>
/// <example>
/// <code>&lt;ListBox cf:ListBoxOptions.GroupedItemsSource="{Binding Groups}" cf:ListBoxOptions.GroupHeaderTemplate="{StaticResource Header}" /&gt;</code>
/// </example>
public static partial class ListBoxOptions
{
    /// <summary>
    /// The list's groups, each an <see cref="IEnumerable"/> of its items, as an <c>IGrouping&lt;TKey, TItem&gt;</c> is:
    /// WinRT's <c>CollectionViewSource</c> with <c>IsSourceGrouped</c>. The list's <see cref="ItemsControl.ItemsSource"/>
    /// becomes each group's <see cref="ListViewHeaderItem"/> followed by its items; a group with no items shows nothing
    /// (WinRT's <c>HidesIfEmpty</c>). A change to the groups, or to a group that raises <c>CollectionChanged</c>, lays
    /// the list out again, and the items still there stay selected. Null, the default, leaves the list ungrouped.
    /// </summary>
    /// <remarks>
    /// The headers are items of the list, so <see cref="SelectingItemsControl.SelectedIndex"/> counts them, where
    /// WinRT's counts items alone; a header is never selected, so <c>SelectedItem</c> and <c>SelectedItems</c> are the
    /// phone's. A list whose items panel is a vertical stack (the ListView's) pins the header of the group at its top:
    /// a <see cref="ListViewHeaderItem"/> put in its template's <c>PART_StickyHeader</c>, a <see cref="Border"/>, if it
    /// has one.
    /// </remarks>
    public static readonly AttachedProperty<IEnumerable?> GroupedItemsSourceProperty =
        AvaloniaProperty.RegisterAttached<ListBox, IEnumerable?>("GroupedItemsSource", typeof(ListBoxOptions));

    /// <summary>The template of each group's header, its data the group: WinRT's <c>GroupStyle.HeaderTemplate</c>.</summary>
    public static readonly AttachedProperty<IDataTemplate?> GroupHeaderTemplateProperty =
        AvaloniaProperty.RegisterAttached<ListBox, IDataTemplate?>("GroupHeaderTemplate", typeof(ListBoxOptions));

    private static readonly AttachedProperty<Grouping?> GroupingProperty =
        AvaloniaProperty.RegisterAttached<ListBox, Grouping?>("Grouping", typeof(ListBoxOptions));

    public static IEnumerable? GetGroupedItemsSource(ListBox list) => list.GetValue(GroupedItemsSourceProperty);

    public static void SetGroupedItemsSource(ListBox list, IEnumerable? value) => list.SetValue(GroupedItemsSourceProperty, value);

    public static IDataTemplate? GetGroupHeaderTemplate(ListBox list) => list.GetValue(GroupHeaderTemplateProperty);

    public static void SetGroupHeaderTemplate(ListBox list, IDataTemplate? value) => list.SetValue(GroupHeaderTemplateProperty, value);

    /// <summary>
    /// Scrolls a grouped list so that a group's header is at its top, as far as the list scrolls: for SemanticZoom's jump
    /// list. False if the list isn't grouped or shows no header for the group.
    /// </summary>
    internal static bool ScrollGroupToTop(ListBox list, object group) =>
        list.GetValue(GroupingProperty)?.ScrollToTop(group) ?? false;

    private static void HookGrouping()
    {
        GroupedItemsSourceProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            list.GetValue(GroupingProperty)?.Detach();
            list.ClearValue(GroupingProperty);
            if (e.GetNewValue<IEnumerable?>() is { } source)
            {
                list.SetValue(GroupingProperty, new Grouping(list, source));
            }
            else
            {
                list.ClearValue(ItemsControl.ItemsSourceProperty);
            }
        });
        GroupHeaderTemplateProperty.Changed.AddClassHandler<ListBox>((list, _) => list.GetValue(GroupingProperty)?.Rebuild());
    }

    /// <summary>A grouped list's state: its flattened items, its headers, and its pinned header.</summary>
    private sealed class Grouping : IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
    {
        private readonly ListBox _list;
        private readonly IEnumerable _source;
        private readonly List<INotifyCollectionChanged> _watched = [];
        private readonly List<object> _groups = [];
        private readonly List<ListViewHeaderItem> _headers = [];
        private readonly List<int> _headerIndexes = [];
        private readonly List<int> _groupOf = [];
        private readonly TranslateTransform _push = new();
        private List<object?> _flat = [];
        private Border? _stickyHost;
        private ListViewHeaderItem? _sticky;
        private bool _letGoPending;

        public Grouping(ListBox list, IEnumerable source)
        {
            _list = list;
            _source = source;
            _list.ContainerPrepared += OnContainerPrepared;
            _list.SelectionChanged += OnSelectionChanged;
            _list.TemplateApplied += OnTemplateApplied;
            _list.LayoutUpdated += OnLayoutUpdated;
            FindSticky(_list.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Name == "PART_StickyHeader" && ReferenceEquals(b.TemplatedParent, _list)));
            Rebuild();
        }

        public void Detach()
        {
            _list.ContainerPrepared -= OnContainerPrepared;
            _list.SelectionChanged -= OnSelectionChanged;
            _list.TemplateApplied -= OnTemplateApplied;
            _list.LayoutUpdated -= OnLayoutUpdated;
            Unwatch();
            FindSticky(null);
        }

        public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e) => Rebuild();

        /// <summary>Flattens the groups again, and selects again the items still there.</summary>
        public void Rebuild()
        {
            var selected = _list.SelectedItems?.Cast<object?>().ToList() ?? [];
            var template = GetGroupHeaderTemplate(_list);
            Unwatch();
            Watch(_source);
            _groups.Clear();
            _headers.Clear();
            _headerIndexes.Clear();
            _groupOf.Clear();
            var flat = new List<object?>();
            foreach (var group in _source)
            {
                if (group is not IEnumerable items)
                {
                    continue;
                }

                Watch(group);
                var start = flat.Count;
                flat.Add(null);
                foreach (var item in items)
                {
                    flat.Add(item);
                }

                if (flat.Count == start + 1)
                {
                    flat.RemoveAt(start);
                    continue;
                }

                var header = new ListViewHeaderItem { Content = group, ContentTemplate = template };
                flat[start] = header;
                _groups.Add(group);
                _headers.Add(header);
                _headerIndexes.Add(start);
                _groupOf.AddRange(Enumerable.Repeat(_groups.Count - 1, flat.Count - start));
            }

            _flat = flat;
            _list.ItemsSource = flat;
            foreach (var item in selected)
            {
                var index = flat.IndexOf(item);
                if (index >= 0)
                {
                    _list.Selection.Select(index);
                }
            }

            if (_sticky is not null)
            {
                _sticky.ContentTemplate = template;
            }
        }

        public bool ScrollToTop(object group)
        {
            var at = _groups.IndexOf(group);
            if (at < 0)
            {
                return false;
            }

            var index = _headerIndexes[at];
            _list.ScrollIntoView(index);
            _list.UpdateLayout();
            if (_list.Scroll is not ScrollViewer scroll || TopOf(_headers[at], scroll) is not { } y)
            {
                return false;
            }

            scroll.Offset = scroll.Offset.WithY(scroll.Offset.Y + y);
            _list.UpdateLayout();
            return true;
        }

        private void Watch(object source)
        {
            if (source is INotifyCollectionChanged changing)
            {
                WeakEvents.CollectionChanged.Subscribe(changing, this);
                _watched.Add(changing);
            }
        }

        private void Unwatch()
        {
            foreach (var changing in _watched)
            {
                WeakEvents.CollectionChanged.Unsubscribe(changing, this);
            }

            _watched.Clear();
        }

        private bool IsHeader(int index) => index >= 0 && index < _flat.Count && _flat[index] is ListViewHeaderItem;

        /// <summary>
        /// Avalonia puts the list's <see cref="ItemsControl.ItemContainerTheme"/> on a header, a ListBoxItem (the
        /// GridView's item theme): taken off, so the header wears its own.
        /// </summary>
        private static void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (e.Container is ListViewHeaderItem header)
            {
                header.ClearValue(StyledElement.ThemeProperty);
            }
        }

        /// <summary>
        /// A header taken into the selection (select all, a Shift range, code) is let go, just after: changed while the
        /// list raises the change, the list's <c>SelectedItems</c> would take the items in twice.
        /// </summary>
        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Not by the event's AddedItems, which are empty when a new ItemsSource selects AlwaysSelected's first item.
            if (_letGoPending || !ReferenceEquals(e.Source, _list) || !_list.Selection.SelectedIndexes.Any(IsHeader))
            {
                return;
            }

            _letGoPending = true;
            Dispatcher.UIThread.Post(LetGoOfHeaders, DispatcherPriority.Send);
        }

        private void LetGoOfHeaders()
        {
            _letGoPending = false;
            var selection = _list.Selection;
            var headers = selection.SelectedIndexes.Where(IsHeader).ToList();
            if (headers.Count == 0)
            {
                return;
            }

            var itemsLeft = selection.Count > headers.Count;

            selection.BeginBatchUpdate();
            foreach (var index in headers)
            {
                selection.Deselect(index);
            }

            // AlwaysSelected would choose the header again: its first item instead.
            if (!itemsLeft && _list.SelectionMode.HasFlag(SelectionMode.AlwaysSelected))
            {
                selection.Select(headers[0] + 1);
            }

            selection.EndBatchUpdate();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) =>
            FindSticky(e.NameScope.Find<Border>("PART_StickyHeader"));

        /// <summary>Takes the pinned header out of the last template's host, and puts one in this one's.</summary>
        private void FindSticky(Border? host)
        {
            if (_stickyHost is not null)
            {
                _stickyHost.IsVisible = false;
                _stickyHost.Child = null;
                _stickyHost.RenderTransform = null;
            }

            _stickyHost = host;
            _sticky = null;
            if (host is not null)
            {
                _sticky = new ListViewHeaderItem { ContentTemplate = GetGroupHeaderTemplate(_list) };
                host.Child = _sticky;
            }
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateSticky();

        /// <summary>
        /// The pinned header: the group whose items are at the top of the viewport, once its own header has gone above
        /// the top, pushed up by the next group's header as it arrives. Only in a vertical stack, as the phone's
        /// <c>ItemsStackPanel</c> pins and its <c>ItemsWrapGrid</c> doesn't.
        /// </summary>
        private void UpdateSticky()
        {
            if (_stickyHost is null || _sticky is null)
            {
                return;
            }

            object? group = null;
            var push = 0.0;
            if (_list.Scroll is ScrollViewer scroll &&
                _list.ItemsPanelRoot is VirtualizingStackPanel { Orientation: Orientation.Vertical } or StackPanel { Orientation: Orientation.Vertical } &&
                TopIndex(scroll) is { } top)
            {
                var at = _groupOf[top];
                if (TopOf(_headers[at], scroll) is not { } own || own < 0)
                {
                    group = _groups[at];
                    if (at + 1 < _headers.Count && TopOf(_headers[at + 1], scroll) is { } next)
                    {
                        push = Math.Min(0, next - _stickyHost.Bounds.Height);
                    }
                }
            }

            _stickyHost.IsVisible = group is not null;
            if (group is null)
            {
                return;
            }

            _sticky.Content = group;
            _push.Y = push;
            _stickyHost.RenderTransform = _push;
        }

        /// <summary>The index of the first realized container that reaches below the viewport's top.</summary>
        private int? TopIndex(ScrollViewer scroll)
        {
            int? top = null;
            foreach (var container in _list.GetRealizedContainers())
            {
                if (TopOf(container, scroll) is { } y && y + container.Bounds.Height > 0 &&
                    _list.IndexFromContainer(container) is var index and >= 0 && index < _groupOf.Count &&
                    (top is null || index < top))
                {
                    top = index;
                }
            }

            return top;
        }

        /// <summary>A realized, shown container's top, in the viewport; null if it isn't.</summary>
        private Visual? Realized(Control container) =>
            container.IsVisible && _list.IndexFromContainer(container) >= 0 ? container : null;

        private double? TopOf(Control container, ScrollViewer scroll) =>
            Realized(container)?.TranslatePoint(default, scroll)?.Y;
    }
}
