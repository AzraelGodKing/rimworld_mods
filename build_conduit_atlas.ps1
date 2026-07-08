# Build heavy-armored linked conduit atlas from segment + junction hub source art.
param(
    [string]$SegmentPath = "$PSScriptRoot\assets\ArmoredConduit_segment_proc.png",
    [string]$HubPath = "$PSScriptRoot\assets\ArmoredConduit_hub_proc.png",
    [string]$OutAtlas = "$PSScriptRoot\Stormproof\Textures\Stormproof\Buildings\ArmoredConduit_Atlas.png",
    [string]$OutSegment = "$PSScriptRoot\Stormproof\Textures\Stormproof\Buildings\ArmoredConduit.png",
    [string]$OutMenu = "$PSScriptRoot\Stormproof\Textures\Stormproof\Buildings\ArmoredConduit_MenuIcon.png",
    [int]$Cell = 128
)

Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

[Flags]
public enum LinkDir {
    None=0, Up=1, Right=2, UpRight=3, Down=4, UpDown=5, RightDown=6, UpRightDown=7,
    Left=8, UpLeft=9, LeftRight=10, UpLeftRight=11, DownLeft=12, UpDownLeft=13, DownLeftRight=14, All=15
}

public static class HeavyConduitAtlas
{
    const float BandY = 0.34f;
    const float BandH = 0.32f;
    const float HubScale = 0.52f;
    const float HubCrop = 0.62f;
    const float ArmGap = 0.08f;

    public static Bitmap Build(Bitmap segment, Bitmap hub, int cell)
    {
        var sheet = new Bitmap(cell * 4, cell * 4, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sheet)) g.Clear(Color.Transparent);
        for (int i = 0; i < 16; i++)
        {
            var tile = RenderCell(segment, hub, cell, (LinkDir)i);
            using (var g = Graphics.FromImage(sheet))
                g.DrawImage(tile, (i % 4) * cell, (i / 4) * cell);
            tile.Dispose();
        }
        return sheet;
    }

    public static Bitmap RenderCell(Bitmap segment, Bitmap hub, int cell, LinkDir link)
    {
        var bmp = new Bitmap(cell, cell, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool n = Has(link, LinkDir.Up);
            bool e = Has(link, LinkDir.Right);
            bool s = Has(link, LinkDir.Down);
            bool w = Has(link, LinkDir.Left);
            int count = (n?1:0)+(e?1:0)+(s?1:0)+(w?1:0);
            bool corner = count == 2 && !((n && s) || (e && w));
            bool needsHub = corner || count >= 3;

            float cx = cell * 0.5f;
            float cy = cell * 0.5f;
            float thick = cell * BandH;
            var band = new RectangleF(0, segment.Height * BandY, segment.Width, segment.Height * BandH);
            float hubHalf = needsHub ? cell * HubScale * 0.5f : 0f;

            if (count == 0)
            {
                DrawH(g, segment, cell, cx - cell * 0.22f, cx + cell * 0.22f, cy, thick, band, true, true);
                return bmp;
            }

            if (n) DrawVArm(g, segment, cell, 0, needsHub ? cy - hubHalf : cy, cx, thick, band);
            if (s) DrawVArm(g, segment, cell, needsHub ? cy + hubHalf : cy, cell, cx, thick, band);
            if (e) DrawHArm(g, segment, cell, needsHub ? cx + hubHalf : cx, cell, cy, thick, band);
            if (w) DrawHArm(g, segment, cell, 0, needsHub ? cx - hubHalf : cx, cy, thick, band);

            if (corner) ClearInner(bmp, cell, cx, cy, n, e, s, w);
            if (needsHub) DrawHub(g, hub, cell, cx, cy, thick);
            if (count == 1) DrawEndCap(g, segment, cell, cx, cy, thick, band, n, e, s, w);
            if (count == 3) DrawEndCap(g, segment, cell, cx, cy, thick, band, n, e, s, w);
        }
        return bmp;
    }

    static bool Has(LinkDir v, LinkDir bit)
    {
        if (bit == LinkDir.Up) return ((int)v & 1) != 0;
        if (bit == LinkDir.Right) return ((int)v & 2) != 0;
        if (bit == LinkDir.Down) return ((int)v & 4) != 0;
        if (bit == LinkDir.Left) return ((int)v & 8) != 0;
        return false;
    }

    static void DrawHArm(Graphics g, Bitmap segment, int cell, float x0, float x1, float cy, float thick, RectangleF band)
    {
        if (x1 <= x0 + 1) return;
        DrawH(g, segment, cell, x0, x1, cy, thick, band, false, false);
    }

    static void DrawVArm(Graphics g, Bitmap segment, int cell, float y0, float y1, float cx, float thick, RectangleF band)
    {
        if (y1 <= y0 + 1) return;
        var state = g.Save();
        g.TranslateTransform(cx, (y0 + y1) * 0.5f);
        g.RotateTransform(90f);
        g.TranslateTransform(-cx, -(y0 + y1) * 0.5f);
        float len = y1 - y0;
        DrawH(g, segment, cell, cx - len * 0.5f, cx + len * 0.5f, (y0 + y1) * 0.5f, thick, band, false, false);
        g.Restore(state);
    }

    static void DrawH(Graphics g, Bitmap segment, int cell, float x0, float x1, float cy, float thick, RectangleF band, bool capL, bool capR)
    {
        var dest = new RectangleF(x0, cy - thick * 0.5f, x1 - x0, thick);
        g.DrawImage(segment, dest, band, GraphicsUnit.Pixel);
        if (capL) DrawCap(g, segment, dest.Left, cy, thick, band, true);
        if (capR) DrawCap(g, segment, dest.Right, cy, thick, band, false);
    }

    static void DrawCap(Graphics g, Bitmap seg, float x, float cy, float thick, RectangleF band, bool left)
    {
        float w = thick * 0.55f;
        var src = left
            ? new RectangleF(band.X, band.Y, band.Width * 0.16f, band.Height)
            : new RectangleF(band.X + band.Width * 0.84f, band.Y, band.Width * 0.16f, band.Height);
        var dest = new RectangleF(left ? x - w * 0.2f : x - w * 0.8f, cy - thick * 0.5f, w, thick);
        g.DrawImage(seg, dest, src, GraphicsUnit.Pixel);
    }

    static void DrawHub(Graphics g, Bitmap hub, int cell, float cx, float cy, float thick)
    {
        float size = Math.Max(thick * 1.35f, cell * HubScale);
        float crop = hub.Width * (1f - HubCrop) * 0.5f;
        var src = new RectangleF(crop, crop, hub.Width - crop * 2f, hub.Height - crop * 2f);
        var dest = new RectangleF(cx - size * 0.5f, cy - size * 0.5f, size, size);
        g.DrawImage(hub, dest, src, GraphicsUnit.Pixel);
    }

    static void DrawEndCap(Graphics g, Bitmap segment, int cell, float cx, float cy, float thick, RectangleF band, bool n, bool e, bool s, bool w)
    {
        int count = (n?1:0)+(e?1:0)+(s?1:0)+(w?1:0);
        if (count == 1)
        {
            if (n) PlaceCap(g, segment, cx, cy, thick, band, 0, 1);
            if (s) PlaceCap(g, segment, cx, cy, thick, band, 0, -1);
            if (e) PlaceCap(g, segment, cx, cy, thick, band, 1, 0);
            if (w) PlaceCap(g, segment, cx, cy, thick, band, -1, 0);
            return;
        }
        if (count == 3)
        {
            if (!n) PlaceCap(g, segment, cx, cy, thick, band, 0, -1);
            if (!s) PlaceCap(g, segment, cx, cy, thick, band, 0, 1);
            if (!e) PlaceCap(g, segment, cx, cy, thick, band, 1, 0);
            if (!w) PlaceCap(g, segment, cx, cy, thick, band, -1, 0);
        }
    }

    static void PlaceCap(Graphics g, Bitmap seg, float cx, float cy, float thick, RectangleF band, int dx, int dy)
    {
        float cap = thick * 0.55f;
        if (dx != 0)
        {
            float x = dx > 0 ? cx + thick * 0.35f : cx - thick * 0.35f;
            DrawCap(g, seg, x, cy, thick, band, dx < 0);
            return;
        }
        float y = dy > 0 ? cy + thick * 0.35f : cy - thick * 0.35f;
        var state = g.Save();
        g.TranslateTransform(cx, y);
        g.RotateTransform(90f);
        g.TranslateTransform(-cx, -y);
        DrawCap(g, seg, cx, y, thick, band, dy < 0);
        g.Restore(state);
    }

    static void ClearInner(Bitmap bmp, int cell, float cx, float cy, bool n, bool e, bool s, bool w)
    {
        RectangleF dead;
        if (n && e) dead = new RectangleF(0, cy, cx, cell - cy);
        else if (n && w) dead = new RectangleF(cx, cy, cell - cx, cell - cy);
        else if (s && e) dead = new RectangleF(0, 0, cx, cy);
        else dead = new RectangleF(cx, 0, cell - cx, cy);

        using (var g = Graphics.FromImage(bmp))
        {
            var st = g.Save();
            g.CompositingMode = CompositingMode.SourceCopy;
            using (var brush = new SolidBrush(Color.Transparent))
                g.FillRectangle(brush, dead.X, dead.Y, dead.Width, dead.Height);
            g.Restore(st);
        }
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
$segment = [HeavyConduitAtlas]::RenderCell($seg, $hub, $Cell, [LinkDir]::LeftRight)
$menu = [HeavyConduitAtlas]::RenderCell($seg, $hub, $Cell, [LinkDir]::None)

$dir = Split-Path $OutAtlas -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

$atlas.Save($OutAtlas, [Drawing.Imaging.ImageFormat]::Png)
$segment.Save($OutSegment, [Drawing.Imaging.ImageFormat]::Png)
$menu.Save($OutMenu, [Drawing.Imaging.ImageFormat]::Png)

$atlas.Dispose(); $segment.Dispose(); $menu.Dispose(); $seg.Dispose(); $hub.Dispose()

Copy-Item $OutSegment "$PSScriptRoot\docs\img\ArmoredConduit.png" -Force
Copy-Item $OutAtlas "$PSScriptRoot\docs\img\ArmoredConduit_Atlas.png" -Force

Write-Output "Atlas:   $OutAtlas"
Write-Output "Segment: $OutSegment"
Write-Output "Menu:    $OutMenu"
