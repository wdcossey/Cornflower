# Checks drive.ps1 without running it: its pure helpers (helpers.ps1) on known answers, and the script itself as parsed,
# that it parses, that each section's title selects it alone under -Only, and that every page it opens and every control
# it finds by name is in the sample. Takes no input and opens no window, so it runs on any OS, as
# tests/Cornflower.Tests/Tools/RealWindowTests.cs runs it:
#   pwsh -NoProfile -File tools/real-window/selftest.ps1
# Exits 1 if a check fails.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'helpers.ps1')

$script:failures = 0
$script:checks = 0

'== the helpers'
$line = 'CORNFLOWER ready open=212ms scaling=1.25 view=490x800 raw=612x1000 safe=0,0,0,0 page=449.6x800 title=a page, with spaces'
$w, $h = PageOf $line
Check 'PageOf reads page=, not view=' (($w -eq 449.6) -and ($h -eq 800)) "$w x $h"
Check 'ScalingOf reads scaling=' ((ScalingOf $line) -eq 1.25) (ScalingOf $line)
Check 'ScalingOf is 1 without it' ((ScalingOf 'CORNFLOWER ready open=1ms') -eq 1)
Check 'TitleOf reads the title to the end, spaces and all' ((TitleOf $line) -ceq 'a page, with spaces') (TitleOf $line)
Check 'WithinAPixel takes 449.6 for 450 at 1.25' (WithinAPixel 449.6 450 1.25)
Check 'but not 448.8 (a pixel and a half)' (-not (WithinAPixel 448.8 450 1.25))
Check 'Literal escapes a pattern' (('a*b' -like (Literal 'a*b')) -and -not ('axb' -like (Literal 'a*b')))

Check 'Selected takes * for every title' (Selected 'slider: anything' '*')
Check 'Selected takes one of several patterns' (Selected 'slider: a drag' 'toggleswitch*, slider*')
Check 'Selected leaves a title no pattern is like' (-not (Selected 'slider: a drag' 'toggleswitch*,repeat*'))
Check 'Selected takes a title with a comma, by itself' (Selected 'one, two' 'one, two')
Check 'Selected ignores an empty part' (-not (Selected 'x' 'a,,b'))

Check 'Inside is inside' (Inside 0 0 10 10 5 5)
Check 'Inside leaves out the edges' (-not (Inside 0 0 10 10 0 5) -and -not (Inside 0 0 10 10 5 10))
Check 'Inside leaves out a point beyond them' (-not (Inside 0 0 10 10 11 5))

$path = @(DragPath 100 200 30 -60 3)
Check 'DragPath has its steps, and ends where the drag does' (($path.Count -eq 3) -and ($path[2].X -eq 130) -and ($path[2].Y -eq 140)) (($path | ForEach-Object { "$($_.X),$($_.Y)" }) -join ' ')
Check 'DragPath moves evenly, in whole pixels' (($path[0].X -eq 110) -and ($path[0].Y -eq 180) -and ($path[1].X -eq 120) -and ($path[1].Y -eq 160))
$one = @(DragPath 5 5 10 0 1)
Check 'DragPath of one step is one point' (($one.Count -eq 1) -and ($one[0].X -eq 15) -and ($one[0].Y -eq 5)) "$($one.Count)"

# At a scaling of 4, where every offset here is a whole pixel.
$ox, $oy = KnobAt 1000 500 4 $false
$nx, $ny = KnobAt 1000 500 4 $true
Check 'KnobAt off is half a knob in from the column''s left' ($ox -eq (1000 - 4 * ($SwitchColumn - $SwitchKnob / 2))) "$ox"
Check 'KnobAt on is half a knob in from the right' ($nx -eq (1000 - 4 * $SwitchKnob / 2)) "$nx"
Check 'the knob travels the column less its width' (($nx - $ox) -eq (4 * ($SwitchColumn - $SwitchKnob))) "$($nx - $ox)"
Check 'KnobAt is the switch''s middle, above the row below it' (($oy -eq $ny) -and ($oy -eq (500 - 4 * ($SwitchBelow + $SwitchHeight / 2)))) "$oy"
# Against the tap distance: RealWindowTests, which reads PhoneToggleSwitch's own.
Check 'the drags fall either side of half the travel, the short one short of it and the long one past it, within the travel' (
    ($SwitchTapMove -lt $SwitchShortDrag) -and ($SwitchShortDrag -lt ($SwitchColumn - $SwitchKnob) / 2) -and
    ($SwitchLongDrag -gt ($SwitchColumn - $SwitchKnob) / 2) -and ($SwitchLongDrag -le ($SwitchColumn - $SwitchKnob)))

# A Slider 400 view px longer than its thumb, so that each value is a whole number of view px along.
$width = 4 * (400 + $SliderThumb)
$x0, $sy = SliderAt 100 $width 900 4 0
$x100, $null = SliderAt 100 $width 900 4 100
$x25, $null = SliderAt 100 $width 900 4 25
Check 'SliderAt 0 is half a thumb in' ($x0 -eq (100 + 4 * $SliderThumb / 2)) "$x0"
Check 'SliderAt 100 is half a thumb short of the end' ($x100 -eq (100 + $width - 4 * $SliderThumb / 2)) "$x100"
Check 'SliderAt 25 is a quarter of the way along the thumb''s travel' ($x25 -eq (100 + 4 * ($SliderThumb / 2 + 100))) "$x25"
Check 'SliderAt is the track''s middle' ($sy -eq (900 - 4 * $SliderTrackAbove)) "$sy"
$x5, $null = SliderAt 0 (4 * (100 + $SliderThumb)) 0 4 5 0 10
Check 'SliderAt takes another range' ($x5 -eq (4 * ($SliderThumb / 2 + 50))) "$x5"

Check 'CountOf reads a count' (((CountOf '31') -eq 31) -and ((CountOf '-2') -eq -2) -and ((CountOf '0') -eq 0))
Check 'CountOf is nothing for anything else' (($null -eq (CountOf 'wi-fi is on')) -and ($null -eq (CountOf '')) -and ($null -eq (CountOf $null)))

Check 'HoverVerdict: the rest''s pixel under the touch is not set' (((HoverVerdict 1 2 1 $false) -eq 'not set') -and ((HoverVerdict 1 2 1 $true) -eq 'not set'))
Check 'HoverVerdict: any other is set' (((HoverVerdict 1 2 2 $false) -eq 'set') -and ((HoverVerdict 1 2 3 $false) -eq 'set'))
Check 'HoverVerdict: set with the cursor on it is not settled' ((HoverVerdict 1 2 2 $true) -like 'set, but*')
Check 'HoverVerdict: a pixel the mouse doesn''t change settles nothing' ((HoverVerdict 1 1 1 $false) -eq 'unsettled')

'== drive.ps1, as parsed'
$repo = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path
$drive = Join-Path $PSScriptRoot 'drive.ps1'
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($drive, [ref]$null, [ref]$errors)
Check 'it parses' ($errors.Count -eq 0) (($errors | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Message)" }) -join '; ')

# The first argument of each call to a command, where it is a string.
function ArgumentsOf($name) {
    $ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq $name }, $true) |
        ForEach-Object { $_.CommandElements[1] } |
        Where-Object { $_ -is [System.Management.Automation.Language.StringConstantExpressionAst] } |
        ForEach-Object { $_.Value }
}
$sections = @(ArgumentsOf 'Section')
Check 'it has sections' ($sections.Count -gt 0)
$twice = @($sections | Group-Object -CaseSensitive | Where-Object Count -gt 1 | ForEach-Object Name)
Check 'no two sections share a title' ($twice.Count -eq 0) ($twice -join ', ')
foreach ($title in $sections) {
    $also = @($sections | Where-Object { $_ -cne $title -and (Selected $_ $title) })
    Check "-Only '$title' runs that section alone" ((Selected $title $title) -and $also.Count -eq 0) ($also -join ', ')
}

# The sample's pages, by their [SamplePage] titles, and its controls' x:Names.
$sample = Join-Path $repo 'samples'
# Not bin/ or obj/, where a stale copy could keep a name the source has lost.
function Sources($filter) { Get-ChildItem $sample -Recurse -Filter $filter | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } }
$pages = @(Sources *.cs | Select-String -Pattern '\[SamplePage\("([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value })
$names = @(Sources *.axaml | Select-String -Pattern 'x:Name="([^"]+)"' -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value })
Check 'the sample has pages' ($pages.Count -gt 0)
foreach ($title in @(ArgumentsOf 'Open' | Sort-Object -Unique)) {
    Check "the page it opens, '$title', is in the sample" ($pages -ccontains $title)
}
foreach ($id in @((ArgumentsOf 'ById') + (ArgumentsOf 'ReadOut') | Sort-Object -Unique)) {
    Check "the control it finds as '$id' is named in the sample" ($names -ccontains $id)
}
# RealWindowTests finds the same names with a pattern, which sees only a quoted name: so every call outside the two
# functions' own definitions names its control in single quotes.
function InsideFinder($node) {
    for ($up = $node.Parent; $up; $up = $up.Parent) {
        if ($up -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return $up.Name -in 'ById', 'ReadOut' }
    }
    $false
}
$unquoted = @($ast.FindAll({ param($n)
            $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -in 'ById', 'ReadOut' -and
            -not ($n.CommandElements[1] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                $n.CommandElements[1].StringConstantType -eq 'SingleQuoted') -and -not (InsideFinder $n)
        }, $true) | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Extent.Text)" })
Check 'every control it finds by name is named in single quotes' ($unquoted.Count -eq 0) ($unquoted -join '; ')

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
