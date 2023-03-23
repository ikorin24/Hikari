#nullable enable
using Elffy.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Buffer : IEngineManaged
{
    private readonly HostScreen _screen;
    private Rust.OptionBox<Wgpu.Buffer> _native;
    private readonly BufferUsages? _usage;
    private readonly usize _byteLen;

    public HostScreen Screen => _screen;

    public BufferUsages Usage => _usage.GetOrThrow();

    public usize ByteLength => _byteLen;

    internal Rust.Ref<Wgpu.Buffer> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private Buffer(HostScreen screen, Rust.Box<Wgpu.Buffer> native, BufferUsages usage, usize byteLen)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
        _byteLen = byteLen;
    }

    ~Buffer() => Release(false);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.Buffer>.None).IsSome(out var native)) {
            native.DestroyBuffer();
            if(disposing) {
            }
        }
    }

    public static Own<Buffer> CreateUniformBuffer<T>(HostScreen screen, in T data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, new ReadOnlySpan<T>(in data), BufferUsages.Uniform | BufferUsages.CopyDst);
    }

    public static Own<Buffer> CreateVertexBuffer<T>(HostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Vertex | BufferUsages.CopyDst);
    }

    public static Own<Buffer> CreateIndexBuffer<T>(HostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Index | BufferUsages.CopyDst);
    }

    public unsafe static Own<Buffer> Create<T>(HostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Own<Buffer> Create(HostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Own<Buffer> CreateFromSpan<T>(HostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (usize)data.Length * (usize)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Own<Buffer> CreateFromPtr(HostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRefChecked();
        var data = new CE.Slice<u8>(ptr, byteLength);
        var bufferNative = screenRef.CreateBufferInit(data, usage.FlagsMap());
        var buffer = new Buffer(screen, bufferNative, usage, byteLength);
        return Own.RefType(buffer, static x => SafeCast.As<Buffer>(x).Release());
    }

    public BufferSlice Slice()
        => BufferSlice.Full(this);

    public BufferSlice Slice(u64? start, u64? end)
        => BufferSlice.Range(this, start, end);

    public BufferSlice<T> Slice<T>() where T : unmanaged
        => BufferSlice<T>.Full(this);

    public BufferSlice<T> Slice<T>(u64? start, u64? end) where T : unmanaged
        => BufferSlice<T>.Range(this, start, end);

    public unsafe void Write<T>(u64 offset, in T data) where T : unmanaged
    {
        fixed(T* p = &data) {
            Write(offset, (byte*)p, (usize)sizeof(T));
        }
    }

    public unsafe void Write<T>(u64 offset, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed(T* p = data) {
            var len = (usize)sizeof(T) * (usize)data.Length;
            Write(offset, (byte*)p, len);
        }
    }

    public unsafe void Write(u64 offset, byte* data, usize dataLen)
    {
        var native = NativeRef;
        var screen = Screen;
        if(screen == null) { return; }
        if(offset >= _byteLen) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if(offset + dataLen > _byteLen) {
            throw new ArgumentOutOfRangeException(nameof(dataLen));
        }
        if(dataLen == 0) {
            return;
        }
        var slice = new CE.Slice<byte>(data, dataLen);
        screen.AsRefChecked().WriteBuffer(native, offset, slice);
    }
}

public readonly struct BufferSlice<T> : IEquatable<BufferSlice<T>> where T : unmanaged
{
    private readonly BufferSlice _inner;

    internal Buffer Buffer => _inner.Buffer;
    internal u64? Start => _inner.Start;
    internal u64? End => _inner.End;

    internal BufferSlice(in BufferSlice slice)
    {
        _inner = slice;
    }

    public static BufferSlice<T> Full(Buffer buffer)
        => BufferSlice.Full(buffer).OfType<T>();

    public static BufferSlice<T> StartAt(Buffer buffer, u64 start)
        => BufferSlice.StartAt(buffer, start).OfType<T>();

    public static BufferSlice<T> EndAt(Buffer buffer, u64 end)
        => BufferSlice.EndAt(buffer, end).OfType<T>();

    public static BufferSlice<T> Range(Buffer buffer, u64 start, u64 end)
        => BufferSlice.Range(buffer, start, end).OfType<T>();

    public static BufferSlice<T> Range(Buffer buffer, u64? start, u64? end)
        => BufferSlice.Range(buffer, start, end).OfType<T>();

    public BufferSlice Untyped() => _inner;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CE.BufferSlice Native() => _inner.Native();

    public override bool Equals(object? obj) => obj is BufferSlice<T> slice && Equals(slice);

    public bool Equals(BufferSlice<T> other) => _inner.Equals(other._inner);

    public override int GetHashCode() => HashCode.Combine(_inner);

    public static implicit operator BufferSlice(BufferSlice<T> self) => self._inner;

    public static bool operator ==(BufferSlice<T> left, BufferSlice<T> right) => left.Equals(right);

    public static bool operator !=(BufferSlice<T> left, BufferSlice<T> right) => !(left == right);
}

public readonly struct BufferSlice : IEquatable<BufferSlice>
{
    private readonly Buffer? _buffer;
    private readonly u64? _start;
    private readonly u64? _end;

    internal Buffer Buffer
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_buffer);
            return _buffer;
        }
    }
    internal u64? Start => _start;
    internal u64? End => _end;

    private BufferSlice(Buffer buffer, u64? start, u64? end)
    {
        _buffer = buffer;
        _start = start;
        _end = end;
    }

    public BufferSlice<T> OfType<T>() where T : unmanaged
        => new BufferSlice<T>(this);

    public static BufferSlice Full(Buffer buffer)
        => new BufferSlice(buffer, null, null);

    public static BufferSlice StartAt(Buffer buffer, u64 start)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if(start > buffer.ByteLength) {
            ThrowOutOfRange(nameof(start));
        }
        return new BufferSlice(buffer, start, null);
    }

    public static BufferSlice EndAt(Buffer buffer, u64 end)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if(end > buffer.ByteLength) {
            ThrowOutOfRange(nameof(end));
        }
        return new BufferSlice(buffer, null, end);
    }

    public static BufferSlice Range(Buffer buffer, u64 start, u64 end)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if(start > buffer.ByteLength) {
            ThrowOutOfRange(nameof(start));
        }
        if(start > end) {
            ThrowOutOfRange(nameof(end));
        }
        if(end > buffer.ByteLength) {
            ThrowOutOfRange(nameof(end));
        }
        return new BufferSlice(buffer, start, end);
    }

    public static BufferSlice Range(Buffer buffer, u64? start, u64? end)
    {
        return (start, end) switch
        {
            (u64 s, u64 e) => Range(buffer, s, e),
            (u64 s, null) => StartAt(buffer, s),
            (null, u64 e) => EndAt(buffer, e),
            (null, null) => Full(buffer),
        };
    }

    [DoesNotReturn]
    private static void ThrowOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CE.BufferSlice Native()
    {
        ArgumentNullException.ThrowIfNull(_buffer);
        var start = _start;
        var end = _end;
        return new CE.BufferSlice
        {
            buffer = _buffer.NativeRef,
            range = new()
            {
                start = start ?? default,
                has_start = start.HasValue,
                end_excluded = end ?? default,
                has_end_excluded = end.HasValue,
            },
        };
    }

    public override bool Equals(object? obj) => obj is BufferSlice slice && Equals(slice);

    public bool Equals(BufferSlice other)
    {
        return EqualityComparer<Buffer?>.Default.Equals(_buffer, other._buffer) &&
               _start == other._start &&
               _end == other._end;
    }

    public override int GetHashCode() => HashCode.Combine(_buffer, _start, _end);

    public static bool operator ==(BufferSlice left, BufferSlice right) => left.Equals(right);

    public static bool operator !=(BufferSlice left, BufferSlice right) => !(left == right);
}
