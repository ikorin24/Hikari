#nullable enable
using System;

namespace Elffy;

public sealed class PbrShader : Shader<PbrShader, PbrMaterial>
{
    private static ReadOnlySpan<byte> ShaderSource => """
        struct Vin {
            @location(0) pos: vec3<f32>,
            @location(1) uv: vec2<f32>,
        }
        struct V2F {
            @builtin(position) clip_pos: vec4<f32>,
            @location(0) uv: vec2<f32>,
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
        @group(0) @binding(2) var albedo: texture_2d<f32>;
        @group(0) @binding(3) var mr: texture_2d<f32>;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = u.proj * u.view * u.model * vec4(v.pos, 1.0);
            output.uv = v.uv;
            return output;
        }

        @fragment fn fs_main(in: V2F) -> GBuffer {
            var output: GBuffer;
            output.g0 = vec4(1.0, 1.0, 1.0, 1.0);
            output.g1 = vec4(1.0, 1.0, 1.0, 1.0);
            output.g2 = textureSample(albedo, tex_sampler, in.uv);
            output.g3 = textureSample(mr, tex_sampler, in.uv);
            return output;
        }
        """u8;

    private static readonly BindGroupLayoutDescriptor _bglDesc = new()
    {
        Entries = new[]
        {
            BindGroupLayoutEntry.Buffer(
                0,
                ShaderStages.Vertex,
                new BufferBindingData
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
