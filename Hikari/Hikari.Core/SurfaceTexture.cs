#nullable enable
using Hikari.NativeBind;

namespace Hikari;

internal sealed class SurfaceTexture : ITexture2D
{
    private Rust.OptionBox<Wgpu.SurfaceTexture> _native;
    private Rust.OptionBox<Wgpu.TextureView> _viewNative;
    private Vector2u _size;

    internal bool HasInnerNative => _native.IsSome(out _);

    public Vector2u Size => _size;

    internal Rust.Ref<Wgpu.Texture> NativeRef => _native.Unwrap().AsRef().SurfaceTextureToTexture();
    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef => _viewNative.Unwrap();

    Rust.Ref<Wgpu.Texture> ITexture.NativeRef => NativeRef;

    Rust.Ref<Wgpu.TextureView> ITexture.ViewNativeRef => ViewNativeRef;

    internal SurfaceTexture()
    {
        _native = Rust.OptionBox<Wgpu.SurfaceTexture>.None;
    }

    internal void Set(Rust.Box<Wgpu.SurfaceTexture> native, Vector2u size)
    {
        Rust.Box<Wgpu.TextureView> view;
        try {
            view = native
                .AsRef()
                .SurfaceTextureToTexture()
                .CreateTextureView(CH.TextureViewDescriptor.Default);
        }
        catch {
            native.DestroySurfaceTexture();
            throw;
        }
        _native = native;
        _viewNative = view;
        _size = size;
    }

    internal Rust.Box<Wgpu.SurfaceTexture> Remove()
    {
        var native = _native.Unwrap();
        _native = Rust.OptionBox<Wgpu.SurfaceTexture>.None;
        _viewNative.Unwrap().DestroyTextureView();
        _viewNative = Rust.OptionBox<Wgpu.TextureView>.None;
        _size = Vector2u.Zero;
        return native;
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
