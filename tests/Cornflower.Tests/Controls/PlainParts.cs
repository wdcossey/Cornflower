using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cornflower.Tests.Controls;

/// <summary>What the plain themes' tests share (ADR 0007): resources by key, template parts by name, and a press.</summary>
internal static class PlainParts
{
    /// <summary>The resource <paramref name="key"/> as <paramref name="element"/> finds it, in its own variant.</summary>
    public static T Resource<T>(StyledElement element, string key) =>
        Assert.IsAssignableFrom<T>(element.FindResource(element.ActualThemeVariant, key));

    /// <summary>The part named <paramref name="name"/> in <paramref name="control"/>'s own template.</summary>
    public static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(part => part.Name == name && part.TemplatedParent == control);

    /// <summary>The centre of <paramref name="control"/> in <paramref name="scene"/>'s window.</summary>
    public static Point Centre(Scene scene, Visual control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), scene.Window)
        ?? throw new InvalidOperationException("Not in the window.");

    /// <summary>The left button down on <paramref name="control"/>'s centre.</summary>
    public static Point Press(Scene scene, Visual control)
    {
        var at = Centre(scene, control);
        scene.Window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        return at;
    }

    /// <summary>The left button up at <paramref name="at"/>.</summary>
    public static void Release(Scene scene, Point at)
    {
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
