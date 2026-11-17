using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using static Cornflower.Tests.Controls.AutoSuggestBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/AutoSuggestBox/COMPARISON.md: the reference app's six AutoSuggestBox grabs (#150) headless,
/// 450 × 800 at 2.4, named as the reference app names them. Focused, the stage is drawn as the phone draws it, the page
/// moved up so the control's top is at the window's top (ours doesn't move: <see cref="AutoSuggestBoxTests.Stage"/>
/// stands it there), with the caret transparent and no keyboard drawn; twelve suggestions stop at the phone's keyboard's
/// top (a <see cref="AutoSuggestBoxTests.FakeKeyboard"/>). By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c>
/// names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~AutoSuggestBoxFrames
/// </code>
/// </summary>
public class AutoSuggestBoxFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        var variants = new (string Name, bool Focus, string? Text, int Suggestions)[]
        {
            ("rest", false, null, 0), ("focused", true, null, 0), ("text", true, "ap", 0),
            ("list-1", true, "ap", 1), ("list-3", true, "a", 3), ("list-12", true, "a", 12),
        };
        foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var (name, focus, text, n) in variants)
        {
            var keyboard = new AutoSuggestBoxTests.FakeKeyboard();
            using var stage = new AutoSuggestBoxTests.Stage(theme, atTop: focus, keyboard: keyboard);
            stage.TextBox.CaretBrush = Brushes.Transparent;
            if (focus)
                keyboard.Show(Keyboard);
            if (n > 0)
                stage.Open(n, text!);
            else if (focus)
            {
                stage.Focus();
                stage.Box.Text = text;
            }
            stage.Settle();
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Scene.Capture();
            frame.Save(Path.Combine(folder, $"{theme.ToString().ToLowerInvariant()}__AutoSuggestBox__{name}__popup__screen.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
