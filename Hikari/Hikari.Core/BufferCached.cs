#nullable enable
using Hikari.Threading;
using System;

namespace Hikari;

internal struct BufferCached<T> : IDisposable where T : unmanaged
{
    private Own<Buffer> _buffer;
    private FastSpinLock _lock;
    private T _value;

    public readonly T Value
    {
        get
        {
            try {
                _lock.Enter();
                return _value;
            }
            finally {
                _lock.Exit();
            }
        }
    }

    public readonly Buffer Buffer => _buffer.AsValue();

    public BufferCached(Screen screen, in T data, BufferUsages usage)
    {
        _buffer = Buffer.CreateInitData(screen, data, usage);
        _value = data;
    }

    public void WriteValue(in T value)
    {
        try {
            _lock.Enter();
            _buffer.AsValue().WriteData(0, value);
            _value = value;
        }
        finally {
            _lock.Exit();
        }
    }

    public void Dispose()
    {
        try {
            _lock.Enter();
            _buffer.Dispose();
            _buffer = Own<Buffer>.None;
            _value = default;
        }
        finally {
            _lock.Exit();
        }
    }
}
