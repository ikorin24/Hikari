#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Internal;

internal sealed class TimingAwaitable : IUniTaskSource, IChainInstancePooled<TimingAwaitable>
{
    private static Int16TokenFactory _tokenFactory;

    private TimingAwaitable? _next;
    private TimingAwaitableCore _awaitableCore;

    public ref TimingAwaitable? NextPooled => ref _next;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimingAwaitable(Timing? timingPoint, short token, CancellationToken cancellationToken)
    {
        _awaitableCore = new(timingPoint, token, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult(short token)
    {
        _awaitableCore.GetResult(token);
        Return(this);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTaskStatus GetStatus(short token) => _awaitableCore.GetStatus(token);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object?> continuation, object? state, short token) => _awaitableCore.OnCompleted(continuation, state, token);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTaskStatus UnsafeGetStatus() => _awaitableCore.UnsafeGetStatus();

    internal static UniTask Create(Timing? queue, CancellationToken ct)
    {
        var token = _tokenFactory.CreateToken();
        if(ChainInstancePool<TimingAwaitable>.TryGetInstanceFast(out var taskSource)) {
            taskSource._awaitableCore = new TimingAwaitableCore(queue, token, ct);
        }
        else {
            taskSource = new TimingAwaitable(queue, token, ct);
        }
        return new UniTask(taskSource, token);
    }

    private static void Return(TimingAwaitable source)
    {
        source._awaitableCore = default;
        ChainInstancePool<TimingAwaitable>.ReturnInstanceFast(source);
    }
}

internal struct TimingAwaitableCore
{
    private static readonly object _completedGuard = new object();

    private object? _queue;       // AsyncEventQueue instance (pending) || '_completedGuard' (completed) || null (after completed)
    private CancellationToken _cancellationToken;
    private short _token;

    public TimingAwaitableCore(Timing? queue, short token, CancellationToken ct)
    {
        _queue = queue ?? _completedGuard;
        _cancellationToken = ct;
        _token = token;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult(short token)
    {
        ValidateToken(token);
        if(Interlocked.CompareExchange(ref _queue, null, _completedGuard) == _completedGuard) {
            // success
            return;
        }
        else {
            NotSuccess();
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private void NotSuccess()
    {
        var status = UnsafeGetStatus();
        Debug.Assert(status != UniTaskStatus.Succeeded);
        throw status switch
        {
            UniTaskStatus.Pending => new InvalidOperationException("Not yet completed, UniTask only allow to use await."),
            UniTaskStatus.Canceled => new OperationCanceledException(),
            _ => new UnreachableException("Invalid status. How did you get here?"),
        };
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTaskStatus GetStatus(short token)
    {
        ValidateToken(token);
        return UnsafeGetStatus();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object?> continuation, object? state, short token)
    {
        ValidateToken(token);
        var queue = Interlocked.Exchange(ref _queue, _completedGuard);
        if(ReferenceEquals(queue, _completedGuard) || queue is null) {
            _queue = null;
            ThrowHelper.ThrowInvalidOperation("Can not await twice");
        }
        else {
            SafeCast.As<Timing>(queue).Post(continuation, state);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTaskStatus UnsafeGetStatus()
    {
        if(ReferenceEquals(_queue, _completedGuard)) {
            return UniTaskStatus.Succeeded;
        }
        else if(_cancellationToken.IsCancellationRequested) {
            return UniTaskStatus.Canceled;
        }
        else {
            return UniTaskStatus.Pending;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateToken(short token)
    {
        if(token != _token) { ThrowHelper.ThrowInvalidOperation("Token version is not matched, can not await twice or get Status after await."); }
    }
}
