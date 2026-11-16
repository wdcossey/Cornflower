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
public class Pivot : SelectingItemsControl
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
    private int _loaded = -1;

    static Pivot()
    {
        SelectionModeProperty.OverrideDefaultValue<Pivot>(SelectionMode.Single | SelectionMode.AlwaysSelected);
    }

    /// <summary>Initialises an empty Pivot.</summary>
    public Pivot()
    {
        _motion = new PivotMotion(this);
        GestureRecognizers.Add(new PivotSwipeGestureRecognizer(this));
        AddHandler(TappedEvent, OnTapped);
        ItemsView.CollectionChanged += OnItemsChanged;
        UpdateOrientation();
    }

    /// <summary>Raised when an item is about to be shown: as it is selected.</summary>
    public event EventHandler<PivotItemEventArgs>? PivotItemLoading;

    /// <summary>Raised when an item has been shown: when it comes in.</summary>
    public event EventHandler<PivotItemEventArgs>? PivotItemLoaded;

    /// <summary>Raised when the shown item is about to go: as another is selected.</summary>
    public event EventHandler<PivotItemEventArgs>? PivotItemUnloading;

    /// <summary>Raised when an item has gone: when the next one comes in.</summary>
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

    internal void BeginSwipe() => _motion.BeginDrag();

    internal void Swipe(double travel) => _motion.Drag(travel);

    internal void EndSwipe(double velocity) => _motion.Release(velocity);

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

    /// <summary>The new item has come in: the item at <paramref name="from"/> has gone and the one at <paramref name="to"/> is shown.</summary>
    internal void OnItemCameIn(int from, int to)
    {
        ApplyMotion();
        if (_loaded < 0 || _loaded != from) return;     // nothing loaded yet: the first item loads when it is shown
        _loaded = to;
        Raise(PivotItemUnloaded, from);
        Raise(PivotItemLoaded, to);
    }

    /// <summary>Draws the motion's state: the strip laid out and slid, the shown item and where it is.</summary>
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
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is { } container)
                container.IsVisible = i == lead;
        }
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
            pivotItem.SetCurrentValue(PivotItem.HeaderProperty, item);
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is PivotItem pivotItem && !Items.Contains(container))
            pivotItem.ClearValue(PivotItem.HeaderProperty);
    }

    /// <inheritdoc />
    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        container.IsVisible = index == _motion.Lead;
        if (index == SelectedIndex && _loaded < 0 && container is PivotItem prepared && this.IsAttachedToVisualTree())
            LoadFirst(prepared);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _knownCount = ItemCount;
        BuildHeaders();
        _motion.Rest(Math.Max(SelectedIndex, 0));
        UpdateHeaderStates();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _header?.Children.Clear();
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
        if (_loaded < 0)
            LoadFirst();
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
        if (_selectingFromMotion)
        {
            Raise(PivotItemUnloading, from);
            Raise(PivotItemLoading, to);
            return;
        }

        var count = ItemCount;
        // A selection that moves because the items changed (the count isn't yet the one this saw last) goes there at once.
        var moving = ItemCount == _knownCount && from >= 0 && to >= 0 && count > 1 && _header is not null &&
                     PageWidth > 0 && this.IsAttachedToVisualTree() && IsEffectivelyVisible;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Rest(Math.Max(from, 0));

        Raise(PivotItemUnloading, from);
        Raise(PivotItemLoading, to);
        if (!moving)
        {
            _motion.Rest(Math.Max(to, 0));
            OnItemCameIn(from, to);
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

        // A tap on the selected header raises Tapped and changes nothing, as on the phone (#136).
        if (tapped.Index == SelectedIndex)
            return;

        e.Handled = true;
        if (_motion.IsMoving)
            _motion.Finish();

        var from = SelectedIndex;
        var steps = PivotHeaderPanel.Wrap(tapped.Index - from, ItemCount);
        SelectFromMotion(tapped.Index);
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
        header.Children.Clear();
        for (var i = 0; i < ItemCount; i++)
            header.Children.Add(new PivotHeaderItem { Index = i, Content = HeaderFor(i), ContentTemplate = HeaderTemplate });
    }

    private object? HeaderFor(int index) => ItemsView[index] is PivotItem item ? item.Header : ItemsView[index];

    private void UpdateHeaderStates()
    {
        if (_header is not { } header) return;
        foreach (var child in header.Children)
        {
            var item = (PivotHeaderItem)child;
            item.SetState(item.Index == SelectedIndex, IsLocked);
        }
    }

    /// <summary>The first item shown, once the Pivot is in a window and its container is made: it loads.</summary>
    private void LoadFirst(PivotItem? item = null)
    {
        item ??= SelectedIndex >= 0 ? ContainerFromIndex(SelectedIndex) as PivotItem : null;
        if (item is null || !this.IsAttachedToVisualTree())
            return;
        _loaded = SelectedIndex;
        PivotItemLoading?.Invoke(this, new PivotItemEventArgs(item));
        PivotItemLoaded?.Invoke(this, new PivotItemEventArgs(item));
    }

    private void Raise(EventHandler<PivotItemEventArgs>? handler, int index)
    {
        if (handler is null || index < 0 || ContainerFromIndex(index) is not PivotItem item) return;
        handler(this, new PivotItemEventArgs(item));
    }
}
