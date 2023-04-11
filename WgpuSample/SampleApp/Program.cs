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
        });
        var albedo = LoadImage(screen, "pic.png", TextureFormat.Rgba8UnormSrgb);
        var mr = LoadImage(screen, "pic.png", TextureFormat.Rgba8Unorm);
        var normal = LoadImage(screen, "pic.png", TextureFormat.Rgba8Unorm);

        var model = new PbrModel(layer, SampleData.SampleMesh(screen), sampler, albedo, mr, normal);
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

    protected override void Render(RenderPass renderPass)
    {
        var mesh = Mesh;
        var material = Material;
        renderPass.SetBindGroup(0, material.BindGroup0);
        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
        renderPass.SetIndexBuffer(mesh.IndexBuffer);
        renderPass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
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
                0,
                ShaderStages.Fragment,
                new TextureBindingData
                {
                    Multisampled = false,
                    ViewDimension = TextureViewDimension.D2,
                    SampleType = TextureSampleType.FloatFilterable,
                }),
            BindGroupLayoutEntry.Sampler(
                1,
                ShaderStages.Fragment,
                SamplerBindingType.Filtering),
            BindGroupLayoutEntry.Buffer(
                2,
                ShaderStages.Vertex,
                new BufferBindingData
                {
                    HasDynamicOffset = false,
                    MinBindingSize = 0,
                    Type = BufferBindingType.Uniform,
                }),
        },
    };

    private readonly Own<BindGroupLayout> _bindGroupLayout0;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    private MyShader(Screen screen)
        : base(screen, ShaderSource, BuildPipelineLayoutDescriptor(screen, out var bindGroupLayout0))
    {
        _bindGroupLayout0 = bindGroupLayout0;
    }

    public static Own<MyShader> Create(Screen screen)
    {
        var self = new MyShader(screen);
        return CreateOwn(self);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroupLayout0.Dispose();
        }
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(Screen screen, out Own<BindGroupLayout> bindGroupLayout0)
    {
        bindGroupLayout0 = BindGroupLayout.Create(screen, _groupDesc0);
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout0.AsValue(),
            },
        };
    }
}

public sealed class MyMaterial : Material<MyMaterial, MyShader>
{
    private readonly Own<Texture> _texture;
    private readonly Own<Sampler> _sampler;
    private readonly Own<Uniform<Vector3>> _uniform;
    private readonly Own<BindGroup> _bindGroup;

    public BindGroup BindGroup0 => _bindGroup.AsValue();
    public Texture Texture => _texture.AsValue();
    public Sampler Sampler => _sampler.AsValue();

    private MyMaterial(
        MyShader shader,
        Own<Texture> texture,
        Own<Sampler> sampler,
        Own<Uniform<Vector3>> uniform,
        Own<BindGroup> bindGroup)
        : base(shader)
    {
        _texture = texture;
        _sampler = sampler;
        _uniform = uniform;
        _bindGroup = bindGroup;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _texture.Dispose();
            _sampler.Dispose();
            _uniform.Dispose();
            _bindGroup.Dispose();
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
            Layout = shader.BindGroupLayout0,
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
