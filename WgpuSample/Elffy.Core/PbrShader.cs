#nullable enable
using System;

namespace Elffy;

public sealed class PbrShader : Shader<PbrShader, PbrMaterial>
{
    private static ReadOnlySpan<byte> ShadowShaderSource => """
        @group(0) @binding(0) var<uniform> model: mat4x4<f32>;
        @group(0) @binding(1) var<uniform> lightMat: mat4x4<f32>;

        @vertex fn vs_main(
            @location(0) pos: vec3<f32>,
        ) -> @builtin(position) vec4<f32> {
            return lightMat * model * vec4<f32>(pos, 1.0);
        }
        @fragment fn fs_main() {
        }
        """u8;

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

        struct CameraMat {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> u: UniformValue;
        @group(0) @binding(1) var tex_sampler: sampler;
        @group(0) @binding(2) var albedo_tex: texture_2d<f32>;
        @group(0) @binding(3) var mr_tex: texture_2d<f32>;
        @group(0) @binding(4) var normal_tex: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> c: CameraMat;
        @group(2) @binding(0) var shadowmap: texture_depth_2d;
        @group(2) @binding(1) var shadowmap_sampler: sampler;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var model_view = c.view * u.model;
            var mv33 = mat44_to_33(model_view);
            var output: V2F;
            var pos4: vec4<f32> = model_view * vec4(v.pos, 1.0);
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
            return mat3x3<f32>(
                vec3<f32>(m[0].x, m[0].y, m[0].z),
                vec3<f32>(m[1].x, m[1].y, m[1].z),
                vec3<f32>(m[2].x, m[2].y, m[2].z),
            );
        }
        """u8;

    private readonly Own<ShaderModule> _shadowModule;
    private readonly Own<PipelineLayout> _shadowPipelineLayout;
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly BindGroupLayout _bindGroupLayout1;
    private readonly Own<BindGroupLayout> _bindGroupLayout2;

    private readonly Own<BindGroupLayout> _shadowBindGroupLayout0;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1;
    public BindGroupLayout BindGroupLayout2 => _bindGroupLayout2.AsValue();

    public BindGroupLayout ShadowBindGroupLayout0 => _shadowBindGroupLayout0.AsValue();

    public ShaderModule ShadowModule => _shadowModule.AsValue();
    public PipelineLayout ShadowPipelineLayout => _shadowPipelineLayout.AsValue();

    private PbrShader(Screen screen)
        : base(
            screen,
            ShaderSource,
            BuildPipelineLayoutDescriptor(
                screen,
                out var bindGroupLayout0,
                out var bindGroupLayout1,
                out var bindGroupLayout2))
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;
        _bindGroupLayout2 = bindGroupLayout2;
        _shadowModule = ShaderModule.Create(screen, ShadowShaderSource);
        _shadowPipelineLayout = BuildShadowPipeline(screen, out var shadowBgl0);
        _shadowBindGroupLayout0 = shadowBgl0;
    }

    public override void Validate()
    {
        base.Validate();
        _bindGroupLayout1.Validate();
    }

    public static Own<PbrShader> Create(Screen screen)
    {
        var self = new PbrShader(screen);
        return CreateOwn(self);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroupLayout0.Dispose();
            _bindGroupLayout2.Dispose();
            _shadowBindGroupLayout0.Dispose();
            _shadowModule.Dispose();
            _shadowPipelineLayout.Dispose();
        }
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0,
        out BindGroupLayout bindGroupLayout1,
        out Own<BindGroupLayout> bindGroupLayout2)
    {
        bindGroupLayout1 = screen.Camera.CameraDataBindGroupLayout;
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new BufferBindingData
                        {
                            Type = BufferBindingType.Uniform,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Fragment, SamplerBindingType.Filtering),
                        BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(4, ShaderStages.Fragment, new TextureBindingData
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.FloatFilterable,
                        }),
                    },
                }).AsValue(out bindGroupLayout0),
                bindGroupLayout1,
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.Depth,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Fragment, SamplerBindingType.Comparison),
                    },
                }).AsValue(out bindGroupLayout2),
            },
        };
    }

    private static Own<PipelineLayout> BuildShadowPipeline(
        Screen screen,
        out Own<BindGroupLayout> bgl0)
    {
        return PipelineLayout.Create(screen, new()
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
                    },
                }).AsValue(out bgl0),
            },
        });
    }
}
