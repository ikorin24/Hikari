#nullable enable
using Elffy.NativeBind;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

public abstract class Operation
{
    private readonly Screen _screen;
    private LifeState _lifeState;
    private readonly int _sortOrder;
    private readonly SubscriptionBag _subscriptions = new();
    private EventSource<Operation> _onDead = new();

    public Screen Screen => _screen;
    public LifeState LifeState => _lifeState;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public int SortOrder => _sortOrder;
    public Event<Operation> Dead => _onDead.Event;

    protected Operation(Screen screen, int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _lifeState = LifeState.New;
        _sortOrder = sortOrder;
        screen.Operations.Add(this);
    }

    protected abstract void RenderShadowMap(in RenderShadowMapContext context);

    protected abstract void Execute(in OperationContext context);

    protected abstract void EarlyUpdate();
    protected abstract void Update();
    protected abstract void LateUpdate();

    protected virtual void FrameInit()
    {
        // nop
    }
    protected virtual void FrameEnd()
    {
        // nop
    }

    internal void InvokeFrameInit() => FrameInit();
    internal void InvokeFrameEnd() => FrameEnd();
    internal void InvokeRenderShadowMap(in RenderShadowMapContext context) => RenderShadowMap(in context);
    internal void InvokeExecute(in OperationContext context) => Execute(in context);
    internal void InvokeEarlyUpdate() => EarlyUpdate();
    internal void InvokeUpdate() => Update();
    internal void InvokeLateUpdate() => LateUpdate();

    public bool Terminate()
    {
        var currentState = InterlockedEx.CompareExchange(ref _lifeState, LifeState.Terminating, LifeState.Alive);
        if(currentState != LifeState.Alive) {
            return false;
        }
        Screen.Operations.Remove(this);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateAlive()
    {
        Debug.Assert(_lifeState == LifeState.New);
        _lifeState = LifeState.Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateDead()
    {
        Debug.Assert(_lifeState == LifeState.Terminating);
        _lifeState = LifeState.Dead;
    }

    internal void InvokeRelease() => Release();

    protected virtual void Release()
    {
        _onDead.Invoke(this);
        _subscriptions.Dispose();
    }
}

public readonly ref struct OperationContext
{
    private readonly Screen _screen;
    private readonly Rust.Ref<Wgpu.TextureView> _surfaceView;

    public Screen Screen => _screen;
    internal readonly Rust.Ref<Wgpu.TextureView> SurfaceView => _surfaceView;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OperationContext() => throw new NotSupportedException("Don't use default constructor.");

    internal OperationContext(Screen screen, Rust.Ref<Wgpu.TextureView> surfaceView)
    {
        _screen = screen;
        _surfaceView = surfaceView;
    }

    public Own<RenderPass> CreateSurfaceRenderPass()
    {
        return RenderPass.SurfaceRenderPass(_screen, _surfaceView);
    }
}
