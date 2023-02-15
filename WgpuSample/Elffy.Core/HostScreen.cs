#nullable enable
using Elffy.NativeBind;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Elffy;

internal sealed class HostScreen : IHostScreen
{
    private Rust.Box<CE.HostScreen> _native;
    private CE.HostScreenId _id;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;
    private string _title = "";
    private Own<Texture> _depthTexture;
    private Own<TextureView> _depthTextureView;

    public event Action<IHostScreen, Vector2i>? Resized;
    public event Action<HostScreenDrawState>? RedrawRequested;

    public HostScreenRef Ref => new HostScreenRef(_native);

    public nuint Id => _id.AsNumber();

    public Texture DepthTexture => _depthTexture.AsValue();
    public TextureView DepthTextureView => _depthTextureView.AsValue();

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
                    EngineCore.ScreenSetTitle(_native, buf);
                }
                finally {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
            _title = value;
        }
    }

    private HostScreen(Rust.Box<CE.HostScreen> screen, CE.HostScreenId id)
    {
        _native = screen;
        _id = id;
    }

    ~HostScreen() => Release(false);

    private static readonly Action<HostScreen> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool manualRelease)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        // TODO: Destroy HostScreen
        //native.
        _depthTexture.Dispose();
        _depthTextureView.Dispose();
        _depthTexture = Own.None<Texture>();
        _depthTextureView = Own.None<TextureView>();
        _id = CE.HostScreenId.None;
        if(manualRelease) {
            Resized = null;
            RedrawRequested = null;
        }
    }

    internal static Own<HostScreen> Create(Rust.Box<CE.HostScreen> screen, CE.HostScreenId id)
    {
        return Own.New(new HostScreen(screen, id), _release);
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
        _native.AsRef().ScreenRequestRedraw();
    }

    internal unsafe void OnRedrawRequested()
    {
        var depthTextureView = _depthTextureView;
        if(depthTextureView.IsNone) {
            return;
        }

        if(HostScreenDrawState.TryCreate(this, out var drawStateOwn) == false) {
            return;
        }
        try {
            RedrawRequested?.Invoke(drawStateOwn.AsValue());
        }
        finally {
            drawStateOwn.Dispose();
        }
    }

    internal void OnResized(uint width, uint height)
    {
        if(width != 0 && height != 0) {
            _native.AsRef().ScreenResizeSurface(width, height);
            UpdateDepthTexture(new Vector2i((i32)width, (i32)height));
        }

        Resized?.Invoke(this, checked(new Vector2i((int)width, (int)height)));
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

public interface IHostScreen
{
    event Action<HostScreenDrawState>? RedrawRequested;
    event Action<IHostScreen, Vector2i>? Resized;

    HostScreenRef Ref { get; }

    nuint Id { get; }
    Vector2i ClientSize { get; set; }
    string Title { get; set; }
    TextureFormat SurfaceFormat { get; }
    GraphicsBackend Backend { get; }
    Texture DepthTexture { get; }
    TextureView DepthTextureView { get; }
}

internal static class HostScreenExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Ref<CE.HostScreen> AsRefChecked(this IHostScreen screen)
    {
        return screen.Ref.AsRefChecked();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rust.Ref<CE.HostScreen> AsRefUnchecked(this IHostScreen screen)
    {
        return screen.Ref.AsRefUnchecked();
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
            title = Slice<u8>.Empty,
            style = Style.MapOrThrow(),
            width = Width,
            height = Height,
            backend = Backend.MapOrThrow(),
        };
    }
}


//public sealed class SurfaceTexture : INativeRef<Wgpu.Texture>
//{
//    private Box<Wgpu.Texture> _native;
//    private readonly int _thread;
//    private ulong _token = 0;

//    internal Ref<Wgpu.Texture> NativeRef
//    {
//        get
//        {
//            CheckThread();
//            if(_native.IsInvalid) {
//                ThrowInvalidTiming();
//            }
//            return _native;
//        }
//    }

//    Ref<Wgpu.Texture> INativeRef<Wgpu.Texture>.NativeRef => NativeRef;

//    internal SurfaceTexture()
//    {
//        _thread = Thread.CurrentThread.ManagedThreadId;
//    }

//    internal Box<Wgpu.Texture> Replace(Box<Wgpu.Texture> native)
//    {
//        CheckThread();
//        _token++;
//        return Box.Swap(ref _native, native);
//    }

//    private void CheckThread()
//    {
//        if(_thread != Thread.CurrentThread.ManagedThreadId) {
//            throw new InvalidOperationException();
//        }
//    }

//    [DoesNotReturn]
//    private static void ThrowInvalidTiming()
//    {
//        throw new InvalidOperationException("The surface texture is not accessible at the current time.");
//    }
//}

//internal interface INativeRef<T> where T : INativeTypeNonReprC
//{
//    Ref<T> NativeRef { get; }
//}
