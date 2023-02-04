#nullable enable
using System;

namespace Elffy.Bind;

/// <summary>
/// `coreelffy` crate in Rust
/// </summary>
internal static class CoreElffy
{
    internal sealed class HostScreen : INativeTypeNonReprC { private HostScreen() { } }

    internal enum WindowStyle
    {
        Default = 0,
        Fixed = 1,
        Fullscreen = 2,
    }

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
        public required Opt<Wgpu.TextureFormat> surface_format;
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

    internal readonly struct HostScreenId : IEquatable<HostScreenId>
    {
        private readonly nuint _id;

        public nuint AsNumber() => _id;

        public override bool Equals(object? obj) => obj is HostScreenId id && Equals(id);

        public bool Equals(HostScreenId other) => _id == other._id;

        public override int GetHashCode() => _id.GetHashCode();

        public static bool operator ==(HostScreenId left, HostScreenId right) => left.Equals(right);

        public static bool operator !=(HostScreenId left, HostScreenId right) => !(left == right);
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

    internal ref struct RenderPassDepthStencilAttachment
    {
        public required Ref<Wgpu.TextureView> view;
        public required Opt<f32> depth_clear;
        public required Opt<u32> stencil_clear;
    }

    internal struct BindGroupLayoutDescriptor
    {
        public required Slice<BindGroupLayoutEntry> entries;
    }

    internal ref struct ImageCopyTexture
    {
        public required Ref<Wgpu.Texture> texture;
        public required u32 mip_level;
        public required u32 origin_x;
        public required u32 origin_y;
        public required u32 origin_z;
        public required TextureAspect aspect;
    }

    internal struct TextureViewDescriptor
    {
        public required Opt<Wgpu.TextureFormat> format;
        public required Opt<TextureViewDimension> dimension;
        public required TextureAspect aspect;
        public required u32 base_mip_level;
        public required u32 mip_level_count;
        public required u32 base_array_layer;
        public required u32 array_layer_count;

        public static TextureViewDescriptor Default => default;
    }

    internal enum TextureAspect
    {
        All = 0,
        StencilOnly = 1,
        DepthOnly = 2,
    }

    internal struct SamplerDescriptor
    {
        public required Wgpu.AddressMode address_mode_u;
        public required Wgpu.AddressMode address_mode_v;
        public required Wgpu.AddressMode address_mode_w;
        public required Wgpu.FilterMode mag_filter;
        public required Wgpu.FilterMode min_filter;
        public required Wgpu.FilterMode mipmap_filter;
        public required f32 lod_min_clamp;
        public required f32 lod_max_clamp;
        public required Opt<Wgpu.CompareFunction> compare;
        public u8 anisotropy_clamp;
        public Opt<SamplerBorderColor> border_color;
    }

    internal enum SamplerBorderColor
    {
        TransparentBlack = 0,
        OpaqueBlack = 1,
        OpaqueWhite = 2,
        Zero = 3,
    }
}
