#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Elffy;

public static class SamplePrimitives
{
    //public static (Vertex[] Vertices, uint[] Indices) Rectangle()
    //{
    //    var vertices = new Vertex[4]
    //    {
    //        new(new(-0.5f, 0.5f, 0.1f),  new(0, 1), new(1.0f, 0.0f, 0.0f)),
    //        new(new(-0.5f, -0.5f, 0.1f), new(0, 0), new(0.0f, 1.0f, 0.0f)),
    //        new(new(0.5f, -0.5f, 0.1f),  new(1, 0), new(0.0f, 0.0f, 1.0f)),
    //        new(new(0.5f, 0.5f, 0.1f),   new(1, 1), new(0.0f, 0.0f, 1.0f)),
    //    };
    //    var indices = new uint[6] { 0, 1, 2, 2, 3, 0 };
    //    return (Vertices: vertices, Indices: indices);
    //}

    public static (MyVertex[] Vertices, ushort[] Indices, IndexFormat IndexFormat) SampleData()
    {
        //var vertices = new Vertex[5]
        //{
        //    new Vertex
        //    {
        //        Pos = new(-0.0868241f, 0.49240386f, 0.0f),
        //        UV = new(0.4131759f, 0.00759614f),
        //    },
        //    new Vertex
        //    {
        //        Pos = new(-0.49513406f, 0.06958647f, 0.0f),
        //        UV = new(0.0048659444f, 0.43041354f),
        //    },
        //    new Vertex
        //    {
        //        Pos = new(-0.21918549f, -0.44939706f, 0.0f),
        //        UV = new (0.28081453f, 0.949397f),
        //    },
        //    new Vertex
        //    {
        //        Pos = new(0.35966998f, -0.3473291f, 0.0f),
        //        UV = new(0.85967f, 0.84732914f),
        //    },
        //    new Vertex
        //    {
        //        Pos = new(0.44147372f, 0.2347359f, 0.0f),
        //        UV = new(0.9414737f, 0.2652641f),
        //    },
        //};
        //var indices = new ushort[] { 0, 1, 4, 1, 2, 4, 2, 3, 4, /* padding */ 0 };

        const float A = 0.3f;
        var vertices = new MyVertex[4]
        {
            new MyVertex
            {
                Pos = new(-A, A, 0.0f),
                UV = new(0f, 0f),
            },
            new MyVertex
            {
                Pos = new(-A, -A, 0.0f),
                UV = new(0f, 1f),
            },
            new MyVertex
            {
                Pos = new(A, -A, 0.0f),
                UV = new (1f, 1f),
            },
            new MyVertex
            {
                Pos = new(A, A, 0.0f),
                UV = new(1f, 0f),
            },
        };
        var indices = new ushort[] { 0, 1, 2, 2, 3, 0 };
        return (Vertices: vertices, Indices: indices, IndexFormat: IndexFormat.Uint16);
    }

    public static (ColorByte[] PixelData, int Width, int Height) LoadImagePixels(string filepath)
    {
        using var stream = File.OpenRead(filepath);
        using var skBitmap = ParseToSKBitmap(stream);
        var width = skBitmap.Width;
        var height = skBitmap.Height;
        var pixelData = MemoryMarshal.Cast<byte, ColorByte>(skBitmap.GetPixelSpan()).ToArray();
        return (PixelData: pixelData, Width: width, Height: height);

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
