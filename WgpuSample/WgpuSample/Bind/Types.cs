#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Bind;

internal readonly struct RenderPassDescriptor
{
    private readonly Slice<Opt<RenderPassColorAttachment>> _color_attachments_clear;
    private readonly Opt<CE.RenderPassDepthStencilAttachment> _depth_stencil_attachment_clear;

    public required Slice<Opt<RenderPassColorAttachment>> color_attachments_clear
    {
        get => _color_attachments_clear; init => _color_attachments_clear = value;
    }
    public required Opt<CE.RenderPassDepthStencilAttachment> depth_stencil_attachment_clear
    {
        get => _depth_stencil_attachment_clear; init => _depth_stencil_attachment_clear = value;
    }
}

internal readonly struct RenderPassColorAttachment
{
    private readonly NativePointer _view;   // Ref<Wgpu.TextureView>
    private readonly Wgpu.Color _clear;

    public unsafe required Ref<Wgpu.TextureView> view
    {
        get
        {
            var view = _view;
            return *(Ref<Wgpu.TextureView>*)(&view);
        }
        init => _view = value.AsPtr();
    }

    public required Wgpu.Color clear { get => _clear; init => _clear = value; }
}

internal readonly struct BindGroupDescriptor
{
    private readonly NativePointer _layout;
    private readonly Slice<BindGroupEntry> _entries;

    public unsafe required Ref<Wgpu.BindGroupLayout> layout
    {
        get
        {
            var layout = _layout;
            return *(Ref<Wgpu.BindGroupLayout>*)(&layout);
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

    public unsafe static BindingResource TextureView(Ref<Wgpu.TextureView> textureView) => new(BindingResourceTag.TextureView, textureView.AsPtr());

    public unsafe static BindingResource Sampler(Ref<Wgpu.Sampler> sampler) => new(BindingResourceTag.Sampler, sampler.AsPtr());

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

    public unsafe required Ref<Wgpu.Buffer> buffer
    {
        get
        {
            var buffer = _buffer;
            return *(Ref<Wgpu.Buffer>*)(&buffer);
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

    public unsafe PipelineLayoutDescriptor(Ref<Wgpu.BindGroupLayout>* bind_group_layouts, nuint count)
    {
        _bind_group_layouts = new Slice<NativePointer>((NativePointer*)bind_group_layouts, count);
    }
}

internal readonly struct RenderPipelineDescriptor
{
    private readonly NativePointer _layout;
    private readonly VertexState _vertex;
    private readonly Opt<FragmentState> _fragment;
    private readonly PrimitiveState _primitive;
    private readonly Opt<DepthStencilState> _depth_stencil;
    private readonly Wgpu.MultisampleState _multisample;
    private readonly NonZeroU32OrNone _multiview;

    public unsafe required Ref<Wgpu.PipelineLayout> layout
    {
        get
        {
            var layout = _layout;
            return *(Ref<Wgpu.PipelineLayout>*)(&layout);
        }
        init => _layout = value.AsPtr();
    }
    public required VertexState vertex { get => _vertex; init => _vertex = value; }
    public required Opt<FragmentState> fragment { get => _fragment; init => _fragment = value; }
    public required PrimitiveState primitive { get => _primitive; init => _primitive = value; }
    public required Opt<DepthStencilState> depth_stencil { get => _depth_stencil; init => _depth_stencil = value; }
    public required Wgpu.MultisampleState multisample { get => _multisample; init => _multisample = value; }
    public required NonZeroU32OrNone multiview { get => _multiview; init => _multiview = value; }
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

    public unsafe required Ref<Wgpu.ShaderModule> module
    {
        get
        {
            var module = _module;
            return *(Ref<Wgpu.ShaderModule>*)(&module);
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

    public unsafe required Ref<Wgpu.ShaderModule> module
    {
        get
        {
            var module = _module;
            return *(Ref<Wgpu.ShaderModule>*)(&module);
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

internal enum TextureDimension
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

[StructLayout(LayoutKind.Sequential)]
internal struct Opt<T> where T : unmanaged
{
    public required bool exists;
    public required T value;
    public static Opt<T> None => default;

    public static Opt<T> Some(T value) => new() { exists = true, value = value };

    public bool TryGetValue(out T value)
    {
        value = this.value;
        return this.exists;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap()
    {
        if(exists == false) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new InvalidOperationException("cannot get value");
        }
        return value;
    }
}

internal ref struct BufferSlice
{
    public required Ref<Wgpu.Buffer> buffer;
    public required RangeBoundsU64 range;

    [SetsRequiredMembers]
    public BufferSlice(Ref<Wgpu.Buffer> buffer, RangeBoundsU64 range)
    {
        this.buffer = buffer;
        this.range = range;
    }
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

internal static class Slice
{
    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(Span<T> fixedSpan) where T : unmanaged
        => FromFixedSpanUnsafe((ReadOnlySpan<T>)fixedSpan);

    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(ReadOnlySpan<T> fixedSpan) where T : unmanaged
    {
        var pointer = (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(fixedSpan));
        return new Slice<T>(pointer, fixedSpan.Length);
    }
}

internal struct RangeU32
{
    public required u32 start;
    public required u32 end_excluded;

    [SetsRequiredMembers]
    public RangeU32(u32 start, u32 end_excluded)
    {
        this.start = start;
        this.end_excluded = end_excluded;
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

internal struct RangeBoundsU64
{
    public required u64 start;
    public required u64 end_excluded;
    public required bool has_start;
    public required bool has_end_excluded;

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

/// <summary>
/// `Option&lt;NonZeroU32&gt;` in Rust
/// </summary>
internal readonly struct NonZeroU32OrNone : IEquatable<NonZeroU32OrNone>
{
    private readonly u32 _value;
    public static NonZeroU32OrNone None => default;

    private NonZeroU32OrNone(u32 value) => _value = value;

    public static implicit operator NonZeroU32OrNone(u32 value) => new(value);

    public static bool operator ==(NonZeroU32OrNone left, NonZeroU32OrNone right) => left.Equals(right);

    public static bool operator !=(NonZeroU32OrNone left, NonZeroU32OrNone right) => !(left == right);

    public override bool Equals(object? obj) => obj is NonZeroU32OrNone none && Equals(none);

    public bool Equals(NonZeroU32OrNone other) => _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();
}
