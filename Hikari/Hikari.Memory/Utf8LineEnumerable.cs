﻿#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hikari.Unsafes;

namespace Hikari;

[DebuggerTypeProxy(typeof(Utf8StringsDebuggerTypeProxy))]
[DebuggerDisplay("ReadOnlySpan<byte>[{Count()}]")]
public readonly ref struct Utf8LineEnumerable
{
    private readonly ReadOnlySpan<byte> _str;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Utf8LineEnumerable(ReadOnlySpan<byte> str) => _str = str;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new Enumerator(_str);

    /// <summary>Count the lines. (The operation is O(N), N is the length of the original string.)</summary>
    /// <returns>number of the lines</returns>
    public int Count()
    {
        var count = 0;
        foreach(var line in this) {
            count++;
        }
        return count;
    }

    /// <summary>Copy to the array of <see cref="string"/></summary>
    /// <returns>array of <see cref="string"/></returns>
    public string[] ToStringArray()
    {
        var array = new string[Count()];
        var i = 0;
        var enc = Encoding.UTF8;
        foreach(var line in this) {
            array[i++] = enc.GetString(line);
        }
        return array;
    }

    public ref struct Enumerator
    {
        private ReadOnlySpan<byte> _str;
        private ReadOnlySpan<byte> _current;

        public ReadOnlySpan<byte> Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<byte> str)
        {
            _str = str;
            _current = ReadOnlySpan<byte>.Empty;
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if(_str.IsEmpty) {
                return false;
            }

            // LF (\n) and CRLF (\r\n) are supported.
            // CR (\r) is not supported.
            (_current, _str) = _str.Split2((byte)'\n');
            if(_current.IsEmpty == false && _current.At(_current.Length - 1) == '\r') {
                _current = _current.SliceUnsafe(0, _current.Length - 1);
            }
            return true;
        }
    }
}
