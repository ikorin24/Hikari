#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public abstract class Renderable<TLayer, TVertex, TShader, TMaterial, TMatArg>
    : Positionable<TLayer, TVertex, TShader, TMaterial, TMatArg>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TMatArg>
    where TVertex : unmanaged
    where TShader : Shader<TShader, TMaterial, TMatArg>
    where TMaterial : Material<TMaterial, TShader, TMatArg>
{
    private readonly Own<TMaterial> _material;
    private readonly Own<Mesh> _mesh;

    public TMaterial Material => _material.AsValue();
    public Mesh Mesh => _mesh.AsValue();

    protected Renderable(TLayer layer, Own<Mesh> mesh, Own<TMaterial> material) : base(layer)
    {
        material.ThrowArgumentExceptionIfNone();
        mesh.ThrowArgumentExceptionIfNone();
        _material = material;
        _mesh = mesh;
    }

    internal void Render(RenderPass renderPass)
    {
        var material = Material;
        var bindGroups = material.BindGroups.Span;
        var mesh = _mesh.AsValue();
        for(int i = 0; i < bindGroups.Length; i++) {
            renderPass.SetBindGroup((uint)i, bindGroups[i]);
        }

        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        renderPass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }

    internal override void OnDead()
    {
        base.OnDead();
        _material.Dispose();
        _mesh.Dispose();
    }
}

public abstract class Positionable<TLayer, TVertex, TShader, TMaterial, TMatArg>
    : FrameObject<TLayer, TVertex, TShader, TMaterial, TMatArg>
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TMatArg>
    where TVertex : unmanaged
    where TShader : Shader<TShader, TMaterial, TMatArg>
    where TMaterial : Material<TMaterial, TShader, TMatArg>
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

    protected Positionable(TLayer layer) : base(layer)
    {
    }
}

public abstract class FrameObject
{
    private readonly IHostScreen _screen;
    private string? _name;

    public IHostScreen Screen => _screen;

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    protected FrameObject(IHostScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }
}

public abstract class FrameObject<TLayer, TVertex, TShader, TMaterial, TMatArg> : FrameObject
    where TLayer : ObjectLayer<TLayer, TVertex, TShader, TMaterial, TMatArg>
    where TVertex : unmanaged
    where TShader : Shader<TShader, TMaterial, TMatArg>
    where TMaterial : Material<TMaterial, TShader, TMatArg>
{
    private readonly IHostScreen _screen;
    private readonly TLayer _layer;
    private readonly SubscriptionBag _subscriptions = new SubscriptionBag();
    private string? _name;
    private LifeState _state;
    private bool _isFrozen;

    public TLayer Layer => _layer;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public LifeState LifeState => _state;

    public bool IsFrozen
    {
        get => _isFrozen;
        set => _isFrozen = value;
    }

    protected FrameObject(TLayer layer) : base(layer.Screen)
    {
        _screen = layer.Screen;
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
