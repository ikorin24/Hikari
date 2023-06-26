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
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetVertexBuffer(1, _tangent);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }

    internal void RenderShadowMap(in RenderPass pass, uint cascade, Lights lights, PbrMaterial material, Mesh<V> mesh)
    {
        var directionalLight = lights.DirectionalLight;
        var shadowMapSize = directionalLight.ShadowMap.Size;
        var cascadeCount = directionalLight.CascadeCount;

        var size = new Vector2u(shadowMapSize.X / cascadeCount, shadowMapSize.Y);
        var viewPort = (
            X: size.X * cascade,
            Y: 0,
            Width: size.X,
            Height: size.Y,
            MinDepth: 0,
            MaxDepth: 1);
        pass.SetBindGroup(0, material.ShadowBindGroup0);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetViewport(viewPort.X, viewPort.Y, viewPort.Width, viewPort.Height, viewPort.MinDepth, viewPort.MaxDepth);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
