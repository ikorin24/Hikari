#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.NativeBind;

/// <summary>Provides types in std of Rust</summary>
internal static class Rust
{
    /// <summary>
    /// `Option&lt;NonZeroU32&gt;` in Rust
    /// </summary>
    internal readonly struct OptionNonZeroU32 : IEquatable<OptionNonZeroU32>
    {
        private readonly u32 _value;
        public static OptionNonZeroU32 None => default;

        private OptionNonZeroU32(u32 value) => _value = value;

        public static implicit operator OptionNonZeroU32(u32 value) => new(value);

        public static bool operator ==(OptionNonZeroU32 left, OptionNonZeroU32 right) => left.Equals(right);

        public static bool operator !=(OptionNonZeroU32 left, OptionNonZeroU32 right) => !(left == right);

        public override bool Equals(object? obj) => obj is OptionNonZeroU32 none && Equals(none);

        public bool Equals(OptionNonZeroU32 other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();
    }

    /// <summary>`Option&lt;Box&lt;T&gt;&gt;` in Rust</summary>
    /// <typeparam name="T">native type in Box</typeparam>
    internal readonly struct OptionBox<T> where T : INativeTypeNonReprC
    {
        private readonly NativePointer _p;
        public static OptionBox<T> None => default;
        public unsafe bool IsNone => (void*)_p == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box<T> Unwrap()
        {
            if(IsNone) {
                Throw();
                static void Throw() => throw new InvalidOperationException("Cannot unwrap None");
            }
            return Unsafe.As<OptionBox<T>, Box<T>>(ref Unsafe.AsRef(in this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box<T> UnwrapUnchecked()
        {
            return Unsafe.As<OptionBox<T>, Box<T>>(ref Unsafe.AsRef(in this));
        }
    }

    /// <summary>`Box&lt;T&gt;` in Rust</summary>
    /// <typeparam name="T">native type in Box</typeparam>
    [DebuggerDisplay("{DebugDisplay,nq}")]
    internal readonly struct Box<T> where T : INativeTypeNonReprC
    {
        private readonly NativePointer _p;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"0x{(IntPtr)_p:x16} (Box::<{typeof(T).Name}>)";

        public static Box<T> Invalid => default;

        public unsafe bool IsInvalid => (void*)_p == null;

        [Obsolete("Can not create the instance. It is only from native library.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Box() => throw new NotSupportedException("Can not create the instance. It is only from native library.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Ref<T> AsRef()
        {
            var self = this;
            return *(Ref<T>*)&self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Ref<T> AsRefChecked()
        {
            var self = this;
            var r = *(Ref<T>*)&self;
            r.ThrowIfInvalid();
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe MutRef<T> AsMut()
        {
            var self = this;
            return *(MutRef<T>*)&self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe MutRef<T> AsMutChecked()
        {
            var self = this;
            var r = *(MutRef<T>*)&self;
            r.AsRef().ThrowIfInvalid();
            return r;
        }

        public NativePointer AsPtr() => _p;

        public NativePointer AsPtrChecked()
        {
            var p = _p;

            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void ThrowIfInvalid() => AsRef().ThrowIfInvalid();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MutRef<T>(Box<T> x) => x.AsMut();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Ref<T>(Box<T> x) => x.AsRef();
    }

    /// <summary>`&amp;mut T` in Rust</summary>
    /// <typeparam name="T">referenced native type</typeparam>
    [DebuggerDisplay("{DebugDisplay,nq}")]
    internal readonly ref struct MutRef<T> where T : INativeTypeNonReprC
    {
        private readonly NativePointer _p;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"0x{(IntPtr)_p:x16} (&mut {typeof(T).Name})";

        public static MutRef<T> Invalid => default;
        public unsafe bool IsInvalid => (void*)_p == null;

        [Obsolete("Can not create the instance by the constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MutRef() => throw new NotSupportedException("Can not create the instance by the constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Ref<T> AsRef()
        {
            var self = this;
            return *(Ref<T>*)&self;
        }

        public NativePointer AsPtr() => _p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void ThrowIfInvalid() => AsRef().ThrowIfInvalid();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Ref<T>(MutRef<T> x) => x.AsRef();
    }

    /// <summary>`&amp;T` in Rust</summary>
    /// <typeparam name="T">referenced native type</typeparam>
    [DebuggerDisplay("{DebugDisplay,nq}")]
    internal readonly ref struct Ref<T> where T : INativeTypeNonReprC
    {
        private readonly NativePointer _p;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"0x{(IntPtr)_p:x16} (&{typeof(T).Name})";

        public static Ref<T> Invalid => default;
        public unsafe bool IsInvalid => (void*)_p == null;

        [Obsolete("Can not create the instance by the constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Ref() => throw new NotSupportedException("Can not create the instance by the constructor.");

        public NativePointer AsPtr() => _p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void ThrowIfInvalid()
        {
            if(IsInvalid) {
                Throw();

                [DoesNotReturn]
                [DebuggerHidden]
                static void Throw() => throw new InvalidOperationException($"Box<{typeof(T).Name}> is already destroyed");
            }
        }
    }
}

internal interface INativeTypeNonReprC
{
}
