#nullable enable
using Elffy.NativeBind;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elffy;

public sealed class HostScreen
{
    private Rust.OptionBox<CE.HostScreen> _native;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;
    private string _title = "";
    private Mouse _mouse;
    private Own<Texture> _depthTexture;
    private Own<TextureView> _depthTextureView;
    private SurfaceTextureView _surfaceTexView;
    private readonly Keyboard _keyboard;
    private ulong _frameNum;
    private readonly RenderOperations _renderOperations;
    private bool _isCloseRequested;

    public event Action<HostScreen, Vector2u>? Resized;

    internal CE.ScreenId ScreenId => new CE.ScreenId(_native.Unwrap());

    public Mouse Mouse => _mouse;
    public Keyboard Keyboard => _keyboard;
    public ulong FrameNum => _frameNum;
    public RenderOperations RenderOperations => _renderOperations;

    public Texture DepthTexture => _depthTexture.AsValue();
    public TextureView DepthTextureView => _depthTextureView.AsValue();

    public SurfaceTextureView SurfaceTextureView => _surfaceTexView;

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

    public Vector2u ClientSize
    {
        get
        {
            var native = _native.Unwrap().AsRef();
            return native.ScreenGetInnerSize();
        }
        set
        {
            var native = _native.Unwrap().AsRef();
            native.ScreenSetInnerSize(value.X, value.Y);
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfNotInit();

            if(value.Length != 0) {
                var utf8 = Encoding.UTF8;

                var byteLen = utf8.GetByteCount(value);
                var array = ArrayPool<byte>.Shared.Rent(byteLen);
                try {
                    var buf = array.AsSpan(0, byteLen);
                    utf8.GetBytes(value.AsSpan(), buf);
                    EngineCore.ScreenSetTitle(_native.Unwrap(), buf);
                }
                finally {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
            _title = value;
        }
    }

    internal HostScreen(Rust.Box<CE.HostScreen> screen)
    {
        _native = screen;
        _surfaceTexView = new SurfaceTextureView(this, Environment.CurrentManagedThreadId);
        _mouse = new Mouse(this);
        _keyboard = new Keyboard(this);
        _renderOperations = new RenderOperations(this);
    }

    public void Close()
    {
        _isCloseRequested = true;
    }

    public Vector2i GetLocation(uint? monitorIndex)
    {
        var native = _native.Unwrap().AsRef();
        return native.ScreenGetLocation(monitorIndex);
    }

    public void SetLocation(uint? monitorIndex, Vector2i location)
    {
        var native = _native.Unwrap().AsRef();
        native.ScreenSetLocation(location.X, location.Y, monitorIndex);
    }

    public uint MonitorIndex => _native.Unwrap().AsRef().ScreenMonitorIndex().ToUInt32();

    public uint MonitorCount => _native.Unwrap().AsRef().ScreenAllMonitorCount().ToUInt32();

    private void UpdateDepthTexture(Vector2u size)
    {
        var depth = Texture.Create(this, new TextureDescriptor
        {
            Size = new Vector3u(size.X, size.Y, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Depth32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
        }).AsValue(out var depthOwn);
        var view = TextureView.Create(depth);

        _depthTextureView.Dispose();
        _depthTexture.Dispose();
        _depthTexture = depthOwn;
        _depthTextureView = view;
    }

    internal void OnInitialize(in CE.HostScreenInfo info)
    {
        _surfaceFormat = info.surface_format.Unwrap().MapOrThrow();
        _backend = info.backend.MapOrThrow();

        var size = ClientSize;
        UpdateDepthTexture(size);
        _initialized = true;
    }

    internal void OnCleared()
    {
        _native.Unwrap().AsRef().ScreenRequestRedraw();
    }

    internal unsafe bool OnRedrawRequested()
    {
        var depthTextureView = _depthTextureView;
        if(depthTextureView.IsNone) {
            return true;
        }

        var screenRef = _native.Unwrap().AsRef();
        Rust.Box<Wgpu.CommandEncoder> encoderNative;
        Rust.Box<Wgpu.SurfaceTexture> surfaceTexNative;
        {
            if(screenRef.ScreenBeginCommand(out encoderNative, out surfaceTexNative, out var surfaceViewNative) == false) {
                return true;
            }
            var oldSurfaceView = _surfaceTexView.Replace(surfaceViewNative);
            Debug.Assert(oldSurfaceView.IsNone);
        }
        try {
            Render(new CommandEncoder(encoderNative));
            var isCloseRequested = _isCloseRequested;
            _isCloseRequested = false;
            return !isCloseRequested;
        }
        finally {
            var surfaceViewNative = _surfaceTexView.Replace(Rust.OptionBox<Wgpu.TextureView>.None);
            screenRef.ScreenFinishCommand(encoderNative, surfaceTexNative, surfaceViewNative.Unwrap());
            _frameNum++;
        }
    }

    private void Render(CommandEncoder encoder)
    {
        using var renderPassOwn = encoder.CreateSurfaceRenderPass(SurfaceTextureView, DepthTextureView);
        var renderPass = renderPassOwn.AsValue();

        _renderOperations.ApplyAdd();
        RenderOperations.Render(renderPass);
        _renderOperations.ApplyRemove();
        _keyboard.PrepareNextFrame();
    }

    internal void OnResized(uint width, uint height)
    {
        if(width != 0 && height != 0) {
            _native.Unwrap().AsRef().ScreenResizeSurface(width, height);
            UpdateDepthTexture(new Vector2u(width, height));
        }

        Resized?.Invoke(this, new Vector2u(width, height));
    }

    internal void OnClosing(ref bool cancel)
    {
    }

    internal Rust.OptionBox<CE.HostScreen> OnClosed()
    {
        var native = InterlockedEx.Exchange(ref _native, Rust.OptionBox<CE.HostScreen>.None);
        _renderOperations.DisposeInternal();
        _depthTexture.Dispose();
        _depthTextureView.Dispose();
        _depthTexture = Own<Texture>.None;
        _depthTextureView = Own<TextureView>.None;
        Resized = null;
        //RedrawRequested = null;
        return native;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Rust.Ref<CE.HostScreen> AsRefChecked()
    {
        return _native.Unwrap().AsRef();
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

public readonly struct HostScreenConfig
{
    public required WindowStyle Style { get; init; }
    public required u32 Width { get; init; }
    public required u32 Height { get; init; }
    public required GraphicsBackend Backend { get; init; }

    internal CE.HostScreenConfig ToCoreType()
    {
        return new CE.HostScreenConfig
        {
            style = Style.MapOrThrow(),
            width = Width,
            height = Height,
            backend = Backend.MapOrThrow(),
        };
    }
}

//public readonly struct Monitor
//{
//}
