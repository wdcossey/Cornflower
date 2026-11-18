using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone shell's pan: while the keyboard is up, the window's content moves up so the focused text box is not under
/// it, and comes back down when the keyboard goes (#179). Turned on by the TextBox and PasswordBox themes' own setter,
/// and so for the AutoSuggestBox's TextBox part too. Avalonia shows the keyboard on focus and says what it covers
/// (<c>TopLevel.InputPane</c>, read through <see cref="PhoneSuggestionList.KeyboardOf"/>), but moves nothing, and Android
/// neither pans nor resizes an Avalonia view.
/// </summary>
/// <remarks>
/// <para><b>How far.</b> An AutoSuggestBox goes up until the control's top (its header's) is at the window's top, inside
/// its safe area, even when the keyboard didn't cover it: #150 read 21.67 up from 48.33, #200 312.08 up from 300 down
/// (<see cref="AutoCompleteBoxOptions.AutoMaximizeSuggestionAreaProperty"/>, true by default). A TextBox or PasswordBox,
/// goes up only as far as it must for the caret's line to clear the keyboard's top, its bottom border left under the
/// keyboard, as #200 read (452.92 against 453.33); an AutoSuggestBox with that false, until the TextBox part's bottom
/// border is at the keyboard's top, and <see cref="ListAboveLift"/> (10.42) more while its list is open, which then
/// opens above the box (#205: 184.58, then 195). No further than puts the control's own top at the window's top; one
/// the keyboard leaves room for stays.</para>
/// <para><b>What moves.</b> The window's content, all of it, through a <see cref="TranslateTransform"/> at animation
/// priority on the window's presenter of it, so the content's own transform (an app's, a page's motion) is left alone:
/// a box in a <see cref="ScrollViewer"/> moves with the page, not by scrolling, as a scroll can't take a box at the end
/// of its content up. A box in a popup (a ContentDialog's) moves nothing: #150 saw a focused TextBox's dialog stay at the
/// top. The moved content is cut at the safe area's top, so it goes under the status bar unseen (the phone's system tray
/// stays as the page slides under it), and what the move leaves bare, above and below, shows the page's background
/// (<c>ApplicationPageBackgroundThemeBrush</c>) rather than the window's own. The safe area, the popups and the keyboard
/// stay where they are; the AutoSuggestBox's list is hung from its box
/// again as the page moves (<see cref="Moved"/>).</para>
/// <para><b>When.</b> As the keyboard comes up, changes or goes (worked out afresh), as the focus comes to or leaves a
/// box (the least change from where the page is), as an AutoSuggestBox's list opens or closes, and as the box the page
/// moved for leaves the window.</para>
/// <para><b>The slide</b> (#205's recording, about 30 frames a second). The page slides on <see cref="CornflowerClock"/>
/// from where it is to where it is going, starting as the keyboard does: up over
/// <see cref="CornflowerConfig.KeyboardPanSlide"/> (300 ms, the phone's) on an exponential ease-out
/// (<see cref="PhoneEase"/>, exponent <see cref="UpExponent"/>), down over two thirds of it (200 ms) on an ease-in
/// (<see cref="DownExponent"/>); the exponents are fitted to the recording's frames, which are too far apart to say
/// more. That value at 0 or less, or <see cref="CornflowerConfig.Animated"/> off, and the page jumps. What is worked out
/// from the box's place reads where the page will end (<see cref="StillToGo"/>), not where it is mid-slide.</para>
/// <para><b>Back and the keyboard.</b> On the phone the first Back hides the keyboard and takes the focus from the box,
/// a dialog it is in staying; the next Back goes on (#150). On Android the keyboard takes that Back itself and Avalonia
/// never sees it (#197: on the Pixel the page neither went back nor lost the box's focus), so the keyboard going down
/// while a box of ours has the focus takes the focus. A Back Avalonia does see while one has it (no keyboard up: a
/// hardware one, a desktop) does the same through the phone popups' Back handler
/// (<see cref="PhonePopupHost.TakeBackFirst"/>), before any popup, and is taken. An AutoSuggestBox's TextBox part is
/// one of ours, so this covers it too, as <see cref="PhoneSuggestionList"/> already did (closing its list).</para>
/// </remarks>
internal static class PhoneKeyboardPan
{
    /// <summary>Set by a theme: while this box has the focus and the keyboard is up, the page moves up to show it.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsEnabled", typeof(PhoneKeyboardPan));

    private static readonly AttachedProperty<Pan?> PanProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, Pan?>("Pan", typeof(PhoneKeyboardPan));

    static PhoneKeyboardPan()
    {
        // The focus coming: once the press that gave it has put the caret where it was tapped.
        InputElement.GotFocusEvent.AddClassHandler<TextBox>((box, _) => PanOf(box)?.UpdateSoon());
        // The focus leaving: once it has settled, as it may be on its way to another box.
        InputElement.LostFocusEvent.AddClassHandler<TextBox>((box, _) => PanOf(box)?.UpdateSoon());
        // An AutoSuggestBox's list opening or closing: one not maximized goes ListAboveLift further while it is open.
        AutoCompleteBox.IsDropDownOpenProperty.Changed.AddClassHandler<AutoCompleteBox>((box, _) =>
            TopLevel.GetTopLevel(box)?.GetValue(PanProperty)?.UpdateSoon());
        PhonePopupHost.TakeBackFirst(TakesBack);
    }

    /// <summary>The page's background, shown where the move leaves the window bare.</summary>
    internal const string PageBackground = "ApplicationPageBackgroundThemeBrush";

    /// <summary>Far enough, for the clip's other three sides.</summary>
    private const double Reach = 1e6;

    /// <summary>
    /// How much further the page goes for an AutoSuggestBox not maximized while its list is open (#205: the box's bottom
    /// 10.42 above the keyboard, 195 against 184.58). Why is unread (the text and the list came together): it is taken
    /// as the list's.
    /// </summary>
    internal const double ListAboveLift = 10.42;

    /// <summary>The slide up's ease-out exponent, fitted to #205's frames (0.23, 0.42, 0.58, ... of the move).</summary>
    internal const double UpExponent = 2;

    /// <summary>The slide down's ease-in exponent, fitted to #205's frames (183.33, 170.83, 160, 144.17, ... 0).</summary>
    internal const double DownExponent = 3.5;

    /// <summary>The slide down's time, as a share of the slide up's (#205: about 0.2 s against 0.3).</summary>
    internal const double DownShare = 2.0 / 3;

    /// <summary>Raised with the window as its content moves, after it has.</summary>
    internal static event Action<TopLevel>? Moved;

    public static bool GetIsEnabled(TextBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    /// <summary>How far the window's content is moved up once it has slid there (0 when it isn't).</summary>
    internal static double ShiftOf(TopLevel top) => top.GetValue(PanProperty)?.Shift ?? 0;

    /// <summary>How far the window's content is moved up now, mid-slide where it is sliding.</summary>
    internal static double ShownShiftOf(TopLevel top) => top.GetValue(PanProperty)?.Shown ?? 0;

    /// <summary>
    /// How much further up <paramref name="visual"/> goes before the slide ends: for a visual the pan moves, the rest of
    /// the slide (down, negative); 0 for one it doesn't (a popup's) and at rest.
    /// </summary>
    internal static double StillToGo(TopLevel top, Visual visual) => top.GetValue(PanProperty)?.StillToGo(visual) ?? 0;

    /// <summary>
    /// Back while a box of ours has the focus, in the topmost popup if one is open: take its focus; Back is taken. A box
    /// under an open popup leaves Back to the popup.
    /// </summary>
    private static bool TakesBack(TopLevel top, PhonePopupHost? over)
    {
        if (BoxWithFocus(top) is not { } box || (over is not null && !over.Holds(box)))
            return false;
        LetGo(top);
        return true;
    }

    /// <summary>The text box of ours that has the focus in the top level, if one has (an AutoSuggestBox's part included).</summary>
    private static TextBox? BoxWithFocus(TopLevel top) =>
        top.FocusManager?.GetFocusedElement() is TextBox box && GetIsEnabled(box) ? box : null;

    /// <summary>What the phone does on Back or when its keyboard goes: the box loses the focus.</summary>
    private static void LetGo(TopLevel top) => top.FocusManager?.Focus(null);

    /// <summary>The window's pan, made the first time a box of ours in it has the focus.</summary>
    private static Pan? PanOf(TextBox box)
    {
        // A popup of its own (a desktop's, not the phone's overlay) is a window the page's move can't take.
        if (!GetIsEnabled(box) || TopLevel.GetTopLevel(box) is not { } top || top is PopupRoot)
            return null;
        if (top.GetValue(PanProperty) is not { } pan)
            top.SetValue(PanProperty, pan = new Pan(top));
        return pan;
    }

    /// <summary>One window's pan: its keyboard, what it moves and how far.</summary>
    private sealed class Pan
    {
        private readonly TopLevel _top;
        private readonly TranslateTransform _transform = new();
        private readonly RectangleGeometry _clip = new();
        private PhoneSuggestionList.IPhoneKeyboard? _keyboard;
        private Visual? _moved;
        private IDisposable? _transformed;
        private IDisposable? _clipped;
        private IDisposable? _backed;
        private TextBox? _for;
        private bool _keyboardWasOpen;
        private IDisposable? _frame;
        private double _from;
        private TimeSpan _start;
        private TimeSpan _length;
        private bool _up;

        public Pan(TopLevel top) => _top = top;

        /// <summary>Where the page is going: how far up it is once the slide ends.</summary>
        public double Shift { get; private set; }

        /// <summary>How far up the page is now.</summary>
        public double Shown { get; private set; }

        public double StillToGo(Visual visual) => _moved?.IsVisualAncestorOf(visual) == true ? Shift - Shown : 0;

        public void UpdateSoon() => Dispatcher.UIThread.Post(() => Update(keyboardChanged: false));

        /// <param name="keyboardChanged">
        /// The keyboard came, went or changed: the move is worked out afresh, the least it can be. Otherwise (the focus
        /// moved) it is the least change from where the page is, so a box already in view leaves the page where it is.
        /// </param>
        public void Update(bool keyboardChanged)
        {
            // Asked for again until the window has one (an input pane not there yet at the first focus).
            if (_keyboard is null && (_keyboard = PhoneSuggestionList.KeyboardOf(_top)) is not null)
            {
                _keyboardWasOpen = _keyboard.IsOpen;
                _keyboard.Changed += (_, _) => OnKeyboardChanged();
            }
            var mover = Mover();
            var (shift, box) = Wanted(mover, keyboardChanged);
            Follow(shift > 0 ? box : null);
            if (shift == Shift && (shift == 0 || mover == _moved))
                return;
            Shift = shift;
            if (mover is null || (mover != _moved && shift == 0))
                TakeBack();
            else
            {
                if (mover != _moved)
                {
                    // A new content: the old one is given back at once, and the new one slides from unmoved.
                    TakeBack();
                    _transform.Y = 0;
                    _clip.Rect = ClipFor(mover, 0);
                    _transformed = mover.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
                    _clipped = mover.SetValue(Visual.ClipProperty, _clip, BindingPriority.Animation);
                    // What the move leaves bare below the page, and above it under the safe area's top: the page's
                    // background, not the window's own (Simple's grey).
                    _backed = _top.Bind(TemplatedControl.BackgroundProperty,
                        _top.GetResourceObservable(PageBackground), BindingPriority.Animation);
                    _moved = mover;
                }
                Slide();
            }
            Moved?.Invoke(_top);
        }

        /// <summary>
        /// Starts the slide from where the page is now to <see cref="Shift"/>, up or down, its time read from the config
        /// now; or puts it there at once, the config's slide 0 or less or <see cref="CornflowerConfig.Animated"/> off.
        /// </summary>
        private void Slide()
        {
            var up = CornflowerConfig.KeyboardPanSlide;
            if (!CornflowerConfig.Animated || up <= TimeSpan.Zero)
            {
                Show(Shift);
                return;
            }
            _from = Shown;
            _up = Shift > Shown;
            _length = _up ? up : up * DownShare;
            _start = CornflowerClock.Now;
            _frame ??= CornflowerClock.RequestFrame(_top, Step);
        }

        /// <summary>A frame of the slide: the page where the ease has it now, or at its end.</summary>
        private void Step()
        {
            _frame = null;
            if (_moved is null)
                return;
            var progress = (CornflowerClock.Now - _start) / _length;
            if (progress >= 1)
                Show(Shift);
            else
            {
                var eased = _up ? PhoneEase.ExponentialOut(UpExponent, progress) : PhoneEase.ExponentialIn(DownExponent, progress);
                Show(_from + (Shift - _from) * eased);
                _frame = CornflowerClock.RequestFrame(_top, Step);
            }
            Moved?.Invoke(_top);
        }

        /// <summary>The page <paramref name="shift"/> up; given back once it is at rest unmoved.</summary>
        private void Show(double shift)
        {
            if (shift <= 0 && Shift <= 0)
            {
                TakeBack();
                return;
            }
            if (_moved is null)
                return;
            Shown = shift;
            _transform.Y = -shift;
            _clip.Rect = ClipFor(_moved, shift);
        }

        /// <summary>
        /// The keyboard going down while a box of ours has the focus takes it, as the phone's Back does. Not a read-only
        /// box's: it asks for no keyboard, so the keyboard going as the focus comes to it is the last box's.
        /// </summary>
        private void OnKeyboardChanged()
        {
            var open = _keyboard?.IsOpen == true;
            var went = _keyboardWasOpen && !open;
            _keyboardWasOpen = open;
            if (went && BoxWithFocus(_top) is { IsReadOnly: false })
                LetGo(_top);
            Update(keyboardChanged: true);
        }

        private void TakeBack()
        {
            _frame?.Dispose();
            _transformed?.Dispose();
            _clipped?.Dispose();
            _backed?.Dispose();
            _frame = _transformed = _clipped = _backed = null;
            _moved = null;
            Shown = 0;
        }

        /// <summary>
        /// The moved content cut at the safe area's top, so that it goes under the status bar unseen, as the phone's page
        /// slides under its system tray, which stays. In the mover's own coordinates, which the move takes with it.
        /// </summary>
        private Rect ClipFor(Visual mover, double shift)
        {
            var at = mover.GetVisualParent() is { } parent && parent.TranslatePoint(mover.Bounds.Position, _top) is { } p ? p.Y : 0;
            var top = PhonePopupHost.AreaOf(_top).Top + shift - at;
            return new Rect(-Reach, top, 2 * Reach, Reach);
        }

        /// <summary>
        /// What moves: the window's presenter of its content, so the content's own transform is left alone and a new
        /// content is moved as the old was; the content itself where the window has no presenter.
        /// </summary>
        private Visual? Mover() => _top.Content is not Visual content
            ? null
            : content.GetVisualParent() is ContentPresenter presenter && presenter.TemplatedParent == _top ? presenter : content;

        /// <summary>The box the page is moved for: should it leave the window (focused all the while), the page comes back.</summary>
        private void Follow(TextBox? box)
        {
            if (box == _for)
                return;
            if (_for is not null)
                _for.DetachedFromVisualTree -= OnGone;
            _for = box;
            if (_for is not null)
                _for.DetachedFromVisualTree += OnGone;
        }

        private void OnGone(object? sender, VisualTreeAttachmentEventArgs e) => UpdateSoon();

        /// <summary>How far to move up for the box that has the focus, if one of ours has, and that box.</summary>
        private (double Shift, TextBox? Box) Wanted(Visual? mover, bool keyboardChanged)
        {
            if (_keyboard is not { IsOpen: true } keyboard
                || _top.FocusManager?.GetFocusedElement() is not TextBox box || !GetIsEnabled(box)
                || mover is null || !mover.IsVisualAncestorOf(box))
                return (0, null);
            // What the keyboard covers of the window: on Windows every window has a pane (the touch keyboard), whose place
            // may be beside or below the window (as Avonic's KeyboardAssist.CoveredHeight).
            var covered = keyboard.Occluded.Intersect(new Rect(_top.ClientSize));
            if (covered.Height <= 0)
                return (0, null);
            Visual target = PhoneSuggestionList.BoxWithFocus(_top) is { } suggest && suggest.IsVisualAncestorOf(box) ? suggest : box;
            if (target.TranslatePoint(default, _top) is not { } at)
                return (0, null);
            // Where it would be were nothing moved: the page as it is now, mid-slide.
            var moved = _moved?.IsVisualAncestorOf(target) == true ? Shown : 0;
            var top = at.Y + moved;
            var area = PhonePopupHost.AreaOf(_top);
            if (target is AutoCompleteBox suggestBox && AutoCompleteBoxOptions.GetAutoMaximizeSuggestionArea(suggestBox))
                return (Math.Max(0, top - area.Top), box);
            // The least move that shows the caret above the keyboard (#200's read): its line's bottom at the keyboard's
            // top, the box's own bottom left under it. An AutoSuggestBox not maximized, its whole box (#205's read), and
            // ListAboveLift more while its list is open, the list then above it. Never so far that the control's top
            // goes past the safe area's.
            var needed = target is AutoCompleteBox suggesting
                ? BottomOf(box, _top) + (suggesting.IsDropDownOpen ? ListAboveLift : 0)
                : CaretBottomOf(box, _top);
            if (needed is null)
                return (0, null);
            var bottom = needed.Value + moved;
            var shift = keyboardChanged ? 0 : Shift;
            if (bottom - shift > covered.Top)
                shift = bottom - covered.Top;
            if (top - shift < area.Top)
                shift = top - area.Top;
            return (Math.Max(0, shift), box);
        }
    }

    /// <summary>The bottom of <paramref name="box"/>'s border in <paramref name="to"/>, as the box is placed now.</summary>
    private static double? BottomOf(TextBox box, Visual to) => box.TranslatePoint(new Point(0, box.Bounds.Height), to)?.Y;

    /// <summary>
    /// The bottom of the caret's line in <paramref name="to"/>, as the box is placed now: for a box of many lines, the
    /// line the caret is on. The box's bottom where its template has no text presenter.
    /// </summary>
    internal static double? CaretBottomOf(TextBox box, Visual to)
    {
        if (box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault(p => p.TemplatedParent == box) is not { } presenter)
            return box.TranslatePoint(new Point(0, box.Bounds.Height), to)?.Y;
        var line = presenter.TextLayout.HitTestTextPosition(presenter.CaretIndex);
        return presenter.TranslatePoint(line.BottomLeft, to)?.Y;
    }
}
