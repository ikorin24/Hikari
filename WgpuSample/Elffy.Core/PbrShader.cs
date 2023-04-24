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

            var mr: vec2<f32> = textureSample(mr_tex, tex_sampler, in.uv).rg;
            var normal_camera_coord: vec3<f32> = tbn * textureSample(normal_tex, tex_sampler, in.uv).rgb * 2.0 - 1.0;
            var output: GBuffer;
            output.g0 = vec4(in.pos_camera_coord, mr.r);
            output.g1 = vec4(normal_camera_coord, mr.g);
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

    private static readonly BindGroupLayoutDescriptor _bindGroupLayoutDesc0 = new()
    {
        Entries = new[]
        {
            BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new BufferBindingData
            {
                HasDynamicOffset = false,
                MinBindingSize = null,
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

    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly BindGroupLayout _bindGroupLayout1;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1;

    private PbrShader(Screen screen)
        : base(
            screen,
            ShaderSource,
            BuildPipelineLayoutDescriptor(screen, out var bindGroupLayout0, out var bindGroupLayout1))
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;
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
        }
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0,
        out BindGroupLayout bindGroupLayout1)
    {
        bindGroupLayout0 = BindGroupLayout.Create(screen, _bindGroupLayoutDesc0);
        bindGroupLayout1 = screen.Camera.CameraDataBindGroupLayout;
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout0.AsValue(),
                bindGroupLayout1,
            },
        };
    }
}
