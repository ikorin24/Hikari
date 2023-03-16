#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class Buffer : IEngineManaged
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.Buffer> _native;
    private BufferUsages? _usage;
    private usize _byteLen;

    public IHostScreen? Screen => _screen;

    public BufferUsages Usage => _usage.GetOrThrow();

    public usize ByteLength => _byteLen;

    internal Rust.Ref<Wgpu.Buffer> NativeRef => _native.Unwrap();

    private Buffer(IHostScreen screen, Rust.Box<Wgpu.Buffer> native, BufferUsages usage, usize byteLen)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
        _byteLen = byteLen;
    }

    ~Buffer() => Release(false);

    private static readonly Action<Buffer> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.Buffer>.None).IsSome(out var native)) {
            native.DestroyBuffer();
            if(disposing) {
                _screen = null;
                _usage = null;
                _byteLen = 0;
            }
        }
    }

    public static Own<Buffer> CreateUniformBuffer<T>(IHostScreen screen, in T data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, new ReadOnlySpan<T>(in data), BufferUsages.Uniform | BufferUsages.CopyDst);
    }

    public static Own<Buffer> CreateVertexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Vertex | BufferUsages.CopyDst);
    }

    public static Own<Buffer> CreateIndexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Index | BufferUsages.CopyDst);
    }

    public unsafe static Own<Buffer> Create<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Own<Buffer> Create(IHostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Own<Buffer> CreateFromSpan<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (usize)data.Length * (usize)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Own<Buffer> CreateFromPtr(IHostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRefChecked();
        var data = new CE.Slice<u8>(ptr, byteLength);
        var buffer = screenRef.CreateBufferInit(data, usage.FlagsMap());
        return Own.New(new Buffer(screen, buffer, usage, byteLength), _release);
    }

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
