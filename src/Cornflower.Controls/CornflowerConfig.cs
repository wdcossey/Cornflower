namespace Cornflower.Controls;

/// <summary>
/// Process-wide switches for Cornflower's motion, and the named departures from the phone. Read at the moment a motion
/// starts, so they may be changed at runtime: a change reaches the next motion, not one already under way.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defaults are the phone's</b> (ADR 0006): with nothing set, every motion runs at the time the 8.1 reference
/// measured. An app that wants something else says so here, and only here.
/// </para>
/// <para>
/// A speed is a multiplier on a motion's time: 1 is the phone, 2 twice as fast (a motion of 500 ms takes 250), 0.5
/// half as fast. An area's own speed, when set, is used in place of <see cref="MotionSpeed"/>, not on top of it.
/// Read and set on the UI thread, as <c>CornflowerClock</c> is.
/// </para>
/// <para>
/// Not read, so they keep the phone's time whatever is set here: the ProgressRing's and the ProgressBar's
/// indeterminate loops, the user's only sign that something is happening; the scroll indicators' timers (how long
/// they stay after a scroll), as scrolling itself is Avalonia's and doesn't read this either; and the input timings
/// (a ToggleSwitch's hold, a swipe's speed, the LoopingSelector's flick), which are the finger's, not a motion's.
/// The keyboard's pan has a time of its own instead (<see cref="KeyboardPanSlide"/>), which only <see cref="Animated"/>
/// overrides.
/// </para>
/// </remarks>
public static class CornflowerConfig
{
    /// <summary>
    /// The speed <see cref="Animated"/> off gives every motion that reads one: so fast that a motion ends within a few
    /// frames, one for each of its phases. A motion that would show its start for a frame first doesn't start at all.
    /// </summary>
    internal const double NoMotion = 1_000_000;

    private static bool _animated = true;
    private static double _motionSpeed = 1;
    private static double? _popupSpeed;
    private static double? _pageSpeed;
    private static double? _tiltSpeed;
    private static double? _pivotAndHubSpeed;
    private static double? _controlSpeed;
    private static TimeSpan _keyboardPanSlide = PhoneKeyboardPanSlide;

    /// <summary>
    /// Whether Cornflower's motions move. Default <see langword="true"/>. Off, each motion that reads it is at its end
    /// at once (within a frame or two): a popup or a page is there, a Pivot or Hub is at its section, the tilt
    /// doesn't tilt.
    /// </summary>
    /// <remarks>
    /// Not a master switch. The ProgressRing's and the ProgressBar's indeterminate loops don't read it and keep moving
    /// with it off: they're the user's only sign that something is happening. Nor do the scroll indicators' timers, or
    /// Avalonia's own scrolling. Every other motion on Cornflower's clock reads it, through the speeds below; the
    /// theme's Avalonia transitions (the ToggleSwitch's knob, the ListView's check boxes, the Pivot's headers) go to
    /// their end at once. With it off the tilt doesn't tilt at all, as a press shows no motion.
    /// </remarks>
    public static bool Animated
    {
        get => _animated;
        set
        {
            _animated = value;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// How fast Cornflower's motions run, as a multiplier of the phone's time: 1, the default, is the phone; 2 is
    /// twice as fast. An area's own speed (<see cref="PopupSpeed"/>, <see cref="PageSpeed"/>, <see cref="TiltSpeed"/>,
    /// <see cref="PivotAndHubSpeed"/>, <see cref="ControlSpeed"/>), when set, is used instead.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not a finite number above 0.</exception>
    public static double MotionSpeed
    {
        get => _motionSpeed;
        set
        {
            _motionSpeed = Checked(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The popups' speed, or null, the default, for <see cref="MotionSpeed"/>: the open and close of the Flyout, the
    /// MenuFlyout, the pickers' flyouts (ListPickerFlyout, PickerFlyout, DatePickerFlyout, TimePickerFlyout), the
    /// ContentDialog and the AutoSuggestBox's suggestion list, all on the popup host.
    /// </summary>
    /// <remarks>Read when a popup starts to open, and again when it starts to close.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Not null or a finite number above 0.</exception>
    public static double? PopupSpeed
    {
        get => _popupSpeed;
        set
        {
            _popupSpeed = CheckedOrNull(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The page transitions' speed, or null, the default, for <see cref="MotionSpeed"/>: the turnstile, the slide and
    /// the rest of the theme's <c>IPageTransition</c>s.
    /// </summary>
    /// <remarks>Read when a transition starts.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Not null or a finite number above 0.</exception>
    public static double? PageSpeed
    {
        get => _pageSpeed;
        set
        {
            _pageSpeed = CheckedOrNull(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The tilt's speed, or null, the default, for <see cref="MotionSpeed"/>: its hold and return after the finger
    /// lets go. A press jumps to its tilt on the phone, so a press isn't timed.
    /// </summary>
    /// <remarks>Read when the finger lets go.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Not null or a finite number above 0.</exception>
    public static double? TiltSpeed
    {
        get => _tiltSpeed;
        set
        {
            _tiltSpeed = CheckedOrNull(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The Pivot's and the Hub's speed, or null, the default, for <see cref="MotionSpeed"/>: the Pivot's move to an
    /// item and its headers' fade and slide; the Hub's move to a section, its loop, its entrance turn and its
    /// background's fade in.
    /// </summary>
    /// <remarks>
    /// Read when a move, the turn or the fade starts; the Pivot's header transitions, Avalonia's, take it when the
    /// header is placed and whenever it is set.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Not null or a finite number above 0.</exception>
    public static double? PivotAndHubSpeed
    {
        get => _pivotAndHubSpeed;
        set
        {
            _pivotAndHubSpeed = CheckedOrNull(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The other controls' motions' speed, or null, the default, for <see cref="MotionSpeed"/>: the AppBar's and the
    /// CommandBar's open and close, the LoopingSelector's move and its wheels' expansion, the ToggleSwitch's knob, the
    /// ListView's check boxes sliding in, the inline ComboBox's open, and the scroll indicators' fade.
    /// </summary>
    /// <remarks>
    /// Read when each motion starts; the ToggleSwitch's knob and the ListView's check boxes, Avalonia's transitions, take
    /// it when they are placed and whenever it is set.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Not null or a finite number above 0.</exception>
    public static double? ControlSpeed
    {
        get => _controlSpeed;
        set
        {
            _controlSpeed = CheckedOrNull(value);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// How long the page takes to slide up when the keyboard comes up over a focused text box (the keyboard's pan): 300
    /// ms by default, the phone's (#205). It slides back down in two thirds of it (the phone's 200 ms). Zero or less, and
    /// the page jumps, up and down, with no slide.
    /// </summary>
    /// <remarks>
    /// Read when a slide starts. A time of its own, so neither <see cref="MotionSpeed"/> nor an area's speed scales it;
    /// with <see cref="Animated"/> off the page jumps whatever it is.
    /// </remarks>
    public static TimeSpan KeyboardPanSlide
    {
        get => _keyboardPanSlide;
        set
        {
            _keyboardPanSlide = value;
            Changed?.Invoke();
        }
    }

    /// <summary>The phone's <see cref="KeyboardPanSlide"/>: the page's slide up, about 0.3 s in #205's recording.</summary>
    internal static TimeSpan PhoneKeyboardPanSlide => TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Named departures from the phone, each off by default, so the phone's behaviour stays the default (ADR 0006).
    /// Each is recorded in its control's README, under "Departures".
    /// </summary>
    public static class Deviations
    {
        /// <summary>
        /// The MenuFlyout opens in one step: its width and height grow together, over the 380 ms the phone's width takes,
        /// in place of the phone's two (the width over 380 ms, then the height until 500 ms). Default
        /// <see langword="false"/>, the phone's.
        /// </summary>
        /// <remarks>Read when the menu starts to open. It runs at <see cref="PopupSpeed"/> as the phone's open does.</remarks>
        public static bool MenuFlyoutSingleStepOpen { get; set; }
    }

    /// <summary>
    /// The speed a motion of <paramref name="area"/> runs at now: the area's own, else <see cref="MotionSpeed"/>; with
    /// <see cref="Animated"/> off, <see cref="NoMotion"/>.
    /// </summary>
    internal static double SpeedOf(MotionArea area)
    {
        if (!_animated)
            return NoMotion;
        var own = area switch
        {
            MotionArea.Popup => _popupSpeed,
            MotionArea.Page => _pageSpeed,
            MotionArea.Tilt => _tiltSpeed,
            MotionArea.PivotAndHub => _pivotAndHubSpeed,
            MotionArea.Control => _controlSpeed,
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, null),
        };
        return own ?? _motionSpeed;
    }

    /// <summary>Every setting back to the phone's: the defaults, the deviations off.</summary>
    public static void Reset()
    {
        _animated = true;
        _motionSpeed = 1;
        _popupSpeed = _pageSpeed = _tiltSpeed = _pivotAndHubSpeed = _controlSpeed = null;
        _keyboardPanSlide = PhoneKeyboardPanSlide;
        Deviations.MenuFlyoutSingleStepOpen = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Raised when <see cref="Animated"/> or a speed is set, on the thread that set it: for what can't read the speed
    /// as its motion starts, the theme's Avalonia transitions, whose durations are set again then.
    /// </summary>
    internal static event Action? Changed;

    private static double Checked(double speed, string name = "value") =>
        double.IsFinite(speed) && speed > 0
            ? speed
            : throw new ArgumentOutOfRangeException(name, speed, "A speed is a finite number above 0: 1 is the phone's.");

    private static double? CheckedOrNull(double? speed) => speed is { } value ? Checked(value) : null;
}

/// <summary>The groups of motions that <see cref="CornflowerConfig"/> gives a speed of their own.</summary>
internal enum MotionArea
{
    /// <summary><see cref="CornflowerConfig.PopupSpeed"/>.</summary>
    Popup,

    /// <summary><see cref="CornflowerConfig.PageSpeed"/>.</summary>
    Page,

    /// <summary><see cref="CornflowerConfig.TiltSpeed"/>.</summary>
    Tilt,

    /// <summary><see cref="CornflowerConfig.PivotAndHubSpeed"/>.</summary>
    PivotAndHub,

    /// <summary><see cref="CornflowerConfig.ControlSpeed"/>.</summary>
    Control,
}
