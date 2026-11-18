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
/// and an AutoSuggestBox with that false, goes up only as far as it must for the caret's line to clear the keyboard's
/// top, its bottom border left under the keyboard, as #200 read (452.92 against 453.33), and no further than puts the
/// control's own top at the window's top; one whose caret the keyboard doesn't cover stays.</para>
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
/// box (the least change from where the page is), and as the box the page moved for leaves the window. The move is at
/// once, not a slide: whether the phone's slides, and how fast, is unmeasured.</para>
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
        InputElement.GotFocusEvent.AddClassHandler<TextBox>((box, _) => PanOf(box)?.Update(keyboardChanged: false));
        // The focus leaving: once it has settled, as it may be on its way to another box.
        InputElement.LostFocusEvent.AddClassHandler<TextBox>((box, _) => PanOf(box)?.UpdateSoon());
    }

    /// <summary>The page's background, shown where the move leaves the window bare.</summary>
    internal const string PageBackground = "ApplicationPageBackgroundThemeBrush";

    /// <summary>Far enough, for the clip's other three sides.</summary>
    private const double Reach = 1e6;

    /// <summary>Raised with the window as its content moves, after it has.</summary>
    internal static event Action<TopLevel>? Moved;

    public static bool GetIsEnabled(TextBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    /// <summary>How far the window's content is moved up now (0 when it isn't).</summary>
    internal static double ShiftOf(TopLevel top) => top.GetValue(PanProperty)?.Shift ?? 0;

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
        private IDisposable? _shown;
        private IDisposable? _clipped;
        private IDisposable? _backed;
        private TextBox? _for;

        public Pan(TopLevel top) => _top = top;

        public double Shift { get; private set; }

        public void UpdateSoon() => Dispatcher.UIThread.Post(() => Update(keyboardChanged: false));

        /// <param name="keyboardChanged">
        /// The keyboard came, went or changed: the move is worked out afresh, the least it can be. Otherwise (the focus
        /// moved) it is the least change from where the page is, so a box already in view leaves the page where it is.
        /// </param>
        public void Update(bool keyboardChanged)
        {
            // Asked for again until the window has one (an input pane not there yet at the first focus).
            if (_keyboard is null && (_keyboard = PhoneSuggestionList.KeyboardOf(_top)) is not null)
                _keyboard.Changed += (_, _) => Update(keyboardChanged: true);
            var mover = Mover();
            var (shift, box) = Wanted(mover, keyboardChanged);
            Follow(shift > 0 ? box : null);
            if (shift == Shift && (shift == 0 || mover == _moved))
                return;
            Shift = shift;
            if (shift > 0 && mover is not null)
            {
                _transform.Y = -shift;
                _clip.Rect = ClipFor(mover, shift);
                if (mover != _moved)
                {
                    TakeBack();
                    _shown = mover.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
                    _clipped = mover.SetValue(Visual.ClipProperty, _clip, BindingPriority.Animation);
                    // What the move leaves bare below the page, and above it under the safe area's top: the page's
                    // background, not the window's own (Simple's grey).
                    _backed = _top.Bind(TemplatedControl.BackgroundProperty,
                        _top.GetResourceObservable(PageBackground), BindingPriority.Animation);
                    _moved = mover;
                }
            }
            else
                TakeBack();
            Moved?.Invoke(_top);
        }

        private void TakeBack()
        {
            _shown?.Dispose();
            _clipped?.Dispose();
            _backed?.Dispose();
            _shown = _clipped = _backed = null;
            _moved = null;
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
            // Where it would be were nothing moved.
            var moved = _moved?.IsVisualAncestorOf(target) == true ? Shift : 0;
            var top = at.Y + moved;
            var area = PhonePopupHost.AreaOf(_top);
            if (target is AutoCompleteBox suggestBox && AutoCompleteBoxOptions.GetAutoMaximizeSuggestionArea(suggestBox))
                return (Math.Max(0, top - area.Top), box);
            // The least move that shows the caret above the keyboard (#200's read): its line's bottom at the keyboard's
            // top, the box's own bottom left under it; never so far that the control's top goes past the safe area's.
            if (CaretBottomOf(box, _top) is not { } caret)
                return (0, null);
            var bottom = caret + moved;
            var shift = keyboardChanged ? 0 : Shift;
            if (bottom - shift > covered.Top)
                shift = bottom - covered.Top;
            if (top - shift < area.Top)
                shift = top - area.Top;
            return (Math.Max(0, shift), box);
        }
    }

    /// <summary>
    /// The bottom of the caret's line in the window, as the box is placed now: for a box of many lines, the line the
    /// caret is on. No lower than the box's own bottom, as a line scrolled out of the box isn't shown; the box's bottom
    /// where its template has no text presenter.
    /// </summary>
    internal static double? CaretBottomOf(TextBox box, Visual to)
    {
        if (box.TranslatePoint(new Point(0, box.Bounds.Height), to) is not { } boxBottom)
            return null;
        if (box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault(p => p.TemplatedParent == box) is not { } presenter)
            return boxBottom.Y;
        var line = presenter.TextLayout.HitTestTextPosition(presenter.CaretIndex);
        return presenter.TranslatePoint(line.BottomLeft, to) is { } caret ? Math.Min(caret.Y, boxBottom.Y) : boxBottom.Y;
    }
}
