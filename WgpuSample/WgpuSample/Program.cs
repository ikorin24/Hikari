#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WgpuSample.Bind;

namespace WgpuSample;

internal class Program
{
    [STAThread]
    private static void Main(string[] args) => EngineCore.EngineStart(new()
    {
        OnStart = OnStart,
        OnRender = OnRender,
    });

    private static PipelineLayoutHandle _pipelineLayout;
    private static ShaderModuleHandle _shaderModule;
    private static RenderPipelineHandle _renderPipeline;

    private static BufferHandle _vertexBuffer;
    private static uint _vertexCount;
    private static BufferHandle _indexBuffer;
    private static uint _indexCount;
    private static wgpu_IndexFormat _indexFormat;

    private static TextureHandle _texture;
    private static TextureViewHandle _textureView;
    private static SamplerHandle _sampler;
    private static BindGroupLayoutHandle _textureBindGroupLayout;
    private static BindGroupHandle _textureBindGroup;

    private static BufferHandle _uniformBuffer;
    private static BindGroupLayoutHandle _uniformBindGroupLayout;
    private static BindGroupHandle _uniformBindGroup;

    private static unsafe void OnStart(HostScreenHandle screen, in HostScreenInfo info)
    {
        var surfaceFormat = info.surface_format.Unwrap();
        System.Diagnostics.Debug.WriteLine(info.backend);

        // Texture
        _texture = HostScreenInitializer.CreateTexture(screen, "pic.png");

        // TextureView, Sampler
        (_textureView, _sampler) = HostScreenInitializer.CreateTextureViewSampler(screen, _texture);

        // BindGroupLayout
        _textureBindGroupLayout = HostScreenInitializer.CreateTextureBindGroupLayout(screen);

        // BindGroup
        _textureBindGroup = HostScreenInitializer.CreateTextureBindGroup(screen, _textureBindGroupLayout, _textureView, _sampler);

        // Buffer (uniform)
        _uniformBuffer = HostScreenInitializer.CreateUniformBuffer(screen, stackalloc Vec4[] { new Vec4(1, 0, 0, 1) });

        // BindGroupLayout (uniform)
        _uniformBindGroupLayout = HostScreenInitializer.CreateUniformBindGroupLayout(screen);

        // BindGroup (uniform)
        _uniformBindGroup = HostScreenInitializer.CreateUniformBindGroup(screen, _uniformBindGroupLayout, _uniformBuffer);

        // PipelineLayout
        {
            var desc = new PipelineLayoutDescriptor
            {
                bind_group_layouts = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutHandle[]
                {
                    _textureBindGroupLayout,
                    _uniformBindGroupLayout,
                }),
            };
            _pipelineLayout = EngineCore.CreatePipelineLayout(screen, &desc);
        }

        // ShaderModule
        _shaderModule = EngineCore.CreateShaderModule(screen, ShaderSource);

        // RenderPipeline
        {
            var desc = new RenderPipelineDescription
            {
                layout = _pipelineLayout,
                vertex = new()
                {
                    module = _shaderModule,
                    entry_point = Slice.FromFixedSpanUnsafe("vs_main"u8),
                    inputs = Slice.FromFixedSingleUnsafe(UnsafeEx.StackPointer(new VertexBufferLayout()
                    {
                        vertex_size = (ulong)sizeof(PosColorVertex),
                        attributes = Slice.FromFixedSpanUnsafe(stackalloc wgpu_VertexAttribute[3]
                        {
                            new() { format = wgpu_VertexFormat.Float32x3, offset = 0, shader_location = 0 },
                            new() { format = wgpu_VertexFormat.Float32x2, offset = 12, shader_location = 1 },
                            new() { format = wgpu_VertexFormat.Float32x3, offset = 20, shader_location = 2 },
                        }),
                    })),
                },
                fragment = Opt.Some(new FragmentState
                {
                    module = _shaderModule,
                    entry_point = Slice.FromFixedSpanUnsafe("fs_main"u8),
                    targets = Slice.FromFixedSingleUnsafe(UnsafeEx.StackPointer(
                        Opt.Some(new ColorTargetState()
                        {
                            format = surfaceFormat,
                            blend = Opt.Some(wgpu_BlendState.REPLACE),
                            write_mask = wgpu_ColorWrites.ALL,
                        }))),
                }),
                primitive = new()
                {
                    topology = wgpu_PrimitiveTopology.TriangleList,
                    strip_index_format = Opt.None<wgpu_IndexFormat>(),
                    front_face = wgpu_FrontFace.Ccw,
                    cull_mode = Opt.Some(wgpu_Face.Back),
                    polygon_mode = wgpu_PolygonMode.Fill,
                },
            };
            _renderPipeline = EngineCore.CreateRenderPipeline(screen, &desc);
        }

        // Buffer (vertex, index)
        {
            var (vertices, indices) = SamplePrimitives.Rectangle();
            (_vertexBuffer, _vertexCount, _indexBuffer, _indexCount, _indexFormat) =
                HostScreenInitializer.CreateVertexIndexBuffer(
                    screen,
                    (ReadOnlySpan<PosColorVertex>)vertices,
                    indices);
        }
    }

    private static unsafe void OnRender(HostScreenHandle screen, RenderPassRef renderPass)
    {
        EngineCore.SetPipeline(renderPass, _renderPipeline);
        EngineCore.SetBindGroup(renderPass, 0, _textureBindGroup);
        EngineCore.SetBindGroup(renderPass, 1, _uniformBindGroup);

        var arg = new DrawBufferIndexedArg
        {
            vertex_buffer_slice = new()
            {
                buffer = _vertexBuffer,
                range = RangeBoundsU64.All,
            },
            slot = 0,
            index_buffer_slice = new()
            {
                buffer = _indexBuffer,
                range = RangeBoundsU64.All,
            },
            index_format = _indexFormat,
            index_start = 0,
            index_end_excluded = _indexCount,
            instance_start = 0,
            instance_end_excluded = 1,
        };
        EngineCore.DrawBufferIndexed(renderPass, &arg);
    }

    private unsafe static Slice<byte> ShaderSource
    {
        get
        {
            return Slice.FromFixedSpanUnsafe("""
            struct VertexInput {
                @location(0) position: vec3<f32>,
                @location(1) uv: vec2<f32>,
                @location(2) color: vec3<f32>,
            };
            
            struct VertexOutput {
                @builtin(position) clip_position: vec4<f32>,
                @location(0) color: vec3<f32>,
                @location(1) uv: vec2<f32>,
            };

            @group(0) @binding(0) var tex: texture_2d<f32>;
            @group(0) @binding(1) var s: sampler;
            @group(1) @binding(0) var<uniform> data: vec4<f32>;

            
            @vertex
            fn vs_main(vin: VertexInput) -> VertexOutput {
                var vout: VertexOutput;
                vout.color = vin.color;
                vout.uv = vin.uv;
                vout.clip_position = vec4<f32>(vin.position, 1.0);
                return vout;
            }
            
            @fragment
            fn fs_main(fin: VertexOutput) -> @location(0) vec4<f32> {
                var tex_color: vec4<f32> = textureSample(tex, s, fin.uv);
                return (tex_color + data) * 0.5;
            }
            
            """u8);
        }
    }
}

internal static class HostScreenInitializer
{
    public unsafe static TextureHandle CreateTexture(HostScreenHandle screen, string filepath)
    {
        var pixels = SamplePrimitives.LoadImagePixels(filepath, out var width, out var height);
        var desc = new TextureDescriptor
        {
            dimension = TextureDimension.D2,
            format = TextureFormat.Rgba8UnormSrgb,
            mip_level_count = 1,
            sample_count = 1,
            size = new() { width = width, height = height, depth_or_array_layers = 1, },
            usage = wgpu_TextureUsages.TEXTURE_BINDING | wgpu_TextureUsages.COPY_DST,
        };
        var texture = EngineCore.CreateTexture(screen, &desc);
        var writeTex = new ImageCopyTexture
        {
            texture = texture,
            mip_level = 0,
            aspect = TextureAspect.All,
            origin_x = 0,
            origin_y = 0,
            origin_z = 0,
        };
        var dataLayout = new wgpu_ImageDataLayout
        {
            offset = 0,
            bytes_per_row = 4 * width,
            rows_per_image = height,
        };
        var textureSize = new wgpu_Extent3d
        {
            width = width,
            height = height,
            depth_or_array_layers = 1,
        };
        fixed(byte* p = pixels) {
            var data = new Slice<byte> { data = new(p), len = (nuint)pixels.Length };
            EngineCore.WriteTexture(screen, &writeTex, data, &dataLayout, &textureSize);
        }
        return texture;
    }

    public unsafe static (TextureViewHandle TextureView, SamplerHandle Sampler) CreateTextureViewSampler(HostScreenHandle screen, TextureHandle texture)
    {
        var texViewDesc = TextureViewDescriptor.Default;
        var textureView = EngineCore.CreateTextureView(screen, texture, &texViewDesc);

        var samplerDesc = new SamplerDescriptor
        {
            address_mode_u = wgpu_AddressMode.ClampToEdge,
            address_mode_v = wgpu_AddressMode.ClampToEdge,
            address_mode_w = wgpu_AddressMode.ClampToEdge,
            mag_filter = wgpu_FilterMode.Linear,
            min_filter = wgpu_FilterMode.Nearest,
            mipmap_filter = wgpu_FilterMode.Nearest,
            anisotropy_clamp = 0,
            lod_max_clamp = 0,
            lod_min_clamp = 0,
            border_color = Opt.None<SamplerBorderColor>(),
            compare = Opt.None<wgpu_CompareFunction>(),
        };
        var sampler = EngineCore.CreateSampler(screen, &samplerDesc);

        return (TextureView: textureView, Sampler: sampler);
    }

    public unsafe static BindGroupLayoutHandle CreateTextureBindGroupLayout(HostScreenHandle screen)
    {
        var desc = new BindGroupLayoutDescriptor
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[2]
                        {
                new()
                {
                    binding = 0,
                    visibility = wgpu_ShaderStages.FRAGMENT,
                    ty = BindingType.Texture(UnsafeEx.StackPointer(new TextureBindingData
                    {
                        multisampled = false,
                        view_dimension = TextureViewDimension.D2,
                        sample_type = TextureSampleType.FloatFilterable,
                    })),
                    count = 0,
                },
                new()
                {
                    binding = 1,
                    visibility = wgpu_ShaderStages.FRAGMENT,
                    ty = BindingType.Sampler(UnsafeEx.StackPointer(SamplerBindingType.Filtering)),
                    count = 0,
                },
            }),
        };
        return EngineCore.CreateBindGroupLayout(screen, &desc);
    }

    public unsafe static BindGroupHandle CreateTextureBindGroup(
        HostScreenHandle screen,
        BindGroupLayoutHandle bindGroupLayout,
        TextureViewHandle textureView,
        SamplerHandle sampler)
    {
        var desc = new BindGroupDescriptor
        {
            layout = bindGroupLayout,
            entries = Slice.FromFixedSpanUnsafe((stackalloc BindGroupEntry[2]
            {
                new()
                {
                    binding = 0,
                    resource = BindingResource.TextureView(textureView),
                },
                new()
                {
                    binding = 1,
                    resource = BindingResource.Sampler(sampler),
                },
            })),
        };
        return EngineCore.CreateBindGroup(screen, &desc);
    }

    public unsafe static BufferHandle CreateUniformBuffer<T>(HostScreenHandle screen, Span<T> data) where T : unmanaged
        => CreateUniformBuffer(screen, (ReadOnlySpan<T>)data);

    public unsafe static BufferHandle CreateUniformBuffer<T>(HostScreenHandle screen, ReadOnlySpan<T> data) where T : unmanaged
    {
        var bytes = data.AsBytes();
        fixed(byte* p = bytes) {
            var contents = new Slice<byte>(p, (nuint)bytes.Length);
            var usage = wgpu_BufferUsages.UNIFORM | wgpu_BufferUsages.COPY_DST;
            return EngineCore.CreateBufferInit(screen, contents, usage);
        }
    }

    public unsafe static BindGroupLayoutHandle CreateUniformBindGroupLayout(HostScreenHandle screen)
    {
        var desc = new BindGroupLayoutDescriptor
        {
            entries = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutEntry[1]
                {
                new()
                {
                    binding = 0,
                    visibility = wgpu_ShaderStages.VERTEX_FRAGMENT,
                    ty = BindingType.Buffer(UnsafeEx.StackPointer(new BufferBindingData
                    {
                        ty = BufferBindingType.Uniform,
                        has_dynamic_offset = false,
                        min_binding_size = 0,
                    })),
                    count = 0,
                },
            }),
        };
        return EngineCore.CreateBindGroupLayout(screen, &desc);
    }

    public unsafe static BindGroupHandle CreateUniformBindGroup(
        HostScreenHandle screen,
        BindGroupLayoutHandle layout,
        BufferHandle buffer)
    {
        var desc = new BindGroupDescriptor
        {
            layout = layout,
            entries = Slice.FromFixedSpanUnsafe((stackalloc BindGroupEntry[1]
            {
                new()
                {
                    binding = 0,
                    resource = BindingResource.Buffer(UnsafeEx.StackPointer(
                        buffer.AsEntriesBinding()
                    )),
                },
            })),
        };
        return EngineCore.CreateBindGroup(screen, &desc);
    }

    public unsafe static (
        BufferHandle VertexBuffer,
        uint VertexCount,
        BufferHandle IndexBuffer,
        uint IndexCount,
        wgpu_IndexFormat IndexFormat
    ) CreateVertexIndexBuffer<TVertex>(
            HostScreenHandle screen,
            ReadOnlySpan<TVertex> vertices,
            ReadOnlySpan<uint> indices) where TVertex : unmanaged
    {
        BufferHandle vertexBuffer;
        uint vertexCount;
        fixed(TVertex* v = vertices) {
            nuint bytelen = (nuint)sizeof(TVertex) * (nuint)vertices.Length;
            var contents = new Slice<byte>(v, bytelen);
            vertexBuffer = EngineCore.CreateBufferInit(screen, contents, wgpu_BufferUsages.VERTEX);
            vertexCount = (uint)vertices.Length;
        }

        BufferHandle indexBuffer;
        uint indexCount;
        const wgpu_IndexFormat indexFormat = wgpu_IndexFormat.Uint32;
        fixed(uint* i = indices) {
            nuint bytelen = (nuint)sizeof(uint) * (nuint)indices.Length;
            var contents = new Slice<byte>(i, bytelen);
            indexBuffer = EngineCore.CreateBufferInit(screen, contents, wgpu_BufferUsages.INDEX);
            indexCount = (uint)indices.Length;
        }

        return (
            VertexBuffer: vertexBuffer,
            VertexCount: vertexCount,
            IndexBuffer: indexBuffer,
            IndexCount: indexCount,
            IndexFormat: indexFormat
        );
    }
}


internal unsafe static class UnsafeEx
{
    public static T* StackPointer<T>(in T x) where T : unmanaged
    {
        return (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in x));
    }
}

internal static class SpanExtensions
{
    public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) where T : unmanaged
    {
        return (ReadOnlySpan<T>)span;
    }

    public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> span) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(span);
    }
}
