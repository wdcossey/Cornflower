using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The groups of <see cref="CornflowerConfig"/> a template's Avalonia transitions run at (<see cref="PhoneTransitions"/>).
/// The theme's own, as XAML here sets it; <see cref="MotionArea"/> is <c>Cornflower.Controls</c>' internal one.
/// </summary>
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
/// A transition starts inside Avalonia's property change, before any handler sees the change, so the speed can't be
/// read as it starts. It is set instead whenever it could change: when an element it follows is placed in a window (an
/// element with <see cref="AreaProperty"/>: the ListView's check boxes, the Pivot's headers; and the ToggleSwitch's
/// <c>KnobTransitions</c>, which <see cref="PhoneToggleSwitch"/> has it follow), and for every placed one when the
/// config changes (<c>CornflowerConfig.Changed</c>). So each transition that starts has the config's speed of that
/// moment, as a motion on <see cref="CornflowerClock"/> does.
/// </remarks>
internal static class PhoneTransitions
{
    /// <summary>Each transition's own duration, the phone's, as first seen.</summary>
    private static readonly ConditionalWeakTable<TransitionBase, StrongBox<TimeSpan>> Written = new();

    /// <summary>What each followed element's transitions are and their area.</summary>
    private static readonly ConditionalWeakTable<Visual, Follower> Followers = new();

    /// <summary>The followed elements in a window now, to set again when the config changes.</summary>
    private static readonly HashSet<Follower> Placed = [];

    /// <summary>Which speed the element's <c>Transitions</c> run at; set by a template.</summary>
    public static readonly AttachedProperty<PhoneTransitionArea> AreaProperty =
        AvaloniaProperty.RegisterAttached<Animatable, PhoneTransitionArea>("Area", typeof(PhoneTransitions));

    static PhoneTransitions()
    {
        CornflowerConfig.Changed += () =>
        {
            foreach (var follower in Placed)
                follower.Rescale();
        };
        AreaProperty.Changed.AddClassHandler<Visual>((visual, change) =>
        {
            MotionArea? area = change.GetNewValue<PhoneTransitionArea>() switch
            {
                PhoneTransitionArea.None => null,
                PhoneTransitionArea.PivotAndHub => MotionArea.PivotAndHub,
                PhoneTransitionArea.Control => MotionArea.Control,
                var other => throw new ArgumentOutOfRangeException(nameof(change), other, "An area with no MotionArea."),
            };
            if (area is { } known)
                Follow(visual, () => visual.Transitions, known);
            else
                Unfollow(visual);
        });
    }

    public static PhoneTransitionArea GetArea(Animatable element) => element.GetValue(AreaProperty);

    public static void SetArea(Animatable element, PhoneTransitionArea value) => element.SetValue(AreaProperty, value);

    /// <summary>
    /// Keeps the transitions <paramref name="of"/> gives at <paramref name="area"/>'s speed while
    /// <paramref name="owner"/> is in a window: set now if it is, again when it is placed and when the config changes.
    /// </summary>
    public static void Follow(Visual owner, Func<Transitions?> of, MotionArea area)
    {
        Unfollow(owner);
        var follower = new Follower(of, area);
        Followers.Add(owner, follower);
        owner.AttachedToVisualTree += follower.OnAttached;
        owner.DetachedFromVisualTree += follower.OnDetached;
        if (owner.IsAttachedToVisualTree())
            follower.OnAttached(owner, null!);
    }

    /// <summary>Stops <see cref="Follow"/>; the durations stay as last set.</summary>
    public static void Unfollow(Visual owner)
    {
        if (!Followers.TryGetValue(owner, out var follower))
            return;
        Followers.Remove(owner);
        owner.AttachedToVisualTree -= follower.OnAttached;
        owner.DetachedFromVisualTree -= follower.OnDetached;
        Placed.Remove(follower);
    }

    /// <summary>Sets each of <paramref name="transitions"/>' durations for <paramref name="area"/>'s speed now.</summary>
    public static void Rescale(Transitions? transitions, MotionArea area)
    {
        if (transitions is null)
            return;
        var speed = CornflowerConfig.SpeedOf(area);
        foreach (var transition in transitions)
        {
            if (transition is not TransitionBase timed)
                continue;
            var written = Written.GetValue(timed, static t => new StrongBox<TimeSpan>(t.Duration)).Value;
            var duration = !CornflowerConfig.Animated ? TimeSpan.Zero : CornflowerClock.RealTime(written, speed);
            if (timed.Duration != duration)
                timed.Duration = duration;
        }
    }

    /// <summary>One followed element: where its transitions are, their area; in <see cref="Placed"/> while in a window.</summary>
    private sealed class Follower(Func<Transitions?> of, MotionArea area)
    {
        public void Rescale() => PhoneTransitions.Rescale(of(), area);

        public void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Placed.Add(this);
            Rescale();
        }

        public void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Placed.Remove(this);
    }
}
