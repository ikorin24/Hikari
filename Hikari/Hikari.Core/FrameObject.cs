#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Hikari;

public sealed class FrameObject : ITreeModel
{
    private readonly Screen _screen;
    private string? _name;
    private LifeState _state;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private bool _isFrozen;
    private readonly Renderer _renderer;
    private TreeModelImpl _treeModelImpl = new TreeModelImpl();
    private EventSource<FrameObject> _update;
    private EventSource<FrameObject> _lateUpdate;
    private EventSource<FrameObject> _earlyUpdate;
    private EventSource<FrameObject> _alive;
    private EventSource<FrameObject> _terminated;
    private EventSource<FrameObject> _dead;

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
    public IReadOnlyList<ITreeModel> Children => _treeModelImpl.Children;

    public Event<FrameObject> Alive => _alive.Event;
    public Event<FrameObject> Terminated => _terminated.Event;
    public Event<FrameObject> Dead => _dead.Event;
    public Event<FrameObject> EarlyUpdate => _earlyUpdate.Event;
    public Event<FrameObject> LateUpdate => _lateUpdate.Event;
    public Event<FrameObject> Update => _update.Event;

    public Screen Screen => _screen;
    public LifeState LifeState => _state;
    public Renderer Renderer => _renderer;

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    public FrameObject(MaybeOwn<Mesh> mesh, Own<IMaterial> material) : this(mesh, [material])
    {
    }

    public FrameObject(MaybeOwn<Mesh> mesh, ImmutableArray<Own<IMaterial>> materials)
    {
        var renderer = new Renderer(mesh, materials);
        var screen = renderer.Screen;
        _screen = screen;
        _state = LifeState.New;
        _isFrozen = false;
        _renderer = renderer;
        screen.Store.Add(this);
        screen.Scheduler.Add(renderer);
    }

    void ITreeModel.OnAddedToChildren(ITreeModel parent) => _treeModelImpl.OnAddedToChildren(parent);

    void ITreeModel.OnRemovedFromChildren() => _treeModelImpl.OnRemovedFromChildren();

    public void AddChild(ITreeModel child) => _treeModelImpl.AddChild(this, child);

    public void RemoveChild(ITreeModel child) => _treeModelImpl.RemoveChild(child);

    public Matrix4 GetModel(out bool isUniformScale) => _treeModelImpl.GetModel(out isUniformScale);

    public Matrix4 GetSelfModel(out bool isUniformScale) => _treeModelImpl.GetSelfModel(out isUniformScale);

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
                self._state = LifeState.Terminating;
                self.Screen.Store.Remove(self);
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
