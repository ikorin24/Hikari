#nullable enable
using Hikari.Threading;
using System;

namespace Hikari;

public sealed class CachedOwnBuffer<T> : IDisposable where T : unmanaged
{
    private TypedOwnBuffer<T> _buffer;
    private FastSpinLock _lock;
    private T _data;

    public bool IsNone
    {
        get
        {
            using(_ = _lock.Scope()) {
                return _buffer.IsNone;
            }
        }
    }

    public T Data
    {
        get
        {
            using(_ = _lock.Scope()) {
                return _data;
            }
        }
    }

    public CachedOwnBuffer(Screen screen, in T data, BufferUsages usage)
    {
        _buffer = new(screen, data, usage);
        _data = data;
    }

    internal Buffer AsBuffer()
    {
        return _buffer.AsBuffer();
    }

    public void WriteData(in T data)
    {
        using(_ = _lock.Scope()) {
            _buffer.WriteData(data);
            _data = data;
        }
    }

    public void Dispose()
    {
        using(_ = _lock.Scope()) {
            _buffer.Dispose();
            _buffer = TypedOwnBuffer<T>.None;
            _data = default;
        }
    }
}
