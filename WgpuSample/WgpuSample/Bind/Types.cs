#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Bind;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Opt<T> where T : unmanaged
{
    private readonly bool _exists;
    private readonly T _value;

    public Opt()
    {
        this = default;
    }

    public Opt(T value)
    {
        _exists = true;
        _value = value;
    }

    public static Opt<T> None => default;

    public static Opt<T> Some(T value) => new(value);

    public bool TryGetValue(out T value)
    {
        value = _value;
        return _exists;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap()
    {
        if(_exists == false) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("cannot get value");
        }
        return _value;
    }
}

internal struct Slice<T> where T : unmanaged
{
    public unsafe required T* data; // allow null
    public required usize len;

    public static Slice<T> Empty => default;

    [SetsRequiredMembers]
    public unsafe Slice(T* data, usize len)
    {
        this.data = data;
        this.len = len;
    }

    [SetsRequiredMembers]
    public unsafe Slice(T* data, int len)
    {
        this.data = data;
        this.len = checked((usize)len);
    }
}

internal static class Slice
{
    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(Span<T> fixedSpan) where T : unmanaged
        => FromFixedSpanUnsafe((ReadOnlySpan<T>)fixedSpan);

    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(ReadOnlySpan<T> fixedSpan) where T : unmanaged
    {
        var pointer = (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(fixedSpan));
        return new Slice<T>(pointer, fixedSpan.Length);
    }
}

internal readonly struct RangeU32
{
    private readonly u32 _start;
    private readonly u32 _end_excluded;

    public required u32 start
    {
        get => _start;
        init => _start = value;
    }

    public required u32 end_excluded
    {
        get => _end_excluded;
        init => _end_excluded = value;
    }

    [SetsRequiredMembers]
    public RangeU32(u32 start, u32 end_excluded)
    {
        _start = start;
        _end_excluded = end_excluded;
    }

    public static implicit operator RangeU32(Range range)
    {
        return new()
        {
            start = (u32)range.Start.Value,
            end_excluded = (u32)range.End.Value,
        };
    }
}

internal readonly struct RangeBoundsU64
{
    private readonly u64 _start;
    private readonly u64 _end_excluded;
    private readonly bool _has_start;
    private readonly bool _has_end_excluded;

    public required u64 start { get => start; init => start = value; }
    public required u64 end_excluded { get => end_excluded; init => end_excluded = value; }
    public required bool has_start { get => has_start; init => has_start = value; }
    public required bool has_end_excluded { get => has_end_excluded; init => has_end_excluded = value; }

    public static RangeBoundsU64 RangeFull => default;

    public static RangeBoundsU64 StartAt(u64 start) => new()
    {
        start = start,
        has_start = true,
        end_excluded = default,
        has_end_excluded = false,
    };

    public static RangeBoundsU64 EndAt(u64 endExcluded) => new()
    {
        start = default,
        has_start = false,
        end_excluded = endExcluded,
        has_end_excluded = true,
    };

    public static RangeBoundsU64 StartEnd(u64 start, u64 endExcluded) => new()
    {
        start = start,
        has_start = true,
        end_excluded = endExcluded,
        has_end_excluded = true,
    };

    public static RangeBoundsU64 StartLength(u64 start, u64 length) => new()
    {
        start = start,
        has_start = true,
        end_excluded = start + length,
        has_end_excluded = true,
    };
}

/// <summary>
/// `Option&lt;NonZeroU32&gt;` in Rust
/// </summary>
internal readonly struct NonZeroU32OrNone : IEquatable<NonZeroU32OrNone>
{
    private readonly u32 _value;
    public static NonZeroU32OrNone None => default;

    private NonZeroU32OrNone(u32 value) => _value = value;

    public static implicit operator NonZeroU32OrNone(u32 value) => new(value);

    public static bool operator ==(NonZeroU32OrNone left, NonZeroU32OrNone right) => left.Equals(right);

    public static bool operator !=(NonZeroU32OrNone left, NonZeroU32OrNone right) => !(left == right);

    public override bool Equals(object? obj) => obj is NonZeroU32OrNone none && Equals(none);

    public bool Equals(NonZeroU32OrNone other) => _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();
}
