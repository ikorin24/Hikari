#nullable enable
using System.Runtime.InteropServices;

namespace Elffy;

[StructLayout(LayoutKind.Sequential)]
public struct PosColorVertex
{
    public Vector3 Position;
    public Vector2 UV;
    public Vector3 Color;

    public PosColorVertex(Vector3 pos, Vector2 uv, Vector3 color)
    {
        Position = pos;
        UV = uv;
        Color = color;
    }
}

[StructLayout(LayoutKind.Sequential)]
public record struct Vector2(float X, float Y);

[StructLayout(LayoutKind.Sequential)]
public record struct Vector3(float X, float Y, float Z);

[StructLayout(LayoutKind.Sequential)]
public record struct Vector4(float X, float Y, float Z, float W);
