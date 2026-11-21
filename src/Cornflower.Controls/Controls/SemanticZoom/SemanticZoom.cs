using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>
/// The Windows Phone 8.1 SemanticZoom: a grouped list (<see cref="ZoomedInView"/>) and its jump list
/// (<see cref="ZoomedOutView"/>), a popup over the whole window, opened by a tap on a group's header and closed by a tap
/// on a tile, which brings that group's header to the top of the list, or by Back. Behaviour only: a theme draws it,
/// places the popup and moves it (Wp81's covers the window, with no dim and no background, in the window's theme, and
/// takes Back).
/// </summary>
/// <remarks>
/// <para>The zoomed-in view is a <see cref="ListBox"/> grouped by <see cref="ListBoxOptions.GroupedItemsSource"/>: a tap on
/// one of its <see cref="ListViewHeaderItem"/>s, the pinned one too, zooms out. The zoomed-out view is a
/// <see cref="ListBox"/> of the same groups, all of them, the empty ones too (a GridView:
/// <c>Theme="{DynamicResource Wp81GridViewTheme}"</c>), its tiles the app's template, as on the phone, coloured by
/// <see cref="JumpListItemBackgroundConverter"/> and <see cref="JumpListItemForegroundConverter"/>. Its item click is
/// turned on (<see cref="ListBoxOptions.IsItemClickEnabledProperty"/>), so a tap on a tile selects nothing: a tile of a
/// group with items zooms in, one of an empty group does nothing. Another kind of view changes view only by
/// <see cref="IsZoomedInViewActive"/> or <see cref="ToggleActiveView"/>.</para>
/// <para>Both events come at once, in the change's own call, <see cref="ViewChangeStarted"/> then
/// <see cref="ViewChangeCompleted"/>, as on the phone (1 to 2 ms after the release); zooming in, the list is scrolled
/// between them. The popup's motion is the theme's.</para>
/// <para>With <see cref="CanChangeViews"/> false nothing changes the view: a tap on a header does nothing. The phone
/// closes the app there (#211); Cornflower doesn't copy that.</para>
/// <para>The template's <c>PART_ZoomedInPresenter</c> shows the zoomed-in view and <c>PART_Popup</c> (a
/// <see cref="Avalonia.Controls.Primitives.Popup"/>) holds the zoomed-out one, open while it is the active view.</para>
/// </remarks>
/// <example>
/// <code>
/// &lt;cf:SemanticZoom&gt;
///   &lt;cf:SemanticZoom.ZoomedInView&gt;
///     &lt;ListBox cf:ListBoxOptions.GroupedItemsSource="{Binding Contacts}" cf:ListBoxOptions.GroupHeaderTemplate="{StaticResource Header}" /&gt;
///   &lt;/cf:SemanticZoom.ZoomedInView&gt;
///   &lt;cf:SemanticZoom.ZoomedOutView&gt;
///     &lt;ListBox ItemsSource="{Binding Contacts}" ItemTemplate="{StaticResource Tile}" Theme="{DynamicResource Wp81GridViewTheme}" /&gt;
///   &lt;/cf:SemanticZoom.ZoomedOutView&gt;
/// &lt;/cf:SemanticZoom&gt;
/// </code>
/// </example>
[TemplatePart(ZoomedInPresenterPart, typeof(ContentPresenter))]
[TemplatePart(PopupPart, typeof(Popup))]
[TemplatePart(PopupBorderPart, typeof(Border))]
[TemplatePart(ZoomedOutPresenterPart, typeof(ContentPresenter))]
public class SemanticZoom : TemplatedControl
{
    /// <summary>The grouped list: WinRT's <c>ZoomedInView</c>.</summary>
    public static readonly StyledProperty<Control?> ZoomedInViewProperty =
        AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedInView));

    /// <summary>The jump list: WinRT's <c>ZoomedOutView</c>.</summary>
    public static readonly StyledProperty<Control?> ZoomedOutViewProperty =
        AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedOutView));

    /// <summary>Whether the grouped list is the active view, the jump list closed: WinRT's <c>IsZoomedInViewActive</c>. True by default.</summary>
    public static readonly DirectProperty<SemanticZoom, bool> IsZoomedInViewActiveProperty =
        AvaloniaProperty.RegisterDirect<SemanticZoom, bool>(nameof(IsZoomedInViewActive), z => z.IsZoomedInViewActive,
            (z, v) => z.IsZoomedInViewActive = v, unsetValue: true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether the view can change: WinRT's <c>CanChangeViews</c>. True by default.</summary>
    public static readonly StyledProperty<bool> CanChangeViewsProperty =
        AvaloniaProperty.Register<SemanticZoom, bool>(nameof(CanChangeViews), true);

    /// <summary>Where the template puts each view across: WinRT's <c>Control.HorizontalContentAlignment</c>, which Avalonia's <see cref="TemplatedControl"/> lacks.</summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<SemanticZoom>();

    /// <summary>Where the template puts each view down: WinRT's <c>Control.VerticalContentAlignment</c>.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<SemanticZoom>();

    private const string ZoomedInPresenterPart = "PART_ZoomedInPresenter";
    private const string PopupPart = "PART_Popup";
    private const string PopupBorderPart = "PART_PopupBorder";
    private const string ZoomedOutPresenterPart = "PART_ZoomedOutPresenter";

    private bool _zoomedIn = true;
    private Popup? _popup;
    private ListBox? _jumpList;

    static SemanticZoom()
    {
        ZoomedInViewProperty.Changed.AddClassHandler<SemanticZoom>((zoom, e) => zoom.Adopt(e));
        ZoomedOutViewProperty.Changed.AddClassHandler<SemanticZoom>((zoom, e) =>
        {
            zoom.Adopt(e);
            zoom.OnZoomedOutViewChanged(e);
        });
    }

    public SemanticZoom()
    {
        AddHandler(TappedEvent, OnTapped);
    }

    /// <summary>Raised as the view starts to change, before <see cref="IsZoomedInViewActive"/> does.</summary>
    public event EventHandler<SemanticZoomViewChangedEventArgs>? ViewChangeStarted;

    /// <summary>Raised as the view has changed: at once after <see cref="ViewChangeStarted"/>, the popup's motion still to come.</summary>
    public event EventHandler<SemanticZoomViewChangedEventArgs>? ViewChangeCompleted;

    /// <inheritdoc cref="ZoomedInViewProperty"/>
    public Control? ZoomedInView
    {
        get => GetValue(ZoomedInViewProperty);
        set => SetValue(ZoomedInViewProperty, value);
    }

    /// <inheritdoc cref="ZoomedOutViewProperty"/>
    public Control? ZoomedOutView
    {
        get => GetValue(ZoomedOutViewProperty);
        set => SetValue(ZoomedOutViewProperty, value);
    }

    /// <summary>
    /// Whether the grouped list is the active view. Set, it changes the view as a tap would, both events raised, with no
    /// source or destination zooming in, and the top item and its group zooming out; with
    /// <see cref="CanChangeViews"/> false it stays as it is.
    /// </summary>
    public bool IsZoomedInViewActive
    {
        get => _zoomedIn;
        set
        {
            if (value == _zoomedIn)
                return;
            if (value)
            {
                ChangeView(true, null, null, false);
            }
            else
            {
                var top = TopItem();
                ChangeView(false, top, GroupOf(top), false);
            }
        }
    }

    /// <inheritdoc cref="HorizontalContentAlignmentProperty"/>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <inheritdoc cref="VerticalContentAlignmentProperty"/>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <inheritdoc cref="CanChangeViewsProperty"/>
    public bool CanChangeViews
    {
        get => GetValue(CanChangeViewsProperty);
        set => SetValue(CanChangeViewsProperty, value);
    }

    /// <summary>The template's popup, once it is applied.</summary>
    internal Popup? Popup => _popup;

    /// <summary>
    /// Set by a theme that moves the jump list in: called in place of opening the popup at once; the theme opens it.
    /// None by default.
    /// </summary>
    internal Action? OpenMotion { get; set; }

    /// <summary>
    /// Set by a theme that moves the jump list out: called in place of closing the popup at once, true when a tile's tap
    /// closes it; the theme closes it. None by default.
    /// </summary>
    internal Action<bool>? CloseMotion { get; set; }

    /// <summary>Changes the view, as setting <see cref="IsZoomedInViewActive"/> to its other value does: WinRT's <c>ToggleActiveView</c>.</summary>
    public void ToggleActiveView() => IsZoomedInViewActive = !IsZoomedInViewActive;

    /// <summary>Back, from a theme that takes it while the jump list is open: zooms in, with no source or destination.</summary>
    internal void ZoomInByBack() => ChangeView(true, null, null, false);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_popup is not null && _popup.IsOpen)
            _popup.IsOpen = false;
        _popup = e.NameScope.Find<Popup>(PopupPart);
        if (!_zoomedIn)
            OpenPopup();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // A popup closes as its owner leaves the tree: the jump list, still the active view, opens again as it comes back.
        if (!_zoomedIn && _popup is { IsOpen: false })
            OpenPopup();
    }

    /// <summary>
    /// The view changes: <see cref="ViewChangeStarted"/>, the new <see cref="IsZoomedInViewActive"/>, zooming in the list
    /// scrolled to the destination's header, the popup asked to open or close, then <see cref="ViewChangeCompleted"/>.
    /// </summary>
    private void ChangeView(bool zoomIn, object? source, object? destination, bool byTile)
    {
        if (zoomIn == _zoomedIn || !CanChangeViews)
            return;
        ViewChangeStarted?.Invoke(this, new SemanticZoomViewChangedEventArgs(_zoomedIn, source, destination));
        var from = _zoomedIn;
        SetAndRaise(IsZoomedInViewActiveProperty, ref _zoomedIn, zoomIn);
        if (zoomIn)
        {
            if (destination is not null && ListOf(ZoomedInView) is { } list && !ListBoxOptions.ScrollGroupToTop(list, destination))
                list.ScrollIntoView(destination);
            ClosePopup(byTile);
        }
        else
        {
            OpenPopup();
        }

        ViewChangeCompleted?.Invoke(this, new SemanticZoomViewChangedEventArgs(from, source, destination));
    }

    private void OpenPopup()
    {
        if (_popup is null || !this.IsAttachedToVisualTree())
            return;
        if (OpenMotion is { } motion)
            motion();
        else
            _popup.IsOpen = true;
    }

    private void ClosePopup(bool byTile)
    {
        if (CloseMotion is { } motion)
            motion(byTile);
        else if (_popup is not null)
            _popup.IsOpen = false;
    }

    /// <summary>A tap on a group's header in the zoomed-in view, its pinned one too, zooms out to the jump list.</summary>
    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!_zoomedIn || e.Source is not Visual hit || ZoomedInView is not { } view ||
            hit.GetSelfAndVisualAncestors().OfType<ListViewHeaderItem>().FirstOrDefault() is not { } header ||
            !view.IsVisualAncestorOf(header) || !CanChangeViews)
            return;
        e.Handled = true;
        ChangeView(false, TopItem(), header.Content, false);
    }

    /// <summary>A tap on a tile: a group with items zooms in to it; an empty group's tile does nothing.</summary>
    private void OnTileClick(object? sender, ItemClickEventArgs e)
    {
        if (_zoomedIn || !JumpList.HasItems(e.ClickedItem))
            return;
        ChangeView(true, e.ClickedItem, e.ClickedItem, true);
    }

    /// <summary>
    /// A view is the SemanticZoom's logical child, so it takes the page's data context and resources while the jump list's
    /// popup is closed, as WinRT's views do.
    /// </summary>
    private void Adopt(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is Control old)
            LogicalChildren.Remove(old);
        if (e.NewValue is Control view)
            LogicalChildren.Add(view);
    }

    /// <summary>A list as the zoomed-out view clicks its tiles, so a tap selects nothing; given back as it was when it goes.</summary>
    private void OnZoomedOutViewChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_jumpList is not null)
        {
            _jumpList.RemoveHandler(ListBoxOptions.ItemClickEvent, OnTileClick);
            _jumpList.ClearValue(ListBoxOptions.IsItemClickEnabledProperty);
        }

        _jumpList = e.NewValue as ListBox;
        if (_jumpList is not null)
        {
            ListBoxOptions.SetIsItemClickEnabled(_jumpList, true);
            _jumpList.AddHandler(ListBoxOptions.ItemClickEvent, OnTileClick);
        }
    }

    private static ListBox? ListOf(Control? view) => view as ListBox;

    /// <summary>The zoomed-in list's top item: the first realized item, not a header, that reaches below the viewport's top.</summary>
    private object? TopItem()
    {
        if (ListOf(ZoomedInView) is not { Scroll: ScrollViewer scroll } list)
            return null;
        object? top = null;
        var topIndex = int.MaxValue;
        foreach (var container in list.GetRealizedContainers())
        {
            if (container is ListViewHeaderItem || !container.IsVisible || list.IndexFromContainer(container) is not (>= 0 and var index) ||
                index >= topIndex || container.TranslatePoint(default, scroll) is not { } at || at.Y + container.Bounds.Height <= 0)
                continue;
            topIndex = index;
            top = list.ItemFromContainer(container);
        }

        return top;
    }

    /// <summary>The group of the zoomed-in list's that holds <paramref name="item"/>; none if it isn't grouped or holds none.</summary>
    private object? GroupOf(object? item)
    {
        if (item is null || ListOf(ZoomedInView) is not { } list || ListBoxOptions.GetGroupedItemsSource(list) is not { } groups)
            return null;
        foreach (var group in groups)
        {
            if (group is IEnumerable items and not string && items.Cast<object?>().Contains(item))
                return group;
        }

        return null;
    }
}
