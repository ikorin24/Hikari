#nullable enable
using Elffy.NativeBind;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elffy;

public sealed class Screen
{
    private Rust.OptionBox<CE.HostScreen> _native;
    private readonly ThreadId _mainThread;
    private readonly SubscriptionBag _subscriptions;
    private readonly Camera _camera;
    private readonly Lights _lights;
    private readonly Timing _earlyUpdate;
    private readonly Timing _update;
    private readonly Timing _lateUpdate;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;
    private string _title = "";
    private Mouse _mouse;
    private Own<Texture> _depthTexture;
    private readonly Keyboard _keyboard;
    private ulong _frameNum;
    private readonly Operations _operations;
    private bool _isCloseRequested;
    private EventSource<(Screen Screen, Vector2u Size)> _resized;

    public Event<(Screen Screen, Vector2u Size)> Resized => _resized.Event;

    internal CE.ScreenId ScreenId => new CE.ScreenId(_native.Unwrap());
    internal ThreadId MainThread => _mainThread;

    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public Mouse Mouse => _mouse;
    public Keyboard Keyboard => _keyboard;
    public ulong FrameNum => _frameNum;
    public Operations Operations => _operations;

    public Timing EarlyUpdate => _earlyUpdate;
    public Timing Update => _update;
    public Timing LateUpdate => _lateUpdate;

    public Texture DepthTexture
    {
        get
        {
            ThrowIfNotInit();
            return _depthTexture.AsValue();
        }
    }

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

    public uint MonitorCount => _native.Unwrap().AsRef().MonitorCount().ToUInt32();

    public MonitorId? CurrentMonitor => _native.Unwrap().AsRef().CurrentMonitor();

    public Camera Camera => _camera;
    public Lights Lights => _lights;

    internal Screen(Rust.Box<CE.HostScreen> screen, ThreadId mainThread)
    {
        _native = screen;
        _mainThread = mainThread;
        _camera = new Camera(this);
        _lights = new Lights(this);
        _earlyUpdate = new Timing(this);
        _update = new Timing(this);
        _lateUpdate = new Timing(this);
        _mouse = new Mouse(this);
        _keyboard = new Keyboard(this);
        _operations = new Operations(this);
        _subscriptions = new SubscriptionBag();
    }

    public void Close()
    {
        _isCloseRequested = true;
    }

    public Vector2i GetLocation(MonitorId? monitorId = null)
    {
        _mainThread.ThrowIfNotMatched();
        var native = _native.Unwrap().AsRef();
        return native.ScreenGetLocation(monitorId);
    }

    public void SetLocation(Vector2i location, MonitorId? monitorId = null)
    {
        _mainThread.ThrowIfNotMatched();
        var native = _native.Unwrap().AsRef();
        native.ScreenSetLocation(location.X, location.Y, monitorId);
    }

    public unsafe MonitorId[] GetMonitors()
    {
        _mainThread.ThrowIfNotMatched();
        var native = _native.Unwrap().AsRef();
        var count = native.MonitorCount().ToUInt32();
        if(count == 0) {
            return Array.Empty<MonitorId>();
        }
        Span<CE.MonitorId> buf = stackalloc CE.MonitorId[(int)count];
        native.Monitors(buf);
        var monitors = new MonitorId[count];
        for(int i = 0; i < monitors.Length; i++) {
            monitors[i] = new MonitorId(buf[i]);
        }
        return monitors;
    }

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
        });
        _depthTexture.Dispose();
        _depthTexture = depth;
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
        if(EngineCore.ScreenBeginCommand(this, out var encoder) == false) {
            return true;
        }
        try {
            Render(in encoder);
            var isCloseRequested = _isCloseRequested;
            _isCloseRequested = false;
            return !isCloseRequested;
        }
        finally {
            EngineCore.ScreenFinishCommand(encoder);
            _frameNum++;
        }
    }

    private void Render(in CommandEncoder encoder)
    {
        var operations = _operations;

        operations.ApplyAdd();

        // early update
        _earlyUpdate.DoQueuedEvents();
        operations.EarlyUpdate();

        // update
        _update.DoQueuedEvents();
        operations.Update();

        // late update
        _lateUpdate.DoQueuedEvents();
        operations.LateUpdate();

        // render
        _camera.UpdateUniformBuffer();
        operations.Execute(in encoder);

        operations.ApplyRemove();
        _keyboard.PrepareNextFrame();
    }

    internal void OnResized(uint width, uint height)
    {
        Debug.Assert(width != 0);
        Debug.Assert(height != 0);
        var size = new Vector2u(width, height);
        _native.Unwrap().AsRef().ScreenResizeSurface(size.X, size.Y);
        UpdateDepthTexture(size);
        _camera.ChangeScreenSize(size);
        _resized.Invoke((this, size));
    }

    internal void OnClosing(ref bool cancel)
    {
    }

    internal Rust.OptionBox<CE.HostScreen> OnClosed()
    {
        var native = InterlockedEx.Exchange(ref _native, Rust.OptionBox<CE.HostScreen>.None);
        _operations.DisposeInternal();
        _camera.DisposeInternal();
        _depthTexture.Dispose();
        _depthTexture = Own<Texture>.None;
        _lights.DisposeInternal();
        _resized.Clear();
        _subscriptions.Dispose();
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

public readonly struct ScreenConfig
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

public readonly struct MonitorId : IEquatable<MonitorId>
{
    private readonly CE.MonitorId _id;
    internal CE.MonitorId Id => _id;

    internal MonitorId(CE.MonitorId id)
    {
        _id = id;
    }

    public override bool Equals(object? obj) => obj is MonitorId handle && Equals(handle);

    public bool Equals(MonitorId other) => _id.Equals(other._id);

    public override int GetHashCode() => _id.GetHashCode();

    public static bool operator ==(MonitorId left, MonitorId right) => left.Equals(right);

    public static bool operator !=(MonitorId left, MonitorId right) => !(left == right);
}
