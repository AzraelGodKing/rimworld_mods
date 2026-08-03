using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

string src = @"C:\Users\Myleek\.cursor\projects\f-Repositories-rimworld-mods\assets\SmokeToggle.png";
string dest = @"F:\Repositories\rimworld_mods\Strata\Textures\UI\Strata\SmokeToggle.png";
string destArt = @"F:\Repositories\rimworld_mods\Strata\ArtPackage\StrataArt\Textures\UI\Strata\SmokeToggle.png";

using var image = Image.Load<Rgba32>(src);
image.Mutate(x => x.Resize(128, 128));

image.ProcessPixelRows(accessor =>
{
    for (int y = 0; y < accessor.Height; y++)
    {
        Span<Rgba32> row = accessor.GetRowSpan(y);
        for (int x = 0; x < row.Length; x++)
        {
            ref Rgba32 p = ref row[x];
            if (p.R > 240 && p.G < 25 && p.B > 240)
            {
                p = default;
                continue;
            }
            if (p.R > 200 && p.G > 200 && p.B > 200
                && Math.Abs(p.R - p.G) < 20 && Math.Abs(p.G - p.B) < 20)
            {
                p = default;
            }
        }
    }
});

Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
Directory.CreateDirectory(Path.GetDirectoryName(destArt)!);
image.SaveAsPng(dest);
File.Copy(dest, destArt, overwrite: true);
var fi = new FileInfo(dest);
Console.WriteLine($"Wrote {fi.Length} bytes {image.Width}x{image.Height}");
var c0 = image[0, 0];
var cm = image[64, 64];
Console.WriteLine($"corner A={c0.A} mid A={cm.A} RGB={cm.R},{cm.G},{cm.B}");
