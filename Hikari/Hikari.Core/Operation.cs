#nullable enable
using Hikari.NativeBind;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hikari;

public abstract class Operation
{
    private readonly Screen _screen;
    private LifeState _lifeState;
    private readonly SubscriptionBag _subscriptions = new();

    public Screen Screen => _screen;
    public LifeState LifeState => _lifeState;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;

    private protected Operation(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _lifeState = LifeState.New;
    }

    protected abstract void RenderShadowMap(in RenderShadowMapContext context);

    protected abstract void Execute(in OperationContext context);

    internal abstract void InvokeAlive();
    internal abstract void InvokeTerminated();
    internal abstract void InvokeFrameInit();
    internal abstract void InvokeFrameEnd();
    internal abstract void InvokeEarlyUpdate();
    internal abstract void InvokeUpdate();
    internal abstract void InvokeLateUpdate();

    internal void InvokeRenderShadowMap(in RenderShadowMapContext context) => RenderShadowMap(in context);
    internal void InvokeExecute(in OperationContext context) => Execute(in context);

    public void Terminate()
    {
        if(_screen.MainThread.IsCurrentThread) {
            Terminate(this);
        }
        else {
            _screen.Update.Post(static x =>
            {
                var self = SafeCast.NotNullAs<Operation>(x);
                Terminate(self);
            }, this);
        }
        return;

        static void Terminate(Operation self)
        {
            Debug.Assert(self._screen.MainThread.IsCurrentThread);
            if(self._lifeState == LifeState.Alive || self._lifeState == LifeState.New) {
                self._lifeState = LifeState.Terminating;
                self.Screen.Operations.Remove(self);
                self.InvokeTerminated();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateAlive()
    {
        Debug.Assert(_lifeState == LifeState.New);
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        _lifeState = LifeState.Alive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLifeStateDead()
    {
        Debug.Assert(_lifeState == LifeState.Terminating);
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        _lifeState = LifeState.Dead;
    }

    internal void InvokeRelease() => Release();

    protected virtual void Release()
    {
        _subscriptions.Dispose();
    }
}

public abstract class Operation<TSelf>
    : Operation
    where TSelf : Operation<TSelf>
{
    private EventSource<TSelf> _frameInit = new();
    private EventSource<TSelf> _frameEnd = new();
    private EventSource<TSelf> _earlyUpdate = new();
    private EventSource<TSelf> _update = new();
    private EventSource<TSelf> _lateUpdate = new();
    private EventSource<TSelf> _terminated = new();
    private EventSource<TSelf> _alive = new();
    private EventSource<TSelf> _dead = new();

    public Event<TSelf> FrameInit => _frameInit.Event;
    public Event<TSelf> EarlyUpdate => _earlyUpdate.Event;
    public Event<TSelf> Update => _update.Event;
    public Event<TSelf> LateUpdate => _lateUpdate.Event;
    public Event<TSelf> FrameEnd => _frameEnd.Event;
    public Event<TSelf> Terminated => _terminated.Event;
    public Event<TSelf> Alive => _alive.Event;
    public Event<TSelf> Dead => _dead.Event;

    /// <summary>Strong typed `this`</summary>
    protected TSelf This => SafeCast.As<TSelf>(this);

    internal sealed override void InvokeAlive() => _alive.Invoke(This);
    internal sealed override void InvokeTerminated() => _terminated.Invoke(This);
    internal sealed override void InvokeFrameInit() => _frameInit.Invoke(This);
    internal sealed override void InvokeFrameEnd() => _frameEnd.Invoke(This);
    internal sealed override void InvokeEarlyUpdate() => _earlyUpdate.Invoke(This);
    internal sealed override void InvokeUpdate() => _update.Invoke(This);
    internal sealed override void InvokeLateUpdate() => _lateUpdate.Invoke(This);

    private protected Operation(Screen screen) : base(screen)
    {
        // `this` must be of type `TSelf`.
        // This is true as long as a derived class is implemented correctly.
        if(this is not TSelf) {
            ThrowHelper.ThrowInvalidOperation("Invalid self type.");
        }
    }

    protected override void Release()
    {
        base.Release();
        _dead.Invoke(This);
        _frameInit.Clear();
        _earlyUpdate.Clear();
        _update.Clear();
        _lateUpdate.Clear();
        _frameEnd.Clear();
        _terminated.Clear();
        _alive.Clear();
        _dead.Clear();
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

    public OwnRenderPass CreateSurfaceRenderPass(
        scoped in ColorBufferInit colorInit,
        scoped in DepthStencilBufferInit depthStencilInit)
    {
        return RenderPass.Create(
            _screen,
            _surfaceView,
            _screen.DepthTexture.View.NativeRef,
            colorInit,
            depthStencilInit);
    }

    public OwnRenderPass CreateRenderPass(
        TextureView color,
        TextureView depthStencil,
        scoped in ColorBufferInit colorInit,
        scoped in DepthStencilBufferInit depthStencilInit)
    {
        return RenderPass.Create(_screen, color.NativeRef, depthStencil.NativeRef, colorInit, depthStencilInit);
    }

    public OwnRenderPass CreateRenderPass(
        TextureView color,
        scoped in ColorBufferInit colorInit)
    {
        return RenderPass.Create(_screen, color.NativeRef, colorInit);
    }
}
