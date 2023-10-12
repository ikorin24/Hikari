#nullable enable
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UStr = System.ReadOnlySpan<byte>;

namespace Hikari.Internal;

internal ref struct Utf8StringBuilder
{
    private byte[] _buffer;
    private int _length;

    private static UStr NL => OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8;

    public readonly UStr Utf8String => _buffer.AsSpan(0, _length);

    public Utf8StringBuilder()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(0);
    }

    public Utf8StringBuilder(int capacity = 0)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(capacity);
    }

    public void AppendLine(UStr utf8String)
    {
        Append(utf8String);
        Append(NL);
    }

    [SkipLocalsInit]
    public void Append(uint value)
    {
        Span<byte> buf = stackalloc byte[10];
        var result = Utf8Formatter.TryFormat(value, buf, out var writtenLen);
        Debug.Assert(result);
        Append(buf.Slice(0, writtenLen));
    }

    public void Append(scoped UStr utf8String)
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

    public static Utf8StringBuilder FromLines(UStr l1, UStr l2)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + NL.Length * 2);
        b.AppendLine(l1);
        b.AppendLine(l2);
        return b;
    }

    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + NL.Length * 3);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + NL.Length * 4);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + NL.Length * 5);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + NL.Length * 6);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + NL.Length * 7);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + NL.Length * 8);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + NL.Length * 9);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + NL.Length * 10);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10, UStr l11)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + l11.Length + NL.Length * 11);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        b.AppendLine(l11);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10, UStr l11, UStr l12)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + l11.Length + l12.Length + NL.Length * 12);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        b.AppendLine(l11);
        b.AppendLine(l12);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10, UStr l11, UStr l12, UStr l13)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + l11.Length + l12.Length + l13.Length + NL.Length * 13);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        b.AppendLine(l11);
        b.AppendLine(l12);
        b.AppendLine(l13);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10, UStr l11, UStr l12, UStr l13, UStr l14)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + l11.Length + l12.Length + l13.Length + l14.Length + NL.Length * 14);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        b.AppendLine(l11);
        b.AppendLine(l12);
        b.AppendLine(l13);
        b.AppendLine(l14);
        return b;
    }
    public static Utf8StringBuilder FromLines(UStr l1, UStr l2, UStr l3, UStr l4, UStr l5, UStr l6, UStr l7, UStr l8, UStr l9, UStr l10, UStr l11, UStr l12, UStr l13, UStr l14, UStr l15)
    {
        var b = new Utf8StringBuilder(l1.Length + l2.Length + l3.Length + l4.Length + l5.Length + l6.Length + l7.Length + l8.Length + l9.Length + l10.Length + l11.Length + l12.Length + l13.Length + l14.Length + l15.Length + NL.Length * 15);
        b.AppendLine(l1);
        b.AppendLine(l2);
        b.AppendLine(l3);
        b.AppendLine(l4);
        b.AppendLine(l5);
        b.AppendLine(l6);
        b.AppendLine(l7);
        b.AppendLine(l8);
        b.AppendLine(l9);
        b.AppendLine(l10);
        b.AppendLine(l11);
        b.AppendLine(l12);
        b.AppendLine(l13);
        b.AppendLine(l14);
        b.AppendLine(l15);
        return b;
    }
}
