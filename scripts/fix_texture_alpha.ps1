# Strip baked white/gray backgrounds and halos from RimWorld mod PNGs.
# Safe for in-repo reprocessing: writes via temp file, preserves dimensions.
param(
    [Parameter(Mandatory=$true)][string[]]$Paths,
    [switch]$Conservative   # border flood + fringe only; keeps intentional white subjects
)

Add-Type -AssemblyName System.Drawing

$code = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class AlphaFixV4
{
    static int MinCh(Color c) { return Math.Min(c.R, Math.Min(c.G, c.B)); }
    static int MaxCh(Color c) { return Math.Max(c.R, Math.Max(c.G, c.B)); }
    static int Chroma(Color c) { return MaxCh(c) - MinCh(c); }

    static bool IsNeutralBright(Color c, int minChannel, int maxChroma)
    {
        if (c.A <= 10) return false;
        return MinCh(c) >= minChannel && Chroma(c) <= maxChroma;
    }

    static bool IsNearBlack(Color c)
    {
        return c.A > 10 && MaxCh(c) <= 28;
    }

    public static Bitmap Clean(Bitmap src, bool conservative)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);

        FloodBorder(bmp);
        if (!conservative)
            RemoveInteriorNeutralBright(bmp, 208, 45);
        else
            ClearColorSurroundedPockets(bmp, 190, 55, 40, 1500);
        SpillFringeFromTransparent(bmp, conservative ? 8 : 16, conservative ? 215 : 200, 55);
        return bmp;
    }

    static void ClearColorSurroundedPockets(Bitmap bmp, int minChannel, int maxChroma, int minEdgeChroma, int maxRegion)
    {
        int w = bmp.Width, h = bmp.Height;
        var seen = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (seen[x,y]) continue;
                var seed = bmp.GetPixel(x, y);
                if (seed.A <= 10 || !IsNeutralBright(seed, minChannel, maxChroma)) continue;

                var region = new List<Point>();
                var queue = new Queue<Point>();
                bool touchesBorder = false;
                bool touchesTransparent = false;
                bool colorfulBoundary = true;
                queue.Enqueue(new Point(x,y)); seen[x,y] = true;

                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    region.Add(p);
                    if (p.X == 0 || p.Y == 0 || p.X == w - 1 || p.Y == h - 1) touchesBorder = true;
                    foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) })
                    {
                        int nx = p.X + d.X, ny = p.Y + d.Y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var c = bmp.GetPixel(nx, ny);
                        if (c.A <= 10) { touchesTransparent = true; continue; }
                        if (IsNeutralBright(c, minChannel, maxChroma))
                        {
                            if (!seen[nx,ny]) { seen[nx,ny] = true; queue.Enqueue(new Point(nx, ny)); }
                        }
                        else if (Chroma(c) < minEdgeChroma)
                        {
                            colorfulBoundary = false;
                        }
                    }
                }

                if (touchesBorder || touchesTransparent || !colorfulBoundary || region.Count > maxRegion) continue;
                foreach (var p in region) bmp.SetPixel(p.X, p.Y, Color.Transparent);
            }
    }

    static void FloodBorder(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        var visited = new bool[w, h];
        var queue = new Queue<Point>();

        Action<int,int> trySeed = (x, y) => {
            if (visited[x,y]) return;
            var c = bmp.GetPixel(x, y);
            if (IsNearBlack(c) || IsNeutralBright(c, 190, 55))
            {
                visited[x,y] = true;
                queue.Enqueue(new Point(x,y));
            }
        };

        for (int x = 0; x < w; x++) { trySeed(x, 0); trySeed(x, h - 1); }
        for (int y = 0; y < h; y++) { trySeed(0, y); trySeed(w - 1, y); }

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            bmp.SetPixel(p.X, p.Y, Color.Transparent);
            foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) })
            {
                int nx = p.X + d.X, ny = p.Y + d.Y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || visited[nx,ny]) continue;
                var c = bmp.GetPixel(nx, ny);
                if (IsNearBlack(c) || IsNeutralBright(c, 190, 55))
                {
                    visited[nx,ny] = true;
                    queue.Enqueue(new Point(nx, ny));
                }
            }
        }
    }

    static void RemoveInteriorNeutralBright(Bitmap bmp, int minChannel, int maxChroma)
    {
        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var c = bmp.GetPixel(x, y);
                if (IsNeutralBright(c, minChannel, maxChroma))
                    bmp.SetPixel(x, y, Color.Transparent);
            }
    }

    static void SpillFringeFromTransparent(Bitmap bmp, int passes, int minChannel, int maxChroma)
    {
        int w = bmp.Width, h = bmp.Height;
        for (int pass = 0; pass < passes; pass++)
        {
            var toClear = new List<Point>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    if (c.A <= 10 || !IsNeutralBright(c, minChannel, maxChroma)) continue;
                    bool adjTrans = false;
                    foreach (var d in new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) })
                    {
                        int nx = x + d.X, ny = y + d.Y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (bmp.GetPixel(nx, ny).A <= 10) { adjTrans = true; break; }
                    }
                    if (adjTrans) toClear.Add(new Point(x, y));
                }
            if (toClear.Count == 0) break;
            foreach (var p in toClear) bmp.SetPixel(p.X, p.Y, Color.Transparent);
        }
    }
}
"@
if (-not ("AlphaFixV4" -as [type])) {
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
}

foreach ($path in $Paths) {
    if (-not (Test-Path $path)) {
        Write-Warning "Missing: $path"
        continue
    }
    $src = New-Object System.Drawing.Bitmap($path)
    $clean = [AlphaFixV4]::Clean($src, $Conservative.IsPresent)
    $src.Dispose()
    $tmp = "$path.tmp.png"
    $clean.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
    $clean.Dispose()
    Move-Item -Force $tmp $path
    Write-Output "Fixed: $path"
}
