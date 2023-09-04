#nullable enable
using Hikari.NativeBind;

namespace Hikari;

internal sealed class SurfaceTexture : ITexture2D
{
    private Rust.OptionBox<Wgpu.Texture> _native;
    private Rust.OptionBox<Wgpu.TextureView> _viewNative;
    private Vector2u _size;

    public bool HasInnerNative => _native.IsSome(out _);

    public Vector2u Size => _size;

    internal Rust.Ref<Wgpu.Texture> NativeRef => _native.Unwrap();
    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef => _viewNative.Unwrap();

    Rust.Ref<Wgpu.Texture> ITexture.NativeRef => NativeRef;

    Rust.Ref<Wgpu.TextureView> ITexture.ViewNativeRef => ViewNativeRef;

    internal SurfaceTexture()
    {
        _native = Rust.OptionBox<Wgpu.Texture>.None;
    }

    public void SetTexture(Rust.Box<Wgpu.Texture> native, Rust.Box<Wgpu.TextureView> viewNative, Vector2u size)
    {
        _native = native;
        _viewNative = viewNative;
        _size = size;
    }

    public void ClearTexture()
    {
        _native = Rust.OptionBox<Wgpu.Texture>.None;
        _viewNative = Rust.OptionBox<Wgpu.TextureView>.None;
        _size = Vector2u.Zero;
    }
}

internal sealed class SurfaceTextureProvider : IRenderTextureProvider
{
    public Event<ITexture2D> Changed => throw new System.NotImplementedException();

    public uint MipLevelCount => throw new System.NotImplementedException();

    public uint SampleCount => throw new System.NotImplementedException();

    public TextureFormat Format => throw new System.NotImplementedException();

    public TextureUsages Usage => throw new System.NotImplementedException();

    public TextureDimension Dimension => throw new System.NotImplementedException();

    public ITexture2D GetCurrent()
    {
        throw new System.NotImplementedException();
    }
}
