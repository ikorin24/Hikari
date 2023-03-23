#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public abstract class FrameObject
{
    private readonly Screen _screen;
    private string? _name;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();

    public Screen Screen => _screen;

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    public abstract bool IsFrozen { get; set; }
    public abstract LifeState LifeState { get; }

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
    private LifeState _state;
    private bool _isFrozen;

    public TLayer Layer => _layer;
    public sealed override LifeState LifeState => _state;

    public sealed override bool IsFrozen
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
    }
}
