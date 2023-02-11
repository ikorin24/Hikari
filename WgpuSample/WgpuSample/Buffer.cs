#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

public sealed class Buffer : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.Buffer> _native;
    private BufferUsages _usage;

    public IHostScreen? Screen => _screen;

    public BufferUsages Usage => _usage;

    internal Ref<Wgpu.Buffer> NativeRef => _native;

    private Buffer(IHostScreen screen, Box<Wgpu.Buffer> native, BufferUsages usage)
    {
        _screen = screen;
        _native = native;
        _usage = usage;
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

    public unsafe static Buffer Create<T>(IHostScreen screen, ReadOnlySpan<byte> data, BufferUsages usage) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromSpan(screen, data, usage);
    }

    public unsafe static Buffer Create(IHostScreen screen, byte* ptr, nuint byteLength, BufferUsages usage)
    {
        ArgumentNullException.ThrowIfNull(screen);
        return CreateFromPtr(screen, ptr, byteLength, usage);
    }

    private unsafe static Buffer CreateFromSpan<T>(IHostScreen screen, ReadOnlySpan<T> data, BufferUsages usage) where T : unmanaged
    {
        fixed(T* ptr = data) {
            var bytelen = (nuint)data.Length * (nuint)sizeof(T);
            return CreateFromPtr(screen, (byte*)ptr, bytelen, usage);
        }
    }

    private unsafe static Buffer CreateFromPtr(IHostScreen screen, byte* ptr, nuint byteLength, BufferUsages usage)
    {
        var screenRef = screen.AsRefChecked();
        usage.TryMapTo(out Wgpu.BufferUsages nativeUsage).WithDebugAssertTrue();
        var data = new Slice<u8>(ptr, byteLength);
        var buffer = screenRef.CreateBufferInit(data, nativeUsage);
        return new Buffer(screen, buffer, usage);
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyBuffer();
        _native = Box<Wgpu.Buffer>.Invalid;
        _screen = null;
    }
}
