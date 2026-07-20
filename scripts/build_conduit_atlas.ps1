# Build heavy-armored linked conduit atlas from segment + junction hub source art.
# Arms are drawn as one continuous band per axis and clipped to connected directions,
# so wires stay seamless across tiles and through junctions.
param(
    [string]$SegmentPath = "",
    [string]$HubPath = "",
    [string]$OutAtlas = "",
    [string]$OutSegment = "",
    [string]$OutMenu = "",
    [int]$Cell = 128
)

$repo = Split-Path $PSScriptRoot -Parent
if (-not $SegmentPath) { $SegmentPath = Join-Path $repo 'assets\ArmoredConduit_segment_proc.png' }
if (-not $HubPath) { $HubPath = Join-Path $repo 'assets\ArmoredConduit_hub_proc.png' }
if (-not $OutAtlas) { $OutAtlas = Join-Path $repo 'Stormproof\Textures\Stormproof\Buildings\ArmoredConduit_Atlas.png' }
if (-not $OutSegment) { $OutSegment = Join-Path $repo 'Stormproof\Textures\Stormproof\Buildings\ArmoredConduit.png' }
if (-not $OutMenu) { $OutMenu = Join-Path $repo 'Stormproof\Textures\Stormproof\Buildings\ArmoredConduit_MenuIcon.png' }

Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class HeavyConduitAtlas
{
    const float BandY = 0.34f;      // vertical start of the armor band in source art
    const float BandH = 0.32f;      // armor band height fraction of source
    const float SrcInsetX = 0.02f;  // trim source ends so tile edges butt cleanly
    const float HubScale = 0.50f;   // junction hub size as fraction of cell
    const float TermScale = 0.34f;  // terminator block for dead ends / lone cells

    public static Bitmap Build(Bitmap segment, Bitmap hub, int cell)
    {
        var sheet = new Bitmap(cell * 4, cell * 4, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sheet)) g.Clear(Color.Transparent);
        for (int i = 0; i < 16; i++)
        {
            var tile = RenderCell(segment, hub, cell, i);
            // Unity samples atlas cells with UV origin at bottom-left:
            // link index i lives at column i%4, row i/4 counted from the BOTTOM of the PNG.
            using (var g = Graphics.FromImage(sheet))
                g.DrawImage(tile, (i % 4) * cell, (3 - i / 4) * cell);
            tile.Dispose();
        }
        return sheet;
    }

    // link bits: 1=Up 2=Right 4=Down 8=Left (vanilla LinkDirections order)
    public static Bitmap RenderCell(Bitmap segment, Bitmap hub, int cell, int link)
    {
        var bmp = new Bitmap(cell, cell, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool n = (link & 1) != 0;
            bool e = (link & 2) != 0;
            bool s = (link & 4) != 0;
            bool w = (link & 8) != 0;
            int count = (n?1:0)+(e?1:0)+(s?1:0)+(w?1:0);
            bool corner = count == 2 && !((n && s) || (e && w));
            bool needsHub = corner || count >= 3;

            float cx = cell * 0.5f;
            float cy = cell * 0.5f;
            float thick = cell * BandH;
            // arms extend slightly past center so the hub always overlaps opaque wire
            float reach = thick * 0.25f;

            if (count == 0)
            {
                // lone stub: short horizontal band with a small terminator block
                DrawBandClipped(g, segment, cell, thick, false,
                    new RectangleF(cell * 0.22f, cy - thick * 0.5f, cell * 0.56f, thick));
                DrawHub(g, hub, cx, cy, cell * TermScale);
                return bmp;
            }

            if (e || w)
            {
                float x0 = w ? 0f : cx - reach;
                float x1 = e ? cell : cx + reach;
                DrawBandClipped(g, segment, cell, thick, false,
                    new RectangleF(x0, cy - thick * 0.5f, x1 - x0, thick));
            }
            if (n || s)
            {
                float y0 = n ? 0f : cy - reach;
                float y1 = s ? cell : cy + reach;
                DrawBandClipped(g, segment, cell, thick, true,
                    new RectangleF(cx - thick * 0.5f, y0, thick, y1 - y0));
            }

            if (needsHub)
                DrawHub(g, hub, cx, cy, Math.Max(thick * 1.45f, cell * HubScale));
            else if (count == 1)
                DrawHub(g, hub, cx, cy, cell * TermScale);
        }
        return bmp;
    }

    // Draws the full segment band across the whole cell on the given axis,
    // clipped to `clip`. Both half-arms sample the same pixels, so plate
    // patterns and the glowing core stay continuous through the tile.
    static void DrawBandClipped(Graphics g, Bitmap segment, int cell, float thick, bool vertical, RectangleF clip)
    {
        var src = new RectangleF(
            segment.Width * SrcInsetX,
            segment.Height * BandY,
            segment.Width * (1f - SrcInsetX * 2f),
            segment.Height * BandH);

        var state = g.Save();
        g.SetClip(clip);
        float c = cell * 0.5f;
        if (vertical)
        {
            g.TranslateTransform(c, c);
            g.RotateTransform(90f);
            g.TranslateTransform(-c, -c);
        }
        // draw 1px past both edges to avoid bicubic transparent fringe at tile seams
        g.DrawImage(segment, new RectangleF(-1f, c - thick * 0.5f, cell + 2f, thick), src, GraphicsUnit.Pixel);
        g.Restore(state);
    }

    static void DrawHub(Graphics g, Bitmap hub, float cx, float cy, float size)
    {
        // crop to the solid armored block, skipping the hub art's protruding ports
        float crop = hub.Width * 0.19f;
        var src = new RectangleF(crop, crop, hub.Width - crop * 2f, hub.Height - crop * 2f);
        g.DrawImage(hub, new RectangleF(cx - size * 0.5f, cy - size * 0.5f, size, size), src, GraphicsUnit.Pixel);
    }
}
"@
if (-not ('HeavyConduitAtlas' -as [type])) {
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
}

if (-not (Test-Path $SegmentPath)) { throw "Missing segment: $SegmentPath" }
if (-not (Test-Path $HubPath)) { throw "Missing hub: $HubPath" }

$seg = [Drawing.Bitmap]::FromFile($SegmentPath)
$hub = [Drawing.Bitmap]::FromFile($HubPath)
$atlas = [HeavyConduitAtlas]::Build($seg, $hub, $Cell)
$segment = [HeavyConduitAtlas]::RenderCell($seg, $hub, $Cell, 10)  # LeftRight straight
$menu = [HeavyConduitAtlas]::RenderCell($seg, $hub, $Cell, 10)

$dir = Split-Path $OutAtlas -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

$atlas.Save($OutAtlas, [Drawing.Imaging.ImageFormat]::Png)
$segment.Save($OutSegment, [Drawing.Imaging.ImageFormat]::Png)
$menu.Save($OutMenu, [Drawing.Imaging.ImageFormat]::Png)

$atlas.Dispose(); $segment.Dispose(); $menu.Dispose(); $seg.Dispose(); $hub.Dispose()

Copy-Item $OutSegment (Join-Path $repo 'docs\img\ArmoredConduit.png') -Force
Copy-Item $OutAtlas (Join-Path $repo 'docs\img\ArmoredConduit_Atlas.png') -Force

Write-Output "Atlas:   $OutAtlas"
Write-Output "Segment: $OutSegment"
Write-Output "Menu:    $OutMenu"
