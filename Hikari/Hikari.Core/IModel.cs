#nullable enable
using System;
using System.Collections.Generic;

namespace Hikari;

internal interface IModel
{
    Vector3 Position { get; set; }
    Vector3 Scale { get; set; }
    Quaternion Rotation { get; set; }
    Matrix4 GetModel(out bool isUniformScale);
}

internal interface ITreeModel<TSelf> : IModel
    where TSelf : ITreeModel<TSelf>
{
    TSelf? Parent { get; }
    IReadOnlyList<TSelf> Children { get; }
    Matrix4 GetSelfModel(out bool isUniformScale);

    void AddChild(TSelf child);
    void RemoveChild(TSelf child);

    void OnAddedToChildren(TSelf parent);
    void OnRemovedFromChildren();
}

internal record struct TreeModelImpl<T> where T : class, ITreeModel<T>
{
    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _scale;
    private T? _parent;
    private readonly List<T> _children;

    public TreeModelImpl()
    {
        _position = Vector3.Zero;
        _rotation = Quaternion.Identity;
        _scale = Vector3.One;
        _parent = null;
        _children = new();
    }

    public Vector3 Position
    {
        readonly get => _position;
        set => _position = value;
    }

    public Quaternion Rotation
    {
        readonly get => _rotation;
        set => _rotation = value;
    }

    public Vector3 Scale
    {
        readonly get => _scale;
        set => _scale = value;
    }

    public readonly T? Parent => _parent;

    public readonly IReadOnlyList<T> Children => _children;

    public readonly void AddChild(T self, T child)
    {
        if(child.Parent != null) {
            ThrowHelper.ThrowArgument("The child already has a parent");
        }
        _children.Add(child);
        child.OnAddedToChildren(self);
    }

    public readonly void RemoveChild(T child)
    {
        if(child.Parent != null) {
            if(_children.Remove(child)) {
                child.OnRemovedFromChildren();
            }
        }
    }

    public void OnAddedToChildren(T parent)
    {
        if(parent == null) {
            ArgumentNullException.ThrowIfNull(parent);
        }
        _parent = parent;
    }

    public void OnRemovedFromChildren()
    {
        if(_parent == null) {
            ThrowHelper.ThrowInvalidOperation("no parent");
        }
        _parent = null;
    }

    public readonly Matrix4 GetModel(out bool isUniformScale)
    {
        var parent = Parent;
        if(parent == null) {
            return GetSelfModel(out isUniformScale);
        }
        var mat = parent.GetModel(out var isUniformScale1) * GetSelfModel(out var isUniformScale2);
        isUniformScale = isUniformScale1 && isUniformScale2;
        return mat;
    }

    public readonly Matrix4 GetSelfModel(out bool isUniformScale)
    {
        // TODO: cache
        // TODO: thread safe
        return CalcModel(out isUniformScale);
    }

    private readonly Matrix4 CalcModel(out bool isUniformScale)
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
}
