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

    private static State? _state;
    private static RenderableObj? _renderable;

    private static void OnInitialized(IHostScreen screen)
    {
        //screen.RedrawRequested += OnRedrawRequested;
        //screen.Resized += OnResized;
        //OnSetup(screen);

        screen.RedrawRequested += OnRedrawRequested2;
        screen.Resized += OnResized;
        OnSetup2(screen);
    }

    private static void OnRedrawRequested(IHostScreen screen, CommandEncoder encoder)
    {
        var state = _state;
        if(state == null) { return; }
        using var renderPassOwn = encoder.CreateSurfaceRenderPass(screen.SurfaceTextureView, screen.DepthTextureView);
        var renderPass = renderPassOwn.AsValue();


        if(screen.Keyboard.IsDown(Keys.Escape)) {
            screen.Close();
        }

        renderPass.SetPipeline(state.RenderPipeline.AsValue());
        renderPass.SetBindGroup(0, state.BindGroup.AsValue());
        renderPass.SetVertexBuffer(0, state.VertexBuffer.AsValue());
        //renderPass.SetVertexBuffer(1, state.InstanceBuffer.AsValue());
        renderPass.SetIndexBuffer(state.IndexBuffer.AsValue(), state.IndexFormat);
        //renderPass.DrawIndexed(0, state.IndexCount, 0, 0, state.InstanceCount);
        renderPass.DrawIndexed(0, state.IndexCount, 0, 0, 1);
    }

    private static void OnResized(IHostScreen screen, Vector2i newSize)
    {
    }

    private static void OnRedrawRequested2(IHostScreen screen, CommandEncoder encoder)
    {
        using var renderPassOwn = encoder.CreateSurfaceRenderPass(screen.SurfaceTextureView, screen.DepthTextureView);
        var renderPass = renderPassOwn.AsValue();

        if(screen.Keyboard.IsDown(Keys.Escape)) {
            screen.Close();
        }

        // TODO:
        screen.RenderOperations.Render(renderPass);
        _renderable?.Render(renderPass);
    }

    private static void OnSetup2(IHostScreen screen)
    {
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
        var objectLayer = ObjectLayer.Create(shaderOwn, new RenderPipelineDescriptor
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

        var (vertices, indices, indexFormat) = SamplePrimitives.SampleData();
        var vertexBufferOwn = Buffer.CreateVertexBuffer<MyVertex>(screen, vertices);
        var indexBufferOwn = Buffer.CreateIndexBuffer<ushort>(screen, indices);
        var (pixelData, width, height) = SamplePrimitives.LoadImagePixels("pic.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(width, height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        }).AsValue(out var textureOwn);
        texture.Write(0, (uint)Unsafe.SizeOf<ColorByte>(), pixelData.AsSpan());
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

        var bindGroupOwn = BindGroup.Create(screen, new BindGroupDescriptor
        {
            Layout = objectLayer.Shader.GetBindGroupLayout(0), //bindGroupLayout,
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, view),
                BindGroupEntry.Sampler(1, sampler),
            },
        });

        _renderable = new RenderableObj
        {
            Texture = textureOwn,
            TextureView = viewOwn,
            Sampler = samplerOwn,
            BindGroup = bindGroupOwn,
            VertexBuffer = vertexBufferOwn,
            IndexBuffer = indexBufferOwn,
            IndexCount = (uint)indices.Length,
            IndexFormat = indexFormat,
        };
    }

    private static void Hoge(IHostScreen screen)
    {
        var shader = Shader.Create(screen, new BindGroupLayoutDescriptor
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
        }, ShaderSource)
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
        }).AsValue(out var textureOwn);
        texture.Write(0, (uint)Unsafe.SizeOf<ColorByte>(), pixelData.AsSpan());
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

        var model = Model3D.Create(layer, new BindGroupDescriptor
        {
            Layout = layer.Shader.GetBindGroupLayout(0),
            Entries = new[]
            {
                BindGroupEntry.TextureView(0, view),
                BindGroupEntry.Sampler(1, sampler),
            },
        });
    }

    private static void OnSetup(IHostScreen screen)
    {
        screen.Title = "sample";

        var surfaceFormat = screen.SurfaceFormat;
        Debug.WriteLine($"backend: {screen.Backend}");
        var (pixelData, width, height) = SamplePrimitives.LoadImagePixels("pic.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3i(width, height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        }).AsValue(out var textureOwn);
        texture.Write(0, (uint)Unsafe.SizeOf<ColorByte>(), pixelData.AsSpan());
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

        var shader = ShaderModule.Create(screen, ShaderSource).AsValue(out var shaderOwn);

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

        var (vertices, indices, indexFormat) = SamplePrimitives.SampleData();
        var vertexBufferOwn = Buffer.CreateVertexBuffer<MyVertex>(screen, vertices);
        var indexBufferOwn = Buffer.CreateIndexBuffer<ushort>(screen, indices);

        //ReadOnlySpan<InstanceData> instances = stackalloc InstanceData[]
        //{
        //    //new InstanceData(new Vector3(0.5f, 0, 0)),
        //    //new InstanceData(new Vector3(-0.5f, 0, 0)),
        //    new InstanceData(new Vector3(0f, 0, 0)),
        //};
        //var instanceBuffer = Buffer.CreateVertexBuffer(screen, instances);
        //RuntimeHelpers.CreateSpan
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
            //InstanceBuffer = instanceBuffer,
            //InstanceCount = (uint)instances.Length,
            IndexBuffer = indexBufferOwn,
            IndexCount = (uint)indices.Length,
            IndexFormat = indexFormat,
        };
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
internal record struct InstanceData(Vector3 Offset);

internal sealed class State : IDisposable
{
    public required Own<Texture> Texture { get; init; }
    public required Own<TextureView> TextureView { get; init; }
    public required Own<Sampler> Sampler { get; init; }
    public required Own<BindGroupLayout> BindGroupLayout { get; init; }
    public required Own<BindGroup> BindGroup { get; init; }
    public required Own<ShaderModule> Shader { get; init; }
    public required Own<PipelineLayout> PipelineLayout { get; init; }
    public required Own<RenderPipeline> RenderPipeline { get; init; }

    public required Own<Buffer> VertexBuffer { get; init; }
    //public required Own<Buffer> InstanceBuffer { get; init; }
    //public required uint InstanceCount { get; init; }
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
        //InstanceBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}

internal sealed class RenderableObj : IDisposable
{
    public required Own<Texture> Texture { get; init; }
    public required Own<TextureView> TextureView { get; init; }
    public required Own<Sampler> Sampler { get; init; }
    public required Own<BindGroup> BindGroup { get; init; }
    public required Own<Buffer> VertexBuffer { get; init; }
    public required Own<Buffer> IndexBuffer { get; init; }
    public required uint IndexCount { get; init; }
    public required IndexFormat IndexFormat { get; init; }

    public void Render(RenderPass renderPass)
    {
        renderPass.SetBindGroup(0, BindGroup.AsValue());
        renderPass.SetVertexBuffer(0, VertexBuffer.AsValue());
        renderPass.SetIndexBuffer(IndexBuffer.AsValue(), IndexFormat);
        renderPass.DrawIndexed(0, IndexCount, 0, 0, 1);
    }

    public void Dispose()
    {
        Texture.Dispose();
        TextureView.Dispose();
        Sampler.Dispose();
        BindGroup.Dispose();
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
