#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed class PbrModel : FrameObject, ITreeModel
{
    private TreeModelImpl _treeModelImpl;

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

    public PbrModel(MaybeOwn<Mesh> mesh, Own<PbrMaterial> material)
        : base(mesh, material.Cast<Material>())
    {
        if(Mesh.TangentBuffer is not BufferSlice) {
            throw new ArgumentException("The mesh does not have Tangent vertex buffer", nameof(mesh));
        }
        _treeModelImpl = new();
    }

    void ITreeModel.OnAddedToChildren(ITreeModel parent) => _treeModelImpl.OnAddedToChildren(parent);

    void ITreeModel.OnRemovedFromChildren() => _treeModelImpl.OnRemovedFromChildren();

    public void AddChild(ITreeModel child) => _treeModelImpl.AddChild(this, child);

    public void RemoveChild(ITreeModel child) => _treeModelImpl.RemoveChild(child);

    public Matrix4 GetModel(out bool isUniformScale) => _treeModelImpl.GetModel(out isUniformScale);

    public Matrix4 GetSelfModel(out bool isUniformScale) => _treeModelImpl.GetSelfModel(out isUniformScale);

    protected override void PrepareForRender()
    {
        var material = SafeCast.As<PbrMaterial>(Material);
        material.WriteModelUniform(new()
        {
            Model = GetModel(out var isUniformScale),
            IsUniformScale = isUniformScale ? 1 : 0,
        });
    }
}
