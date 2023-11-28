#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using V = Hikari.Vertex;

namespace Hikari;

public sealed class PbrShader : Shader
{
    private static readonly Lazy<ImmutableArray<byte>> _source = new(() =>
    {
        using var sb = Utf8StringBuilder.FromLines(
            ShaderSource.Fn_Inverse3x3,
            Source);
        return sb.Utf8String.ToImmutableArray();
    });

    private static ReadOnlySpan<byte> Source => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) normal: vec3<f32>,
            @location(2) uv: vec2<f32>,
            @location(3) tangent: vec3<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) pos_camera_coord: vec3<f32>,
            @location(1) uv: vec2<f32>,
            @location(2) tangent_camera_coord: vec3<f32>,
            @location(3) bitangent_camera_coord: vec3<f32>,
            @location(4) normal_camera_coord: vec3<f32>,
        }

        struct GBuffer {
            @location(0) g0 : vec4<f32>,
            @location(1) g1 : vec4<f32>,
            @location(2) g2 : vec4<f32>,
            @location(3) g3 : vec4<f32>,
        }
        struct UniformValue {
            model: mat4x4<f32>,
            is_uniform_scale: i32,
        }

        struct CameraMatrix {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
            inv_proj: mat4x4<f32>,
            inv_view: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> u: UniformValue;
        @group(0) @binding(1) var albedo_tex: texture_2d<f32>;
        @group(0) @binding(2) var albedo_sampler: sampler;
        @group(0) @binding(3) var mr_tex: texture_2d<f32>;
        @group(0) @binding(4) var mr_sampler: sampler;
        @group(0) @binding(5) var normal_tex: texture_2d<f32>;
        @group(0) @binding(6) var normal_sampler: sampler;
        @group(1) @binding(0) var<uniform> c: CameraMatrix;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            let model_view = c.view * u.model;
            var mv33 = mat44_to_33(model_view);
            if(u.is_uniform_scale == 0) {
                mv33 = transpose(Inverse3x3(mv33));
            }
            var output: V2F;
            let pos4: vec4<f32> = model_view * vec4(v.pos, 1.0);

            output.clip_pos = c.proj * pos4;
            output.pos_camera_coord = pos4.xyz / pos4.w;
            output.uv = v.uv;
            output.tangent_camera_coord = normalize(mv33 * v.tangent);
            output.bitangent_camera_coord = normalize(mv33 * cross(v.normal, v.tangent));
            output.normal_camera_coord = normalize(mv33 * v.normal);
            return output;
        }

        @fragment fn fs_main(in: V2F) -> GBuffer {
            // TBN matrix: tangent space -> camera space
            var tbn = mat3x3<f32>(in.tangent_camera_coord, in.bitangent_camera_coord, in.normal_camera_coord);
            var mrao: vec3<f32> = textureSample(mr_tex, mr_sampler, in.uv).rgb;
            var normal_camera_coord: vec3<f32> = tbn * (textureSample(normal_tex, normal_sampler, in.uv).rgb * 2.0 - 1.0);

            var output: GBuffer;
            output.g0 = vec4(in.pos_camera_coord, mrao.r);
            output.g1 = vec4(normal_camera_coord, mrao.g);
            output.g2 = textureSample(albedo_tex, albedo_sampler, in.uv);
            output.g3 = vec4(mrao.b, 1.0, 1.0, 1.0);
            return output;
        }

        fn mat44_to_33(m: mat4x4<f32>) -> mat3x3<f32> {
            return mat3x3<f32>(m[0].xyz, m[1].xyz, m[2].xyz);
        }
        """u8;

    private static readonly Lazy<ImmutableArray<byte>> _shadowShader = new(() =>
    {
        using var sb = new Utf8StringBuilder();
        sb.Append("const CASCADE_COUNT: u32 = "u8);
        sb.Append(DirectionalLight.CascadeCountConst);
        sb.AppendLine("u;"u8);

        sb.Append("const SM_WIDTH = "u8);
        sb.Append(DirectionalLight.ShadowMapWidth);
        sb.AppendLine(";"u8);

        sb.AppendLine("""
            struct V2F {
                @builtin(position) clip_pos: vec4<f32>,
                @location(0) cascade: u32,
            }

            @group(0) @binding(0) var<uniform> model: mat4x4<f32>;
            @group(0) @binding(1) var<storage, read> lightMatrices: array<mat4x4<f32>>;
            @vertex fn vs_main(
                @location(0) pos: vec3<f32>,
                @builtin(instance_index) cascade: u32,
            ) -> V2F {
                let p: vec4<f32> = lightMatrices[cascade] * model * vec4<f32>(pos, 1.0);
                let p3: vec3<f32> = p.xyz / p.w;
                let x = (p3.x * 0.5 + 0.5 + f32(cascade)) / (f32(CASCADE_COUNT) * 0.5) - 1.0;
                var v2f: V2F;
                v2f.clip_pos = vec4<f32>(x, p3.y, p3.z, 1.0);
                v2f.cascade = cascade;
                return v2f;
            }

            struct Fout {
                @location(0) color: vec2<f32>,
                @builtin(frag_depth) depth: f32,
            }

            @fragment fn fs_main(
                v2f: V2F,
            ) -> Fout {
                if(v2f.clip_pos.x < f32(v2f.cascade) * f32(SM_WIDTH)) {
                    discard;
                }
                if(v2f.clip_pos.x >= f32(v2f.cascade + 1u) * f32(SM_WIDTH)) {
                    discard;
                }
                var output: Fout;
                //output.color = vec4<f32>(v2f.clip_pos.z, v2f.clip_pos.z * v2f.clip_pos.z, 0.0, 0.0);
                output.color = vec2<f32>(v2f.clip_pos.z, v2f.clip_pos.z * v2f.clip_pos.z);
                output.depth = v2f.clip_pos.z;
                return output;
            }
            """u8);
        return sb.Utf8String.ToImmutableArray();
    });

    private readonly IGBufferProvider _gbufferProvider;
    public IGBufferProvider GBufferProvider => _gbufferProvider;

    private PbrShader(Screen screen, IGBufferProvider gBufferProvider)
        : base(
            screen,
            [
                new()
                {
                    Source = _shadowShader.Value,
                    LayoutDescriptor = GetShadowLayoutDesc(screen, out var disposable),
                    PipelineDescriptorFactory = GetShadowDescriptor,
                    SortOrder = -1000,
                    PassKind = PassKind.ShadowMap,
                    OnRenderPass = (in RenderPass renderPass, RenderPipeline pipeline, Material material, Mesh mesh, in SubmeshData submesh, int passIndex) =>
                    {
                        renderPass.SetPipeline(pipeline);
                        renderPass.SetBindGroups(material.GetBindGroups(passIndex));
                        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                        renderPass.DrawIndexed(submesh.IndexOffset, submesh.IndexCount, submesh.VertexOffset, 0, DirectionalLight.CascadeCountConst);
                    },
                },
                new()
                {
                    Source = _source.Value,
                    LayoutDescriptor = GetLayoutDesc(screen, out var disposable2),
                    PipelineDescriptorFactory = static (module, layout) => GetDescriptor(module, layout, module.Screen.DepthStencil.Format),
                    SortOrder = 0,
                    PassKind = PassKind.GBuffer,
                    OnRenderPass = (in RenderPass renderPass, RenderPipeline pipeline, Material material, Mesh mesh, in SubmeshData submesh, int passIndex) =>
                    {
                        renderPass.SetPipeline(pipeline);
                        renderPass.SetBindGroups(material.GetBindGroups(passIndex));
                        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                        Debug.Assert(mesh.TangentBuffer.HasValue);
                        renderPass.SetVertexBuffer(1, mesh.TangentBuffer.Value);
                        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                        renderPass.DrawIndexed(submesh.IndexOffset, submesh.IndexCount, submesh.VertexOffset, 0, 1);
                    },
                },
            ])
    {
        _gbufferProvider = gBufferProvider;
        disposable2.DisposeOn(Disposed);
        disposable.DisposeOn(Disposed);
    }

    public static Own<PbrShader> Create(Screen screen, IGBufferProvider gBufferProvider)
    {
        ArgumentNullException.ThrowIfNull(gBufferProvider);
        var self = new PbrShader(screen, gBufferProvider);
        return CreateOwn(self);
    }


    private static PipelineLayoutDescriptor GetLayoutDesc(
        Screen screen,
        out DisposableBag disposable)
    {
        disposable = new DisposableBag();
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = [
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new()
                        {
                            Type = BufferBindingType.Uniform,
                        }),
                        BindGroupLayoutEntry.Texture(1, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(2, ShaderStages.Fragment, SamplerBindingType.Filtering),
                        BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(4, ShaderStages.Fragment, SamplerBindingType.Filtering),
                        BindGroupLayoutEntry.Texture(5, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Sampler(6, ShaderStages.Fragment, SamplerBindingType.Filtering),
                    ],
                }).AddTo(disposable),
                screen.Camera.CameraDataBindGroupLayout,
            ],
        };
    }

    private static RenderPipelineDescriptor GetDescriptor(ShaderModule module, PipelineLayout layout, TextureFormat depthStencilFormat)
    {
        return new RenderPipelineDescriptor
        {
            Layout = layout,
            Vertex = new VertexState
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToImmutableArray(),
                Buffers =
                [
                    VertexBufferLayout.FromVertex<V>(
                    [
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.Normal),
                        (2, VertexFieldSemantics.UV),
                    ]),
                    new VertexBufferLayout
                    {
                        ArrayStride = (ulong)Vector3.SizeInBytes,
                        Attributes =
                        [
                            new()
                            {
                                Format = VertexFormat.Float32x3,
                                Offset = 0,
                                ShaderLocation = 3,
                            },
                        ],
                        StepMode = VertexStepMode.Vertex,
                    },
                ],
            },
            Fragment = new FragmentState
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToImmutableArray(),
                Targets =
                [
                    new ColorTargetState
                    {
                        Format = TextureFormat.Rgba32Float,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    },
                    new ColorTargetState
                    {
                        Format = TextureFormat.Rgba32Float,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    },
                    new ColorTargetState
                    {
                        Format = TextureFormat.Rgba32Float,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    },
                    new ColorTargetState
                    {
                        Format = TextureFormat.Rgba32Float,
                        Blend = null,
                        WriteMask = ColorWrites.All,
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
            DepthStencil = new DepthStencilState
            {
                Format = depthStencilFormat,
                DepthWriteEnabled = true,
                DepthCompare = CompareFunction.Greater,
                Stencil = StencilState.Default,
                Bias = DepthBiasState.Default,
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        };
    }

    private static PipelineLayoutDescriptor GetShadowLayoutDesc(
        Screen screen,
        out DisposableBag disposable)
    {
        disposable = new DisposableBag();
        return new()
        {
            BindGroupLayouts =
            [
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex, new() { Type = BufferBindingType.StorageReadOnly }),
                    ],
                }).AddTo(disposable),
            ],
        };
    }

    private static RenderPipelineDescriptor GetShadowDescriptor(ShaderModule module, PipelineLayout layout)
    {
        var screen = module.Screen;
        return new()
        {
            Layout = layout,
            Vertex = new VertexState
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToImmutableArray(),
                Buffers =
                [
                    VertexBufferLayout.FromVertex<V>(
                    [
                        (0, VertexFieldSemantics.Position),
                    ]),
                ],
            },
            Fragment = new FragmentState
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToImmutableArray(),
                Targets = [
                    new ColorTargetState
                    {
                        Format = TextureFormat.Rg16Float,
                        Blend = null,
                        WriteMask = ColorWrites.All,
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
        };
    }
}
