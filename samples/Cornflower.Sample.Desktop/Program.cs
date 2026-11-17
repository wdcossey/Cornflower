using Avalonia;

namespace Cornflower.Sample.Desktop;

// No font is added here: the pages' text is Selawik, which Wp81Theme bundles.
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Avalonia reads the switch once, so it's set before Avalonia starts: the sample's Debug line
        // reads Avalonia's own meter (docs/performance/README.md).
        Diagnostics.PerformanceMonitor.EnableDiagnostics();
#endif
        // Twice the phone's motion, so trying the sample feels quick; --phone-motion, the phone's (drive.ps1 passes it).
        App.StartMotion(phone: args.Contains(App.PhoneMotionArgument));
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Public and named so, for Avalonia's designer and tools that look for it.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
