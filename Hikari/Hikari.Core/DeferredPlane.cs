#nullable enable
using System;

namespace Hikari;

public sealed class DeferredPlane : FrameObject
{
    public DeferredPlane(DeferredProcessShader shader, IGBufferProvider gBufferProvider)
        : base(PlaneMesh(shader.Screen), [DeferredProcessMaterial.Create(shader, gBufferProvider)])
    {
    }

    private static Own<Mesh> PlaneMesh(Screen screen)
    {
        const float Z = 0;
        ReadOnlySpan<VertexSlim> vertices =
        [
            new(new(-1, -1, Z), new(0, 1)),
            new(new(1, -1, Z), new(1, 1)),
            new(new(1, 1, Z), new(1, 0)),
            new(new(-1, 1, Z), new(0, 0)),
        ];
        ReadOnlySpan<ushort> indices = [0, 1, 2, 2, 3, 0];
        return Mesh.Create<VertexSlim, ushort>(screen, vertices, indices).Cast<Mesh>();
    }

    protected override void PrepareForRender()
    {
        // nop
    }
}
