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

    void AddChild(ITreeModel child);
    void RemoveChild(ITreeModel child);

    void OnAddedToChildren(ITreeModel parent);
    void OnRemovedFromChildren();

    public IEnumerable<ITreeModel> GetAncestors()
    {
        var parent = Parent;
        while(parent != null) {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public IEnumerable<ITreeModel> GetDescendants()
    {
        foreach(var child in Children) {
            yield return child;
            foreach(var descendant in child.GetDescendants()) {
                yield return descendant;
            }
        }
    }
}
