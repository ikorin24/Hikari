#nullable enable
using V = Hikari.VertexSlim;

using System;

namespace Hikari;

public sealed class DeferredProcess : RenderOperation<DeferredProcess, DeferredProcessShader, DeferredProcessMaterial>
{
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly Own<BindGroupLayout> _bindGroupLayout3;
    private readonly IGBufferProvider _gBufferProvider;
    private Own<DeferredProcessMaterial> _material;
    private readonly Own<Mesh<V>> _rectMesh;
    private readonly Own<DeferredProcessShader> _shader;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();
    public BindGroupLayout BindGroupLayout3 => _bindGroupLayout3.AsValue();

    internal DeferredProcess(Screen screen, IGBufferProvider gBufferProvider)
        : base(screen,
            BuildPipelineLayout(screen, out var bindGroupLayout0, out var bindGroupLayout3))
    {
        ArgumentNullException.ThrowIfNull(gBufferProvider);
        _gBufferProvider = gBufferProvider;
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout3 = bindGroupLayout3;


        const float Z = 0;
        ReadOnlySpan<V> vertices = stackalloc V[]
        {
            new(new(-1, -1, Z), new(0, 1)),
            new(new(1, -1, Z), new(1, 1)),
            new(new(1, 1, Z), new(1, 0)),
            new(new(-1, 1, Z), new(0, 0)),
        };
        ReadOnlySpan<ushort> indices = stackalloc ushort[] { 0, 1, 2, 2, 3, 0 };
        _rectMesh = Mesh.Create(Screen, vertices, indices);
        Dead.Subscribe(static x => ((DeferredProcess)x).OnDead()).AddTo(Subscriptions);

        _shader = DeferredProcessShader.Create(this);
        RecreateMaterial(gBufferProvider.CurrentGBuffer);
        gBufferProvider.GBufferChanged.Subscribe(RecreateMaterial).AddTo(Subscriptions);
    }

    private void OnDead()
    {
        _shader.Dispose();
        _material.Dispose();
        _rectMesh.Dispose();
    }

    private void RecreateMaterial(GBuffer gBuffer)
    {
        _material.Dispose();
        _material = DeferredProcessMaterial.Create(_shader.AsValue(), gBuffer);
    }

    protected override OwnRenderPass CreateRenderPass(in OperationContext context)
    {
        return context.CreateSurfaceRenderPass(
            ColorBufferInit.Clear(),
            new DepthStencilBufferInit
            {
                Depth = DepthBufferInit.Clear(1f),
                Stencil = null,
            });
    }

    private static readonly BindGroupLayoutDescriptor _bindGroupLayoutDesc0 = new BindGroupLayoutDescriptor
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

    private static Own<PipelineLayout> BuildPipelineLayout(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0,
        out Own<BindGroupLayout> bindGroupLayout3)
    {
        return PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                // [0]
                BindGroupLayout.Create(screen, new BindGroupLayoutDescriptor
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
                }).AsValue(out bindGroupLayout0),
                // [1]
                screen.Camera.CameraDataBindGroupLayout,
                // [2]
                screen.Lights.DataBindGroupLayout,
                // [3]
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Texture(0, ShaderStages.Fragment, new()
                        {
                            ViewDimension = TextureViewDimension.D2,
                            Multisampled = false,
                            SampleType = TextureSampleType.Depth,
                        }),
                        BindGroupLayoutEntry.Sampler(1, ShaderStages.Fragment, SamplerBindingType.Comparison),
                        BindGroupLayoutEntry.Buffer(2, ShaderStages.Fragment, new() { Type = BufferBindingType.StorateReadOnly }),
                        BindGroupLayoutEntry.Buffer(3, ShaderStages.Fragment, new() { Type = BufferBindingType.StorateReadOnly }),
                    },
                }).AsValue(out bindGroupLayout3),
            },
        });
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context)
    {
        // nop
    }

    protected override void Render(in RenderPass pass)
    {
        var pipeline = _shader.AsValue().Pipeline;
        pass.SetPipeline(pipeline);
        var mesh = _rectMesh.AsValue();
        var material = _material.AsValue();
        pass.SetVertexBuffer(0, mesh.VertexBuffer);
        pass.SetIndexBuffer(mesh.IndexBuffer, mesh.IndexFormat);
        pass.SetBindGroup(0, material.BindGroup0);
        pass.SetBindGroup(1, material.BindGroup1);
        pass.SetBindGroup(2, material.BindGroup2);
        pass.SetBindGroup(3, material.BindGroup3);
        pass.DrawIndexed(0, mesh.IndexCount, 0, 0, 1);
    }
}
