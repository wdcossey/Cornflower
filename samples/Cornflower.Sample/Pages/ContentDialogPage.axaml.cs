using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ContentDialog: dialogs with two buttons, one, none, full size, too long for the screen and with a TextBox,
/// what the last one returned shown; the MessageDialog text styles; and a switch for the light theme.
/// </summary>
[SamplePage("contentdialog", Order = 157)]
public partial class ContentDialogPage : UserControl
{
    private const string Body = "Agjy 0123 the dialog's body text, on one line.";

    private TopLevel? _themed;
    private ThemeVariant? _windowTheme;

    public ContentDialogPage()
    {
        InitializeComponent();
    }

    /// <summary>The dialog shown last, while it is shown.</summary>
    internal ContentDialog? Shown { get; private set; }

    private void OnShowTwo(object? sender, RoutedEventArgs e) =>
        Show("two buttons", new ContentDialog { Title = "content dialog", Content = Body, PrimaryButtonText = "ok", SecondaryButtonText = "cancel" });

    private void OnShowOne(object? sender, RoutedEventArgs e) =>
        Show("one button", new ContentDialog { Title = "content dialog", Content = Body, SecondaryButtonText = "close" });

    private void OnShowNone(object? sender, RoutedEventArgs e) =>
        Show("no buttons", new ContentDialog { Title = "content dialog", Content = "No buttons: press Back." });

    private void OnShowFull(object? sender, RoutedEventArgs e) =>
        Show("full size", new ContentDialog
        {
            Title = "content dialog",
            Content = Body,
            PrimaryButtonText = "ok",
            SecondaryButtonText = "cancel",
            FullSizeDesired = true,
        });

    private void OnShowLong(object? sender, RoutedEventArgs e) =>
        Show("long", new ContentDialog
        {
            Title = "Agjy 0123 a content dialog title long enough to wrap onto a second line",
            Content = string.Join("\n", Enumerable.Range(1, 40).Select(i => $"line {i} of the dialog's long body")),
            PrimaryButtonText = "ok",
            SecondaryButtonText = "cancel",
        });

    private void OnShowTextBox(object? sender, RoutedEventArgs e) =>
        Show("text box", new ContentDialog
        {
            Title = "content dialog",
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Type a name:" },
                    new TextBox { Margin = new Thickness(0, 6, 0, 0) },
                },
            },
            PrimaryButtonText = "ok",
            SecondaryButtonText = "cancel",
        });

    private async void Show(string name, ContentDialog dialog)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;
        dialog.Opened += (_, _) => State.Text = $"{name} opened";
        Shown = dialog;
        try
        {
            var result = await dialog.ShowAsync(top);
            State.Text = $"{name}: {result.ToString().ToLowerInvariant()}";
        }
        catch (InvalidOperationException error)
        {
            State.Text = $"{name}: {error.Message}";
        }
        finally
        {
            if (Shown == dialog)
                Shown = null;
        }
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
            return;
        if (_themed is null)
        {
            _themed = top;
            _windowTheme = top.RequestedThemeVariant;
        }

        top.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : _windowTheme;
    }

    /// <summary>The window's own theme back as the page goes.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_themed is not null)
        {
            _themed.RequestedThemeVariant = _windowTheme;
            _themed = null;
        }

        base.OnDetachedFromVisualTree(e);
    }
}
