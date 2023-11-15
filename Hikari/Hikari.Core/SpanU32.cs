#nullable enable
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikari;

[DebuggerDisplay("{DebuggerView,nq}")]
[DebuggerTypeProxy(typeof(SpanU32<>.DebuggerProxy))]
public readonly ref struct SpanU32<T> where T : unmanaged
{
    private readonly ref T _head;
    private readonly u32 _len;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerView => $"{typeof(T).Name}[{_len}]";

    public ref T this[u32 index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if(index >= _len) {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            }
            return ref Unsafe.Add(ref _head, index);
        }
    }

    public ref T this[i32 index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((u32)index >= _len) {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            }
            return ref Unsafe.Add(ref _head, index);
        }
    }

    public u32 Length => _len;
    public usize ByteLength => _len * (usize)Unsafe.SizeOf<T>();
    public bool IsEmpty => _len == 0;

    public static SpanU32<T> Empty => default;

    public unsafe SpanU32(void* head, u32 len)
    {
        _head = ref Unsafe.AsRef(in *(T*)head);
        _len = len;
    }

    public SpanU32(ref T head, u32 len)
    {
        _head = ref head;
        _len = len;
    }

    public SpanU32(Span<T> span)
    {
        _head = ref MemoryMarshal.GetReference(span);
        _len = (u32)span.Length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref T GetPinnableReference() => ref _head;

    public ref T GetReference() => ref _head;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T UnsafeAt(u32 index) => ref Unsafe.Add(ref _head, index);

    public SpanU32<T> Slice(u32 start)
    {
        if(start >= _len) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(start));
        }
        return new SpanU32<T>(ref Unsafe.Add(ref _head, start), _len - start);
    }

    public SpanU32<T> Slice(u32 start, u32 len)
    {
        if(start >= _len) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(start));
        }
        if(len > _len - start) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(len));
        }
        return new SpanU32<T>(ref Unsafe.Add(ref _head, start), len);
    }

    public SpanU32<byte> AsBytes()
    {
        return new SpanU32<byte>(ref Unsafe.As<T, byte>(ref _head), _len * (u32)Unsafe.SizeOf<T>());
    }

    public ReadOnlySpanU32<T> AsReadOnly() => this;

    public ReadOnlySequence<T> AsSequence() => AsReadOnly().AsSequence();

    public Enumerator GetEnumerator() => new Enumerator(this);

    public static implicit operator SpanU32<T>(Span<T> span)
    {
        return new SpanU32<T>(ref MemoryMarshal.GetReference(span), (u32)span.Length);
    }

    public ref struct Enumerator
    {
        private readonly SpanU32<T> _span;
        private u32 _index;

        public Enumerator(SpanU32<T> span)
        {
            _span = span;
            _index = u32.MaxValue;
        }

        public readonly ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span.UnsafeAt(_index);
        }

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

    private sealed class DebuggerProxy
    {
        private readonly ReadOnlySequence<T> _items;

        public DebuggerProxy(SpanU32<T> span)
        {
            _items = span.AsSequence();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ReadOnlySequence<T> Items => _items;
    }
}
