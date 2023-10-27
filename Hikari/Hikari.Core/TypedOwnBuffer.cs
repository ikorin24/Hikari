#nullable enable
using System;

namespace Hikari;

internal readonly record struct TypedOwnBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Own<Buffer> _buffer;

    public static TypedOwnBuffer<T> None => new(Own<Buffer>.None);

    private TypedOwnBuffer(Own<Buffer> buffer)
    {
        _buffer = buffer;
    }

    public TypedOwnBuffer(Screen screen, in T data, BufferUsages usage)
    {
        _buffer = Buffer.CreateInitData(screen, data, usage);
    }

    internal readonly Buffer AsBuffer() => _buffer.AsValue();

    public void WriteData(in T data) => _buffer.AsValue().WriteData(0, data);

    public void Dispose() => _buffer.Dispose();
}
