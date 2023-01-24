#nullable enable
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using i32 = System.Int32;
using u64 = System.UInt64;
using f32 = System.Single;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Elffy.Bind;

#pragma warning disable IDE1006 // naming style

/// <summary>Opaque wrapper of a native pointer</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe readonly struct NativePointer : IEquatable<NativePointer>
{
    private readonly void* _ptr;

    internal static NativePointer Null => default;

    private NativePointer(void* p) => _ptr = p;

    public override bool Equals(object? obj) => obj is NativePointer handle && Equals(handle);

    public bool Equals(NativePointer other) => _ptr == other._ptr;

    public override int GetHashCode() => ((IntPtr)_ptr).GetHashCode();

    public static bool operator ==(NativePointer left, NativePointer right) => left.Equals(right);

    public static bool operator !=(NativePointer left, NativePointer right) => !(left == right);

    public override string ToString() => ((IntPtr)_ptr).ToString();

    public unsafe static implicit operator NativePointer(void* nativePtr) => new(nativePtr);
    public unsafe static implicit operator void*(NativePointer nativePtr) => nativePtr._ptr;
}

internal interface IHandle
{
    NativePointer Pointer { get; }
}

internal interface IHandle<TSelf> :
    IHandle
    where TSelf : unmanaged, IHandle<TSelf>
{
    static abstract TSelf DestroyedHandle { get; }
    unsafe static abstract explicit operator TSelf(void* nativePtr);
}

internal static class HandleExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDestroyed<THandle>(this THandle handle)
        where THandle : unmanaged, IHandle<THandle>, IEquatable<THandle>
    {
        return handle.Equals(THandle.DestroyedHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerHidden]
    public static void ThrowIfDestroyed<THandle>(this THandle handle)
        where THandle : unmanaged, IHandle<THandle>, IEquatable<THandle>
    {
        if(handle.Equals(THandle.DestroyedHandle)) {
            Throw();

            [DoesNotReturn]
            [DebuggerHidden]
            static void Throw() => throw new InvalidOperationException($"The handle is already destroyed (handle type '{typeof(THandle).Name}')");
        }
    }
}

internal enum WindowStyle
{
    Default = 0,
    Fixed = 1,
    Fullscreen = 2,
}

internal readonly record struct BindGroupLayoutHandle(NativePointer Pointer) : IHandle<BindGroupLayoutHandle>
{
    public static BindGroupLayoutHandle DestroyedHandle => default;
    public unsafe static explicit operator BindGroupLayoutHandle(void* nativePtr) => new(nativePtr);
}

internal readonly record struct BindGroupHandle(NativePointer Pointer) : IHandle<BindGroupHandle>
{
    public static BindGroupHandle DestroyedHandle => default;
    public unsafe static explicit operator BindGroupHandle(void* nativePtr) => new(nativePtr);
}

internal readonly record struct BufferHandle(NativePointer Pointer) : IHandle<BufferHandle>
{
    public static BufferHandle DestroyedHandle => default;
    public unsafe static explicit operator BufferHandle(void* nativePtr) => new(nativePtr);

    public BufferBinding AsEntireBufferBinding()
    {
        return new BufferBinding
        {
            buffer = this,
            offset = 0,
            size = 0,
        };
    }
}

internal readonly record struct RenderPipelineHandle(NativePointer Pointer) : IHandle<RenderPipelineHandle>
{
    public static RenderPipelineHandle DestroyedHandle => default;
    public unsafe static explicit operator RenderPipelineHandle(void* nativePtr) => new(nativePtr);
}

internal readonly record struct SamplerHandle(NativePointer Pointer) : IHandle<SamplerHandle>
{
    public static SamplerHandle DestroyedHandle => default;
    public unsafe static explicit operator SamplerHandle(void* nativePtr) => new(nativePtr);
}

internal readonly record struct PipelineLayoutHandle(NativePointer Pointer) : IHandle<PipelineLayoutHandle>
{
    public static PipelineLayoutHandle DestroyedHandle => default;
    public unsafe static explicit operator PipelineLayoutHandle(void* nativePtr) => new(nativePtr);
}

internal readonly record struct ShaderModuleHandle(NativePointer Pointer) : IHandle<ShaderModuleHandle>
{
    public static ShaderModuleHandle DestroyedHandle => default;
    public unsafe static explicit operator ShaderModuleHandle(void* nativePtr) => new(nativePtr);
}

internal unsafe readonly record struct TextureHandle(NativePointer Pointer) : IHandle<TextureHandle>
{
    public static TextureHandle DestroyedHandle => default;
    public static explicit operator TextureHandle(void* nativePtr) => new(nativePtr);

    public TextureViewHandle CreateTextureView() => CreateTextureView(TextureViewDescriptor.Default);

    public TextureViewHandle CreateTextureView(in TextureViewDescriptor desc)
    {
        fixed(TextureViewDescriptor* descPtr = &desc) {
            return EngineCore.CreateTextureView(this, descPtr);
        }
    }
}

internal readonly record struct TextureViewHandle(NativePointer Pointer) : IHandle<TextureViewHandle>
{
    public static TextureViewHandle DestroyedHandle => default;
    public unsafe static explicit operator TextureViewHandle(void* nativePtr) => new(nativePtr);
}

internal unsafe readonly struct HostScreenInitFn
{
#pragma warning disable IDE0052
    private readonly delegate* unmanaged[Cdecl]<HostScreenHandle, HostScreenInfo*, HostScreenCallbacks> _func;
#pragma warning restore IDE0052
    public HostScreenInitFn(delegate* unmanaged[Cdecl]<HostScreenHandle, HostScreenInfo*, HostScreenCallbacks> f) => _func = f;
}

internal unsafe readonly struct DispatchErrFn
{
#pragma warning disable IDE0052
    private readonly delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> _func;
#pragma warning restore IDE0052
    public DispatchErrFn(delegate* unmanaged[Cdecl]<ErrMessageId, u8*, nuint, void> f) => _func = f;
}

internal unsafe struct EngineCoreConfig
{
    public required HostScreenInitFn on_screen_init;
    public required DispatchErrFn err_dispatcher;
}

internal readonly record struct ErrMessageId(nuint Value);

internal struct HostScreenConfig
{
    public required Slice<u8> title;
    public required WindowStyle style;
    public required u32 width;
    public required u32 height;
    public required wgpu_Backends backend;
}

internal struct HostScreenInfo
{
    public required wgpu_Backend backend;
    public required Opt<TextureFormat> surface_format;
}

internal unsafe struct HostScreenCallbacks
{
    public required delegate* unmanaged[Cdecl]<HostScreenHandle, RenderPassRef, void> on_render;
}

internal struct BindGroupLayoutDescriptor
{
    public required Slice<BindGroupLayoutEntry> entries;
}

internal struct ImageCopyTexture
{
    public required TextureHandle texture;
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
    public required wgpu_AddressMode address_mode_u;
    public required wgpu_AddressMode address_mode_v;
    public required wgpu_AddressMode address_mode_w;
    public required wgpu_FilterMode mag_filter;
    public required wgpu_FilterMode min_filter;
    public required wgpu_FilterMode mipmap_filter;
    public required f32 lod_min_clamp;
    public required f32 lod_max_clamp;
    public required Opt<wgpu_CompareFunction> compare;
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

internal unsafe struct BindGroupDescriptor
{
    public required BindGroupLayoutHandle layout;
    public required Slice<BindGroupEntry> entries;
}

internal struct BindGroupEntry
{
    public required u32 binding;
    public required BindingResource resource;
}

internal struct BindingResource
{
    public required BindingResourceTag tag;
    public required Pointer payload;

    public unsafe static BindingResource Buffer(BufferBinding* payload) => new()
    {
        tag = BindingResourceTag.Buffer,
        payload = payload,
    };

    public unsafe static BindingResource TextureView(TextureViewHandle textureView) => new()
    {
        tag = BindingResourceTag.TextureView,
        payload = textureView.Pointer,
    };

    public unsafe static BindingResource Sampler(SamplerHandle sampler) => new()
    {
        tag = BindingResourceTag.Sampler,
        payload = sampler.Pointer,
    };
}

internal readonly struct Pointer : IEquatable<Pointer>
{
    private readonly IntPtr _ptr;

    private unsafe Pointer(void* p) => _ptr = (IntPtr)p;

    public unsafe static implicit operator Pointer(void* p) => new(p);
    public unsafe static implicit operator Pointer(IntPtr p) => new((void*)p);
    public unsafe static implicit operator Pointer(NativePointer p) => new(p);

    public override bool Equals(object? obj) => obj is Pointer pointer && Equals(pointer);
    public bool Equals(Pointer other) => _ptr.Equals(other._ptr);
    public override int GetHashCode() => _ptr.GetHashCode();
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

internal struct BufferBinding
{
    public required BufferHandle buffer;
    public required u64 offset;
    public required u64 size;
}

internal struct PipelineLayoutDescriptor
{
    public required Slice<BindGroupLayoutHandle> bind_group_layouts;
}

internal struct RenderPipelineDescriptor
{
    public required PipelineLayoutHandle layout;
    public required VertexState vertex;
    public required Opt<FragmentState> fragment;
    public required PrimitiveState primitive;
    public required Opt<DepthStencilState> depth_stencil;
    public required wgpu_MultisampleState multisample;
    public required NonZeroU32OrNone multiview;
}

internal struct DepthStencilState
{
    public required TextureFormat format;
    public required bool depth_write_enabled;
    public required wgpu_CompareFunction depth_compare;
    public required wgpu_StencilState stencil;
    public required wgpu_DepthBiasState bias;
}

internal struct wgpu_StencilState
{
    public required wgpu_StencilFaceState front;
    public required wgpu_StencilFaceState back;
    public required u32 read_mask;
    public required u32 write_mask;
}

internal struct wgpu_StencilFaceState
{
    public required wgpu_CompareFunction compare;
    public required wgpu_StencilOperation fail_op;
    public required wgpu_StencilOperation depth_fail_op;
    public required wgpu_StencilOperation pass_op;
}

internal enum wgpu_StencilOperation : u32
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

internal struct wgpu_DepthBiasState
{
    public required i32 constant;
    public required f32 slope_scale;
    public required f32 clamp;
}

internal struct wgpu_MultisampleState
{
    public required u32 count;
    public required u64 mask;
    public required bool alpha_to_coverage_enabled;

    public static wgpu_MultisampleState Default => new()
    {
        count = 1,
        mask = 0xffff_ffff_ffff_ffff,
        alpha_to_coverage_enabled = false,
    };
}

internal struct VertexState
{
    public required ShaderModuleHandle module;
    public required Slice<u8> entry_point;
    public required Slice<VertexBufferLayout> buffers;
}

internal struct FragmentState
{
    public required ShaderModuleHandle module;
    public required Slice<u8> entry_point;
    public required Slice<Opt<ColorTargetState>> targets;
}

internal struct ColorTargetState
{
    public required TextureFormat format;
    public required Opt<wgpu_BlendState> blend;
    public required wgpu_ColorWrites write_mask;
}

internal struct PrimitiveState
{
    public required wgpu_PrimitiveTopology topology;
    public required Opt<wgpu_IndexFormat> strip_index_format;
    public required wgpu_FrontFace front_face;
    public required Opt<wgpu_Face> cull_mode;
    public required wgpu_PolygonMode polygon_mode;
}

internal struct BindGroupLayoutEntry
{
    public required u32 binding;
    public required wgpu_ShaderStages visibility;
    public required BindingType ty;
    public required u32 count;
}

internal enum wgpu_Face
{
    Front = 0,
    Back = 1,
}

internal enum wgpu_ShaderStages : u32
{
    NONE = 0,
    VERTEX = 1 << 0,
    FRAGMENT = 1 << 1,
    COMPUTE = 1 << 2,
    VERTEX_FRAGMENT = VERTEX | FRAGMENT,
}

internal enum wgpu_PolygonMode : u32
{
    Fill = 0,
    Line = 1,
    Point = 2,
}

internal enum wgpu_FrontFace : u32
{
    Ccw = 0,
    Cw = 1,
}

internal enum wgpu_IndexFormat : u32
{
    Uint16 = 0,
    Uint32 = 1,
}

internal enum wgpu_PrimitiveTopology : u32
{
    PointList = 0,
    LineList = 1,
    LineStrip = 2,
    TriangleList = 3,
    TriangleStrip = 4,
}

internal enum wgpu_BlendOperation : u32
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Min = 3,
    Max = 4,
}

internal enum wgpu_BlendFactor : u32
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

internal enum wgpu_AddressMode : u32
{
    ClampToEdge = 0,
    Repeat = 1,
    MirrorRepeat = 2,
    ClampToBorder = 3,
}

internal enum wgpu_FilterMode : u32
{
    Nearest = 0,
    Linear = 1,
}

internal enum wgpu_CompareFunction : u32
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

internal enum wgpu_ColorWrites : u32
{
    RED = 1 << 0,
    GREEN = 1 << 1,
    BLUE = 1 << 2,
    ALPHA = 1 << 3,
    COLOR = RED | GREEN | BLUE,
    ALL = RED | GREEN | BLUE | ALPHA,
}

internal struct wgpu_BlendState
{
    public required wgpu_BlendComponent color;
    public required wgpu_BlendComponent alpha;

    public static wgpu_BlendState REPLACE => new()
    {
        color = wgpu_BlendComponent.REPLACE,
        alpha = wgpu_BlendComponent.REPLACE,
    };
}

internal struct wgpu_BlendComponent
{
    public required wgpu_BlendFactor src_factor;
    public required wgpu_BlendFactor dst_factor;
    public required wgpu_BlendOperation operation;

    public static wgpu_BlendComponent REPLACE => new()
    {
        src_factor = wgpu_BlendFactor.One,
        dst_factor = wgpu_BlendFactor.Zero,
        operation = wgpu_BlendOperation.Add,
    };
}

internal struct wgpu_Extent3d
{
    public required u32 width;
    public required u32 height;
    public required u32 depth_or_array_layers;
}

[Flags]
internal enum wgpu_TextureUsages : u32
{
    COPY_SRC = 1 << 0,
    COPY_DST = 1 << 1,
    TEXTURE_BINDING = 1 << 2,
    STORAGE_BINDING = 1 << 3,
    RENDER_ATTACHMENT = 1 << 4,
}

internal struct wgpu_VertexAttribute
{
    public required wgpu_VertexFormat format;
    public required u64 offset;
    public required u32 shader_location;
}

internal enum wgpu_VertexFormat : u32
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
internal enum wgpu_BufferUsages : u32
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

internal enum wgpu_Backend : u8
{
    Empty = 0,
    Vulkan = 1,
    Metal = 2,
    Dx12 = 3,
    Dx11 = 4,
    Gl = 5,
    BrowserWebGpu = 6,
}

[Flags]
internal enum wgpu_Backends : u32
{
    VULKAN = 1 << wgpu_Backend.Vulkan,
    GL = 1 << wgpu_Backend.Gl,
    METAL = 1 << wgpu_Backend.Metal,
    DX12 = 1 << wgpu_Backend.Dx12,
    DX11 = 1 << wgpu_Backend.Dx11,
    BROWSER_WEBGPU = 1 << wgpu_Backend.BrowserWebGpu,

    PRIMARY = VULKAN | METAL | DX12 | BROWSER_WEBGPU,
    SECONDARY = GL | DX11,
    ALL = VULKAN | GL | METAL | DX12 | DX11 | BROWSER_WEBGPU,
}

internal struct wgpu_ImageDataLayout
{
    public required u64 offset;
    public required u32 bytes_per_row;
    public required u32 rows_per_image;
}

internal enum wgpu_VertexStepMode : u32
{
    Vertex = 0,
    Instance = 1,
}

internal struct BindingType
{
    public required BindingTypeTag tag;
    public required Pointer payload;

    public unsafe static BindingType Buffer(BufferBindingData* payload) => new()
    {
        tag = BindingTypeTag.Buffer,
        payload = payload,
    };

    public unsafe static BindingType Texture(TextureBindingData* payload) => new()
    {
        tag = BindingTypeTag.Texture,
        payload = payload,
    };

    public unsafe static BindingType Sampler(SamplerBindingType* payload) => new()
    {
        tag = BindingTypeTag.Sampler,
        payload = payload,
    };
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
    public required wgpu_Extent3d size;
    public required u32 mip_level_count;
    public required u32 sample_count;
    public required TextureDimension dimension;
    public required TextureFormat format;
    public required wgpu_TextureUsages usage;
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
    public required wgpu_VertexStepMode step_mode;
    public required Slice<wgpu_VertexAttribute> attributes;
}

internal struct Opt<T> where T : unmanaged
{
    public required bool exists;
    public required T value;

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

internal static class Opt
{
    public static Opt<T> None<T>() where T : unmanaged => default;

    public static Opt<T> Some<T>(T value) where T : unmanaged => new()
    {
        exists = true,
        value = value,
    };
}

internal struct BufSlice
{
    public required BufferHandle buffer;
    public required RangeBoundsU64 range;

    [SetsRequiredMembers]
    public BufSlice(BufferHandle buffer, RangeBoundsU64 range)
    {
        this.buffer = buffer;
        this.range = range;
    }
}

internal unsafe readonly struct NullableRef<T> where T : unmanaged
{
    private readonly T* _p;
    public static explicit operator NullableRef<T>(T* pointer) => new(pointer);

    public NullableRef(T* pointer) => _p = pointer;

    public NullableRef<U> Cast<U>() where U : unmanaged => new((U*)_p);
}

internal struct Slice<T> where T : unmanaged
{
    public required NullableRef<T> data;
    public required nuint len;

    [SetsRequiredMembers]
    public unsafe Slice(void* data, nuint len)
    {
        this.data = new((T*)data);
        this.len = len;
    }

    [SetsRequiredMembers]
    public unsafe Slice(void* data, int len)
    {
        this.data = new((T*)data);
        this.len = checked((nuint)len);
    }

    public unsafe readonly Slice<u8> AsBytes() => new()
    {
        data = data.Cast<u8>(),
        len = len * (nuint)sizeof(T)
    };
}

internal static class Slice
{
    public static Slice<T> Empty<T>() where T : unmanaged => default;

    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(Span<T> fixedSpan) where T : unmanaged
        => FromFixedSpanUnsafe((ReadOnlySpan<T>)fixedSpan);

    public unsafe static Slice<T> FromFixedSpanUnsafe<T>(ReadOnlySpan<T> fixedSpan) where T : unmanaged
    {
        var pointer = (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(fixedSpan));
        return new Slice<T> { data = new(pointer), len = (nuint)fixedSpan.Length };
    }

    public unsafe static Slice<T> FromFixedSingleUnsafe<T>(T* item) where T : unmanaged
    {
        return new Slice<T> { data = new(item), len = 1 };
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawBufferArg
{
    public required SlotBufSlice vertex_buffer;
    public required RangeU32 vertices_range;
    public required RangeU32 instances_range;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawBufferIndexedArg
{
    public required BufSlice vertex_buffer_slice;
    public required u32 slot;
    public required BufSlice index_buffer_slice;
    public required wgpu_IndexFormat index_format;
    public required u32 index_start;
    public required u32 index_end_excluded;
    public required u32 instance_start;
    public required u32 instance_end_excluded;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawBuffersIndexedArg
{
    public required Slice<SlotBufSlice> vertex_buffers;
    public required BufSlice index_buffer_slice;
    public required wgpu_IndexFormat index_format;
    public required u32 index_start;
    public required u32 index_end_excluded;
    public required u32 instance_start;
    public required u32 instance_end_excluded;
}

internal struct SlotBufSlice
{
    public required BufSlice buffer_slice;
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

    public static RangeBoundsU64 All => default;
}

internal struct RangeBoundsU32
{
    public required u32 start;
    public required u32 end_excluded;
    public required bool has_start;
    public required bool has_end_excluded;
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
