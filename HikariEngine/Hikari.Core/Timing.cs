#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hikari.Threading;
using Hikari.Internal;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
        var workItem = WorkItem.New(continuation);
        _queueLock.Enter();
        try {
            _queue.Enqueue(workItem);
        }
        finally {
            _queueLock.Exit();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(Action<T> continuation, T state)
    {
        if(continuation is null) { return; }
        var workItem = WorkItem.New(continuation, state);
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
        private readonly Action<Delegate, object?, Union> _caller;
        private readonly Delegate _action;
        private readonly object? _state0;
        private readonly Union _state1;

        private static readonly Action<Delegate, object?, Union> NoArgCaller = static (action, _, _) => SafeCast.NotNullAs<Action>(action).Invoke();

        private WorkItem(Action<Delegate, object?, Union> caller, Delegate action, object? s0, Union s1)
        {
            _caller = caller;
            _action = action;
            _state0 = s0;
            _state1 = s1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WorkItem New(Action action)
        {
            return new WorkItem(NoArgCaller, action, null, Union.None);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WorkItem New<T>(Action<T> action, T state)
        {
            if(typeof(T).IsValueType == false) {
                return new WorkItem(RefTypeCache<T>.Caller, action, state, Union.None);
            }
            else {
                if(RuntimeHelpers.IsReferenceOrContainsReferences<T>() == false && Unsafe.SizeOf<T>() <= Unsafe.SizeOf<Union>()) {
                    var (caller, union) = Unsafe.SizeOf<T>() switch
                    {
                        1 => (SizedTypeCache1<T>.Caller, new Union { Size1 = Unsafe.As<T, Size1>(ref state) }),
                        2 => (SizedTypeCache2<T>.Caller, new Union { Size2 = Unsafe.As<T, Size2>(ref state) }),
                        3 => (SizedTypeCache3<T>.Caller, new Union { Size3 = Unsafe.As<T, Size3>(ref state) }),
                        4 => (SizedTypeCache4<T>.Caller, new Union { Size4 = Unsafe.As<T, Size4>(ref state) }),
                        5 => (SizedTypeCache5<T>.Caller, new Union { Size5 = Unsafe.As<T, Size5>(ref state) }),
                        6 => (SizedTypeCache6<T>.Caller, new Union { Size6 = Unsafe.As<T, Size6>(ref state) }),
                        7 => (SizedTypeCache7<T>.Caller, new Union { Size7 = Unsafe.As<T, Size7>(ref state) }),
                        8 => (SizedTypeCache8<T>.Caller, new Union { Size8 = Unsafe.As<T, Size8>(ref state) }),
                        _ => throw new UnreachableException($"unexpected type size: {Unsafe.SizeOf<T>()}"),
                    };
                    return new WorkItem(caller, action, null, union);
                }
                else {
                    // boxing
                    object? s0 = (object?)state;
                    return new WorkItem(ValueTypeCache<T>.Caller, action, s0, Union.None);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke() => _caller.Invoke(_action, _state0, _state1);

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        struct Size1 { }
        [StructLayout(LayoutKind.Sequential, Size = 2)]
        struct Size2 { }
        [StructLayout(LayoutKind.Sequential, Size = 3)]
        struct Size3 { }
        [StructLayout(LayoutKind.Sequential, Size = 4)]
        struct Size4 { }
        [StructLayout(LayoutKind.Sequential, Size = 5)]
        struct Size5 { }
        [StructLayout(LayoutKind.Sequential, Size = 6)]
        struct Size6 { }
        [StructLayout(LayoutKind.Sequential, Size = 7)]
        struct Size7 { }
        [StructLayout(LayoutKind.Sequential, Size = 8)]
        struct Size8 { }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        struct Union
        {
            [FieldOffset(0)]
            public Size1 Size1;
            [FieldOffset(0)]
            public Size2 Size2;
            [FieldOffset(0)]
            public Size3 Size3;
            [FieldOffset(0)]
            public Size4 Size4;
            [FieldOffset(0)]
            public Size5 Size5;
            [FieldOffset(0)]
            public Size6 Size6;
            [FieldOffset(0)]
            public Size7 Size7;
            [FieldOffset(0)]
            public Size8 Size8;

            public static Union None => default;
        }

        private sealed class SizedTypeCache1<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size1, T>(ref s1.Size1));
        }
        private sealed class SizedTypeCache2<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size2, T>(ref s1.Size2));
        }
        private sealed class SizedTypeCache3<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size3, T>(ref s1.Size3));
        }
        private sealed class SizedTypeCache4<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size4, T>(ref s1.Size4));
        }
        private sealed class SizedTypeCache5<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size5, T>(ref s1.Size5));
        }
        private sealed class SizedTypeCache6<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size6, T>(ref s1.Size6));
        }
        private sealed class SizedTypeCache7<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size7, T>(ref s1.Size7));
        }
        private sealed class SizedTypeCache8<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, _, s1) => SafeCast.As<Action<T>>(action).Invoke(Unsafe.As<Size8, T>(ref s1.Size8));
        }
        private sealed class ValueTypeCache<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, s0, _) =>
            {
                // unboxing
                var arg = (T)s0!;
                SafeCast.As<Action<T>>(action).Invoke(arg);
            };
        }
        private sealed class RefTypeCache<T>
        {
            public static readonly Action<Delegate, object?, Union> Caller = static (action, s0, _) =>
            {
                var arg = Unsafe.As<object?, T>(ref s0);
                SafeCast.As<Action<T>>(action).Invoke(arg);
            };
        }
    }
}
