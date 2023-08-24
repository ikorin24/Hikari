#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Elffy;

namespace Elffy;

partial class SpanExtension
{
    public static SplitUtf16Strings Split(this ReadOnlySpan<char> str, char separator, StringSplitOptions options = StringSplitOptions.None)
    {
        return new SplitUtf16Strings(str, separator, options);
    }

    public static SplitUtf16Strings Split(this ReadOnlySpan<char> str, ReadOnlySpan<char> separator, StringSplitOptions options = StringSplitOptions.None)
    {
        return new SplitUtf16Strings(str, separator, options);
    }

    public static ReadOnlySpanTuple<char, char> Split2(this ReadOnlySpan<char> str, char separator)
    {
        for(int i = 0; i < str.Length; i++) {
            if(str[i] == separator) {
                var latterStart = Math.Min(i + 1, str.Length);
                return new(str.Slice(0, i), str.Slice(latterStart, str.Length - latterStart));
            }
        }
        return new(str, ReadOnlySpan<char>.Empty);
    }

    public static ReadOnlySpanTuple<char, char> Split2(this ReadOnlySpan<char> str, ReadOnlySpan<char> separator)
    {
        if((uint)separator.Length > (uint)str.Length) {
            return new(str, ReadOnlySpan<char>.Empty);
        }
        var maxLoop = str.Length - separator.Length + 1;
        for(int i = 0; i < maxLoop; i++) {
            if(str.Slice(i, separator.Length).SequenceEqual(separator)) {
                var latterStart = Math.Min(i + separator.Length, str.Length);
                return new(str.Slice(0, i), str.Slice(latterStart, str.Length - latterStart));
            }
        }
        return new(str, ReadOnlySpan<char>.Empty);
    }
}

[DebuggerTypeProxy(typeof(Utf16StringsDebuggerTypeProxy))]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly ref struct SplitUtf16Strings
{
    private readonly ReadOnlySpan<char> _str;
    private readonly bool _isSeparatorSingleChar;
    private readonly char _separator;
    private readonly ReadOnlySpan<char> _spanSeparator;
    private readonly StringSplitOptions _options;

    private string DebuggerDisplay => $"ReadOnlySpan<char>[{Count()}]";

    internal SplitUtf16Strings(ReadOnlySpan<char> str, char separator, StringSplitOptions options)
    {
        _str = str;
        _isSeparatorSingleChar = true;
        _separator = separator;
        _spanSeparator = default;
        _options = options;
    }

    internal SplitUtf16Strings(ReadOnlySpan<char> str, ReadOnlySpan<char> separator, StringSplitOptions options)
    {
        _str = str;
        _isSeparatorSingleChar = false;
        _separator = default;
        _spanSeparator = separator;
        _options = options;
    }

    public int Count()
    {
        var count = 0;
        foreach(var _ in this) {
            count++;
        }
        return count;
    }

    public string[] ToStringArray()
    {
        var list = new List<string>();
        foreach(var split in this) {
            list.Add(split.ToString());
        }
        return list.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => _isSeparatorSingleChar ? new Enumerator(_str, _separator, _options) : new Enumerator(_str, _spanSeparator, _options);

    public ref struct Enumerator
    {
        private ReadOnlySpan<char> _str;
        private ReadOnlySpan<char> _current;
        private readonly bool _isSeparatorChar;
        private readonly char _separatorChar;
        private readonly ReadOnlySpan<char> _separatorStr;
        private readonly StringSplitOptions _options;

        public ReadOnlySpan<char> Current => _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ReadOnlySpan<char> str, char separator, StringSplitOptions options)
        {
            _str = str;
            _current = ReadOnlySpan<char>.Empty;
            _isSeparatorChar = true;
            _separatorChar = separator;
            _separatorStr = default;
            _options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ReadOnlySpan<char> str, ReadOnlySpan<char> separator, StringSplitOptions options)
        {
            _str = str;
            _current = ReadOnlySpan<char>.Empty;
            _isSeparatorChar = false;
            _separatorChar = default;
            _separatorStr = separator;
            _options = options;
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            if(_str.IsEmpty) {
                return false;
            }
            if(_isSeparatorChar) {
                (_current, _str) = _str.Split2(_separatorChar);
            }
            else {
                (_current, _str) = _str.Split2(_separatorStr);
            }

            if((_options & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries) {
                _current = _current.Trim();
            }
            if((_options & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries && _current.IsEmpty) {
                goto Next;
            }
            return true;
        }
    }
}

internal sealed class Utf16StringsDebuggerTypeProxy
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string[] _array;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string[] Items => _array;

    //public Utf16StringsDebuggerTypeProxy(Utf8LineEnumerable lines)
    //{
    //    _array = lines.ToStringArray();
    //}

    public Utf16StringsDebuggerTypeProxy(SplitUtf16Strings strings)
    {
        _array = strings.ToStringArray();
    }
}
