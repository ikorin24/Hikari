#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace WgpuSample;

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

[StructLayout(LayoutKind.Sequential)]
public struct PosColorVertex
{
    public Vec3 Position;
    public Vec2 UV;
    public Vec3 Color;

    public PosColorVertex(Vec3 pos, Vec2 uv, Vec3 color)
    {
        Position = pos;
        UV = uv;
        Color = color;
    }
}

[StructLayout(LayoutKind.Sequential)]
public record struct Vec2(float X, float Y);

[StructLayout(LayoutKind.Sequential)]
public record struct Vec3(float X, float Y, float Z);

[StructLayout(LayoutKind.Sequential)]
public record struct Vec4(float X, float Y, float Z, float W);

//[StructLayout(LayoutKind.Sequential)]
//public record struct Color3(float R, float G, float B);
