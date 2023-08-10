#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Effective;

[DebuggerTypeProxy(typeof(TemporalBufferDebuggerTypeProxy<>))]
[DebuggerDisplay("Count = {Count}")]
[Obsolete("don't use", true)]
public ref struct TemporalBuffer<T>
{
    private T[]? _rent;
    private int _count;

    public readonly bool IsEmpty => _count == 0;
    public readonly int Count => _count;
    public readonly int Capacity => _rent?.Length ?? 0;

    public readonly ref T this[int index]
    {
        get
        {
            var array = _rent ?? Array.Empty<T>();
            return ref array[index];
        }
    }

    public TemporalBuffer()
    {
        _rent = null;
        _count = 0;
    }

    public TemporalBuffer(int minCapacity)
    {
        _rent = ArrayPool<T>.Shared.Rent(minCapacity);
        _count = 0;
    }

    public TemporalBuffer(Span<T> span)
    {
        _rent = ArrayPool<T>.Shared.Rent(span.Length);
        span.CopyTo(_rent);
        _count = span.Length;
    }

    public void Dispose()
    {
        if(_rent != null) {
            ArrayPool<T>.Shared.Return(_rent);
            _rent = null;
        }
        _count = 0;
    }

    public readonly Span<T> AsSpan() => _rent.AsSpan(0, _count);

    public readonly T[] ToArray() => AsSpan().ToArray();

    public void Add(T item)
    {
        var currentCap = _rent?.Length ?? 0;
        if(_count >= currentCap) {
            GrowForAdd();
        }
        // Strictly speaking, this is wrong.
        // I don't care about overflow or very large array (~= int.MaxValue).
        Debug.Assert(_rent != null);
        _rent[_count++] = item;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [MemberNotNull(nameof(_rent))]
    private void GrowForAdd()
    {
        var currentCap = _rent?.Length ?? 0;
        var newCapacity = int.Max(4, currentCap * 2);
        var newBuf = ArrayPool<T>.Shared.Rent(newCapacity);
        if(_rent != null) {
            _rent.AsSpan(0, _count).CopyTo(newBuf);
            ArrayPool<T>.Shared.Return(_rent);
        }
        _rent = newBuf;
    }
}

[Obsolete("don't use", true)]
internal sealed class TemporalBufferDebuggerTypeProxy<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T[] _items;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _items;

    public TemporalBufferDebuggerTypeProxy(TemporalBuffer<T> buf)
    {
        _items = buf.ToArray();
    }
}
