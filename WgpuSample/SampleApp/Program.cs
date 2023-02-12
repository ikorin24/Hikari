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
        Engine.Start(screenConfig, OnInitialized);
    }

    private static State? _state;

    private static void OnInitialized(IHostScreen screen)
    {
        screen.RedrawRequested += OnRedrawRequested;
        screen.Resized += OnResized;
        OnSetup(screen);
    }

    private static void OnRedrawRequested(IHostScreen screen, RenderPass renderPass)
    {
        var state = _state;
        if(state == null) { return; }
        renderPass.SetPipeline(state.RenderPipeline.AsValue());
        renderPass.SetBindGroup(0, state.BindGroup.AsValue());
        renderPass.SetVertexBuffer(0, state.VertexBuffer.AsValue());
        renderPass.SetVertexBuffer(1, state.InstanceBuffer.AsValue());
        renderPass.SetIndexBuffer(state.IndexBuffer.AsValue(), state.IndexFormat);
        renderPass.DrawIndexed(0, state.IndexCount, 0, 0, state.InstanceCount);
    }

    private static void OnResized(IHostScreen screen, Vector2i newSize)
    {
    }

    private static void OnSetup(IHostScreen screen)
    {
        screen.Title = "sample";

        var surfaceFormat = screen.SurfaceFormat;
        Debug.WriteLine($"backend: {screen.Backend}");
        var (pixelData, width, height) = SamplePrimitives.LoadImagePixels("happy-tree.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(width, height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        }).AsValue(out var textureOwn);
        texture.Write(0, 4, pixelData);
        var view = TextureView.Create(texture).AsValue(out var viewOwn);
        var sampler = Sampler.Create(screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Nearest,
            MipmapFilter = FilterMode.Nearest,
            AnisotropyClamp = 0,
            LodMaxClamp = 0,
            LodMinClamp = 0,
            BorderColor = null,
            Compare = null,
        }).AsValue(out var samplerOwn);

        var bindGroupLayout = BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
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
        }).AsValue(out var bindGroupLayoutOwn);
        var bindGroupOwn = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = bindGroupLayout,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, view),
                BindGroupEntry.Sampler(1, sampler),
            },
        });

        var shader = Shader.Create(screen, ShaderSource).AsValue(out var shaderOwn);

        var pipelineLayout = PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout,
            },
        }).AsValue(out var pipelineLayoutOwn);

        var renderPipelineOwn = RenderPipeline.Create(screen, new RenderPipelineDescriptor
        {
            Layout = pipelineLayout,
            Vertex = new VertexState
            {
                Module = shader,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    new VertexBufferLayout
                    {
                        ArrayStride = (ulong)Unsafe.SizeOf<Vertex>(),
                        StepMode = VertexStepMode.Vertex,
                        Attributes = new[]
                        {
                            new VertexAttribute
                            {
                                Offset = 0,
                                ShaderLocation = 0,
                                Format = VertexFormat.Float32x3,
                            },
                            new VertexAttribute
                            {
                                Offset = 12,
                                ShaderLocation = 1,
                                Format = VertexFormat.Float32x2,
                            },
                        },
                    },
                    new VertexBufferLayout
                    {
                        ArrayStride = (ulong)Unsafe.SizeOf<InstanceData>(),
                        StepMode = VertexStepMode.Instance,
                        Attributes = new[]
                        {
                            new VertexAttribute
                            {
                                Offset = 0,
                                ShaderLocation = 5,
                                Format = VertexFormat.Float32x3,
                            }
                        },
                    },
                },
            },
            Fragment = new FragmentState
            {
                Module = shader,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = surfaceFormat,
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

        var (vertices, indices) = SamplePrimitives.SampleData();
        var vertexBufferOwn = Buffer.CreateVertexBuffer<Vertex>(screen, vertices);
        var indexBufferOwn = Buffer.CreateIndexBuffer<ushort>(screen, indices);
        var indexFormat = IndexFormat.Uint16;

        ReadOnlySpan<InstanceData> instances = stackalloc InstanceData[2]
        {
            new InstanceData(new Vector3(0.5f, 0, 0)),
            new InstanceData(new Vector3(-0.5f, 0, 0)),
        };
        var instanceBuffer = Buffer.CreateVertexBuffer(screen, instances);

        _state = new State
        {
            Texture = textureOwn,
            TextureView = viewOwn,
            Sampler = samplerOwn,
            BindGroupLayout = bindGroupLayoutOwn,
            BindGroup = bindGroupOwn,
            Shader = shaderOwn,
            PipelineLayout = pipelineLayoutOwn,
            RenderPipeline = renderPipelineOwn,
            VertexBuffer = vertexBufferOwn,
            InstanceBuffer = instanceBuffer,
            InstanceCount = (uint)instances.Length,
            IndexBuffer = indexBufferOwn,
            IndexCount = (uint)indices.Length,
            IndexFormat = indexFormat,
        };
    }

    private unsafe static ReadOnlySpan<byte> ShaderSource => """
        struct Vertex {
            @location(0) position: vec3<f32>,
            @location(1) tex_coords: vec2<f32>,
        }
        struct InstanceData {
            @location(5) offset: vec3<f32>,
        }

        struct VertexOutput {
            @builtin(position) clip_position: vec4<f32>,
            @location(0) tex_coords: vec2<f32>,
        }

        @vertex fn vs_main(
            v: Vertex,
            instance: InstanceData,
        ) -> VertexOutput {
            var out: VertexOutput;
            out.clip_position = vec4<f32>(v.position + instance.offset, 1.0);
            return out;
        }

        @group(0) @binding(0) var t_diffuse: texture_2d<f32>;
        @group(0)@binding(1) var s_diffuse: sampler;

        @fragment fn fs_main(in: VertexOutput) -> @location(0) vec4<f32> {
            return textureSample(t_diffuse, s_diffuse, in.tex_coords);
        }            
        """u8;
}
internal record struct InstanceData(Vector3 Offset);

internal sealed class State : IDisposable
{
    public required Own<Texture> Texture { get; init; }
    public required Own<TextureView> TextureView { get; init; }
    public required Own<Sampler> Sampler { get; init; }
    public required Own<BindGroupLayout> BindGroupLayout { get; init; }
    public required Own<BindGroup> BindGroup { get; init; }
    public required Own<Shader> Shader { get; init; }
    public required Own<PipelineLayout> PipelineLayout { get; init; }
    public required Own<RenderPipeline> RenderPipeline { get; init; }

    public required Own<Buffer> VertexBuffer { get; init; }
    public required Own<Buffer> InstanceBuffer { get; init; }
    public required uint InstanceCount { get; init; }
    public required Own<Buffer> IndexBuffer { get; init; }
    public required uint IndexCount { get; init; }
    public required IndexFormat IndexFormat { get; init; }

    public void Dispose()
    {
        Texture.Dispose();
        TextureView.Dispose();
        Sampler.Dispose();
        BindGroupLayout.Dispose();
        BindGroup.Dispose();
        Shader.Dispose();
        PipelineLayout.Dispose();
        RenderPipeline.Dispose();
        VertexBuffer.Dispose();
        InstanceBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
