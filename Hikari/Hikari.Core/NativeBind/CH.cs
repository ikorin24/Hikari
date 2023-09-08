#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hikari.NativeBind;

#pragma warning disable IDE1006 // naming rule
#pragma warning disable 0649    // field never assigned
#pragma warning disable IDE0052 // remove unread non public member

/// <summary>
/// `corehikari` crate in Rust
/// </summary>
internal static class CH
{
    internal sealed class HostScreen : INativeTypeNonReprC { private HostScreen() { } }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Opt<T> where T : unmanaged
    {
        private readonly bool _exists;
        private readonly T _value;

        public Opt()
        {
            this = default;
        }

        public Opt(T value)
        {
            _exists = true;
            _value = value;
        }

        public static Opt<T> None => default;

        public static Opt<T> Some(T value) => new(value);

        public bool TryGetValue(out T value)
        {
            value = _value;
            return _exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Unwrap()
        {
            if(_exists == false) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("cannot get value");
            }
            return _value;
        }
    }

    internal readonly struct TextureFormatInfo
    {
        public readonly Wgpu.Features required_features;
        public readonly TextureSampleType sample_type;
        /// <summary>Dimension of a "block" of texels. This is always (1, 1) on uncompressed textures.</summary>
        public readonly TupleU8U8 block_dimensions;
        /// <summary>
        /// Size in bytes of a "block" of texels. This is the size per pixel on uncompressed textures.
        /// (For example, 4 if format is 'Rgba8Unorm')
        /// </summary>
        public readonly u8 block_size;
        public readonly u8 components;
        public readonly bool srgb;
        public readonly TextureFormatFeatures guaranteed_format_features;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal record struct TupleU8U8(u8 Value1, u8 Value2);

    internal struct TextureFormatFeatures
    {
        public Wgpu.TextureUsages allowed_usages;
        public Wgpu.TextureFormatFeatureFlags flags;
    }

    internal enum TextureFormat : u32
    {
        [EnumMapTo(Hikari.TextureFormat.R8Unorm)] R8Unorm = 0,
        [EnumMapTo(Hikari.TextureFormat.R8Snorm)] R8Snorm = 1,
        [EnumMapTo(Hikari.TextureFormat.R8Uint)] R8Uint = 2,
        [EnumMapTo(Hikari.TextureFormat.R8Sint)] R8Sint = 3,
        [EnumMapTo(Hikari.TextureFormat.R16Uint)] R16Uint = 4,
        [EnumMapTo(Hikari.TextureFormat.R16Sint)] R16Sint = 5,
        [EnumMapTo(Hikari.TextureFormat.R16Unorm)] R16Unorm = 6,
        [EnumMapTo(Hikari.TextureFormat.R16Snorm)] R16Snorm = 7,
        [EnumMapTo(Hikari.TextureFormat.R16Float)] R16Float = 8,
        [EnumMapTo(Hikari.TextureFormat.Rg8Unorm)] Rg8Unorm = 9,
        [EnumMapTo(Hikari.TextureFormat.Rg8Snorm)] Rg8Snorm = 10,
        [EnumMapTo(Hikari.TextureFormat.Rg8Uint)] Rg8Uint = 11,
        [EnumMapTo(Hikari.TextureFormat.Rg8Sint)] Rg8Sint = 12,
        [EnumMapTo(Hikari.TextureFormat.R32Uint)] R32Uint = 13,
        [EnumMapTo(Hikari.TextureFormat.R32Sint)] R32Sint = 14,
        [EnumMapTo(Hikari.TextureFormat.R32Float)] R32Float = 15,
        [EnumMapTo(Hikari.TextureFormat.Rg16Uint)] Rg16Uint = 16,
        [EnumMapTo(Hikari.TextureFormat.Rg16Sint)] Rg16Sint = 17,
        [EnumMapTo(Hikari.TextureFormat.Rg16Unorm)] Rg16Unorm = 18,
        [EnumMapTo(Hikari.TextureFormat.Rg16Snorm)] Rg16Snorm = 19,
        [EnumMapTo(Hikari.TextureFormat.Rg16Float)] Rg16Float = 20,
        [EnumMapTo(Hikari.TextureFormat.Rgba8Unorm)] Rgba8Unorm = 21,
        [EnumMapTo(Hikari.TextureFormat.Rgba8UnormSrgb)] Rgba8UnormSrgb = 22,
        [EnumMapTo(Hikari.TextureFormat.Rgba8Snorm)] Rgba8Snorm = 23,
        [EnumMapTo(Hikari.TextureFormat.Rgba8Uint)] Rgba8Uint = 24,
        [EnumMapTo(Hikari.TextureFormat.Rgba8Sint)] Rgba8Sint = 25,
        [EnumMapTo(Hikari.TextureFormat.Bgra8Unorm)] Bgra8Unorm = 26,
        [EnumMapTo(Hikari.TextureFormat.Bgra8UnormSrgb)] Bgra8UnormSrgb = 27,
        [EnumMapTo(Hikari.TextureFormat.Rgb10a2Unorm)] Rgb10a2Unorm = 28,
        [EnumMapTo(Hikari.TextureFormat.Rg11b10Float)] Rg11b10Float = 29,
        [EnumMapTo(Hikari.TextureFormat.Rg32Uint)] Rg32Uint = 30,
        [EnumMapTo(Hikari.TextureFormat.Rg32Sint)] Rg32Sint = 31,
        [EnumMapTo(Hikari.TextureFormat.Rg32Float)] Rg32Float = 32,
        [EnumMapTo(Hikari.TextureFormat.Rgba16Uint)] Rgba16Uint = 33,
        [EnumMapTo(Hikari.TextureFormat.Rgba16Sint)] Rgba16Sint = 34,
        [EnumMapTo(Hikari.TextureFormat.Rgba16Unorm)] Rgba16Unorm = 35,
        [EnumMapTo(Hikari.TextureFormat.Rgba16Snorm)] Rgba16Snorm = 36,
        [EnumMapTo(Hikari.TextureFormat.Rgba16Float)] Rgba16Float = 37,
        [EnumMapTo(Hikari.TextureFormat.Rgba32Uint)] Rgba32Uint = 38,
        [EnumMapTo(Hikari.TextureFormat.Rgba32Sint)] Rgba32Sint = 39,
        [EnumMapTo(Hikari.TextureFormat.Rgba32Float)] Rgba32Float = 40,
        [EnumMapTo(Hikari.TextureFormat.Depth32Float)] Depth32Float = 41,
        [EnumMapTo(Hikari.TextureFormat.Depth32FloatStencil8)] Depth32FloatStencil8 = 42,
        [EnumMapTo(Hikari.TextureFormat.Depth24Plus)] Depth24Plus = 43,
        [EnumMapTo(Hikari.TextureFormat.Depth24PlusStencil8)] Depth24PlusStencil8 = 44,
        [EnumMapTo(Hikari.TextureFormat.Rgb9e5Ufloat)] Rgb9e5Ufloat = 45,
        [EnumMapTo(Hikari.TextureFormat.Bc1RgbaUnorm)] Bc1RgbaUnorm = 46,
        [EnumMapTo(Hikari.TextureFormat.Bc1RgbaUnormSrgb)] Bc1RgbaUnormSrgb = 47,
        [EnumMapTo(Hikari.TextureFormat.Bc2RgbaUnorm)] Bc2RgbaUnorm = 48,
        [EnumMapTo(Hikari.TextureFormat.Bc2RgbaUnormSrgb)] Bc2RgbaUnormSrgb = 49,
        [EnumMapTo(Hikari.TextureFormat.Bc3RgbaUnorm)] Bc3RgbaUnorm = 50,
        [EnumMapTo(Hikari.TextureFormat.Bc3RgbaUnormSrgb)] Bc3RgbaUnormSrgb = 51,
        [EnumMapTo(Hikari.TextureFormat.Bc4RUnorm)] Bc4RUnorm = 52,
        [EnumMapTo(Hikari.TextureFormat.Bc4RSnorm)] Bc4RSnorm = 53,
        [EnumMapTo(Hikari.TextureFormat.Bc5RgUnorm)] Bc5RgUnorm = 54,
        [EnumMapTo(Hikari.TextureFormat.Bc5RgSnorm)] Bc5RgSnorm = 55,
        [EnumMapTo(Hikari.TextureFormat.Bc6hRgbUfloat)] Bc6hRgbUfloat = 56,
        [EnumMapTo(Hikari.TextureFormat.Bc6hRgbSfloat)] Bc6hRgbSfloat = 57,
        [EnumMapTo(Hikari.TextureFormat.Bc7RgbaUnorm)] Bc7RgbaUnorm = 58,
        [EnumMapTo(Hikari.TextureFormat.Bc7RgbaUnormSrgb)] Bc7RgbaUnormSrgb = 59,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgb8Unorm)] Etc2Rgb8Unorm = 60,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgb8UnormSrgb)] Etc2Rgb8UnormSrgb = 61,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgb8A1Unorm)] Etc2Rgb8A1Unorm = 62,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgb8A1UnormSrgb)] Etc2Rgb8A1UnormSrgb = 63,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgba8Unorm)] Etc2Rgba8Unorm = 64,
        [EnumMapTo(Hikari.TextureFormat.Etc2Rgba8UnormSrgb)] Etc2Rgba8UnormSrgb = 65,
        [EnumMapTo(Hikari.TextureFormat.EacR11Unorm)] EacR11Unorm = 66,
        [EnumMapTo(Hikari.TextureFormat.EacR11Snorm)] EacR11Snorm = 67,
        [EnumMapTo(Hikari.TextureFormat.EacRg11Unorm)] EacRg11Unorm = 68,
        [EnumMapTo(Hikari.TextureFormat.EacRg11Snorm)] EacRg11Snorm = 69,
    }

    internal readonly struct RangeU32
    {
        private readonly u32 _start;
        private readonly u32 _end_excluded;

        public required u32 start
        {
            get => _start;
            init => _start = value;
        }

        public required u32 end_excluded
        {
            get => _end_excluded;
            init => _end_excluded = value;
        }

        [SetsRequiredMembers]
        public RangeU32(u32 start, u32 end_excluded)
        {
            _start = start;
            _end_excluded = end_excluded;
        }

        public static implicit operator RangeU32(Range range)
        {
            return new()
            {
                start = (u32)range.Start.Value,
                end_excluded = (u32)range.End.Value,
            };
        }
    }

    internal readonly struct RangeBoundsU64
    {
        private readonly u64 _start;
        private readonly u64 _end_excluded;
        private readonly bool _has_start;
        private readonly bool _has_end_excluded;

        public required u64 start { get => _start; init => _start = value; }
        public required u64 end_excluded { get => _end_excluded; init => _end_excluded = value; }
        public required bool has_start { get => _has_start; init => _has_start = value; }
        public required bool has_end_excluded { get => _has_end_excluded; init => _has_end_excluded = value; }

        public static RangeBoundsU64 RangeFull => default;

        public static RangeBoundsU64 StartAt(u64 start) => new()
        {
            start = start,
            has_start = true,
            end_excluded = default,
            has_end_excluded = false,
        };

        public static RangeBoundsU64 EndAt(u64 endExcluded) => new()
        {
            start = default,
            has_start = false,
            end_excluded = endExcluded,
            has_end_excluded = true,
        };

        public static RangeBoundsU64 StartEnd(u64 start, u64 endExcluded) => new()
        {
            start = start,
            has_start = true,
            end_excluded = endExcluded,
            has_end_excluded = true,
        };

        public static RangeBoundsU64 StartLength(u64 start, u64 length) => new()
        {
            start = start,
            has_start = true,
            end_excluded = start + length,
            has_end_excluded = true,
        };
    }

    internal struct Slice<T> where T : unmanaged
    {
        public unsafe required T* data; // allow null
        public required usize len;

        public static Slice<T> Empty => default;

        [SetsRequiredMembers]
        public unsafe Slice(T* data, usize len)
        {
            this.data = data;
            this.len = len;
        }

        [SetsRequiredMembers]
        public unsafe Slice(T* data, int len)
        {
            this.data = data;
            this.len = checked((usize)len);
        }
    }

    internal readonly struct ImeInputData
    {
        public readonly Tag tag;
        public readonly Slice<u8> text;
        public readonly Opt<RangeValue> range;

        internal enum Tag : u32
        {
            Enabled = 0,
            Preedit = 1,
            Commit = 2,
            Disabled = 3,
        }

        internal record struct RangeValue(usize Start, usize End);
    }

    internal enum PresentMode : u32
    {
        AutoVsync = 0,
        AutoNoVsync = 1,
        Fifo = 2,
        FifoRelaxed = 3,
        Immediate = 4,
        Mailbox = 5,
    }

    internal enum WindowStyle
    {
        Default = 0,
        Fixed = 1,
        Fullscreen = 2,
    }

    internal unsafe struct EngineCoreConfig
    {
        public required HostScreenInitFn on_screen_init;
        public required EngineUnhandledErrorFn on_unhandled_error;
        public required ClearedEventFn event_cleared;
        public required RedrawRequestedEventFn event_redraw_requested;
        public required ResizedEventFn event_resized;
        public required KeyboardEventFn event_keyboard;
        public required CharReceivedEventFn event_char_received;
        public required MouseButtonEventFn event_mouse_button;
        public required ImeInputEventFn event_ime;
        public required MouseWheelEventFn event_wheel;
        public required CursorMovedEventFn event_cursor_moved;
        public required CursorEnteredLeftEventFn event_cursor_entered_left;
        public required ClosingEventFn event_closing;
        public required ClosedEventFn event_closed;
    }

    internal struct HostScreenConfig
    {
        public required WindowStyle style;
        public required u32 width;
        public required u32 height;
        public required Wgpu.Backends backend;
        public required CH.PresentMode present_mode;
    }

    internal struct HostScreenInfo
    {
        public required Wgpu.Backend backend;
        public required Opt<TextureFormat> surface_format;
    }

    internal readonly struct MonitorId : IEquatable<MonitorId>
    {
        private readonly usize _v;

        public override bool Equals(object? obj) => obj is MonitorId id && Equals(id);

        public bool Equals(MonitorId other) => _v.Equals(other._v);

        public override int GetHashCode() => _v.GetHashCode();
    }

    internal readonly struct MouseButton
    {
        public readonly u16 number;
        public readonly bool is_named_buton;
    }

    internal unsafe readonly struct HostScreenInitFn
    {
        private readonly delegate* unmanaged[Cdecl]<Rust.Box<HostScreen>, HostScreenInfo*, ScreenId> _func;

        public HostScreenInitFn(delegate* unmanaged[Cdecl]<void*, HostScreenInfo*, ScreenId> f)
        {
            _func = (delegate* unmanaged[Cdecl]<Rust.Box<HostScreen>, HostScreenInfo*, ScreenId>)f;
        }
    }

    internal unsafe readonly struct EngineUnhandledErrorFn
    {
        private readonly delegate* unmanaged[Cdecl]<u8*, nuint, void> _func;

        public EngineUnhandledErrorFn(delegate* unmanaged[Cdecl]<u8*, nuint, void> f) => _func = f;
    }

    internal unsafe readonly struct ClearedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, void> _func;

        public ClearedEventFn(delegate* unmanaged[Cdecl]<ScreenId, void> f) => _func = f;
    }

    internal unsafe readonly struct RedrawRequestedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, bool> _func;

        public RedrawRequestedEventFn(delegate* unmanaged[Cdecl]<ScreenId, bool> f) => _func = f;
    }

    internal unsafe readonly struct ResizedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, u32, u32, void> _func;

        public ResizedEventFn(delegate* unmanaged[Cdecl]<ScreenId, u32, u32, void> f) => _func = f;
    }

    internal unsafe readonly struct KeyboardEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, Winit.VirtualKeyCode, bool, void> _func;
        public KeyboardEventFn(delegate* unmanaged[Cdecl]<ScreenId, Winit.VirtualKeyCode, bool, void> f) => _func = f;
    }

    internal unsafe readonly struct CharReceivedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, Rune, void> _func;
        public CharReceivedEventFn(delegate* unmanaged[Cdecl]<ScreenId, Rune, void> f) => _func = f;
    }

    internal unsafe readonly struct MouseButtonEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, MouseButton, bool, void> _func;
        public MouseButtonEventFn(delegate* unmanaged[Cdecl]<ScreenId, MouseButton, bool, void> f) => _func = f;
    }

    internal unsafe readonly struct ImeInputEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, ImeInputData*, void> _func;
        public ImeInputEventFn(delegate* unmanaged[Cdecl]<ScreenId, ImeInputData*, void> f) => _func = f;
    }

    internal unsafe readonly struct MouseWheelEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, f32, f32, void> _func;
        public MouseWheelEventFn(delegate* unmanaged[Cdecl]<ScreenId, f32, f32, void> f) => _func = f;
    }

    internal unsafe readonly struct CursorMovedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, f32, f32, void> _func;
        public CursorMovedEventFn(delegate* unmanaged[Cdecl]<ScreenId, f32, f32, void> f) => _func = f;
    }

    internal unsafe readonly struct CursorEnteredLeftEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, bool, void> _func;
        public CursorEnteredLeftEventFn(delegate* unmanaged[Cdecl]<ScreenId, bool, void> f) => _func = f;
    }

    internal unsafe readonly struct ClosingEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, bool*, void> _func;
        public ClosingEventFn(delegate* unmanaged[Cdecl]<ScreenId, bool*, void> f) => _func = f;
    }

    internal unsafe readonly struct ClosedEventFn
    {
        private readonly delegate* unmanaged[Cdecl]<ScreenId, Rust.OptionBox<HostScreen>> _func;
        public ClosedEventFn(delegate* unmanaged[Cdecl]<ScreenId, NativePointer> f)
        {
            _func = (delegate* unmanaged[Cdecl]<ScreenId, Rust.OptionBox<HostScreen>>)f;
        }
    }

    internal readonly struct ScreenId : IEquatable<ScreenId>
    {
        private readonly NativePointer _value;

        internal ScreenId(Rust.Box<HostScreen> screen)
        {
            _value = screen.AsPtrChecked();
        }

        public override string ToString() => _value.ToString();

        public override bool Equals(object? obj) => obj is ScreenId id && Equals(id);

        public bool Equals(ScreenId other) => _value.Equals(other._value);

        public override int GetHashCode() => HashCode.Combine(_value);

        public static bool operator ==(ScreenId left, ScreenId right) => left.Equals(right);

        public static bool operator !=(ScreenId left, ScreenId right) => !(left == right);
    }

    internal struct SizeU32
    {
        public u32 width;
        public u32 height;
    }

    internal readonly struct RenderPassDepthStencilAttachment
    {
        private readonly NativePointer _view;
        private readonly Opt<RenderPassDepthBufferInit> _depth;
        private readonly Opt<RenderPassStencilBufferInit> _stencil;

        public unsafe required Rust.Ref<Wgpu.TextureView> view
        {
            get
            {
                var view = _view;
                return *(Rust.Ref<Wgpu.TextureView>*)(&view);
            }
            init => _view = value.AsPtr();
        }
        public required Opt<RenderPassDepthBufferInit> depth { get => _depth; init => _depth = value; }
        public required Opt<RenderPassStencilBufferInit> stencil { get => _stencil; init => _stencil = value; }
    }

    internal enum RenderPassBufferInitMode : u32
    {
        Clear = 0,
        Load = 1,
    }

    internal struct RenderPassColorBufferInit
    {
        public RenderPassBufferInitMode mode;
        public Wgpu.Color value;
    }

    internal struct RenderPassDepthBufferInit
    {
        public RenderPassBufferInitMode mode;
        public f32 value;
    }

    internal struct RenderPassStencilBufferInit
    {
        public RenderPassBufferInitMode mode;
        public u32 value;
    }

    internal struct BindGroupLayoutDescriptor
    {
        public required Slice<BindGroupLayoutEntry> entries;
    }

    internal ref struct ImageCopyTexture
    {
        public required Rust.Ref<Wgpu.Texture> texture;
        public required u32 mip_level;
        public required u32 origin_x;
        public required u32 origin_y;
        public required u32 origin_z;
        public required TextureAspect aspect;
    }

    internal struct TextureViewDescriptor
    {
        public required Opt<TextureFormat> format;
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
        public u8 anisotropy_clamp;     // Option<NonZeroU8> in Rust
        public Opt<SamplerBorderColor> border_color;
    }

    internal enum SamplerBorderColor
    {
        TransparentBlack = 0,
        OpaqueBlack = 1,
        OpaqueWhite = 2,
        Zero = 3,
    }

    internal readonly struct RenderPassDescriptor
    {
        private readonly Slice<Opt<RenderPassColorAttachment>> _color_attachments;
        private readonly Opt<RenderPassDepthStencilAttachment> _depth_stencil_attachment;

        public required Slice<Opt<RenderPassColorAttachment>> color_attachments
        {
            get => _color_attachments; init => _color_attachments = value;
        }
        public required Opt<RenderPassDepthStencilAttachment> depth_stencil_attachment
        {
            get => _depth_stencil_attachment; init => _depth_stencil_attachment = value;
        }
    }

    internal readonly struct RenderPassColorAttachment
    {
        private readonly NativePointer _view;   // Rust.Ref<Wgpu.TextureView>
        private readonly RenderPassColorBufferInit _init;

        public unsafe required Rust.Ref<Wgpu.TextureView> view
        {
            get
            {
                var view = _view;
                return *(Rust.Ref<Wgpu.TextureView>*)(&view);
            }
            init => _view = value.AsPtr();
        }

        public required RenderPassColorBufferInit init { get => _init; init => _init = value; }

        [SetsRequiredMembers]
        public RenderPassColorAttachment(NativePointer view, RenderPassColorBufferInit init)
        {
            _view = view;
            _init = init;
        }
    }

    internal readonly struct BindGroupDescriptor
    {
        private readonly NativePointer _layout;
        private readonly Slice<BindGroupEntry> _entries;

        public unsafe required Rust.Ref<Wgpu.BindGroupLayout> layout
        {
            get
            {
                var layout = _layout;
                return *(Rust.Ref<Wgpu.BindGroupLayout>*)(&layout);
            }
            init => _layout = value.AsPtr();
        }
        public required Slice<BindGroupEntry> entries
        {
            get => _entries;
            init => _entries = value;
        }
    }

    internal readonly struct BindGroupEntry
    {
        private readonly u32 _binding;
        private readonly BindingResource _resource;

        public required u32 binding { get => _binding; init => _binding = value; }
        public required BindingResource resource { get => _resource; init => _resource = value; }
    }

    internal unsafe readonly struct BindingResource
    {
        private readonly BindingResourceTag _tag;
        private readonly void* _payload;

        private BindingResource(BindingResourceTag tag, void* payload)
        {
            _tag = tag;
            _payload = payload;
        }

        public unsafe static BindingResource Buffer(BufferBinding* payload) => new(BindingResourceTag.Buffer, payload);

        public unsafe static BindingResource TextureView(Rust.Ref<Wgpu.TextureView> textureView) => new(BindingResourceTag.TextureView, textureView.AsPtr());

        public unsafe static BindingResource Sampler(Rust.Ref<Wgpu.Sampler> sampler) => new(BindingResourceTag.Sampler, sampler.AsPtr());

        private enum BindingResourceTag : u32
        {
            Buffer = 0,
            BufferArray = 1,
            Sampler = 2,
            SamplerArray = 3,
            TextureView = 4,
            TextureViewArray = 5,
        }
    }

    internal readonly struct BufferBinding
    {
        private readonly NativePointer _buffer;
        private readonly u64 _offset;
        private readonly u64 _size;

        public unsafe required Rust.Ref<Wgpu.Buffer> buffer
        {
            get
            {
                var buffer = _buffer;
                return *(Rust.Ref<Wgpu.Buffer>*)(&buffer);
            }
            init => _buffer = value.AsPtr();
        }
        public required u64 offset
        {
            get => _offset;
            init => _offset = value;
        }
        public required u64 size
        {
            get => _size;
            init => _size = value;
        }
    }

    internal readonly struct PipelineLayoutDescriptor
    {
        private readonly Slice<NativePointer> _bind_group_layouts;

        public unsafe PipelineLayoutDescriptor(Rust.Ref<Wgpu.BindGroupLayout>* bind_group_layouts, nuint count)
        {
            _bind_group_layouts = new Slice<NativePointer>((NativePointer*)bind_group_layouts, count);
        }
    }

    internal readonly struct RenderPipelineDescriptor
    {
        private readonly NativePointer _layout; // Ref<Wgpu.PipelineLayout>
        private readonly VertexState _vertex;
        private readonly Opt<FragmentState> _fragment;
        private readonly PrimitiveState _primitive;
        private readonly Opt<DepthStencilState> _depth_stencil;
        private readonly Wgpu.MultisampleState _multisample;
        private readonly Rust.OptionNonZeroU32 _multiview;

        public unsafe required Rust.Ref<Wgpu.PipelineLayout> layout
        {
            get
            {
                var layout = _layout;
                return *(Rust.Ref<Wgpu.PipelineLayout>*)(&layout);
            }
            init => _layout = value.AsPtr();
        }
        public required VertexState vertex { get => _vertex; init => _vertex = value; }
        public required Opt<FragmentState> fragment { get => _fragment; init => _fragment = value; }
        public required PrimitiveState primitive { get => _primitive; init => _primitive = value; }
        public required Opt<DepthStencilState> depth_stencil { get => _depth_stencil; init => _depth_stencil = value; }
        public required Wgpu.MultisampleState multisample { get => _multisample; init => _multisample = value; }
        public required Rust.OptionNonZeroU32 multiview { get => _multiview; init => _multiview = value; }
    }

    internal readonly struct ComputePipelineDescriptor
    {
        private readonly NativePointer _layout; //: &'a wgpu::PipelineLayout,
        private readonly NativePointer _module; //: &'a wgpu::ShaderModule,
        private readonly Slice<u8> _entry_point;

        public unsafe required Rust.Ref<Wgpu.PipelineLayout> layout
        {
            get
            {
                var layout = _layout;
                return *(Rust.Ref<Wgpu.PipelineLayout>*)(&layout);
            }
            init => _layout = value.AsPtr();
        }
        public unsafe required Rust.Ref<Wgpu.ShaderModule> module
        {
            get
            {
                var module = _module;
                return *(Rust.Ref<Wgpu.ShaderModule>*)(&module);
            }
            init => _module = value.AsPtr();
        }
        public required Slice<u8> entry_point
        {
            get => _entry_point;
            init => _entry_point = value;
        }
    }

    internal struct DepthStencilState
    {
        public required TextureFormat format;
        public required bool depth_write_enabled;
        public required Wgpu.CompareFunction depth_compare;
        public required Wgpu.StencilState stencil;
        public required Wgpu.DepthBiasState bias;
    }

    internal readonly struct VertexState
    {
        private readonly NativePointer _module;
        private readonly Slice<u8> _entry_point;
        private readonly Slice<VertexBufferLayout> _buffers;

        public unsafe required Rust.Ref<Wgpu.ShaderModule> module
        {
            get
            {
                var module = _module;
                return *(Rust.Ref<Wgpu.ShaderModule>*)(&module);
            }
            init => _module = value.AsPtr();
        }
        public required Slice<u8> entry_point { get => _entry_point; init => _entry_point = value; }
        public required Slice<VertexBufferLayout> buffers { get => _buffers; init => _buffers = value; }
    }

    internal readonly struct FragmentState
    {
        private readonly NativePointer _module;
        private readonly Slice<u8> _entry_point;
        private readonly Slice<Opt<ColorTargetState>> _targets;

        public unsafe required Rust.Ref<Wgpu.ShaderModule> module
        {
            get
            {
                var module = _module;
                return *(Rust.Ref<Wgpu.ShaderModule>*)(&module);
            }
            init => _module = value.AsPtr();
        }
        public required Slice<u8> entry_point { get => _entry_point; init => _entry_point = value; }
        public required Slice<Opt<ColorTargetState>> targets { get => _targets; init => _targets = value; }
    }

    internal struct ColorTargetState
    {
        public required TextureFormat format;
        public required Opt<Wgpu.BlendState> blend;
        public required Wgpu.ColorWrites write_mask;
    }

    internal struct PrimitiveState
    {
        public required Wgpu.PrimitiveTopology topology;
        public required Opt<Wgpu.IndexFormat> strip_index_format;
        public required Wgpu.FrontFace front_face;
        public required Opt<Wgpu.Face> cull_mode;
        public required Wgpu.PolygonMode polygon_mode;
    }

    internal struct BindGroupLayoutEntry
    {
        public required u32 binding;
        public required Wgpu.ShaderStages visibility;
        public required BindingType ty;
        public required u32 count;
    }

    internal unsafe readonly struct BindingType
    {
        private readonly BindingTypeTag tag;
        private readonly void* payload;

        private BindingType(BindingTypeTag tag, void* payload)
        {
            this.tag = tag;
            this.payload = payload;
        }

        public unsafe static BindingType Buffer(BufferBindingData* payload) => new(BindingTypeTag.Buffer, payload);

        public unsafe static BindingType Texture(TextureBindingData* payload) => new(BindingTypeTag.Texture, payload);

        public unsafe static BindingType Sampler(SamplerBindingType* payload) => new(BindingTypeTag.Sampler, payload);

        public unsafe static BindingType StorageTexture(StorageTextureBindingData* payload) => new(BindingTypeTag.StorageTexture, payload);

        private enum BindingTypeTag : u32
        {
            Buffer = 0,
            Sampler = 1,
            Texture = 2,
            StorageTexture = 3,
        }
    }

    internal struct BufferBindingData
    {
        public required BufferBindingType ty;
        public required bool has_dynamic_offset;
        public required u64 min_binding_size;
    }

    internal enum BufferBindingType
    {
        Uniform = 0,
        Storate = 1,
        StorateReadOnly = 2,
    }

    internal enum SamplerBindingType
    {
        Filtering = 0,
        NonFiltering = 1,
        Comparison = 2,
    }

    internal struct TextureBindingData
    {
        public required TextureSampleType sample_type;
        public required TextureViewDimension view_dimension;
        public required bool multisampled;
    }

    internal enum TextureSampleType
    {
        [EnumMapTo(Hikari.TextureSampleType.FloatFilterable)] FloatFilterable = 0,
        [EnumMapTo(Hikari.TextureSampleType.FloatNotFilterable)] FloatNotFilterable = 1,
        [EnumMapTo(Hikari.TextureSampleType.Depth)] Depth = 2,
        [EnumMapTo(Hikari.TextureSampleType.Sint)] Sint = 3,
        [EnumMapTo(Hikari.TextureSampleType.Uint)] Uint = 4,
    }

    internal enum TextureViewDimension
    {
        D1 = 0,
        D2 = 1,
        D2Array = 2,
        Cube = 3,
        CubeArray = 4,
        D3 = 5,
    }

    internal struct StorageTextureBindingData
    {
        public required StorageTextureAccess access;
        public required TextureFormat format;
        public required TextureViewDimension view_dimension;
    }

    internal enum StorageTextureAccess : u32
    {
        WriteOnly = 0,
        ReadOnly = 1,
        ReadWrite = 2,
    }

    internal struct TextureDescriptor
    {
        public required Wgpu.Extent3d size;
        public required u32 mip_level_count;
        public required u32 sample_count;
        public required TextureDimension dimension;
        public required TextureFormat format;
        public required Wgpu.TextureUsages usage;
    }

    internal enum TextureDimension : u32
    {
        [EnumMapTo(Hikari.TextureDimension.D1)] D1 = 0,
        [EnumMapTo(Hikari.TextureDimension.D2)] D2 = 1,
        [EnumMapTo(Hikari.TextureDimension.D3)] D3 = 2,
    }

    internal struct VertexBufferLayout
    {
        public required u64 array_stride;
        public required Wgpu.VertexStepMode step_mode;
        public required Slice<Wgpu.VertexAttribute> attributes;
    }

    internal readonly struct BufferSlice
    {
        private readonly NativePointer _buffer;
        private readonly RangeBoundsU64 _range;

        public unsafe required Rust.Ref<Wgpu.Buffer> buffer
        {
            get
            {
                var buffer = _buffer;
                return *(Rust.Ref<Wgpu.Buffer>*)(&buffer);
            }
            init => _buffer = value.AsPtr();
        }

        public required RangeBoundsU64 range
        {
            get => _range;
            init => _range = value;
        }

        [SetsRequiredMembers]
        public BufferSlice(Rust.Ref<Wgpu.Buffer> buffer, RangeBoundsU64 range)
        {
            _buffer = buffer.AsPtr();
            _range = range;
        }
    }
}
