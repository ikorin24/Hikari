#nullable enable
using System;

namespace Elffy;

public sealed class DeferredProcessShader : Shader<DeferredProcessShader, DeferredProcessMaterial>
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
        @group(0) @binding(0) var g_sampler: sampler;
        @group(0) @binding(1) var g0: texture_2d<f32>;
        @group(0) @binding(2) var g1: texture_2d<f32>;
        @group(0) @binding(3) var g2: texture_2d<f32>;
        @group(0) @binding(4) var g3: texture_2d<f32>;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = vec4(v.pos, 1.0);
            output.uv = v.uv;
            return output;
        }

        @fragment fn fs_main(in: V2F) -> @location(0) vec4<f32> {
            var c0: vec3<f32> = textureSample(g0, g_sampler, in.uv).xyz;
            var c1: vec3<f32> = textureSample(g1, g_sampler, in.uv).xyz;
            var c2: vec3<f32> = textureSample(g2, g_sampler, in.uv).xyz;
            var c3: vec3<f32> = textureSample(g3, g_sampler, in.uv).xyz;
            return vec4(c2, 1.0);
        }
        """u8;

    private static readonly BindGroupLayoutDescriptor _bindGroupLayoutDesc0 = new()
    {
        Entries = new BindGroupLayoutEntry[]
        {
            BindGroupLayoutEntry.Sampler(0, ShaderStages.Fragment, SamplerBindingType.NonFiltering),
            BindGroupLayoutEntry.Texture(1, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }),
            BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }),
            BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }),
            BindGroupLayoutEntry.Texture(4, ShaderStages.Fragment, new TextureBindingData
            {
                Multisampled = false,
                ViewDimension = TextureViewDimension.D2,
                SampleType = TextureSampleType.FloatNotFilterable,
            }),
        }
    };

    private readonly Own<BindGroupLayout> _bindGroupLayout0;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    private DeferredProcessShader(Screen screen)
        : base(screen, ShaderSource, BuildPipelineLayoutDescriptor(screen, out var bindGroupLayout0))
    {
        _bindGroupLayout0 = bindGroupLayout0;
    }

    public static Own<DeferredProcessShader> Create(Screen screen)
    {
        var shader = new DeferredProcessShader(screen);
        return CreateOwn(shader);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _bindGroupLayout0.Dispose();
        }
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(Screen screen, out Own<BindGroupLayout> bindGroupLayout0)
    {
        bindGroupLayout0 = BindGroupLayout.Create(screen, _bindGroupLayoutDesc0);
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout0.AsValue(),
            },
        };
    }
}
