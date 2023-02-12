#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

public sealed class Buffer : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Buffer> _native;
    private BufferUsages? _usage;
    private usize _byteLen;

    public IHostScreen? Screen => _screen;

    public BufferUsages Usage => _usage.GetOrThrow();

    public usize ByteLength => _byteLen;

    internal Ref<Wgpu.Buffer> NativeRef => _native;

    private Buffer(IHostScreen screen, Box<Wgpu.Buffer> native, BufferUsages usage, usize byteLen)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
        _byteLen = byteLen;
    }

    ~Buffer() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroyBuffer();
        if(disposing) {
            _screen = null;
            _usage = null;
            _byteLen = 0;
        }
    }

    public static Buffer CreateUniformBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Uniform | BufferUsages.CopyDst);
    }

    public static Buffer CreateVertexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Vertex | BufferUsages.CopyDst);
    }

    public static Buffer CreateIndexBuffer<T>(IHostScreen screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, BufferUsages.Index | BufferUsages.CopyDst);
    }

    public unsafe static Buffer Create<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Buffer Create(IHostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Buffer CreateFromSpan<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (usize)data.Length * (usize)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Buffer CreateFromPtr(IHostScreen screen, byte* ptr, usize byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRefChecked();
        var data = new Slice<u8>(ptr, byteLength);
        var buffer = screenRef.CreateBufferInit(data, usage.FlagsMap());
        return new Buffer(screen, buffer, usage, byteLength);
    }
}
