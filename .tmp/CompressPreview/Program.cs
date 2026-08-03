using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

var src = args[0];
var dst = args[1];
var maxColors = args.Length > 2 ? int.Parse(args[2]) : 256;
using var image = Image.Load(src);
Console.WriteLine($"in {new FileInfo(src).Length} bytes {image.Width}x{image.Height}");
image.Mutate(x => x.Quantize(new WuQuantizer(new QuantizerOptions { MaxColors = maxColors })));
var encoder = new PngEncoder
{
    CompressionLevel = PngCompressionLevel.BestCompression,
    ColorType = PngColorType.Palette,
    BitDepth = PngBitDepth.Bit8
};
image.Save(dst, encoder);
Console.WriteLine($"out {new FileInfo(dst).Length} bytes colors={maxColors}");
