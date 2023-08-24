#nullable enable
using Hikari.NativeBind;

namespace Hikari;

internal readonly struct SurfaceData
{
    private readonly Rust.OptionBox<Wgpu.SurfaceTexture> _native;
    private readonly Rust.OptionBox<Wgpu.TextureView> _textureView;

    public static SurfaceData None => default;

    public SurfaceData(Rust.Box<Wgpu.SurfaceTexture> native, Rust.Box<Wgpu.TextureView> textureView)
    {
        _native = native;
        _textureView = textureView;
    }

    public unsafe bool IsSome(out Rust.Box<Wgpu.SurfaceTexture> surface, out Rust.Box<Wgpu.TextureView> textureView)
    {
        if(_native.IsSome(out var s)) {
            var t = _textureView;
            surface = s;
            textureView = *(Rust.Box<Wgpu.TextureView>*)&t;
            return true;
        }
        else {
            surface = Rust.Box<Wgpu.SurfaceTexture>.Invalid;
            textureView = Rust.Box<Wgpu.TextureView>.Invalid;
            return false;
        }
    }
}
