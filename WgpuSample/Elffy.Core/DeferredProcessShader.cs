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
        struct DirLightData {
            dir: vec4<f32>,
            color: vec4<f32>,
        }
        @group(0) @binding(0) var g_sampler: sampler;
        @group(0) @binding(1) var g0: texture_2d<f32>;
        @group(0) @binding(2) var g1: texture_2d<f32>;
        @group(0) @binding(3) var g2: texture_2d<f32>;
        @group(0) @binding(4) var g3: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> camera: CameraMat;
        @group(2) @binding(0) var<storage> dir_light: DirLightData;

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
            let c0: vec4<f32> = textureSample(g0, g_sampler, in.uv);
            let c1: vec4<f32> = textureSample(g1, g_sampler, in.uv);
            let c2: vec4<f32> = textureSample(g2, g_sampler, in.uv);
            let c3: vec4<f32> = textureSample(g3, g_sampler, in.uv);
            let pos_camera_coord: vec3<f32> = c0.rgb;
            let n: vec3<f32> = c1.rgb;    // normal direction in eye space, normalized
            let albedo: vec3<f32> = c2.rgb;
            let metallic: f32 = c0.a;
            let roughness: f32 = c1.a;
            let alpha: f32 = roughness * roughness;
            let v: vec3<f32> = -normalize(pos_camera_coord);    // camera direction in camera space, normalized
            let dot_nv: f32 = abs(dot(n, v));
            let reflectivity: f32 = mix(DIELECTRIC_F0, 1.0, metallic);
            let f0: vec3<f32> = mix(vec3<f32>(DIELECTRIC_F0, DIELECTRIC_F0, DIELECTRIC_F0), albedo, metallic);

            var fragColor: vec3<f32>;

            let l: vec3<f32> = mat44_to_33(camera.view) * (-dir_light.dir.xyz);
            
            let l_color = dir_light.color.rgb;
            let h = normalize(v + l);                  // half vector in eye space, normalized
            let dot_nl: f32 = max(0.0, dot(n, l));
            let dot_lh: f32 = max(0.0, dot(l, h));
            let irradiance: vec3<f32> = dot_nl * l_color;

            // Diffuse
            // You can use Burley instead of Lambert.
            let diffuse: vec3<f32> = (1.0 - reflectivity) * Lambert() * irradiance * albedo;

            // Specular
            let dot_nh: f32 = dot(n, h);
            let V = SmithGGXCorrelated(dot_nl, dot_nv, alpha);
            let D = GGX(n, h, dot_nh, roughness) * step(0.0, dot_nh);
            let F = FresnelSchlick(f0, vec3(1.0, 1.0, 1.0), dot_lh);
            let specular: vec3<f32> = max(vec3<f32>(), V * D * F * irradiance);

            //let bias: f32 = clamp(_shadowMapBias * tan(acos(dot_nl)), 0.0, _shadowMapBias * 10);
            let bias: f32 = 0.0;

            //float shadow = CalcShadow(_lightMatData, _viewInv * vec4(pos, 1), _shadowMap, bias);
            //let shadow: f32 = 0.0;
            let shadow: f32 = 1.0 - c3.g;

            fragColor = (diffuse + specular) * (1.0 - shadow);

            fragColor *= c3.r;  // ao
            let ssao: f32 = 1.0;
            fragColor *= ssao;

            var out: Fout;
            out.color = vec4<f32>(fragColor, 1.0);

            var pos_dnc = camera.proj * vec4(pos_camera_coord, 1.0);
            out.depth = (pos_dnc.z / pos_dnc.w) * 0.5 + 0.5;
            return out;
        }

        fn Lambert() -> f32 {
            return INV_PI;
        }

        fn SmithGGXCorrelated(dot_nl: f32, dot_nv: f32, alpha: f32) -> f32 {
            // For optimization, we will approximate the following expression.
            // (This approximation is not mathematically correct, but it works fine.)
            // let a2 = alpha * alpha;
            // let lambdaV = dot_nl * sqrt((-dot_nv * a2 + dot_nv) * dot_nv + a2);
            // let lambdaL = dot_nv * sqrt((-dot_nl * a2 + dot_nl) * dot_nl + a2);


            let EPSILON: f32 = 0.0001;
            let beta: f32 = 1.0 - alpha;
            let lambdaV: f32 = dot_nl * (dot_nv * beta + alpha);
            let lambdaL: f32 = dot_nv * (dot_nl * beta + alpha);
            return 0.5 / (lambdaV + lambdaL + EPSILON);
        }

        // TODO: f16
        fn GGX(n: vec3<f32>, h: vec3<f32>, dot_nh: f32, roughness: f32) -> f32    // Trowbridge-Reitz
        {
            let p = roughness * dot_nh;
            let cross_nh = cross(n, h);
            let q = roughness / (dot(cross_nh, cross_nh) + p * p);
            return min(q * q * INV_PI, 65504.0);        // 65504.0 is max value of f16
        }


        // TODO: f16
        fn FresnelSchlick(f0: vec3<f32>, f90: vec3<f32>, u: f32) -> vec3<f32>
        {
            let x = 1.0 - u;
            let x2 = x * x;
            return f0 + (f90 - f0) * (x2 * x2 * x);
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
            screen.Lights.DataBindGroup,
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
                screen.Lights.DataBindGroupLayout,
            },
        };
    }
}
