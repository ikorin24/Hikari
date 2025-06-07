#nullable enable
using Hikari;
using System;
using System.Collections.Immutable;

namespace CannonCape;

public sealed partial class SkyShader : ITypedShader
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

        @group(0) @binding(0) var<uniform> model_data: ModelData;
        @group(1) @binding(0) var<uniform> camera: CameraMatrix;

        @vertex fn vs_main(
            vin: Vin,
        ) -> V2F {
            var v2f: V2F;
            v2f.clip_pos = camera.proj * camera.view * model_data.model * vec4(vin.pos, 1.0);
            v2f.uv = vin.uv;
            return v2f;
        }

        @fragment fn fs_main(v2f: V2F) -> Vout {
            let t = v2f.uv.y;
            let c1 = vec3(0.02, 0.03, 0.09);
            let c2 = vec3(0.556, 0.8, 1);
            let c3 =  vec3(0.239, 0.66, 1);
            var color: vec3<f32>;

            let a0 = 0.5;
            let a1 = 0.51;
            let a2 = 0.55;
            if (t < a0) {
                color = c1;
            }
            else if (t < a1) {
                color = mix(c1, c2, (t - a0) / (a1 - a0));
            }
            else if (t < a2) {
                color = mix(c2, c3, (t - a1) / (a2 - a1));
            }
            else {
                color = c3;
            }
            var vout: Vout;
            vout.color = vec4(color, 1.0);
            return vout;
        }
        """u8;

    public Screen Screen => _shader.Screen;

    public ImmutableArray<ShaderPassData> ShaderPasses => _shader.ShaderPasses;
    public ImmutableArray<VertexFieldSemantics> NeededSemantics => _neededSemantics;

    [Owned(nameof(Release))]
    private SkyShader(Screen screen)
    {
        var disposables = new DisposableBag();
        var shader = Shader.Create(
            screen,
            [
                new ShaderPassDescriptor
                {
                    PassKind = PassKind.Forward,
                    Source = Source.ToImmutableArray(),
                    SortOrderInPass = 3000,
                    LayoutDescriptor = new PipelineLayoutDescriptor
                    {
                        BindGroupLayouts =
                        [
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

public sealed partial class SkyMaterial : IMaterial
{
    private readonly SkyShader _shader;

    public Screen Screen => _shader.Screen;

    public SkyShader Shader => _shader;

    ITypedShader IMaterial.Shader => Shader;

    [Owned(nameof(Release))]
    private SkyMaterial(SkyShader shader)
    {
        _shader = shader;
    }

    private void Release()
    {
    }

    void IMaterial.SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        renderPass.SetBindGroup(0, renderer.ModelDataBindGroup);
        renderPass.SetBindGroup(1, renderer.Screen.Camera.CameraDataBindGroup);
    }
}
