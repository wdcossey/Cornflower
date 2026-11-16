using Avalonia.Headless;
using Avalonia.Threading;

namespace Cornflower.Tests.Sample;

/// <summary>
/// Frames for the sample shell's tests: since the Tier 2 roll-up each page comes in with the turnstile (425 ms) and
/// turns back to the list (430 ms), which the tests run on a <see cref="TestClock"/>.
/// </summary>
internal static class ShellTurns
{
    /// <summary>Enough frames for a page to be drawn and the frame after it to come.</summary>
    public static void Draw()
    {
        for (var frame = 0; frame < 4; frame++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// Every turn asked for, up to three one after another, to its end on <paramref name="clock"/>, and the frame at rest
    /// after the last, which the ready line waits for.
    /// </summary>
    public static void Settle(TestClock clock)
    {
        for (var turn = 0; turn < 3; turn++)
        {
            Draw();
            clock.At(clock.Now.TotalMilliseconds + 500);
        }

        Draw();
    }
}
