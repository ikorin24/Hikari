#nullable enable
using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Hikari;

internal sealed class HikariSynchronizationContext : SynchronizationContext
{
    private readonly SyncContextReceiver _syncContextReceiver;

    [ThreadStatic]
    private static SynchronizationContext? _previousSyncContext;

    public SyncContextReceiver Receiver => _syncContextReceiver;

    private Thread? DestinationThread
        => _destinationThread.TryGetTarget(out var thread) ? thread : null;
    private readonly WeakReference<Thread> _destinationThread;

    private HikariSynchronizationContext(SyncContextReceiver receiver)
    {
        _destinationThread = new WeakReference<Thread>(Thread.CurrentThread);
        _syncContextReceiver = receiver;
    }

    private HikariSynchronizationContext(Thread? destinationThread, SyncContextReceiver receiver)
    {
        _destinationThread = new WeakReference<Thread>(destinationThread!);     // It is legal that `destinationThread` is null.
        _syncContextReceiver = receiver;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _syncContextReceiver.Add(d, state);
    }

    public override SynchronizationContext CreateCopy()
    {
        return new HikariSynchronizationContext(DestinationThread, _syncContextReceiver);
    }

    public static HikariSynchronizationContext Install(out SyncContextReceiver receiver)
    {
        var context = AsyncOperationManager.SynchronizationContext;
        _previousSyncContext = context;
        receiver = new SyncContextReceiver();
        var syncContext = new HikariSynchronizationContext(receiver);
        AsyncOperationManager.SynchronizationContext = syncContext;
        return syncContext;
    }

    public static bool InstallIfNeeded(out HikariSynchronizationContext? syncContext, out SyncContextReceiver? receiver)
    {
        var context = AsyncOperationManager.SynchronizationContext;
        if(context is null || context.GetType() == typeof(SynchronizationContext)) {
            _previousSyncContext = context;
            receiver = new SyncContextReceiver();
            syncContext = new HikariSynchronizationContext(receiver);
            AsyncOperationManager.SynchronizationContext = syncContext;
            return true;
        }
        else {
            syncContext = null;
            receiver = null;
            return false;
        }
    }

    public static void Restore()
    {
        if(AsyncOperationManager.SynchronizationContext is HikariSynchronizationContext) {
            AsyncOperationManager.SynchronizationContext = _previousSyncContext!;   // It is valid to set null
            _previousSyncContext = null;
        }
    }
}

internal sealed class SyncContextReceiver
{
    private readonly ConcurrentQueue<(SendOrPostCallback callback, object? state)> _queue
        = new ConcurrentQueue<(SendOrPostCallback callback, object? state)>();

    internal SyncContextReceiver()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Add(SendOrPostCallback callback, object? state) => _queue.Enqueue((callback, state));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoAll()
    {
        var count = _queue.Count;
        if(count == 0) { return; }
        Do(count, _queue);

        static void Do(int count, ConcurrentQueue<(SendOrPostCallback callback, object? state)> queue)
        {
            while(count > 0 && queue.TryDequeue(out var item)) {
                try {
                    item.callback(item.state);
                }
                catch {
                    //if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                    //// Don't throw. (Ignore exceptions in user code)
                }
                count--;
            }
        }
    }
}
