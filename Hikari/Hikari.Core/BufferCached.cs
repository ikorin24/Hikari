#nullable enable
using Hikari.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

internal struct BufferCached<T> : IDisposable where T : unmanaged
{
    private Own<Buffer> _buffer;
    private FastSpinLock _lock;
    private T _data;

    [UnscopedRef]
    public readonly ref readonly T Data
    {
        get
        {
            try {
                _lock.Enter();
                return ref _data;
            }
            finally {
                _lock.Exit();
            }
        }
    }

    public readonly Buffer? Buffer => _buffer.TryAsValue(out var buffer) ? buffer : null;

    public BufferCached(Screen screen, in T data, BufferUsages usage)
    {
        _buffer = Buffer.CreateInitData(screen, data, usage);
        _data = data;
    }

    public readonly bool TryAsBuffer(out Buffer buffer) => _buffer.TryAsValue(out buffer);

    public readonly Buffer AsBuffer() => _buffer.AsValue();

    public void WriteData(in T data)
    {
        try {
            _lock.Enter();
            _buffer.AsValue().WriteData(0, data);
            _data = data;
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
            _data = default;
        }
        finally {
            _lock.Exit();
        }
    }
}
