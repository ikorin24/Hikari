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
/// `elffycore` module in Rust
/// </summary>
internal static class Elffycore
{
    internal sealed class HostScreen : INativeTypeMarker { private HostScreen() { } }

    internal unsafe struct EngineCoreConfig
    {
        public required DispatchErrFn err_dispatcher;
        public required HostScreenInitFn on_screen_init;
        public required OnCommandBeginFn on_command_begin;
        public required HostScreenResizedFn on_resized;
    }

    internal struct HostScreenConfig
    {
        public required Slice<u8> title;
        public required WindowStyle style;
        public required u32 width;
        public required u32 height;
        public required wgpu_Backends backend;
    }
}
