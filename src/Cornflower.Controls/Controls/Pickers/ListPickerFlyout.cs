using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Cornflower.Controls;

/// <summary>How many items a <see cref="ListPickerFlyout"/> picks: WinRT's <c>ListPickerFlyoutSelectionMode</c>.</summary>
public enum ListPickerFlyoutSelectionMode
{
    /// <summary>One: a tap on an item picks it and closes the picker. The default.</summary>
    Single = 0,

    /// <summary>Any number: a tap checks or unchecks an item, and accept picks those checked.</summary>
    Multiple = 1,
}

/// <summary>The data of <see cref="ListPickerFlyout.ItemsPicked"/>: WinRT's <c>ItemsPickedEventArgs</c>.</summary>
public sealed class ItemsPickedEventArgs : EventArgs
{
    public ItemsPickedEventArgs(IReadOnlyList<object> addedItems, IReadOnlyList<object> removedItems)
    {
        AddedItems = addedItems;
        RemovedItems = removedItems;
    }

    /// <summary>The items picked that weren't selected before.</summary>
    public IReadOnlyList<object> AddedItems { get; }

    /// <summary>The items selected before that weren't picked.</summary>
    public IReadOnlyList<object> RemovedItems { get; }
}

/// <summary>
/// WinRT's <c>ListPickerFlyout</c>: a full-screen list to pick from. In <see cref="ListPickerFlyoutSelectionMode.Single"/>
/// a tap on an item picks it and closes the picker; in <see cref="ListPickerFlyoutSelectionMode.Multiple"/> each item has
/// a check box, and accept picks those checked. A pick sets <see cref="SelectedItems"/>, <see cref="SelectedItem"/> and
/// <see cref="SelectedIndex"/>, raises <see cref="ItemsPicked"/>, and is <see cref="ShowAtAsync"/>'s result. Back,
/// cancel or <c>Hide()</c> close it with no pick: the result is null.
/// </summary>
/// <remarks>
/// <para>Behaviour only; its presenter is a <see cref="ListPickerFlyoutPresenter"/>, which a theme draws. Under the
/// Windows Phone 8.1 theme it fills the window; in Multiple, accept and cancel are in the pickers' bar at its foot.</para>
/// <para>WinRT's <c>DisplayMemberPath</c> is Avalonia's <see cref="DisplayMemberBinding"/>, as on an
/// <see cref="ItemsControl"/>. WinRT's <c>SelectedValue</c> and <c>SelectedValuePath</c> aren't carried: read
/// <see cref="SelectedItem"/>.</para>
/// </remarks>
/// <example>
/// <code>var picked = await new ListPickerFlyout { ItemsSource = items }.ShowAtAsync(button);</code>
/// </example>
public class ListPickerFlyout : PickerFlyoutBase
{
    /// <summary>Defines <see cref="ItemsSource"/>.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ListPickerFlyout, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Defines <see cref="ItemTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ListPickerFlyout, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>Defines <see cref="DisplayMemberBinding"/>.</summary>
    public static readonly StyledProperty<BindingBase?> DisplayMemberBindingProperty =
        AvaloniaProperty.Register<ListPickerFlyout, BindingBase?>(nameof(DisplayMemberBinding));

    /// <summary>Defines <see cref="SelectionMode"/>.</summary>
    public static readonly StyledProperty<ListPickerFlyoutSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<ListPickerFlyout, ListPickerFlyoutSelectionMode>(nameof(SelectionMode));

    /// <summary>Defines <see cref="SelectedIndex"/>.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ListPickerFlyout, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="SelectedItem"/>.</summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ListPickerFlyout, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    private readonly AvaloniaList<object> _selectedItems = [];
    private TaskCompletionSource<IReadOnlyList<object>?>? _result;
    private bool _syncing;

    public ListPickerFlyout()
    {
        _selectedItems.CollectionChanged += (_, _) =>
        {
            if (_syncing)
                return;
            var first = _selectedItems.Count > 0 ? _selectedItems[0] : null;
            Sync(() =>
            {
                SelectedItem = first;
                SelectedIndex = IndexOf(first);
            });
        };
    }

    /// <summary>The items to pick from.</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>How each item is shown; as text, by default.</summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>What of each item is shown, when there's no <see cref="ItemTemplate"/>: WinRT's <c>DisplayMemberPath</c>.</summary>
    [AssignBinding]
    public BindingBase? DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    /// <summary>One item, picked by a tap, or any number, checked and accepted. Single by default.</summary>
    public ListPickerFlyoutSelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>The index of the item picked (in Multiple, the first); -1 for none.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>The item picked (in Multiple, the first); null for none.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// The items picked: in Multiple, those checked as the picker opens. Setting <see cref="SelectedIndex"/> or
    /// <see cref="SelectedItem"/> makes it that item alone.
    /// </summary>
    public IList<object> SelectedItems => _selectedItems;

    /// <summary>Raised by a pick, before the picker closes, in the same frame as its result.</summary>
    public event EventHandler<ItemsPickedEventArgs>? ItemsPicked;

    /// <summary>
    /// Shows the picker at <paramref name="target"/> and waits for its pick: the items picked, or null if it was closed
    /// with none (in <c>Closed</c>'s frame) or couldn't open.
    /// </summary>
    public Task<IReadOnlyList<object>?> ShowAtAsync(Control target)
    {
        _result?.TrySetResult(null);
        _result = null;
        // Shown first: shown at another target, it closes there, and that close ends no wait of this call's.
        ShowAt(target);
        if (!IsOpen)
            return Task.FromResult<IReadOnlyList<object>?>(null);
        _result = new TaskCompletionSource<IReadOnlyList<object>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _result.Task;
    }

    /// <inheritdoc />
    protected override Control CreatePresenter()
    {
        var presenter = Hook(new ListPickerFlyoutPresenter());
        presenter.ItemClicked += (_, item) =>
        {
            if (SelectionMode == ListPickerFlyoutSelectionMode.Single && TakeAnswer())
                Pick([item]);
        };
        return presenter;
    }

    /// <inheritdoc />
    protected override bool ShouldShowConfirmationButtons() => SelectionMode == ListPickerFlyoutSelectionMode.Multiple;

    /// <inheritdoc />
    protected override void PreparePresenter(PickerPresenterBase presenter)
    {
        base.PreparePresenter(presenter);
        if (presenter is ListPickerFlyoutPresenter list)
            list.Show(ItemsSource, ItemTemplate, DisplayMemberBinding, SelectionMode == ListPickerFlyoutSelectionMode.Multiple, _selectedItems);
    }

    /// <summary>Accept, in Multiple: the items checked are picked.</summary>
    protected override void OnConfirmed()
    {
        if (SelectionMode == ListPickerFlyoutSelectionMode.Multiple && CurrentPresenter is ListPickerFlyoutPresenter presenter)
            Pick(presenter.CheckedItems());
        else
            base.OnConfirmed();
    }

    /// <inheritdoc />
    protected override void OnClosed()
    {
        _result?.TrySetResult(null);
        _result = null;
        base.OnClosed();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Open, the list follows its items, template and mode, as it would open anew; reading Popup while closed would make it.
        if ((change.Property == ItemsSourceProperty || change.Property == ItemTemplateProperty
                || change.Property == DisplayMemberBindingProperty || change.Property == SelectionModeProperty)
            && IsOpen && Popup.Child is ListPickerFlyoutPresenter open)
            PreparePresenter(open);
        if (_syncing)
            return;
        if (change.Property == SelectedIndexProperty)
        {
            var index = change.GetNewValue<int>();
            var item = ItemAt(index);
            Sync(() =>
            {
                // An index past the items is none; with no items yet, it waits for them.
                if (item is null && ItemsSource is not null)
                    SelectedIndex = -1;
                SelectedItem = item;
                Only(item);
            });
        }
        else if (change.Property == ItemsSourceProperty)
        {
            // New items: the items picked that are in them stay; or an index set before them takes its item.
            Sync(() =>
            {
                if (_selectedItems.Count > 0)
                {
                    if (ItemsSource is null)
                        return;
                    var kept = _selectedItems.Where(i => IndexOf(i) >= 0).ToList();
                    _selectedItems.Clear();
                    _selectedItems.AddRange(kept);
                    var first = kept.Count > 0 ? kept[0] : null;
                    SelectedItem = first;
                    SelectedIndex = IndexOf(first);
                }
                else if (SelectedIndex >= 0)
                {
                    var item = ItemAt(SelectedIndex);
                    if (item is null)
                        SelectedIndex = -1;
                    SelectedItem = item;
                    Only(item);
                }
            });
        }
        else if (change.Property == SelectedItemProperty)
        {
            var item = change.NewValue;
            Sync(() =>
            {
                SelectedIndex = IndexOf(item);
                // An item not in the items is none; with no items yet, it waits for them.
                if (SelectedIndex < 0 && ItemsSource is not null)
                    item = SelectedItem = null;
                Only(item);
            });
        }
    }

    /// <summary>
    /// A pick: the selection becomes <paramref name="picked"/>, <see cref="ItemsPicked"/> is raised with what was added
    /// and removed, the result is set, and the picker closes.
    /// </summary>
    private void Pick(IReadOnlyList<object> picked)
    {
        var before = _selectedItems.ToList();
        var added = picked.Where(i => !before.Contains(i)).ToList();
        var removed = before.Where(i => !picked.Contains(i)).ToList();
        var first = picked.Count > 0 ? picked[0] : null;
        Sync(() =>
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(picked);
            SelectedItem = first;
            SelectedIndex = IndexOf(first);
        });
        ItemsPicked?.Invoke(this, new ItemsPickedEventArgs(added, removed));
        _result?.TrySetResult(picked);
        Hide();
    }

    private void Only(object? item)
    {
        _selectedItems.Clear();
        if (item is not null)
            _selectedItems.Add(item);
    }

    private void Sync(Action action)
    {
        _syncing = true;
        try
        {
            action();
        }
        finally
        {
            _syncing = false;
        }
    }

    private object? ItemAt(int index)
    {
        if (index < 0 || ItemsSource is null)
            return null;
        if (ItemsSource is IList list)
            return index < list.Count ? list[index] : null;
        return ItemsSource.Cast<object?>().ElementAtOrDefault(index);
    }

    private int IndexOf(object? item)
    {
        if (item is null || ItemsSource is null)
            return -1;
        if (ItemsSource is IList list)
            return list.IndexOf(item);
        var index = 0;
        foreach (var each in ItemsSource)
        {
            if (Equals(each, item))
                return index;
            index++;
        }

        return -1;
    }
}
