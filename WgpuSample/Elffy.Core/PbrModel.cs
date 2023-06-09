#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel : Renderable<PbrModel, PbrLayer, V, PbrShader, PbrMaterial>
{
    private readonly BufferSlice _tangent;

    public PbrModel(
        PbrLayer layer,
        MaybeOwn<Mesh<V>> mesh,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal)
        : base(layer, mesh, PbrMaterial.Create(layer.Shader, albedo, metallicRoughness, normal))
    {
        var m = Mesh;
        if(m.TryGetOptionalTangent(out var tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _tangent = tangent;

        var screen = Screen;
        var mat = Material;
        var model = mat.ModelUniform;

        Dead.Subscribe(x =>
        {
            var self = SafeCast.As<PbrModel>(x);
        }).AddTo(Subscriptions);
    }

    protected override void Render(in RenderPass pass, PbrMaterial material, Mesh<V> mesh)
    {
        material.WriteModelUniform(GetModel());
        pass.SetBindGroup(0, material.BindGroup0);
        pass.SetBindGroup(1, material.BindGroup1);
        pass.SetBindGroup(2, material.BindGroup2);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetVertexBuffer(1, _tangent);
        pass.SetIndexBuffer(mesh.IndexBuffer);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }

    internal void RenderShadowMap(in RenderPass pass, Lights lights, PbrMaterial material, Mesh<V> mesh)
    {
        var directionalLight = lights.DirectionalLight;

        for(int i = 0; i < directionalLight.CascadeCount; i++) {
            pass.SetBindGroup(0, material.ShadowBindGroup0);
            pass.SetVertexBuffer(0, mesh.VertexBuffer);
            pass.SetIndexBuffer(mesh.IndexBuffer);
            //pass.SetViewport(-1, -1, 2, 2, 0, 1);           // TODO: viewport
            pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
        }
    }
}
