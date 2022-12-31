#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WgpuSample;

internal class Program
{
    [STAThread]
    private static void Main(string[] args) => EngineCore.EngineStart(Start, OnRender);

    private static unsafe void Start(HostScreenHandle screen)
    {
        VertexAttribute* attrs = stackalloc VertexAttribute[2]
        {
            new() { format = VertexFormat.Float32x3, offset = 0, shader_location = 0 },
            new() { format = VertexFormat.Float32x3, offset = 12, shader_location = 1 },
        };

        var pipelineInfo = new RenderPipelineInfo
        {
            vertex = new VertexLayoutInfo
            {
                vertex_size = (ulong)sizeof(PosColorVertex),
                attributes = new(attrs, 2),
            },
            shader_source = ShaderSource
        };
        _renderPipeline = EngineCore.elffy_add_render_pipeline(screen, in pipelineInfo);

        var vertices = stackalloc PosColorVertex[3]
        {
            new()
            {
                Position = new(0.0f, 0.5f, 0.0f),
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
        };
        var contents = new Sliceffi<byte>((byte*)vertices, (nuint)(3 * sizeof(PosColorVertex)));
        _buffer = EngineCore.elffy_create_buffer_init(screen, contents, BufferUsages.VERTEX);
    }

    private static RenderPipelineHandle _renderPipeline;
    private static BufferHandle _buffer;

    private static void OnRender(HostScreenHandle screen, RenderPassHandle renderPass)
    {
        EngineCore.elffy_set_pipeline(renderPass, _renderPipeline);
        unsafe {
            EngineCore.elffy_draw_buffer(
                render_pass: renderPass,
                vertex_buffer: new()
                {
                    buffer_slice = new() { buffer = _buffer, range = RangeBoundsU64ffi.All },
                    slot = 0,
                },
                vertices_range: new RangeU32ffi(0, 3),
                instances_range: new(0, 1)
                );
        }
    }

    private unsafe static Sliceffi<byte> ShaderSource
    {
        get
        {
            var shader = """
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
    return vec4<f32>(fin.color.r, 0.0, 1.0, 1.0);
}

"""u8;
            return new()
            {
                ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(shader)),
                len = (nuint)shader.Length,
            };
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
