#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.NativeBind;

/// <summary>Opaque wrapper of a native pointer</summary>
internal unsafe readonly struct NativePointer : IEquatable<NativePointer>
{
    private readonly void* _ptr;

    internal static NativePointer Null => default;

    public bool IsNull => _ptr == null;

    private NativePointer(void* p) => _ptr = p;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfNull()
    {
        if(_ptr == null) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new ArgumentNullException(nameof(NativePointer));
        }
    }

    public override bool Equals(object? obj) => obj is NativePointer handle && Equals(handle);

    public bool Equals(NativePointer other) => _ptr == other._ptr;

    public override int GetHashCode() => ((IntPtr)_ptr).GetHashCode();

    public static bool operator ==(NativePointer left, NativePointer right) => left.Equals(right);

    public static bool operator !=(NativePointer left, NativePointer right) => !(left == right);

    public override string ToString() => ((IntPtr)_ptr).ToString();

    public unsafe static implicit operator NativePointer(void* nativePtr) => new(nativePtr);
    public unsafe static implicit operator void*(NativePointer nativePtr) => nativePtr._ptr;
}
