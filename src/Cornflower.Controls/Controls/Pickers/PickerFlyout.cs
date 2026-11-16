using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Cornflower.Controls;

/// <summary>The data of <see cref="PickerFlyout.Confirmed"/>: WinRT's <c>PickerConfirmedEventArgs</c>, which has none.</summary>
public sealed class PickerConfirmedEventArgs : EventArgs
{
}

/// <summary>
/// WinRT's <c>PickerFlyout</c>: a full-screen picker of any <see cref="Content"/>, with accept and cancel buttons when
/// <see cref="ConfirmationButtonsVisible"/>. Accept raises <see cref="Confirmed"/> and closes it; its
/// <see cref="ShowAtAsync"/> returns true. Cancel, Back or <c>Hide()</c> close it with false.
/// </summary>
/// <remarks>
/// Behaviour only; its presenter is a <see cref="PickerFlyoutPresenter"/>, which a theme draws. Under the Windows Phone
/// 8.1 theme it fills the window, the buttons in the pickers' bar at its foot.
/// </remarks>
/// <example>
/// <code>&lt;cf:PickerFlyout ConfirmationButtonsVisible="True" Confirmed="OnConfirmed"&gt;&lt;TextBlock Text="..." /&gt;&lt;/cf:PickerFlyout&gt;</code>
/// </example>
public class PickerFlyout : PickerFlyoutBase
{
    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<PickerFlyout, object?>(nameof(Content));

    /// <summary>Defines <see cref="ConfirmationButtonsVisible"/>.</summary>
    public static readonly StyledProperty<bool> ConfirmationButtonsVisibleProperty =
        AvaloniaProperty.Register<PickerFlyout, bool>(nameof(ConfirmationButtonsVisible));

    private TaskCompletionSource<bool>? _result;

    /// <summary>What the picker shows, below its title.</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Whether the picker shows its accept and cancel buttons. False by default, as WinRT's.</summary>
    public bool ConfirmationButtonsVisible
    {
        get => GetValue(ConfirmationButtonsVisibleProperty);
        set => SetValue(ConfirmationButtonsVisibleProperty, value);
    }

    /// <summary>Raised by accept, before the picker closes, in the same frame as its result.</summary>
    public event EventHandler<PickerConfirmedEventArgs>? Confirmed;

    /// <summary>
    /// Shows the picker at <paramref name="target"/> and waits for it: true if it was accepted, false if it was closed
    /// any other way (in <c>Closed</c>'s frame), or couldn't open.
    /// </summary>
    public Task<bool> ShowAtAsync(Control target)
    {
        _result?.TrySetResult(false);
        _result = null;
        // Shown first: shown at another target, it closes there, and that close ends no wait of this call's.
        ShowAt(target);
        if (!IsOpen)
            return Task.FromResult(false);
        _result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _result.Task;
    }

    /// <inheritdoc />
    protected override Control CreatePresenter() => Hook(new PickerFlyoutPresenter());

    /// <inheritdoc />
    protected override bool ShouldShowConfirmationButtons() => ConfirmationButtonsVisible;

    /// <inheritdoc />
    protected override void PreparePresenter(PickerPresenterBase presenter)
    {
        base.PreparePresenter(presenter);
        if (presenter is PickerFlyoutPresenter picker)
            picker.Content = Content;
    }

    /// <summary>Accept: <see cref="Confirmed"/> and the result true, then the picker closes.</summary>
    protected override void OnConfirmed()
    {
        Confirmed?.Invoke(this, new PickerConfirmedEventArgs());
        _result?.TrySetResult(true);
        base.OnConfirmed();
    }

    /// <inheritdoc />
    protected override void OnClosed()
    {
        _result?.TrySetResult(false);
        _result = null;
        // The closed presenter lets go of the content, which may be put elsewhere; the next opening gives it back.
        if (Popup.Child is PickerFlyoutPresenter presenter)
            presenter.Content = null;
        base.OnClosed();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Only while open: reading Popup makes it, and an opening copies the content anyway.
        if (change.Property == ContentProperty && IsOpen && Popup.Child is PickerFlyoutPresenter presenter)
            presenter.Content = change.NewValue;
        else if (change.Property == ConfirmationButtonsVisibleProperty && IsOpen && Popup.Child is PickerFlyoutPresenter open)
            PickerPresenterOptions.SetShowsConfirmationButtons(open, change.GetNewValue<bool>());
    }
}
