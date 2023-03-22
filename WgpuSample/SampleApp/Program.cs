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
        var screenConfig = new HostScreenConfig
        {
            Backend = GraphicsBackend.Dx12,
            Width = 1280,
            Height = 720,
            Style = WindowStyle.Default,
        };
        Engine.Run(screenConfig, OnInitialized);
    }

    private static void OnResized(HostScreen screen, Vector2u newSize)
    {
    }

    private static void OnInitialized(HostScreen screen)
    {
        screen.Resized += OnResized;
        screen.Title = "sample";
        var location = screen.GetLocation();
        var monitors = screen.GetMonitors();
        Debug.WriteLine(location);
        screen.SetLocation(new Vector2i(100, 100));


        var layer = new MyObjectLayer(screen);
        using var image = SampleData.LoadImage("pic.png");
        var texture = Texture.Create(screen, new TextureDescriptor
        {
            Size = new Vector3u((uint)image.Width, (uint)image.Height, 1),
            MipLevelCount = 1,
            SampleCount = 1,
            Dimension = TextureDimension.D2,
            Format = TextureFormat.Rgba8UnormSrgb,
            Usage = TextureUsages.TextureBinding | TextureUsages.CopyDst,
        });
        texture.AsValue().Write(0, image.GetPixels().AsReadOnly());
        var mesh = SampleData.SampleMesh(screen);
        var model = new MyModel(layer, mesh, texture);
        model.Material.SetUniform(new Vector3(0.1f, 0.4f, 0));
    }
}
//internal record struct InstanceData(Vector3 Offset);

public sealed class MyModel : Renderable<MyObjectLayer, MyVertex, MyShader, MyMaterial>
{
    public MyModel(MyObjectLayer layer, Own<Mesh> mesh, Own<Texture> texture) : base(layer, mesh, MyMaterial.Create(layer.Shader, texture))
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

    private MyShader(HostScreen screen) : base(screen, in _groupDesc0, ShaderSource)
    {
    }

    public static Own<MyShader> Create(HostScreen screen)
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
        : base(shader, new[] { bindGroup }, null)
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
    public MyObjectLayer(HostScreen screen)
        : base(MyShader.Create(screen), static shader => BuildPipeline(shader))
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
                        (0u, VertexFieldSemantics.Position),
                        (1u, VertexFieldSemantics.UV),
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
