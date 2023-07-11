#nullable enable
using System;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrModel
    : Renderable<PbrModel, PbrLayer, V, PbrShader, PbrMaterial>,
      IModel<float>
{
    private readonly BufferSlice _tangent;
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;
    private float _scale = 1f;

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }
    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }
    public float Scale
    {
        get => _scale;
        set => _scale = value;
    }

    public PbrModel(
        PbrShader shader,
        MaybeOwn<Mesh<V>> mesh,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal)
        : base(
            mesh, PbrMaterial.Create(shader, albedo, metallicRoughness, normal))
    {
        if(Mesh.TryGetOptionalTangent(out var tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _tangent = tangent;
    }

    public Matrix4 GetModel()
    {
        // TODO: cache
        // TODO: thread safe
        return CalcModel();
    }

    private Matrix4 CalcModel()
    {
        var s = _scale;
        return
            _position.ToTranslationMatrix4() *
            _rotation.ToMatrix4() *
            new Matrix4(
                new Vector4(s, 0, 0, 0),
                new Vector4(0, s, 0, 0),
                new Vector4(0, 0, s, 0),
                new Vector4(0, 0, 0, 1));
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

        pass.SetPipeline(Shader.ShadowPipeline(cascade));
        pass.SetBindGroup(0, material.ShadowBindGroup0);
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetViewport(viewPort.X, viewPort.Y, viewPort.Width, viewPort.Height, viewPort.MinDepth, viewPort.MaxDepth);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
