#nullable enable
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using i32 = System.Int32;
using u64 = System.UInt64;
using f32 = System.Single;
using f64 = System.Double;

namespace Elffy.Bind;

/// <summary>
/// `elffycore` crate in Rust
/// </summary>
internal static class Elffycore
{
    internal sealed class HostScreen : INativeTypeNonReprC { private HostScreen() { } }

    internal unsafe struct EngineCoreConfig
    {
        public required DispatchErrFn err_dispatcher;
        public required HostScreenInitFn on_screen_init;
        public required ClearedEventFn event_cleared;
        public required RedrawRequestedEventFn event_redraw_requested;
        public required ResizedEventFn event_resized;
    }

    internal struct HostScreenConfig
    {
        public required Slice<u8> title;
        public required WindowStyle style;
        public required u32 width;
        public required u32 height;
        public required Wgpu.Backends backend;
    }

    internal readonly record struct ErrMessageId(nuint Value);

    internal struct HostScreenInfo
    {
        public required Wgpu.Backend backend;
        public required Opt<TextureFormat> surface_format;
    }

    internal unsafe readonly struct HostScreenInitFn
    {
        private readonly delegate* unmanaged[Cdecl]<Box<HostScreen>, HostScreenInfo*, HostScreenId, void> _func;

        public HostScreenInitFn(delegate* unmanaged[Cdecl]<void*, HostScreenInfo*, HostScreenId, void> f)
        {
            _func = (delegate* unmanaged[Cdecl]<Box<HostScreen>, HostScreenInfo*, HostScreenId, void>)f;
        }
    }

    internal unsafe readonly struct DispatchErrFn
    {
        private readonly delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> _func;

        public DispatchErrFn(delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> f) => _func = f;
    }

    internal unsafe readonly struct ClearedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<HostScreenId, void> _func;

        public ClearedEventFn(delegate* unmanaged[Cdecl]<HostScreenId, void> f) => _func = f;
    }

    internal unsafe readonly struct RedrawRequestedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<HostScreenId, void> _func;

        public RedrawRequestedEventFn(delegate* unmanaged[Cdecl]<HostScreenId, void> f) => _func = f;
    }

    internal unsafe readonly struct ResizedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<HostScreenId, u32, u32, void> _func;

        public ResizedEventFn(delegate* unmanaged[Cdecl]<HostScreenId, u32, u32, void> f) => _func = f;
    }

    internal readonly struct HostScreenId
    {
        private readonly nuint screen;
        private readonly nuint window;

        public nuint Screen => screen;
        public nuint Window => window;
    }

    internal readonly struct BeginCommandData
    {
        public readonly bool success;
        public readonly OptionBox<Wgpu.CommandEncoder> command_encoder;
        public readonly OptionBox<Wgpu.SurfaceTexture> surface_texture;
        public readonly OptionBox<Wgpu.TextureView> surface_texture_view;
    }

    internal struct SizeU32
    {
        public u32 width;
        public u32 height;
    }
}
