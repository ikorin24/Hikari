#nullable enable
using System;
using System.ComponentModel;

namespace Elffy;

public readonly struct Uniform<T> where T : unmanaged
{
    private readonly Own<Buffer> _buffer;

    public Buffer Buffer => _buffer.AsValue();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Don't use default constructor.", true)]
    public Uniform() => throw new NotSupportedException("Don't use default constructor.");

    private Uniform(Screen screen, in T value)
    {
        _buffer = Buffer.CreateUniformBuffer(screen, in value);
    }

    private void Release()
    {
        _buffer.Dispose();
    }

    public void Set(in T value)
    {
        var buffer = _buffer.AsValue();
        buffer.Write(0, value);
    }

    public static Own<Uniform<T>> Create(Screen screen, in T value)
    {
        return Own.ValueType(new Uniform<T>(screen, value), static x => x.Release());
    }
}

public static class Uniform
{
    public static Own<Uniform<T>> Create<T>(Screen screen, in T value) where T : unmanaged
    {
        return Uniform<T>.Create(screen, value);
    }
}

