#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

public unsafe readonly struct Own<T> : IDisposable, IEquatable<Own<T>>
{
    private readonly T _value;
    private readonly Action<T>? _release;

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_release))]
    public bool IsNone => _release == null;

    public static Own<T> None => default;

    public Own(T value, Action<T> release)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    public void Dispose()
    {
        if(IsNone) { return; }
        _release.Invoke(_value);
    }

    public T AsValue()
    {
        if(IsNone) {
            ThrowNoValue();
        }
        return _value;
    }

    public T AsValue(out Own<T> self)
    {
        if(IsNone) {
            ThrowNoValue();
        }
        self = this;
        return _value;
    }

    public static explicit operator T(Own<T> own) => own.AsValue();

    [DoesNotReturn]
    private static void ThrowNoValue() => throw new InvalidOperationException("no value exists");

    public override bool Equals(object? obj) => obj is Own<T> own && Equals(own);

    public bool Equals(Own<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value) &&
               EqualityComparer<Action<T>>.Default.Equals(_release, other._release);
    }

    public override int GetHashCode() => HashCode.Combine(_value, _release);

    public static bool operator ==(Own<T> left, Own<T> right) => left.Equals(right);

    public static bool operator !=(Own<T> left, Own<T> right) => !(left == right);
}

public static class Own
{
    public static Own<T> New<T>(T value, Action<T> release)
    {
        return new Own<T>(value, release);
    }

    public static Own<T> None<T>() => Own<T>.None;
}
