#nullable enable
using Elffy.Bind;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elffy;

internal sealed class HostScreen : IHostScreen
{
    private Box<CE.HostScreen> _screen;
    private CE.HostScreenId _id;
    private TextureFormat _surfaceFormat;
    private GraphicsBackend _backend;
    private bool _initialized;
    private string _title = "";

    public event Action<IHostScreen, uint, uint>? Resized;
    public event Action<IHostScreen>? RedrawRequested;

    public HostScreenRef Ref => new HostScreenRef(_screen);

    public nuint Id => _id.AsNumber();

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

    public Vector2i Size
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
                    EngineCore.ScreenSetTitle(_screen, buf);
                }
                finally {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
            _title = value;
        }
    }

    internal HostScreen(Box<CE.HostScreen> screen, CE.HostScreenId id)
    {
        _screen = screen;
        _id = id;
    }

    internal void OnInitialize(in CE.HostScreenInfo info)
    {
        info.surface_format
            .Unwrap()
            .TryMapTo(out TextureFormat surfaceFormat)
            .WithDebugAssertTrue();
        _surfaceFormat = surfaceFormat;
        info.backend
            .TryMapTo(out GraphicsBackend backend)
            .WithDebugAssertTrue();
        _backend = backend;
        _initialized = true;
    }

    internal void OnCleared()
    {
        _screen.AsRef().ScreenRequestRedraw();
    }

    internal void OnRedrawRequested()
    {
        RedrawRequested?.Invoke(this);
    }

    internal void OnResized(uint width, uint height)
    {
        Resized?.Invoke(this, width, height);
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
    private readonly Ref<CE.HostScreen> _ref;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public HostScreenRef() => throw new NotSupportedException("Don't use default constructor.");

    internal HostScreenRef(Ref<CE.HostScreen> screen) => _ref = screen;

    internal Ref<CE.HostScreen> AsRefChecked()
    {
        _ref.ThrowIfInvalid();
        return _ref;
    }

    internal Ref<CE.HostScreen> AsRefUnchecked()
    {
        return _ref;
    }
}

public interface IHostScreen
{
    event Action<IHostScreen>? RedrawRequested;
    event Action<IHostScreen, uint, uint>? Resized;

    HostScreenRef Ref { get; }

    nuint Id { get; }
    Vector2i Size { get; set; }
    string Title { get; set; }
    TextureFormat SurfaceFormat { get; }
    GraphicsBackend Backend { get; }
}

internal static class HostScreenExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ref<CE.HostScreen> AsRefChecked(this IHostScreen screen)
    {
        return screen.Ref.AsRefChecked();
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
        Style.TryMapTo(out CE.WindowStyle style).WithDebugAssertTrue();
        Backend.TryMapTo(out Wgpu.Backends backend).WithDebugAssertTrue();
        return new CE.HostScreenConfig
        {
            title = Slice<u8>.Empty,
            style = style,
            width = Width,
            height = Height,
            backend = backend,
        };
    }
}
