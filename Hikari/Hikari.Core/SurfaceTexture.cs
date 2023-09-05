#nullable enable
using Hikari.NativeBind;
using System.Diagnostics;

namespace Hikari;

internal sealed class SurfaceTexture : ITexture2D, ITextureView, IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.SurfaceTexture> _native;
    private Rust.OptionBox<Wgpu.TextureView> _viewNative;
    private bool _hasDesc;
    private CH.TextureDescriptor _desc;

    private const string Message = "cannot use the SurfaceTexture now";

    public Screen Screen => _screen;
    public Vector2u Size
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return new Vector2u(_desc.size.width, _desc.size.height);
        }
    }

    public uint MipLevelCount
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _desc.mip_level_count;
        }
    }

    public uint SampleCount
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _desc.sample_count;
        }
    }

    public TextureFormat Format
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _desc.format.MapOrThrow();
        }
    }

    public TextureUsages Usage
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _desc.usage.FlagsMap();
        }
    }

    public TextureDimension Dimension
    {
        get
        {
            if(_hasDesc == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _desc.dimension.MapOrThrow();
        }
    }

    internal Rust.Ref<Wgpu.Texture> NativeRef
    {
        get
        {
            _screen.MainThread.ThrowIfNotMatched();
            return _native.Unwrap().AsRef().SurfaceTextureToTexture();
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

    Rust.Ref<Wgpu.TextureView> ITextureView.ViewNativeRef => ViewNativeRef;

    public bool IsManaged => _native.IsNone == false;

    internal SurfaceTexture(Screen screen)
    {
        _screen = screen;
        _native = Rust.OptionBox<Wgpu.SurfaceTexture>.None;
    }

    public void Validate()
    {
        if(_native.IsNone) {
            ThrowHelper.ThrowInvalidOperation(Message);
        }
        IScreenManaged.DefaultValidate(this);
    }

    internal void Set(Rust.Box<Wgpu.SurfaceTexture> native)
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        Debug.Assert(_native.IsNone);
        Debug.Assert(_viewNative.IsNone);

        Rust.Box<Wgpu.TextureView> view;
        try {
            Rust.Ref<Wgpu.Texture> texture = native.AsRef().SurfaceTextureToTexture();
            texture.GetTextureDescriptor(out _desc);
            Debug.Assert(_desc.dimension == CH.TextureDimension.D2);
            _hasDesc = true;
            view = texture.CreateTextureView(CH.TextureViewDescriptor.Default);
        }
        catch {
            native.DestroySurfaceTexture();
            throw;
        }
        _native = native;
        _viewNative = view;
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
        _hasDesc = false;
        return native;
    }
}
