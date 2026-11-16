using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Cornflower.Controls;

/// <summary>
/// The presenter of a <see cref="PickerFlyout"/>, WinRT's <c>PickerFlyoutPresenter</c>: the picker's title, its
/// content, and its accept and cancel buttons (<c>PART_AcceptButton</c>, <c>PART_DismissButton</c>) when it shows
/// them. A theme draws it.
/// </summary>
/// <remarks>
/// <para>WinRT's is a <c>ContentControl</c>. This is an Avalonia <see cref="PickerPresenterBase"/>, as Avalonia's date
/// and time presenters are, with the content of a <see cref="ContentControl"/>: a click on <c>PART_AcceptButton</c>
/// raises <see cref="PickerPresenterBase.Confirmed"/>, on <c>PART_DismissButton</c>
/// <see cref="PickerPresenterBase.Dismissed"/>. The picker sets its title and whether it shows its buttons
/// (<see cref="PickerPresenterOptions"/>).</para>
/// </remarks>
[TemplatePart("PART_AcceptButton", typeof(Button))]
[TemplatePart("PART_DismissButton", typeof(Button))]
public class PickerFlyoutPresenter : PickerPresenterBase
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<PickerFlyoutPresenter>();

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<PickerFlyoutPresenter>();

    private readonly PickerButtons _buttons;

    public PickerFlyoutPresenter()
    {
        _buttons = new PickerButtons(OnConfirmed, OnDismiss);
    }

    /// <summary>What the picker shows: its <see cref="PickerFlyout.Content"/>.</summary>
    [Content]
    [DependsOn(nameof(ContentTemplate))]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The template the content is shown with.</summary>
    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _buttons.Find(e.NameScope);
    }
}
