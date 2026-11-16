using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Cornflower.Controls;

/// <summary>
/// What a picker (<see cref="PickerFlyoutBase"/>) tells its presenter as it opens, for a theme to show: its title and
/// whether it shows its accept and cancel buttons. Data only, on any <see cref="PickerPresenterBase"/>; the picker
/// sets them, and a theme's template reads them.
/// </summary>
/// <example>
/// <code>&lt;TextBlock Text="{TemplateBinding (cf:PickerPresenterOptions.Title)}" /&gt;</code>
/// </example>
public static class PickerPresenterOptions
{
    /// <summary>The picker's title (<see cref="PickerFlyoutBase.TitleProperty"/>); none if it has none.</summary>
    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<PickerPresenterBase, string?>("Title", typeof(PickerPresenterOptions));

    /// <summary>
    /// Whether the presenter shows its accept and cancel buttons (WinRT's <c>ShouldShowConfirmationButtons</c>): false
    /// by default.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowsConfirmationButtonsProperty =
        AvaloniaProperty.RegisterAttached<PickerPresenterBase, bool>("ShowsConfirmationButtons", typeof(PickerPresenterOptions));

    public static string? GetTitle(PickerPresenterBase presenter) => presenter.GetValue(TitleProperty);

    public static void SetTitle(PickerPresenterBase presenter, string? value) => presenter.SetValue(TitleProperty, value);

    public static bool GetShowsConfirmationButtons(PickerPresenterBase presenter) =>
        presenter.GetValue(ShowsConfirmationButtonsProperty);

    public static void SetShowsConfirmationButtons(PickerPresenterBase presenter, bool value) =>
        presenter.SetValue(ShowsConfirmationButtonsProperty, value);
}

/// <summary>
/// A presenter's accept and cancel buttons, the template's <c>PART_AcceptButton</c> and <c>PART_DismissButton</c>
/// (any <see cref="Button"/>, as Avalonia's date and time presenters name theirs): a click on one confirms or dismisses.
/// </summary>
internal sealed class PickerButtons
{
    private readonly Action _accept;
    private readonly Action _dismiss;
    private Button? _acceptButton;
    private Button? _dismissButton;

    public PickerButtons(Action accept, Action dismiss)
    {
        _accept = accept;
        _dismiss = dismiss;
    }

    /// <summary>The template's buttons, in place of the last template's.</summary>
    public void Find(INameScope scope)
    {
        if (_acceptButton is not null)
            _acceptButton.Click -= OnAccept;
        if (_dismissButton is not null)
            _dismissButton.Click -= OnDismiss;
        _acceptButton = scope.Find<Button>("PART_AcceptButton");
        _dismissButton = scope.Find<Button>("PART_DismissButton");
        if (_acceptButton is not null)
            _acceptButton.Click += OnAccept;
        if (_dismissButton is not null)
            _dismissButton.Click += OnDismiss;
    }

    private void OnAccept(object? sender, RoutedEventArgs e) => _accept();

    private void OnDismiss(object? sender, RoutedEventArgs e) => _dismiss();
}
