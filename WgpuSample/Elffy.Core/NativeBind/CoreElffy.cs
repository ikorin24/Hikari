#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elffy.NativeBind;

/// <summary>
/// `coreelffy` crate in Rust
/// </summary>
internal static class CoreElffy
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

        public required u64 start { get => start; init => start = value; }
        public required u64 end_excluded { get => end_excluded; init => end_excluded = value; }
        public required bool has_start { get => has_start; init => has_start = value; }
        public required bool has_end_excluded { get => has_end_excluded; init => has_end_excluded = value; }

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
        public required KeyboardEventFn event_keyboard;
        public required CharReceivedEventFn event_char_received;
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
    }

    internal readonly record struct ErrMessageId(nuint Value);

    internal struct HostScreenInfo
    {
        public required Wgpu.Backend backend;
        public required Opt<Wgpu.TextureFormat> surface_format;
    }

    internal unsafe readonly struct HostScreenInitFn
    {
        private readonly delegate* unmanaged[Cdecl]<Rust.Box<HostScreen>, HostScreenInfo*, ScreenId> _func;

        public HostScreenInitFn(delegate* unmanaged[Cdecl]<void*, HostScreenInfo*, ScreenId> f)
        {
            _func = (delegate* unmanaged[Cdecl]<Rust.Box<HostScreen>, HostScreenInfo*, ScreenId>)f;
        }
    }

    internal unsafe readonly struct DispatchErrFn
    {
        private readonly delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> _func;

        public DispatchErrFn(delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> f) => _func = f;
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

    internal readonly struct BeginCommandData
    {
        public readonly bool success;
        public readonly Rust.OptionBox<Wgpu.CommandEncoder> command_encoder;
        public readonly Rust.OptionBox<Wgpu.SurfaceTexture> surface_texture;
        public readonly Rust.OptionBox<Wgpu.TextureView> surface_texture_view;
    }

    internal struct SizeU32
    {
        public u32 width;
        public u32 height;
    }

    internal readonly struct RenderPassDepthStencilAttachment
    {
        private readonly NativePointer _view;
        private readonly Opt<f32> _depth_clear;
        private readonly Opt<u32> _stencil_clear;

        public unsafe required Rust.Ref<Wgpu.TextureView> view
        {
            get
            {
                var view = _view;
                return *(Rust.Ref<Wgpu.TextureView>*)(&view);
            }
            init => _view = value.AsPtr();
        }
        public required Opt<f32> depth_clear { get => _depth_clear; init => _depth_clear = value; }
        public required Opt<u32> stencil_clear { get => _stencil_clear; init => _stencil_clear = value; }
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

    internal readonly struct RenderPassDescriptor
    {
        private readonly Slice<Opt<RenderPassColorAttachment>> _color_attachments_clear;
        private readonly Opt<RenderPassDepthStencilAttachment> _depth_stencil_attachment_clear;

        public required Slice<Opt<RenderPassColorAttachment>> color_attachments_clear
        {
            get => _color_attachments_clear; init => _color_attachments_clear = value;
        }
        public required Opt<RenderPassDepthStencilAttachment> depth_stencil_attachment_clear
        {
            get => _depth_stencil_attachment_clear; init => _depth_stencil_attachment_clear = value;
        }
    }

    internal readonly struct RenderPassColorAttachment
    {
        private readonly NativePointer _view;   // Rust.Ref<Wgpu.TextureView>
        private readonly Wgpu.Color _clear;

        public unsafe required Rust.Ref<Wgpu.TextureView> view
        {
            get
            {
                var view = _view;
                return *(Rust.Ref<Wgpu.TextureView>*)(&view);
            }
            init => _view = value.AsPtr();
        }

        public required Wgpu.Color clear { get => _clear; init => _clear = value; }
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

    internal struct DepthStencilState
    {
        public required Wgpu.TextureFormat format;
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
        public required Wgpu.TextureFormat format;
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

        private enum BindingTypeTag
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
        FloatFilterable = 0,
        FloatNotFilterable = 1,
        Depth = 2,
        Sint = 3,
        Uint = 4,
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
        public required Wgpu.TextureFormat format;
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
        public required Wgpu.TextureFormat format;
        public required Wgpu.TextureUsages usage;
    }

    internal enum TextureDimension : u32
    {
        D1 = 0,
        D2 = 1,
        D3 = 2,
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
