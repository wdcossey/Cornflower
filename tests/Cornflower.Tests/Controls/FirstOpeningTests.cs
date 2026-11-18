using System.Diagnostics;

namespace Cornflower.Tests.Controls;

/// <summary>
/// A Flyout's and a MenuFlyout's first opening in a process, by <c>IsOpen</c> (#209), run in a process of their own: in
/// the suite an earlier test has nearly always opened one already. The theme's binders hear a flyout open from a class
/// handler their type's static setup adds; left to the theme's first styling of a presenter, that setup ran in the
/// middle of the first opening by <c>IsOpen</c>, too late for it, and that flyout was never bound
/// (<c>PhonePopupBinders.SetUp</c> now runs it as the module loads). The pickers' flyouts are set up there too, untested
/// here: their controls open them by <c>ShowAt</c>.
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
        var start = new ProcessStartInfo(DotnetHost())
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
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        var ended = true;
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            // Ended here, so its pipes close and what it said so far is kept for the failure.
            ended = false;
            process.Kill(entireProcessTree: true);
        }

        var report = await output + await error;
        Assert.True(ended, "The tests' own process didn't end:" + Environment.NewLine + report);
        Assert.True(process.ExitCode == 0, report);
        Assert.Contains($"Total: {Tests.Length}, Errors: 0, Failed: 0", report);
    }

    /// <summary>The dotnet host running these tests: <c>dotnet test</c>'s, or this process's if it is one; else PATH's.</summary>
    private static string DotnetHost()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host)
            return host;
        var self = Environment.ProcessPath;
        return Path.GetFileNameWithoutExtension(self) == "dotnet" ? self! : "dotnet";
    }
}
