#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Bind;

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

    private static State2? _state2;

    private unsafe static void OnRedrawRequested(Ref<CE.HostScreen> screenRef)
    {
        if(screenRef.ScreenBeginCommand(out var commandEncoder, out var surfaceTexture, out var surfaceTextureView) == false) {
            return;
        }
        var state = _state2;
        if(state == null) { return; }
        {
            CE.RenderPassDescriptor desc;
            {
                const int ColorAttachmentCount = 1;
                var colorAttachments = stackalloc Opt<CE.RenderPassColorAttachment>[ColorAttachmentCount]
                {
                    new(new CE.RenderPassColorAttachment
                    {
                        view = surfaceTextureView,
                        clear = new Wgpu.Color(0, 0, 0, 0),
                    }),
                };
                desc = new CE.RenderPassDescriptor
                {
                    color_attachments_clear = new() { data = colorAttachments, len = ColorAttachmentCount },
                    depth_stencil_attachment_clear = new(new CE.RenderPassDepthStencilAttachment
                    {
                        view = state.DepthTextureView.NativeRef,
                        depth_clear = Opt<float>.Some(1f),
                        stencil_clear = Opt<uint>.None,
                    }),
                };
            }

            var renderPass = commandEncoder.AsMut().CreateRenderPass(desc);
            try {
                var r = renderPass.AsMut();
                r.SetPipeline(state.RenderPipeline.NativeRef);
                r.SetBindGroup(0, state.BindGroup.NativeRef);
                r.SetVertexBuffer(0, state.VertexBuffer.NativeRef.AsSlice());
                r.SetVertexBuffer(1, state.InstanceBuffer.NativeRef.AsSlice());
                r.SetIndexBuffer(state.IndexBuffer.NativeRef.AsSlice(), state.IndexFormat.MapOrThrow());
                r.DrawIndexed(0..state.IndexCount, 0, 0..state.InstanceCount);
            }
            finally {
                renderPass.DestroyRenderPass();
            }
        }
        screenRef.ScreenFinishCommand(commandEncoder, surfaceTexture, surfaceTextureView);

    }

    private static void OnInitialized(IHostScreen screen)
    {
        screen.RedrawRequested += screen => OnRedrawRequested(screen.AsRefChecked());
        screen.Resized += (screen, size) => OnResized(screen, size);
        OnSetup(screen);
    }

    private static void OnResized(IHostScreen screen, in Vector2i newSize)
    {
        var state = _state2;
        if(state == null) { return; }
        if(newSize.X == 0 || newSize.Y == 0) {
            return;
        }
        screen.AsRefUnchecked().ScreenResizeSurface((u32)newSize.X, (u32)newSize.Y);
        var (depthTexture, depthView, depthSampler) = CreateDepth(screen, newSize);
        state.DepthTexture.Dispose();
        state.DepthTextureView.Dispose();
        state.DepthSampler.Dispose();
        state.DepthTexture = depthTexture;
        state.DepthTextureView = depthView;
        state.DepthSampler = depthSampler;
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
        });
        texture.Write(0, 4, pixelData);
        var view = TextureView.Create(texture);
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
        });

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
        });
        var bindGroup = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = bindGroupLayout,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, view),
                BindGroupEntry.Sampler(1, sampler),
            },
        });

        var shader = Shader.Create(screen, ShaderSource2);

        var pipelineLayout = PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout,
            },
        });

        var screenSize = screen.ClientSize;
        var (depthTexture, depthView, depthSampler) = CreateDepth(screen, screenSize);

        var renderPipeline = RenderPipeline.Create(screen, new RenderPipelineDescriptor
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
                Format = depthTexture.Format,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.Less,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });

        var (vertices, indices) = SamplePrimitives.SampleData();
        var vb = Buffer.CreateVertexBuffer<Vertex>(screen, vertices);
        var ib = Buffer.CreateIndexBuffer<ushort>(screen, indices);
        var indexFormat = IndexFormat.Uint16;

        ReadOnlySpan<InstanceData> instances = stackalloc InstanceData[2]
        {
            new InstanceData(new Vector3(0.5f, 0, 0)),
            new InstanceData(new Vector3(-0.5f, 0, 0)),
        };
        var instanceBuffer = Buffer.CreateVertexBuffer(screen, instances);

        _state2 = new State2
        {
            Texture = texture,
            TextureView = view,
            Sampler = sampler,
            BindGroupLayout = bindGroupLayout,
            BindGroup = bindGroup,
            Shader = shader,
            DepthTexture = depthTexture,
            DepthTextureView = depthView,
            DepthSampler = depthSampler,
            PipelineLayout = pipelineLayout,
            RenderPipeline = renderPipeline,
            VertexBuffer = vb,
            InstanceBuffer = instanceBuffer,
            InstanceCount = instances.Length,
            IndexBuffer = ib,
            IndexCount = indices.Length,
            IndexFormat = indexFormat,
        };
    }

    private static (Texture Depth, TextureView DepthView, Sampler DepthSampler) CreateDepth(IHostScreen screen, Vector2i size)
    {
        var depth = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(size.X, size.Y, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Depth32Float,
            Usage = TextureUsages.RenderAttachment | TextureUsages.TextureBinding,
        });
        var depthView = TextureView.Create(depth);
        var depthSampler = Sampler.Create(screen, new SamplerDescriptor
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Nearest,
            Compare = CompareFunction.LessEqual,
            LodMinClamp = 0,
            LodMaxClamp = 100,
            AnisotropyClamp = 0,
            BorderColor = null,
        });
        return (depth, depthView, depthSampler);
    }

    private unsafe static ReadOnlySpan<byte> ShaderSource2 => """
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

internal sealed class State2 : IDisposable
{
    public required Texture Texture { get; init; }
    public required TextureView TextureView { get; init; }
    public required Sampler Sampler { get; init; }
    public required BindGroupLayout BindGroupLayout { get; init; }
    public required BindGroup BindGroup { get; init; }
    public required Shader Shader { get; init; }
    public required PipelineLayout PipelineLayout { get; init; }
    public required RenderPipeline RenderPipeline { get; init; }

    public required Buffer VertexBuffer { get; init; }
    public required Buffer InstanceBuffer { get; init; }
    public required int InstanceCount { get; init; }
    public required Buffer IndexBuffer { get; init; }
    public required int IndexCount { get; init; }
    public required IndexFormat IndexFormat { get; init; }


    public required Texture DepthTexture { get; set; }
    public required TextureView DepthTextureView { get; set; }
    public required Sampler DepthSampler { get; set; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
