#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

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

public abstract class FrameObject<TLayer, TVertex, TShader, TMaterial> : FrameObject
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly TLayer _layer;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();

    private EventSource<FrameObject> _update;
    private EventSource<FrameObject> _lateUpdate;
    private EventSource<FrameObject> _earlyUpdate;

    private LifeState _state;
    private bool _isFrozen;

    public TLayer Layer => _layer;
    public sealed override LifeState LifeState => _state;
    public sealed override SubscriptionRegister Subscriptions => _subscriptions.Register;

    public Event<FrameObject> EarlyUpdate => _earlyUpdate.Event;

    public Event<FrameObject> LateUpdate => _lateUpdate.Event;

    public Event<FrameObject> Update => _update.Event;

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    protected FrameObject(TLayer layer) : base(layer.Screen)
    {
        _layer = layer;
        _isFrozen = false;
        _state = LifeState.New;
        layer.Add(this);
    }

    public virtual void InvokeEarlyUpdate() => _earlyUpdate.Invoke(this);
    public virtual void InvokeUpdate() => _update.Invoke(this);
    public virtual void InvokeLateUpdate() => _lateUpdate.Invoke(this);

    internal void SetLifeStateAlive()
    {
        Debug.Assert(_state == LifeState.New);
        _state = LifeState.Alive;
    }

    internal void SetLifeStateDead()
    {
        Debug.Assert(_state == LifeState.Terminating);
        _state = LifeState.Dead;
    }

    internal virtual void OnDead()
    {
        Debug.Assert(_state == LifeState.Dead);
        _subscriptions.Dispose();
    }
}
