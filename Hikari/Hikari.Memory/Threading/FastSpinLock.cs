#nullable enable
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.Threading;

[DebuggerDisplay("{DebuggerView,nq}")]
public struct FastSpinLock
{
    private const int SYNC_ENTER = 1;
    private const int SYNC_EXIT = 0;

    private int _syncFlag;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerView => _syncFlag == SYNC_EXIT ? "unlocked" : "locked";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if(Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_ENTER) {
            SpinWait();
        }
        return;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnter()
    {
        return Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_EXIT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit()
    {
        Volatile.Write(ref _syncFlag, SYNC_EXIT);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public LockedScope Scope()
    {
        return new LockedScope(ref this);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SpinWait()
    {
        var spinner = new SpinWait();
        spinner.SpinOnce();
        while(Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_ENTER) {
            spinner.SpinOnce();
        }
    }

    public ref struct LockedScope
    {
        private ref FastSpinLock _locker;

        public LockedScope(ref FastSpinLock locker)
        {
            locker.Enter();
            _locker = ref locker;
        }

        public void Dispose()
        {
            _locker.Exit();
        }
    }
}
