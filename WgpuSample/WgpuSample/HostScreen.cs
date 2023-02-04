#nullable enable
using Elffy.Bind;
using System;
using EnumMapping;
using System.Runtime.CompilerServices;

namespace Elffy;

internal sealed class HostScreen : IHostScreen
{
    private Box<CE.HostScreen> _screen;
    private CE.HostScreenId _id;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;

    public event Action<IHostScreen, uint, uint>? Resized;
    public event Action<IHostScreen>? RedrawRequested;

    public Ref<CE.HostScreen> Screen => _screen;
    public nuint Id => _id.AsNumber();

    public TextureFormat SurfaceFormat
    {
        get
        {
            ThrowIfNotInit();
            return _surfaceFormat;
        }
    }

    public GraphicsBackend Backend
    {
        get
        {
            ThrowIfNotInit();
            return _backend;
        }
    }

    internal HostScreen(Box<CE.HostScreen> screen, CE.HostScreenId id)
    {
        _screen = screen;
        _id = id;
    }

    internal void OnInitialize(in CE.HostScreenInfo info)
    {
        info.surface_format
            .Unwrap()
            .TryMapTo(out TextureFormat surfaceFormat)
            .WithDebugAssertTrue();
        _surfaceFormat = surfaceFormat;
        info.backend
            .TryMapTo(out GraphicsBackend backend)
            .WithDebugAssertTrue();
        _backend = backend;
    }

    internal void OnCleared()
    {
        _screen.AsRef().ScreenRequestRedraw();
    }

    internal void OnRedrawRequested()
    {
        // TODO:
    }

    internal void OnResized(uint width, uint height)
    {
        Resized?.Invoke(this, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfNotInit()
    {
        if(_initialized == false) {
            Throw();
            static void Throw() => throw new InvalidOperationException("not initialized");
        }
    }
}

public interface IHostScreen
{
    event Action<IHostScreen, uint, uint>? Resized;
    nuint Id { get; }
    TextureFormat SurfaceFormat { get; }
    GraphicsBackend Backend { get; }
}

public readonly ref struct HostScreenConfig
{
    public required WindowStyle Style { get; init; }
    public required u32 Width { get; init; }
    public required u32 Height { get; init; }
    public required GraphicsBackend Backend { get; init; }

    internal CE.HostScreenConfig ToCoreType()
    {
        Style.TryMapTo(out CE.WindowStyle style).WithDebugAssertTrue();
        Backend.TryMapTo(out Wgpu.Backends backend).WithDebugAssertTrue();
        return new CE.HostScreenConfig
        {
            title = Slice<u8>.Empty,
            style = style,
            width = Width,
            height = Height,
            backend = backend,
        };
    }
}
