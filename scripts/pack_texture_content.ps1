# Scale non-transparent content to fill most of each PNG canvas (RimWorld UI + drawSize).
# Preserves canvas size and aspect of the subject; centers on transparent background.
param(
    [Parameter(Mandatory = $true)][string[]]$Paths,
    [ValidateRange(0.5, 0.98)][double]$Fill = 0.90,
    [int]$AlphaThreshold = 16
)

Add-Type -AssemblyName System.Drawing

$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class TextureContentPacker
{
    public static bool Pack(Bitmap src, double fill, int alphaThreshold, out Bitmap result, out string info)
    {
        result = null;
        info = null;
        int w = src.Width, h = src.Height;
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (src.GetPixel(x, y).A < alphaThreshold) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        if (maxX < 0)
        {
            info = "empty";
            return false;
        }

        int cw = maxX - minX + 1;
        int ch = maxY - minY + 1;
        double curFill = Math.Max((double)cw / w, (double)ch / h);
        if (curFill >= fill - 0.02)
        {
            info = string.Format("skip fill={0:P0}", curFill);
            return false;
        }

        using (var content = new Bitmap(cw, ch, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(content))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(src, new Rectangle(0, 0, cw, ch), new Rectangle(minX, minY, cw, ch), GraphicsUnit.Pixel);
            }

            double targetW = w * fill;
            double targetH = h * fill;
            double scale = Math.Min(targetW / cw, targetH / ch);
            int nw = Math.Max(1, (int)Math.Round(cw * scale));
            int nh = Math.Max(1, (int)Math.Round(ch * scale));
            int ox = (w - nw) / 2;
            int oy = (h - nh) / 2;

            result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(content, new Rectangle(ox, oy, nw, nh));
            }

            info = string.Format("{0}x{1} -> {2}x{3} scale={4:F2}", cw, ch, nw, nh, scale);
            return true;
        }
    }
}
"@

if (-not ("TextureContentPacker" -as [type])) {
    Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
}

foreach ($path in $Paths) {
    if (-not (Test-Path $path)) {
        Write-Warning "Missing: $path"
        continue
    }
    $src = New-Object System.Drawing.Bitmap($path)
    $packed = $null
    $info = $null
    $ok = [TextureContentPacker]::Pack($src, $Fill, $AlphaThreshold, [ref]$packed, [ref]$info)
    $src.Dispose()
    if (-not $ok) {
        Write-Output "Skip: $path ($info)"
        continue
    }
    $tmp = "$path.tmp.png"
    $packed.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
    $packed.Dispose()
    Move-Item -Force $tmp $path
    Write-Output "Packed: $path ($info)"
}
