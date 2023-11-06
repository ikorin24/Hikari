#nullable enable
using System;

namespace Hikari;

public sealed class DeferredPlane
    : FrameObject<DeferredPlane, VertexSlim, DeferredProcessShader, DeferredProcessMaterial>
{
    public DeferredPlane(DeferredProcessShader shader, IGBufferProvider gBufferProvider)
        : base(PlaneMesh(shader.Screen), DeferredProcessMaterial.Create(shader, gBufferProvider))
    {
    }

    private static Own<Mesh<VertexSlim>> PlaneMesh(Screen screen)
    {
        const float Z = 0;
        ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[]
        {
            new(new(-1, -1, Z), new(0, 1)),
            new(new(1, -1, Z), new(1, 1)),
            new(new(1, 1, Z), new(1, 0)),
            new(new(-1, 1, Z), new(0, 0)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        return Hikari.Mesh.Create(screen, vertices, indices);
    }

    protected override void Render(in RenderPass renderPass, ShaderPass shaderPass)
    {
        switch(shaderPass.Index) {
            case 0: {
                var mesh = Mesh;
                var material = Material;
                renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                renderPass.SetBindGroups(material.Passes[0].BindGroups);
                renderPass.DrawIndexed(mesh.IndexCount);
                return;
            }
            default: {
                throw new InvalidOperationException();
            }
        }
    }
}
