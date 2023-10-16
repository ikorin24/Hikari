#nullable enable
using System.Collections.Generic;

namespace Hikari;

public interface IModel
{
    Vector3 Position { get; set; }
    Vector3 Scale { get; set; }
    Quaternion Rotation { get; set; }
    Matrix4 GetModel(out bool isUniformScale);
}

public interface ITreeModel : IModel
{
    ITreeModel? Parent { get; }
    IReadOnlyList<ITreeModel> Children { get; }
    Matrix4 GetSelfModel(out bool isUniformScale);
    void OnAddedToChildren(ITreeModel parent);
    void OnRemovedFromChildren();
}
