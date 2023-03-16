#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public abstract class Renderable : Positionable
{
    private readonly Own<Material> _material;

    public Material Material => _material.AsValue();

    protected Renderable(ObjectLayer layer, Own<Material> material) : base(layer)
    {
        ArgumentNullException.ThrowIfNull(material);
        _material = material;
    }
}

public sealed class Model3D : Renderable
{
    private Model3D(ObjectLayer layer, Own<Material> material) : base(layer, material)
    {
    }

    public static Model3D Create(ObjectLayer layer, in BindGroupDescriptor bindGroupDesc)
    {
        return Create(layer, new ReadOnlySpan<BindGroupDescriptor>(in bindGroupDesc));
    }

    public static Model3D Create(ObjectLayer layer, ReadOnlySpan<BindGroupDescriptor> bindGroupDescs)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var material = layer.Shader.CreateMaterial(bindGroupDescs);
        var model3D = new Model3D(layer, material);
        layer.Add(model3D);
        return model3D;
    }
}

public abstract class Positionable : FrameObject
{
    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _scale;

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public Vector3 Scale
    {
        get => _scale;
        set => _scale = value;
    }

    //private Trs<Positionable> _trs = new Trs<Positionable>();
    //private ArrayPooledListCore<Positionable> _childrenCore = new();
    //private EventSource<Positionable> _parentChanged;
    //private Matrix4? _modelCache;
    //private Positionable? _parent;
    protected Positionable(ObjectLayer layer) : base(layer)
    {
    }
}

public abstract class FrameObject
{
    private readonly IHostScreen _screen;
    private readonly ObjectLayer _layer;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    //private AsyncEventSource<FrameObject> _activating;
    //private AsyncEventSource<FrameObject> _terminating;
    //private EventSource<FrameObject> _update;
    //private EventSource<FrameObject> _lateUpdate;
    //private EventSource<FrameObject> _earlyUpdate;
    //private EventSource<FrameObject> _alive;
    //private EventSource<FrameObject> _dead;
    private string? _name;
    private LifeState _state;
    private bool _isFrozen;

    public IHostScreen Screen => _screen;
    public ObjectLayer Layer => _layer;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    public LifeState LifeState => _state;

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    protected FrameObject(ObjectLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        _screen = layer.Screen;
        _layer = layer;
        _isFrozen = false;
        _state = LifeState.New;
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
