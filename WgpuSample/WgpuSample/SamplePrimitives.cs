#nullable enable
using System.IO;
using SkiaSharp;

namespace Elffy;

public static class SamplePrimitives
{
    public static (PosColorVertex[] Vertices, uint[] Indices) Rectangle()
    {
        var vertices = new PosColorVertex[4]
        {
            new(new(-0.5f, 0.5f, 0.0f),  new(0, 1), new(1.0f, 0.0f, 0.0f)),
            new(new(-0.5f, -0.5f, 0.0f), new(0, 0), new(0.0f, 1.0f, 0.0f)),
            new(new(0.5f, -0.5f, 0.0f),  new(1, 0), new(0.0f, 0.0f, 1.0f)),
            new(new(0.5f, 0.5f, 0.0f),   new(1, 1), new(0.0f, 0.0f, 1.0f)),
        };
        var indices = new uint[6] { 0, 1, 2, 2, 3, 0 };
        return (Vertices: vertices, Indices: indices);
    }

    public static byte[] LoadImagePixels(string filepath, out uint width, out uint height)
    {
        using var stream = File.OpenRead(filepath);
        using var skBitmap = ParseToSKBitmap(stream);
        width = (uint)skBitmap.Width;
        height = (uint)skBitmap.Height;
        return skBitmap.GetPixelSpan().ToArray();

        static SKBitmap ParseToSKBitmap(Stream stream)
        {
            using var codec = SKCodec.Create(stream);
            var info = codec.Info;
            info.ColorType = SKColorType.Rgba8888;
            info.AlphaType = SKAlphaType.Unpremul;
            return SKBitmap.Decode(codec, info);
        }
    }
}
