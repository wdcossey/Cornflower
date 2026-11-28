using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's plain validation page (#241, #257).</summary>
public class PlainValidationPageTests
{
    // #262: a binding holds its Source weakly, so a Person only the binding held went at the first GC (the Pixel's
    // came soon), and clearing the name then showed a TargetException from the setter's missing target.
    [AvaloniaTheory]
    [InlineData("NameBox")]
    [InlineData("LightName")]
    public void Clearing_The_Name_After_A_GC_Shows_Its_Error(string name)
    {
        var page = new PlainValidationPage();
        using var scene = new Scene(page);
        var field = page.FindControl<TextBox>(name)!;
        Assert.Equal("Ada", field.Text);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        field.Text = "";
        Dispatcher.UIThread.RunJobs();

        var errors = field.FindAncestorOfType<DataValidationErrors>()!;
        var line = Assert.Single(errors.GetVisualDescendants().OfType<TextBlock>(), block => block.IsEffectivelyVisible && !field.IsVisualAncestorOf(block));
        Assert.Equal("Enter a name.", line.Text);
    }
}
