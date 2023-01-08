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

    private static unsafe void OnStart(HostScreenHandle screen)
    {
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
                }
            };
            _renderPipeline = EngineCore.elffy_create_render_pipeline(screen, &desc);
        }

        {
            const int VertexCount = 4;
            var vertices = (stackalloc PosColorVertex[VertexCount]
            {
                new()
                {
                    Position = new(-0.5f, 0.5f, 0.0f),
                    Color = new(1.0f, 0.0f, 0.0f),
                },
                new()
                {
                    Position = new(-0.5f, -0.5f, 0.0f),
                    Color = new(0.0f, 1.0f, 0.0f),
                },
                new()
                {
                    Position = new(0.5f, -0.5f, 0.0f),
                    Color = new(0.0f, 0.0f, 1.0f),
                },
                new()
                {
                    Position = new(0.5f, 0.5f, 0.0f),
                    Color = new(0.0f, 0.0f, 1.0f),
                },
            }).AsReadOnly().AsBytes();
            _vertexBuffer = EngineCore.elffy_create_buffer_init(
                screen,
                Slice.FromFixedSpanUnsafe(vertices),
                wgpu_BufferUsages.VERTEX);
            _vertexCount = VertexCount;

            const int IndexCount = 6;
            var indices = (stackalloc uint[IndexCount] { 0, 1, 2, 2, 3, 0 }).AsReadOnly().AsBytes();
            _indexBuffer = EngineCore.elffy_create_buffer_init(
                screen,
                Slice.FromFixedSpanUnsafe(indices),
                wgpu_BufferUsages.INDEX);
            _indexCount = IndexCount;
            _indexFormat = wgpu_IndexFormat.Uint32;
        }
    }

    //private static unsafe void OnStart(HostScreenHandle screen)
    //{
    //    //VertexAttribute* attrs = stackalloc VertexAttribute[2]
    //    //{
    //    //    new() { format = VertexFormat.Float32x3, offset = 0, shader_location = 0 },
    //    //    new() { format = VertexFormat.Float32x3, offset = 12, shader_location = 1 },
    //    //};

    //    //var pipelineInfo = new RenderPipelineInfo
    //    //{
    //    //    vertex = new VertexLayoutInfo
    //    //    {
    //    //        vertex_size = (ulong)sizeof(PosColorVertex),
    //    //        attributes = new(attrs, 2),
    //    //    },
    //    //    shader_source = ShaderSource
    //    //};
    //    //_renderPipeline = EngineCore.elffy_add_render_pipeline(screen, in pipelineInfo);
    //    var desc = new RenderPipelineDescription
    //    {
    //        //layout = pip
    //    };
    //    EngineCore.elffy_create_render_pipeline(screen, &desc);

    //    ReadOnlySpan<PosColorVertex> vertices = stackalloc PosColorVertex[4]
    //    {
    //        new()
    //        {
    //            Position = new(-0.5f, 0.5f, 0.0f),
    //            Color = new(1.0f, 0.0f, 0.0f),
    //        },
    //        new()
    //        {
    //            Position = new(-0.5f, -0.5f, 0.0f),
    //            Color = new(0.0f, 1.0f, 0.0f),
    //        },
    //        new()
    //        {
    //            Position = new(0.5f, -0.5f, 0.0f),
    //            Color = new(0.0f, 0.0f, 1.0f),
    //        },
    //        new()
    //        {
    //            Position = new(0.5f, 0.5f, 0.0f),
    //            Color = new(0.0f, 0.0f, 1.0f),
    //        },
    //    };
    //    ReadOnlySpan<uint> indices = stackalloc uint[6] { 0, 1, 2, 2, 3, 0 };
    //    fixed(uint* i = indices)
    //    fixed(PosColorVertex* v = vertices) {
    //        //var vContents = new Sliceffi<byte>((byte*)v, (nuint)(vertices.Length * sizeof(PosColorVertex)));
    //        //_vertexBuffer = EngineCore.elffy_create_buffer_init(screen, vContents, BufferUsages.VERTEX);
    //        //_vertexCount = (uint)vertices.Length;

    //        //var iContents = new Sliceffi<byte>((byte*)i, (nuint)(indices.Length * sizeof(uint)));
    //        //_indexBuffer = EngineCore.elffy_create_buffer_init(screen, iContents, BufferUsages.INDEX);
    //        //_indexCount = (uint)indices.Length;
    //    }
    //}

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
        return;
        EngineCore.elffy_draw_buffer_indexed(
            renderPass,
            UnsafeEx.StackPointer<DrawBufferIndexedArg>(new()
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
            }));



        //EngineCore.elffy_set_pipeline(renderPass, _renderPipeline);
        //EngineCore.elffy_draw_buffer(
        //    render_pass: renderPass,
        //    vertex_buffer: new()
        //    {
        //        buffer_slice = new() { buffer = _vertexBuffer, range = RangeBoundsU64ffi.All },
        //        slot = 0,
        //    },
        //    vertices_range: new RangeU32ffi(0, _vertexCount),
        //    instances_range: new(0, 1)
        //);
        //EngineCore.elffy_draw_buffer_indexed(
        //    render_pass: renderPass,
        //    vertex_buffer: new()
        //    {
        //        buffer_slice = new() { buffer = _vertexBuffer, range = RangeBoundsU64ffi.All },
        //        slot = 0,
        //    },
        //    index_buffer: new()
        //    {
        //        buffer_slice = new() { buffer = _indexBuffer, range = RangeBoundsU64ffi.All },
        //        format = IndexFormat.Uint32,
        //    },
        //    indices_range: new RangeU32ffi(0, _indexCount),
        //    instances_range: new(0, 1)
        //);
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

[StructLayout(LayoutKind.Sequential)]
public struct PosColorVertex
{
    public Vec3 Position;
    public Color3 Color;
}

[StructLayout(LayoutKind.Sequential)]
public record struct Vec3(float X, float Y, float Z);

[StructLayout(LayoutKind.Sequential)]
public record struct Color3(float R, float G, float B);


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
