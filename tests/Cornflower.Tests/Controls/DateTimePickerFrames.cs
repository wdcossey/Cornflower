using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/DatePicker/COMPARISON.md: the reference app's DatePicker (15 July 2014) and TimePicker
/// (12:30), headed, closed on its 450 × 140 stage, and their pickers open at rest, headless at the reference profile,
/// dark and light, named as the reference app names its screenshots. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~DateTimePickerFrames
/// </code>
/// </summary>
public class DateTimePickerFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var theme = variant.ToString().ToLowerInvariant();
            foreach (var (name, open) in new[] { ("DatePicker", false), ("TimePicker", false), ("DatePickerFlyout", true), ("TimePickerFlyout", true) })
            {
                using var stage = new DateTimePickerTests.Stage(
                    name.StartsWith("Date", StringComparison.Ordinal) ? DateTimePickerTests.Date() : DateTimePickerTests.Time(),
                    variant, height: open ? Scene.ReferenceHeight : 140);
                if (open)
                    stage.OpenAtRest();
                using var frame = stage.Scene.Capture();
                frame.Save(Path.Combine(folder, $"{theme}__{name}__default__ours.png"), PngBitmapEncoderOptions.Default);
            }
        }
    }
}
