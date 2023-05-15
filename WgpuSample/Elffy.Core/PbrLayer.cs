#nullable enable
using System;
using System.Threading;
using V = Elffy.Vertex;

namespace Elffy;

public sealed class PbrLayer
    : ObjectLayer<PbrLayer, V, PbrShader, PbrMaterial, PbrModel>,
    IGBufferProvider
{
    private const int MrtCount = 4;
    private static readonly TextureFormat[] _formats = new TextureFormat[MrtCount]
    {
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
    };
    private static readonly ReadOnlyMemory<ColorTargetState?> _targets = new ColorTargetState?[MrtCount]
    {
        new ColorTargetState
        {
            Format = _formats[0],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[1],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[2],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
        new ColorTargetState
        {
            Format = _formats[3],
            Blend = null,
            WriteMask = ColorWrites.All,
        },
    };

    private Own<GBuffer> _gBuffer;
    private EventSource<GBuffer> _gBufferChanged = new();
    private readonly ComputePipeline _shadowPipeline;
    private readonly BindGroupLayout _shadowBgl0;
    private readonly BindGroupLayout _shadowBgl1;
    private IDisposable[] _disposables;

    public GBuffer CurrentGBuffer => _gBuffer.AsValue();

    public Event<GBuffer> GBufferChanged => _gBufferChanged.Event;

    internal BindGroupLayout ShadowBindGroupLayout0 => _shadowBgl0;

    public PbrLayer(Screen screen, int sortOrder)
        : base(screen, PbrShader.Create(screen), static shader => BuildPipeline(shader), sortOrder)
    {
        RecreateGBuffer(screen, screen.ClientSize);
        _shadowPipeline = CreateRenderShadowPipeline(
            screen,
            out _disposables,
            out _shadowBgl0,
            out _shadowBgl1);

        screen.Resized.Subscribe(x =>
        {
            RecreateGBuffer(x.Screen, x.Size);
        }).AddTo(Subscriptions);
        Dead.Subscribe(static x =>
        {
            var self = SafeCast.As<PbrLayer>(x);
            self._gBuffer.Dispose();

            var disposables = Interlocked.Exchange(ref self._disposables, Array.Empty<IDisposable>());
            foreach(var item in disposables) {
                item.Dispose();
            }
        }).AddTo(Subscriptions);
    }

    private void RecreateGBuffer(Screen screen, Vector2u newSize)
    {
        if(_gBuffer.TryAsValue(out var gBuffer) && gBuffer.Size == newSize) {
            return;
        }
        _gBuffer.Dispose();
        _gBuffer = GBuffer.Create(screen, newSize, _formats);
        _gBufferChanged.Invoke(_gBuffer.AsValue());
    }

    protected override Own<RenderPass> CreateRenderPass(in CommandEncoder encoder)
    {
        return _gBuffer.AsValue().CreateRenderPass(encoder);
    }

    private static Own<RenderPipeline> BuildPipeline(PbrShader shader)
    {
        var screen = shader.Screen;
        var desc = new RenderPipelineDescriptor
        {
            Layout = shader.PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.Module,
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
                Module = shader.Module,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = _targets,
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
        return RenderPipeline.Create(screen, in desc);
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<PbrModel> objects)
    {
        using var pass = ComputePass.Create(context.CommandEncoder);
        var p = pass.AsValue();
        p.SetPipeline(_shadowPipeline);
        foreach(var obj in objects) {
            obj.RenderShadowMap(context, p);
        }
    }

    private static ReadOnlySpan<u8> ShadowMappingSource => """
        struct State {
            mvp: mat4x4<f32>,
        };
        struct Depth {
            width: u32,
            height: u32,
            data: array<atomic<u32>>,
        };

        @group(0) @binding(0) var<storage, read> vertex_buffer: array<Vertex>;
        @group(0) @binding(1) var<storage, read> index_buffer: array<u32>;
        @group(0) @binding(2) var<storage, read> state: State;
        @group(1) @binding(0) var<storage, read_write> depth: Depth;

        struct Vertex {
            x: f32,
            y: f32,
            z: f32,
            n_x: f32,
            n_y: f32,
            n_z: f32,
            uv_x: f32,
            uv_y: f32,
        };

        @compute @workgroup_size(256, 1)
        fn clear(@builtin(global_invocation_id) global_id : vec3<u32>) {
            let index = global_id.x * 3u;
            atomicStore(&depth.data[index + 0u], 255u);
            atomicStore(&depth.data[index + 1u], 255u);
            atomicStore(&depth.data[index + 2u], 255u);
        }

        @compute @workgroup_size(256, 1)
        fn main(@builtin(global_invocation_id) global_id : vec3<u32>) {
            let index = global_id.x * 3u;

            let v1 = project(vertex_buffer[index_buffer[index + 0u]]);
            let v2 = project(vertex_buffer[index_buffer[index + 1u]]);
            let v3 = project(vertex_buffer[index_buffer[index + 2u]]);
  
            draw_triangle(v1, v2, v3);
        }

        fn color_pixel(x: u32, y: u32, r: u32, g: u32, b: u32) {
            let pixelID = u32(x + y * depth.width) * 3u;
            atomicMin(&depth.data[pixelID + 0u], r);
            atomicMin(&depth.data[pixelID + 1u], g);
            atomicMin(&depth.data[pixelID + 2u], b);
        }

        fn project(v: Vertex) -> vec3<f32> {
            var screenPos = state.mvp * vec4<f32>(v.x, v.y, v.z, 1.0);
            screenPos.x = (screenPos.x / screenPos.w) * f32(depth.width);
            screenPos.y = (screenPos.y / screenPos.w) * f32(depth.height);
            return vec3<f32>(screenPos.x, screenPos.y, screenPos.w);
        }



        fn barycentric(v1: vec3<f32>, v2: vec3<f32>, v3: vec3<f32>, p: vec2<f32>) -> vec3<f32> {
            let u = cross(
                vec3<f32>(v3.x - v1.x, v2.x - v1.x, v1.x - p.x), 
                vec3<f32>(v3.y - v1.y, v2.y - v1.y, v1.y - p.y)
            );

            if (abs(u.z) < 1.0) {
                return vec3<f32>(-1.0, 1.0, 1.0);
            }

            return vec3<f32>(1.0 - (u.x+u.y)/u.z, u.y/u.z, u.x/u.z); 
        }

        fn get_min_max(v1: vec3<f32>, v2: vec3<f32>, v3: vec3<f32>) -> vec4<f32> {
            var min_max = vec4<f32>();
            min_max.x = min(min(v1.x, v2.x), v3.x);
            min_max.y = min(min(v1.y, v2.y), v3.y);
            min_max.z = max(max(v1.x, v2.x), v3.x);
            min_max.w = max(max(v1.y, v2.y), v3.y);

            return min_max;
        }

        fn draw_triangle(v1: vec3<f32>, v2: vec3<f32>, v3: vec3<f32>) {
            let min_max = get_min_max(v1, v2, v3);
            let startX = u32(min_max.x);
            let startY = u32(min_max.y);
            let endX = u32(min_max.z);
            let endY = u32(min_max.w);

            for (var x: u32 = startX; x <= endX; x = x + 1u) {
                for (var y: u32 = startY; y <= endY; y = y + 1u) {
                    let bc = barycentric(v1, v2, v3, vec2<f32>(f32(x), f32(y))); 
                    let color = bc.x * v1.z + bc.y * v2.z + bc.z * v3.z;

                    let R = color;
                    let G = color;
                    let B = color;

                    if (bc.x < 0.0 || bc.y < 0.0 || bc.z < 0.0) {
                        continue;
                    }
                    color_pixel(x, y, u32(R), u32(G), u32(B));
                }
            }
        }
        """u8;

    private static ComputePipeline CreateRenderShadowPipeline(
        Screen screen,
        out IDisposable[] disposables,
        out BindGroupLayout bindGroupLayout0,
        out BindGroupLayout bindGroupLayout1)
    {
        var lights = screen.Lights;
        var bgl1 = lights.DirectionalLight.LightDepthBindGroupLayout;

        var pipeline = ComputePipeline.Create(screen, new()
        {
            Layout = PipelineLayout.Create(screen, new()
            {
                BindGroupLayouts = new[]
                {
                    BindGroupLayout.Create(screen, new()
                    {
                        Entries = new[]
                        {
                            BindGroupLayoutEntry.Buffer(0, ShaderStages.Compute, new() { Type = BufferBindingType.StorateReadOnly }),
                            BindGroupLayoutEntry.Buffer(1, ShaderStages.Compute, new() { Type = BufferBindingType.StorateReadOnly }),
                            BindGroupLayoutEntry.Buffer(2, ShaderStages.Compute, new() { Type = BufferBindingType.StorateReadOnly }),
                        },
                    })
                    .AsValue(out var bgl0),
                    bgl1,
                },
            }).AsValue(out var pipelineLayout),
            Module = ShaderModule.Create(screen, ShadowMappingSource).AsValue(out var module),
            EntryPoint = "main"u8.ToArray(),
        });

        disposables = new IDisposable[]
        {
            module,
            bgl0,
            pipelineLayout,
            pipeline,
        };
        bindGroupLayout0 = bgl0.AsValue();
        bindGroupLayout1 = bgl1;
        return pipeline.AsValue();
    }
}
