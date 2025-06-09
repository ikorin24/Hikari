#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed partial class UnlitTextureShader : ITypedShader
{
    private static readonly ImmutableArray<VertexFieldSemantics> _neededSemantics =
    [
        VertexFieldSemantics.Position,
        VertexFieldSemantics.UV,
    ];

    private readonly Shader _shader;
    private readonly DisposableBag _disposables;

    private static ReadOnlySpan<byte> Source =>
        """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }

        struct ModelData {
            model: mat4x4<f32>,
            is_uniform_scale: i32,
        }

        struct CameraMatrix {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
            inv_proj: mat4x4<f32>,
            inv_view: mat4x4<f32>,
        }

        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
        }

        struct Vout {
            @location(0) color : vec4<f32>,
        }

        @group(0) @binding(0) var tex: texture_2d<f32>;
        @group(0) @binding(1) var tex_sampler: sampler;
        @group(1) @binding(0) var<uniform> model_data: ModelData;
        @group(2) @binding(0) var<uniform> camera: CameraMatrix;

        @vertex fn vs_main(
            vin: Vin,
        ) -> V2F {
            var v2f: V2F;
            v2f.clip_pos = camera.proj * camera.view * model_data.model * vec4(vin.pos, 1.0);
            v2f.uv = vin.uv;
            return v2f;
        }

        @fragment fn fs_main(v2f: V2F) -> Vout {
            var vout: Vout;
            vout.color = textureSample(tex, tex_sampler, v2f.uv);
            return vout;
        }
        """u8;

    public Screen Screen => _shader.Screen;

    public ImmutableArray<ShaderPassData> ShaderPasses => _shader.ShaderPasses;

    public ImmutableArray<VertexFieldSemantics> NeededSemantics => _neededSemantics;

    [Owned(nameof(Release))]
    private UnlitTextureShader(Screen screen)
    {
        var disposables = new DisposableBag();
        var shader = Shader.Create(
            screen,
            [
                new ShaderPassDescriptor
                {
                    PassKind = PassKind.Forward,
                    Source = Source.ToImmutableArray(),
                    SortOrderInPass = 0,
                    LayoutDescriptor = new PipelineLayoutDescriptor
                    {
                        BindGroupLayouts =
                        [
                            BindGroupLayout.Create(screen, new()
                            {
                                Entries =
                                [
                                    BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                                    {
                                        ViewDimension = TextureViewDimension.D2,
                                        Multisampled = false,
                                        SampleType = TextureSampleType.FloatFilterable,
                                    }),
                                    BindGroupLayoutEntry.Sampler(1, ShaderStages.Fragment, SamplerBindingType.Filtering),
                                ],
                            }).AddTo(disposables),
                            screen.UtilResource.ModelDataBindGroupLayout,
                            screen.Camera.CameraDataBindGroupLayout,
                        ]
                    },
                    PipelineDescriptorFactory = (module, layout) => new RenderPipelineDescriptor
                    {
                        Layout = layout,
                        Vertex = new VertexState()
                        {
                            Module = module,
                            EntryPoint = "vs_main"u8.ToImmutableArray(),
                            Buffers =
                            [
                                VertexBufferLayout.FromVertex<Vertex>(
                                [
                                    (0, VertexFieldSemantics.Position),
                                    (1, VertexFieldSemantics.UV),
                                ]),
                            ],
                        },
                        Fragment = new FragmentState()
                        {
                            Module = module,
                            EntryPoint = "fs_main"u8.ToImmutableArray(),
                            Targets =
                            [
                                new ColorTargetState
                                {
                                    Format = screen.Surface.Format,
                                    Blend = BlendState.AlphaBlending,
                                    WriteMask = ColorWrites.All,
                                },
                            ],
                        },
                        Primitive = new PrimitiveState()
                        {
                            Topology = PrimitiveTopology.TriangleList,
                            FrontFace = FrontFace.Ccw,
                            CullMode = Face.Back,
                            PolygonMode = PolygonMode.Fill,
                            StripIndexFormat = null,
                        },
                        DepthStencil = new DepthStencilState
                        {
                            Format = screen.DepthStencil.Format,
                            DepthWriteEnabled = true,
                            DepthCompare = CompareFunction.Greater,
                            Stencil = StencilState.Default,
                            Bias = DepthBiasState.Default,
                        },
                        Multisample = MultisampleState.Default,
                        Multiview = 0,
                    },
                    OnRenderPass = static (in RenderPassState state) => state.DefaultDrawIndexed(),
                },
            ])
            .AddTo(disposables);

        _shader = shader;
        _disposables = disposables;
    }

    private void Release()
    {
        _disposables.Dispose();
    }
}

public sealed partial class UnlitTextureMaterial : IMaterial
{
    private readonly UnlitTextureShader _shader;
    private readonly Texture2D _texture;
    private readonly BindGroup _bindGroup0;
    private readonly DisposableBag _disposables;

    public Screen Screen => _shader.Screen;

    public UnlitTextureShader Shader => _shader;

    ITypedShader IMaterial.Shader => Shader;

    public Texture2D Texture => _texture;

    [Owned(nameof(Release))]
    private UnlitTextureMaterial(UnlitTextureShader shader, Texture2D texture)
    {
        var screen = shader.Screen;
        var disposables = new DisposableBag();
        var bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.ShaderPasses[0].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.TextureView(0, texture.View),
                BindGroupEntry.Sampler(1, screen.UtilResource.LinearSampler),
            ],
        }).AddTo(disposables);

        _shader = shader;
        _texture = texture;
        _bindGroup0 = bindGroup0;
        _disposables = disposables;
    }

    private void Release()
    {
        _disposables.Dispose();
    }

    void IMaterial.SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        renderPass.SetBindGroup(0, _bindGroup0);
        renderPass.SetBindGroup(1, renderer.ModelDataBindGroup);
        renderPass.SetBindGroup(2, renderer.Screen.Camera.CameraDataBindGroup);
    }
}
