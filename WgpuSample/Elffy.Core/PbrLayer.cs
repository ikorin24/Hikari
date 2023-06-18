#nullable enable
using System;
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
    private readonly Own<RenderPipeline> _shadowPipeline;

    public GBuffer CurrentGBuffer => _gBuffer.AsValue();

    public Event<GBuffer> GBufferChanged => _gBufferChanged.Event;

    public PbrLayer(Screen screen, int sortOrder)
        : base(screen, PbrShader.Create(screen), BuildPipeline, sortOrder)
    {
        RecreateGBuffer(screen, screen.ClientSize);

        _shadowPipeline = CreateShadowPipeline(Shader);

        screen.Resized.Subscribe(x =>
        {
            RecreateGBuffer(x.Screen, x.Size);
        }).AddTo(Subscriptions);
        Dead.Subscribe(static x =>
        {
            var self = SafeCast.As<PbrLayer>(x);
            self._gBuffer.Dispose();
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

    protected override Own<RenderPass> CreateRenderPass(in OperationContext context)
    {
        return _gBuffer.AsValue().CreateRenderPass();
    }

    private static Own<RenderPipeline> CreateShadowPipeline(PbrShader shader)
    {
        var screen = shader.Screen;
        return RenderPipeline.Create(screen, new()
        {
            Layout = shader.ShadowPipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.ShadowModule,
                EntryPoint = "vs_main"u8.ToArray(),
                Buffers = new VertexBufferLayout[]
                {
                    VertexBufferLayout.FromVertex<V>(stackalloc[]
                    {
                        (0, VertexFieldSemantics.Position),
                    }),
                },
            },
            Fragment = new FragmentState
            {
                Module = shader.ShadowModule,
                EntryPoint = "fs_main"u8.ToArray(),
                Targets = null,
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
                Bias = DepthBiasState.Default,  // TODO: change bias
            },
            Multisample = MultisampleState.Default,
            Multiview = 0,
        });
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
        using var pass = context.CreateRenderPass();
        var p = pass.AsValue();
        p.SetPipeline(_shadowPipeline.AsValue());
        foreach(var obj in objects) {
            obj.RenderShadowMap(in p, context.Lights, obj.Material, obj.Mesh);
        }
    }
}
