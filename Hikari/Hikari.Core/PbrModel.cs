#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using V = Hikari.Vertex;

namespace Hikari;

public sealed class PbrModel
    : FrameObject<PbrModel, PbrLayer, V, PbrShader, PbrMaterial>,
      ITreeModel
{
    private TreeModelImpl _treeModelImpl;
    private readonly BufferSlice _tangent;

    public Vector3 Position
    {
        get => _treeModelImpl.Position;
        set => _treeModelImpl.Position = value;
    }
    public Quaternion Rotation
    {
        get => _treeModelImpl.Rotation;
        set => _treeModelImpl.Rotation = value;
    }
    public Vector3 Scale
    {
        get => _treeModelImpl.Scale;
        set => _treeModelImpl.Scale = value;
    }

    public ITreeModel? Parent => _treeModelImpl.Parent;

    [MemberNotNullWhen(true, nameof(Parent))]
    public bool HasParent => _treeModelImpl.Parent != null;

    public IReadOnlyList<ITreeModel> Children => _treeModelImpl.Children;

    public bool HasChildren => _treeModelImpl.Children.Count > 0;

    public PbrModel(MaybeOwn<Mesh<V>> mesh, Own<PbrMaterial> material)
        : base(mesh, material)
    {
        if(Mesh.TryGetOptionalTangent(out var tangent) == false) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _tangent = tangent;
    }

    void ITreeModel.OnAddedToChildren(ITreeModel parent) => _treeModelImpl.OnAddedToChildren(parent);

    void ITreeModel.OnRemovedFromChildren() => _treeModelImpl.OnRemovedFromChildren();

    public void AddChild(ITreeModel child) => _treeModelImpl.AddChild(this, child);

    public void RemoveChild(ITreeModel child) => _treeModelImpl.RemoveChild(child);

    public Matrix4 GetModel(out bool isUniformScale) => _treeModelImpl.GetModel(out isUniformScale);

    public Matrix4 GetSelfModel(out bool isUniformScale) => _treeModelImpl.GetSelfModel(out isUniformScale);

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
