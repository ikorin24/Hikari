#nullable enable

namespace Elffy;

public interface IModel
{
    Vector3 Position { get; set; }
    Quaternion Rotation { get; set; }
    Matrix4 GetModel();
}

public interface IModel<TScale>
    : IModel
    where TScale : unmanaged
{
    TScale Scale { get; set; }
}

public interface ITreeModel<TChildren, TScale>
    : IModel<TScale>
    where TScale : unmanaged
{
    IModel? Parent { get; }
    TChildren Children { get; }
    Matrix4 GetSelfModel();
}


public interface IStrongTypedTreeModel<TParent, TChildren, TScale>
    : IModel<TScale>
    where TParent : IStrongTypedTreeModel<TParent, TChildren, TScale>
    where TScale : unmanaged
{
    TParent? Parent { get; }
    TChildren Children { get; }
    Matrix4 GetSelfModel();
}
