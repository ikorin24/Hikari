#nullable enable
using Elffy.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Buffer : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Buffer> _native;
    private readonly BufferUsages? _usage;
    private readonly usize _byteLen;

    public Screen Screen => _screen;

    public BufferUsages Usage => _usage.GetOrThrow();

    public usize ByteLength => _byteLen;

    internal Rust.Ref<Wgpu.Buffer> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private Buffer(Screen screen, Rust.Box<Wgpu.Buffer> native, BufferUsages usage, usize byteLen)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
        _byteLen = byteLen;
    }

    ~Buffer() => Release(false);

    public void Validate() => IScreenManaged.DefaultValidate(this);

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

    public unsafe static Own<Buffer> CreateZeroed(Screen screen, usize size, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);

        var screenRef = screen.AsRefChecked();
        var bufferNative = screenRef.CreateBuffer(size, usage.FlagsMap());
        var buffer = new Buffer(screen, bufferNative, usage, size);
        return Own.New(buffer, static x => SafeCast.As<Buffer>(x).Release());
    }

    public unsafe static Own<Buffer> Create<T>(Screen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Own<Buffer> Create<T>(Screen screen, in T data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, new ReadOnlySpan<T>(in data), usage);
    }

    public unsafe static Own<Buffer> Create(Screen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Own<Buffer> CreateFromSpan<T>(Screen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (usize)data.Length * (usize)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Own<Buffer> CreateFromPtr(Screen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRefChecked();
        var data = new CE.Slice<u8>(ptr, byteLength);
        var bufferNative = screenRef.CreateBufferInit(data, usage.FlagsMap());
        var buffer = new Buffer(screen, bufferNative, usage, byteLength);
        return Own.New(buffer, static x => SafeCast.As<Buffer>(x).Release());
    }

    public BufferSlice<u8> Slice() => BufferSlice<u8>.Full(this);

    public BufferSlice<u8> Slice(u64 byteOffset, u64 byteLength)
    {
        return new BufferSlice<u8>(this, byteOffset, byteLength);
    }

    public BufferSlice<T> Slice<T>() where T : unmanaged => BufferSlice<T>.Full(this);

    public unsafe void Write<T>(u64 offset, in T data) where T : unmanaged
    {
        fixed(T* p = &data) {
            WriteBytes(offset, (byte*)p, (usize)sizeof(T));
        }
    }

    public unsafe void WriteSpan<T>(u64 offset, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed(T* p = data) {
            var len = (usize)sizeof(T) * (usize)data.Length;
            WriteBytes(offset, (byte*)p, len);
        }
    }

    public unsafe void WriteBytes(u64 offset, byte* data, usize dataLen)
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
    private readonly Buffer? _buffer;
    private readonly u64 _startByte;
    private readonly u64 _elementCount;

    public Buffer Buffer
    {
        get
        {
            if(_buffer is null) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("invalid buffer slice instance");
            }
            return _buffer;
        }
    }

    public u64 Length => _elementCount;
    public u64 StartByteOffset => _startByte;
    public u64 ByteLength => _elementCount * (u64)Unsafe.SizeOf<T>();

    internal BufferSlice(Buffer buffer, u64 startByte, u64 elementCount)
    {
        _buffer = buffer;
        _startByte = startByte;
        _elementCount = elementCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferSlice<U> Cast<U>() where U : unmanaged
    {
        var (q, rem) = u64.DivRem(ByteLength, (u64)Unsafe.SizeOf<U>());
        if(rem != 0) {
            Throw();
            static void Throw() => throw new InvalidOperationException($"byte length is not multiple of the size of {typeof(U).FullName}");
        }
        return new BufferSlice<U>(Buffer, _startByte, q);
    }


    public static BufferSlice<T> Full(Buffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var (q, rem) = u64.DivRem(buffer.ByteLength, (u64)Unsafe.SizeOf<T>());
        if(rem != 0) {
            Throw();
            static void Throw() => throw new InvalidOperationException($"byte length is not multiple of the size of {typeof(T).FullName}");
        }
        return new BufferSlice<T>(buffer, 0, q);
    }

    public void Write(ReadOnlySpan<T> data)
    {
        if(Length < (u64)data.Length) {
            ThrowTooLong();
            [DoesNotReturn] static void ThrowTooLong() => throw new ArgumentException("data is too long to write");
        }
        Buffer.WriteSpan(_startByte, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CE.BufferSlice Native()
    {
        ArgumentNullException.ThrowIfNull(_buffer);
        var start = _startByte;
        var end = start + ByteLength;
        return new CE.BufferSlice
        {
            buffer = _buffer.NativeRef,
            range = new()
            {
                start = start,
                has_start = true,
                end_excluded = end,
                has_end_excluded = true,
            },
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is BufferSlice<T> slice && Equals(slice);
    }

    public bool Equals(BufferSlice<T> other)
    {
        return EqualityComparer<Buffer?>.Default.Equals(_buffer, other._buffer) &&
               _startByte == other._startByte &&
               _elementCount == other._elementCount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_buffer, _startByte, _elementCount);
    }

    public static bool operator ==(BufferSlice<T> left, BufferSlice<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BufferSlice<T> left, BufferSlice<T> right)
    {
        return !(left == right);
    }
}
