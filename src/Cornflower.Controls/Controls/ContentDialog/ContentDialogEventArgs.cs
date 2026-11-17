using Avalonia.Threading;

namespace Cornflower.Controls;

/// <summary>WinRT's <c>ContentDialogResult</c>: which button closed a <see cref="ContentDialog"/>.</summary>
public enum ContentDialogResult
{
    /// <summary>No button: <see cref="ContentDialog.Hide"/>, or Back.</summary>
    None,

    /// <summary>The primary button.</summary>
    Primary,

    /// <summary>The secondary button.</summary>
    Secondary,
}

/// <summary>The arguments of <see cref="ContentDialog.Opened"/>: none, as WinRT's.</summary>
public sealed class ContentDialogOpenedEventArgs : EventArgs
{
    internal ContentDialogOpenedEventArgs()
    {
    }
}

/// <summary>The arguments of <see cref="ContentDialog.Closed"/>: why it closed.</summary>
public sealed class ContentDialogClosedEventArgs : EventArgs
{
    internal ContentDialogClosedEventArgs(ContentDialogResult result) => Result = result;

    /// <summary>Which button closed the dialog; <see cref="ContentDialogResult.None"/> for <c>Hide()</c> and Back.</summary>
    public ContentDialogResult Result { get; }
}

/// <summary>
/// The arguments of <see cref="ContentDialog.Closing"/>: why it is closing, and a way to keep it open, now or, with a
/// deferral, once the app has decided.
/// </summary>
public sealed class ContentDialogClosingEventArgs : EventArgs
{
    internal ContentDialogClosingEventArgs(ContentDialogResult result) => Result = result;

    /// <summary>Which button is closing the dialog; <see cref="ContentDialogResult.None"/> for <c>Hide()</c> and Back.</summary>
    public ContentDialogResult Result { get; }

    /// <summary>Set to keep the dialog open.</summary>
    public bool Cancel { get; set; }

    internal Deferrals Deferrals { get; } = new();

    /// <summary>Holds the closing until the deferral is completed; <see cref="Cancel"/> is read then.</summary>
    public ContentDialogClosingDeferral GetDeferral() => new(Deferrals.Take());
}

/// <summary>
/// The arguments of <see cref="ContentDialog.PrimaryButtonClick"/> and <see cref="ContentDialog.SecondaryButtonClick"/>:
/// a way to keep the dialog open, now or, with a deferral, once the app has decided.
/// </summary>
public sealed class ContentDialogButtonClickEventArgs : EventArgs
{
    internal ContentDialogButtonClickEventArgs()
    {
    }

    /// <summary>Set to keep the dialog open: the button's command doesn't run and the dialog doesn't close.</summary>
    public bool Cancel { get; set; }

    internal Deferrals Deferrals { get; } = new();

    /// <summary>Holds the click until the deferral is completed; <see cref="Cancel"/> is read then.</summary>
    public ContentDialogButtonClickDeferral GetDeferral() => new(Deferrals.Take());
}

/// <summary>A deferral of <see cref="ContentDialog.Closing"/>, from <see cref="ContentDialogClosingEventArgs.GetDeferral"/>.</summary>
public sealed class ContentDialogClosingDeferral
{
    private readonly Action _complete;

    internal ContentDialogClosingDeferral(Action complete) => _complete = complete;

    /// <summary>The app has decided: the dialog closes unless <see cref="ContentDialogClosingEventArgs.Cancel"/> is set.</summary>
    public void Complete() => _complete();
}

/// <summary>A deferral of a button's click, from <see cref="ContentDialogButtonClickEventArgs.GetDeferral"/>.</summary>
public sealed class ContentDialogButtonClickDeferral
{
    private readonly Action _complete;

    internal ContentDialogButtonClickDeferral(Action complete) => _complete = complete;

    /// <summary>The app has decided: the dialog closes unless <see cref="ContentDialogButtonClickEventArgs.Cancel"/> is set.</summary>
    public void Complete() => _complete();
}

/// <summary>
/// The deferrals taken from one event's arguments: what was waiting on them runs when the last is completed, on the UI
/// thread, or at once if none was taken. A deferral completed twice counts once; one taken once the decision has been
/// made, as by a handler after an <c>await</c>, throws.
/// </summary>
internal sealed class Deferrals
{
    private readonly object _gate = new();
    private int _open;
    private bool _decided;
    private Action? _then;

    public Action Take()
    {
        lock (_gate)
        {
            if (_decided)
                throw new InvalidOperationException("A deferral must be taken in the event's handler, before it returns.");
            _open++;
        }

        var done = 0;
        return () =>
        {
            if (Interlocked.Exchange(ref done, 1) == 1)
                return;
            if (Dispatcher.UIThread.CheckAccess())
                Release();
            else
                Dispatcher.UIThread.Post(Release);
        };
    }

    /// <summary>Runs <paramref name="then"/> now if no deferral is open, or when the last is completed.</summary>
    public void Then(Action then)
    {
        lock (_gate)
        {
            if (_open > 0)
            {
                _then = then;
                return;
            }

            _decided = true;
        }

        then();
    }

    private void Release()
    {
        Action? then;
        lock (_gate)
        {
            if (--_open > 0 || _then is null)
                return;
            then = _then;
            _then = null;
            _decided = true;
        }

        then();
    }
}
