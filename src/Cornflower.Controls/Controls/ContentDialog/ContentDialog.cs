using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>ContentDialog</c>: a dialog over the page with a <see cref="Title"/>, its content, and up to two buttons,
/// shown with <see cref="ShowAsync()"/>, which completes with the button that closed it.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour only. <see cref="ShowAsync(TopLevel)"/> shows the dialog in a <see cref="Popup"/> of its own, over the
/// window; a theme draws it, places it and moves it. The template's <c>PART_Button1Host</c> and
/// <c>PART_Button2Host</c> (<see cref="Decorator"/>s) take the buttons, as the phone's code fills its hosts: the first
/// host the primary button, or the secondary when there is no primary; the second the secondary when there are both. A
/// host with no button is hidden. A button is there while its text is set.
/// </para>
/// <para>
/// A button's click raises <see cref="PrimaryButtonClick"/> or <see cref="SecondaryButtonClick"/>; unless that is
/// cancelled, the button's command runs and the dialog closes with its result. Closing, by a button or
/// <see cref="Hide"/>, raises <see cref="Closing"/>, which may keep it open; then <see cref="Closed"/>, and then
/// <see cref="ShowAsync()"/>'s task completes, as the phone's do in one frame (#150). Either event's arguments can
/// defer the decision.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dialog = new ContentDialog { Title = "delete", Content = "Delete this photo?", PrimaryButtonText = "delete",
///     SecondaryButtonText = "cancel" };
/// if (await dialog.ShowAsync() == ContentDialogResult.Primary) ...
/// </code>
/// </example>
[TemplatePart(Button1HostPart, typeof(Decorator))]
[TemplatePart(Button2HostPart, typeof(Decorator))]
public class ContentDialog : ContentControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<ContentDialog, object?>(nameof(Title));

    /// <summary>Defines <see cref="TitleTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        AvaloniaProperty.Register<ContentDialog, IDataTemplate?>(nameof(TitleTemplate));

    /// <summary>Defines <see cref="PrimaryButtonText"/>.</summary>
    public static readonly StyledProperty<string?> PrimaryButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(PrimaryButtonText));

    /// <summary>Defines <see cref="SecondaryButtonText"/>.</summary>
    public static readonly StyledProperty<string?> SecondaryButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(SecondaryButtonText));

    /// <summary>Defines <see cref="PrimaryButtonCommand"/>.</summary>
    public static readonly StyledProperty<ICommand?> PrimaryButtonCommandProperty =
        AvaloniaProperty.Register<ContentDialog, ICommand?>(nameof(PrimaryButtonCommand));

    /// <summary>Defines <see cref="SecondaryButtonCommand"/>.</summary>
    public static readonly StyledProperty<ICommand?> SecondaryButtonCommandProperty =
        AvaloniaProperty.Register<ContentDialog, ICommand?>(nameof(SecondaryButtonCommand));

    /// <summary>Defines <see cref="PrimaryButtonCommandParameter"/>.</summary>
    public static readonly StyledProperty<object?> PrimaryButtonCommandParameterProperty =
        AvaloniaProperty.Register<ContentDialog, object?>(nameof(PrimaryButtonCommandParameter));

    /// <summary>Defines <see cref="SecondaryButtonCommandParameter"/>.</summary>
    public static readonly StyledProperty<object?> SecondaryButtonCommandParameterProperty =
        AvaloniaProperty.Register<ContentDialog, object?>(nameof(SecondaryButtonCommandParameter));

    /// <summary>Defines <see cref="IsPrimaryButtonEnabled"/>.</summary>
    public static readonly StyledProperty<bool> IsPrimaryButtonEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsPrimaryButtonEnabled), true);

    /// <summary>Defines <see cref="IsSecondaryButtonEnabled"/>.</summary>
    public static readonly StyledProperty<bool> IsSecondaryButtonEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsSecondaryButtonEnabled), true);

    /// <summary>Defines <see cref="FullSizeDesired"/>.</summary>
    public static readonly StyledProperty<bool> FullSizeDesiredProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(FullSizeDesired));

    private const string Button1HostPart = "PART_Button1Host";
    private const string Button2HostPart = "PART_Button2Host";

    private readonly Button _primary;
    private readonly Button _secondary;
    private Decorator? _host1;
    private Decorator? _host2;
    private Popup? _popup;
    private TaskCompletionSource<ContentDialogResult>? _result;
    private State _state;
    private int _shows;
    private int _clickingIn = -1;
    private IInputElement? _focusBefore;

    /// <summary>Initialises a dialog with no title, content or buttons.</summary>
    public ContentDialog()
    {
        _primary = MakeButton(PrimaryButtonTextProperty, IsPrimaryButtonEnabledProperty, PrimaryButtonCommandProperty,
            PrimaryButtonCommandParameterProperty);
        _secondary = MakeButton(SecondaryButtonTextProperty, IsSecondaryButtonEnabledProperty,
            SecondaryButtonCommandProperty, SecondaryButtonCommandParameterProperty);
        // Modal, as WinRT's: it takes the keyboard focus while shown, and Tab stays in it.
        Focusable = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
    }

    private enum State
    {
        Closed,
        Open,

        /// <summary><see cref="Closing"/> raised, its decision deferred.</summary>
        Closing,
    }

    /// <summary>Raised as the dialog is shown, in <see cref="ShowAsync()"/>'s call.</summary>
    public event EventHandler<ContentDialogOpenedEventArgs>? Opened;

    /// <summary>Raised when the dialog is asked to close, by a button or <see cref="Hide"/>: it may be kept open.</summary>
    public event EventHandler<ContentDialogClosingEventArgs>? Closing;

    /// <summary>Raised when the dialog has closed, before <see cref="ShowAsync()"/>'s task completes.</summary>
    public event EventHandler<ContentDialogClosedEventArgs>? Closed;

    /// <summary>Raised when the primary button is clicked, before its command runs and the dialog closes.</summary>
    public event EventHandler<ContentDialogButtonClickEventArgs>? PrimaryButtonClick;

    /// <summary>Raised when the secondary button is clicked, before its command runs and the dialog closes.</summary>
    public event EventHandler<ContentDialogButtonClickEventArgs>? SecondaryButtonClick;

    /// <summary>What the template shows above the content.</summary>
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

    /// <summary>The primary button's text; no primary button while it is null or empty.</summary>
    public string? PrimaryButtonText
    {
        get => GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    /// <summary>The secondary button's text; no secondary button while it is null or empty.</summary>
    public string? SecondaryButtonText
    {
        get => GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    /// <summary>
    /// What the primary button runs when clicked, before the dialog closes; the button is disabled while it can't run.
    /// </summary>
    public ICommand? PrimaryButtonCommand
    {
        get => GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }

    /// <summary>
    /// What the secondary button runs when clicked, before the dialog closes; the button is disabled while it can't run.
    /// </summary>
    public ICommand? SecondaryButtonCommand
    {
        get => GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }

    /// <summary>The parameter of <see cref="PrimaryButtonCommand"/>.</summary>
    public object? PrimaryButtonCommandParameter
    {
        get => GetValue(PrimaryButtonCommandParameterProperty);
        set => SetValue(PrimaryButtonCommandParameterProperty, value);
    }

    /// <summary>The parameter of <see cref="SecondaryButtonCommand"/>.</summary>
    public object? SecondaryButtonCommandParameter
    {
        get => GetValue(SecondaryButtonCommandParameterProperty);
        set => SetValue(SecondaryButtonCommandParameterProperty, value);
    }

    /// <summary>Whether the primary button can be clicked. True by default.</summary>
    public bool IsPrimaryButtonEnabled
    {
        get => GetValue(IsPrimaryButtonEnabledProperty);
        set => SetValue(IsPrimaryButtonEnabledProperty, value);
    }

    /// <summary>Whether the secondary button can be clicked. True by default.</summary>
    public bool IsSecondaryButtonEnabled
    {
        get => GetValue(IsSecondaryButtonEnabledProperty);
        set => SetValue(IsSecondaryButtonEnabledProperty, value);
    }

    /// <summary>Whether the dialog fills the window rather than being as tall as its content. False by default.</summary>
    public bool FullSizeDesired
    {
        get => GetValue(FullSizeDesiredProperty);
        set => SetValue(FullSizeDesiredProperty, value);
    }

    /// <summary>The popup the dialog is shown in, once it has been shown.</summary>
    internal Popup? Popup => _popup;

    /// <summary>
    /// Set by a theme that draws the dialog going: called as it closes, in place of closing its popup at once; the theme
    /// closes the popup when it has gone. None by default.
    /// </summary>
    internal Action? CloseMotion { get; set; }

    /// <summary>Whether the dialog is shown and not yet closed: from <see cref="ShowAsync()"/> to <see cref="Closed"/>.</summary>
    internal bool IsShown => _state != State.Closed;

    /// <summary>
    /// Shows the dialog over the app's window: the desktop's active window, or its main one, or the single view's.
    /// </summary>
    /// <returns>Which button closed it; <see cref="ContentDialogResult.None"/> for <see cref="Hide"/> and Back.</returns>
    /// <exception cref="InvalidOperationException">The app has no window, or the dialog is already shown.</exception>
    public Task<ContentDialogResult> ShowAsync() =>
        ShowAsync(AppTopLevel(Application.Current?.ApplicationLifetime) ?? throw new InvalidOperationException(
            "There is no window to show the dialog over: pass one to ShowAsync(TopLevel)."));

    /// <summary>Shows the dialog over <paramref name="topLevel"/>, a window or a single view's top level.</summary>
    /// <returns>Which button closed it; <see cref="ContentDialogResult.None"/> for <see cref="Hide"/> and Back.</returns>
    /// <exception cref="InvalidOperationException">
    /// The dialog is already shown, or is in a tree of its own: declare it in resources, or make it in code.
    /// </exception>
    public Task<ContentDialogResult> ShowAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        if (_state != State.Closed)
            throw new InvalidOperationException("The dialog is already shown.");
        if (Parent is not null && Parent != _popup)
            throw new InvalidOperationException(
                "The dialog is in a tree of its own: declare it in resources, or make it in code, and show it with ShowAsync.");

        if (_popup is null)
        {
            _popup = new Popup
            {
                Child = this,
                IsLightDismissEnabled = false,
                Placement = PlacementMode.Center,
            };
            _popup.Closed += OnPopupClosed;
        }

        // One still drawn as it goes is gone at once.
        if (_popup.IsOpen)
            _popup.IsOpen = false;
        if (_popup.Parent != topLevel)
        {
            // In the window's logical tree, for its resources, its theme and the dialog's own theme.
            ((ISetLogicalParent)_popup).SetParent(null);
            ((ISetLogicalParent)_popup).SetParent(topLevel);
        }

        _popup.PlacementTarget = topLevel;
        var result = _result = new TaskCompletionSource<ContentDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _state = State.Open;
        _shows++;
        _clickingIn = -1;
        var focused = topLevel.FocusManager?.GetFocusedElement();
        _popup.IsOpen = true;
        if (!_popup.IsOpen)
        {
            _state = State.Closed;
            _result = null;
            throw new InvalidOperationException("The dialog's popup didn't open.");
        }

        _focusBefore = focused;
        Focus();
        Opened?.Invoke(this, new ContentDialogOpenedEventArgs());
        return result.Task;
    }

    /// <summary>
    /// Closes the dialog with <see cref="ContentDialogResult.None"/>, unless <see cref="Closing"/> keeps it open. Does
    /// nothing while it isn't shown.
    /// </summary>
    public void Hide() => Close(ContentDialogResult.None);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host1 = e.NameScope.Find<Decorator>(Button1HostPart);
        _host2 = e.NameScope.Find<Decorator>(Button2HostPart);
        PlaceButtons();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PrimaryButtonTextProperty || change.Property == SecondaryButtonTextProperty)
            PlaceButtons();
    }

    /// <summary>The window <see cref="ShowAsync()"/> shows the dialog over, by the app's lifetime.</summary>
    internal static TopLevel? AppTopLevel(IApplicationLifetime? lifetime) => lifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop =>
            desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow,
        ISingleViewApplicationLifetime { MainView: { } view } => TopLevel.GetTopLevel(view),
        _ => null,
    };

    private Button MakeButton(StyledProperty<string?> text, StyledProperty<bool> enabled, StyledProperty<ICommand?> command,
        StyledProperty<object?> parameter)
    {
        // The phone's buttons fill their hosts.
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            [!ContentProperty] = this[!text],
            [!IsEnabledProperty] = this[!enabled],
            // For the button's enabled state only: the dialog runs the command itself, after the click's event.
            [!Button.CommandProperty] = this[!command],
            [!Button.CommandParameterProperty] = this[!parameter],
        };
        button.Click += OnButtonClick;
        return button;
    }

    /// <summary>The buttons in the hosts, as the phone's code places them: a lone button in the first.</summary>
    private void PlaceButtons()
    {
        var primary = !string.IsNullOrEmpty(PrimaryButtonText);
        var secondary = !string.IsNullOrEmpty(SecondaryButtonText);
        var first = primary ? _primary : secondary ? _secondary : null;
        var second = primary && secondary ? _secondary : null;
        Place(_host1, first);
        Place(_host2, second);
    }

    private static void Place(Decorator? host, Button? button)
    {
        if (host is null)
            return;
        // A button still in an old template's host comes out of it first.
        if (button?.Parent is Decorator old)
            old.Child = null;
        host.Child = button;
        host.IsVisible = button is not null;
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        // The button doesn't run the command: the click's handlers may cancel, or defer, first.
        e.Handled = true;
        // One click at a time, and a deferred click is this show's only.
        var show = _shows;
        if (_state != State.Open || _clickingIn == show)
            return;
        var primary = sender == _primary;
        var args = new ContentDialogButtonClickEventArgs();
        _clickingIn = show;
        (primary ? PrimaryButtonClick : SecondaryButtonClick)?.Invoke(this, args);
        args.Deferrals.Then(() =>
        {
            if (_shows != show)
                return;
            _clickingIn = -1;
            if (args.Cancel || _state != State.Open)
                return;
            var (command, parameter) = primary
                ? (PrimaryButtonCommand, PrimaryButtonCommandParameter)
                : (SecondaryButtonCommand, SecondaryButtonCommandParameter);
            if (command?.CanExecute(parameter) == true)
                command.Execute(parameter);
            Close(primary ? ContentDialogResult.Primary : ContentDialogResult.Secondary);
        });
    }

    /// <summary><see cref="Closing"/>, and unless it keeps the dialog open, <see cref="Closed"/>, the popup's close and the result.</summary>
    private void Close(ContentDialogResult result)
    {
        if (_state != State.Open)
            return;
        _state = State.Closing;
        var show = _shows;
        var args = new ContentDialogClosingEventArgs(result);
        Closing?.Invoke(this, args);
        args.Deferrals.Then(() =>
        {
            // A deferral of an earlier show, completed late, decides nothing.
            if (_state != State.Closing || _shows != show)
                return;
            if (args.Cancel)
            {
                _state = State.Open;
                return;
            }

            // The popup's close first, so a Closed handler may show the dialog again.
            var task = Finished();
            if (CloseMotion is { } motion)
                motion();
            else if (_popup is not null)
                _popup.IsOpen = false;
            Raise(result, task);
        });
    }

    /// <summary>The dialog closed: its show's task to complete.</summary>
    private TaskCompletionSource<ContentDialogResult>? Finished()
    {
        _state = State.Closed;
        _clickingIn = -1;
        var task = _result;
        _result = null;
        return task;
    }

    /// <summary><see cref="Closed"/>, then the result, its continuations after it.</summary>
    private void Raise(ContentDialogResult result, TaskCompletionSource<ContentDialogResult>? task)
    {
        Closed?.Invoke(this, new ContentDialogClosedEventArgs(result));
        task?.TrySetResult(result);
    }

    /// <summary>
    /// The popup has closed, the dialog gone from the screen: the focus goes back where it was, and the popup leaves
    /// its window, so a dialog kept by the app doesn't keep the window. A popup closed under a shown dialog (its window
    /// closing, say) closes the dialog, with no result.
    /// </summary>
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        var focus = _focusBefore;
        _focusBefore = null;
        if (focus is Visual visual && TopLevel.GetTopLevel(visual) is not null && IsKeyboardFocusWithin)
            focus.Focus();
        if (_popup is not null)
        {
            _popup.ClearValue(Popup.PlacementTargetProperty);
            ((ISetLogicalParent)_popup).SetParent(null);
        }

        if (_state != State.Closed)
            Raise(ContentDialogResult.None, Finished());
    }
}
