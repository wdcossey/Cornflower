using System.Diagnostics;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Runs on its own, after the parallel tests: a hold waits on Avalonia's real-time hold timer in a pushed frame, which
/// would run other tests' queued work, and their touches, while this touch is held.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ContextMenuHoldCollection
{
    public const string Name = "context menu hold";
}

/// <summary>The plain ContextMenu opened as a phone app on Android opens it: a long press (a touch hold).</summary>
[Collection(ContextMenuHoldCollection.Name)]
public class ContextMenuHoldTests
{
    /// <summary>Runs the dispatcher's loop on real time until <paramref name="done"/> or <paramref name="limitMs"/>: a hold's timer.</summary>
    private static void RunFor(double limitMs, Func<bool> done)
    {
        var frame = new DispatcherFrame();
        var watch = Stopwatch.StartNew();
        var poll = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Background, (_, _) =>
        {
            if (watch.Elapsed.TotalMilliseconds >= limitMs || done())
                frame.Continue = false;
        });
        poll.Start();
        Dispatcher.UIThread.PushFrame(frame);
        poll.Stop();
    }

    // Avalonia opens a ContextMenu on a long press (a touch hold, as on Android): it opens on the MenuFlyout's binder,
    // below the held control, with the MenuFlyout's motion.
    [AvaloniaFact]
    public void A_Long_Press_Opens_It_Below_The_Held_Control()
    {
        using var stage = new ContextMenuTests.Stage(targetTop: 200);
        var at = PlainParts.Centre(stage.Scene, stage.Button);

        var touch = stage.Scene.Window.TouchBegin(at);
        RunFor(3000, () => stage.Menu.IsOpen);
        try
        {
            Assert.True(stage.Menu.IsOpen);
            Assert.True(PhoneContextMenu.IsOpening(stage.Menu));
            Assert.True(stage.Popup.IsUsingOverlayLayer);
            stage.At(stage.Clock.Now.TotalMilliseconds + 600);
            var bounds = stage.Bounds();
            Assert.Equal(0, bounds.X);
            Assert.Equal(Scene.ReferenceWidth, bounds.Width);
            var targetBottom = stage.Button.TranslatePoint(new Point(0, stage.Button.Bounds.Height), stage.Scene.Window)!.Value.Y;
            Assert.Equal(targetBottom, bounds.Y, 3);
        }
        finally
        {
            stage.Scene.Window.TouchEnd(touch, at);
            Dispatcher.UIThread.RunJobs();
        }

        // Let go, the menu stays open; a tap on an item clicks it and closes the menu at once.
        Assert.True(stage.Menu.IsOpen);
        var clicked = 0;
        stage.Item(1).Click += (_, _) => clicked++;
        var item = PlainParts.Centre(stage.Scene, stage.Item(1));
        var tap = stage.Scene.Window.TouchBegin(item);
        stage.Scene.Window.TouchEnd(tap, item);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicked);
        Assert.False(stage.Menu.IsOpen);
    }
}
