using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cornflower.Controls.Primitives;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>Pivot</c>: a set of pages under a <see cref="Title"/>, one shown at a time, with a strip of their headers
/// above that moves with the content and wraps from the last page to the first. A swipe goes to the next or previous
/// page; a tap on a header goes to its page.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour only: its items, their containers (<see cref="PivotItem"/>), the header strip's headers
/// (<see cref="PivotHeaderItem"/>, in the template's <see cref="PivotHeaderPanel"/>), the selection, the swipe and the
/// motion. A theme's template draws it, with these parts: <c>PART_ScrollViewer</c>, the area a swipe starts in
/// (optional: the whole Pivot without it); <c>PART_Header</c>, a <see cref="PivotHeaderPanel"/>; and
/// <c>PART_ItemsPresenter</c>, the items. The motion, the phone's, read from the 8.1 emulator, is the control's own,
/// frame by frame on <see cref="CornflowerClock"/> (<see cref="PivotMotion"/>).
/// </para>
/// <para>
/// The states are WinRT's <c>Orientation</c> group: <c>:portrait</c> or <c>:landscape</c>, by the shape of the window
/// the Pivot is in (landscape when it is wider than tall).
/// </para>
/// <para>
/// An item is always selected while there are any, as on the phone: <see cref="SelectingItemsControl.SelectedIndex"/>
/// is 0 once the first is added. A swipe changes the selection part way through, as the phone's does; a tap on a
/// header, or a change from code, changes it at once and then moves.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;cf:Pivot Title="MY APP"&gt;
///   &lt;cf:PivotItem Header="one"&gt;…&lt;/cf:PivotItem&gt;
///   &lt;cf:PivotItem Header="two"&gt;…&lt;/cf:PivotItem&gt;
/// &lt;/cf:Pivot&gt;
/// </code>
/// </example>
[TemplatePart("PART_Header", typeof(PivotHeaderPanel))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
[TemplatePart("PART_ScrollViewer", typeof(Control))]
[PseudoClasses(PortraitPseudoClass, LandscapePseudoClass)]
public class Pivot : SelectingItemsControl, ISidewaysSwipeTarget
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<Pivot, object?>(nameof(Title));

    /// <summary>Defines <see cref="TitleTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        AvaloniaProperty.Register<Pivot, IDataTemplate?>(nameof(TitleTemplate));

    /// <summary>Defines <see cref="HeaderTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<Pivot, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines <see cref="IsLocked"/>.</summary>
    public static readonly StyledProperty<bool> IsLockedProperty =
        AvaloniaProperty.Register<Pivot, bool>(nameof(IsLocked));

    private const string PortraitPseudoClass = ":portrait";
    private const string LandscapePseudoClass = ":landscape";

    private readonly PivotMotion _motion;
    private readonly TranslateTransform _itemsTranslation = new();
    private PivotHeaderPanel? _header;
    private ItemsPresenter? _itemsPresenter;
    private Control? _swipeArea;
    private TopLevel? _topLevel;
    private bool _selectingFromMotion;
    private int _knownCount;
    private object? _selected;
    private PivotItem? _shown;
    private readonly HashSet<PivotItem> _headed = [];

    static Pivot()
    {
        SelectionModeProperty.OverrideDefaultValue<Pivot>(SelectionMode.Single | SelectionMode.AlwaysSelected);
    }

    /// <summary>Initialises an empty Pivot.</summary>
    public Pivot()
    {
        _motion = new PivotMotion(this);
        GestureRecognizers.Add(SidewaysSwipeGestureRecognizer.For(this));
        AddHandler(TappedEvent, OnTapped);
        ItemsView.CollectionChanged += OnItemsChanged;
        UpdateOrientation();
    }

    /// <summary>
    /// Raised for the item selected as it comes in, after <see cref="PivotItemUnloaded"/> for the one it replaces and
    /// before <see cref="PivotItemLoaded"/>; and for the first item shown in a window.
    /// </summary>
    /// <remarks>
    /// The four item events come together, in the order <see cref="PivotItemUnloading"/>,
    /// <see cref="PivotItemUnloaded"/>, <see cref="PivotItemLoading"/>, <see cref="PivotItemLoaded"/>, after
    /// <c>SelectionChanged</c>, with <see cref="SelectingItemsControl.SelectedIndex"/> already the new one, as the
    /// phone's do (#148): for a swipe at the selection change, before the new item slides in; for a header tap or a
    /// change from code when the new item comes in.
    /// </remarks>
    public event EventHandler<PivotItemEventArgs>? PivotItemLoading;

    /// <summary>Raised for the item selected as it comes in, after <see cref="PivotItemLoading"/>; and for the first item shown.</summary>
    /// <remarks><inheritdoc cref="PivotItemLoading" path="/remarks"/></remarks>
    public event EventHandler<PivotItemEventArgs>? PivotItemLoaded;

    /// <summary>Raised for the item that was shown as the one selected comes in, first of the four item events.</summary>
    /// <remarks><inheritdoc cref="PivotItemLoading" path="/remarks"/></remarks>
    public event EventHandler<PivotItemEventArgs>? PivotItemUnloading;

    /// <summary>Raised for the item that was shown as the one selected comes in, after <see cref="PivotItemUnloading"/>.</summary>
    /// <remarks><inheritdoc cref="PivotItemLoading" path="/remarks"/></remarks>
    public event EventHandler<PivotItemEventArgs>? PivotItemUnloaded;

    /// <summary>What the template shows above the header strip; on the phone, the app's name in capitals.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The template for <see cref="Title"/>.</summary>
    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    /// <summary>The template for each item's header in the header strip.</summary>
    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// Whether the Pivot stays on its selected item: no swipe or header tap moves it, and the other headers go, as the
    /// phone's do while locked. A change from code still selects. False by default.
    /// </summary>
    public bool IsLocked
    {
        get => GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    /// <summary>A page: how far a swipe moves the content, the width of the items' view.</summary>
    internal double PageWidth => _itemsPresenter is { Bounds.Width: > 0 } presenter ? presenter.Bounds.Width : Bounds.Width;

    /// <summary>Whether a finger can swipe the Pivot now.</summary>
    internal bool CanSwipe => IsEffectivelyEnabled && !IsLocked && ItemCount > 1 && PageWidth > 0;

    internal PivotMotion Motion => _motion;

    internal PivotHeaderPanel? HeaderPanel => _header;

    internal ItemsPresenter? ItemsPresenterPart => _itemsPresenter;

    /// <summary>Whether <paramref name="point"/>, in the Pivot's coordinates, is in the part a swipe starts in.</summary>
    internal bool IsInSwipeArea(Point point)
    {
        if (_swipeArea is null) return new Rect(Bounds.Size).Contains(point);
        return this.TranslatePoint(point, _swipeArea) is { } local && new Rect(_swipeArea.Bounds.Size).Contains(local);
    }

    bool ISidewaysSwipeTarget.CanSwipe => CanSwipe;

    bool ISidewaysSwipeTarget.IsInSwipeArea(Point point) => IsInSwipeArea(point);

    // A press while a swipe moves it holds it where it is (#148); one during a tap's or a code change's move ends that
    // move when the drag begins (PivotMotion.BeginDrag).
    bool ISidewaysSwipeTarget.HoldsAPress => _motion.CanHold;

    void ISidewaysSwipeTarget.BeginDrag() => _motion.BeginDrag();

    void ISidewaysSwipeTarget.Drag(double travel) => _motion.Drag(travel);

    void ISidewaysSwipeTarget.Release(double velocity) => _motion.Release(velocity);

    /// <summary>The motion's selection change: the selection moves, and the motion goes on from it.</summary>
    internal void SelectFromMotion(int index)
    {
        _selectingFromMotion = true;
        try
        {
            SelectedIndex = index;
        }
        finally
        {
            _selectingFromMotion = false;
        }
    }

    /// <summary>
    /// Draws the motion's state: the strip laid out and slid, and the shown item and where it is. When the item shown
    /// changes, the four item events come together: <see cref="PivotItemUnloading"/> and <see cref="PivotItemUnloaded"/>
    /// for the one that went, then <see cref="PivotItemLoading"/> and <see cref="PivotItemLoaded"/> for the one that came
    /// in; the first shown in a window raises <see cref="PivotItemLoading"/> and <see cref="PivotItemLoaded"/>.
    /// </summary>
    internal void ApplyMotion()
    {
        var lead = _motion.Lead;
        if (_header is { } header)
        {
            header.Lead = lead;
            var page = PageWidth;
            header.Translation = page > 0 && header.Children.Count > 0
                ? -PivotMotion.StripTravel(header.Pitch, lead, _motion.Offset / page)
                : 0;
        }

        _itemsTranslation.X = _motion.ItemX;
        var shown = lead >= 0 && lead < ItemCount ? ContainerFor(lead) : null;
        if (ReferenceEquals(shown, _shown) && shown?.IsVisible != false)
            return;

        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFor(i) is { } container)
                container.IsVisible = ReferenceEquals(container, shown);
        }

        if (shown is null || !this.IsAttachedToVisualTree() || ReferenceEquals(shown, _shown))
            return;

        var gone = _shown;
        _shown = shown;
        // Together, in the phone's order; a handler that shows another item meanwhile ends the four, as they are stale.
        RaiseFor(PivotItemUnloading, gone);
        if (!ReferenceEquals(_shown, shown)) return;
        RaiseFor(PivotItemUnloaded, gone);
        if (!ReferenceEquals(_shown, shown)) return;
        RaiseFor(PivotItemLoading, shown);
        if (!ReferenceEquals(_shown, shown)) return;
        RaiseFor(PivotItemLoaded, shown);
    }

    /// <summary>A <see cref="PivotItem"/>'s header has changed: its header in the strip shows the new one.</summary>
    internal void OnItemHeaderChanged(PivotItem item)
    {
        var index = IndexFromContainer(item);
        if (index < 0 && Items.Contains(item)) index = Items.IndexOf(item);
        if (_header is { } header && index >= 0 && index < header.Children.Count)
            ((PivotHeaderItem)header.Children[index]).Content = HeaderFor(index);
    }

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        => NeedsContainer<PivotItem>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        => new PivotItem();

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is PivotItem pivotItem && !ReferenceEquals(container, item))
        {
            // A data item is its own header; a control can't be in two places, so it gets none.
            pivotItem.SetCurrentValue(PivotItem.HeaderProperty, item is Control ? null : item);
            _headed.Add(pivotItem);
        }
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is PivotItem pivotItem && _headed.Remove(pivotItem))
            pivotItem.ClearValue(PivotItem.HeaderProperty);
    }

    /// <inheritdoc />
    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        container.IsVisible = index == _motion.Lead;
        if (index == _motion.Lead)
            ApplyMotion();
    }

    // A change to the items ends any move where it is: the indices it was moving between may no longer hold.
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _knownCount = ItemCount;
        BuildHeaders();
        // Rest on the selected item where it now is: the selection may not have caught up with the change yet.
        var at = SelectedIndex >= 0 && SelectedIndex < ItemCount && ReferenceEquals(ItemsView[SelectedIndex], _selected)
            ? SelectedIndex
            : _selected is null ? -1 : ItemsView.IndexOf(_selected);
        _motion.Rest(at >= 0 ? at : Math.Max(SelectedIndex, 0));
        UpdateHeaderStates();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_header is { } oldHeader)
            ClearHeaders(oldHeader);
        _header = e.NameScope.Find<PivotHeaderPanel>("PART_Header");
        if (_itemsPresenter is { } old && ReferenceEquals(old.RenderTransform, _itemsTranslation))
            old.RenderTransform = null;
        _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        if (_itemsPresenter is { } presenter)
            presenter.RenderTransform = _itemsTranslation;
        _swipeArea = e.NameScope.Find<Control>("PART_ScrollViewer");
        BuildHeaders();
        _motion.Rest(Math.Max(SelectedIndex, 0));
        UpdateHeaderStates();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
        UpdateOrientation();
        ApplyMotion();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is not null)
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        _topLevel = null;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Finish();
        _motion.StopFrames();
        _motion.Rest(Math.Max(SelectedIndex, 0));
        _shown = null;                                   // shown again in a window, the item loads again
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
        {
            OnSelectedIndexChanged(change.GetOldValue<int>(), change.GetNewValue<int>());
        }
        else if (change.Property == IsLockedProperty)
        {
            if (IsLocked && _motion.IsDragging)
                _motion.Rest(Math.Max(SelectedIndex, 0));
            UpdateHeaderStates();
        }
        else if (change.Property == HeaderTemplateProperty && _header is { } header)
        {
            foreach (var child in header.Children)
                ((PivotHeaderItem)child).ContentTemplate = HeaderTemplate;
        }
        else if (change.Property == BoundsProperty)
        {
            if (!_motion.IsDragging && !_motion.IsMoving)
                _motion.Rest(_motion.Lead);
        }
    }

    private void OnSelectedIndexChanged(int from, int to)
    {
        UpdateHeaderStates();
        // The motion's own change is this one only: a handler of it that selects again is a change from code.
        var fromMotion = _selectingFromMotion;
        _selectingFromMotion = false;
        var item = to >= 0 && to < ItemCount ? ItemsView[to] : null;
        var previous = _selected;
        // The same item at a new index, the items having changed round it (not an equal item elsewhere: the old index
        // no longer holds it).
        var same = item is not null && ReferenceEquals(item, previous) &&
                   !(from >= 0 && from < ItemCount && ReferenceEquals(ItemsView[from], previous));
        _selected = item;
        if (same)
        {
            // The items changed round the selected one, which moved: nothing is selected anew.
            if (!_motion.IsMoving && !_motion.IsDragging)
                _motion.Rest(Math.Max(to, 0));
            return;
        }

        // The item events come when the new item comes in (ApplyMotion), not here.
        if (fromMotion)
            return;

        var count = ItemCount;
        // A selection that moves because the items changed (the count isn't yet the one this saw last) goes there at once.
        var moving = count == _knownCount && from >= 0 && to >= 0 && count > 1 && _header is not null &&
                     PageWidth > 0 && this.IsAttachedToVisualTree() && IsEffectivelyVisible;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Rest(Math.Max(from, 0));

        if (!moving)
        {
            _motion.Rest(Math.Max(to, 0));
            return;
        }

        // Back one, if there are more than two; forward otherwise, as far as it takes (#128: a code change from the
        // second item to the first went back; from the last to the first, forward).
        var steps = count > 2 && to == PivotHeaderPanel.Wrap(from - 1, count) ? -1 : PivotHeaderPanel.Wrap(to - from, count);
        _motion.MoveTo(from, steps);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        // A swipe that starts on a header raises no Tapped: the swipe's recogniser has captured the pointer.
        if (IsLocked || _header is null || _motion.IsDragging)
            return;
        if ((e.Source as Visual)?.FindAncestorOfType<PivotHeaderItem>(includeSelf: true) is not { } tapped ||
            !ReferenceEquals(tapped.GetVisualParent(), _header))
            return;

        if (_motion.IsMoving)
            _motion.Finish();

        // A tap on the selected header raises Tapped and changes nothing, as on the phone (#136).
        if (tapped.Index == SelectedIndex)
            return;

        e.Handled = true;

        var from = SelectedIndex;
        var steps = PivotHeaderPanel.Wrap(tapped.Index - from, ItemCount);
        SelectFromMotion(tapped.Index);
        if (SelectedIndex == tapped.Index && !_motion.IsMoving)      // unless a handler of the selection moved it on
            _motion.MoveTo(from, steps);
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
            UpdateOrientation();
    }

    private void UpdateOrientation()
    {
        var size = _topLevel?.Bounds.Size ?? default;
        var landscape = size.Width > size.Height;
        PseudoClasses.Set(PortraitPseudoClass, !landscape);
        PseudoClasses.Set(LandscapePseudoClass, landscape);
    }

    private void BuildHeaders()
    {
        if (_header is not { } header) return;
        ClearHeaders(header);
        for (var i = 0; i < ItemCount; i++)
            header.Children.Add(new PivotHeaderItem { Index = i, Content = HeaderFor(i), ContentTemplate = HeaderTemplate });
    }

    /// <summary>An item's header: a <see cref="PivotItem"/>'s own, or a data item itself; a control has none, as it is its page.</summary>
    private object? HeaderFor(int index) => ItemsView[index] switch
    {
        PivotItem item => item.Header,
        Control => null,
        var data => data,
    };

    /// <summary>Empties a strip, its headers letting go of their content first, so a header that is a control can go to the next.</summary>
    private static void ClearHeaders(PivotHeaderPanel header)
    {
        foreach (var child in header.Children)
            ((PivotHeaderItem)child).Content = null;
        header.Children.Clear();
    }

    private void UpdateHeaderStates()
    {
        if (_header is not { } header) return;
        foreach (var child in header.Children)
        {
            var item = (PivotHeaderItem)child;
            item.SetState(item.Index == SelectedIndex, IsLocked);
        }
    }

    /// <summary>
    /// Item <paramref name="index"/>'s container: the item itself when it is a <see cref="PivotItem"/>, which holds while
    /// the items change and the containers' indices haven't caught up.
    /// </summary>
    private PivotItem? ContainerFor(int index) => ItemsView[index] as PivotItem ?? ContainerFromIndex(index) as PivotItem;

    private void RaiseFor(EventHandler<PivotItemEventArgs>? handler, PivotItem? item)
    {
        if (handler is not null && item is not null)
            handler(this, new PivotItemEventArgs(item));
    }
}
