#nullable enable
using Hikari.NativeBind;
using System.Diagnostics;

namespace Hikari;

public sealed class Surface : ITexture2DProvider
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.SurfaceTexture> _native;
    private Rust.OptionBox<Wgpu.TextureView> _viewNative;
    private EventSource<ITexture2DProvider> _textureChanged;
    private EventSource<ITextureViewProvider> _textureViewChanged;
    private Vector2u _currentSize;
    private bool _isSet;
    private uint _mipLevelCount;
    private uint _sampleCount;
    private TextureFormat _format;
    private TextureUsages _usage;

    private const string Message = "cannot use the SurfaceTexture now";

    public Screen Screen => _screen;

    public Vector2u GetCurrentSize()
    {
        if(_isSet == false) {
            ThrowHelper.ThrowInvalidOperation(Message);
        }
        return _currentSize;

    }

    public uint MipLevelCount
    {
        get
        {
            if(_isSet == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _mipLevelCount;
        }
    }
    public uint SampleCount
    {
        get
        {
            if(_isSet == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _sampleCount;
        }
    }

    public TextureFormat Format
    {
        get
        {
            if(_isSet == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _format;
        }
    }

    public TextureUsages Usage
    {
        get
        {
            if(_isSet == false) {
                ThrowHelper.ThrowInvalidOperation(Message);
            }
            return _usage;
        }
    }

    public TextureDimension Dimension => TextureDimension.D2;

    Event<ITexture2DProvider> ITextureProvider.TextureChanged => _textureChanged.Event;

    Event<ITextureViewProvider> ITextureViewProvider.TextureViewChanged => _textureViewChanged.Event;

    uint ITexture2DProvider.GetCurrentMipLevelCount() => MipLevelCount;

    uint ITexture2DProvider.GetCurrentSampleCount() => SampleCount;

    TextureFormat ITexture2DProvider.GetCurrentFormat() => Format;

    TextureUsages ITexture2DProvider.GetCurrentUsage() => Usage;

    TextureDimension ITexture2DProvider.GetCurrentDimension() => Dimension;

    internal Rust.Ref<Wgpu.Texture> GetCurrentTexture()
    {
        _screen.MainThread.ThrowIfNotMatched();
        return _native.Unwrap().AsRef().SurfaceTextureToTexture();
    }

    internal Rust.Ref<Wgpu.TextureView> GetCurrentTextureView()
    {
        _screen.MainThread.ThrowIfNotMatched();
        return _viewNative.Unwrap();
    }

    Rust.Ref<Wgpu.Texture> ITextureProvider.GetCurrentTexture() => GetCurrentTexture();

    Rust.Ref<Wgpu.TextureView> ITextureViewProvider.GetCurrentTextureView() => GetCurrentTextureView();


    internal Surface(Screen screen)
    {
        _screen = screen;
        _native = Rust.OptionBox<Wgpu.SurfaceTexture>.None;
    }

    internal void Set(Rust.Box<Wgpu.SurfaceTexture> native)
    {
        Debug.Assert(_screen.MainThread.IsCurrentThread);
        Debug.Assert(_native.IsNone);
        Debug.Assert(_viewNative.IsNone);

        Rust.Box<Wgpu.TextureView> view;
        try {
            Rust.Ref<Wgpu.Texture> texture = native.AsRef().SurfaceTextureToTexture();
            texture.GetTextureDescriptor(out var desc);
            Debug.Assert(desc.dimension == CH.TextureDimension.D2);

            _currentSize = new Vector2u(desc.size.width, desc.size.height);
            if(_isSet == false) {
                _mipLevelCount = desc.mip_level_count;
                _sampleCount = desc.sample_count;
                _format = desc.format.MapOrThrow();
                _usage = desc.usage.FlagsMap();
                _isSet = true;
            }
            view = texture.CreateTextureView(CH.TextureViewDescriptor.Default);
        }
        catch {
            native.DestroySurfaceTexture();
            throw;
        }
        _native = native;
        _viewNative = view;

        _textureChanged.Invoke(this);
        _textureViewChanged.Invoke(this);
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
        return native;
    }
}
