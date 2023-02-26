#nullable enable
using Elffy.NativeBind;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Elffy;

internal sealed class HostScreen : IHostScreen
{
    private Rust.OptionBox<CE.HostScreen> _native;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;
    private string _title = "";
    private Own<Texture> _depthTexture;
    private Own<TextureView> _depthTextureView;
    private SurfaceTextureView _surfaceTexView;

    private bool _isCloseRequested;

    public event Action<IHostScreen, Vector2i>? Resized;
    public event RedrawRequestedAction? RedrawRequested;

    public HostScreenRef Ref => new HostScreenRef(_native.Unwrap());

    internal CE.ScreenId ScreenId => new CE.ScreenId(_native.Unwrap());

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

    public Vector2i ClientSize
    {
        get
        {
            var screenRef = Ref.AsRefUnchecked();
            if(screenRef.IsInvalid) {
                return Vector2i.Zero;
            }
            var (width, height) = screenRef.ScreenGetInnerSize();
            return new Vector2i(checked((int)width), checked((int)height));
        }
        set
        {
            var screenRef = Ref.AsRefChecked();
            var width = checked((uint)value.X);
            var height = checked((uint)value.Y);
            screenRef.ScreenSetInnerSize(width, height);
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

    private HostScreen(Rust.Box<CE.HostScreen> screen)
    {
        _native = screen;
        _surfaceTexView = new SurfaceTextureView(this, Environment.CurrentManagedThreadId);
    }

    //~HostScreen() => Release(false);

    //private static readonly Action<HostScreen> _release = static self =>
    //{
    //    self.Release(true);
    //    GC.SuppressFinalize(self);
    //};

    //private void Release(bool manualRelease)
    //{
    //    var native = InterlockedEx.Exchange(ref _native, Rust.OptionBox<CE.HostScreen>.None);
    //    if(native.IsNone) {
    //        return;
    //    }
    //    // TODO: Destroy HostScreen
    //    //native.
    //    _depthTexture.Dispose();
    //    _depthTextureView.Dispose();
    //    _depthTexture = Own.None<Texture>();
    //    _depthTextureView = Own.None<TextureView>();
    //    if(manualRelease) {
    //        Resized = null;
    //        RedrawRequested = null;
    //    }
    //}

    internal static HostScreen Create(Rust.Box<CE.HostScreen> screen)
    {
        return new HostScreen(screen);
    }

    private void UpdateDepthTexture(Vector2i size)
    {
        var depth = Texture.Create(this, new TextureDescriptor
        {
            Size = new Vector3i(size.X, size.Y, 1),
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

    public void RequestClose()
    {
        _isCloseRequested = true;
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
            RedrawRequested?.Invoke(this, new CommandEncoder(encoderNative));

            var isCloseRequested = _isCloseRequested;
            _isCloseRequested = false;
            return !isCloseRequested;
        }
        finally {
            var surfaceViewNative = _surfaceTexView.Replace(Rust.OptionBox<Wgpu.TextureView>.None);
            screenRef.ScreenFinishCommand(encoderNative, surfaceTexNative, surfaceViewNative.Unwrap());
        }
    }

    internal void OnResized(uint width, uint height)
    {
        if(width != 0 && height != 0) {
            _native.Unwrap().AsRef().ScreenResizeSurface(width, height);
            UpdateDepthTexture(new Vector2i((i32)width, (i32)height));
        }

        Resized?.Invoke(this, checked(new Vector2i((int)width, (int)height)));
    }

    internal void OnKeyboardInput(Winit.VirtualKeyCode key, bool pressed)
    {
        // TODO: The following is sample code. Remove it.
        if(key == Winit.VirtualKeyCode.Escape && pressed) {
            RequestClose();
        }
        if(key == Winit.VirtualKeyCode.A && pressed) {
            EngineCore.CreateScreen();
        }
        Debug.WriteLine($"{key}: {pressed}");
    }

    internal void OnCharReceived(Rune input)
    {
        Debug.WriteLine(input);
    }

    internal void OnClosing(ref bool cancel)
    {
        Debug.WriteLine("closing");
    }

    internal Rust.OptionBox<CE.HostScreen> OnClosed()
    {
        var native = InterlockedEx.Exchange(ref _native, Rust.OptionBox<CE.HostScreen>.None);
        _depthTexture.Dispose();
        _depthTextureView.Dispose();
        _depthTexture = Own.None<Texture>();
        _depthTextureView = Own.None<TextureView>();
        Resized = null;
        RedrawRequested = null;
        return native;
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

public delegate void RedrawRequestedAction(IHostScreen screen, CommandEncoder encoder);

public readonly ref struct HostScreenRef
{
    private readonly Rust.Ref<CE.HostScreen> _ref;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public HostScreenRef() => throw new NotSupportedException("Don't use default constructor.");

    internal HostScreenRef(Rust.Ref<CE.HostScreen> screen) => _ref = screen;

    internal Rust.Ref<CE.HostScreen> AsRefChecked()
    {
        _ref.ThrowIfInvalid();
        return _ref;
    }

    internal Rust.Ref<CE.HostScreen> AsRefUnchecked()
    {
        return _ref;
    }
}

public readonly ref struct HostScreenConfig
{
    public required WindowStyle Style { get; init; }
    public required u32 Width { get; init; }
    public required u32 Height { get; init; }
    public required GraphicsBackend Backend { get; init; }

    internal CE.HostScreenConfig ToCoreType()
    {
        return new CE.HostScreenConfig
        {
            title = CE.Slice<u8>.Empty,
            style = Style.MapOrThrow(),
            width = Width,
            height = Height,
            backend = Backend.MapOrThrow(),
        };
    }
}
