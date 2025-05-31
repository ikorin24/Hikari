#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public readonly struct Own<T> : IDisposable, IEquatable<Own<T>> where T : notnull
{
    // _value can be null if the instance made by default(Own<T>),
    // but there are no matter because IsNone == true in that case.
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

        if(typeof(T) == typeof(object) || typeof(T) == typeof(ValueType)) {
            // This is a special case. It is for downcasting.
            if(value.GetType().IsValueType) {
                throw new NotSupportedException("cannot create Own<object> or Own<ValueType> from boxed instance of value type");
            }
        }
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
            if(typeof(T) == typeof(object) || typeof(T) == typeof(ValueType)) {
                // This is a special case. It is for downcasting.
                if(value.GetType().IsValueType) {
                    throw new NotSupportedException("cannot create Own<object> or Own<ValueType> from boxed instance of value type");
                }
            }
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

    public T DisposeOn<S>(Event<S> e)
    {
        var self = this;
        e.Subscribe(_ => self.Dispose());
        return AsValue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsValue()
    {
        if(IsNone) {
            ThrowNoValue();
        }
        return _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsValue(out Own<T> self)
    {
        self = this;
        return AsValue();
    }

    public bool TryAsValue(out T value)
    {
        value = _value;
        return !IsNone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Own<U> Cast<U>() where U : notnull
    {
        var casted = CastCore<U>(out var success);
        if(success) {
            return casted;
        }
        else {
            throw new InvalidCastException($"cannot cast Own<{typeof(T).Name}> to Own<{typeof(U).Name}>");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Own<U> CastOrNone<U>() where U : notnull
    {
        var casted = CastCore<U>(out var success);
        if(success) {
            return casted;
        }
        else {
            return Own<U>.None;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Own<U> CastCore<U>(out bool success) where U : notnull
    {
        if(typeof(T) == typeof(U)) {
            // It always succeeds regardless of whether the inner value is None.
            success = true;
            return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
        }
        else if(typeof(T).IsAssignableTo(typeof(U))) {
            // upcast, T is subclass of U
            if(typeof(T).IsValueType) {
                // There are two cases:
                // 1. It cannot upcast value type into object or ValueType
                //    (e.g. Own<SomeStruct> -> Own<object>)
                // 2. None is always upcasted into None.
                //    (e.g. Own<SomeStruct>.None -> Own<object>.None)
                success = IsNone;
                return Own<U>.None;
            }
            else {
                // It always succeeds regardless of whether the inner value is None.
                success = true;
                return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
            }
        }
        else if(typeof(U).IsAssignableTo(typeof(T))) {
            // downcast, T is superclass of U
            if(TryAsValue(out var value)) {
                if(value is U) {
#if DEBUG
                    // The inner value is not boxed struct when typeof(U) == object || typeof(U) == ValueType
                    // because the constructor does not allow it.
                    Debug.Assert(value.GetType().IsValueType == false);
#endif
                    success = true;
                    return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
                }
                else {
                    // Failed to downcast.
                    // The inner value cannot be downcasted into U.
                    success = false;
                    return Own<U>.None;
                }
            }
            else {
                // Own<T>.None -> Own<U>.None always succeeds
                // (e.g. Own<object>.None -> Own<SomeClass>.None)
                // This is the same as casting (object)null to (SomeClass)null, so it succeeds.
                success = true;
                return Unsafe.As<Own<T>, Own<U>>(ref Unsafe.AsRef(in this));
            }
        }
        else {
            // It never comes here. It is for safety.
            success = false;
            return Own<U>.None;
        }
    }

    public static explicit operator T(Own<T> own) => own.AsValue();
    public static implicit operator Own<T>(Own.OwnNone _) => None;

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

    public static OwnNone None => default;

    public static Own<T> New<T>(T value, Action<object> release) where T : class
    {
        return new Own<T>(value, release, RefType);
    }

    public static Own<T> New<T>(T value, Action<T> release) where T : struct
    {
        return new Own<T>(value, release, ValueType);
    }

    internal static void ThrowArgumentExceptionIfNone<T>(this Own<T> self, [CallerArgumentExpression(nameof(self))] string? paramName = null)
        where T : notnull
    {
        if(self.IsNone) {
            Throw(paramName);

            [DebuggerHidden]
            [DoesNotReturn]
            static void Throw(string? paramName) => throw new ArgumentException("the value is none", paramName);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly record struct OwnNone;
}
