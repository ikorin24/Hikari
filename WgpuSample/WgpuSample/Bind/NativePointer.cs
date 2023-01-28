#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy.Bind;

/// <summary>Opaque wrapper of a native pointer</summary>
internal unsafe readonly struct NativePointer : IEquatable<NativePointer>
{
    private readonly void* _ptr;

    internal static NativePointer Null => default;

    private NativePointer(void* p) => _ptr = p;

    public override bool Equals(object? obj) => obj is NativePointer handle && Equals(handle);

    public bool Equals(NativePointer other) => _ptr == other._ptr;

    public override int GetHashCode() => ((IntPtr)_ptr).GetHashCode();

    public static bool operator ==(NativePointer left, NativePointer right) => left.Equals(right);

    public static bool operator !=(NativePointer left, NativePointer right) => !(left == right);

    public override string ToString() => ((IntPtr)_ptr).ToString();

    public unsafe static implicit operator NativePointer(void* nativePtr) => new(nativePtr);
    public unsafe static implicit operator void*(NativePointer nativePtr) => nativePtr._ptr;
}
