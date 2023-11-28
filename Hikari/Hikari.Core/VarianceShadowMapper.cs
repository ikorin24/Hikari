#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;
using Utf8StringInterpolation;

namespace Hikari;

public sealed class VarianceShadowMapper : FrameObject
{
    private VarianceShadowMapper(Screen screen)
        : base(
            Mesh.Create<VertexPosOnly, ushort>(
                screen,
                [
                    new(-1, 1, 0),
                    new(-1, -1, 0),
                    new(1, -1, 0),
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
    private readonly BindGroupData _pass0BindGroup;
    private readonly BindGroupData _pass1BindGroup;

    private VsmMaterial(VsmShader shader) : base(shader)
    {
        var light = shader.Screen.Lights.DirectionalLight;
        _pass0BindGroup = new BindGroupData
        {
            Index = 0,
            BindGroup = BindGroup.Create(shader.Screen, new()
            {
                Layout = shader.ShaderPasses[0].Pipeline.Layout.BindGroupLayouts[0],
                Entries = [
                    BindGroupEntry.TextureView(0, light.ShadowMap.View),
                ],
            }).DisposeOn(Disposed)
        };
        _pass1BindGroup = new BindGroupData
        {
            Index = 0,
            BindGroup = BindGroup.Create(shader.Screen, new()
            {
                Layout = shader.ShaderPasses[1].Pipeline.Layout.BindGroupLayouts[0],
                Entries = [
                    BindGroupEntry.TextureView(0, light.ShadowMapTemp.View),
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
            0 => new ReadOnlySpan<BindGroupData>(in _pass0BindGroup),
            1 => new ReadOnlySpan<BindGroupData>(in _pass1BindGroup),
            _ => throw new ArgumentOutOfRangeException(nameof(passIndex)),
        };
    }
}

internal sealed class VsmShader : Shader
{
    private static readonly Lazy<ImmutableArray<byte>> VSMGaussian = new(() =>
    {
        // Create Gaussian blur kernel
        // kernel size is NxN
        // Gaussian range is [-R * Sigma, R * Sigma]
        const int N = 7;
        const float Sigma = 1f;
        const float R = 2f;

        Span<float> values = stackalloc float[N];
        {
            var interval = 2f * R * Sigma / (N - 1);
            for(int i = 0; i < N; i++) {
                float x = -R * Sigma + interval * i;
                values[i] = float.Exp(-x * x / (2f * Sigma * Sigma));
            }
            float sum = 0;
            foreach(var value in values) {
                sum += value;
            }
            for(int i = 0; i < values.Length; i++) {
                values[i] /= sum;
            }
        }

        using var buf = new PooledArrayBufferWriter<byte>();
        var sb = Utf8String.CreateWriter(buf);
        sb.AppendFormat($$"""
            const SM_WIDTH = {{DirectionalLight.ShadowMapWidth}};
            const SM_HEIGHT = {{DirectionalLight.ShadowMapHeight}};
            const CASCADE_COUNT_F = {{DirectionalLight.CascadeCountConst}}.0;
            const weights: array<f32, {{N}}> = array<f32, {{N}}>(

            """);
        for(int i = 0; i < values.Length; i++) {
            sb.AppendFormat($$"""
                {{values[i]}},

            """);
        }
        sb.AppendFormat($$"""
            );
            @group(0) @binding(0) var tex: texture_2d<f32>;

            struct V2F {
                @builtin(position) clip_pos: vec4<f32>,
                @location(0) cascade: u32,
            };

            @vertex fn vs_main(
                @location(0) pos: vec3<f32>,
                @builtin(instance_index) cascade: u32,
            ) -> V2F {
                // pos.x is -1 ~ 1.
                // Whadow map is texture atlas of each cascade.
                // Convert its x coordinate to each cascade's x coordinate.

                // For example, When cascade count is 4,
                // x is
                // [0]: -1.0 ~ -0.5,
                // [1]: -0.5 ~ 0.0,
                // [2]: 0.0 ~ 0.5,
                // [3]: 0.5 ~ 1.0
                let x = (pos.x * 0.5 + 0.5) / CASCADE_COUNT_F * 2.0 - 1.0
                        + f32(cascade) * 2.0 / CASCADE_COUNT_F;
                return V2F(vec4<f32>(x, pos.yz, 1.0), cascade);
            }

            @fragment fn gaussian_x(
                v2f: V2F,
            ) -> @location(0) vec4<f32> {
                let pos: vec2<i32> = vec2<i32>(v2f.clip_pos.xy / v2f.clip_pos.w);
                var c: vec2<f32>;
                let x_min: i32 = i32(v2f.cascade) * SM_WIDTH;
                let x_max: i32 = x_min + SM_WIDTH - 1;
                c += load_texel_clamp_x(pos.x - 3, pos.y, x_min, x_max) * weights[0];
                c += load_texel_clamp_x(pos.x - 2, pos.y, x_min, x_max) * weights[1];
                c += load_texel_clamp_x(pos.x - 1, pos.y, x_min, x_max) * weights[2];
                c += load_texel_clamp_x(pos.x + 0, pos.y, x_min, x_max) * weights[3];
                c += load_texel_clamp_x(pos.x + 1, pos.y, x_min, x_max) * weights[4];
                c += load_texel_clamp_x(pos.x + 2, pos.y, x_min, x_max) * weights[5];
                c += load_texel_clamp_x(pos.x + 3, pos.y, x_min, x_max) * weights[6];
                return vec4<f32>(c, 0.0, 0.0);
            }

            @fragment fn gaussian_y(
                v2f: V2F,
            ) -> @location(0) vec4<f32> {
                let pos: vec2<i32> = vec2<i32>(v2f.clip_pos.xy / v2f.clip_pos.w);
                var c: vec2<f32>;
                let y_min: i32 = 0;
                let y_max: i32 = SM_HEIGHT - 1;
                c += load_texel_clamp_y(pos.x, pos.y - 3, y_min, y_max) * weights[0];
                c += load_texel_clamp_y(pos.x, pos.y - 2, y_min, y_max) * weights[1];
                c += load_texel_clamp_y(pos.x, pos.y - 1, y_min, y_max) * weights[2];
                c += load_texel_clamp_y(pos.x, pos.y + 0, y_min, y_max) * weights[3];
                c += load_texel_clamp_y(pos.x, pos.y + 1, y_min, y_max) * weights[4];
                c += load_texel_clamp_y(pos.x, pos.y + 2, y_min, y_max) * weights[5];
                c += load_texel_clamp_y(pos.x, pos.y + 3, y_min, y_max) * weights[6];
                return vec4<f32>(c, 0.0, 0.0);
            }

            fn load_texel_clamp_x(x: i32, y: i32, x_min: i32, x_max: i32) -> vec2<f32> {
                let x_actual: i32 = clamp(x, x_min, x_max);
                return textureLoad(tex, vec2<i32>(x_actual, y), 0).rg;
            }

            fn load_texel_clamp_y(x: i32, y: i32, y_min: i32, y_max: i32) -> vec2<f32> {
                let y_actual: i32 = clamp(y, y_min, y_max);
                return textureLoad(tex, vec2<i32>(x, y_actual), 0).rg;
            }
            """);
        sb.Flush();
        return buf.WrittenMemory.ToArray().AsImmutableArray();
    });

    private VsmShader(Screen screen)
        : base(screen, [
            GetPassDescriptorX(screen, out var pass0Bgl0),
            GetPassDescriptorY(screen, out var pass1Bgl0),
        ])
    {
        pass0Bgl0.DisposeOn(Disposed);
        pass1Bgl0.DisposeOn(Disposed);
    }

    public static Own<VsmShader> Create(Screen screen)
    {
        return CreateOwn(new VsmShader(screen));
    }

    private static ShaderPassDescriptor GetPassDescriptorX(Screen screen, out Own<BindGroupLayout> bgl0)
    {
        return new()
        {
            Source = VSMGaussian.Value,
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
                                //SampleType = TextureSampleType.FloatFilterable,
                                SampleType = TextureSampleType.FloatNotFilterable,
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
                            Format = DirectionalLight.VarianceShadowMapFormat,
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
            PassKind = PassKind.Custom("prepare-vsm-x"),
            OnRenderPass = (in RenderPass renderPass, RenderPipeline pipeline, Material material, Mesh mesh, in SubmeshData submesh, int passIndex) =>
            {
                renderPass.SetPipeline(pipeline);
                renderPass.SetBindGroups(material.GetBindGroups(passIndex));
                renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                renderPass.DrawIndexed(submesh.IndexOffset, submesh.IndexCount, submesh.VertexOffset, 0, DirectionalLight.CascadeCountConst);
            },
        };
    }

    private static ShaderPassDescriptor GetPassDescriptorY(Screen screen, out Own<BindGroupLayout> bgl0)
    {
        return new()
        {
            Source = VSMGaussian.Value,
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
                                //SampleType = TextureSampleType.FloatFilterable,
                                SampleType = TextureSampleType.FloatNotFilterable,
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
                    EntryPoint = "gaussian_y"u8.ToImmutableArray(),
                    Targets = [
                        new ColorTargetState
                        {
                            Format = DirectionalLight.VarianceShadowMapFormat,
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
            PassKind = PassKind.Custom("prepare-vsm-y"),
            OnRenderPass = (in RenderPass renderPass, RenderPipeline pipeline, Material material, Mesh mesh, in SubmeshData submesh, int passIndex) =>
            {
                renderPass.SetPipeline(pipeline);
                renderPass.SetBindGroups(material.GetBindGroups(passIndex));
                renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                renderPass.DrawIndexed(submesh.IndexOffset, submesh.IndexCount, submesh.VertexOffset, 0, DirectionalLight.CascadeCountConst);
            },
        };
    }
}
