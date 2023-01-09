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
    private static BindGroupLayoutHandle _bindGroupLayout;
    private static BindGroupHandle _bindGroup;

    private static unsafe void OnStart(HostScreenHandle screen, in HostScreenInfo info)
    {
        var surfaceFormat = info.surface_format.Unwrap();
        System.Diagnostics.Debug.WriteLine(info.backend);

        // Texture
        {
            var pixels = SamplePrimitives.LoadImagePixels("pic.png", out var width, out var height);
            var desc = new TextureDescriptor
            {
                dimension = TextureDimension.D2,
                format = TextureFormat.Rgba8UnormSrgb,
                mip_level_count = 1,
                sample_count = 1,
                size = new() { width = width, height = height, depth_or_array_layers = 1, },
                usage = wgpu_TextureUsages.TEXTURE_BINDING | wgpu_TextureUsages.COPY_DST,
            };
            _texture = EngineCore.elffy_create_texture(screen, &desc);
            var writeTex = new ImageCopyTexture
            {
                texture = _texture,
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
                EngineCore.elffy_write_texture(screen, &writeTex, data, &dataLayout, &textureSize);
            }
        }

        // TextureView, Sampler
        {
            var texViewDesc = TextureViewDescriptor.Default;
            _textureView = EngineCore.elffy_create_texture_view(screen, _texture, &texViewDesc);

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
            _sampler = EngineCore.elffy_create_sampler(screen, &samplerDesc);
        }

        // BindGroupLayout
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
            _bindGroupLayout = EngineCore.elffy_create_bind_group_layout(screen, &desc);
        }

        // BindGroup
        {
            var desc = new BindGroupDescriptor
            {
                layout = _bindGroupLayout,
                entries = Slice.FromFixedSpanUnsafe((stackalloc BindGroupEntry[2]
                {
                    new()
                    {
                        binding = 0,
                        resource = BindingResource.TextureView(_textureView),
                    },
                    new()
                    {
                        binding = 1,
                        resource = BindingResource.Sampler(_sampler),
                    },
                })),
            };
            _bindGroup = EngineCore.elffy_create_bind_group(screen, &desc);
        }

        // PipelineLayout
        {
            var desc = new PipelineLayoutDescriptor
            {
                bind_group_layouts = Slice.FromFixedSpanUnsafe(stackalloc BindGroupLayoutHandle[]
                {
                    _bindGroupLayout,
                }),
            };
            _pipelineLayout = EngineCore.elffy_create_pipeline_layout(screen, &desc);
        }

        // ShaderModule
        _shaderModule = EngineCore.elffy_create_shader_module(screen, ShaderSource);

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
            _renderPipeline = EngineCore.elffy_create_render_pipeline(screen, &desc);
        }

        // Buffer (vertex, index)
        {
            var (vertices, indices) = SamplePrimitives.Rectangle();
            fixed(PosColorVertex* v = vertices) {
                _vertexBuffer = EngineCore.elffy_create_buffer_init(
                screen,
                new Slice<byte>(v, (nuint)sizeof(PosColorVertex) * (nuint)vertices.Length),
                wgpu_BufferUsages.VERTEX);
                _vertexCount = (uint)vertices.Length;
            }
            fixed(uint* i = indices) {
                _indexBuffer = EngineCore.elffy_create_buffer_init(
                screen,
                new Slice<byte>(i, (nuint)sizeof(uint) * (nuint)indices.Length),
                wgpu_BufferUsages.INDEX);
            }
            _indexCount = (uint)indices.Length;
            _indexFormat = wgpu_IndexFormat.Uint32;
        }
    }

    private static unsafe void OnRender(HostScreenHandle screen, RenderPassHandle renderPass)
    {
        EngineCore.elffy_set_pipeline(renderPass, _renderPipeline);
        EngineCore.elffy_set_bind_group(renderPass, 0, _bindGroup);

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
        EngineCore.elffy_draw_buffer_indexed(renderPass, &arg);
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

    @group(0) @binding(0)
    var tex: texture_2d<f32>;
    @group(0)@binding(1)
    var s: sampler;
    
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
        //return vec4<f32>(fin.color, 1.0);
        //return vec4<f32>(fin.uv, 0.0, 1.0);
        return textureSample(tex, s, fin.uv);
    }
    
    """u8);
        }
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
