# Post-process AI-generated sprites for RimWorld:
# - remove baked-in background (flood fill from corners)
# - trim transparent border, pad to square, resize to target
# - optionally emit directional variants for Graphic_Multi buildings
param(
    [Parameter(Mandatory=$true)][string]$Source,
    [Parameter(Mandatory=$true)][string]$Dest,
    [int]$Size = 128,
    [int]$TargetW = 0,
    [int]$TargetH = 0,
    [switch]$Multi,          # emit _south/_north/_east/_west
    [switch]$NoTrim,         # terrain tiles: keep full frame, just resize
    [switch]$Conservative,    # keep intentional white subjects (salt, sugar, etc.)
    [switch]$BackgroundOnly   # remove border-connected background only; no fringe/interior passes
)

Add-Type -AssemblyName System.Drawing

$code = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

public static class TexProc
{
    static bool Near(Color a, Color b, int tol)
    {
        return Math.Abs(a.R - b.R) <= tol && Math.Abs(a.G - b.G) <= tol && Math.Abs(a.B - b.B) <= tol;
    }

    public static Bitmap RemoveBackground(Bitmap src)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);

        // If corners already transparent, nothing to do
        var corners = new[] { bmp.GetPixel(0,0), bmp.GetPixel(bmp.Width-1,0), bmp.GetPixel(0,bmp.Height-1), bmp.GetPixel(bmp.Width-1,bmp.Height-1) };
        bool anyOpaque = false;
        foreach (var c in corners) if (c.A > 10) anyOpaque = true;
        if (!anyOpaque) return bmp;

        int tol = 28;
        var visited = new bool[bmp.Width, bmp.Height];
        var queue = new Queue<Point>();
        // seed from all border pixels matching a corner color (handles checkerboard: two shades)
        var seeds = new List<Color>();
        foreach (var c in corners) if (c.A > 10) seeds.Add(c);
        // checkerboard second shade: sample a few border points
        for (int x = 0; x < bmp.Width; x += 16) { seeds.Add(bmp.GetPixel(x, 0)); seeds.Add(bmp.GetPixel(x, bmp.Height-1)); }
        for (int y = 0; y < bmp.Height; y += 16) { seeds.Add(bmp.GetPixel(0, y)); seeds.Add(bmp.GetPixel(bmp.Width-1, y)); }

        Func<Color, bool> isBg = (c) => {
            if (c.A <= 10) return false;
            foreach (var s in seeds) if (Near(c, s, tol)) return true;
            return false;
        };

        for (int x = 0; x < bmp.Width; x++) {
            foreach (int y in new[] {0, bmp.Height-1})
                if (!visited[x,y] && isBg(bmp.GetPixel(x,y))) { queue.Enqueue(new Point(x,y)); visited[x,y] = true; }
        }
        for (int y = 0; y < bmp.Height; y++) {
            foreach (int x in new[] {0, bmp.Width-1})
                if (!visited[x,y] && isBg(bmp.GetPixel(x,y))) { queue.Enqueue(new Point(x,y)); visited[x,y] = true; }
        }

        while (queue.Count > 0) {
            var p = queue.Dequeue();
            bmp.SetPixel(p.X, p.Y, Color.Transparent);
            foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) }) {
                int nx = p.X + d.X, ny = p.Y + d.Y;
                if (nx < 0 || ny < 0 || nx >= bmp.Width || ny >= bmp.Height || visited[nx,ny]) continue;
                var c = bmp.GetPixel(nx, ny);
                if (isBg(c)) { visited[nx,ny] = true; queue.Enqueue(new Point(nx,ny)); }
            }
        }
        return bmp;
    }

    static int MinCh(Color c) { return Math.Min(c.R, Math.Min(c.G, c.B)); }
    static int MaxCh(Color c) { return Math.Max(c.R, Math.Max(c.G, c.B)); }
    static int Chroma(Color c) { return MaxCh(c) - MinCh(c); }
    static bool IsNeutralBright(Color c, int minChannel, int maxChroma)
    {
        if (c.A <= 10) return false;
        return MinCh(c) >= minChannel && Chroma(c) <= maxChroma;
    }
    static bool IsNearBlack(Color c) { return c.A > 10 && MaxCh(c) <= 28; }

    public static Bitmap CleanAlpha(Bitmap src, bool conservative)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
        int w = bmp.Width, h = bmp.Height;
        var visited = new bool[w, h];
        var queue = new Queue<Point>();
        Action<int,int> trySeed = (x, y) => {
            if (visited[x,y]) return;
            var c = bmp.GetPixel(x, y);
            if (IsNearBlack(c) || IsNeutralBright(c, 190, 55)) { visited[x,y] = true; queue.Enqueue(new Point(x,y)); }
        };
        for (int x = 0; x < w; x++) { trySeed(x, 0); trySeed(x, h - 1); }
        for (int y = 0; y < h; y++) { trySeed(0, y); trySeed(w - 1, y); }
        while (queue.Count > 0) {
            var p = queue.Dequeue();
            bmp.SetPixel(p.X, p.Y, Color.Transparent);
            foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) }) {
                int nx = p.X + d.X, ny = p.Y + d.Y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || visited[nx,ny]) continue;
                var c = bmp.GetPixel(nx, ny);
                if (IsNearBlack(c) || IsNeutralBright(c, 190, 55)) { visited[nx,ny] = true; queue.Enqueue(new Point(nx, ny)); }
            }
        }
        if (!conservative) {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (IsNeutralBright(bmp.GetPixel(x, y), 208, 45))
                        bmp.SetPixel(x, y, Color.Transparent);
        }
        int fringePasses = conservative ? 6 : 16;
        int fringeMin = conservative ? 215 : 200;
        for (int pass = 0; pass < fringePasses; pass++) {
            var toClear = new List<Point>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    var c = bmp.GetPixel(x, y);
                    if (c.A <= 10 || !IsNeutralBright(c, fringeMin, 55)) continue;
                    bool adjTrans = false;
                    foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) }) {
                        int nx = x + d.X, ny = y + d.Y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (bmp.GetPixel(nx, ny).A <= 10) { adjTrans = true; break; }
                    }
                    if (adjTrans) toClear.Add(new Point(x, y));
                }
            if (toClear.Count == 0) break;
            foreach (var p in toClear) bmp.SetPixel(p.X, p.Y, Color.Transparent);
        }
        return bmp;
    }

    public static Bitmap Trim(Bitmap bmp)
    {
        int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
                if (bmp.GetPixel(x,y).A > 10) {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
        if (maxX < 0) return bmp;
        var rect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return bmp.Clone(rect, PixelFormat.Format32bppArgb);
    }

    public static Bitmap FitCanvas(Bitmap src, int w, int h, double fill)
    {
        var outBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outBmp)) {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            double scale = Math.Min((double)w * fill / src.Width, (double)h * fill / src.Height);
            int nw = (int)Math.Round(src.Width * scale), nh = (int)Math.Round(src.Height * scale);
            g.DrawImage(src, (w - nw) / 2, (h - nh) / 2, nw, nh);
        }
        return outBmp;
    }

    public static Bitmap ResizeExact(Bitmap src, int w, int h)
    {
        var outBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outBmp)) {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, w, h);
        }
        return outBmp;
    }
}
"@
if (-not ("TexProc" -as [type])) {
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
}

if (-not (Test-Path $Source)) { Write-Error "Source not found: $Source"; exit 1 }
$destDir = Split-Path $Dest -Parent
if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }

$srcBmp = New-Object System.Drawing.Bitmap($Source)
try {
    if ($NoTrim) {
        $w = if ($TargetW -gt 0) { $TargetW } else { $Size }
        $h = if ($TargetH -gt 0) { $TargetH } else { $Size }
        $work = $srcBmp
        $disposeWork = $false
        if ($BackgroundOnly) {
            $work = [TexProc]::RemoveBackground($srcBmp)
            $disposeWork = $true
        }
        $final = [TexProc]::ResizeExact($work, $w, $h)
        $final.Save($Dest, [System.Drawing.Imaging.ImageFormat]::Png)
        $final.Dispose()
        if ($disposeWork) { $work.Dispose() }
        Write-Output "OK (noTrim): $Dest"
    } else {
        $clean = [TexProc]::RemoveBackground($srcBmp)
        if (-not $BackgroundOnly) {
            $clean = [TexProc]::CleanAlpha($clean, [bool]$Conservative)
        }
        $trimmed = [TexProc]::Trim($clean)
        $w = if ($TargetW -gt 0) { $TargetW } else { $Size }
        $h = if ($TargetH -gt 0) { $TargetH } else { $Size }
        $final = [TexProc]::FitCanvas($trimmed, $w, $h, 0.94)

        if ($Multi) {
            $base = [System.IO.Path]::Combine((Split-Path $Dest -Parent), [System.IO.Path]::GetFileNameWithoutExtension($Dest))
            $final.Save("${base}_south.png", [System.Drawing.Imaging.ImageFormat]::Png)
            $final.Save("${base}_north.png", [System.Drawing.Imaging.ImageFormat]::Png)
            $east = New-Object System.Drawing.Bitmap($final)
            $east.RotateFlip([System.Drawing.RotateFlipType]::Rotate90FlipNone)
            $east.Save("${base}_east.png", [System.Drawing.Imaging.ImageFormat]::Png)
            $west = New-Object System.Drawing.Bitmap($final)
            $west.RotateFlip([System.Drawing.RotateFlipType]::Rotate270FlipNone)
            $west.Save("${base}_west.png", [System.Drawing.Imaging.ImageFormat]::Png)
            $east.Dispose(); $west.Dispose()
            Write-Output "OK (multi): ${base}_south/_north/_east/_west"
        } else {
            $final.Save($Dest, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-Output "OK: $Dest"
        }
        $final.Dispose()
        if ($clean -ne $srcBmp) { $clean.Dispose() }
    }
} finally {
    $srcBmp.Dispose()
}
