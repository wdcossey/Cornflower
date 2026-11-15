# drive.ps1's pure helpers: no window, no input and no Win32, so selftest.ps1 checks them on any OS. drive.ps1 dot-sources
# this file. See README.md here.

# Where drive.ps1 aims on a control, in view pixels from its bounds, read from the Wp81 templates. tests/Cornflower.Tests/
# Tools/RealWindowTests.cs reads these lines and checks each against the templates as they are laid out, so a template that
# moves a part fails a test rather than a run.

# ToggleSwitch (Controls/ToggleSwitch.axaml): the switch's column at the control's right, the knob's width, the row below
# the switch and the switch's height. The knob travels the column less its width.
$SwitchColumn = 77
$SwitchKnob = 20.5
$SwitchBelow = 9.5
$SwitchHeight = 36
# How far drive.ps1's presses on the knob move: a tap that moves (under PhoneToggleSwitch.TapDistance, 17.85), a drag short
# of half the travel, and a drag past it. RealWindowTests checks them against TapDistance itself.
$SwitchTapMove = 10
$SwitchShortDrag = 24
$SwitchLongDrag = 45
# A point this far in from the switch's top left corner, on its header, which is the switch's but not its thumb's.
$SwitchHeaderIn = 10

# Slider (Controls/Slider.axaml): the track's middle above the Slider's bottom (the 30.5 row below it and half its 9),
# and the thumb's width, which the thumb's middle travels the track less.
$SliderTrackAbove = 35
$SliderThumb = 9.5

# A check's line, counted in the script that dot-sources this: drive.ps1 and selftest.ps1 print the same.
function Check($name, [bool]$ok, $detail = '') {
    $script:checks++
    if ($ok) { "  ok    $name" } else { $script:failures++; "  FAIL  $name $detail" }
}

# A section runs when its title is like -Only, as -like matches, or like one of its comma-separated parts: so a title with a
# comma in it is still selected by itself, which selftest.ps1 checks for each.
function Selected([string]$title, [string]$only) {
    ($title -like $only) -or [bool]@($only -split ',' | Where-Object { $_.Trim() -and $title -like $_.Trim() })
}

# The ready line's fields:
#   CORNFLOWER ready open=<ms>ms scaling=... view=WxH raw=WxH safe=l,t,r,b page=WxH title=<title>
# The page's own size, which a shell's chrome would take from; the window's client area is view=.
function PageOf($line) { if ($line -match ' page=([\d.]+)x([\d.]+)') { [double]$Matches[1], [double]$Matches[2] } }
function ScalingOf($line) { if ($line -match 'scaling=([\d.]+) ') { [double]$Matches[1] } else { 1 } }
# The page the line is for: last on the line, as a title can have spaces.
function TitleOf($line) { if ($line -match ' title=(.+)$') { $Matches[1].Trim() } }
# Windows gives a window whole pixels, so at 1.25 a 450 view is 562 pixels, 449.6: within a pixel is the size.
function WithinAPixel($view, $want, $scaling) { [math]::Abs($view - $want) * $scaling -le 1 }
# A title as a -like pattern that matches only itself.
function Literal($text) { [Management.Automation.WildcardPattern]::Escape($text) }

# Whether a point is inside a rectangle, its edges excluded, so that a point on the window's frame is outside it.
function Inside([double]$left, [double]$top, [double]$right, [double]$bottom, [double]$x, [double]$y) { $x -gt $left -and $x -lt $right -and $y -gt $top -and $y -lt $bottom }

# The points a drag passes through after its first, in whole pixels, as X and Y: $steps of them, evenly, the last at
# $x + $dx, $y + $dy.
function DragPath([double]$x, [double]$y, [double]$dx, [double]$dy, [int]$steps) {
    for ($i = 1; $i -le $steps; $i++) {
        [pscustomobject]@{ X = [int][math]::Round($x + $dx * $i / $steps); Y = [int][math]::Round($y + $dy * $i / $steps) }
    }
}

# The knob's middle at its rest, on or off, in screen pixels, from the switch's right and bottom on the screen.
function KnobAt([double]$right, [double]$bottom, [double]$scale, [bool]$on) {
    $fromRight = if ($on) { $SwitchKnob / 2 } else { $SwitchColumn - $SwitchKnob / 2 }
    [int][math]::Round($right - $fromRight * $scale), [int][math]::Round($bottom - ($SwitchBelow + $SwitchHeight / 2) * $scale)
}

# Where a press on a horizontal Slider puts the thumb's middle for a value, in screen pixels, from the Slider's left, width
# and bottom on the screen: Avalonia's Slider moves the thumb's middle to the press.
function SliderAt([double]$left, [double]$width, [double]$bottom, [double]$scale, [double]$value, [double]$minimum = 0, [double]$maximum = 100) {
    $track = $width / $scale - $SliderThumb
    $x = $left + ($SliderThumb / 2 + ($value - $minimum) / ($maximum - $minimum) * $track) * $scale
    [int][math]::Round($x), [int][math]::Round($bottom - $SliderTrackAbove * $scale)
}

# A count the sample shows, such as the RepeatButton page's: a whole number, or nothing.
function CountOf($text) { if ("$text" -match '^-?\d+$') { [int]$text } }

# Whether :pointerover was set under a held touch, from one pixel of a control that draws a brush only while the pointer is
# over it: at rest, under the mouse (which has to differ, or the pixel shows nothing), and under the touch; and whether
# the cursor was on the control then, as Windows can make a mouse move of a touch, and the mouse would set it too.
function HoverVerdict($rest, $hover, $touched, [bool]$cursorOn) {
    if ($hover -eq $rest) { 'unsettled' }
    elseif ($touched -eq $rest) { 'not set' }
    elseif ($cursorOn) { 'set, but the cursor was on it, so perhaps by the mouse' }
    else { 'set' }
}
