Add-Type -AssemblyName System.Drawing

$size = 256
$cell = 64
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None

function DrawPipe($gfx, $x, $y, $north, $south, $east, $west) {
    $cx = $x + 32
    $cy = $y + 32
    $outline = [System.Drawing.Pens]::Black
    $dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 72, 76, 82))
    $mid = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 118, 124, 132))
    $hi = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 168, 174, 182))
    $pw = 12
    $half = [math]::Floor($pw / 2)

    if ($north) {
        $gfx.FillRectangle($dark, $cx - $half, $cy - 32, $pw, 32)
        $gfx.DrawRectangle($outline, $cx - $half, $cy - 32, $pw, 32)
        $gfx.DrawLine($hi, $cx - $half + 2, $cy - 32, $cx - $half + 2, $cy)
    }
    if ($south) {
        $gfx.FillRectangle($dark, $cx - $half, $cy, $pw, 32)
        $gfx.DrawRectangle($outline, $cx - $half, $cy, $pw, 32)
        $gfx.DrawLine($hi, $cx - $half + 2, $cy, $cx - $half + 2, $cy + 32)
    }
    if ($east) {
        $gfx.FillRectangle($dark, $cx, $cy - $half, 32, $pw)
        $gfx.DrawRectangle($outline, $cx, $cy - $half, 32, $pw)
        $gfx.DrawLine($hi, $cx, $cy - $half + 2, $cx + 32, $cy - $half + 2)
    }
    if ($west) {
        $gfx.FillRectangle($dark, $cx - 32, $cy - $half, 32, $pw)
        $gfx.DrawRectangle($outline, $cx - 32, $cy - $half, 32, $pw)
        $gfx.DrawLine($hi, $cx - 32, $cy - $half + 2, $cx, $cy - $half + 2)
    }

    $gfx.FillEllipse($mid, $cx - 8, $cy - 8, 16, 16)
    $gfx.DrawEllipse($outline, $cx - 8, $cy - 8, 16, 16)

    $dark.Dispose()
    $mid.Dispose()
    $hi.Dispose()
}

$idx = 0
for ($row = 0; $row -lt 4; $row++) {
    for ($col = 0; $col -lt 4; $col++) {
        $mask = $idx
        $w = ($mask -band 1) -ne 0
        $s = ($mask -band 2) -ne 0
        $e = ($mask -band 4) -ne 0
        $n = ($mask -band 8) -ne 0
        DrawPipe $g ($col * $cell) ($row * $cell) $n $s $e $w
        $idx++
    }
}

$menu = New-Object System.Drawing.Bitmap 64, 64
$mg = [System.Drawing.Graphics]::FromImage($menu)
$mg.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
DrawPipe $mg 0 0 $false $false $true $true
$mg.Dispose()

$texDir = Join-Path $PSScriptRoot "..\Textures\Strata\Buildings"
$atlasPath = Join-Path $texDir "SmokeDuct_Atlas.png"
$menuPath = Join-Path $texDir "SmokeDuct_MenuIcon.png"
$singlePath = Join-Path $texDir "SmokeDuct.png"

$bmp.Save($atlasPath, [System.Drawing.Imaging.ImageFormat]::Png)
$menu.Save($menuPath, [System.Drawing.Imaging.ImageFormat]::Png)
$menu.Save($singlePath, [System.Drawing.Imaging.ImageFormat]::Png)

# Blueprint atlas: lighter translucent-style pipes for placement ghost
$bp = New-Object System.Drawing.Bitmap $size, $size
$bg = [System.Drawing.Graphics]::FromImage($bp)
$bg.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))

function DrawBlueprintPipe($gfx, $x, $y, $north, $south, $east, $west) {
    $cx = $x + 32
    $cy = $y + 32
    $outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 120, 130, 150))
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 150, 160, 180))

    if ($north) { $gfx.FillRectangle($fill, $cx - 5, $cy - 32, 10, 32); $gfx.DrawRectangle($outline, $cx - 5, $cy - 32, 10, 32) }
    if ($south) { $gfx.FillRectangle($fill, $cx - 5, $cy, 10, 32); $gfx.DrawRectangle($outline, $cx - 5, $cy, 10, 32) }
    if ($east)  { $gfx.FillRectangle($fill, $cx, $cy - 5, 32, 10); $gfx.DrawRectangle($outline, $cx, $cy - 5, 32, 10) }
    if ($west)  { $gfx.FillRectangle($fill, $cx - 32, $cy - 5, 32, 10); $gfx.DrawRectangle($outline, $cx - 32, $cy - 5, 32, 10) }
    $gfx.FillEllipse($fill, $cx - 7, $cy - 7, 14, 14)
    $gfx.DrawEllipse($outline, $cx - 7, $cy - 7, 14, 14)

    $outline.Dispose()
    $fill.Dispose()
}

$idx = 0
for ($row = 0; $row -lt 4; $row++) {
    for ($col = 0; $col -lt 4; $col++) {
        $mask = $idx
        $w = ($mask -band 1) -ne 0
        $s = ($mask -band 2) -ne 0
        $e = ($mask -band 4) -ne 0
        $n = ($mask -band 8) -ne 0
        DrawBlueprintPipe $bg ($col * $cell) ($row * $cell) $n $s $e $w
        $idx++
    }
}

$blueprintPath = Join-Path $texDir "SmokeDuct_Blueprint_Atlas.png"
$bp.Save($blueprintPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
$menu.Dispose()
$bg.Dispose()
$bp.Dispose()

Write-Output "Wrote $atlasPath"
Write-Output "Wrote $menuPath"
Write-Output "Wrote $blueprintPath"
