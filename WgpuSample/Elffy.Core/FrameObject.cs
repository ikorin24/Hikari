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
}

public abstract class FrameObject<TLayer, TVertex, TShader, TMaterial> : FrameObject
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial>
    where TVertex : unmanaged, IVertex<TVertex>
    where TShader : Shader<TShader, TMaterial>
    where TMaterial : Material<TMaterial, TShader>
{
    private readonly TLayer _layer;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private LifeState _state;
    private bool _isFrozen;

    public TLayer Layer => _layer;
    public sealed override LifeState LifeState => _state;
    public sealed override SubscriptionRegister Subscriptions => _subscriptions.Register;
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
