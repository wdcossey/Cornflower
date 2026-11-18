using System.Diagnostics;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Each flyout kind's first opening in a process, by <c>IsOpen</c> (#209), run in a process of its own: in the suite an
/// earlier test has nearly always opened one already. The theme's binders hear a flyout open from a class handler their
/// type's static setup adds; once that setup was left to the theme's first styling of a presenter, it ran in the middle
/// of the first opening by <c>IsOpen</c>, too late for it, and that flyout was never bound.
/// </summary>
public class FirstOpeningTests
{
    private static readonly string[] Tests =
    [
        $"{typeof(MenuFlyoutTests).FullName}.{nameof(MenuFlyoutTests.Opened_By_IsOpen_It_Is_Placed_Against_Its_Target_Too)}",
        $"{typeof(FlyoutTests).FullName}.{nameof(FlyoutTests.Opened_By_IsOpen_Its_Closing_Is_Held_Too)}",
    ];

    [Fact]
    public async Task The_First_Flyouts_Opened_By_IsOpen_Are_Bound()
    {
        // The test assembly is xunit v3's own runner: it runs just these, first, in a new process.
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(typeof(FirstOpeningTests).Assembly.Location);
        foreach (var test in Tests)
        {
            start.ArgumentList.Add("-method");
            start.ArgumentList.Add(test);
        }

        using var process = Process.Start(start)!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var report = await output + await error;
        Assert.True(process.ExitCode == 0, report);
        Assert.Contains($"Total: {Tests.Length}, Errors: 0, Failed: 0", report);
    }
}
