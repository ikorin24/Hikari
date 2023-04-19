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
        struct Fout {
            @location(0) color: vec4<f32>,
            @builtin(frag_depth) depth: f32,
        }
        struct CameraMat {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
        }
        @group(0) @binding(0) var g_sampler: sampler;
        @group(0) @binding(1) var g0: texture_2d<f32>;
        @group(0) @binding(2) var g1: texture_2d<f32>;
        @group(0) @binding(3) var g2: texture_2d<f32>;
        @group(0) @binding(4) var g3: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> camera: CameraMat;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = vec4(v.pos, 1.0);
            output.uv = v.uv;
            return output;
        }
        const INV_PI = 0.3183098861837907;
        const DIELECTRIC_F0 = 0.04;

        @fragment fn fs_main(in: V2F) -> Fout {
            var c0: vec4<f32> = textureSample(g0, g_sampler, in.uv);
            var c1: vec4<f32> = textureSample(g1, g_sampler, in.uv);
            var c2: vec4<f32> = textureSample(g2, g_sampler, in.uv);
            var c3: vec4<f32> = textureSample(g3, g_sampler, in.uv);
            var pos_camera_coord: vec3<f32> = c0.rgb;
            var n: vec3<f32> = c1.rgb;    // normal direction in eye space, normalized
            var albedo: vec3<f32> = c2.rgb;
            var metallic: f32 = c0.a;
            var roughness: f32 = c1.a;
            var alpha: f32 = roughness * roughness;
            var v: vec3<f32> = -normalize(pos_camera_coord);    // camera direction in camera space, normalized
            var dot_nv: f32 = abs(dot(n, v));
            var reflectivity: f32 = mix(DIELECTRIC_F0, 1.0, metallic);
            var f0: vec3<f32> = mix(vec3<f32>(DIELECTRIC_F0, DIELECTRIC_F0, DIELECTRIC_F0), albedo, metallic);
            
            var out: Fout;
            out.color = vec4(c1.rgb, 1.0);

            var pos_dnc = camera.proj * vec4(pos_camera_coord, 1.0);
            out.depth = (pos_dnc.z / pos_dnc.w) * 0.5 + 0.5;
            return out;
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

    private DeferredProcessShader(Screen screen)
        : base(
            screen,
            ShaderSource,
            BuildPipelineLayoutDescriptor(screen, out var bindGroupLayout0))
    {
        _bindGroupLayout0 = bindGroupLayout0;
    }

    internal static Own<DeferredProcessShader> Create(Screen screen)
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

    internal BindGroup[] CreateBindGroups(GBuffer gBuffer, out IDisposable[] disposables)
    {
        var screen = Screen;
        var sampler = Sampler.NoMipmap(screen, AddressMode.ClampToEdge, FilterMode.Nearest, FilterMode.Nearest);
        var bg0 = BindGroup.Create(screen, new()
        {
            Layout = _bindGroupLayout0.AsValue(),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Sampler(0, sampler.AsValue()),
                BindGroupEntry.TextureView(1, gBuffer.ColorAttachment(0).View),
                BindGroupEntry.TextureView(2, gBuffer.ColorAttachment(1).View),
                BindGroupEntry.TextureView(3, gBuffer.ColorAttachment(2).View),
                BindGroupEntry.TextureView(4, gBuffer.ColorAttachment(3).View),
            },
        });
        disposables = new IDisposable[]
        {
            sampler,
            bg0,
        };
        return new BindGroup[]
        {
            bg0.AsValue(),
            screen.Camera.CameraDataBindGroup,
        };
    }

    private static PipelineLayoutDescriptor BuildPipelineLayoutDescriptor(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0)
    {
        bindGroupLayout0 = BindGroupLayout.Create(screen, _bindGroupLayoutDesc0);
        return new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                bindGroupLayout0.AsValue(),
                screen.Camera.CameraDataBindGroupLayout,
            },
        };
    }
}
