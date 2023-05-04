#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrLayer, V, PbrShader, PbrMaterial>
{
    private readonly BufferSlice<Vector3> _tangent;

    public PbrModel(PbrLayer layer, MaybeOwn<Mesh<V>> mesh, Own<PbrMaterial> material)
        : base(layer, GetTangentBuffer(mesh, out var tangent), material)
    {
        _tangent = tangent;
    }

    private static MaybeOwn<Mesh<V>> GetTangentBuffer(MaybeOwn<Mesh<V>> mesh, out BufferSlice<Vector3> tangent)
    {
        if(mesh.AsValue().TryGetOptionalTangent(out tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        return mesh;
    }

    protected override void Render(in RenderPass pass, PbrMaterial material, Mesh<V> mesh)
    {
        var (bindGroup0, bindGroup1) = material.GetBindGroups();
        material.WriteModelUniform(GetModel());
        pass.SetBindGroup(0, bindGroup0);
        pass.SetBindGroup(1, bindGroup1);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetVertexBuffer(1, _tangent);
        pass.SetIndexBuffer(mesh.IndexBuffer);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
