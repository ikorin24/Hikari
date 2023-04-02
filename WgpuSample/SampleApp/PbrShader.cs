#nullable enable
using System;

namespace Elffy;

public sealed class PbrShader : Shader<PbrShader, PbrMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) normal: vec3<f32>,
            @location(2) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) pos_camera_coord: vec3<f32>,
            @location(1) normal: vec3<f32>,
            @location(2) uv: vec2<f32>,

            // TBN matrix: tangent space -> camera space
            @location(3) tbn_t: vec3<f32>,
            @location(4) tbn_b: vec3<f32>,
            @location(5) tbn_n: vec3<f32>,
        }
        struct GBuffer {
            @location(0) g0 : vec4<f32>,
            @location(1) g1 : vec4<f32>,
            @location(2) g2 : vec4<f32>,
            @location(3) g3 : vec4<f32>,
        }
        struct UniformValue {
            model: mat4x4<f32>,
            view: mat4x4<f32>,
            proj: mat4x4<f32>,
        }

        @group(0) @binding(0) var<uniform> u: UniformValue;
        @group(0) @binding(1) var tex_sampler: sampler;
        @group(0) @binding(2) var albedo_tex: texture_2d<f32>;
        @group(0) @binding(3) var mr_tex: texture_2d<f32>;
        @group(0) @binding(4) var normal_tex: texture_2d<f32>;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var model_view = u.view * u.model;
            var mv33 = mat44_to_33(model_view);
            var output: V2F;
            var pos4: vec4<f32> = model_view * vec4(v.pos, 1.0);
            output.pos_camera_coord = pos4.xyz / pos4.w;
            output.clip_pos = u.proj * pos4;
            output.normal = v.normal;
            output.uv = v.uv;
            return output;
        }

        @fragment fn fs_main(in: V2F) -> GBuffer {
            var mr: vec2<f32> = textureSample(mr_tex, tex_sampler, in.uv).rg;
            var output: GBuffer;
            output.g0 = vec4(in.pos_camera_coord, mr.r);
            output.g1 = vec4(1.0, 1.0, 1.0, mr.g);
            output.g2 = textureSample(albedo_tex, tex_sampler, in.uv);
            output.g3 = vec4(1.0, 1.0, 1.0, 1.0);
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

    private static readonly BindGroupLayoutDescriptor _bglDesc = new()
    {
        Entries = new[]
        {
            BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new BufferBindingData
            {
                HasDynamicOffset = false,
                MinBindingSize = 0,
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
    };

    private PbrShader(Screen screen) : base(screen, in _bglDesc, ShaderSource)
    {

    }

    public static Own<PbrShader> Create(Screen screen)
    {
        var self = new PbrShader(screen);
        return CreateOwn(self);
    }
}
