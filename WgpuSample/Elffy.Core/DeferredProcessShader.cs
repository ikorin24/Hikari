#nullable enable
using System;
using V = Elffy.VertexSlim;

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
        struct CameraMatrix {
            proj: mat4x4<f32>,
            view: mat4x4<f32>,
            inv_proj: mat4x4<f32>,
            inv_view: mat4x4<f32>,
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
        @group(1) @binding(0) var<uniform> camera: CameraMatrix;
        @group(2) @binding(0) var<storage> dir_light: DirLightData;
        @group(3) @binding(0) var shadowmap: texture_depth_2d;
        @group(3) @binding(1) var sm_sampler: sampler_comparison;
        @group(3) @binding(2) var<storage, read> lightMatrices: array<mat4x4<f32>>;
        @group(3) @binding(3) var<storage, read> cascadeFars: array<f32>;

        @vertex fn vs_main(
            v: Vin,
        ) -> V2F {
            var output: V2F;
            output.clip_pos = vec4(v.pos, 1.0);
            output.uv = v.uv;
            return output;
        }
        const PI = 3.141592653589793;
        const INV_PI = 0.3183098861837907;
        const DIELECTRIC_F0 = 0.04;
        const INV_U32_MAX_VALUE = 2.3283064E-10;    // 1.0 / u32.max_value

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

            // Shadow
            var shadow_visibility: f32 = 1.0;

            let distance: f32 = length(pos_camera_coord);
            let cascade_count: u32 = arrayLength(&cascadeFars);
            var cascade: u32 = cascade_count;
            for(var i: u32 = 0u; i < cascade_count; i++) {
                if(distance < cascadeFars[i]) {
                    cascade = i;
                    break;
                }
            }
            
            var visibility: f32 = 1.0;
            if(cascade < cascade_count) {
                let pos_world_coord = camera.inv_view * vec4(pos_camera_coord, 1.0);
                let p = to_vec3(lightMatrices[cascade] * pos_world_coord);
                let shadowmap_pos = vec3<f32>(
                    p.x * 0.5 + 0.5,
                    -p.y * 0.5 + 0.5,
                    p.z);
                let bias = 0.001;       // TODO:
                let slope_scaled_bias = bias * acos(cos(dot_nl));
                var vis: f32 = 0.0;

                let shadowmap_size: vec2<i32> = textureDimensions(shadowmap, 0);
                let sm_size_inv = vec2<f32>(
                    1.0 / f32(shadowmap_size.x) / f32(cascade_count),
                    1.0 / f32(shadowmap_size.y),
                );

                // PCF
                var seed: vec2<u32> = random_vec2_u32(in.uv);
                let sample_count: i32 = 4;
                let ref_z = shadowmap_pos.z - slope_scaled_bias;
                let R = 2.0;
                let u32_max_inv = 2.3283064E-10;    // 1.0 / u32.maxvalue
                for(var i: i32 = 0; i < sample_count; i++) {
                    seed ^= (seed << 13u); seed ^= (seed >> 17u); seed ^= (seed << 5u);
                    let r = R * sqrt(f32(seed.x) * u32_max_inv);
                    let offset = vec2<f32>(
                        r * cos(2.0 * PI * f32(seed.y) * u32_max_inv),
                        r * sin(2.0 * PI * f32(seed.y) * u32_max_inv),
                    );
                    var shadow_uv = shadowmap_pos.xy + offset * sm_size_inv;
                    if(shadow_uv.x < 0.0 || shadow_uv.x > 1.0 || shadow_uv.y < 0.0 || shadow_uv.y > 1.0 || ref_z > 1.0 || ref_z < 0.0) {
                        vis += 1.0;
                    }
                    else {
                        shadow_uv.x = shadow_uv.x / f32(cascade_count) + f32(cascade) / f32(cascade_count);
                        vis += textureSampleCompareLevel(shadowmap, sm_sampler, shadow_uv, ref_z);
                    }
                }
                visibility = vis / f32(sample_count);
            }

            if (cascade == cascade_count - 1u) {
                let coeff: f32 = (cascadeFars[cascade] - distance) / (cascadeFars[cascade] - cascadeFars[cascade - 1u]);
                visibility = 1.0 - (1.0 - visibility) * coeff;
            }

            fragColor = (diffuse + specular) * visibility;

            fragColor *= c3.r;  // ao
            let ssao: f32 = 1.0;
            fragColor *= ssao;

            var out: Fout;
            out.color = vec4<f32>(fragColor, 1.0);
            let pos_dnc = camera.proj * vec4(pos_camera_coord, 1.0);
            out.depth = (pos_dnc.z / pos_dnc.w) * 0.5 + 0.5;
            return out;
        }

        fn to_vec3(v: vec4<f32>) -> vec3<f32> {
            return v.xyz / v.w;
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

        // Return random vec2<u32> in [0, u32.maxvalue]
        fn random_vec2_u32(p: vec2<f32>) -> vec2<u32> {
            let K: vec2<u32> = vec2<u32>(0x456789abu, 0x6789ab45u);

            var n: vec2<u32> = bitcast<vec2<u32>>(p);
            n ^= (n.yx << 9u);
            n ^= (n.yx >> 1u);
            n *= K;
            n ^= (n.yx << 1u);
            n *= K;
            return n;
        }
        """u8;

    private DeferredProcessShader(RenderOperation<DeferredProcessShader, DeferredProcessMaterial> operation)
        : base(
            ShaderSource,
            operation,
            Desc)
    {
    }

    internal static Own<DeferredProcessShader> Create(RenderOperation<DeferredProcessShader, DeferredProcessMaterial> operation)
    {
        var shader = new DeferredProcessShader(operation);
        return CreateOwn(shader);
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
        }
    }

    internal BindGroup[] CreateBindGroups(GBuffer gBuffer, out IDisposable[] disposables)
    {
        var screen = Screen;
        var directionalLight = screen.Lights.DirectionalLight;

        var bindGroups = new BindGroup[]
        {
            // [0]
            BindGroup.Create(screen, new()
            {
                Layout = ((DeferredProcess)Operation).BindGroupLayout0, // TODO:
                Entries = new BindGroupEntry[]
                {
                    BindGroupEntry.Sampler(0, Sampler.Create(screen, new()
                    {
                        AddressModeU = AddressMode.ClampToEdge,
                        AddressModeV = AddressMode.ClampToEdge,
                        AddressModeW = AddressMode.ClampToEdge,
                        MagFilter = FilterMode.Nearest,
                        MinFilter = FilterMode.Nearest,
                        MipmapFilter = FilterMode.Nearest,
                    }).AsValue(out var sampler)),
                    BindGroupEntry.TextureView(1, gBuffer.ColorAttachment(0).View),
                    BindGroupEntry.TextureView(2, gBuffer.ColorAttachment(1).View),
                    BindGroupEntry.TextureView(3, gBuffer.ColorAttachment(2).View),
                    BindGroupEntry.TextureView(4, gBuffer.ColorAttachment(3).View),
                },
            }).AsValue(out var bindGroup0),
            // [1]
            screen.Camera.CameraDataBindGroup,
            // [2]
            screen.Lights.DataBindGroup,
            // [3]
            BindGroup.Create(screen, new()
            {
                Layout = ((DeferredProcess)Operation).BindGroupLayout3, // TODO:
                Entries = new[]
                {
                    BindGroupEntry.TextureView(0, directionalLight.ShadowMap.View),
                    BindGroupEntry.Sampler(1, Sampler.Create(screen, new()
                    {
                        AddressModeU = AddressMode.ClampToEdge,
                        AddressModeV = AddressMode.ClampToEdge,
                        AddressModeW = AddressMode.ClampToEdge,
                        MagFilter = FilterMode.Nearest,
                        MinFilter = FilterMode.Nearest,
                        MipmapFilter = FilterMode.Nearest,
                        Compare = CompareFunction.Less,
                    }).AsValue(out var shadowSampler)),
                    BindGroupEntry.Buffer(2, directionalLight.LightMatricesBuffer),
                    BindGroupEntry.Buffer(3, directionalLight.CascadeFarsBuffer),
                },
            }).AsValue(out var bindGroup3),
        };
        disposables = new IDisposable[]
        {
            sampler,
            bindGroup0,
            shadowSampler,
            bindGroup3
        };
        return bindGroups;
    }

    private static RenderPipelineDescriptor Desc(PipelineLayout pipelineLayout, ShaderModule module)
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
                        (1, VertexFieldSemantics.UV),
                    }),
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
                        Format = screen.SurfaceFormat,
                        Blend = null,
                        WriteMask = ColorWrites.All,
                    }
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
