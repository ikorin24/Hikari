﻿#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;

namespace Hikari;

public static class DeferredProcessShader
{
    private static readonly Lazy<ImmutableArray<byte>> ShaderSource = new(() =>
    {
        return """
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
        struct LightData {
            ambient_strength: f32,
        }
        @group(0) @binding(0) var g_sampler: sampler;
        @group(0) @binding(1) var g0: texture_2d<f32>;
        @group(0) @binding(2) var g1: texture_2d<f32>;
        @group(0) @binding(3) var g2: texture_2d<f32>;
        @group(0) @binding(4) var g3: texture_2d<f32>;
        @group(1) @binding(0) var<uniform> camera: CameraMatrix;
        @group(2) @binding(0) var<storage, read> dir_light: DirLightData;
        @group(2) @binding(1) var<storage, read> light: LightData;
        @group(3) @binding(0) var shadowmap: texture_depth_2d;
        @group(3) @binding(1) var<storage, read> lightMatrices: array<mat4x4<f32>>;
        @group(3) @binding(2) var<storage, read> cascadeFars: array<f32>;

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
            let distance: f32 = length(pos_camera_coord);
            let cascade_count: u32 = arrayLength(&lightMatrices);
            var cascade: u32 = cascade_count;
            for(var i: u32 = 0u; i < cascade_count; i++) {
                if(distance < cascadeFars[i]) {
                    cascade = i;
                    break;
                }
            }
            var visibility: f32;
            if(cascade < cascade_count) {
                let shadow_pos = to_vec3(lightMatrices[cascade] * camera.inv_view * vec4(pos_camera_coord, 1.0));
                let shadow_uv = vec3<f32>(
                    shadow_pos.x * 0.5 + 0.5,
                    -shadow_pos.y * 0.5 + 0.5,
                    shadow_pos.z);
                let bias = 0.005 / (f32(cascade + 1u) * 5.0);
                let ref_z = shadow_uv.z + bias;
                if(shadow_uv.x < 0.0 || shadow_uv.x > 1.0 || shadow_uv.y < 0.0 || shadow_uv.y > 1.0 || ref_z > 1.0 || ref_z < 0.0) {
                    visibility = 1.0;
                }
                else {
                    let atlas_shadow_uv = vec2<f32>(
                        shadow_uv.x / f32(cascade_count) + f32(cascade) / f32(cascade_count),
                        shadow_uv.y,
                    );
                    visibility = calcShadowVisibility(shadowmap, atlas_shadow_uv, cascade, ref_z);
                }
            }
            else {
                visibility = 1.0;
            }

            if (cascade == cascade_count - 1u) {
                // fade out shadow
                let coeff: f32 = (cascadeFars[cascade] - distance) / (cascadeFars[cascade] - cascadeFars[cascade - 1u]);
                visibility = 1.0 - (1.0 - visibility) * coeff;
            }

            fragColor = (diffuse + specular) * visibility + albedo * light.ambient_strength;

            //fragColor *= c3.r;  // ao
            let ssao: f32 = 1.0;
            fragColor *= ssao;

            var out: Fout;
            out.color = vec4<f32>(fragColor, 1.0);
            //out.color = vec4<f32>(visibility, visibility, visibility, 1.0);
            let pos_dnc = camera.proj * vec4(pos_camera_coord, 1.0);
            out.depth = (pos_dnc.z / pos_dnc.w) * 0.5 + 0.5;
            return out;
        }

        fn texture0ManualSampleRG(t: texture_2d<f32>, uv: vec2<f32>, ref_z: f32) -> vec2<f32> {
            let size: vec2<u32> = textureDimensions(t, 0);
            let p: vec2<f32> = uv * vec2<f32>(size);
            let floor_p: vec2<f32> = floor(p);
            let delta: vec2<f32> = p - floor_p;
            let p0 = vec2<u32>(floor_p);
            let p1 = vec2<u32>(p0.x + 1u, p0.y);
            let p2 = vec2<u32>(p0.x, p0.y + 1u);
            let p3 = vec2<u32>(p0.x + 1u, p0.y + 1u);
            let c0 = textureLoad(t, p0, 0).xy;
            let c1 = textureLoad(t, p1, 0).xy;
            let c2 = textureLoad(t, p2, 0).xy;
            let c3 = textureLoad(t, p3, 0).xy;
            return mix(mix(c0, c1, delta.x), mix(c2, c3, delta.x), delta.y);
        }

        fn calcShadowVisibility(tex: texture_depth_2d, uv: vec2<f32>, cascade: u32, ref_z: f32) -> f32 {
            let size: vec2<u32> = textureDimensions(tex, 0);
            let p: vec2<f32> = uv * vec2<f32>(size);
            let floor_p: vec2<f32> = floor(p);
            let floor_pu: vec2<u32> = vec2<u32>(floor_p);
            let delta: vec2<f32> = p - floor_p;

            let A = (1.0 - delta.x) * (1.0 - delta.y);
            let B = delta.x * (1.0 - delta.y);
            let C = (1.0 - delta.x) * delta.y;
            let D = delta.x * delta.y;
            let AB = A + B;
            let AC = A + C;
            let BD = B + D;
            let CD = C + D;

            let x_min: u32 = cascade * size.y;
            let x_max: u32 = (cascade + 1u) * size.y;
            let y_min: u32 = 0u;
            let y_max: u32 = size.y;

            let s = array<u32, 4>(
                clamp(floor_pu.x - 1u, x_min, x_max),
                clamp(floor_pu.x + 0u, x_min, x_max),
                clamp(floor_pu.x + 1u, x_min, x_max),
                clamp(floor_pu.x + 2u, x_min, x_max),
            );
            let t = array<u32, 4>(
                clamp(floor_pu.y - 1u, y_min, y_max),
                clamp(floor_pu.y + 0u, y_min, y_max),
                clamp(floor_pu.y + 1u, y_min, y_max),
                clamp(floor_pu.y + 2u, y_min, y_max),
            );

            let pos = array<vec2<u32>, 16>(
                vec2(s[0], t[0]), vec2<u32>(s[1], t[0]), vec2<u32>(s[2], t[0]), vec2<u32>(s[3], t[0]),
                vec2(s[0], t[1]), vec2<u32>(s[1], t[1]), vec2<u32>(s[2], t[1]), vec2<u32>(s[3], t[1]),
                vec2(s[0], t[2]), vec2<u32>(s[1], t[2]), vec2<u32>(s[2], t[2]), vec2<u32>(s[3], t[2]),
                vec2(s[0], t[3]), vec2<u32>(s[1], t[3]), vec2<u32>(s[2], t[3]), vec2<u32>(s[3], t[3]),
            );
            let norm: f32 = 0.111111111;        // 1.0 / 9.0
            let kernel = array<f32, 16>(
                A * norm, AB * norm, AB * norm, B * norm,
                AC * norm, 1.0 * norm, 1.0 * norm, BD * norm,
                AC * norm, 1.0 * norm, 1.0 * norm, BD * norm,
                C * norm, CD * norm, CD * norm, D * norm,
            );
            return 
                step(textureLoad(tex, pos[0], 0), ref_z) * kernel[0] +
                step(textureLoad(tex, pos[1], 0), ref_z) * kernel[1] +
                step(textureLoad(tex, pos[2], 0), ref_z) * kernel[2] +
                step(textureLoad(tex, pos[3], 0), ref_z) * kernel[3] +
                step(textureLoad(tex, pos[4], 0), ref_z) * kernel[4] +
                step(textureLoad(tex, pos[5], 0), ref_z) * kernel[5] +
                step(textureLoad(tex, pos[6], 0), ref_z) * kernel[6] +
                step(textureLoad(tex, pos[7], 0), ref_z) * kernel[7] +
                step(textureLoad(tex, pos[8], 0), ref_z) * kernel[8] +
                step(textureLoad(tex, pos[9], 0), ref_z) * kernel[9] +
                step(textureLoad(tex, pos[10], 0), ref_z) * kernel[10] +
                step(textureLoad(tex, pos[11], 0), ref_z) * kernel[11] +
                step(textureLoad(tex, pos[12], 0), ref_z) * kernel[12] +
                step(textureLoad(tex, pos[13], 0), ref_z) * kernel[13] +
                step(textureLoad(tex, pos[14], 0), ref_z) * kernel[14] +
                step(textureLoad(tex, pos[15], 0), ref_z) * kernel[15];
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
            n ^= (n.yx << vec2<u32>(9u, 9u));
            n ^= (n.yx >> vec2<u32>(1u, 1u));
            n *= K;
            n ^= (n.yx << vec2<u32>(1u, 1u));
            n *= K;
            return n;
        }

        // Return random vec2<f32> in [0, 1]
        fn random_vec2_f32(p: vec2<f32>) -> vec2<f32> {
            let a: vec2<u32> = random_vec2_u32(p);
            let u32_max_inv = 2.3283064E-10;    // 1.0 / u32.maxvalue
            return vec2<f32>(f32(a.x) * u32_max_inv, f32(a.y) * u32_max_inv);
        }
        """u8.ToImmutableArray();
    });

    public static Own<Shader> Create(Screen screen)
    {
        var shader = Shader.Create(
            screen,
            [
                new()
                {
                    Source = ShaderSource.Value,
                    SortOrder = 2000,
                    LayoutDescriptor = BuildPipelineLayout(screen, out var disposable),
                    PipelineDescriptorFactory = PipelineFactory,
                    PassKind = PassKind.Surface,
                    OnRenderPass = (in RenderPass renderPass, RenderPipeline pipeline, IMaterial material, Mesh mesh, in SubmeshData submesh, int passIndex) =>
                    {
                        renderPass.SetPipeline(pipeline);
                        renderPass.SetBindGroups(material.GetBindGroups(passIndex));
                        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
                        renderPass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
                        renderPass.DrawIndexed(submesh.IndexOffset, submesh.IndexCount, submesh.VertexOffset, 0, 1);
                    },
                },
            ],
            null);
        var shaderValue = shader.AsValue();
        disposable.DisposeOn(shaderValue.Disposed);
        return shader;
    }

    private static RenderPipelineDescriptor PipelineFactory(ShaderModule module, PipelineLayout layout)
    {
        var screen = module.Screen;
        return new RenderPipelineDescriptor
        {
            Layout = layout,
            Vertex = new VertexState
            {
                Module = module,
                EntryPoint = "vs_main"u8.ToImmutableArray(),
                Buffers =
                [
                    VertexBufferLayout.FromVertex<VertexSlim>(
                    [
                        (0, VertexFieldSemantics.Position),
                        (1, VertexFieldSemantics.UV),
                    ]),
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
                        Format = screen.Surface.Format,
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

    private static PipelineLayoutDescriptor BuildPipelineLayout(Screen screen, out DisposableBag disposable)
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
                        BindGroupLayoutEntry.Sampler(0, ShaderStages.Fragment, SamplerBindingType.NonFiltering),
                        BindGroupLayoutEntry.Texture(1, ShaderStages.Fragment, new()
                        {
                            Multisampled = false,
                            ViewDimension = TextureViewDimension.D2,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(2, ShaderStages.Fragment, new()
                        {
                            Multisampled = false,
                            ViewDimension = TextureViewDimension.D2,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(3, ShaderStages.Fragment, new()
                        {
                            Multisampled = false,
                            ViewDimension = TextureViewDimension.D2,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                        BindGroupLayoutEntry.Texture(4, ShaderStages.Fragment, new()
                        {
                            Multisampled = false,
                            ViewDimension = TextureViewDimension.D2,
                            SampleType = TextureSampleType.FloatNotFilterable,
                        }),
                    ]
                }).AddTo(disposable),
                screen.Camera.CameraDataBindGroupLayout,
                screen.Lights.DataBindGroupLayout,
                BindGroupLayout.Create(screen, new()
                {
                    Entries =
                    [
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.Depth,
                        }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Fragment, new() { Type = BufferBindingType.StorageReadOnly }),
                        BindGroupLayoutEntry.Buffer(2, ShaderStages.Fragment, new() { Type = BufferBindingType.StorageReadOnly }),
                    ],
                }).AddTo(disposable),
            ],
        };
    }
}
