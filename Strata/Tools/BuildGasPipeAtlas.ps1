Add-Type -AssemblyName System.Drawing

$size = 256
$cell = 64
$texDir = Join-Path $PSScriptRoot "..\Textures\Strata\Buildings"

function New-AtlasBitmap {
    return New-Object System.Drawing.Bitmap $size, $size
}

function DrawVisiblePipe($gfx, $x, $y, $north, $south, $east, $west) {
    $cx = $x + 32
    $cy = $y + 32
    $outline = [System.Drawing.Pens]::Black
    $dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 58, 52, 44))
    $mid = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 168, 118, 58))
    $hi = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 228, 188, 118))
    $pw = 18
    $half = [math]::Floor($pw / 2)

    if ($north) {
        $gfx.FillRectangle($dark, $cx - $half, $cy - 32, $pw, 32)
        $gfx.DrawRectangle($outline, $cx - $half, $cy - 32, $pw, 32)
        $gfx.DrawLine($hi, $cx - $half + 3, $cy - 32, $cx - $half + 3, $cy)
    }
    if ($south) {
        $gfx.FillRectangle($dark, $cx - $half, $cy, $pw, 32)
        $gfx.DrawRectangle($outline, $cx - $half, $cy, $pw, 32)
        $gfx.DrawLine($hi, $cx - $half + 3, $cy, $cx - $half + 3, $cy + 32)
    }
    if ($east) {
        $gfx.FillRectangle($dark, $cx, $cy - $half, 32, $pw)
        $gfx.DrawRectangle($outline, $cx, $cy - $half, 32, $pw)
        $gfx.DrawLine($hi, $cx, $cy - $half + 3, $cx + 32, $cy - $half + 3)
    }
    if ($west) {
        $gfx.FillRectangle($dark, $cx - 32, $cy - $half, 32, $pw)
        $gfx.DrawRectangle($outline, $cx - 32, $cy - $half, 32, $pw)
        $gfx.DrawLine($hi, $cx - 32, $cy - $half + 3, $cx, $cy - $half + 3)
    }

    $gfx.FillEllipse($mid, $cx - 10, $cy - 10, 20, 20)
    $gfx.DrawEllipse($outline, $cx - 10, $cy - 10, 20, 20)

    $dark.Dispose()
    $mid.Dispose()
    $hi.Dispose()
}

function DrawHiddenPipe($gfx, $x, $y, $north, $south, $east, $west) {
    $cx = $x + 32
    $cy = $y + 32
    $outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(90, 70, 74, 82))
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 88, 92, 98))
    $pw = 10

    if ($north) { $gfx.FillRectangle($fill, $cx - 5, $cy - 32, $pw, 32); $gfx.DrawRectangle($outline, $cx - 5, $cy - 32, $pw, 32) }
    if ($south) { $gfx.FillRectangle($fill, $cx - 5, $cy, $pw, 32); $gfx.DrawRectangle($outline, $cx - 5, $cy, $pw, 32) }
    if ($east)  { $gfx.FillRectangle($fill, $cx, $cy - 5, 32, $pw); $gfx.DrawRectangle($outline, $cx, $cy - 5, 32, $pw) }
    if ($west)  { $gfx.FillRectangle($fill, $cx - 32, $cy - 5, 32, $pw); $gfx.DrawRectangle($outline, $cx - 32, $cy - 5, 32, $pw) }
    $gfx.FillEllipse($fill, $cx - 6, $cy - 6, 12, 12)
    $gfx.DrawEllipse($outline, $cx - 6, $cy - 6, 12, 12)

    $outline.Dispose()
    $fill.Dispose()
}

function DrawBlueprintPipe($gfx, $x, $y, $north, $south, $east, $west, [int]$alpha) {
    $cx = $x + 32
    $cy = $y + 32
    $outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb($alpha, 120, 130, 150))
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb([math]::Min($alpha, 140), 150, 160, 180))

    if ($north) { $gfx.FillRectangle($fill, $cx - 6, $cy - 32, 12, 32); $gfx.DrawRectangle($outline, $cx - 6, $cy - 32, 12, 32) }
    if ($south) { $gfx.FillRectangle($fill, $cx - 6, $cy, 12, 32); $gfx.DrawRectangle($outline, $cx - 6, $cy, 12, 32) }
    if ($east)  { $gfx.FillRectangle($fill, $cx, $cy - 6, 32, 12); $gfx.DrawRectangle($outline, $cx, $cy - 6, 32, 12) }
    if ($west)  { $gfx.FillRectangle($fill, $cx - 32, $cy - 6, 32, 12); $gfx.DrawRectangle($outline, $cx - 32, $cy - 6, 32, 12) }
    $gfx.FillEllipse($fill, $cx - 7, $cy - 7, 14, 14)
    $gfx.DrawEllipse($outline, $cx - 7, $cy - 7, 14, 14)

    $outline.Dispose()
    $fill.Dispose()
}

function Fill-Atlas($bmp, $drawFn) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $idx = 0
    for ($row = 0; $row -lt 4; $row++) {
        for ($col = 0; $col -lt 4; $col++) {
        $mask = $idx
        $n = ($mask -band 1) -ne 0
        $e = ($mask -band 2) -ne 0
        $s = ($mask -band 4) -ne 0
        $w = ($mask -band 8) -ne 0
        & $drawFn $g ($col * $cell) ($row * $cell) $n $s $e $w
            $idx++
        }
    }
    $g.Dispose()
}

function Save-MenuIcon($path, $drawFn) {
    $menu = New-Object System.Drawing.Bitmap 64, 64
    $mg = [System.Drawing.Graphics]::FromImage($menu)
    $mg.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    & $drawFn $mg 0 0 $false $false $true $true
    $mg.Dispose()
    $menu.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $menu.Dispose()
}

$visibleAtlas = New-AtlasBitmap
Fill-Atlas $visibleAtlas { param($g,$x,$y,$n,$s,$e,$w) DrawVisiblePipe $g $x $y $n $s $e $w }
$visibleAtlas.Save((Join-Path $texDir "GasPipe_Atlas.png"), [System.Drawing.Imaging.ImageFormat]::Png)
Save-MenuIcon (Join-Path $texDir "GasPipe_MenuIcon.png") { param($g,$x,$y,$n,$s,$e,$w) DrawVisiblePipe $g $x $y $n $s $e $w }

$visibleBlueprint = New-AtlasBitmap
Fill-Atlas $visibleBlueprint { param($g,$x,$y,$n,$s,$e,$w) DrawBlueprintPipe $g $x $y $n $s $e $w 180 }
$visibleBlueprint.Save((Join-Path $texDir "GasPipe_Blueprint_Atlas.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$visibleBlueprint.Dispose()

$hiddenAtlas = New-AtlasBitmap
Fill-Atlas $hiddenAtlas { param($g,$x,$y,$n,$s,$e,$w) DrawHiddenPipe $g $x $y $n $s $e $w }
$hiddenAtlas.Save((Join-Path $texDir "HiddenGasPipe_Atlas.png"), [System.Drawing.Imaging.ImageFormat]::Png)
Save-MenuIcon (Join-Path $texDir "HiddenGasPipe_MenuIcon.png") { param($g,$x,$y,$n,$s,$e,$w) DrawHiddenPipe $g $x $y $n $s $e $w }

$hiddenBlueprint = New-AtlasBitmap
Fill-Atlas $hiddenBlueprint { param($g,$x,$y,$n,$s,$e,$w) DrawBlueprintPipe $g $x $y $n $s $e $w 90 }
$hiddenBlueprint.Save((Join-Path $texDir "HiddenGasPipe_Blueprint_Atlas.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$hiddenBlueprint.Dispose()

$visibleAtlas.Dispose()
$hiddenAtlas.Dispose()

Write-Output "Wrote gas pipe atlases to $texDir"
