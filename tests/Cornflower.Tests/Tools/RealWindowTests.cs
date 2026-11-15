using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;
using Xunit;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace Cornflower.Tests.Tools;

/// <summary>
/// <c>tools/real-window/drive.ps1</c> without its window (#115): it takes the desktop's cursor and focus, so it is run by
/// hand, and these check what can be checked here. Its self-test, its helpers and its own text, under pwsh; and what it
/// assumes of the sample, against the sample laid out headless: where it aims on a ToggleSwitch and a Slider, the view
/// pixels in <c>helpers.ps1</c>, and the controls it finds by name, through Avalonia's automation peers, which are what
/// UI Automation reads.
/// </summary>
public partial class RealWindowTests
{
    [Fact]
    public async Task The_Self_Test_Passes_Under_Pwsh()
    {
        var script = Path.Combine(Root, "tools", "real-window", "selftest.ps1");
        var start = new ProcessStartInfo("pwsh")
        {
            ArgumentList = { "-NoProfile", "-NonInteractive", "-File", script },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        Process process;
        try
        {
            process = Process.Start(start) ?? throw new InvalidOperationException("pwsh didn't start");
        }
        catch (Win32Exception e)
        {
            throw new InvalidOperationException("pwsh 7 isn't on the PATH, and the repo's tools need it (tools/README.md).", e);
        }

        using (process)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errors = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }

            var said = $"{await output}\n{await errors}";
            Assert.True(process.ExitCode == 0, $"selftest.ps1 exited {process.ExitCode}:\n{said}");
            Assert.Contains(" checks passed", said);
        }
    }

    [AvaloniaFact]
    public void Where_It_Presses_A_ToggleSwitch_Is_The_Knob_At_Its_Rests_And_The_Header_Off_The_Thumb()
    {
        var at = Helpers();
        var page = new ToggleSwitchPage();
        using var scene = new Scene(page);
        using var frame = scene.Capture();
        var wifi = page.FindControl<ToggleSwitch>("WiFi")!;
        var thumb = Part<Control>(wifi, "SwitchThumb");
        var knob = Part<Rectangle>(wifi, "SwitchKnob");
        var track = Part<Canvas>(wifi, "PART_SwitchKnob");
        var size = wifi.Bounds.Size;

        // helpers.ps1's KnobAt, in view pixels from the switch's own right and bottom.
        var y = size.Height - (at["SwitchBelow"] + at["SwitchHeight"] / 2);
        var off = new Point(size.Width - (at["SwitchColumn"] - at["SwitchKnob"] / 2), y);
        var on = new Point(size.Width - at["SwitchKnob"] / 2, y);

        var middle = knob.TranslatePoint(new Point(knob.Bounds.Width / 2, knob.Bounds.Height / 2), wifi)!.Value;
        Assert.Equal(off.X, middle.X, RawPixel);
        Assert.Equal(off.Y, middle.Y, RawPixel);
        Assert.Equal(on.X - off.X, track.Bounds.Width, RawPixel);
        Assert.True(IsOn(HitAt(scene, wifi, off), thumb), "the knob's rest off isn't on the thumb");
        Assert.True(IsOn(HitAt(scene, wifi, on), thumb), "the knob's rest on isn't on the thumb");

        var header = new Point(at["SwitchHeaderIn"], at["SwitchHeaderIn"]);
        var hit = HitAt(scene, wifi, header);
        Assert.True(IsOn(hit, wifi) && !IsOn(hit, thumb), "the header's point isn't the switch's, off its thumb");

        // The presses it makes either side of the phone's limits.
        var half = track.Bounds.Width / 2;
        Assert.True(at["SwitchTapMove"] < PhoneToggleSwitch.TapDistance);
        Assert.InRange(at["SwitchShortDrag"], PhoneToggleSwitch.TapDistance, half);
        Assert.InRange(at["SwitchLongDrag"], half, track.Bounds.Width);
    }

    [AvaloniaTheory]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(80)]
    public void Where_It_Presses_A_Slider_For_A_Value_Is_The_Thumbs_Middle_There_On_The_Track(double value)
    {
        var at = Helpers();
        var page = new SliderPage();
        using var scene = new Scene(page);
        var volume = page.FindControl<Slider>("Volume")!;
        volume.Value = value;
        using var frame = scene.Capture();
        var thumb = Part<Control>(volume, "HorizontalThumb");
        var track = Part<Rectangle>(volume, "HorizontalTrackRect");
        var size = volume.Bounds.Size;

        // helpers.ps1's SliderAt, in view pixels from the Slider's own left and bottom.
        var x = at["SliderThumb"] / 2 + value / 100 * (size.Width - at["SliderThumb"]);
        var y = size.Height - at["SliderTrackAbove"];

        Assert.Equal(at["SliderThumb"], thumb.Bounds.Width, RawPixel);
        var middle = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, 0), volume)!.Value;
        Assert.Equal(x, middle.X, RawPixel);
        var line = track.TranslatePoint(new Point(0, track.Bounds.Height / 2), volume)!.Value;
        Assert.Equal(y, line.Y, RawPixel);
        Assert.True(IsOn(HitAt(scene, volume, new Point(x, y)), thumb), "the press isn't on the thumb");
    }

    /// <summary>
    /// Every control drive.ps1 finds by its x:Name (<c>ById</c>, <c>ReadOut</c>) is on a sample page, and Avalonia's peer
    /// for it gives UI Automation that name as its AutomationId, as a control element; a TextBlock it reads gives its text
    /// as its name.
    /// </summary>
    [AvaloniaFact]
    public void The_Controls_It_Finds_By_Name_Give_UI_Automation_That_Name()
    {
        var drive = File.ReadAllText(Path.Combine(Root, "tools", "real-window", "drive.ps1"));
        var found = FindsByName().Matches(drive).Select(m => (Way: m.Groups[1].Value, Id: m.Groups[2].Value)).Distinct().ToList();
        Assert.NotEmpty(found);
        var pages = SampleShell.Pages.Select(entry => entry.Create()).ToList();

        foreach (var (way, id) in found)
        {
            var controls = pages.Select(page => page.FindControl<Control>(id)).OfType<Control>().ToList();
            Assert.True(controls.Count > 0, $"No sample page has a control named {id}");
            foreach (var control in controls)
            {
                var peer = ControlAutomationPeer.CreatePeerForElement(control);
                Assert.Equal(id, peer.GetAutomationId());
                Assert.True(peer.IsControlElement(), $"{id} isn't a control element");
                if (way == "ReadOut")
                    Assert.Equal(Assert.IsType<TextBlock>(control).Text, peer.GetName());
            }
        }
    }

    /// <summary>
    /// How near a part has to be to where drive.ps1 aims, in view pixels: a raw pixel of the reference profile (0.42 of a
    /// view pixel at 2.4), as layout rounds each row and part to that grid. Far less than a part's size, so the aim is
    /// still on it; a screen pixel is 0.8 of a view pixel at 125 %.
    /// </summary>
    private const double RawPixel = 1 / Scene.ReferenceScaling + 0.01;

    /// <summary>helpers.ps1's view pixels, its <c>$Name = number</c> lines.</summary>
    private static Dictionary<string, double> Helpers() =>
        Constant().Matches(File.ReadAllText(Path.Combine(Root, "tools", "real-window", "helpers.ps1")))
            .ToDictionary(m => m.Groups[1].Value, m => double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(part => part.Name == name);

    /// <summary>What a press at a point in a control lands on, hit tested in the window once it has drawn a frame.</summary>
    private static IInputElement? HitAt(Scene scene, Visual control, Point point) =>
        scene.Window.InputHitTest(control.TranslatePoint(point, scene.Window)!.Value);

    /// <summary>Whether a hit is the control or inside it.</summary>
    private static bool IsOn(IInputElement? hit, Visual control) =>
        hit is Visual visual && (visual == control || control.IsVisualAncestorOf(visual));

    /// <summary>The repository, found once: the folder above the tests' output that holds the solution.</summary>
    private static string Root => LazyRoot.Value;

    private static readonly Lazy<string> LazyRoot = new(() =>
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Cornflower.sln")))
                return dir.FullName;
        throw new InvalidOperationException("Cornflower.sln not found above " + AppContext.BaseDirectory);
    });

    [GeneratedRegex(@"^\$(\w+) = ([\d.]+)\r?$", RegexOptions.Multiline)]
    private static partial Regex Constant();

    [GeneratedRegex(@"\b(ById|ReadOut) '([^']+)'")]
    private static partial Regex FindsByName();
}
