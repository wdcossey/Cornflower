using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>PickerFlyoutBase</c>: the base of the full-screen pickers (<see cref="ListPickerFlyout"/>,
/// <see cref="PickerFlyout"/>), on Avalonia's <see cref="PopupFlyoutBase"/>. It holds the picker's
/// <see cref="TitleProperty">title</see>, whether it shows its accept and cancel buttons
/// (<see cref="ShouldShowConfirmationButtons"/>), and what accept does (<see cref="OnConfirmed"/>).
/// </summary>
/// <remarks>
/// <para>Behaviour only. A picker's presenter is a <see cref="PickerPresenterBase"/>: its accept raises
/// <see cref="PickerPresenterBase.Confirmed"/>, which the picker passes to <see cref="OnConfirmed"/>, and its cancel
/// <see cref="PickerPresenterBase.Dismissed"/>, which hides the picker with no result. A theme draws the presenter, its
/// title and its buttons: under the Windows Phone 8.1 theme it fills the window and turns in and out in bands, and
/// Back closes it.</para>
/// <para>A picker closed any way but by its accept or a pick (Back, cancel, <c>Hide()</c>) gives no result: its
/// <c>ShowAtAsync</c> returns its empty result when it has closed, in <c>Closed</c>'s frame.</para>
/// </remarks>
public abstract class PickerFlyoutBase : PopupFlyoutBase
{
    private bool _answered;

    /// <summary>
    /// The title a picker shows above its content: WinRT's <c>PickerFlyoutBase.Title</c>, attached, as WinRT's, and read
    /// from the picker. None by default, and then no title is shown.
    /// </summary>
    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<PickerFlyoutBase, AvaloniaObject, string?>("Title");

    /// <summary>The picker's title, <see cref="TitleProperty"/>.</summary>
    public static string? GetTitle(AvaloniaObject element) => element.GetValue(TitleProperty);

    /// <summary>Sets the picker's title, <see cref="TitleProperty"/>.</summary>
    public static void SetTitle(AvaloniaObject element, string? value) => element.SetValue(TitleProperty, value);

    /// <summary>The presenter of the picker's popup, once it has been made.</summary>
    protected PickerPresenterBase? CurrentPresenter => Popup.Child as PickerPresenterBase;

    /// <summary>
    /// What accept does: the picker's result and its event (<see cref="ListPickerFlyout.ItemsPicked"/>,
    /// <see cref="PickerFlyout.Confirmed"/>), then it closes. WinRT's <c>OnConfirmed</c>.
    /// </summary>
    protected virtual void OnConfirmed() => Hide();

    /// <summary>Whether the picker shows its accept and cancel buttons. WinRT's <c>ShouldShowConfirmationButtons</c>.</summary>
    protected virtual bool ShouldShowConfirmationButtons() => false;

    /// <summary>The picker's presenter, set up for this opening: <see cref="PreparePresenter"/>.</summary>
    protected override void OnOpening(CancelEventArgs args)
    {
        base.OnOpening(args);
        if (!args.Cancel && Popup.Child is PickerPresenterBase presenter)
            PreparePresenter(presenter);
    }

    /// <inheritdoc />
    protected override void OnOpened()
    {
        _answered = false;
        base.OnOpened();
    }

    /// <summary>Asked to close: from now on the picker takes no accept or pick, while its closing motion runs.</summary>
    protected override void OnClosing(CancelEventArgs args)
    {
        base.OnClosing(args);
        _answered = true;
    }

    /// <summary>
    /// Whether the picker may take an answer (an accept or a pick) now: it is open and has taken none, nor been asked to
    /// close. True once an opening, so a second accept by the keyboard while it closes is nothing.
    /// </summary>
    protected bool TakeAnswer()
    {
        if (!IsOpen || _answered)
            return false;
        _answered = true;
        return true;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Open, the title follows; reading Popup while closed would make it.
        if (change.Property == TitleProperty && IsOpen && Popup.Child is PickerPresenterBase presenter)
            PickerPresenterOptions.SetTitle(presenter, GetTitle(this));
    }

    /// <summary>
    /// Sets the presenter up for an opening: its title and whether it shows its buttons. A picker adds its own content.
    /// </summary>
    protected virtual void PreparePresenter(PickerPresenterBase presenter)
    {
        PickerPresenterOptions.SetTitle(presenter, GetTitle(this));
        PickerPresenterOptions.SetShowsConfirmationButtons(presenter, ShouldShowConfirmationButtons());
    }

    /// <summary>Hooks a presenter just made to the picker: its accept and cancel.</summary>
    protected T Hook<T>(T presenter) where T : PickerPresenterBase
    {
        presenter.Confirmed += (_, _) =>
        {
            if (TakeAnswer())
                OnConfirmed();
        };
        presenter.Dismissed += (_, _) => Hide();
        return presenter;
    }
}
