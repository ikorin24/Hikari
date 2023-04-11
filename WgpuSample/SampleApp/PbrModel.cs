#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrLayer, V, PbrShader, PbrMaterial>
{
    private readonly BufferSlice<Vector3> _tangent;

    public PbrModel(
        PbrLayer layer,
        Own<Mesh<V>> mesh,
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal)
        : base(
            layer,
            GetTangentBuffer(mesh, out var tangent),
            PbrMaterial.Create(layer.Shader, sampler, albedo, metallicRoughness, normal))
    {
        _tangent = tangent;
    }

    private static Own<Mesh<V>> GetTangentBuffer(Own<Mesh<V>> mesh, out BufferSlice<Vector3> tangent)
    {
        if(mesh.AsValue().TryGetOptionalTangent(out tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        return mesh;
    }

    protected override void Render(RenderPass pass)
    {
        var material = Material;
        var mesh = Mesh;
        material.WriteModelUniform(GetModel());
        pass.SetBindGroup(0, material.BindGroup0);
        pass.SetBindGroup(1, material.BindGroup1);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetVertexBuffer(1, _tangent);
        pass.SetIndexBuffer(mesh.IndexBuffer);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
