#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public readonly struct MaybeOwn<T> : IDisposable, IEquatable<MaybeOwn<T>>
    where T : notnull
{
    internal readonly Own<T> _inner;

    public static MaybeOwn<T> None => default;

    public bool IsNone => _inner.IsNone;

    private MaybeOwn(Own<T> inner)
    {
        _inner = inner;
    }

    public bool IsOwn(out Own<T> own)
    {
        var isShared = ReferenceEquals(_inner._release, MaybeOwn.GetSharedReleaseFunc<T>());
        if(isShared || _inner.IsNone) {
            own = Own<T>.None;
            return false;
        }
        own = _inner;
        return true;
    }

    internal static MaybeOwn<T> FromOwn(Own<T> own)
    {
        return new MaybeOwn<T>(own);
    }

    internal static MaybeOwn<T> FromShared(T value)
    {
        var onRelease = MaybeOwn.GetSharedReleaseFunc<T>();
        return new(new Own<T>(value, onRelease));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsValue() => _inner.AsValue();

    public bool TryAsValue(out T value) => _inner.TryAsValue(out value);

    public void Dispose() => _inner.Dispose();

    public override bool Equals(object? obj) => obj is MaybeOwn<T> shared && Equals(shared);

    public bool Equals(MaybeOwn<T> other) => _inner.Equals(other._inner);

    public override int GetHashCode() => HashCode.Combine(_inner);

    public static bool operator ==(MaybeOwn<T> left, MaybeOwn<T> right) => left.Equals(right);

    public static bool operator !=(MaybeOwn<T> left, MaybeOwn<T> right) => !(left == right);

    public static implicit operator MaybeOwn<T>(Own<T> own) => MaybeOwn<T>.FromOwn(own);
    public static implicit operator MaybeOwn<T>(T shared) => MaybeOwn<T>.FromShared(shared);
}

public static class MaybeOwn
{
    public static MaybeOwn<T> New<T>(Own<T> own) where T : notnull => MaybeOwn<T>.FromOwn(own);

    public static MaybeOwn<T> New<T>(T value) where T : notnull => MaybeOwn<T>.FromShared(value);

    public static void Validate<T>(this MaybeOwn<T> self) where T : IScreenManaged
    {
        self._inner.Validate();
    }

    [DebuggerHidden]
    internal static void ThrowArgumentExceptionIfNone<T>(this MaybeOwn<T> self, [CallerArgumentExpression(nameof(self))] string? paramName = null)
        where T : notnull
    {
        if(self.IsNone) {
            Throw(paramName);

            [DoesNotReturn]
            [DebuggerHidden]
            static void Throw(string? paramName) => throw new ArgumentException("the value is none", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Delegate GetSharedReleaseFunc<T>()
    {
        if(typeof(T).IsValueType) {
            return TypedCache<T>.ReleaseFunc;
        }
        else {
            return _refTypeSharedReleaseFunc;
        }
    }

    private static readonly Action<object> _refTypeSharedReleaseFunc = (object _) =>
    {
        // do nothing
    };

    private static class TypedCache<T>
    {
        internal static Action<T> ReleaseFunc = (T _) =>
        {
            // do nothing
        };
    }
}
