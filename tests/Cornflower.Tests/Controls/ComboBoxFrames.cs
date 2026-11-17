using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using static Cornflower.Tests.Controls.ComboBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ComboBox/COMPARISON.md: the ComboBox headless on the reference app's stages (450 × 140
/// closed, 450 × 400 open, at 2.4), closed at rest, pressed and settled, disabled, with nothing selected, in the Full
/// form with nothing selected, and open inline with 3 and 5 items, dark and light, named as the reference app names its
/// captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ComboBoxFrames
/// </code>
/// </summary>
public class ComboBoxFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var (variant, name) in new[] { (ThemeVariant.Dark, "dark"), (ThemeVariant.Light, "light") })
        {
            foreach (var state in new[] { "base", "CommonStates.Pressed", "CommonStates.Disabled", "PresenterStates.InlinePlaceholder", "PresenterStates.Full" })
            {
                var full = state == "PresenterStates.Full";
                using var stage = new ComboBoxTests.Stage(full ? 6 : 3, selected: state.EndsWith("Placeholder") || full ? -1 : 0, variant: variant,
                    height: ClosedStageHeight);
                stage.Combo.IsEnabled = state != "CommonStates.Disabled";
                stage.Layout();
                if (state == "CommonStates.Pressed")
                {
                    stage.Scene.Window.MouseDown(stage.CentreOf(stage.Box) + new Vector(120, 0), MouseButton.Left);
                    stage.At(160);
                }

                Write(stage, Path.Combine(folder, $"{name}__ComboBox__closed__{state}.png"));
            }

            foreach (var count in new[] { 3, 5 })
            {
                using var stage = new ComboBoxTests.Stage(count, variant: variant);
                stage.OpenAtRest();
                Write(stage, Path.Combine(folder, $"{name}__ComboBox__open-{count}-items__stage.png"));
            }
        }
    }

    private static void Write(ComboBoxTests.Stage stage, string path)
    {
        stage.Below.IsVisible = false;   // the tests' tap target: the reference app's stage has none
        stage.Layout();
        using var frame = stage.Scene.Capture();
        frame.Save(path, PngBitmapEncoderOptions.Default);
    }
}
