using Cornflower.Controls;

namespace Cornflower.Tests;

/// <summary>
/// <see cref="CornflowerConfig"/> at the phone's defaults for the life of a test, which sets what it needs, and back at
/// them at its end. The config is process-wide, as <see cref="TestClock"/>'s clock is, so a test holds one only on the
/// UI thread and not across an <c>await</c>.
/// </summary>
internal sealed class ConfigScope : IDisposable
{
    public ConfigScope() => CornflowerConfig.Reset();

    public void Dispose() => CornflowerConfig.Reset();
}
