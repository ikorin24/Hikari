﻿#nullable enable
using Cysharp.Threading.Tasks;
using Hikari.NativeBind;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hikari;

public sealed partial class Buffer : IReadBuffer<Buffer>
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.Buffer> _native;
    private readonly BufferUsages _usage;
    private readonly usize _byteLen;

    public Screen Screen => _screen;

    public BufferUsages Usage => _usage;

    public usize ByteLength => _byteLen;

    internal Rust.Ref<Wgpu.Buffer> NativeRef => _native.Unwrap();

    [Owned(nameof(Release))]
    private Buffer(Screen screen, usize size, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var screenRef = screen.AsRefChecked();
        _screen = screen;
        _native = screenRef.CreateBuffer(size, usage.FlagsMap());
        _usage = usage;
        _byteLen = size;
    }

    [Owned(nameof(Release))]
    private Buffer(Screen screen, ref readonly byte ptr, usize byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        unsafe {
            fixed(byte* p = &ptr) {
                var data = new CH.Slice<u8>(p, byteLength);
                _native = screen.AsRefChecked().CreateBufferInit(data, usage.FlagsMap());
            }
        }
        _screen = screen;
        _usage = usage;
        _byteLen = byteLength;
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

    public static Own<Buffer> Create<T>(Screen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        return Create(
            screen,
            in UnsafeEx.As<T, byte>(in data.GetReference()),
            checked((usize)(Unsafe.SizeOf<T>() * data.Length)),
            usage);
    }

    public static Own<Buffer> Create<T>(Screen screen, in T data, BufferUsages usage) where T : unmanaged
    {
        return Create(screen, in UnsafeEx.As<T, byte>(in data), (usize)Unsafe.SizeOf<T>(), usage);
    }

    public unsafe static Own<Buffer> Create(Screen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        return Create(screen, ref *ptr, byteLength, usage);
    }

    public BufferSlice Slice() => BufferSlice.Full(this);

    public BufferSlice Slice(u64 byteOffset, u64 byteLength)
    {
        return new BufferSlice(this, byteOffset, byteLength);
    }

    public unsafe void WriteData<T>(u64 offset, in T data) where T : unmanaged
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
        var slice = new CH.Slice<byte>(data, dataLen);
        screen.AsRefChecked().WriteBuffer(native, offset, slice);
    }

    public UniTask<byte[]> ReadToArray()
        => Slice().ReadToArray();

    public UniTask<int> Read(Memory<byte> dest) => Slice().Read(dest);

    public void ReadCallback(ReadOnlySpanAction<byte, Buffer> onRead, Action<Exception>? onException = null)
    {
        var slice = Slice();
        slice.ReadCallback((bytes, slice) => onRead(bytes, slice.Buffer), onException);
    }
}

public readonly struct BufferSlice : IEquatable<BufferSlice>, IReadBuffer<BufferSlice>
{
    private readonly Buffer? _buffer;
    private readonly u64 _offset;
    private readonly u64 _length;

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

    public u64 Length => _length;
    public u64 Offset => _offset;

    internal BufferSlice(Buffer buffer, u64 offset, u64 length)
    {
        _buffer = buffer;
        _offset = offset;
        _length = length;
    }

    public BufferSlice Slice(u64 byteOffset, u64 byteLength)
    {
        if(byteOffset >= _length) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(byteOffset));
        }
        if(byteOffset + byteLength > _length) {
            ThrowHelper.ThrowArgumentOutOfRange(nameof(byteLength));
        }
        if(_buffer is null) {
            ThrowHelper.ThrowInvalidOperation("buffer should not be null");
        }
        return new BufferSlice(_buffer, _offset + byteOffset, byteLength);
    }

    public static BufferSlice Full(Buffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return new BufferSlice(buffer, 0, buffer.ByteLength);
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        var buffer = Buffer;
        CheckUsageFlag(BufferUsages.CopyDst, buffer.Usage);
        if(_length < (u64)data.Length) {
            ThrowTooLong();
            [DoesNotReturn] static void ThrowTooLong() => throw new ArgumentException("data is too long to write");
        }
        buffer.WriteSpan(_offset, data);
    }

    public UniTask<byte[]> ReadToArray()
    {
        var completionSource = new UniTaskCompletionSource<byte[]>();
        ReadCore(
            (span, _) => completionSource.TrySetResult(span.ToArray()),
            (ex) => completionSource.TrySetException(ex));
        return completionSource.Task;
    }

    public UniTask<int> Read(Memory<byte> dest)
    {
        var completionSource = new UniTaskCompletionSource<int>();
        ReadCore(
            (bytes, _) =>
            {
                bytes.CopyTo(dest.Span);
                completionSource.TrySetResult(bytes.Length);
            },
            (ex) => completionSource.TrySetException(ex));
        return completionSource.Task;
    }

    public void ReadCallback(ReadOnlySpanAction<byte, BufferSlice> onRead, Action<Exception>? onException = null)
    {
        ArgumentNullException.ThrowIfNull(onRead);
        ReadCore(onRead, onException);
    }

    private void ReadCore(ReadOnlySpanAction<byte, BufferSlice> onRead, Action<Exception>? onException)
    {
        var buffer = Buffer;
        CheckUsageFlag(BufferUsages.CopySrc, buffer.Usage);
        var len = Length;
        var screen = buffer.Screen;
        if(len == 0) {
            onRead(Span<byte>.Empty, this);
        }
        else {
            var self = this;
            screen.AsRefChecked().ReadBuffer(
                Native(),
                (bytes) =>
                {
                    onRead(bytes, self);
                },
                onException);
        }
    }

    private static void CheckUsageFlag(BufferUsages needed, BufferUsages actual)
    {
        if(actual.HasFlag(needed) == false) {
            throw new InvalidOperationException($"'{needed}' flag is needed, but the flag the buffer has is '{actual}'.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CH.BufferSlice Native()
    {
        ArgumentNullException.ThrowIfNull(_buffer);
        var start = _offset;
        var end = start + Length;
        return new CH.BufferSlice
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
        return obj is BufferSlice slice && Equals(slice);
    }

    public bool Equals(BufferSlice other)
    {
        return EqualityComparer<Buffer?>.Default.Equals(_buffer, other._buffer) &&
               _offset == other._offset &&
               _length == other._length;
    }

    public override int GetHashCode() => HashCode.Combine(_buffer, _offset, _length);

    public static bool operator ==(BufferSlice left, BufferSlice right) => left.Equals(right);

    public static bool operator !=(BufferSlice left, BufferSlice right) => !(left == right);

    public static implicit operator BufferSlice(Buffer buffer) => Full(buffer);
}

internal interface IReadBuffer<TSelf>
{
    UniTask<byte[]> ReadToArray();

    UniTask<int> Read(Memory<byte> dest);

    void ReadCallback(ReadOnlySpanAction<byte, TSelf> onRead, Action<Exception>? onException = null);
}
