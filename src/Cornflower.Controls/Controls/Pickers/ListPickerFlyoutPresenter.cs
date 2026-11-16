using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Cornflower.Controls;

/// <summary>
/// The presenter of a <see cref="ListPickerFlyout"/>, WinRT's <c>ListPickerFlyoutPresenter</c>: the picker's title, its
/// items in a <see cref="ListBox"/> (<c>PART_ItemsHost</c>), and its accept and cancel buttons
/// (<c>PART_AcceptButton</c>, <c>PART_DismissButton</c>) when it shows them. A theme draws it.
/// </summary>
/// <remarks>
/// <para>The phone's runtime puts a ListView in the template's <c>ItemsHostPanel</c>; here the template holds the
/// <see cref="ListBox"/>, which the picker fills as it opens: its items, their template, and in Multiple the check mode
/// (<see cref="SelectionMode.Multiple"/> with <see cref="SelectionMode.Toggle"/>) with the items picked so far checked.
/// In Single, a tap on an item clicks it (<see cref="ListBoxOptions.IsItemClickEnabledProperty"/>) and the picker picks
/// it.</para>
/// <para>An Avalonia <see cref="PickerPresenterBase"/>, as Avalonia's date and time presenters are: a click on
/// <c>PART_AcceptButton</c> raises <see cref="PickerPresenterBase.Confirmed"/>, on <c>PART_DismissButton</c>
/// <see cref="PickerPresenterBase.Dismissed"/>.</para>
/// </remarks>
[TemplatePart("PART_ItemsHost", typeof(ListBox))]
[TemplatePart("PART_AcceptButton", typeof(Button))]
[TemplatePart("PART_DismissButton", typeof(Button))]
public class ListPickerFlyoutPresenter : PickerPresenterBase
{
    private readonly PickerButtons _buttons;
    private ListBox? _items;
    private IEnumerable? _source;
    private IDataTemplate? _template;
    private BindingBase? _display;
    private bool _multiple;
    private IReadOnlyList<object> _checked = [];

    public ListPickerFlyoutPresenter()
    {
        _buttons = new PickerButtons(OnConfirmed, OnDismiss);
    }

    /// <summary>The list of items, the template's <c>PART_ItemsHost</c>, once the template is applied.</summary>
    public ListBox? ItemsHost => _items;

    /// <summary>A tap on an item in Single: the item.</summary>
    internal event EventHandler<object>? ItemClicked;

    /// <summary>The picker's items, as it opens.</summary>
    internal void Show(IEnumerable? source, IDataTemplate? template, BindingBase? display, bool multiple, IReadOnlyList<object> picked)
    {
        _source = source;
        _template = template;
        _display = display;
        _multiple = multiple;
        _checked = picked.ToList();
        Fill();
    }

    /// <summary>The items checked now, in the list's order.</summary>
    internal IReadOnlyList<object> CheckedItems()
    {
        if (_items is null)
            return _checked;
        var selected = _items.Selection.SelectedItems.OfType<object>().ToList();
        return _items.Items.OfType<object>().Where(selected.Contains).ToList();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _buttons.Find(e.NameScope);
        if (_items is not null)
            _items.RemoveHandler(ListBoxOptions.ItemClickEvent, OnItemClick);
        _items = e.NameScope.Find<ListBox>("PART_ItemsHost");
        if (_items is not null)
            _items.AddHandler(ListBoxOptions.ItemClickEvent, OnItemClick);
        Fill();
    }

    private void Fill()
    {
        if (_items is null)
            return;
        _items.SelectedItems?.Clear();
        _items.ItemsSource = _source;
        _items.ItemTemplate = _template;
        _items.DisplayMemberBinding = _display;
        _items.SelectionMode = _multiple ? SelectionMode.Multiple | SelectionMode.Toggle : SelectionMode.Single;
        ListBoxOptions.SetIsItemClickEnabled(_items, !_multiple);
        if (_multiple)
        {
            foreach (var index in _checked.Select(IndexOf).Where(i => i >= 0))
                _items.Selection.Select(index);
        }

        if (_items.ItemCount > 0)
            _items.ScrollIntoView(0);
    }

    private int IndexOf(object item)
    {
        var index = 0;
        foreach (var each in _items!.Items)
        {
            if (Equals(each, item))
                return index;
            index++;
        }

        return -1;
    }

    private void OnItemClick(object? sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is { } item)
            ItemClicked?.Invoke(this, item);
    }
}
