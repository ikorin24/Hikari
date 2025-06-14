﻿#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

/// <summary>Helper class of <see cref="Unsafe"/></summary>
public static class UnsafeEx
{
    /// <summary>
    /// Casts given readonly reference to a readonly reference to a value of type <typeparamref name="TTo"/>.
    /// </summary>
    /// <remarks>This is compatible with `ref TTo Unsafe.As&lt;TFrom, TTo&gt;(ref TFrom)`</remarks>
    /// <typeparam name="TFrom">source type</typeparam>
    /// <typeparam name="TTo">destination type</typeparam>
    /// <param name="source">source reference</param>
    /// <returns>readonly reference to a value of type <typeparamref name="TTo"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTo As<TFrom, TTo>(scoped ref readonly TFrom source)
    {
        return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in source));
    }

    /// <summary>Determines whether the specified references point to the same location.</summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="left">The first reference to compare.</param>
    /// <param name="right">The second reference to compare.</param>
    /// <returns>true if left and right point to the same location; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreSame<T>(scoped ref readonly T left, scoped ref readonly T right)
    {
        return Unsafe.AreSame(ref Unsafe.AsRef(in left), ref Unsafe.AsRef(in right));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* AsPointer<T>(ref readonly T value)
    {
        return Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref T NullRef<T>()
    {
        return ref Unsafe.AsRef<T>(null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static bool IsNullRef<T>(ref T source)
    {
        return Unsafe.AreSame(ref source, ref Unsafe.AsRef<T>(null));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static bool IsNullRefReadOnly<T>(in T source)
    {
        return Unsafe.AreSame(ref Unsafe.AsRef(in source), ref Unsafe.AsRef<T>(null));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<byte> AsBytes<T>(ref T value) where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlySpan<byte> AsReadOnlyBytes<T>(in T value) where T : unmanaged
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)), sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T Add<T>(in T source, nuint elementOffset)
    {
        return ref Unsafe.Add(ref Unsafe.AsRef(in source), elementOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T Add<T>(in T source, int elementOffset)
    {
        return ref Unsafe.Add(ref Unsafe.AsRef(in source), elementOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T Add<T>(in T source, nint elementOffset)
    {
        return ref Unsafe.Add(ref Unsafe.AsRef(in source), elementOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T AddByteOffset<T>(in T source, nuint byteOffset)
    {
        return ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in source), byteOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T AddByteOffset<T>(in T source, nint byteOffset)
    {
        return ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in source), byteOffset);
    }
}
