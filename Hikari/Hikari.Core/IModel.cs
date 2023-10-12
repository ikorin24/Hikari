#nullable enable
using System.Collections.Generic;

namespace Hikari;

public interface IModel
{
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }
    Matrix4 GetModel(out bool isUniformScale);
}

public interface IModel<TScale>
    : IModel
    where TScale : unmanaged
{
    TScale Scale { get; set; }
}

public interface ITreeModel<TScale>
    : IModel<TScale>
    where TScale : unmanaged
{
    IModel? Parent { get; }
    IReadOnlyList<IModel> Children { get; }
    Matrix4 GetSelfModel(out bool isUniformScale);
}


public interface IStrongTypedTreeModel<TParent, TChildren, TScale>
    : IModel<TScale>
    where TParent : IStrongTypedTreeModel<TParent, TChildren, TScale>
    where TScale : unmanaged
{
    TParent? Parent { get; }
    TChildren Children { get; }
    Matrix4 GetSelfModel(out bool isUniformScale);
}
