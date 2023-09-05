#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hikari.NativeBind;

/// <summary>
/// `wgpu` crate in Rust
/// </summary>
internal static class Wgpu
{
    internal sealed class BindGroupLayout : INativeTypeNonReprC { private BindGroupLayout() { } }
    internal sealed class BindGroup : INativeTypeNonReprC { private BindGroup() { } }
    internal sealed class Buffer : INativeTypeNonReprC { private Buffer() { } }
    internal sealed class RenderPipeline : INativeTypeNonReprC { private RenderPipeline() { } }
    internal sealed class ComputePipeline : INativeTypeNonReprC { private ComputePipeline() { } }
    internal sealed class Sampler : INativeTypeNonReprC { private Sampler() { } }
    internal sealed class PipelineLayout : INativeTypeNonReprC { private PipelineLayout() { } }
    internal sealed class ShaderModule : INativeTypeNonReprC { private ShaderModule() { } }
    internal sealed class Texture : INativeTypeNonReprC { private Texture() { } }
    internal sealed class TextureView : INativeTypeNonReprC { private TextureView() { } }
    internal sealed class CommandEncoder : INativeTypeNonReprC { private CommandEncoder() { } }
    internal sealed class RenderPass : INativeTypeNonReprC { private RenderPass() { } }
    internal sealed class ComputePass : INativeTypeNonReprC { private ComputePass() { } }
    internal sealed class SurfaceTexture : INativeTypeNonReprC { private SurfaceTexture() { } }

    [Flags]
    internal enum Features : u64
    {
        //
        // ---- Start numbering at 1 << 0 ----
        //
        // WebGPU features:
        //

        DEPTH_CLIP_CONTROL = 1uL << 0,
        DEPTH32FLOAT_STENCIL8 = 1uL << 2,
        TEXTURE_COMPRESSION_BC = 1uL << 3,
        TEXTURE_COMPRESSION_ETC2 = 1uL << 4,
        TEXTURE_COMPRESSION_ASTC_LDR = 1uL << 5,
        INDIRECT_FIRST_INSTANCE = 1uL << 6,
        TIMESTAMP_QUERY = 1uL << 7,
        PIPELINE_STATISTICS_QUERY = 1uL << 8,
        SHADER_FLOAT16 = 1uL << 9,

        //
        // ---- Restart Numbering for Native Features ---
        //
        // Native Features:
        //

        MAPPABLE_PRIMARY_BUFFERS = 1uL << 16,
        TEXTURE_BINDING_ARRAY = 1uL << 17,
        BUFFER_BINDING_ARRAY = 1uL << 18,
        STORAGE_RESOURCE_BINDING_ARRAY = 1uL << 19,
        SAMPLED_TEXTURE_AND_STORAGE_BUFFER_ARRAY_NON_UNIFORM_INDEXING = 1uL << 20,
        UNIFORM_BUFFER_AND_STORAGE_TEXTURE_ARRAY_NON_UNIFORM_INDEXING = 1uL << 21,
        PARTIALLY_BOUND_BINDING_ARRAY = 1uL << 22,
        MULTI_DRAW_INDIRECT = 1uL << 23,
        MULTI_DRAW_INDIRECT_COUNT = 1uL << 24,
        PUSH_CONSTANTS = 1uL << 25,
        ADDRESS_MODE_CLAMP_TO_BORDER = 1uL << 26,
        POLYGON_MODE_LINE = 1uL << 27,
        POLYGON_MODE_POINT = 1uL << 28,
        TEXTURE_ADAPTER_SPECIFIC_FORMAT_FEATURES = 1uL << 29,
        SHADER_FLOAT64 = 1uL << 30,
        VERTEX_ATTRIBUTE_64BIT = 1uL << 31,
        CONSERVATIVE_RASTERIZATION = 1uL << 32,
        VERTEX_WRITABLE_STORAGE = 1uL << 33,
        CLEAR_TEXTURE = 1uL << 34,
        SPIRV_SHADER_PASSTHROUGH = 1uL << 35,
        SHADER_PRIMITIVE_INDEX = 1uL << 36,
        MULTIVIEW = 1uL << 37,
        TEXTURE_FORMAT_16BIT_NORM = 1uL << 38,
        ADDRESS_MODE_CLAMP_TO_ZERO = 1uL << 39,
        TEXTURE_COMPRESSION_ASTC_HDR = 1uL << 40,
        WRITE_TIMESTAMP_INSIDE_PASSES = 1uL << 41,
    }


    internal enum Face
    {
        Front = 0,
        Back = 1,
    }

    [Flags]
    internal enum ShaderStages : u32
    {
        NONE = 0,
        VERTEX = 1 << 0,
        FRAGMENT = 1 << 1,
        COMPUTE = 1 << 2,
        VERTEX_FRAGMENT = VERTEX | FRAGMENT,
    }

    internal enum PolygonMode : u32
    {
        Fill = 0,
        Line = 1,
        Point = 2,
    }

    internal enum FrontFace : u32
    {
        Ccw = 0,
        Cw = 1,
    }

    internal enum IndexFormat : u32
    {
        Uint16 = 0,
        Uint32 = 1,
    }

    internal enum PrimitiveTopology : u32
    {
        PointList = 0,
        LineList = 1,
        LineStrip = 2,
        TriangleList = 3,
        TriangleStrip = 4,
    }

    internal enum BlendOperation : u32
    {
        Add = 0,
        Subtract = 1,
        ReverseSubtract = 2,
        Min = 3,
        Max = 4,
    }

    internal enum BlendFactor : u32
    {
        Zero = 0,
        One = 1,
        Src = 2,
        OneMinusSrc = 3,
        SrcAlpha = 4,
        OneMinusSrcAlpha = 5,
        Dst = 6,
        OneMinusDst = 7,
        DstAlpha = 8,
        OneMinusDstAlpha = 9,
        SrcAlphaSaturated = 10,
        Constant = 11,
        OneMinusConstant = 12,
    }

    internal enum AddressMode : u32
    {
        ClampToEdge = 0,
        Repeat = 1,
        MirrorRepeat = 2,
        ClampToBorder = 3,
    }

    internal enum FilterMode : u32
    {
        Nearest = 0,
        Linear = 1,
    }

    internal enum CompareFunction : u32
    {
        Never = 1,
        Less = 2,
        Equal = 3,
        LessEqual = 4,
        Greater = 5,
        NotEqual = 6,
        GreaterEqual = 7,
        Always = 8,
    }

    [Flags]
    internal enum ColorWrites : u32
    {
        RED = 1 << 0,
        GREEN = 1 << 1,
        BLUE = 1 << 2,
        ALPHA = 1 << 3,
        COLOR = RED | GREEN | BLUE,
        ALL = RED | GREEN | BLUE | ALPHA,
    }

    internal struct BlendState
    {
        public required BlendComponent color;
        public required BlendComponent alpha;

        public static BlendState REPLACE => new()
        {
            color = BlendComponent.REPLACE,
            alpha = BlendComponent.REPLACE,
        };
    }

    internal struct BlendComponent
    {
        public required BlendFactor src_factor;
        public required BlendFactor dst_factor;
        public required BlendOperation operation;

        public static BlendComponent REPLACE => new()
        {
            src_factor = BlendFactor.One,
            dst_factor = BlendFactor.Zero,
            operation = BlendOperation.Add,
        };
    }

    internal struct Extent3d
    {
        public required u32 width;
        public required u32 height;
        public required u32 depth_or_array_layers;

        public Extent3d()
        {
        }

        [SetsRequiredMembers]
        public Extent3d(u32 width, u32 height, u32 depth_or_array_layers)
        {
            this.width = width;
            this.height = height;
            this.depth_or_array_layers = depth_or_array_layers;
        }
    }

    [Flags]
    internal enum TextureUsages : u32
    {
        [EnumMapTo(Hikari.TextureUsages.CopySrc)] COPY_SRC = 1 << 0,
        [EnumMapTo(Hikari.TextureUsages.CopyDst)] COPY_DST = 1 << 1,
        [EnumMapTo(Hikari.TextureUsages.TextureBinding)] TEXTURE_BINDING = 1 << 2,
        [EnumMapTo(Hikari.TextureUsages.StorageBinding)] STORAGE_BINDING = 1 << 3,
        [EnumMapTo(Hikari.TextureUsages.RenderAttachment)] RENDER_ATTACHMENT = 1 << 4,
    }

    [Flags]
    internal enum TextureFormatFeatureFlags : u32
    {
        FILTERABLE = 1 << 0,
        MULTISAMPLE_X2 = 1 << 1,
        MULTISAMPLE_X4 = 1 << 2,
        MULTISAMPLE_X8 = 1 << 3,
        MULTISAMPLE_RESOLVE = 1 << 4,
        STORAGE_READ_WRITE = 1 << 5,
        STORAGE_ATOMICS = 1 << 6,
        BLENDABLE = 1 << 7,
    }

    internal struct VertexAttribute
    {
        public required VertexFormat format;
        public required u64 offset;
        public required u32 shader_location;
    }

    internal enum VertexFormat : u32
    {
        Uint8x2 = 0,
        Uint8x4 = 1,
        Sint8x2 = 2,
        Sint8x4 = 3,
        Unorm8x2 = 4,
        Unorm8x4 = 5,
        Snorm8x2 = 6,
        Snorm8x4 = 7,
        Uint16x2 = 8,
        Uint16x4 = 9,
        Sint16x2 = 10,
        Sint16x4 = 11,
        Unorm16x2 = 12,
        Unorm16x4 = 13,
        Snorm16x2 = 14,
        Snorm16x4 = 15,
        Float16x2 = 16,
        Float16x4 = 17,
        Float32 = 18,
        Float32x2 = 19,
        Float32x3 = 20,
        Float32x4 = 21,
        Uint32 = 22,
        Uint32x2 = 23,
        Uint32x3 = 24,
        Uint32x4 = 25,
        Sint32 = 26,
        Sint32x2 = 27,
        Sint32x3 = 28,
        Sint32x4 = 29,
        Float64 = 30,
        Float64x2 = 31,
        Float64x3 = 32,
        Float64x4 = 33,
    }


    [Flags]
    internal enum BufferUsages : u32
    {
        MAP_READ = 1 << 0,
        MAP_WRITE = 1 << 1,
        COPY_SRC = 1 << 2,
        COPY_DST = 1 << 3,
        INDEX = 1 << 4,
        VERTEX = 1 << 5,
        UNIFORM = 1 << 6,
        STORAGE = 1 << 7,
        INDIRECT = 1 << 8,
    }

    internal enum Backend : u8
    {
        [EnumMapTo(GraphicsBackend.None)] Empty = 0,
        [EnumMapTo(GraphicsBackend.Vulkan)] Vulkan = 1,
        [EnumMapTo(GraphicsBackend.Metal)] Metal = 2,
        [EnumMapTo(GraphicsBackend.Dx12)] Dx12 = 3,
        [EnumMapTo(GraphicsBackend.Dx11)] Dx11 = 4,
        [EnumMapTo(GraphicsBackend.GL)] Gl = 5,
        [EnumMapTo(GraphicsBackend.BrowserWebGpu)] BrowserWebGpu = 6,
    }

    [Flags]
    internal enum Backends : u32
    {
        NONE = 1 << Backend.Empty,

        VULKAN = 1 << Backend.Vulkan,
        GL = 1 << Backend.Gl,
        METAL = 1 << Backend.Metal,
        DX12 = 1 << Backend.Dx12,
        DX11 = 1 << Backend.Dx11,
        BROWSER_WEBGPU = 1 << Backend.BrowserWebGpu,

        PRIMARY = VULKAN | METAL | DX12 | BROWSER_WEBGPU,
        SECONDARY = GL | DX11,
        ALL = VULKAN | GL | METAL | DX12 | DX11 | BROWSER_WEBGPU,
    }

    internal struct ImageDataLayout
    {
        public required u64 offset;
        public required u32 bytes_per_row;
        public required u32 rows_per_image;
    }

    internal enum VertexStepMode : u32
    {
        Vertex = 0,
        Instance = 1,
    }

    internal record struct Color(f64 R, f64 G, f64 B, f64 A);

    internal struct StencilState
    {
        public required StencilFaceState front;
        public required StencilFaceState back;
        public required u32 read_mask;
        public required u32 write_mask;

        public static StencilState Default => new()
        {
            front = StencilFaceState.Default,
            back = StencilFaceState.Default,
            read_mask = 0,
            write_mask = 0,
        };
    }

    internal struct StencilFaceState
    {
        public required CompareFunction compare;
        public required StencilOperation fail_op;
        public required StencilOperation depth_fail_op;
        public required StencilOperation pass_op;

        public static StencilFaceState Default => Ignore;

        public static StencilFaceState Ignore => new()
        {
            compare = CompareFunction.Always,
            fail_op = StencilOperation.Keep,
            depth_fail_op = StencilOperation.Keep,
            pass_op = StencilOperation.Keep,
        };
    }

    internal enum StencilOperation : u32
    {
        Keep = 0,
        Zero = 1,
        Replace = 2,
        Invert = 3,
        IncrementClamp = 4,
        DecrementClamp = 5,
        IncrementWrap = 6,
        DecrementWrap = 7,
    }

    internal struct DepthBiasState
    {
        public required i32 constant;
        public required f32 slope_scale;
        public required f32 clamp;

        public static DepthBiasState Default => default;
    }

    internal struct MultisampleState
    {
        public required u32 count;
        public required u64 mask;
        public required bool alpha_to_coverage_enabled;

        public static MultisampleState Default => new()
        {
            count = 1,
            mask = 0xffff_ffff_ffff_ffff,
            alpha_to_coverage_enabled = false,
        };
    }
}
