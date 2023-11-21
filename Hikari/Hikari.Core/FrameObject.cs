#nullable enable
using System.Diagnostics;

namespace Hikari;

public abstract class FrameObject : IScreenManaged
{
    private readonly Screen _screen;
    private string? _name;
    private LifeState _state;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private bool _isFrozen;
    private readonly MaybeOwn<Mesh> _mesh;
    private readonly Own<Material> _material;
    private EventSource<FrameObject> _update;
    private EventSource<FrameObject> _lateUpdate;
    private EventSource<FrameObject> _earlyUpdate;
    private EventSource<FrameObject> _alive;
    private EventSource<FrameObject> _terminated;
    private EventSource<FrameObject> _dead;

    public Event<FrameObject> Alive => _alive.Event;
    public Event<FrameObject> Terminated => _terminated.Event;
    public Event<FrameObject> Dead => _dead.Event;
    public Event<FrameObject> EarlyUpdate => _earlyUpdate.Event;
    public Event<FrameObject> LateUpdate => _lateUpdate.Event;
    public Event<FrameObject> Update => _update.Event;

    public Screen Screen => _screen;
    public LifeState LifeState => _state;
    public Mesh Mesh => _mesh.AsValue();
    public Material Material => _material.AsValue();

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

    public bool IsManaged => LifeState != LifeState.Dead;

    protected FrameObject(MaybeOwn<Mesh> mesh, Own<Material> material)
    {
        mesh.ThrowArgumentExceptionIfNone();
        var meshValue = mesh.AsValue();
        var screen = meshValue.Screen;
        _screen = screen;
        _state = LifeState.New;
        _isFrozen = false;
        _mesh = mesh;
        _material = material;
        screen.Store.Add(this);
        screen.Scheduler.Add(this);
    }

    public virtual void Validate() => IScreenManaged.DefaultValidate(this);

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
    internal void InvokePrepareForRender() => PrepareForRender();

    protected abstract void PrepareForRender();

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

    internal virtual void OnDead()
    {
        _subscriptions.Dispose();
        _mesh.Dispose();
        _dead.Invoke(this);

        _update.Clear();
        _lateUpdate.Clear();
        _earlyUpdate.Clear();
        _alive.Clear();
        _terminated.Clear();
        _dead.Clear();
    }
}

//public abstract class FrameObject<TSelf, TVertex, TShader, TMaterial>
//    : FrameObject
//    where TSelf : FrameObject<TSelf, TVertex, TShader, TMaterial>
//    where TVertex : unmanaged, IVertex
//    where TShader : Shader<TShader, TMaterial>
//    where TMaterial : Material<TMaterial, TShader>
//{
//    private readonly Own<TMaterial> _material;
//    private readonly MaybeOwn<Mesh<TVertex>> _mesh;

//    private EventSource<TSelf> _update;
//    private EventSource<TSelf> _lateUpdate;
//    private EventSource<TSelf> _earlyUpdate;
//    private EventSource<TSelf> _alive;
//    private EventSource<TSelf> _terminated;
//    private EventSource<TSelf> _dead;

//    public TShader Shader => _material.AsValue().Shader;
//    public TMaterial Material => _material.AsValue();
//    public Mesh<TVertex> Mesh => _mesh.AsValue();
//    //public bool IsOwnMesh => _mesh.IsOwn(out _);

//    //public Event<TSelf> Alive => _alive.Event;
//    //public Event<TSelf> Terminated => _terminated.Event;
//    //public Event<TSelf> Dead => _dead.Event;
//    //public Event<TSelf> EarlyUpdate => _earlyUpdate.Event;
//    //public Event<TSelf> LateUpdate => _lateUpdate.Event;
//    //public Event<TSelf> Update => _update.Event;

//    ///// <summary>Strong typed <see langword="this"/></summary>
//    //protected TSelf This => SafeCast.As<TSelf>(this);

//    protected FrameObject(
//        MaybeOwn<Mesh<TVertex>> mesh,
//        Own<TMaterial> material) : base(material.AsValue().Screen, TypeCheck)
//    {
//        var screen = Screen;
//        var shader = material.AsValue().Shader;
//        mesh.ThrowArgumentExceptionIfNone();
//        _material = material;
//        _mesh = mesh;

//        foreach(var pipeline in shader.Passes) {
//            pipeline.Register(this);
//        }
//    }

//    private static void TypeCheck(FrameObject self)
//    {
//        // `this` must be of type `TSelf`.
//        // This is true as long as a derived class is implemented correctly.
//        if(self is not TSelf) {
//            ThrowHelper.ThrowInvalidOperation("Invalid self type.");
//        }
//    }

//    internal sealed override void OnAlive()
//    {
//        Debug.Assert(LifeState == LifeState.Alive);
//        _alive.Invoke(This);
//    }

//    internal sealed override void OnTerminated()
//    {
//        Debug.Assert(LifeState == LifeState.Terminating);
//        _terminated.Invoke(This);
//    }

//    internal sealed override void OnDead()
//    {
//        _dead.Invoke(This);
//        _material.Dispose();
//        _mesh.Dispose();

//        _update.Clear();
//        _lateUpdate.Clear();
//        _earlyUpdate.Clear();
//        _alive.Clear();
//        _terminated.Clear();
//        _dead.Clear();
//    }

//    internal sealed override void InvokeEarlyUpdate() => _earlyUpdate.Invoke(This);
//    internal sealed override void InvokeUpdate() => _update.Invoke(This);
//    internal sealed override void InvokeLateUpdate() => _lateUpdate.Invoke(This);
//}
