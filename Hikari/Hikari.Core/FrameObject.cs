#nullable enable
using System;
using System.Diagnostics;

namespace Hikari;

public abstract class FrameObject : IScreenManaged
{
    private readonly Screen _screen;
    private string? _name;
    private LifeState _state;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private bool _isFrozen;

    public Screen Screen => _screen;
    public LifeState LifeState => _state;

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

    private protected FrameObject(Screen screen, Action<FrameObject> typeCheck)
    {
        ArgumentNullException.ThrowIfNull(screen);
        typeCheck(this);
        _screen = screen;
        _state = LifeState.New;
        _isFrozen = false;
        screen.Store.Add(this);
    }

    public virtual void Validate() => IScreenManaged.DefaultValidate(this);

    internal void OnRender(in RenderPass renderPass, ShaderPass shaderPass) => Render(renderPass, shaderPass);

    protected abstract void Render(in RenderPass renderPass, ShaderPass shaderPass);

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

    internal abstract void InvokeEarlyUpdate();
    internal abstract void InvokeUpdate();
    internal abstract void InvokeLateUpdate();

    internal abstract void OnAlive();

    internal abstract void OnTerminated();

    internal void SetLifeStateDead()
    {
        Debug.Assert(_state == LifeState.Terminating);
        _state = LifeState.Dead;
    }

    internal virtual void OnDead()
    {
        _subscriptions.Dispose();
    }
}

public abstract class FrameObject<TSelf, TVertex, TShader, TMaterial>
    : FrameObject
    where TSelf : FrameObject<TSelf, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly Own<TMaterial> _material;
    private readonly MaybeOwn<Mesh<TVertex>> _mesh;

    private EventSource<TSelf> _update;
    private EventSource<TSelf> _lateUpdate;
    private EventSource<TSelf> _earlyUpdate;
    private EventSource<TSelf> _alive;
    private EventSource<TSelf> _terminated;
    private EventSource<TSelf> _dead;

    public TShader Shader => _material.AsValue().Shader;
    public TMaterial Material => _material.AsValue();
    public Mesh<TVertex> Mesh => _mesh.AsValue();
    public bool IsOwnMesh => _mesh.IsOwn(out _);

    public Event<TSelf> Alive => _alive.Event;
    public Event<TSelf> Terminated => _terminated.Event;
    public Event<TSelf> Dead => _dead.Event;
    public Event<TSelf> EarlyUpdate => _earlyUpdate.Event;
    public Event<TSelf> LateUpdate => _lateUpdate.Event;
    public Event<TSelf> Update => _update.Event;

    /// <summary>Strong typed <see langword="this"/></summary>
    protected TSelf This => SafeCast.As<TSelf>(this);

    protected FrameObject(
        MaybeOwn<Mesh<TVertex>> mesh,
        Own<TMaterial> material) : base(material.AsValue().Screen, TypeCheck)
    {
        var screen = Screen;
        var shader = material.AsValue().Shader;
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;

        foreach(var pipeline in shader.Passes) {
            pipeline.Register(this);
        }
    }

    private static void TypeCheck(FrameObject self)
    {
        // `this` must be of type `TSelf`.
        // This is true as long as a derived class is implemented correctly.
        if(self is not TSelf) {
            ThrowHelper.ThrowInvalidOperation("Invalid self type.");
        }
    }

    internal sealed override void OnAlive()
    {
        Debug.Assert(LifeState == LifeState.Alive);
        _alive.Invoke(This);
    }

    internal sealed override void OnTerminated()
    {
        Debug.Assert(LifeState == LifeState.Terminating);
        _terminated.Invoke(This);
    }

    internal sealed override void OnDead()
    {
        _dead.Invoke(This);
        _material.Dispose();
        _mesh.Dispose();

        _update.Clear();
        _lateUpdate.Clear();
        _earlyUpdate.Clear();
        _alive.Clear();
        _terminated.Clear();
        _dead.Clear();
    }

    internal sealed override void InvokeEarlyUpdate() => _earlyUpdate.Invoke(This);
    internal sealed override void InvokeUpdate() => _update.Invoke(This);
    internal sealed override void InvokeLateUpdate() => _lateUpdate.Invoke(This);
}
