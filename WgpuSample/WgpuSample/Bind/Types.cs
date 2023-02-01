#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Bind;

internal enum WindowStyle
{
    Default = 0,
    Fixed = 1,
    Fullscreen = 2,
}

internal ref struct RenderPassDescriptor
{
    public required Slice_Opt_RenderPassColorAttachment color_attachments_clear;
    public required Opt_RenderPassDepthStencilAttachment depth_stencil_attachment_clear;
}

// Slice<Opt<RenderPassColorAttachment>>
internal unsafe ref struct Slice_Opt_RenderPassColorAttachment
{
    public required Opt_RenderPassColorAttachment* data;
    public required usize len;
}

// Opt<RenderPassColorAttachment>
internal ref struct Opt_RenderPassColorAttachment
{
    public required bool exists;
    public required RenderPassColorAttachment value;

    public static Opt_RenderPassColorAttachment None => default;
}

internal ref struct RenderPassColorAttachment
{
    public required Ref<Wgpu.TextureView> view;
    public required Wgpu.Color clear;
}

// Opt<RenderPassDepthStencilAttachment>
internal ref struct Opt_RenderPassDepthStencilAttachment
{
    public required bool exists;
    public required CE.RenderPassDepthStencilAttachment value;

    public static Opt_RenderPassDepthStencilAttachment None => default;
}

internal ref struct BindGroupDescriptor
{
    public required Ref<Wgpu.BindGroupLayout> layout;
    public required Slice_BindGroupEntry entries;
}

// Slice<BindGroupEntry>
internal unsafe ref struct Slice_BindGroupEntry
{
    public required BindGroupEntry* data;
    public required usize len;
}

internal ref struct BindGroupEntry
{
    public required u32 binding;
    public required BindingResource resource;
}

internal unsafe readonly ref struct BindingResource
{
    private readonly BindingResourceTag tag;
    private readonly void* payload;

    public BindingResource(BindingResourceTag tag, void* payload)
    {
        this.tag = tag;
        this.payload = payload;
    }

    public unsafe static BindingResource Buffer(BufferBinding* payload) => new(BindingResourceTag.Buffer, payload);

    public unsafe static BindingResource TextureView(Ref<Wgpu.TextureView> textureView) => new(BindingResourceTag.TextureView, textureView.AsPtr());

    public unsafe static BindingResource Sampler(Ref<Wgpu.Sampler> sampler) => new(BindingResourceTag.Sampler, sampler.AsPtr());
}

internal enum BindingResourceTag
{
    Buffer = 0,
    BufferArray = 1,
    Sampler = 2,
    SamplerArray = 3,
    TextureView = 4,
    TextureViewArray = 5,
}

internal ref struct BufferBinding
{
    public required Ref<Wgpu.Buffer> buffer;
    public required u64 offset;
    public required u64 size;
}

internal ref struct PipelineLayoutDescriptor
{
    public required Slice_Ref_WgpuBindGroupLayout bind_group_layouts;
}

// Slice<Ref<Wgpu.BindGroupLayout>>
internal unsafe ref struct Slice_Ref_WgpuBindGroupLayout
{
    public required Ref<Wgpu.BindGroupLayout>* data;
    public required usize len;
}

internal ref struct RenderPipelineDescriptor
{
    public required Ref<Wgpu.PipelineLayout> layout;
    public required VertexState vertex;
    public required Opt_FragmentState fragment;
    public required PrimitiveState primitive;
    public required Opt<DepthStencilState> depth_stencil;
    public required Wgpu.MultisampleState multisample;
    public required NonZeroU32OrNone multiview;
}

internal struct DepthStencilState
{
    public required TextureFormat format;
    public required bool depth_write_enabled;
    public required Wgpu.CompareFunction depth_compare;
    public required Wgpu.StencilState stencil;
    public required Wgpu.DepthBiasState bias;
}

internal ref struct VertexState
{
    public required Ref<Wgpu.ShaderModule> module;
    public required Slice<u8> entry_point;
    public required Slice<VertexBufferLayout> buffers;
}

internal ref struct FragmentState
{
    public required Ref<Wgpu.ShaderModule> module;
    public required Slice<u8> entry_point;
    public required Slice<Opt<ColorTargetState>> targets;
}

// Opt<FragmentState>
internal ref struct Opt_FragmentState
{
    public required bool exists;
    public required FragmentState value;
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
}

internal enum BindingTypeTag
{
    Buffer = 0,
    Sampler = 1,
    Texture = 2,
    StorageTexture = 3,
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
    public required TextureFormat format;
    public required TextureViewDimension view_dimension;
}

internal enum StorageTextureAccess
{
    WriteOnly = 0,
    ReadOnly = 1,
    ReadWrite = 2,
}

internal enum TextureFormat
{
    R8Unorm = 0,
    R8Snorm = 1,
    R8Uint = 2,
    R8Sint = 3,
    R16Uint = 4,
    R16Sint = 5,
    R16Unorm = 6,
    R16Snorm = 7,
    R16Float = 8,
    Rg8Unorm = 9,
    Rg8Snorm = 10,
    Rg8Uint = 11,
    Rg8Sint = 12,
    R32Uint = 13,
    R32Sint = 14,
    R32Float = 15,
    Rg16Uint = 16,
    Rg16Sint = 17,
    Rg16Unorm = 18,
    Rg16Snorm = 19,
    Rg16Float = 20,
    Rgba8Unorm = 21,
    Rgba8UnormSrgb = 22,
    Rgba8Snorm = 23,
    Rgba8Uint = 24,
    Rgba8Sint = 25,
    Bgra8Unorm = 26,
    Bgra8UnormSrgb = 27,
    Rgb10a2Unorm = 28,
    Rg11b10Float = 29,
    Rg32Uint = 30,
    Rg32Sint = 31,
    Rg32Float = 32,
    Rgba16Uint = 33,
    Rgba16Sint = 34,
    Rgba16Unorm = 35,
    Rgba16Snorm = 36,
    Rgba16Float = 37,
    Rgba32Uint = 38,
    Rgba32Sint = 39,
    Rgba32Float = 40,
    Depth32Float = 41,
    Depth32FloatStencil8 = 42,
    Depth24Plus = 43,
    Depth24PlusStencil8 = 44,
    Depth24UnormStencil8 = 45,
    Rgb9e5Ufloat = 46,
    Bc1RgbaUnorm = 47,
    Bc1RgbaUnormSrgb = 48,
    Bc2RgbaUnorm = 49,
    Bc2RgbaUnormSrgb = 50,
    Bc3RgbaUnorm = 51,
    Bc3RgbaUnormSrgb = 52,
    Bc4RUnorm = 53,
    Bc4RSnorm = 54,
    Bc5RgUnorm = 55,
    Bc5RgSnorm = 56,
    Bc6hRgbUfloat = 57,
    Bc6hRgbSfloat = 58,
    Bc7RgbaUnorm = 59,
    Bc7RgbaUnormSrgb = 60,
    Etc2Rgb8Unorm = 61,
    Etc2Rgb8UnormSrgb = 62,
    Etc2Rgb8A1Unorm = 63,
    Etc2Rgb8A1UnormSrgb = 64,
    Etc2Rgba8Unorm = 65,
    Etc2Rgba8UnormSrgb = 66,
    EacR11Unorm = 67,
    EacR11Snorm = 68,
    EacRg11Unorm = 69,
    EacRg11Snorm = 70,
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

// Slice<SlotBufSlice>
internal unsafe ref struct Slice_SlotBufSlice
{
    public required SlotBufSlice* data;
    public required usize len;
}

internal ref struct SlotBufSlice
{
    public required BufferSlice buffer_slice;
    public required u32 slot;
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
