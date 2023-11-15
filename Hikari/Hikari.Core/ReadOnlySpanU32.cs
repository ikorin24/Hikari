#nullable enable
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

[DebuggerDisplay("{DebuggerView,nq}")]
[DebuggerTypeProxy(typeof(ReadOnlySpanU32<>.DebuggerProxy))]
public readonly ref struct ReadOnlySpanU32<T> where T : unmanaged
{
    private readonly ref readonly T _head;
    private readonly u32 _len;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerView => $"{typeof(T).Name}[{_len}]";

    public ref readonly T this[u32 index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(index >= _len) {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            }
            return ref Unsafe.Add(ref Unsafe.AsRef(in _head), index);
        }
    }

    public ref readonly T this[i32 index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((u32)index >= _len) {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            }
            return ref Unsafe.Add(ref Unsafe.AsRef(in _head), index);
        }
    }

    public ref readonly T this[usize index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(index >= _len) {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            }
            return ref Unsafe.Add(ref Unsafe.AsRef(in _head), index);
        }
    }

    public u32 Length => _len;

    public usize ByteLength => _len * (usize)Unsafe.SizeOf<T>();

    public bool IsEmpty => _len == 0;

    public static ReadOnlySpanU32<T> Empty => default;

    public unsafe ReadOnlySpanU32(void* head, u32 len)
    {
        _head = ref Unsafe.AsRef(in *(T*)head);
        _len = len;
    }

    public ReadOnlySpanU32(in T head, u32 len)
    {
        _head = ref head;
        _len = len;
    }

    public ReadOnlySpanU32(ReadOnlySpan<T> span)
    {
        _head = ref MemoryMarshal.GetReference(span);
        _len = (u32)span.Length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref readonly T GetPinnableReference() => ref _head;

    public ref readonly T GetReference() => ref _head;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T UnsafeAt(u32 index) => ref Unsafe.Add(ref Unsafe.AsRef(in _head), index);

    public ReadOnlySpanU32<T> Slice(u32 start)
    {
        if(start >= _len) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(start));
        }
        return new ReadOnlySpanU32<T>(in Unsafe.Add(ref Unsafe.AsRef(in _head), start), _len - start);
    }

    public ReadOnlySpanU32<T> Slice(u32 start, u32 len)
    {
        if(start >= _len) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(start));
        }
        if(len > _len - start) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(len));
        }
        return new ReadOnlySpanU32<T>(in Unsafe.Add(ref Unsafe.AsRef(in _head), start), len);
    }

    public ReadOnlySpanU32<byte> AsBytes()
    {
        return new ReadOnlySpanU32<byte>(in Unsafe.As<T, byte>(ref Unsafe.AsRef(in _head)), checked(_len * (u32)Unsafe.SizeOf<T>()));
    }

    public ReadOnlySequence<T> AsSequence()
    {
        if(IsEmpty) {
            return ReadOnlySequence<T>.Empty;
        }
        CustomSegment? head = null;
        long index = 0;
        uint len = Length;
        CustomSegment? prev = null;

        while(true) {
            uint l = uint.Min(len, int.MaxValue);
            var part = Slice(0, l);
            var partCopy = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in part.GetReference()), (int)l).ToArray();
            var segment = new CustomSegment(partCopy, index);
            prev?.SetNext(segment);
            prev = segment;
            index += l;
            len -= l;
            head ??= segment;
            if(len == 0) {
                break;
            }
        }
        return new ReadOnlySequence<T>(head, 0, prev, prev.Memory.Length);
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    public static implicit operator ReadOnlySpanU32<T>(Span<T> span)
    {
        return new ReadOnlySpanU32<T>(in MemoryMarshal.GetReference(span), (u32)span.Length);
    }

    public static implicit operator ReadOnlySpanU32<T>(SpanU32<T> span)
    {
        return new ReadOnlySpanU32<T>(in span.GetReference(), span.Length);
    }

    public static implicit operator ReadOnlySpanU32<T>(ReadOnlySpan<T> span)
    {
        return new ReadOnlySpanU32<T>(in MemoryMarshal.GetReference(span), (u32)span.Length);
    }

    public ref struct Enumerator
    {
        private readonly ReadOnlySpanU32<T> _span;
        private u32 _index;

        public Enumerator(ReadOnlySpanU32<T> span)
        {
            _span = span;
            _index = u32.MaxValue;
        }

        public readonly ref readonly T Current => ref _span.UnsafeAt(_index);

        public bool MoveNext()
        {
            var index = unchecked(_index + 1);
            if(index < _span.Length) {
                _index = index;
                return true;
            }
            return false;
        }
    }

    private sealed class CustomSegment : ReadOnlySequenceSegment<T>
    {
        public CustomSegment(ReadOnlyMemory<T> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public void SetNext(CustomSegment segment)
        {
            Next = segment;
        }
    }

    private sealed class DebuggerProxy
    {
        private readonly ReadOnlySequence<T> _items;

        public DebuggerProxy(ReadOnlySpanU32<T> span)
        {
            _items = span.AsSequence();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ReadOnlySequence<T> Items => _items;
    }
}
