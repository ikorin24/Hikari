#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        var screenConfig = new HostScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static void OnResized(IHostScreen screen, Vector2i newSize)
    {
    }

    private static void OnInitialized(IHostScreen screen)
    {
        screen.Resized += OnResized;
        screen.Title = "sample";

        var shader = Shader.Create(
            screen,
            new BindGroupLayoutDescriptor
            {
                Entries = new[]
                {
                    BindGroupLayoutEntry.Texture(
                        binding: 0,
                        visibility: ShaderStages.Fragment,
                        type: new TextureBindingData
                        {
                            Multisampled = false,
                            ViewDimension = TextureViewDimension.D2,
                            SampleType = TextureSampleType.FloatFilterable,
                        },
                        count: 0),
                    BindGroupLayoutEntry.Sampler(
                        binding: 1,
                        visibility: ShaderStages.Fragment,
                        type: SamplerBindingType.Filtering,
                        count: 0),
                },
            },
            ShaderSource)
            .AsValue(out var shaderOwn);
        var layer = ObjectLayer.Create(shaderOwn, new RenderPipelineDescriptor
        {
            Layout = shader.PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.Module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    new VertexBufferLayout
                    {
                        ArrayStride = MyVertex.TypeSize,
                        StepMode = VertexStepMode.Vertex,
                        Attributes = new[]
                        {
                            new VertexAttr
                            {
                                Offset = 0,
                                ShaderLocation = 0,
                                Format = VertexFormat.Float32x3,
                            },
                            new VertexAttr
                            {
                                Offset = 12,
                                ShaderLocation = 1,
                                Format = VertexFormat.Float32x2,
                            },
                        },
                    },
                    //new VertexBufferLayout
                    //{
                    //    ArrayStride = (ulong)Unsafe.SizeOf<InstanceData>(),
                    //    StepMode = VertexStepMode.Instance,
                    //    Attributes = new[]
                    //    {
                    //        new VertexAttribute
                    //        {
                    //            Offset = 0,
                    //            ShaderLocation = 5,
                    //            Format = VertexFormat.Float32x3,
                    //        }
                    //    },
                    //},
                },
            },
            Fragment = new FragmentState
            {
                Module = shader.Module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = screen.SurfaceFormat,
                        Blend = BlendState.Replace,
                        WriteMask = ColorWrites.All,
                    },
                },
            },
            Primitive = new PrimitiveState
            {
                Topology = PrimitiveTopology.TriangleList,
                StripIndexFormat = null,
                FrontFace = FrontFace.Ccw,
                CullMode = Face.Back,
                PolygonMode = PolygonMode.Fill,
            },
            DepthStencil = new DepthStencilState
            {
                Format = screen.DepthTexture.Format,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.Less,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });

        var (pixelData, width, height) = SamplePrimitives.LoadImagePixels("pic.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(width, height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        });
        texture.AsValue().Write(0, pixelData.AsSpan());
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Linear, FilterMode.Nearest);
        var mesh = SamplePrimitives.SampleMesh(screen);
        var model = Model3D.Create(layer, mesh, new BindGroupDescriptor
        {
            Layout = layer.Shader.GetBindGroupLayout(0),
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, texture.AsValue().View),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
            },
        }, texture, sampler);
    }

    private unsafe static ReadOnlySpan<byte> ShaderSource => """
        struct Vertex {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }

        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }

        @vertex fn vs_main(
            v: Vertex,
        ) -> V2F {
            return V2F
            (
                vec4(v.pos, 1.0),
                v.uv,
            );
        }

        @group(0) @binding(0) var t_diffuse: texture_2d<f32>;
        @group(0) @binding(1) var s_diffuse: sampler;

        @fragment fn fs_main(in: V2F) -> @location(0) vec4<f32> {
            return textureSample(t_diffuse, s_diffuse, in.uv);
        }            
        """u8;
}
//internal record struct InstanceData(Vector3 Offset);
