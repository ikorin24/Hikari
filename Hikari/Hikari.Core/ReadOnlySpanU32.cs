#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

public readonly ref struct ReadOnlySpanU32<T> where T : unmanaged
{
    private readonly ref readonly T _head;
    private readonly u32 _len;

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
        return new ReadOnlySpanU32<byte>(in Unsafe.As<T, byte>(ref Unsafe.AsRef(in _head)), _len * (u32)Unsafe.SizeOf<T>());
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
}
