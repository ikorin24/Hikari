#nullable enable
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Internal;

internal ref struct Utf8StringBuilder
{
    private byte[] _buffer;
    private int _length;

    public readonly ReadOnlySpan<byte> Utf8String => _buffer.AsSpan(0, _length);

    public Utf8StringBuilder()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(0);
    }

    public Utf8StringBuilder(int capacity = 0)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(capacity);
    }

    public void AppendLine(ReadOnlySpan<byte> utf8String)
    {
        Append(utf8String);
        if(OperatingSystem.IsWindows()) {
            Append("\r\n"u8);
        }
        else {
            Append("\n"u8);
        }
    }

    [SkipLocalsInit]
    public void Append(uint value)
    {
        Span<byte> buf = stackalloc byte[10];
        var result = Utf8Formatter.TryFormat(value, buf, out var writtenLen);
        Debug.Assert(result);
        Append(buf.Slice(0, writtenLen));
    }

    public void Append(scoped ReadOnlySpan<byte> utf8String)
    {
        if(utf8String.Length > _buffer.Length - _length) {
            var c = int.Max(_length + utf8String.Length, checked(_buffer.Length * 2));
            var newBuffer = ArrayPool<byte>.Shared.Rent(c);
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
        Debug.Assert(_buffer.Length - _length >= utf8String.Length);
        utf8String.CopyTo(_buffer.AsSpan(_length));
        _length += utf8String.Length;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
