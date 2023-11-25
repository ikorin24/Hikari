#nullable enable
using System;

namespace Hikari;

public sealed class DeferredPlane
{
    public static void AddRenderer(DeferredProcessShader shader, IGBufferProvider gBuffer)
    {
        var screen = shader.Screen;
        var material = DeferredProcessMaterial.Create(shader, gBuffer);
        const float Z = 0;
        ReadOnlySpan<VertexSlim> vertices =
        [
            new(new(-1, -1, Z), new(0, 1)),
            new(new(1, -1, Z), new(1, 1)),
            new(new(1, 1, Z), new(1, 0)),
            new(new(-1, 1, Z), new(0, 0)),
        ];
        ReadOnlySpan<ushort> indices = [0, 1, 2, 2, 3, 0];
        var mesh = Mesh.Create<VertexSlim, ushort>(screen, vertices, indices).Cast<Mesh>();
        var renderer = new Renderer(mesh, [material]);
        screen.Scheduler.Add(renderer);
    }
}
