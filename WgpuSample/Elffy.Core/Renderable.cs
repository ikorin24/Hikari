#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public abstract class Renderable : Positionable
{
    private readonly Own<Material> _material;
    private readonly Own<Mesh> _mesh;

    public Material Material => _material.AsValue();
    public Mesh Mesh => _mesh.AsValue();

    protected Renderable(ObjectLayer layer, Own<Material> material, Own<Mesh> mesh) : base(layer)
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

public sealed class Mesh
{
    private Own<Buffer> _vertexBuffer;
    private Own<Buffer> _indexBuffer;
    private uint _indexCount;
    private IndexFormat _indexFormat;

    public Buffer VertexBuffer => _vertexBuffer.AsValue();
    public Buffer IndexBuffer => _indexBuffer.AsValue();
    public IndexFormat IndexFormat => _indexFormat;
    public uint IndexCount => _indexCount;

    private Mesh(Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _indexCount = indexCount;
        _indexFormat = indexFormat;
    }

    private static readonly Action<Mesh> _release = static self =>
    {
        self.Release();
    };

    private void Release()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    public static Own<Mesh> Create(IHostScreen screen, Own<Buffer> vertexBuffer, Own<Buffer> indexBuffer, uint indexCount, IndexFormat indexFormat)
    {
        ArgumentNullException.ThrowIfNull(screen);
        vertexBuffer.ThrowArgumentExceptionIfNone();
        indexBuffer.ThrowArgumentExceptionIfNone();
        return new Own<Mesh>(new Mesh(vertexBuffer, indexBuffer, indexCount, indexFormat), _release);
    }
}

public sealed class Model3D : Renderable
{
    private Model3D(ObjectLayer layer, Own<Material> material, Own<Mesh> mesh) : base(layer, material, mesh)
    {
    }

    public static Model3D Create(ObjectLayer layer, Own<Mesh> mesh, in BindGroupDescriptor bindGroupDesc, params IDisposable?[]? associates)
    {
        return Create(layer, mesh, new ReadOnlySpan<BindGroupDescriptor>(in bindGroupDesc), associates);
    }

    public static Model3D Create(ObjectLayer layer, Own<Mesh> mesh, ReadOnlySpan<BindGroupDescriptor> bindGroupDescs, params IDisposable?[]? associates)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var material = layer.Shader.CreateMaterial(bindGroupDescs, associates);
        var model3D = new Model3D(layer, material, mesh);
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
