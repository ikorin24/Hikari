#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Hikari;

[DebuggerDisplay("{DebugView,nq}")]
public sealed class FrameObject : ITreeModel<FrameObject>
{
    private readonly Screen _screen;
    private string _name = "";
    private LifeState _state;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private bool _isFrozen;
    private readonly IRenderer _renderer;
    private TreeModelImpl<FrameObject> _treeModelImpl = new();
    private EventSource<FrameObject> _update;
    private EventSource<FrameObject> _lateUpdate;
    private EventSource<FrameObject> _earlyUpdate;
    private EventSource<FrameObject> _alive;
    private EventSource<FrameObject> _terminated;
    private EventSource<FrameObject> _dead;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => $"{nameof(FrameObject)} (Name: \"{Name}\")";

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
    public FrameObject? Parent => _treeModelImpl.Parent;
    public IReadOnlyList<FrameObject> Children => _treeModelImpl.Children;

    public Event<FrameObject> Alive => _alive.Event;
    public Event<FrameObject> Terminated => _terminated.Event;
    public Event<FrameObject> Dead => _dead.Event;
    public Event<FrameObject> EarlyUpdate => _earlyUpdate.Event;
    public Event<FrameObject> LateUpdate => _lateUpdate.Event;
    public Event<FrameObject> Update => _update.Event;

    public Screen Screen => _screen;
    public LifeState LifeState => _state;
    public Renderer? Renderer => _renderer as Renderer;

    public string Name
    {
        get => _name;
        set => _name = value ?? "";
    }

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    public bool IsVisible
    {
        get => _renderer.IsVisible;
        set
        {
            if(value == IsVisible) { return; }
            _renderer.IsVisible = value;
            RecurseVisibleHierarchy();
        }
    }

    private void RecurseVisibleHierarchy()
    {
        bool v = IsVisibleInHierarchy;
        foreach(var child in Children) {
            child.AreAllAncestorsVisible = v;
            child.RecurseVisibleHierarchy();
        }
    }

    internal bool AreAllAncestorsVisible
    {
        get => _renderer.AreAllAncestorsVisible;
        private set => _renderer.AreAllAncestorsVisible = value;
    }

    public bool IsVisibleInHierarchy => _renderer.IsVisibleInHierarchy;

    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    public FrameObject(Screen screen)
        : this(new NoneRenderer(screen), null)
    {
    }

    public FrameObject(Screen screen, FrameObjectInitArg arg)
    : this(new NoneRenderer(screen), arg)
    {
    }

    public FrameObject(MaybeOwn<Mesh> mesh, MaybeOwn<IMaterial> material)
        : this(new Renderer(mesh, [material]), null)
    {
    }

    public FrameObject(MaybeOwn<Mesh> mesh, MaybeOwn<IMaterial> material, FrameObjectInitArg arg)
        : this(new Renderer(mesh, [material]), arg)
    {
    }

    public FrameObject(MaybeOwn<Mesh> mesh, ImmutableArray<MaybeOwn<IMaterial>> materials)
        : this(new Renderer(mesh, materials), null)
    {
    }

    public FrameObject(MaybeOwn<Mesh> mesh, ImmutableArray<MaybeOwn<IMaterial>> materials, FrameObjectInitArg arg)
        : this(new Renderer(mesh, materials), arg)
    {

    }

    private FrameObject(IRenderer renderer, FrameObjectInitArg? arg)
    {
        var screen = renderer.Screen;
        _screen = screen;
        _state = LifeState.New;
        _isFrozen = false;
        _renderer = renderer;
        // ----------------------------------
        if(arg.HasValue) {
            var values = arg.Value;
            if(values.IsVisible != null) {
                IsVisible = values.IsVisible.Value;
            }
            if(values.IsFrozen != null) {
                IsFrozen = values.IsFrozen.Value;
            }
            if(values.Name != null) {
                Name = values.Name;
            }
            if(values.Position != null) {
                Position = values.Position.Value;
            }
            if(values.Rotation != null) {
                Rotation = values.Rotation.Value;
            }
            if(values.Scale != null) {
                Scale = values.Scale.Value;
            }
        }

        // *** Initialize fields and properties before here ***
        // After the following, the object is on the rendering pipeline and may be rendererd right now. (This constructor can be called from non-main thread)
        // ----------------------------------
        screen.Store.Add(this);
        if(renderer is Renderer r) {
            screen.RenderScheduler.Add(r);
        }
    }

    void ITreeNode<FrameObject>.OnAddedToChildren(FrameObject parent) => _treeModelImpl.OnAddedToChildren(parent);

    void ITreeNode<FrameObject>.OnRemovedFromChildren() => _treeModelImpl.OnRemovedFromChildren();

    public void AddChild(FrameObject child)
    {
        _treeModelImpl.AddChild(this, child);
        child.AreAllAncestorsVisible = IsVisibleInHierarchy;
        child.RecurseVisibleHierarchy();
    }

    public void RemoveChild(FrameObject child)
    {
        _treeModelImpl.RemoveChild(child);
        Debug.Assert(child.Parent == null);
        child.AreAllAncestorsVisible = true;
        child.RecurseVisibleHierarchy();
    }

    public Matrix4 GetModel(out bool isUniformScale) => _treeModelImpl.GetModel(out isUniformScale);

    public Matrix4 GetSelfModel(out bool isUniformScale) => _treeModelImpl.GetSelfModel(out isUniformScale);

    public IEnumerable<FrameObject> GetAncestors()
    {
        var parent = Parent;
        while(parent != null) {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public void UseAncestors(Action<FrameObject> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var parent = Parent;
        while(parent != null) {
            action.Invoke(parent);
            parent = parent.Parent;
        }
    }

    public void UseAncestors<T>(T state, Action<FrameObject, T> action)
        where T : allows ref struct
    {
        ArgumentNullException.ThrowIfNull(action);
        var parent = Parent;
        while(parent != null) {
            action.Invoke(parent, state);
            parent = parent.Parent;
        }
    }

    public IEnumerable<FrameObject> GetDescendants()
    {
        foreach(var child in Children) {
            yield return child;
            foreach(var descendant in child.GetDescendants()) {
                yield return descendant;
            }
        }
    }

    public void UseDescendants(Action<FrameObject> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        foreach(var child in Children) {
            action.Invoke(child);
            foreach(var descendant in child.GetDescendants()) {
                descendant.UseDescendants(action);
            }
        }
    }

    public void UseDescendants<T>(T state, Action<FrameObject, T> action)
        where T : allows ref struct
    {
        ArgumentNullException.ThrowIfNull(action);
        foreach(var child in Children) {
            action.Invoke(child, state);
            foreach(var descendant in child.GetDescendants()) {
                descendant.UseDescendants(state, action);
            }
        }
    }

    internal void SetLifeStateAlive()
    {
        Debug.Assert(_state == LifeState.New);
        _state = LifeState.Alive;
    }

    public void Terminate()
    {
        if(Screen.MainThread.IsCurrentThread) {
            Terminate(this);
        }
        else {
            Screen.Update.Post(static x =>
            {
                var self = SafeCast.NotNullAs<FrameObject>(x);
                Terminate(self);
            }, this);
        }
        return;

        static void Terminate(FrameObject self)
        {
            Debug.Assert(self.Screen.MainThread.IsCurrentThread);
            if(self._state == LifeState.Alive || self._state == LifeState.New) {
                foreach(var obj in self.GetDescendants()) {
                    obj.Terminate();
                }
                self._state = LifeState.Terminating;
                self.Screen.Store.Remove(self);
                var renderer = self.Renderer;
                if(renderer != null) {
                    self.Screen.RenderScheduler.RemoveRenderer(renderer);
                }
                self.OnTerminated();
            }
        }
    }

    internal void InvokeEarlyUpdate() => _earlyUpdate.Invoke(this);
    internal void InvokeUpdate() => _update.Invoke(this);
    internal void InvokeLateUpdate() => _lateUpdate.Invoke(this);

    internal void OnAlive()
    {
        Debug.Assert(LifeState == LifeState.Alive);
        _alive.Invoke(this);
    }

    internal void OnTerminated()
    {
        Debug.Assert(LifeState == LifeState.Terminating);
        _terminated.Invoke(this);
    }

    internal void SetLifeStateDead()
    {
        Debug.Assert(_state == LifeState.Terminating);
        _state = LifeState.Dead;
    }

    internal void OnDead()
    {
        _subscriptions.Dispose();
        _renderer.DisposeInternal();
        _dead.Invoke(this);

        _update.Clear();
        _lateUpdate.Clear();
        _earlyUpdate.Clear();
        _alive.Clear();
        _terminated.Clear();
        _dead.Clear();
    }
}

public readonly record struct FrameObjectInitArg
{
    public bool? IsVisible { get; init; }
    public bool? IsFrozen { get; init; }
    public string? Name { get; init; }
    public Vector3? Position { get; init; }
    public Quaternion? Rotation { get; init; }
    public Vector3? Scale { get; init; }
}
