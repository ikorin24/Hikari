#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public unsafe readonly struct Own<T> : IDisposable, IEquatable<Own<T>>
{
    private readonly T _value;

    // The following tricks are for upcasting.
    // Action<object>?  if (typeof(T).IsValueType == false)
    // Action<T>?       if (typeof(T).IsValueType == true)
    private readonly Delegate? _release;

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_release))]
    public bool IsNone => _release == null;

    public static Own<T> None => default;

    internal Own(T value, Action<object> release, RefTypeMarker _)
    {
        Debug.Assert(typeof(T).IsValueType == false);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    internal Own(T value, Action<T> release, ValueTypeMarker _)
    {
        Debug.Assert(typeof(T).IsValueType);
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    public void Dispose()
    {
        if(IsNone) { return; }

        if(typeof(T).IsValueType) {
            var release = SafeCast.As<Action<T>>(_release);
            release.Invoke(_value);
        }
        else {
            var release = SafeCast.As<Action<object>>(_release);
            release.Invoke(_value);
        }
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

    public bool TryAsValue(out T value)
    {
        value = _value;
        return !IsNone;
    }

    public static explicit operator T(Own<T> own) => own.AsValue();

    [DoesNotReturn]
    private static void ThrowNoValue() => throw new InvalidOperationException("no value exists");

    public override bool Equals(object? obj) => obj is Own<T> own && Equals(own);

    public bool Equals(Own<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value) &&
               EqualityComparer<Delegate>.Default.Equals(_release, other._release);
    }

    public override int GetHashCode() => HashCode.Combine(_value, _release);

    public static bool operator ==(Own<T> left, Own<T> right) => left.Equals(right);

    public static bool operator !=(Own<T> left, Own<T> right) => !(left == right);

    internal static ValueTypeMarker ValueType => default;
    internal static RefTypeMarker RefType => default;

    internal readonly struct ValueTypeMarker { }
    internal readonly struct RefTypeMarker { }
}

public static class Own
{
    public static Own<T> RefType<T>(T value, Action<object> release) where T : class
    {
        return new Own<T>(value, release, Own<T>.RefType);
    }

    public static Own<T> ValueType<T>(T value, Action<T> release) where T : struct
    {
        return new Own<T>(value, release, Own<T>.ValueType);
    }
}

public static class OwnExtensions
{
    public static void ThrowArgumentExceptionIfNone<T>(this Own<T> self, [CallerArgumentExpression(nameof(self))] string? paramName = null)
    {
        if(self.IsNone) {
            Throw(paramName);
            [DoesNotReturn] static void Throw(string? paramName) => throw new ArgumentException("the value is none", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Own<U> Upcast<T, U>(this Own<T> self) where T : class, U
    {
        // T is subclass of U.
        // In this case, Own<T> and Own<U> has same values in all fields.
        // So, it is valid to return itself

        return Unsafe.As<Own<T>, Own<U>>(ref self);
    }

    public static Own<U> DowncastOrNone<T, U>(this Own<T> self) where U : class, T
    {
        if(self.TryAsValue(out var value)) {
            if(value is U) {
                return Unsafe.As<Own<T>, Own<U>>(ref self);
            }
            else {
                return Own<U>.None;
            }
        }
        else {
            return Unsafe.As<Own<T>, Own<U>>(ref self);
        }
    }

    public static bool TryDowncast<T, U>(this Own<T> self, out Own<U> casted) where U : class, T
    {
        if(self.TryAsValue(out var value)) {
            if(value is U) {
                casted = Unsafe.As<Own<T>, Own<U>>(ref self);
                return true;
            }
            else {
                casted = Own<U>.None;
                return false;
            }
        }
        else {
            casted = Unsafe.As<Own<T>, Own<U>>(ref self);
            return true;
        }
    }

    public static Own<U> Downcast<T, U>(this Own<T> self) where U : class, T
    {
        if(self.TryDowncast<T, U>(out var casted) == false) {
            ThrowInvalidCast();
            [DoesNotReturn] static void ThrowInvalidCast() => throw new InvalidCastException();
        }
        return casted;
    }
}
