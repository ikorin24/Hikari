#nullable enable
using System.Runtime.InteropServices;

namespace Elffy;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Pos;
    public Vector2 UV;

    public Vertex(Vector3 pos, Vector2 uv)
    {
        Pos = pos;
        UV = uv;
    }
}
