#nullable enable
using System;
using System.Runtime.InteropServices;

namespace WgpuSample;

public static class SamplePrimitives
{
    public static (PosColorVertex[] Vertices, uint[] Indices) Rectangle()
    {
        var vertices = new PosColorVertex[4]
        {
            new(new(-0.5f, 0.5f, 0.0f), new(1.0f, 0.0f, 0.0f)),
            new(new(-0.5f, -0.5f, 0.0f), new(0.0f, 1.0f, 0.0f)),
            new(new(0.5f, -0.5f, 0.0f), new(0.0f, 0.0f, 1.0f)),
            new(new(0.5f, 0.5f, 0.0f), new(0.0f, 0.0f, 1.0f)),
        };
        var indices = new uint[6] { 0, 1, 2, 2, 3, 0 };
        return (Vertices: vertices, Indices: indices);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PosColorVertex
{
    public Vec3 Position;
    public Color3 Color;

    public PosColorVertex(Vec3 pos, Color3 color)
    {
        Position = pos;
        Color = color;
    }
}

[StructLayout(LayoutKind.Sequential)]
public record struct Vec3(float X, float Y, float Z);

[StructLayout(LayoutKind.Sequential)]
public record struct Color3(float R, float G, float B);
