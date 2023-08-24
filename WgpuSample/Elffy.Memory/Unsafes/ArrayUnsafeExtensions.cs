#nullable enable
using System.Runtime.CompilerServices;
using System;
using System.Runtime.InteropServices;

namespace Elffy.Unsafes;

public static class ArrayUnsafeExtensions
{
    /// <summary>Get array element at specified index without range checking.</summary>
    /// <remarks>
    /// [NOTE] 
    /// This method does not check null reference and does not check index range.
    /// *** That means UNDEFINED behaviors may occor in this method. ***
    /// </remarks>
    /// <typeparam name="T">type of element</typeparam>
    /// <param name="source">source array</param>
    /// <param name="index">index of array</param>
    /// <returns>element in the array</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T At<T>(this T[] source, int index)
    {
        return ref Unsafe.Add(ref source.GetReference(), index);
    }

    /// <summary>Get reference to the 0th element without any checking.</summary>
    /// <remarks>
    /// [NOTE] DOES NOT use this method for empty array.
    /// </remarks>
    /// <typeparam name="T">type of element</typeparam>
    /// <param name="array">source array</param>
    /// <returns>reference to the 0th element</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReference<T>(this T[] array)
    {
        return ref MemoryMarshal.GetArrayDataReference(array);
    }

    /// <summary>Create span without any checking.</summary>
    /// <typeparam name="T">type of element</typeparam>
    /// <param name="array">source array</param>
    /// <returns><see cref="Span{T}"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpanUnsafe<T>(this T[] array)
    {
        return MemoryMarshal.CreateSpan(ref array.GetReference(), array.Length);
    }

    /// <summary>Create span without any checking.</summary>
    /// <typeparam name="T">type of element</typeparam>
    /// <param name="array">source array</param>
    /// <param name="start">start index</param>
    /// <returns><see cref="Span{T}"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpanUnsafe<T>(this T[] array, int start)
    {
        return MemoryMarshal.CreateSpan(ref array.At(start), array.Length - start);
    }

    /// <summary>Create span without any checking.</summary>
    /// <typeparam name="T">type of element</typeparam>
    /// <param name="array">source array</param>
    /// <param name="start">start index</param>
    /// <param name="length">length of span</param>
    /// <returns><see cref="Span{T}"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpanUnsafe<T>(this T[] array, int start, int length)
    {
        return MemoryMarshal.CreateSpan(ref array.At(start), length);
    }
}
