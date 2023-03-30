#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct MyVertex : IVertex
{
    [FieldOffset(0)]
    public Vector3 Pos;
    [FieldOffset(12)]
    public Vector2 UV;

    public static uint VertexSize => (uint)sizeof(MyVertex);

    public static ReadOnlyMemory<VertexField> Fields { get; } = new[]
    {
        new VertexField(0, (uint)sizeof(Vector3), VertexFormat.Float32x3, VertexFieldSemantics.Position),
        new VertexField(12, (uint)sizeof(Vector2), VertexFormat.Float32x2, VertexFieldSemantics.UV),
    };

    public MyVertex(Vector3 pos, Vector2 uv)
    {
        Pos = pos;
        UV = uv;
    }
}
