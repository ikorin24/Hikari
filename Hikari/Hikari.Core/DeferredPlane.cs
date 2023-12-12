#nullable enable
using System;

namespace Hikari;

public sealed class DeferredPlane
{
    public static void AddRenderer(Screen screen, IGBufferProvider gBuffer)
    {
        var shader = DeferredProcessShader.Create(screen).DisposeOn(screen.Closed);
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
        var mesh = Mesh.Create<VertexSlim, ushort>(screen, vertices, indices);
        var renderer = new Renderer(mesh, material.Cast<IMaterial>());
        screen.Scheduler.Add(renderer);
        screen.Closed.Subscribe(_ => renderer.DisposeInternal());
    }
}
