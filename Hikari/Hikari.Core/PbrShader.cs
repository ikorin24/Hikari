#nullable enable
using Hikari.Internal;
using System;
using V = Hikari.Vertex;

namespace Hikari;

public sealed class PbrShader : Shader<PbrShader, PbrMaterial, PbrLayer>
{
    private static T ShadowShaderSource<TArg, T>(uint cascade, TArg arg, ReadOnlySpanFunc<byte, TArg, T> func)
    {
        using var builder = new Utf8StringBuilder(1024);
        builder.Append("const CASCADE: u32 = "u8);
        builder.Append(cascade);
        builder.Append("""
        u;
        @group(0) @binding(0) var<uniform> model: mat4x4<f32>;
        @group(0) @binding(1) var<storage, read> lightMatrices: array<mat4x4<f32>>;
        @vertex fn vs_main(
            @location(0) pos: vec3<f32>,
        ) -> @builtin(position) vec4<f32> {
            return lightMatrices[CASCADE] * model * vec4<f32>(pos, 1.0);
        }
        """u8);
        return func(builder.Utf8String, arg);
    }

    private static ReadOnlySpan<byte> ShaderSource => """
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
        }

        struct CameraMatrix {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
            inv_proj: mat4x4<f32>,
            inv_view: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> u: UniformValue;
        @group(0) @binding(1) var tex_sampler: sampler;
        @group(0) @binding(2) var albedo_tex: texture_2d<f32>;
        @group(0) @binding(3) var mr_tex: texture_2d<f32>;
        @group(0) @binding(4) var normal_tex: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> c: CameraMatrix;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            let model_view = c.view * u.model;
            let mv33 = mat44_to_33(model_view);
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
            var mrao: vec3<f32> = textureSample(mr_tex, tex_sampler, in.uv).rgb;
            var normal_camera_coord: vec3<f32> = tbn * (textureSample(normal_tex, tex_sampler, in.uv).rgb * 2.0 - 1.0);

            var output: GBuffer;
            output.g0 = vec4(in.pos_camera_coord, mrao.r);
            output.g1 = vec4(normal_camera_coord, mrao.g);
            output.g2 = textureSample(albedo_tex, tex_sampler, in.uv);
            output.g3 = vec4(mrao.b, 1.0, 1.0, 1.0);
            return output;
        }

        fn mat44_to_33(m: mat4x4<f32>) -> mat3x3<f32> {
            return mat3x3<f32>(m[0].xyz, m[1].xyz, m[2].xyz);
        }
        """u8;

    private readonly Own<RenderPipeline>[] _shadowPipeline;
    private readonly Own<ShaderModule>[] _shadowModules;

    private PbrShader(Screen screen, PbrLayer operation)
        : base(
            ShaderSource,
            operation,
            BuildPipeline)
    {
        var cascadeCount = screen.Lights.DirectionalLight.CascadeCount;
        var shadowModules = new Own<ShaderModule>[cascadeCount];
        for(uint i = 0; i < shadowModules.Length; i++) {
            shadowModules[i] = ShadowShaderSource(i, screen, (source, screen) => ShaderModule.Create(screen, source));
        }

        var shadowPipeline = new Own<RenderPipeline>[cascadeCount];
        for(var i = 0; i < shadowPipeline.Length; i++) {
            shadowPipeline[i] = CreateShadowPipeline(operation, shadowModules[i].AsValue());
        }
        _shadowPipeline = shadowPipeline;
        _shadowModules = shadowModules;
    }

    public static Own<PbrShader> Create(Screen screen, PbrLayer operation)
    {
        var self = new PbrShader(screen, operation);
        return CreateOwn(self);
    }

    public RenderPipeline ShadowPipeline(uint cascade) => _shadowPipeline[cascade].AsValue();

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            foreach(var item in _shadowModules) {
                item.Dispose();
            }
            foreach(var item in _shadowPipeline) {
                item.Dispose();
            }
        }
    }

    private static Own<RenderPipeline> CreateShadowPipeline(PbrLayer operation, ShaderModule module)
    {
        var screen = operation.Screen;
        return RenderPipeline.Create(screen, new()
        {
            Layout = operation.ShadowPipelineLayout,
            Vertex = new VertexState
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<V>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                    }),
                },
            },
            Fragment = null,
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
    }

    private static RenderPipelineDescriptor BuildPipeline(PipelineLayout pipelineLayout, ShaderModule module)
    {
        var screen = pipelineLayout.Screen;
        return new RenderPipelineDescriptor
        {
            Layout = pipelineLayout,
            Vertex = new VertexState
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<V>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.Normal),
                        (2, VertexFieldSemantics.UV),
                    }),
                    new VertexBufferLayout
                    {
                        ArrayStride = (ulong)Vector3.SizeInBytes,
                        Attributes = new VertexAttr[]
                        {
                            new VertexAttr
                            {
                                Format = VertexFormat.Float32x3,
                                Offset = 0,
                                ShaderLocation = 3,
                            },
                        },
                        StepMode = VertexStepMode.Vertex,
                    },
                },
            },
            Fragment = new FragmentState
            {
                Module = module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = new ColorTargetState?[]
                {
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
    }
}
