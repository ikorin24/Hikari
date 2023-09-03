#nullable enable
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Hikari.Unsafes;

namespace Hikari;

public static class ListExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span)
    {
        if(span.IsEmpty) { return; }

        if(list.Capacity < list.Count + span.Length) {
            int cap = list.Capacity + span.Length;
            if(cap > 1 << 30) {
                cap = Array.MaxLength;
            }
            else {
                if(cap == 0) {
                    cap = 4;
                }
                // round up to power of two
                cap = 1 << 32 - BitOperations.LeadingZeroCount((uint)cap - 1);
            }
            list.Capacity = cap;
        }
        var dest = Unsafe.As<ListDummy<T>>(list)._items.AsSpan(list.Count);
        span.CopyTo(dest);
        Unsafe.As<ListDummy<T>>(list)._size += span.Length;
        Unsafe.As<ListDummy<T>>(list)._version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T>? list)
    {
        return CollectionsMarshal.AsSpan(list);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T>? list) => list.AsSpan();

    public static bool SwapRemoveAt<T>(this List<T> list, int index)
    {
        if(index < 0) {
            return false;
        }
        var innerSpan = CollectionsMarshal.AsSpan(list);
        var last = innerSpan.Length - 1;
        innerSpan.At(index) = innerSpan.At(last);
        list.RemoveAt(last);
        return true;
    }

    public static bool SwapRemove<T>(this List<T> list, T item)
    {
        var index = list.IndexOf(item);
        return list.SwapRemoveAt(index);
    }

    private abstract class ListDummy<T>
    {
        internal T[] _items = default!;
        internal int _size;
        internal int _version;

        private ListDummy() { }
    }
}
