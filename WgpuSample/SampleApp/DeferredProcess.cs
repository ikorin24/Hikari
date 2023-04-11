#nullable enable
using V = Elffy.VertexSlim;

using System;

namespace Elffy;

public sealed class DeferredProcess : RenderOperation<DeferredProcessShader, DeferredProcessMaterial>
{
    private readonly IGBufferProvider _gBufferProvider;
    private Own<DeferredProcessMaterial> _material;
    private readonly Own<Mesh<V>> _rectMesh;

    public DeferredProcess(IGBufferProvider gBufferProvider, int sortOrder)
        : base(CreateShader(gBufferProvider, out var gBuffer, out var pipeline), pipeline, sortOrder)
    {
        _gBufferProvider = gBufferProvider;

        RecreateMaterial(gBuffer);
        gBufferProvider.GBufferChanged.Subscribe(RecreateMaterial).AddTo(Subscriptions);
        const float Z = 0;
        ReadOnlySpan<V> vertices = stackalloc V[]
        {
            new(new(-1, -1, Z), new(0, 0)),
            new(new(1, -1, Z), new(1, 0)),
            new(new(1, 1, Z), new(1, 1)),
            new(new(-1, 1, Z), new(0, 1)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        _rectMesh = Mesh.Create(Screen, vertices, indices);
        Dead.Subscribe(static x => ((DeferredProcess)x).OnDead()).AddTo(Subscriptions);
    }

    private void OnDead()
    {
        _material.Dispose();
        _rectMesh.Dispose();
    }

    private void RecreateMaterial(GBuffer gBuffer)
    {
        _material.Dispose();
        _material = DeferredProcessMaterial.Create(Shader, gBuffer);
    }

    private static Own<DeferredProcessShader> CreateShader(IGBufferProvider gBufferProvider, out GBuffer gBuffer, out Own<RenderPipeline> pipeline)
    {
        ArgumentNullException.ThrowIfNull(gBufferProvider);
        gBuffer = gBufferProvider.CurrentGBuffer;
        var screen = gBuffer.Screen;
        var shader = DeferredProcessShader.Create(screen);
        var desc = new RenderPipelineDescriptor
        {
            Layout = shader.AsValue().PipelineLayout,
            Vertex = new VertexState
            {
                Module = shader.AsValue().Module,
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
                Module = shader.AsValue().Module,
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
        pipeline = RenderPipeline.Create(screen, desc);
        return shader;
    }

    protected override void Render(RenderPass renderPass)
    {
        var mesh = _rectMesh.AsValue();
        var material = _material.AsValue();
        var bindGroups = material.BindGroups.Span;

        renderPass.SetPipeline(Pipeline);
        renderPass.SetVertexBuffer(0, mesh.VertexBuffer);
        renderPass.SetIndexBuffer(mesh.IndexBuffer);
        renderPass.SetBindGroup(0, bindGroups[0]);
        renderPass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
