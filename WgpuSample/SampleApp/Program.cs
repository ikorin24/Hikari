#nullable enable
using Elffy.Effective;
using System;
using System.Diagnostics;

namespace Elffy;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("RUST_BACKTRACE", "1");
        var screenConfig = new ScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        Engine.Run(screenConfig, OnInitialized2);
    }

    private static void OnInitialized(Screen screen)
    {
        screen.Title = "sample";
        var layer = new MyObjectLayer(screen, 0);
        var model = new MyModel(layer, SampleData.SampleMesh(screen), SampleData.SampleTexture(screen));
        model.Material.SetUniform(new Vector3(0.1f, 0.4f, 0));
    }

    private static void OnInitialized2(Screen screen)
    {
        screen.Title = "sample";
        var layer = new PbrLayer(screen, 0);
        var deferredProcess = new DeferredProcess(layer, 1);
        var model = new PbrModel(layer, SampleData.SampleMesh(screen));
        model.Material.SetUniform(new(Color4.Red, Color4.Green, Color4.Blue));
    }
}
//internal record struct InstanceData(Vector3 Offset);

public sealed class MyModel : Renderable<MyObjectLayer, MyVertex, MyShader, MyMaterial>
{
    public MyModel(MyObjectLayer layer, Own<Mesh<MyVertex>> mesh, Own<Texture> texture) : base(layer, mesh, MyMaterial.Create(layer.Shader, texture))
    {
    }
}

public sealed class MyShader : Shader<MyShader, MyMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vertex {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }

        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }

        @group(0) @binding(0) var t_diffuse: texture_2d<f32>;
        @group(0) @binding(1) var s_diffuse: sampler;
        @group(0) @binding(2) var<uniform> offset: vec3<f32>;

        @vertex fn vs_main(
            v: Vertex,
        ) -> V2F {
            return V2F
            (
                vec4(v.pos + offset, 1.0),
                v.uv,
            );
        }

        @fragment fn fs_main(in: V2F) -> @location(0) vec4<f32> {
            return textureSample(t_diffuse, s_diffuse, in.uv);
        }
        """u8;

    private static readonly BindGroupLayoutDescriptor _groupDesc0 = new()
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
            BindGroupLayoutEntry.Buffer(
                binding: 2,
                visibility: ShaderStages.Vertex,
                type: new BufferBindingData
                {
                    HasDynamicOffset = false,
                    MinBindingSize = 0,
                    Type = BufferBindingType.Uniform,
                },
                count: 0),
        },
    };

    private MyShader(Screen screen) : base(screen, in _groupDesc0, ShaderSource)
    {
    }

    public static Own<MyShader> Create(Screen screen)
    {
        var self = new MyShader(screen);
        return CreateOwn(self);
    }
}

public sealed class MyMaterial : Material<MyMaterial, MyShader>
{
    private readonly Own<Texture> _texture;
    private readonly Own<Sampler> _sampler;
    private readonly Own<Uniform<Vector3>> _uniform;

    public Texture Texture => _texture.AsValue();
    public Sampler Sampler => _sampler.AsValue();

    private MyMaterial(
        MyShader shader,
        Own<Texture> texture,
        Own<Sampler> sampler,
        Own<Uniform<Vector3>> uniform,
        Own<BindGroup> bindGroup)
        : base(shader, new[] { bindGroup })
    {
        _texture = texture;
        _sampler = sampler;
        _uniform = uniform;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _texture.Dispose();
            _sampler.Dispose();
            _uniform.Dispose();
        }
    }

    public static Own<MyMaterial> Create(MyShader shader, Own<Texture> texture)
    {
        ArgumentNullException.ThrowIfNull(shader);
        texture.ThrowArgumentExceptionIfNone();

        var screen = shader.Screen;
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Linear, FilterMode.Nearest);
        var uniform = Uniform.Create(screen, default(Vector3));
        var bindGroup = BindGroup.Create(shader.Screen, new BindGroupDescriptor
        {
            Layout = shader.GetBindGroupLayout(0),
            Entries = new BindGroupEntry[3]
            {
                BindGroupEntry.TextureView(0, texture.AsValue().View),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.Buffer(2, uniform.AsValue().Buffer),
            },
        });
        return CreateOwn(new MyMaterial(shader, texture, sampler, uniform, bindGroup));
    }

    public void SetUniform(in Vector3 value)
    {
        _uniform.AsValue().Set(value);
    }
}

public sealed class MyObjectLayer : ObjectLayer<MyObjectLayer, MyVertex, MyShader, MyMaterial>
{
    public MyObjectLayer(Screen screen, int sortOrder)
        : base(MyShader.Create(screen), static shader => BuildPipeline(shader), sortOrder)
    {
    }

    private static Own<RenderPipeline> BuildPipeline(MyShader shader)
    {
        var screen = shader.Screen;
        var desc = new RenderPipelineDescriptor
        {
            Layout = shader.PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.Module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<MyVertex>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    }),
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
        };
        return RenderPipeline.Create(screen, in desc);
    }
}

// --------

public sealed class PbrModel : Renderable<PbrLayer, MyVertex, PbrShader, PbrMaterial>
{
    public PbrModel(PbrLayer layer, Own<Mesh<MyVertex>> mesh) : base(layer, mesh, PbrMaterial.Create(layer.Shader))
    {
    }
}

public sealed class PbrShader : Shader<PbrShader, PbrMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
        }
        struct GBuffer {
            @location(0) g0 : vec4<f32>,
            @location(1) g1 : vec4<f32>,
            @location(2) g2 : vec4<f32>,
            @location(3) g3 : vec4<f32>,
        }
        struct UniformValue {
            c0: vec4<f32>,
            c1: vec4<f32>,
            c2: vec4<f32>,
        }

        @group(0) @binding(0) var<uniform> uniform: UniformValue;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = vec4(v.pos, 1.0);
            return output;
        }

        @fragment fn fs_main(in: V2F) -> GBuffer {
            var output: GBuffer;
            output.g0 = uniform.c0;
            output.g1 = uniform.c1;
            output.g2 = uniform.c2;
            output.g3 = vec4(1.0, 1.0, 1.0, 1.0);
            return output;
        }
        """u8;

    private static readonly BindGroupLayoutDescriptor _bglDesc = new()
    {
        Entries = new[]
        {
            BindGroupLayoutEntry.Buffer(
                binding: 0,
                visibility: ShaderStages.Fragment,
                type: new BufferBindingData
                {
                    HasDynamicOffset = false,
                    MinBindingSize = 0,
                    Type = BufferBindingType.Uniform,
                },
                count: 0),
        },
    };

    private PbrShader(Screen screen) : base(screen, in _bglDesc, ShaderSource)
    {
    }

    public static Own<PbrShader> Create(Screen screen)
    {
        var self = new PbrShader(screen);
        return CreateOwn(self);
    }
}

public sealed class PbrMaterial : Material<PbrMaterial, PbrShader>
{
    private readonly Own<Uniform<UniformValue>> _uniform;

    private PbrMaterial(PbrShader shader, Own<Uniform<UniformValue>> uniform, Own<BindGroup> bindGroup)
        : base(shader, new[] { bindGroup })
    {
        _uniform = uniform;
    }

    public static Own<PbrMaterial> Create(PbrShader shader, in UniformValue uniformValue = default)
    {
        var screen = shader.Screen;
        var uniform = Uniform<UniformValue>.Create(screen, in uniformValue);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.GetBindGroupLayout(0),
            Entries = new BindGroupEntry[1]
            {
                BindGroupEntry.Buffer(0, uniform.AsValue().Buffer),
            },
        };
        var bindGroup = BindGroup.Create(screen, in desc);
        var material = new PbrMaterial(shader, uniform, bindGroup);
        return CreateOwn(material);
    }

    public void SetUniform(in UniformValue value) => _uniform.AsValue().Set(in value);

    public record struct UniformValue(
        Color4 C0,
        Color4 C1,
        Color4 C2
    );
}

public sealed class PbrLayer
    : ObjectLayer<PbrLayer, MyVertex, PbrShader, PbrMaterial>,
    IGBufferProvider
{
    private const int MrtCount = 4;
    private static readonly TextureFormat[] _formats = new TextureFormat[MrtCount]
    {
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
    };
    private static readonly ReadOnlyMemory<ColorTargetState?> _targets = new ColorTargetState?[MrtCount]
    {
        new ColorTargetState
        {
            Format = _formats[0],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[1],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[2],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[3],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
    };

    private Own<GBuffer> _gBuffer;
    private EventSource<GBuffer> _gBufferChanged = new();

    public GBuffer CurrentGBuffer => _gBuffer.AsValue();

    public Event<GBuffer> GBufferChanged => _gBufferChanged.Event;

    public PbrLayer(Screen screen, int sortOrder)
        : base(PbrShader.Create(screen), static shader => BuildPipeline(shader), sortOrder)
    {
        RecreateGBuffer(screen, screen.ClientSize);
        screen.Resized.Subscribe(x => RecreateGBuffer(x.Screen, x.Size)).AddTo(Subscriptions);
        Dead.Subscribe(static x =>
        {
            ((PbrLayer)x)._gBuffer.Dispose();
        }).AddTo(Subscriptions);
    }

    private void RecreateGBuffer(Screen screen, Vector2u newSize)
    {
        if(_gBuffer.TryAsValue(out var gBuffer) && gBuffer.Size == newSize) {
            return;
        }
        _gBuffer.Dispose();
        _gBuffer = GBuffer.Create(screen, newSize, _formats);
        _gBufferChanged.Invoke(_gBuffer.AsValue());
    }

    protected override Own<RenderPass> CreateRenderPass(in CommandEncoder encoder)
    {
        return _gBuffer.AsValue().CreateRenderPass(encoder);
    }

    private static Own<RenderPipeline> BuildPipeline(PbrShader shader)
    {
        var screen = shader.Screen;
        var desc = new RenderPipelineDescriptor
        {
            Layout = shader.PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.Module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<MyVertex>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    }),
                },
            },
            Fragment = new FragmentState
            {
                Module = shader.Module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = _targets,
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
        };
        return RenderPipeline.Create(screen, in desc);
    }
}

public interface IGBufferProvider
{
    GBuffer CurrentGBuffer { get; }
    Event<GBuffer> GBufferChanged { get; }
}

// --------

public sealed class DeferredProcess : RenderOperation<DeferredProcessShader, DeferredProcessMaterial>
{
    private record struct PosUV(Vector3 Position, Vector2 UV) : IVertex<PosUV>
    {
        public static unsafe uint VertexSize => (uint)sizeof(PosUV);

        public static unsafe ReadOnlyMemory<VertexField> Fields => new[]
        {
            new VertexField(0, 12, VertexFormat.Float32x3, VertexFieldSemantics.Position),
            new VertexField(12, 8, VertexFormat.Float32x2, VertexFieldSemantics.UV),
        };
    }

    private readonly IGBufferProvider _gBufferProvider;
    private Own<DeferredProcessMaterial> _material;
    private readonly Own<Mesh<PosUV>> _rectMesh;

    public DeferredProcess(IGBufferProvider gBufferProvider, int sortOrder)
        : base(CreateShader(gBufferProvider, out var gBuffer, out var pipeline), pipeline, sortOrder)
    {
        _gBufferProvider = gBufferProvider;

        RecreateMaterial(gBuffer);
        gBufferProvider.GBufferChanged.Subscribe(RecreateMaterial).AddTo(Subscriptions);
        const float Z = 0;
        ReadOnlySpan<PosUV> vertices = stackalloc PosUV[]
        {
            new(new(-1, -1, Z), new(0, 0)),
            new(new(1, -1, Z), new(1, 0)),
            new(new(1, 1, Z), new(1, 1)),
            new(new(-1, 1, Z), new(0, 1)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        _rectMesh = Mesh<PosUV>.Create(Screen, vertices, indices);
        Dead.Subscribe(static x => ((DeferredProcess)x).OnDead()).AddTo(Subscriptions);
    }

    private void OnDead()
    {
        _material.Dispose();
        _rectMesh.Dispose();
    }

    private void RecreateMaterial(GBuffer gBuffer)
    {
        _material.Dispose();
        _material = DeferredProcessMaterial.Create(Shader, gBuffer);
    }

    private static Own<DeferredProcessShader> CreateShader(IGBufferProvider gBufferProvider, out GBuffer gBuffer, out Own<RenderPipeline> pipeline)
    {
        ArgumentNullException.ThrowIfNull(gBufferProvider);
        gBuffer = gBufferProvider.CurrentGBuffer;
        var screen = gBuffer.Screen;
        var shader = DeferredProcessShader.Create(screen);
        var desc = new RenderPipelineDescriptor
        {
            Layout = shader.AsValue().PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.AsValue().Module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<MyVertex>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    }),
                },
            },
            Fragment = new FragmentState
            {
                Module = shader.AsValue().Module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
                    new ColorTargetState
                    {
                        Format = screen.SurfaceFormat,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    }
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
        };
        pipeline = RenderPipeline.Create(screen, desc);
        return shader;
    }

    protected override void Render(RenderPass renderPass)
    {
        var mesh = _rectMesh.AsValue();
        var material = _material.AsValue();
        var bindGroups = material.BindGroups.Span;

        renderPass.SetPipeline(Pipeline);
        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        renderPass.SetBindGroup(0, bindGroups[0]);
        renderPass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}

public sealed class DeferredProcessShader : Shader<DeferredProcessShader, DeferredProcessMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }
        @group(0) @binding(0) var g_sampler: sampler;
        @group(0) @binding(1) var g0: texture_2d<f32>;
        @group(0) @binding(2) var g1: texture_2d<f32>;
        @group(0) @binding(3) var g2: texture_2d<f32>;
        @group(0) @binding(4) var g3: texture_2d<f32>;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = vec4(v.pos, 1.0);
            output.uv = v.uv;
            return output;
        }

        @fragment fn fs_main(in: V2F) -> @location(0) vec4<f32> {
            var c0: vec3<f32> = textureSample(g0, g_sampler, in.uv).xyz;
            var c1: vec3<f32> = textureSample(g1, g_sampler, in.uv).xyz;
            var c2: vec3<f32> = textureSample(g2, g_sampler, in.uv).xyz;
            var c = c0 + c1 + c2;
            return vec4(c, 1.0);
            //return vec4(1.0, 0.5, 1.0, 1.0);
        }
        """u8;

    private static readonly BindGroupLayoutDescriptor _bindGroupLayoutDesc = new()
    {
        Entries = new BindGroupLayoutEntry[]
        {
            BindGroupLayoutEntry.Sampler(0, ShaderStages.Fragment, SamplerBindingType.NonFiltering, 0),
            BindGroupLayoutEntry.Texture(1, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }, 0),
            BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }, 0),
            BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }, 0),
            BindGroupLayoutEntry.Texture(4, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }, 0),
        }
    };

    private DeferredProcessShader(Screen screen) : base(screen, _bindGroupLayoutDesc, ShaderSource)
    {
    }

    public static Own<DeferredProcessShader> Create(Screen screen)
    {
        var shader = new DeferredProcessShader(screen);
        return CreateOwn(shader);
    }
}

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private DeferredProcessMaterial(DeferredProcessShader shader, Own<BindGroup>[] bindGroupOwns) : base(shader, bindGroupOwns)
    {
    }

    public static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, GBuffer gBuffer)
    {
        var screen = shader.Screen;
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.GetBindGroupLayout(0),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Sampler(0, sampler.AsValue()),
                BindGroupEntry.TextureView(1, gBuffer.ColorAttachment(0).View),
                BindGroupEntry.TextureView(2, gBuffer.ColorAttachment(1).View),
                BindGroupEntry.TextureView(3, gBuffer.ColorAttachment(2).View),
                BindGroupEntry.TextureView(4, gBuffer.ColorAttachment(3).View),
            },
        };
        var bg = BindGroup.Create(screen, desc);
        var material = new DeferredProcessMaterial(shader, new[] { bg });
        return CreateOwn(material);
    }
}
