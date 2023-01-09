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

    private static unsafe void OnStart(HostScreenHandle screen, in HostScreenInfo info)
    {
        var surfaceFormat = info.surface_format.Unwrap();
        System.Diagnostics.Debug.WriteLine(info.backend);

        {
            var desc = new PipelineLayoutDescriptor
            {
                bind_group_layouts = Slice.Empty<BindGroupLayoutHandle>(),
            };
            _pipelineLayout = EngineCore.elffy_create_pipeline_layout(screen, &desc);
        }

        _shaderModule = EngineCore.elffy_create_shader_module(screen, ShaderSource);

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
                        attributes = Slice.FromFixedSpanUnsafe<wgpu_VertexAttribute>(stackalloc wgpu_VertexAttribute[2]
                        {
                            new() { format = wgpu_VertexFormat.Float32x3, offset = 0, shader_location = 0 },
                            new() { format = wgpu_VertexFormat.Float32x3, offset = 12, shader_location = 1 },
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

    private static PipelineLayoutHandle _pipelineLayout;
    private static ShaderModuleHandle _shaderModule;
    private static RenderPipelineHandle _renderPipeline;
    private static BufferHandle _vertexBuffer;
    private static uint _vertexCount;
    private static BufferHandle _indexBuffer;
    private static uint _indexCount;
    private static wgpu_IndexFormat _indexFormat;

    private static unsafe void OnRender(HostScreenHandle screen, RenderPassHandle renderPass)
    {
        EngineCore.elffy_set_pipeline(renderPass, _renderPipeline);
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
    @location(1) color: vec3<f32>,
};

struct VertexOutput {
    @builtin(position) clip_position: vec4<f32>,
    @location(0) color: vec3<f32>,
};

@vertex
fn vs_main(vin: VertexInput) -> VertexOutput {
    var vout: VertexOutput;
    vout.color = vin.color;
    vout.clip_position = vec4<f32>(vin.position, 1.0);
    return vout;
}

@fragment
fn fs_main(fin: VertexOutput) -> @location(0) vec4<f32> {
    return vec4<f32>(fin.color, 1.0);
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
