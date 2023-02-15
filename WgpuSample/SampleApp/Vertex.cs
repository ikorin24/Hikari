#nullable enable
using System.Runtime.InteropServices;

namespace Elffy;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct Vertex : ISized
{
    [FieldOffset(0)]
    public Vector3 Pos;
    [FieldOffset(12)]
    public Vector2 UV;

    public static ulong TypeSize => (ulong)sizeof(Vertex);

    public Vertex(Vector3 pos, Vector2 uv)
    {
        Pos = pos;
        UV = uv;
    }
}

public interface ISized
{
    static abstract ulong TypeSize { get; }
}
