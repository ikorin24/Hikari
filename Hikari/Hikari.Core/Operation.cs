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
    private EventSource<Operation> _frameInit = new();
    private EventSource<Operation> _frameEnd = new();
    private EventSource<Operation> _earlyUpdate = new();
    private EventSource<Operation> _update = new();
    private EventSource<Operation> _lateUpdate = new();
    private EventSource<Operation> _terminated = new();
    private EventSource<Operation> _alive = new();
    private EventSource<Operation> _dead = new();

    public Screen Screen => _screen;
    public LifeState LifeState => _lifeState;
    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public Event<Operation> FrameInit => _frameInit.Event;
    public Event<Operation> EarlyUpdate => _earlyUpdate.Event;
    public Event<Operation> Update => _update.Event;
    public Event<Operation> LateUpdate => _lateUpdate.Event;
    public Event<Operation> FrameEnd => _frameEnd.Event;
    public Event<Operation> Terminated => _terminated.Event;
    public Event<Operation> Alive => _alive.Event;
    public Event<Operation> Dead => _dead.Event;

    private protected Operation(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _lifeState = LifeState.New;
    }

    protected abstract void RenderShadowMap(in RenderShadowMapContext context);

    protected abstract void Execute(in OperationContext context);

    internal void InvokeAlive() => _alive.Invoke(this);
    internal void InvokeFrameInit() => _frameInit.Invoke(this);
    internal void InvokeFrameEnd() => _frameEnd.Invoke(this);
    internal void InvokeRenderShadowMap(in RenderShadowMapContext context) => RenderShadowMap(in context);
    internal void InvokeExecute(in OperationContext context) => Execute(in context);
    internal void InvokeEarlyUpdate() => _earlyUpdate.Invoke(this);
    internal void InvokeUpdate() => _update.Invoke(this);
    internal void InvokeLateUpdate() => _lateUpdate.Invoke(this);

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
                self._terminated.Invoke(self);
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
        _dead.Invoke(this);
        _subscriptions.Dispose();
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
        return RenderPass.SurfaceRenderPass(
            _screen,
            _surfaceView,
            _screen.DepthTexture.View.NativeRef,
            colorInit,
            depthStencilInit);
    }
}
