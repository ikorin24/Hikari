#nullable enable
using Elffy.NativeBind;
using System;
using System.ComponentModel;

namespace Elffy;

internal readonly ref struct CommandEncoder
{
    private readonly Screen _screen;
    private readonly Rust.Box<Wgpu.CommandEncoder> _encoder;
    private readonly Rust.Box<Wgpu.SurfaceTexture> _surfaceTexture;
    private readonly Rust.Box<Wgpu.TextureView> _surfaceTextureView;

    public Rust.MutRef<Wgpu.CommandEncoder> NativeMut => _encoder;
    public Rust.Ref<Wgpu.SurfaceTexture> Surface => _surfaceTexture;
    public Rust.Ref<Wgpu.TextureView> SurfaceView => _surfaceTextureView;
    public Screen Screen => _screen;

    public static CommandEncoder Invalid => default;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CommandEncoder() => throw new NotSupportedException("Don't use default constructor.");

    internal CommandEncoder(
        Screen screen,
        Rust.Box<Wgpu.CommandEncoder> encoder,
        Rust.Box<Wgpu.SurfaceTexture> surfaceTexture,
        Rust.Box<Wgpu.TextureView> surfaceTextureView)
    {
        _screen = screen;
        _encoder = encoder;
        _surfaceTexture = surfaceTexture;
        _surfaceTextureView = surfaceTextureView;
    }

    public void Deconstruct(
        out Screen screen,
        out Rust.Box<Wgpu.CommandEncoder> encoder,
        out Rust.Box<Wgpu.SurfaceTexture> surfaceTexture,
        out Rust.Box<Wgpu.TextureView> surfaceTextureView
        )
    {
        screen = _screen;
        encoder = _encoder;
        surfaceTexture = _surfaceTexture;
        surfaceTextureView = _surfaceTextureView;
    }
}
