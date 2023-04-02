#nullable enable
using Elffy.Effective;
using Elffy.Imaging;
using System;
using System.Diagnostics;
using System.IO;

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

    //private static void OnInitialized(Screen screen)
    //{
    //    screen.Title = "sample";
    //    var layer = new MyObjectLayer(screen, 0);
    //    var model = new MyModel(layer, SampleData.SampleMesh(screen), SampleData.SampleTexture(screen));
    //    model.Material.SetUniform(new Vector3(0.1f, 0.4f, 0));
    //}

    private static void OnInitialized2(Screen screen)
    {
        screen.Title = "sample";
        var layer = new PbrLayer(screen, 0);
        var deferredProcess = new DeferredProcess(layer, 1);


        var sampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
            LodMinClamp = 0,
            LodMaxClamp = float.MaxValue,
            AnisotropyClamp = 0,
            BorderColor = null,
            Compare = null,
        });
        var albedo = LoadImage(screen, "pic.png", TextureFormat.Rgba8UnormSrgb);
        var mr = LoadImage(screen, "pic.png", TextureFormat.Rgba8Unorm);

        var model = new PbrModel(layer, SampleData.SampleMesh(screen), sampler, albedo, mr);
        model.Material.SetUniform(new(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity));
    }

    private static Own<Texture> LoadImage(Screen screen, string filepath, TextureFormat format)
    {
        Own<Texture> texture;
        using(var stream = File.OpenRead(filepath)) {
            using var image = Image.FromStream(stream, Path.GetExtension(filepath));
            Debug.Assert(image.Width == 2048);
            Debug.Assert(image.Height == 2048);
            texture = Texture.Create(screen, new TextureDescriptor
            {
                Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
                MipLevelCount = 6,
                SampleCount = 1,
                Dimension = TextureDimension.D2,
                Format = format,
                Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
            });
            var tex = texture.AsValue();
            tex.Write<ColorByte>(0, image.GetPixels());
            for(uint i = 1; i < tex.MipLevelCount; i++) {
                var w = (image.Width >> (int)i);
                var h = (image.Height >> (int)i);
                using var curuent = image.Resized(new Vector2i(w, h));
                tex.Write<ColorByte>(i, curuent.GetPixels());
            }
        }
        return texture;
    }
}
//internal record struct InstanceData(Vector3 Offset);

public sealed class MyModel : Renderable<MyObjectLayer, VertexSlim, MyShader, MyMaterial>
{
    public MyModel(MyObjectLayer layer, Own<Mesh<VertexSlim>> mesh, Own<Texture> texture) : base(layer, mesh, MyMaterial.Create(layer.Shader, texture))
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

public sealed class MyObjectLayer : ObjectLayer<MyObjectLayer, VertexSlim, MyShader, MyMaterial>
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
                    VertexBufferLayout.FromVertex<VertexSlim>(stackalloc[]
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

public interface IGBufferProvider
{
    GBuffer CurrentGBuffer { get; }
    Event<GBuffer> GBufferChanged { get; }
}

// --------

public sealed class DeferredProcess : RenderOperation<DeferredProcessShader, DeferredProcessMaterial>
{
    private readonly IGBufferProvider _gBufferProvider;
    private Own<DeferredProcessMaterial> _material;
    private readonly Own<Mesh<VertexSlim>> _rectMesh;

    public DeferredProcess(IGBufferProvider gBufferProvider, int sortOrder)
        : base(CreateShader(gBufferProvider, out var gBuffer, out var pipeline), pipeline, sortOrder)
    {
        _gBufferProvider = gBufferProvider;

        RecreateMaterial(gBuffer);
        gBufferProvider.GBufferChanged.Subscribe(RecreateMaterial).AddTo(Subscriptions);
        const float Z = 0;
        ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[]
        {
            new(new(-1, -1, Z), new(0, 0)),
            new(new(1, -1, Z), new(1, 0)),
            new(new(1, 1, Z), new(1, 1)),
            new(new(-1, 1, Z), new(0, 1)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        _rectMesh = Mesh<VertexSlim>.Create(Screen, vertices, indices);
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
                    VertexBufferLayout.FromVertex<VertexSlim>(stackalloc[]
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
            var c3: vec3<f32> = textureSample(g3, g_sampler, in.uv).xyz;
            return vec4(c2, 1.0);
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
