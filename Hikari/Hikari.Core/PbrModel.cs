#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using V = Hikari.Vertex;

namespace Hikari;

public sealed class PbrModel
    : FrameObject<PbrModel, PbrLayer, V, PbrShader, PbrMaterial>,
      ITreeModel<Vector3>
{
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;
    private Vector3 _scale = Vector3.One;
    private PbrModel? _parent;
    private readonly List<PbrModel> _children = new List<PbrModel>();
    private readonly BufferSlice _tangent;

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
    public Vector3 Scale
    {
        get => _scale;
        set => _scale = value;
    }

    public PbrModel? Parent => _parent;

    [MemberNotNullWhen(true, nameof(Parent))]
    public bool HasParent => _parent != null;

    public IReadOnlyList<PbrModel> Children => _children;

    IModel? ITreeModel<Vector3>.Parent => _parent;

    IReadOnlyList<IModel> ITreeModel<Vector3>.Children => _children;

    public PbrModel(
        PbrShader shader,
        MaybeOwn<Mesh<V>> mesh,
        MaybeOwn<Texture2D> albedo,
        MaybeOwn<Texture2D> metallicRoughness,
        MaybeOwn<Texture2D> normal)
        : base(
            mesh, PbrMaterial.Create(shader, albedo, metallicRoughness, normal))
    {
        if(Mesh.TryGetOptionalTangent(out var tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _tangent = tangent;
    }

    public void AddChild(PbrModel child)
    {
        if(child._parent != null) {
            ThrowHelper.ThrowArgument("The child already has a parent");
        }
        _children.Add(child);
        child._parent = this;
    }

    public void RemoveChild(PbrModel child)
    {
        if(child._parent != null) {
            if(_children.Remove(child)) {
                child._parent = null;
            }
        }
    }

    public Matrix4 GetModel(out bool isUniformScale)
    {
        var parent = Parent;
        if(parent == null) {
            return GetSelfModel(out isUniformScale);
        }
        var mat = parent.GetModel(out var isUniformScale1) * GetSelfModel(out var isUniformScale2);
        isUniformScale = isUniformScale1 && isUniformScale2;
        return mat;
    }

    public Matrix4 GetSelfModel(out bool isUniformScale)
    {
        // TODO: cache
        // TODO: thread safe
        return CalcModel(out isUniformScale);
    }

    private Matrix4 CalcModel(out bool isUniformScale)
    {
        var s = _scale;
        const float Epsilon = 0.0001f;

        isUniformScale =
            float.Abs(s.X - s.Y) / s.X < Epsilon &&
            float.Abs(s.X - s.Z) / s.X < Epsilon;
        return
            _position.ToTranslationMatrix4() *
            _rotation.ToMatrix4() *
            new Matrix4(
                new Vector4(s.X, 0, 0, 0),
                new Vector4(0, s.Y, 0, 0),
                new Vector4(0, 0, s.Z, 0),
                new Vector4(0, 0, 0, 1));
    }

    protected override void Render(in RenderPass pass, PbrMaterial material, Mesh<V> mesh)
    {
        material.WriteModelUniform(new()
        {
            Model = GetModel(out var isUniformScale),
            IsUniformScale = isUniformScale ? 1 : 0,
        });
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
