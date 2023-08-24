#nullable enable
using System;
using System.Diagnostics;

namespace Hikari;

public abstract class FrameObject : IScreenManaged
{
    private readonly Screen _screen;
    private string? _name;

    public Screen Screen => _screen;
    public abstract LifeState LifeState { get; }

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    public abstract SubscriptionRegister Subscriptions { get; }

    public bool IsManaged => LifeState != LifeState.Dead;

    protected FrameObject(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }

    public virtual void Validate() => IScreenManaged.DefaultValidate(this);
}

public abstract class FrameObject<TSelf, TLayer, TVertex, TShader, TMaterial>
    : FrameObject
    where TSelf : FrameObject<TSelf, TLayer, TVertex, TShader, TMaterial>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TSelf>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial, TLayer>
    where TMaterial : Material<TMaterial, TShader, TLayer>
{
    private readonly TLayer _layer;
    private readonly Own<TMaterial> _material;
    private readonly MaybeOwn<Mesh<TVertex>> _mesh;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private ThreadId _threadId;

    private EventSource<TSelf> _update;
    private EventSource<TSelf> _lateUpdate;
    private EventSource<TSelf> _earlyUpdate;
    private EventSource<TSelf> _alive;
    private EventSource<TSelf> _terminated;
    private EventSource<TSelf> _dead;

    private LifeState _state;
    private bool _isFrozen;

    public TLayer Layer => _layer;
    public TShader Shader => _material.AsValue().Shader;
    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();
    public bool IsOwnMesh => _mesh.IsOwn(out _);
    public sealed override LifeState LifeState => _state;
    public sealed override SubscriptionRegister Subscriptions => _subscriptions.Register;

    public Event<TSelf> Alive => _alive.Event;
    public Event<TSelf> Terminated => _terminated.Event;
    public Event<TSelf> Dead => _dead.Event;
    public Event<TSelf> EarlyUpdate => _earlyUpdate.Event;
    public Event<TSelf> LateUpdate => _lateUpdate.Event;
    public Event<TSelf> Update => _update.Event;

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    /// <summary>Strong typed `this`</summary>
    protected TSelf Self => SafeCast.As<TSelf>(this);

    protected FrameObject(
        MaybeOwn<Mesh<TVertex>> mesh,
        Own<TMaterial> material) : base(material.AsValue().Screen)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
        _layer = material.AsValue().Operation;
        _isFrozen = false;
        _state = LifeState.New;

        // `this` must be of type `TSelf`.
        // This is true as long as a derived class is implemented correctly.
        if(this is TSelf self) {
            _layer.Add(self);
        }
        else {
            ThrowHelper.ThrowInvalidOperation("Invalid self type.");
        }
    }

    public bool Terminate()
    {
        var currentState = InterlockedEx.CompareExchange(ref _state, LifeState.Terminating, LifeState.Alive);
        if(currentState != LifeState.Alive) {
            return false;
        }

        if(_threadId.IsCurrentThread) {
            Terminate(Self);
        }
        else {
            Screen.Update.Post(static x =>
            {
                var self = SafeCast.NotNullAs<TSelf>(x);
                Terminate(self);
            }, Self);
        }
        return true;

        static void Terminate(TSelf self)
        {
            Debug.Assert(self._threadId.IsCurrentThread);
            self._layer.Remove(self);
            self._terminated.Invoke(self);
        }
    }

    internal void OnRender(in RenderPass pass)
    {
        pass.SetPipeline(Shader.Pipeline);
        Render(pass, _material.AsValue(), _mesh.AsValue());
    }

    public virtual void InvokeEarlyUpdate() => _earlyUpdate.Invoke(Self);
    public virtual void InvokeUpdate() => _update.Invoke(Self);
    public virtual void InvokeLateUpdate() => _lateUpdate.Invoke(Self);

    internal void SetLifeStateAlive()
    {
        Debug.Assert(_state == LifeState.New);
        _state = LifeState.Alive;
        Debug.Assert(_threadId.IsNone);
        _threadId = ThreadId.CurrentThread();
    }

    internal void OnAlive()
    {
        Debug.Assert(_state == LifeState.Alive);
        _alive.Invoke(Self);
    }

    internal void SetLifeStateDead()
    {
        Debug.Assert(_state == LifeState.Terminating);
        _state = LifeState.Dead;
    }

    internal void OnDead()
    {
        _dead.Invoke(Self);
        _material.Dispose();
        _mesh.Dispose();
        _subscriptions.Dispose();

        _update.Clear();
        _lateUpdate.Clear();
        _earlyUpdate.Clear();
        _alive.Clear();
        _terminated.Clear();
        _dead.Clear();
    }

    protected abstract void Render(in RenderPass pass, TMaterial material, Mesh<TVertex> mesh);
}
