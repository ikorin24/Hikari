#nullable enable
using System;
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
    }

    protected abstract void Execute(in CommandEncoder encoder);

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
    internal void InvokeExecute(in CommandEncoder encoder) => Execute(in encoder);
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
