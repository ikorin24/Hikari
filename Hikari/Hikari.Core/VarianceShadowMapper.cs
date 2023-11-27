#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed class VarianceShadowMapper : FrameObject
{
    private VarianceShadowMapper(Screen screen)
        : base(
            Mesh.Create<VertexPosOnly, ushort>(
                screen,
                [
                    new(0, 1, 0),
                    new(0, 0, 0),
                    new(1, 0, 0),
                    new(1, 1, 0),
                ],
                [0, 1, 2, 2, 3, 0]),
            [VsmMaterial.Create(
                VsmShader.Create(screen).AsValue(out var shaderOwn)).Cast<Material>()
            ])
    {
        shaderOwn.DisposeOn(Dead);
    }

    public static void Create(Screen screen)
    {
        _ = new VarianceShadowMapper(screen);
    }

    protected override void PrepareForRender()
    {
    }
}

internal sealed class VsmMaterial : Material
{
    private readonly BindGroupData _bg;

    private VsmMaterial(VsmShader shader) : base(shader)
    {
        var shadowMap = shader.Screen.Lights.DirectionalLight.ShadowMap;
        _bg = new BindGroupData
        {
            Index = 0,
            BindGroup = BindGroup.Create(shader.Screen, new()
            {
                Layout = shader.MaterialPassData[0].PipelineLayout.BindGroupLayouts[0],
                Entries = [
                BindGroupEntry.TextureView(0, shadowMap.View),
                ],
            }).DisposeOn(Disposed)
        };
    }

    public static Own<VsmMaterial> Create(VsmShader shader)
    {
        return CreateOwn(new VsmMaterial(shader));
    }

    public override ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return passIndex switch
        {
            0 or 1 => new ReadOnlySpan<BindGroupData>(in _bg),
            _ => throw new ArgumentOutOfRangeException(nameof(passIndex)),
        };
    }

    public override uint GetInstanceCount(int passIndex) => 1;

    public override MaterialPassData GetPassData(int passIndex)
    {
        return Shader.MaterialPassData[passIndex];
    }
}

internal sealed class VsmShader : Shader
{
    private static ReadOnlySpan<byte> VSMGaussian => """
        const weights: array<f32, 7> = array<f32, 7>(
            0.036632843,
            0.11128078,
            0.21674533,
            0.27068216,
            0.21674532,
            0.11128078,
            0.036632843,
        );
        @group(0) @binding(0) var tex: texture_2d<f32>;

        @vertex fn vs_main(
            @location(0) pos: vec3<f32>,
        ) -> @builtin(position) vec4<f32> {
            return vec4<f32>(pos, 1.0);
        }

        @fragment fn gaussian_x(@builtin(position) pos: vec4<f32>) -> @location(0) vec4<f32> {
            var c: vec2<f32>;
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0 - 3, 0), 0).rg * weights[0];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(1 - 3, 0), 0).rg * weights[1];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(2 - 3, 0), 0).rg * weights[2];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(3 - 3, 0), 0).rg * weights[3];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(4 - 3, 0), 0).rg * weights[4];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(5 - 3, 0), 0).rg * weights[5];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(6 - 3, 0), 0).rg * weights[6];
            return vec4<f32>(c, 0.0, 0.0);
        }

        @fragment fn gaussian_y(@builtin(position) pos: vec4<f32>) -> @location(0) vec4<f32> {
            var c: vec2<f32>;
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 0 - 3), 0).rg * weights[0];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 1 - 3), 0).rg * weights[1];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 2 - 3), 0).rg * weights[2];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 3 - 3), 0).rg * weights[3];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 4 - 3), 0).rg * weights[4];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 5 - 3), 0).rg * weights[5];
            c += textureLoad(tex, vec2<i32>(pos.xy) + vec2(0, 6 - 3), 0).rg * weights[6];
            return vec4<f32>(c, 0.0, 0.0);
        }
        """u8;

    private VsmShader(Screen screen)
        : base(screen, new ShaderPassDescriptorArray2
        {
            Pass0 = GetPassDescriptor(screen, "gaussian_x"u8, out var pass0Bgl0),
            Pass1 = GetPassDescriptor(screen, "gaussian_y"u8, out var pass1Bgl0),
        })
    {
        pass0Bgl0.DisposeOn(Disposed);
        pass1Bgl0.DisposeOn(Disposed);
    }

    public static Own<VsmShader> Create(Screen screen)
    {
        return CreateOwn(new VsmShader(screen));
    }

    private static ShaderPassDescriptor GetPassDescriptor(Screen screen, ReadOnlySpan<byte> fsEntryPoint, out Own<BindGroupLayout> bgl0)
    {
        return new()
        {
            Source = VSMGaussian,
            LayoutDescriptor = new()
            {
                BindGroupLayouts = [
                    BindGroupLayout.Create(screen, new()
                    {
                        Entries = [
                            BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                            {
                                ViewDimension = TextureViewDimension.D2,
                                Multisampled = false,
                                SampleType = TextureSampleType.FloatFilterable,
                            }),
                        ],
                    }).AsValue(out bgl0),
                ],
            },
            PipelineDescriptorFactory = static (module, layout) => new()
            {
                Layout = layout,
                Vertex = new VertexState
                {
                    Module = module,
                    EntryPoint = "vs_main"u8.ToImmutableArray(),
                    Buffers = [
                        VertexBufferLayout.FromVertex<VertexPosOnly>([
                            (0, VertexFieldSemantics.Position),
                        ]),
                    ],
                },
                Fragment = new FragmentState
                {
                    Module = module,
                    EntryPoint = "gaussian_x"u8.ToImmutableArray(),
                    Targets = [
                        new ColorTargetState
                        {
                            Format = TextureFormat.Rg16Float,
                            Blend = null,
                            WriteMask = ColorWrites.Red | ColorWrites.Green,
                        },
                    ],
                },
                Primitive = new PrimitiveState
                {
                    Topology = PrimitiveTopology.TriangleList,
                    StripIndexFormat = null,
                    FrontFace = FrontFace.Ccw,
                    CullMode = Face.Back,
                    PolygonMode = PolygonMode.Fill,
                },
                DepthStencil = null,
                Multisample = MultisampleState.Default,
                Multiview = 0,
            },
            SortOrder = 0,
            PassKind = PassKind.Custom("vsm-gaussian"),
        };
    }
}
