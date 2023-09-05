#nullable enable
using Hikari.NativeBind;
using System.Diagnostics;

namespace Hikari;

internal sealed class SurfaceTexture : ITexture2D, IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.SurfaceTexture> _native;
    private Rust.OptionBox<Wgpu.TextureView> _viewNative;
    private Vector2u _size;

    public Screen Screen => _screen;
    public Vector2u Size => _size;

    internal Rust.Ref<Wgpu.Texture> NativeRef
    {
        get
        {
            _screen.MainThread.ThrowIfNotMatched();
            return _native.Unwrap().AsRef().SurfaceTextureToTexture(out _);
        }
    }

    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef
    {
        get
        {
            _screen.MainThread.ThrowIfNotMatched();
            return _viewNative.Unwrap();
        }
    }

    Rust.Ref<Wgpu.Texture> ITexture.NativeRef => NativeRef;

    Rust.Ref<Wgpu.TextureView> ITexture.ViewNativeRef => ViewNativeRef;

    public bool IsManaged => _native.IsNone == false;

    internal SurfaceTexture(Screen screen)
    {
        _screen = screen;
        _native = Rust.OptionBox<Wgpu.SurfaceTexture>.None;
    }

    public void Validate()
    {
        if(_native.IsNone) {
            ThrowHelper.ThrowInvalidOperation("");
        }
        IScreenManaged.DefaultValidate(this);
    }

    internal void Set(Rust.Box<Wgpu.SurfaceTexture> native)
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        Debug.Assert(_native.IsNone);
        Debug.Assert(_viewNative.IsNone);

        Rust.Box<Wgpu.TextureView> view;
        Wgpu.Extent3d size;
        try {
            view = native
                .AsRef()
                .SurfaceTextureToTexture(out size)
                .CreateTextureView(CH.TextureViewDescriptor.Default);
        }
        catch {
            native.DestroySurfaceTexture();
            throw;
        }
        _native = native;
        _viewNative = view;
        _size = new Vector2u(size.width, size.height);
    }

    internal Rust.Box<Wgpu.SurfaceTexture> Remove()
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        Debug.Assert(_native.IsNone == false);
        Debug.Assert(_viewNative.IsNone == false);

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
