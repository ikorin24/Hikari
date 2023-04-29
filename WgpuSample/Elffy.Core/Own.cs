#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public readonly struct Own<T> : IDisposable, IEquatable<Own<T>>
{
    internal readonly T _value;

    // The following tricks are for upcasting.
    // Action<object>?  if (typeof(T).IsValueType == false)
    // Action<T>?       if (typeof(T).IsValueType == true)
    internal readonly Delegate? _release;

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_release))]
    public bool IsNone => _release == null;

    public static Own<T> None => default;

    internal Own(T value, Action<object> release, Own.RefTypeMarker _)
    {
        Debug.Assert(typeof(T).IsValueType == false);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    internal Own(T value, Action<T> release, Own.ValueTypeMarker _)
    {
        Debug.Assert(typeof(T).IsValueType);
        ArgumentNullException.ThrowIfNull(release);
        _value = value;
        _release = release;
    }

    internal Own(T value, Delegate release)
    {
        if(typeof(T).IsValueType) {
            Debug.Assert(release is Action<T>);
        }
        else {
            ArgumentNullException.ThrowIfNull(value);
            Debug.Assert(release is Action<object>);
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsValue()
    {
        if(IsNone) {
            ThrowNoValue();
        }
        if(typeof(T).IsValueType) {
            if(_value is IScreenManaged x) {
                x.Validate();
            }
        }
        else {
            if(typeof(T).IsAssignableTo(typeof(IScreenManaged))) {
                SafeCast.As<IScreenManaged>(_value).Validate();
            }
        }
        return _value;
    }

    public bool TryAsValue(out T value)
    {
        value = _value;
        return !IsNone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Own<U> Cast<U>()
    {
        var x = CastOrNone<U>();
        if(x.IsNone) {
            throw new InvalidCastException($"cannot cast Own<{typeof(T).Name}> to Own<{typeof(U).Name}>");
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Own<U> CastOrNone<U>()
    {
        if(typeof(T) == typeof(U)) {
            return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
        }
        else if(typeof(T).IsAssignableTo(typeof(U))) {
            // upcast
            if(typeof(T).IsValueType) {
                // cannot upcast value type into object
                return Own<U>.None;
            }
            else {
                return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
            }
        }
        else if(typeof(U).IsAssignableTo(typeof(T))) {
            // downcast
            if(TryAsValue(out var value)) {
                if(value is U) {
                    return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
                }
                else {
                    return Own<U>.None;
                }
            }
            else {
                return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
            }
        }
        else {
            return Own<U>.None;
        }
    }

    public static explicit operator T(Own<T> own) => own.AsValue();

    [DoesNotReturn]
    [DebuggerHidden]
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
}

public static class Own
{
    internal static ValueTypeMarker ValueType => default;
    internal static RefTypeMarker RefType => default;

    internal readonly struct ValueTypeMarker { }
    internal readonly struct RefTypeMarker { }

    public static Own<T> New<T>(T value, Action<object> release) where T : class
    {
        return new Own<T>(value, release, RefType);
    }

    public static Own<T> New<T>(T value, Action<T> release) where T : struct
    {
        return new Own<T>(value, release, ValueType);
    }

    public static void Validate<T>(this Own<T> self) where T : IScreenManaged
    {
        if(self.IsNone) {
            Throw();

            [DebuggerHidden]
            [DoesNotReturn]
            static void Throw() => throw new InvalidOperationException("the value is none");
        }
        self._value.Validate();
    }

    internal static void ThrowArgumentExceptionIfNone<T>(this Own<T> self, [CallerArgumentExpression(nameof(self))] string? paramName = null)
    {
        if(self.IsNone) {
            Throw(paramName);

            [DebuggerHidden]
            [DoesNotReturn]
            static void Throw(string? paramName) => throw new ArgumentException("the value is none", paramName);
        }
    }
}
