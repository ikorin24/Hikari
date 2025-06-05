#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public readonly struct ThreadId : IEquatable<ThreadId>
{
    private readonly int _threadId;

    private ThreadId(int threadId) => _threadId = threadId;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Don't use default constructor.", true)]
    public ThreadId() => this = default;

    public static ThreadId CurrentThread() => new ThreadId(Environment.CurrentManagedThreadId);

    public bool IsCurrentThread => Environment.CurrentManagedThreadId == _threadId;

    public bool IsNone => _threadId == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfNotMatched()
    {
        if(Environment.CurrentManagedThreadId != _threadId) {
            Throw();

            [DoesNotReturn]
            static void Throw() => throw new InvalidOperationException("Cannot access from the current thread.");
        }
    }

    public override bool Equals(object? obj) => obj is ThreadId id && Equals(id);

    public bool Equals(ThreadId other) => _threadId == other._threadId;

    public override int GetHashCode() => _threadId.GetHashCode();

    public static bool operator ==(ThreadId left, ThreadId right) => left.Equals(right);

    public static bool operator !=(ThreadId left, ThreadId right) => !(left == right);
}
