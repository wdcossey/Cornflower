using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>The groups of <see cref="CornflowerConfig"/> a template's Avalonia transitions run at (<see cref="PhoneTransitions"/>).</summary>
internal enum PhoneTransitionArea
{
    /// <summary>Not scaled: the transitions as written.</summary>
    None,

    /// <summary><see cref="CornflowerConfig.PivotAndHubSpeed"/>: the Pivot's headers.</summary>
    PivotAndHub,

    /// <summary><see cref="CornflowerConfig.ControlSpeed"/>: the ListView's check boxes.</summary>
    Control,
}

/// <summary>
/// The theme's Avalonia transitions at <see cref="CornflowerConfig"/>'s speed. They run on Avalonia's own clock, not on
/// <see cref="CornflowerClock"/>, so their time can't be scaled as a frame loop's is: their <c>Duration</c> is set
/// instead, from the phone's (the one written, remembered the first time it is seen) divided by the area's speed, or
/// 0 with <see cref="CornflowerConfig.Animated"/> off, so they are at their end at once. At the defaults it is the
/// one written.
/// </summary>
/// <remarks>
/// A transition starts inside Avalonia's property change, before any handler sees the change, so the speed is read
/// earlier: when an element with <see cref="AreaProperty"/> is placed in a window (the ListView's check boxes, the
/// Pivot's headers), and by <see cref="PhoneToggleSwitch"/> for the knob's <c>KnobTransitions</c>. A change of the
/// config reaches the next element placed, and the knob at its switch's next tap or drag.
/// </remarks>
internal static class PhoneTransitions
{
    /// <summary>Each transition's own duration, the phone's, as first seen.</summary>
    private static readonly ConditionalWeakTable<TransitionBase, StrongBox<TimeSpan>> Written = new();

    /// <summary>Which speed the element's <c>Transitions</c> run at; set by a template.</summary>
    public static readonly AttachedProperty<PhoneTransitionArea> AreaProperty =
        AvaloniaProperty.RegisterAttached<Animatable, PhoneTransitionArea>("Area", typeof(PhoneTransitions));

    static PhoneTransitions()
    {
        AreaProperty.Changed.AddClassHandler<Visual>((visual, change) =>
        {
            visual.AttachedToVisualTree -= OnAttached;
            if (change.GetNewValue<PhoneTransitionArea>() == PhoneTransitionArea.None)
                return;
            visual.AttachedToVisualTree += OnAttached;
            if (visual.IsAttachedToVisualTree())
                Rescale(visual);
        });
    }

    public static PhoneTransitionArea GetArea(Animatable element) => element.GetValue(AreaProperty);

    public static void SetArea(Animatable element, PhoneTransitionArea value) => element.SetValue(AreaProperty, value);

    /// <summary>Sets each of <paramref name="transitions"/>' durations for <paramref name="area"/>'s speed now.</summary>
    public static void Rescale(Transitions? transitions, MotionArea area)
    {
        if (transitions is null)
            return;
        var speed = CornflowerClock.SpeedOf(area);
        foreach (var transition in transitions)
        {
            if (transition is not TransitionBase timed)
                continue;
            var written = Written.GetValue(timed, static t => new StrongBox<TimeSpan>(t.Duration)).Value;
            var duration = !CornflowerConfig.Animated ? TimeSpan.Zero : speed == 1 ? written : written / speed;
            if (timed.Duration != duration)
                timed.Duration = duration;
        }
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Visual visual)
            Rescale(visual);
    }

    private static void Rescale(Visual visual)
    {
        var area = GetArea(visual) switch
        {
            PhoneTransitionArea.PivotAndHub => MotionArea.PivotAndHub,
            PhoneTransitionArea.Control => MotionArea.Control,
            _ => (MotionArea?)null,
        };
        if (area is { } known)
            Rescale(visual.Transitions, known);
    }
}
