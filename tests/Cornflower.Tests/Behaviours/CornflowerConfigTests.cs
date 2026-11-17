using Avalonia.Headless.XUnit;
using Cornflower.Controls;

namespace Cornflower.Tests.Behaviours;

/// <summary>
/// <see cref="CornflowerConfig"/> itself: the phone's defaults, the speeds it takes, which speed an area runs at, and
/// <see cref="CornflowerClock"/>'s scaling of a motion's time. That each setting reaches its motions is held beside each
/// motion's own tests (the popup host, the MenuFlyout, the page transitions, the tilt, the Pivot, the Hub, the AppBar,
/// the ToggleSwitch, the ListView).
/// </summary>
public class CornflowerConfigTests
{
    private static readonly MotionArea[] Areas = Enum.GetValues<MotionArea>();

    [AvaloniaFact]
    public void The_Defaults_Are_The_Phones()
    {
        using var config = new ConfigScope();
        Assert.True(CornflowerConfig.Animated);
        Assert.Equal(1, CornflowerConfig.MotionSpeed);
        Assert.Null(CornflowerConfig.PopupSpeed);
        Assert.Null(CornflowerConfig.PageSpeed);
        Assert.Null(CornflowerConfig.TiltSpeed);
        Assert.Null(CornflowerConfig.PivotAndHubSpeed);
        Assert.Null(CornflowerConfig.ControlSpeed);
        Assert.False(CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen);
        Assert.All(Areas, area => Assert.Equal(1, CornflowerClock.SpeedOf(area)));
    }

    [AvaloniaFact]
    public void An_Area_Runs_At_Its_Own_Speed_Else_MotionSpeed_And_Off_At_No_Motion()
    {
        using var config = new ConfigScope();
        CornflowerConfig.MotionSpeed = 2;
        Assert.All(Areas, area => Assert.Equal(2, CornflowerClock.SpeedOf(area)));

        CornflowerConfig.PopupSpeed = 0.5;
        CornflowerConfig.PageSpeed = 3;
        CornflowerConfig.TiltSpeed = 4;
        CornflowerConfig.PivotAndHubSpeed = 5;
        CornflowerConfig.ControlSpeed = 6;
        Assert.Equal(0.5, CornflowerClock.SpeedOf(MotionArea.Popup));
        Assert.Equal(3, CornflowerClock.SpeedOf(MotionArea.Page));
        Assert.Equal(4, CornflowerClock.SpeedOf(MotionArea.Tilt));
        Assert.Equal(5, CornflowerClock.SpeedOf(MotionArea.PivotAndHub));
        Assert.Equal(6, CornflowerClock.SpeedOf(MotionArea.Control));

        CornflowerConfig.Animated = false;
        Assert.All(Areas, area => Assert.Equal(CornflowerConfig.NoMotion, CornflowerClock.SpeedOf(area)));
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_Speed_Is_A_Finite_Number_Above_0(double speed)
    {
        using var config = new ConfigScope();
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.MotionSpeed = speed);
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.PopupSpeed = speed);
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.PageSpeed = speed);
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.TiltSpeed = speed);
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.PivotAndHubSpeed = speed);
        Assert.Throws<ArgumentOutOfRangeException>(() => CornflowerConfig.ControlSpeed = speed);
        Assert.Equal(1, CornflowerConfig.MotionSpeed);
        Assert.Null(CornflowerConfig.PopupSpeed);
    }

    [AvaloniaFact]
    public void A_Motions_Time_Is_The_Real_Time_Times_Its_Speed()
    {
        var real = TimeSpan.FromTicks(1_234_567);
        Assert.Equal(real, CornflowerClock.AtSpeed(real, 1));
        Assert.Equal(TimeSpan.FromTicks(2_469_134), CornflowerClock.AtSpeed(real, 2));
        Assert.Equal(TimeSpan.FromTicks(617_284), CornflowerClock.AtSpeed(real, 0.5));
        // No motion is so long that its time overflows a sum.
        Assert.Equal(TimeSpan.FromDays(36_500), CornflowerClock.AtSpeed(TimeSpan.FromDays(400), CornflowerConfig.NoMotion));

        using var clock = new TestClock();
        clock.At(100);
        Assert.Equal(TimeSpan.FromMilliseconds(180), CornflowerClock.Since(TimeSpan.FromMilliseconds(10), 2));
        var ran = 0.0;
        CornflowerClock.Schedule(() => ran = clock.Now.TotalMilliseconds, TimeSpan.FromMilliseconds(100), 4);
        clock.At(200);
        Assert.Equal(125, ran);
    }
}
