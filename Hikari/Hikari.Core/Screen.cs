#nullable enable
using Hikari.NativeBind;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hikari;

public sealed class Screen
{
    private Rust.OptionBox<CH.HostScreen> _native;
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
    private readonly Mouse _mouse;
    private RenderTextureProvider? _depth;
    private readonly SurfaceTexture _surface;
    private readonly Own<Buffer> _info;
    private readonly Keyboard _keyboard;
    private ulong _frameNum;
    private readonly Operations _operations;
    private RunningState _state;
    private EventSource<ScreenClosingState> _closing;
    private EventSource<Screen> _closed;
    private EventSource<(Screen Screen, Vector2u Size)> _resized;

    internal enum RunningState
    {
        Running = 0,
        CloseRequested = 1,
        Closed = 2,
    }

    internal RunningState State
    {
        get
        {
            if(_mainThread.IsCurrentThread == false) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("this property should be only main thread.");
            }
            return _state;
        }
    }

    public Event<ScreenClosingState> Closing => _closing.Event;
    public Event<Screen> Closed => _closed.Event;
    public Event<(Screen Screen, Vector2u Size)> Resized => _resized.Event;

    internal CH.ScreenId ScreenId => new CH.ScreenId(_native.Unwrap());
    internal ThreadId MainThread => _mainThread;

    internal BufferSlice InfoBuffer => _info.AsValue().Slice();
    public SubscriptionRegister Subscriptions => _subscriptions.Register;
    public Mouse Mouse => _mouse;
    public Keyboard Keyboard => _keyboard;
    public ulong FrameNum => _frameNum;
    public Operations Operations => _operations;

    public Timing EarlyUpdate => _earlyUpdate;
    public Timing Update => _update;
    public Timing LateUpdate => _lateUpdate;

    public IRenderTextureProvider Depth
    {
        get
        {
            ThrowIfNotInit();
            return _depth;
        }
    }

    public ITexture2D Surface => _surface;

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

    internal Screen(Rust.Box<CH.HostScreen> screen, ThreadId mainThread)
    {
        _native = screen;
        _mainThread = mainThread;
        _state = RunningState.Running;
        _subscriptions = new SubscriptionBag();
        _operations = new Operations(this);
        _camera = new Camera(this);
        _lights = new Lights(this);
        _earlyUpdate = new Timing(this);
        _update = new Timing(this);
        _lateUpdate = new Timing(this);
        _mouse = new Mouse(this);
        _surface = new SurfaceTexture(this);
        _keyboard = new Keyboard(this);
        _info = Buffer.CreateInitData(this, new ScreenInfo
        {
            Size = Vector2u.Zero,
        }, BufferUsages.Uniform | BufferUsages.Storage | BufferUsages.CopyDst);
    }

    public void RequestClose()
    {
        if(_mainThread.IsCurrentThread) {
            Close(this);
        }
        else {
            _update.Post(static x =>
            {
                var self = SafeCast.NotNullAs<Screen>(x);
                Close(self);
            }, this);
        }

        static void Close(Screen self)
        {
            Debug.Assert(self._mainThread.IsCurrentThread);
            if(self._state == RunningState.Running) {
                self._state = RunningState.CloseRequested;
            }
        }
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
        Span<CH.MonitorId> buf = stackalloc CH.MonitorId[(int)count];
        native.Monitors(buf);
        var monitors = new MonitorId[count];
        for(int i = 0; i < monitors.Length; i++) {
            monitors[i] = new MonitorId(buf[i]);
        }
        return monitors;
    }

    internal void OnInitialize(in CH.HostScreenInfo info)
    {
        _surfaceFormat = info.surface_format.Unwrap().MapOrThrow();
        _backend = info.backend.MapOrThrow();

        var size = ClientSize;
        _depth = new RenderTextureProvider(this, new()
        {
            Size = size,
            MipLevelCount = 1,
            SampleCount = 1,
            Format = TextureFormat.Depth32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding | TextureUsages.CopySrc,
        });
        _initialized = true;
    }

    internal void OnCleared()
    {
        _native.Unwrap().AsRef().ScreenRequestRedraw();
    }

    internal bool OnRedrawRequested()
    {
        Debug.Assert(_mainThread.IsCurrentThread);
        if(AsRefChecked().GetSurfaceTexture().IsSome(out var surfaceTexture) == false) {
            return false;
        }

        try {
            _surface.Set(surfaceTexture);
            Render();
            Debug.Assert(_state is RunningState.Running or RunningState.CloseRequested);
            return _state switch
            {
                RunningState.Running => true,
                RunningState.CloseRequested => false,
                _ => throw new UnreachableException($"current state: {_state}"),
            };
        }
        finally {
            _surface.Remove().PresentSurfaceTexture();
            _frameNum++;
        }
    }

    private void Render()
    {
        Debug.Assert(_state is RunningState.Running or RunningState.CloseRequested);
        var operations = _operations;
        _mouse.InitFrame();

        operations.ApplyAdd();

        // frame init
        operations.FrameInit();

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
        operations.Execute();

        // frame end
        operations.FrameEnd();

        operations.ApplyRemove();
        _keyboard.PrepareNextFrame();
        _mouse.PrepareNextFrame();

        Debug.Assert(_state is RunningState.Running or RunningState.CloseRequested);
    }

    internal void OnResized(uint width, uint height)
    {
        Debug.Assert(width != 0);
        Debug.Assert(height != 0);
        var size = new Vector2u(width, height);
        _native.Unwrap().AsRef().ScreenResizeSurface(size.X, size.Y);
        _depth?.Resize(size);
        _camera.ChangeScreenSize(size);
        _info.AsValue().WriteData(0, new ScreenInfo
        {
            Size = size,
        });
        _resized.Invoke((this, size));
    }

    internal void OnClosing(ref bool cancel)
    {
        Debug.Assert(_mainThread.IsCurrentThread);
        Debug.Assert(_state is RunningState.Running or RunningState.CloseRequested);

        if(_state == RunningState.Running) {
            // This is the case that users pressed the close button of a window.
            _state = RunningState.CloseRequested;
        }

        Debug.Assert(_state == RunningState.CloseRequested);
        var arg = new ScreenClosingState(this);
        _closing.Invoke(arg);
        if(arg.IsCanceled) {
            cancel = true;
            _state = RunningState.Running;
        }
        else {
            cancel = false;
            _state = RunningState.Closed;
        }
    }

    internal Rust.OptionBox<CH.HostScreen> OnClosed()
    {
        _operations.OnClosed();
        _closed.Invoke(this);

        var native = InterlockedEx.Exchange(ref _native, Rust.OptionBox<CH.HostScreen>.None);
        _closed.Clear();
        _camera.DisposeInternal();
        _depth?.Dispose();
        _lights.DisposeInternal();
        _resized.Clear();
        _subscriptions.Dispose();
        _info.Dispose();
        return native;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Rust.Ref<CH.HostScreen> AsRefChecked()
    {
        return _native.Unwrap().AsRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MemberNotNull(nameof(_depth))]
    private void ThrowIfNotInit()
    {
        if(_initialized == false) {
            Throw();
            static void Throw() => throw new InvalidOperationException("not initialized");
        }
        Debug.Assert(_depth != null);
    }

    private struct ScreenInfo
    {
        public Vector2u Size;
    }
}

public sealed class ScreenClosingState
{
    private readonly Screen _screen;
    private bool _isCanceled;

    public Screen Screen => _screen;
    public bool IsCanceled => _isCanceled;

    internal ScreenClosingState(Screen screen)
    {
        _screen = screen;
        _isCanceled = false;
    }

    public void Cancel()
    {
        _isCanceled = true;
    }
}

public readonly struct ScreenConfig
{
    public required WindowStyle Style { get; init; }
    public required u32 Width { get; init; }
    public required u32 Height { get; init; }
    public required GraphicsBackend Backend { get; init; }
    public required SurfacePresentMode PresentMode { get; init; }

    internal CH.HostScreenConfig ToCoreType()
    {
        return new CH.HostScreenConfig
        {
            style = Style.MapOrThrow(),
            width = Width,
            height = Height,
            backend = Backend.MapOrThrow(),
            present_mode = PresentMode.MapOrThrow(),
        };
    }
}

public readonly struct MonitorId : IEquatable<MonitorId>
{
    private readonly CH.MonitorId _id;
    internal CH.MonitorId Id => _id;

    internal MonitorId(CH.MonitorId id)
    {
        _id = id;
    }

    public override bool Equals(object? obj) => obj is MonitorId handle && Equals(handle);

    public bool Equals(MonitorId other) => _id.Equals(other._id);

    public override int GetHashCode() => _id.GetHashCode();

    public static bool operator ==(MonitorId left, MonitorId right) => left.Equals(right);

    public static bool operator !=(MonitorId left, MonitorId right) => !(left == right);
}
