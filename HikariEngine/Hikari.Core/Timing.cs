#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hikari.Threading;
using Hikari.Internal;

namespace Hikari;

public sealed class Timing
{
    private readonly Screen _screen;
    private readonly Queue<WorkItem> _queue;
    private FastSpinLock _queueLock;
    private EventSource<Screen> _eventSource;

    public Screen Screen => _screen;

    public Event<Screen> Event => _eventSource.Event;

    internal Timing(Screen screen)
    {
        _screen = screen;
        _queue = new Queue<WorkItem>();
        _queueLock = new FastSpinLock();
    }

    public UniTask Switch(CancellationToken ct = default)
    {
        return TimingAwaitable.Create(this, ct);
    }

    public async UniTask Delay(TimeSpan time, CancellationToken ct = default)
    {
        var elapsed = TimeSpan.Zero;
        while(true) {
            ct.ThrowIfCancellationRequested();
            if(elapsed >= time) {
                return;
            }
            else {
                elapsed += _screen.DeltaTime;
            }
            await Switch(ct);
        }
    }

    public EventSubscription<Screen> Subscribe(Action<Screen> action)
    {
        return _eventSource.Event.Subscribe(action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post(Action continuation)
    {
        if(continuation is null) { return; }
        var workItem = new WorkItem(continuation);
        _queueLock.Enter();
        try {
            _queue.Enqueue(workItem);
        }
        finally {
            _queueLock.Exit();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post(Action<object?> continuation, object? state)
    {
        if(continuation is null) { return; }
        var workItem = new WorkItem(continuation, state);
        _queueLock.Enter();
        try {
            _queue.Enqueue(workItem);
        }
        finally {
            _queueLock.Exit();
        }
    }

    internal void AbortAllEvents()
    {
        _queueLock.Enter();
        try {
            _queue.Clear();
        }
        finally {
            _queueLock.Exit();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DoQueuedEvents()
    {
        _eventSource.Invoke(_screen);

        var count = _queue.Count;
        if(count > 0) {
            DoPrivate(count);
        }
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DoPrivate(int count)
    {
        WorkItem workItem;
        for(int i = 0; i < count; i++) {
            try {
                _queueLock.Enter();
                try {
                    workItem = _queue.Dequeue();
                }
                finally {
                    _queueLock.Exit();
                }
                workItem.Invoke();
            }
            catch {
                //if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // Don't throw
            }
        }
    }

    private readonly struct WorkItem
    {
        private readonly Action<object?> _action;
        private readonly object? _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WorkItem(Action<object?> action, object? state)
        {
            _action = action;
            _state = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WorkItem(Action action)
        {
            _action = static action => SafeCast.NotNullAs<Action>(action).Invoke();
            _state = action;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke() => _action.Invoke(_state);
    }
}
