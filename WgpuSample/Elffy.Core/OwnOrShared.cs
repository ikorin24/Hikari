#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public readonly struct OwnOrShared<T> : IDisposable, IEquatable<OwnOrShared<T>>
{
    private readonly Own<T> _inner;

    public static OwnOrShared<T> None => default;

    public bool IsNone => _inner.IsNone;

    public bool IsShared([MaybeNullWhen(false)] out T shared)
    {
        var isShared = ReferenceEquals(_inner._release, OwnOrShared.GetSharedReleaseFunc<T>());
        if(isShared) {
            shared = _inner.AsValue();
            return true;
        }
        else {
            shared = default;
            return false;
        }
    }

    public bool IsOwn(out Own<T> own)
    {
        var isShared = ReferenceEquals(_inner._release, OwnOrShared.GetSharedReleaseFunc<T>());
        if(isShared || _inner.IsNone) {
            own = Own<T>.None;
            return false;
        }
        own = _inner;
        return true;
    }

    private OwnOrShared(Own<T> inner)
    {
        _inner = inner;
    }

    internal static OwnOrShared<T> FromOwn(Own<T> own)
    {
        return new OwnOrShared<T>(own);
    }

    internal static OwnOrShared<T> FromShared(T value)
    {
        var onRelease = OwnOrShared.GetSharedReleaseFunc<T>();
        return new(new Own<T>(value, onRelease));
    }

    public T AsValue() => _inner.AsValue();

    public T AsValue(out OwnOrShared<T> self)
    {
        self = this;
        return _inner.AsValue();
    }

    public bool TryAsValue(out T value) => _inner.TryAsValue(out value);

    public void Dispose() => _inner.Dispose();

    public override bool Equals(object? obj) => obj is OwnOrShared<T> shared && Equals(shared);

    public bool Equals(OwnOrShared<T> other) => _inner.Equals(other._inner);

    public override int GetHashCode() => HashCode.Combine(_inner);

    public static bool operator ==(OwnOrShared<T> left, OwnOrShared<T> right) => left.Equals(right);

    public static bool operator !=(OwnOrShared<T> left, OwnOrShared<T> right) => !(left == right);

    public static implicit operator OwnOrShared<T>(Own<T> own) => OwnOrShared<T>.FromOwn(own);
    public static implicit operator OwnOrShared<T>(T shared) => OwnOrShared<T>.FromShared(shared);
}

public static class OwnOrShared
{
    public static OwnOrShared<T> New<T>(Own<T> own) => OwnOrShared<T>.FromOwn(own);

    public static OwnOrShared<T> New<T>(T value) => OwnOrShared<T>.FromShared(value);

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
