using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Sample;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's motion (#189): each head starts it at <see cref="App.SampleMotionSpeed"/>, or at the phone's when the
/// device run or the real-window run asks; the theme page shows <see cref="CornflowerConfig"/>'s settings and changes
/// them. The tests themselves never start the sample's motion, so they see the phone's defaults.
/// </summary>
public class MotionSettingsTests
{
    [AvaloniaFact]
    public void The_Sample_Starts_At_Twice_The_Phones_Speed_Unless_Asked_For_The_Phones_Once_A_Process()
    {
        using var config = new ConfigScope();
        try
        {
            App.ForgetStartMotion();
            App.StartMotion(phone: false);
            Assert.Equal(App.SampleMotionSpeed, CornflowerConfig.MotionSpeed);
            Assert.Equal(2, App.SampleMotionSpeed);

            // An activity made again doesn't undo what the theme page set.
            CornflowerConfig.MotionSpeed = 4;
            App.StartMotion(phone: false);
            Assert.Equal(4, CornflowerConfig.MotionSpeed);

            App.ForgetStartMotion();
            CornflowerConfig.MotionSpeed = 4;
            App.StartMotion(phone: true);
            Assert.Equal(1, CornflowerConfig.MotionSpeed);
        }
        finally
        {
            App.ForgetStartMotion();
        }
    }

    [AvaloniaFact]
    public void The_Theme_Page_Shows_The_Settings_And_Changes_Them()
    {
        using var config = new ConfigScope();
        CornflowerConfig.MotionSpeed = 2;
        CornflowerConfig.TiltSpeed = 4;
        using var scene = new Scene(new MainView());
        var page = (MainView)scene.Window.Content!;
        var animated = page.GetControl<ToggleSwitch>("AnimatedSwitch");
        var speed = page.GetControl<ComboBox>("MotionSpeedBox");
        var areas = page.GetControl<StackPanel>("AreaSpeeds").Children.OfType<ComboBox>().ToArray();
        var popups = areas[Array.FindIndex(MainView.Areas, a => a.Name == "popups")];
        var tilt = areas[Array.FindIndex(MainView.Areas, a => a.Name == "tilt")];
        var singleStep = page.GetControl<ToggleSwitch>("SingleStepSwitch");

        Assert.True(animated.IsChecked);
        Assert.Equal(2d, MainView.Speeds[speed.SelectedIndex]);
        Assert.Equal(0, popups.SelectedIndex);                               // the motion speed
        Assert.Equal(4d, MainView.Speeds[tilt.SelectedIndex - 1]);
        Assert.Equal(MainView.Areas.Length, areas.Length);

        animated.IsChecked = false;
        speed.SelectedIndex = Array.IndexOf(MainView.Speeds, 0.5);
        popups.SelectedIndex = Array.IndexOf(MainView.Speeds, 4) + 1;
        tilt.SelectedIndex = 0;
        singleStep.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.False(CornflowerConfig.Animated);
        Assert.Equal(0.5, CornflowerConfig.MotionSpeed);
        Assert.Equal(4, CornflowerConfig.PopupSpeed);
        Assert.Null(CornflowerConfig.TiltSpeed);
        Assert.True(CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen);

        // A speed the page doesn't offer shows as none chosen (shown again as the page is placed again).
        CornflowerConfig.PopupSpeed = 3;
        scene.Window.Content = null;
        scene.Window.Content = page;
        Assert.Equal(-1, popups.SelectedIndex);

        page.GetControl<Button>("PhoneMotionButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(CornflowerConfig.Animated);
        Assert.Equal(1, CornflowerConfig.MotionSpeed);
        Assert.Null(CornflowerConfig.PopupSpeed);
        Assert.False(CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen);
        Assert.True(animated.IsChecked);
        Assert.Equal(1d, MainView.Speeds[speed.SelectedIndex]);
        Assert.Equal(0, popups.SelectedIndex);
        Assert.False(singleStep.IsChecked);
    }
}
