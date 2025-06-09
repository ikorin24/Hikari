#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.Internal;

internal sealed class PooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private const int DefaultInitialBufferSize = 256;

    private T[] _buffer;
    private int _index;

    public PooledArrayBufferWriter()
    {
        _buffer = Array.Empty<T>();
        _index = 0;
    }

    public PooledArrayBufferWriter(int initialCapacity)
    {
        if(initialCapacity <= 0) {
            ThrowArgument(null, nameof(initialCapacity));
        }

        _buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

    public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

    public int WrittenCount => _index;

    public int Capacity => _buffer.Length;

    public int FreeCapacity => _buffer.Length - _index;

    public void Clear()
    {
        Debug.Assert(_buffer.Length >= _index);
        _buffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    public void ResetWrittenCount() => _index = 0;

    public void Advance(int count)
    {
        if(count < 0) {
            ThrowArgument(null, nameof(count));
        }

        if(_index > _buffer.Length - count) {
            Throw(_buffer.Length);
            [DoesNotReturn]
            static void Throw(int len) => throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {len}.");
        }

        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsMemory(_index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_buffer.Length > _index);
        return _buffer.AsSpan(_index);
    }

    public void Dispose()
    {
        if(_buffer.Length != 0) {
            ArrayPool<T>.Shared.Return(_buffer);
        }
        _buffer = Array.Empty<T>();
        _index = 0;
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        if(sizeHint < 0) {
            throw new ArgumentException(nameof(sizeHint));
        }
        if(sizeHint == 0) {
            sizeHint = 1;
        }
        if(sizeHint > FreeCapacity) {
            int currentLength = _buffer.Length;
            int growBy = Math.Max(sizeHint, currentLength);
            if(currentLength == 0) {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }
            int newSize = currentLength + growBy;
            if((uint)newSize > int.MaxValue) {
                uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                Debug.Assert(needed > currentLength);
                if(needed > Array.MaxLength) {
                    throw new OutOfMemoryException($"Cannot allocate a buffer of size {needed}.");
                }
                newSize = Array.MaxLength;
            }
            var newBuf = ArrayPool<T>.Shared.Rent(newSize);
            _buffer.AsSpan(0, _index).CopyTo(newBuf);
            if(_buffer.Length != 0) {
                ArrayPool<T>.Shared.Return(_buffer);
            }
            _buffer = newBuf;
        }
        Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
    }

    [DoesNotReturn]
    [DebuggerHidden]
    private static void ThrowArgument(string? message, string paramName) => throw new ArgumentException(message, paramName);
}
