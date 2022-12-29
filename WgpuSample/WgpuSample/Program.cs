using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WgpuSample;

internal class Program
{
    [STAThread]
    private static void Main(string[] args) => NativeApi.elffy_engine_start(Start);

    private static unsafe void Start(HostScreenHandle screen)
    {
        VertexAttribute* attrs = stackalloc VertexAttribute[2]
        {
            new() { format = VertexFormat.Float32x3, offset = 0, shader_location = 0 },
            new() { format = VertexFormat.Float32x4, offset = 24, shader_location = 1 },
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
        var renderPipeline = NativeApi.elffy_add_render_pipeline(screen, in pipelineInfo);
    }

    private unsafe static RustSlice<byte> ShaderSource
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
    return vec4<f32>(fin.color, 1.0);
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

public struct PosColorVertex
{
    public (float X, float Y, float Z) Position;
    public (float R, float G, float B, float A) Color;
}
