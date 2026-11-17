# Opens the real desktop sample window and uses it with real mouse and keyboard input, and touch through Windows'
# touch injection. Checks what it can, saves a frame of every page, and exits 1 if a check fails. See README.md here.
#
# It takes over the cursor and focus for a few seconds a page, so ask before running it on someone's PC.
# Windows only, by nature: Win32 input, UI Automation and the screen. PowerShell 7 or Windows PowerShell 5.1; both
# have run it end to end (see README.md):
#   pwsh -NoProfile -File tools/real-window/drive.ps1
#   powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
#
# Ported from Avonic's tools/real-window/drive.ps1 (wdcossey/Avonic, v2 at edf87b8): the window, the input,
# the frames and the every-page loop; then, for #115, from v2 at 57bdd3d: touch, drags, the wheel, pixel reads and
# -Only. Avonic's per-control sections come back here as Cornflower's controls do. The pure helpers are in
# helpers.ps1, which selftest.ps1 checks.
param(
    # The build to run, as tools/android/run.ps1 takes it. A Debug build also has the performance line to check.
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    # Run the build as it is, without building it first. A stale build runs the old code, and its failures
    # look like the new code's.
    [switch]$NoBuild,
    # Where the frames go. By default a new folder for each run, so that an old frame can't pass for a new one.
    [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) ('cornflower-real-window\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))),
    # Runs only the sections whose title is like one of these, comma-separated, such as 'toggleswitch*,slider*', to
    # work on them. The whole run is the gate.
    [string[]]$Only = '*'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'helpers.ps1')
# From a PowerShell prompt, -Only a*,b* comes as an array; through -File, as one string. Either way, one comma list.
$Only = $Only -join ','
function Step([string]$text) { Write-Host "-- $text" }

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'samples\Cornflower.Sample.Desktop'
$Exe = Join-Path $project "bin\$Configuration\net10.0\Cornflower.Sample.Desktop.exe"
$debugBuild = $Configuration -eq 'Debug'

if (-not $NoBuild) {
    # A sample left open locks its files, and the build would fail with only MSBuild's copy errors to say why.
    $open = @(Get-Process -Name 'Cornflower.Sample.Desktop' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($_.Path -ieq $Exe) })
    if ($open.Count) {
        throw "The $Configuration sample is open (process $($open[0].Id)), which locks its files. Close it, or pass -NoBuild."
    }
    Step "Building the $Configuration desktop sample"
    # From the repo, so that its global.json picks the SDK. 'Continue', as Windows PowerShell 5.1 turns a line
    # dotnet writes to a redirected stderr into a stopping error; the exit code is what counts.
    Push-Location $repo
    $ErrorActionPreference = 'Continue'
    try {
        & dotnet build $project -c $Configuration -nologo -v q
        $built = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = 'Stop'
        Pop-Location
    }
    if ($built -ne 0) { throw "dotnet build failed ($built)." }
    if (-not (Test-Path $Exe)) {
        throw "Built, but there's no sample at $Exe. Has the sample's target framework or output path changed?"
    }
} elseif (-not (Test-Path $Exe)) {
    throw "No sample at $Exe. Build it, or leave out -NoBuild."
}
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$stdout = Join-Path $Out 'stdout.txt'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class RealInput {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out PT p);
  public static int[] Cursor() { PT p; GetCursorPos(out p); return new[] { p.x, p.y }; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
  public static void Down() { mouse_event(2, 0, 0, 0, IntPtr.Zero); }
  public static void Up() { mouse_event(4, 0, 0, 0, IntPtr.Zero); }
  public static void Wheel(int d) { mouse_event(0x800, 0, 0, unchecked((uint)d), IntPtr.Zero); }

  // Touch, through InjectTouchInput: one contact, with no feedback drawn (TOUCH_FEEDBACK_NONE), so frames show only the app.
  [StructLayout(LayoutKind.Sequential)] public struct PT { public int x, y; }
  [StructLayout(LayoutKind.Sequential)] struct RC { public int l, t, r, b; }
  [StructLayout(LayoutKind.Sequential)] struct PI {
    public uint type, id, frame, flags; public IntPtr device, hwnd;
    public PT px, him, pxRaw, himRaw; public uint time, history; public int data; public uint keys;
    public ulong perf; public int change; }
  [StructLayout(LayoutKind.Sequential)] struct TI { public PI p; public uint tf, mask; public RC c, cr; public uint orient, press; }
  [DllImport("user32.dll", SetLastError = true)] static extern bool InitializeTouchInjection(uint n, uint mode);
  [DllImport("user32.dll", SetLastError = true)] static extern bool InjectTouchInput(uint n, ref TI t);
  public static bool TouchInit() { return InitializeTouchInjection(1, 3); }
  // The pointer id has to be below the count given to InitializeTouchInjection.
  static void Send(int x, int y, uint flags) {
    var t = new TI();
    t.p.type = 2; t.p.id = 0; t.p.flags = flags; t.p.px.x = x; t.p.px.y = y;
    t.mask = 7; t.orient = 90; t.press = 32000;
    t.c.l = x - 2; t.c.r = x + 2; t.c.t = y - 2; t.c.b = y + 2;
    if (!InjectTouchInput(1, ref t)) throw new InvalidOperationException("Touch injection failed: " + Marshal.GetLastWin32Error());
  }
  public static void TouchDown(int x, int y) { Send(x, y, 0x10000 | 2 | 4); }
  public static void TouchMove(int x, int y) { Send(x, y, 0x20000 | 2 | 4); }
  public static void TouchUp(int x, int y) { Send(x, y, 0x40000); }
}
'@
# Under PowerShell 7, System.Drawing is a facade that forwards Bitmap to System.Drawing.Common.
$drawing = if ($PSVersionTable.PSEdition -eq 'Core') { 'System.Drawing.Common', 'System.Drawing.Primitives' } else { 'System.Drawing' }
Add-Type -ReferencedAssemblies $drawing @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class Pixels {
  // How many of every other pixel differ between two regions of one size.
  public static int Differ(Bitmap a, Bitmap b) {
    if (a.Width != b.Width || a.Height != b.Height) throw new ArgumentException("The regions differ in size");
    var r = new Rectangle(0, 0, a.Width, a.Height);
    var da = a.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var db = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try {
      int n = 0;
      for (int y = 0; y < a.Height; y += 2)
        for (int x = 0; x < a.Width; x += 2)
          if (Marshal.ReadInt32(da.Scan0, y * da.Stride + x * 4) != Marshal.ReadInt32(db.Scan0, y * db.Stride + x * 4)) n++;
      return n;
    } finally { a.UnlockBits(da); b.UnlockBits(db); }
  }
}
'@
# Per-monitor aware (-4, PER_MONITOR_AWARE_V2), so that UI Automation's rectangles, the cursor and a touch are the screen's
# own pixels on any monitor, as the ready line's scaling is: system aware, a window on a monitor at another scale would be
# scaled by Windows and every Px would miss. A host that has set it already keeps its own; then system aware, as before.
if (-not [RealInput]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))) { [void][RealInput]::SetProcessDPIAware() }
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$SendKeys = [System.Windows.Forms.SendKeys]
$cursor = [System.Windows.Forms.Cursor]::Position
$script:failures = 0
$script:checks = 0
$script:sections = 0
# The page's render scaling, from its ready line: set by Open, read by Px.
$script:scale = 0

# From the screen, not PrintWindow, which offsets a window with no frame of its own by its invisible border.
# A region of the screen, which the caller disposes of, or hands to Changed as the region taken now.
function Region($x, $y, $wd, $ht) {
    $bmp = New-Object Drawing.Bitmap ([int]$wd), ([int]$ht)
    $g = [Drawing.Graphics]::FromImage($bmp)
    try { $g.CopyFromScreen([int]$x, [int]$y, 0, 0, $bmp.Size) } finally { $g.Dispose() }
    $bmp
}
function Shot($name) {
    $r = $win.Current.BoundingRectangle
    $bmp = Region $r.X $r.Y $r.Width $r.Height
    try { $bmp.Save((Join-Path $Out "$name.png")) } finally { $bmp.Dispose() }
}
# One pixel of the screen, as ARGB.
function Pixel($x, $y) {
    $bmp = Region $x $y 1 1
    try { $bmp.GetPixel(0, 0).ToArgb() } finally { $bmp.Dispose() }
}
# How many of every other pixel differ between a region taken earlier, which the caller keeps, and one taken now, which this
# disposes of. A disposed bitmap's size reads as nothing in PowerShell, so a reused one would compare nothing and pass.
function Changed($before, $now) {
    try { [Pixels]::Differ($before, $now) } finally { $now.Dispose() }
}
function All($type) {
    $cond = New-Object Windows.Automation.PropertyCondition $AE::ControlTypeProperty, $type
    $win.FindAll($TS::Descendants, $cond) | ForEach-Object { $_ }
}
# By the exact name, case and all: a page's title is a name, not a pattern.
function Named($type, $name) { All $type | Where-Object { $_.Current.Name -ceq $name } | Select-Object -First 1 }
# A control on the page by its x:Name, which Avalonia gives UI Automation as its AutomationId.
function ById($id) {
    $el = $win.FindFirst($TS::Descendants, (New-Object Windows.Automation.PropertyCondition $AE::AutomationIdProperty, $id))
    if (-not $el) { throw "Nothing on the page has the AutomationId $id" }
    $el
}
# A TextBlock on the page that a section reads, by its x:Name: UI Automation's name for it is its text, read live from
# .Current.Name. ById by another name, so that RealWindowTests can check each is a TextBlock; a section finds it once, as
# a search of the whole tree takes long enough to shift a timed read.
function ReadOut($id) { ById $id }
function Texts { (All $CT::Text | ForEach-Object { $_.Current.Name } | Where-Object { $_ }) }
function FirstText($like) { Texts | Where-Object { $_ -like $like } | Select-Object -First 1 }
# A scroll bar's own buttons, which have no size at its ends and aren't the app's.
$scrollParts = 'Line up', 'Page up', 'Page down', 'Line down', 'Line left', 'Page left', 'Page right', 'Line right'
function AppButtons { All $CT::Button | Where-Object { $_.Current.Name -and $_.Current.Name -notin $scrollParts } }
# Throws rather than aim at the corner of the screen, where a click could land in someone's app.
function Centre($el) {
    if (-not $el) { throw 'Nothing to aim at: the element was not found' }
    $b = $el.Current.BoundingRectangle
    if ([double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or [double]::IsNaN($b.Width) -or $b.Width -le 0 -or $b.Height -le 0) { throw "Nothing to aim at: $($el.Current.Name) has no size" }
    [int]($b.X + $b.Width / 2), [int]($b.Y + $b.Height / 2)
}
# View pixels to the screen's, at the open page's scaling.
function Px($dip) {
    if (-not $script:scale) { throw 'No scaling yet: open a page first' }
    [int][math]::Round($dip * $script:scale)
}
# Windows lets a process that isn't in front only ask, so it also asks through UI Automation, which the app itself acts on.
function Fore {
    [void][RealInput]::SetForegroundWindow($p.MainWindowHandle)
    if ($win -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle) { try { $win.SetFocus() } catch { } }
    Start-Sleep -Milliseconds 150
}
# Called before any input, so that nothing lands in another app if something else takes the focus.
function Guard {
    # A few tries: just after it starts, Windows can be slow to bring the sample forward.
    for ($try = 0; $try -lt 5 -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle; $try++) { Fore }
    $front = [RealInput]::GetForegroundWindow()
    if ($front -ne $p.MainWindowHandle) {
        $owner = 0; [void][RealInput]::GetWindowThreadProcessId($front, [ref]$owner)
        $name = try { (Get-Process -Id $owner).ProcessName } catch { "process $owner" }
        throw "The sample is not in front ($name is), so no input was sent"
    }
}
# Refuses a point outside the window, such as a button below the fold of a long list, which would land in whatever is under it.
function InWindow($x, $y) { $w = $win.Current.BoundingRectangle; Inside $w.X $w.Y $w.Right $w.Bottom $x $y }
# Throws rather than send input to a point outside the sample's window, where it would land in another app.
function Within($x, $y) { if (-not (InWindow $x $y)) { throw "$x, $y is outside the window, so no input was sent" } }
# Where a press may go once the sample holds it, and Windows still takes a touch: anywhere on the screens.
function OnScreen($x, $y) {
    if (-not [System.Windows.Forms.SystemInformation]::VirtualScreen.Contains([int]$x, [int]$y)) { throw "$x, $y is off the screen, so no input was sent" }
}
# The cursor to the window's top left corner, on the page, off every control the sections use.
function Away { $r = $win.Current.BoundingRectangle; [void][RealInput]::SetCursorPos(([int]$r.X + 4), ([int]$r.Y + 4)); Start-Sleep -Milliseconds 150 }
function Key($text, $ms = 400) { Guard; $SendKeys::SendWait($text); Start-Sleep -Milliseconds $ms }
function Press { Guard; $script:held = $true; [RealInput]::Down() }
function Release { [RealInput]::Up(); $script:held = $false }
# Lets go of whatever an exception left held, on the user's desktop as well as in the sample: a touch is lifted where it
# last was, as Windows refuses a lift anywhere else.
function LetGo {
    if ($script:held) { Release }
    if ($script:touching) {
        $script:touching = $false
        try { [RealInput]::TouchUp($script:touchX, $script:touchY) } catch { }
    }
}
function TouchDown($x, $y) {
    if (-not $script:touch) { throw 'Touch injection did not start on this PC (InitializeTouchInjection failed), so no touch was sent' }
    Guard; Within $x $y; $script:touching = $true; $script:touchX = $x; $script:touchY = $y; [RealInput]::TouchDown($x, $y)
}
function TouchMove($x, $y) { OnScreen $x $y; [RealInput]::TouchMove($x, $y); $script:touchX = $x; $script:touchY = $y }
function TouchUp { [RealInput]::TouchUp($script:touchX, $script:touchY); $script:touching = $false }
# A touch held still for a while, reported every 50 ms as a digitiser does, so the app sees pointer events all along.
function Hold($ms) {
    $until = (Get-Date).AddMilliseconds($ms)
    while ((Get-Date) -lt $until) { Start-Sleep -Milliseconds 50; TouchMove $script:touchX $script:touchY }
}
# A held touch moved to a point in steps, as a finger slides.
function Slide($x, $y, $steps = 10, $ms = 16) {
    foreach ($at in DragPath $script:touchX $script:touchY ($x - $script:touchX) ($y - $script:touchY) $steps) {
        Start-Sleep -Milliseconds $ms; TouchMove $at.X $at.Y
    }
}
function Tap($x, $y) {
    TouchDown $x $y; Start-Sleep -Milliseconds 40
    TouchMove $x $y; Start-Sleep -Milliseconds 40
    TouchUp; Start-Sleep -Milliseconds 250
}
# A drag in any direction, in screen pixels, by touch and by the mouse.
function TouchDrag($x, $y, $dx, $dy, $steps = 15, $ms = 16) {
    # Windows refuses a touch off the screen (87) partway through.
    OnScreen ($x + $dx) ($y + $dy)
    TouchDown $x $y
    Slide ($x + $dx) ($y + $dy) $steps $ms
    TouchUp
}
function MouseDrag($x, $y, $dx, $dy, $steps = 15, $ms = 16) {
    Guard
    Within $x $y; OnScreen ($x + $dx) ($y + $dy)
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press
    foreach ($at in DragPath $x $y $dx $dy $steps) { Start-Sleep -Milliseconds $ms; [void][RealInput]::SetCursorPos($at.X, $at.Y) }
    Start-Sleep -Milliseconds 60
    Release
}
# Wheel notches down the page, or up it, wherever the cursor is.
function Turn($notches, $delta) {
    Guard
    for ($i = 0; $i -lt $notches; $i++) { [RealInput]::Wheel($delta); Start-Sleep -Milliseconds 50 }
    Start-Sleep -Milliseconds 350
}
function Wheel($notches) { Turn $notches (-120) }
function WheelUp($notches) { Turn $notches 120 }
function ClickAt($x, $y) {
    Within $x $y
    Guard
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press; Start-Sleep -Milliseconds 50; Release; Start-Sleep -Milliseconds 600
}
function Click($el) { $x, $y = Centre $el; ClickAt $x $y }
function IsFocused($el) { $f = $AE::FocusedElement; [bool]($f -and $el -and [Windows.Automation.Automation]::Compare($f, $el)) }
function Invoke-Element($el) { $el.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 600 }
# What a page reports comes after layout, so it can take a moment.
function WaitForText($like, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $found = FirstText $like
        if ($found) { return $found }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
# The sample's standard output, which the app is still writing to.
function ReadyLines {
    try {
        $stream = [IO.File]::Open($stdout, 'Open', 'Read', 'ReadWrite')
        try { $reader = New-Object IO.StreamReader $stream; @($reader.ReadToEnd() -split "`r?`n" | Where-Object { $_ -like 'CORNFLOWER ready*' }) }
        finally { $stream.Dispose() }
    }
    catch { @() }
}
# The next ready line after the $seen-th: the shell writes one for each page it opens, once the page has drawn. Its
# fields are read by helpers.ps1's PageOf, ScalingOf and TitleOf.
function WaitForReady($seen, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $lines = @(ReadyLines)
        if ($lines.Count -gt $seen) { return $lines[$seen] }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
function OnIndex { [bool](Named $CT::Text 'samples') -and -not (Named $CT::Button 'Back') }
function Index { $back = Named $CT::Button 'Back'; if ($back) { Invoke-Element $back } }
# Opens a page afresh, from the list, and takes its scaling from its ready line, which it keeps in $script:ready.
function Open($title) {
    Index
    $link = Named $CT::Button $title
    if (-not $link) { throw "$title isn't on the list" }
    $seen = @(ReadyLines).Count
    Invoke-Element $link
    $line = WaitForReady $seen
    if (-not $line) { throw "$title wrote no ready line" }
    if ($line -notmatch 'scaling=[\d.]+ ') { throw "$title's ready line has no scaling, so nothing can be aimed: $line" }
    $script:ready = $line
    $script:scale = ScalingOf $line
}
function Section($title, [scriptblock]$body) {
    if (-not (Selected $title $Only)) { return }
    $script:sections++
    "== $title"
    try { & $body }
    # Counted as a check, so the summary's count of checks includes it.
    catch { LetGo; $script:checks++; $script:failures++; "  FAIL  the section stopped: $($_.Exception.Message) at line $($_.InvocationInfo.ScriptLineNumber)" }
    if ($p.HasExited) { throw "The app exited during $title, with code $($p.ExitCode)" }
}

# At the phone's motion (App.PhoneMotionArgument), not the sample's twice as quick one: the waits here are the phone's.
$p = Start-Process $Exe -ArgumentList '--phone-motion' -PassThru -RedirectStandardOutput $stdout
try {
    $win = $null
    for ($i = 0; $i -lt 40 -and -not $win; $i++) {
        Start-Sleep -Milliseconds 500
        $p.Refresh()
        if ($p.MainWindowHandle -ne 0) { $win = $AE::FromHandle($p.MainWindowHandle) }
    }
    if (-not $win) { throw 'The window never appeared' }
    Fore
    Start-Sleep -Milliseconds 500
    $r = $win.Current.BoundingRectangle
    $area = [System.Windows.Forms.Screen]::FromHandle($p.MainWindowHandle).WorkingArea
    if ($r.Bottom -gt $area.Bottom -or $r.Right -gt $area.Right -or $r.Y -lt $area.Y -or $r.X -lt $area.X) {
        throw "The window ($r) doesn't fit on the screen ($area), so clicks would miss and frames would be cut off. Move it, or if it's taller than the screen, lower the display scale."
    }
    # Before starting injection, which adds a contact of its own.
    $contacts = [RealInput]::GetSystemMetrics(95)
    $script:touch = [RealInput]::TouchInit()
    "$Exe"
    "window $($r.Width) x $($r.Height) on the screen, frames in $Out"
    "touch hardware contacts $contacts, touch injection $($script:touch)"
    $titles = @(AppButtons | ForEach-Object { $_.Current.Name })
    Shot 'index'
    Check 'the list has a page' ($titles.Count -gt 0)

    Section 'Every page on the list: a click opens it and Back leaves it' {
        foreach ($title in $titles) {
            Index
            $link = Named $CT::Button $title
            $x, $y = Centre $link
            $seen = @(ReadyLines).Count
            # A list longer than the window: what's below the fold is opened through UI Automation instead.
            if (InWindow $x $y) { Click $link } else { "        $title is below the fold: opened through UI Automation"; Invoke-Element $link }
            Check "a click on '$title' opens it" ([bool](Named $CT::Button 'Back'))
            # The shell times every page and writes its ready line, with no code in the page.
            $ready = WaitForReady $seen
            Check "$title reports ready, under its title" ((TitleOf $ready) -ceq $title) "$ready"
            Shot $title
            # A control with no size is invisible, as Avonic's fixed slot's button once was.
            $empty = @(AppButtons | Where-Object {
                $b = $_.Current.BoundingRectangle
                [double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or $b.Width -eq 0 -or $b.Height -eq 0
            } | ForEach-Object { $_.Current.Name })
            Check "$title has no button without a size" ($empty.Count -eq 0) ($empty -join ', ')
            if ($debugBuild) {
                $line = WaitForText "$(Literal $title): * ms, *"
                Check "$title reports its cost" ([bool]$line) ''
                if ($line) { "        $line" }
            }
            Click (Named $CT::Button 'Back')
            Check "Back from '$title' shows the list" (OnIndex)
        }
    }

    Section 'The page opens at the reference view, 450 x 800' {
        Open 'theme'
        $line = $script:ready
        "        $line"
        $w, $h = PageOf $line
        $scaling = ScalingOf $line
        Check 'the ready line is the theme page''s' ((TitleOf $line) -ceq 'theme') "$line"
        Check 'the theme page reports ready, laid out at 450 x 800 to the pixel' ((WithinAPixel $w 450 $scaling) -and (WithinAPixel $h 800 $scaling)) "$line"
    }

    Section 'Alt+Left goes back' {
        Open 'theme'
        Key '%{LEFT}'
        Check 'Alt+Left on a page shows the list' (OnIndex)
    }

    # PhoneToggleSwitch: only the thumb takes the finger, a tap toggles (moved under 17.86 view px, and no pointer event
    # 1,000 ms after the press), a drag goes over past half the travel, and the knob slides after either. The State line
    # under the first switch says whether it is on. The probes' limits are in tools/refapp/README.md, "Probes".
    Section 'toggleswitch: a tap, a drag and a hold on the knob' {
        Open 'toggleswitch'
        $wifi = ById 'WiFi'
        $b = $wifi.Current.BoundingRectangle
        $ox, $oy = KnobAt $b.Right $b.Bottom $script:scale $false
        $nx, $ny = KnobAt $b.Right $b.Bottom $script:scale $true
        $stateText = ReadOut 'State'
        function State { $stateText.Current.Name }
        function IsOn { (State) -eq 'wi-fi is on' }
        # The switch's column, which the knob and the curtain move in.
        $switch = { Region ($b.Right - (Px $SwitchColumn)) ($oy - (Px ($SwitchHeight / 2))) (Px $SwitchColumn) (Px $SwitchHeight) }
        Away
        $offLook = & $switch
        try {
            Check 'the first switch starts off' (-not (IsOn)) (State)

            Tap $ox $oy
            Start-Sleep -Milliseconds 600
            Shot 'toggleswitch-tapped-on'
            Check 'a touch tap on the knob turns it on' (IsOn) (State)
            $moved = Changed $offLook (& $switch)
            Check 'and the knob slides over' ($moved -gt 20) "$moved sampled pixels differ"
            Tap $nx $ny
            Start-Sleep -Milliseconds 600
            Check 'a second tap turns it off' (-not (IsOn)) (State)
            $returned = Changed $offLook (& $switch)
            Check 'and the knob slides back to where it was' ($returned -lt 5) "$returned sampled pixels differ"

            TouchDrag $ox $oy (Px $SwitchTapMove) 0 5
            Start-Sleep -Milliseconds 600
            Check "a press that moves $SwitchTapMove view px is still a tap, and toggles" (IsOn) (State)
            Tap $nx $ny
            Start-Sleep -Milliseconds 600
            TouchDrag $ox $oy (Px $SwitchShortDrag) 0
            Start-Sleep -Milliseconds 600
            Check "a drag of $SwitchShortDrag view px, short of half the travel, goes back off" (-not (IsOn)) (State)
            TouchDrag $ox $oy (Px $SwitchLongDrag) 0
            Start-Sleep -Milliseconds 600
            Check "a drag of $SwitchLongDrag view px goes on" (IsOn) (State)
            TouchDrag $nx $ny (-(Px $SwitchLongDrag)) 0
            Start-Sleep -Milliseconds 600
            Check 'and one back goes off' (-not (IsOn)) (State)

            # The knob is the finger's from the first move, before the switch decides anything.
            TouchDown $ox $oy
            Slide ($ox + (Px 20)) $oy 5
            Hold 150
            $following = Changed $offLook (& $switch)
            Shot 'toggleswitch-following'
            TouchUp
            Start-Sleep -Milliseconds 600
            Check 'the knob follows the finger while it is down' ($following -gt 10) "$following sampled pixels differ"

            TouchDown $ox $oy
            Hold 1300
            TouchUp
            Start-Sleep -Milliseconds 600
            Check 'a still press held past 1,000 ms is a hold, and leaves it off' (-not (IsOn)) (State)

            # Only the thumb takes the finger: the header's corner is the switch's, but nothing happens there.
            Tap ([int]$b.X + (Px $SwitchHeaderIn)) ([int]$b.Y + (Px $SwitchHeaderIn))
            Start-Sleep -Milliseconds 600
            Check 'a tap on the header, away from the knob, does nothing' (-not (IsOn)) (State)

            ClickAt $ox $oy
            Check 'a mouse click on the knob turns it on' (IsOn) (State)
            MouseDrag $nx $ny (-(Px $SwitchLongDrag)) 0
            Start-Sleep -Milliseconds 600
            Check 'and a mouse drag back turns it off' (-not (IsOn)) (State)

            # PhoneNoManipulation on the switch: a drag up it is the switch's, not the page's.
            $top = $wifi.Current.BoundingRectangle.Y
            TouchDrag $ox $oy 0 (-(Px 120))
            Start-Sleep -Milliseconds 1200
            Check "a touch drag up the knob doesn't scroll the page" ($wifi.Current.BoundingRectangle.Y -eq $top) "$($wifi.Current.BoundingRectangle.Y) against $top"
            Check 'and, moving nothing across, leaves it off' (-not (IsOn)) (State)
            # Off the switch, the same drag pans the page, so the check above proves something.
            $hx, $hy = Centre (Named $CT::Text 'no header')
            TouchDrag $hx $hy 0 (-(Px 120))
            Start-Sleep -Milliseconds 1500
            Check 'a touch drag up off the switch scrolls the page' ($wifi.Current.BoundingRectangle.Y -lt ($top - (Px 60))) "$($wifi.Current.BoundingRectangle.Y) against $top"
        }
        finally { $offLook.Dispose() }
    }

    # PhoneNoManipulation on a Slider in the page's ScrollViewer: a touch drag that starts on it moves the thumb, however
    # far up or down it goes, and never pans the page; a drag that starts off it does. The State line under the first
    # Slider is its value, to the nearest whole number.
    Section 'slider: a drag in a ScrollViewer moves the thumb, not the page' {
        Open 'slider'
        $volume = ById 'Volume'
        $b = $volume.Current.BoundingRectangle
        $valueText = ReadOut 'State'
        function Value { CountOf $valueText.Current.Name }
        function Top { $volume.Current.BoundingRectangle.Y }
        function Near($want) { $v = Value; ($null -ne $v) -and [math]::Abs($v - $want) -le 3 }
        $top = Top
        Away

        $x20, $y = SliderAt $b.X $b.Width $b.Bottom $script:scale 20
        $x80, $null = SliderAt $b.X $b.Width $b.Bottom $script:scale 80
        TouchDrag $x20 $y ($x80 - $x20) 0
        Start-Sleep -Milliseconds 400
        Shot 'slider-touch-drag'
        Check 'a touch drag across moves the thumb with the finger, from 20 to 80' (Near 80) ($valueText.Current.Name)
        Check 'and leaves the page where it was' ((Top) -eq $top) "$(Top) against $top"

        $x30, $null = SliderAt $b.X $b.Width $b.Bottom $script:scale 30
        $x50, $null = SliderAt $b.X $b.Width $b.Bottom $script:scale 50
        TouchDrag $x30 $y ($x50 - $x30) (-(Px 150)) 20
        Start-Sleep -Milliseconds 1200
        Shot 'slider-touch-drag-up'
        Check 'a drag mostly up the page moves the thumb across, from 30 to 50' (Near 50) ($valueText.Current.Name)
        Check 'and still leaves the page where it was' ((Top) -eq $top) "$(Top) against $top"

        MouseDrag $x50 $y ($x20 - $x50) 0
        Start-Sleep -Milliseconds 400
        Check 'a mouse drag moves it back, to 20' (Near 20) ($valueText.Current.Name)

        # Off the Slider, the same drag up pans the page, so the two above prove something.
        $hx, $hy = Centre (Named $CT::Text 'reversed')
        TouchDrag $hx $hy 0 (-(Px 150)) 20
        Start-Sleep -Milliseconds 1500
        Shot 'slider-page-panned'
        Check 'a touch drag up off the Slider scrolls the page' ((Top) -lt ($top - (Px 60))) "$(Top) against $top"
        Check 'and leaves the value' (Near 20) ($valueText.Current.Name)

        # In the page's margin, left of every Slider.
        $mx = [int]$win.Current.BoundingRectangle.X + (Px 6)
        Within $mx $hy
        [void][RealInput]::SetCursorPos($mx, $hy); Start-Sleep -Milliseconds 150
        WheelUp 10
        Check 'the wheel scrolls it back to the top' ((Top) -eq $top) "$(Top) against $top"
        Wheel 3
        Check 'and down again' ((Top) -lt $top) "$(Top) against $top"
        Away
    }

    # PhonePress on a RepeatButton: ClickMode Press, the first repeat after 500 ms and then every 33 ms, none while the
    # pressing finger is off the button, and back with no second delay when it returns (run 20261112-0755-dev11). The
    # count under the buttons is the page's. Real time, on Avalonia's own timer, so the bounds are loose.
    Section 'repeatbutton: a hold, sliding off and back' {
        Open 'repeatbutton'
        $more = ById 'More'
        $x, $y = Centre $more
        # Off it sideways, along the row: the page's ScrollViewer doesn't scroll across, so no pan can take the touch,
        # and only PhonePress can stop the repeats.
        $offX = $x + (Px 120)
        Within $offX $y
        $countText = ReadOut 'Count'
        function Count { CountOf $countText.Current.Name }

        ClickAt $x $y
        Check 'a click counts once' ((Count) -eq 1) ($countText.Current.Name)
        Check 'and focuses it' (IsFocused $more) "$($AE::FocusedElement.Current.Name)"
        Key ' '
        Check 'Space on it counts once' ((Count) -eq 2) ($countText.Current.Name)
        Tap $x $y
        Check 'a touch tap counts once' ((Count) -eq 3) ($countText.Current.Name)

        TouchDown $x $y
        Hold 250
        $pressed = Count
        Hold 1250
        $repeated = Count
        Shot 'repeatbutton-held'
        Slide $offX $y
        Hold 150
        $offAt = Count
        Hold 1000
        $offLater = Count
        Slide $x $y
        Hold 400
        $backAgain = Count
        TouchUp
        $lifted = Count
        Start-Sleep -Milliseconds 400
        $after = Count
        Check 'a touch press counts at once, and not again within 500 ms' ($pressed -eq 4) "$pressed"
        Check 'held, it repeats' ((($repeated - $pressed) -ge 10) -and (($repeated - $pressed) -le 60)) "$pressed to $repeated in about 1.3 s"
        Check 'with the finger slid off it sideways, the repeats stop' ($offLater -eq $offAt) "$offAt to $offLater in a second off it"
        Check 'slid back on, they start again, with no second delay' (($backAgain - $offLater) -ge 5) "$offLater to $backAgain in 400 ms"
        Check 'and lifting the finger stops them' ($after -eq $lifted) "$lifted to $after"

        # The mouse: the same, with the button held down.
        $start = Count
        Guard
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
        Press; Start-Sleep -Milliseconds 1000
        [void][RealInput]::SetCursorPos($offX, $y); Start-Sleep -Milliseconds 150
        $offAt = Count
        Start-Sleep -Milliseconds 800
        $offLater = Count
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 400
        $backAgain = Count
        Release; Start-Sleep -Milliseconds 300
        Check 'held with the mouse, it repeats' (($offAt - $start) -ge 10) "$start to $offAt"
        Check 'and stops while the mouse is off it' ($offLater -eq $offAt) "$offAt to $offLater"
        Check 'and starts again back on it' (($backAgain - $offLater) -ge 5) "$offLater to $backAgain"
        Away
    }

    # Whether Avalonia sets :pointerover under a real touch, which PhonePress's note says headless tests found it doesn't.
    # The rail's Back draws PhoneLowBrush behind its arrow only under :pointerover, and dims to 0.6 when pressed, so a pixel
    # in its corner, away from the arrow, is the rail's black unless :pointerover is set. Read while the touch is held,
    # before Windows can turn it into a mouse move; the tap then goes back to the list.
    Section 'pointerover under a held touch, on the rail''s Back' {
        Open 'button'
        $back = Named $CT::Button 'Back'
        $bb = $back.Current.BoundingRectangle
        $cx = [int]$bb.X + 3; $cy = [int]$bb.Y + 3
        $x, $y = Centre $back
        Away
        Start-Sleep -Milliseconds 300
        $rest = Pixel $cx $cy
        Guard
        [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 400
        $hover = Pixel $cx $cy
        Check 'a mouse over Back draws its pointer-over brush' ($hover -ne $rest) ('{0:X8} against {1:X8}' -f $hover, $rest)
        Away
        Start-Sleep -Milliseconds 400
        $left = Pixel $cx $cy
        Check 'and it goes when the mouse leaves' ($left -eq $rest) ('{0:X8} against {1:X8}' -f $left, $rest)

        TouchDown $x $y
        Hold 400
        $touched = Pixel $cx $cy
        # Where Windows has put the cursor: on Back, a mouse move it made of the touch could have set :pointerover too.
        $at = [RealInput]::Cursor()
        $cursorOn = Inside $bb.X $bb.Y $bb.Right $bb.Bottom $at[0] $at[1]
        Shot 'pointerover-touch-held'
        TouchUp
        Start-Sleep -Milliseconds 600
        $verdict = HoverVerdict $rest $hover $touched $cursorOn
        "        under a held touch, :pointerover is $verdict ({0:X8}; at rest {1:X8}, under the mouse {2:X8}; the cursor at {3}, {4})" -f $touched, $rest, $hover, $at[0], $at[1]
        # Only once the pixel can tell: if the mouse didn't change it, the check above has failed for that already.
        if ($hover -ne $rest) { Check 'a held touch settles whether :pointerover is set' ($verdict -in 'set', 'not set') $verdict }
        Check 'and the tap on Back shows the list' (OnIndex)
        Away
    }

    Section 'The window can be resized' {
        $transform = $win.GetCurrentPattern([Windows.Automation.TransformPattern]::Pattern)
        Check 'the window says it can be resized' ($transform.Current.CanResize)
        # The pattern sets the window's outer size, title bar and all, so only the width is checked.
        $r = $win.Current.BoundingRectangle
        $transform.Resize($r.Width + 300, $r.Height)
        Start-Sleep -Milliseconds 500
        Open 'theme'
        $line = $script:ready
        "        $line"
        $w, $h = PageOf $line
        # 300 pixels wider, less what the frame takes, which is far less than 100.
        Check 'once wider, the page is laid out wider' ($w -gt 450 + 200 / (ScalingOf $line)) "$line"
        Shot 'theme-resized'
    }

    # A mistyped -Only would otherwise pass with nothing run.
    Check 'a section ran' ($script:sections -gt 0) "none is like '$Only'"
    Check 'the app is still running' (-not $p.HasExited)
}
finally {
    LetGo
    [void][RealInput]::SetCursorPos($cursor.X, $cursor.Y)
    if (-not $p.HasExited) { [void]$p.CloseMainWindow(); Start-Sleep -Milliseconds 500 }
    if (-not $p.HasExited) { $p.Kill() }
}

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
