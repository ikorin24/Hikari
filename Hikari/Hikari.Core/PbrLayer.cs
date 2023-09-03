#nullable enable
using System;
using V = Hikari.Vertex;

namespace Hikari;

public sealed class PbrLayer
    : ObjectLayer<PbrLayer, V, PbrShader, PbrMaterial, PbrModel>,
    IGBufferProvider
{
    private static readonly TextureFormat[] _formats = new TextureFormat[4]
    {
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
        TextureFormat.Rgba32Float,
    };

    private Own<GBuffer> _gBuffer;
    private EventSource<GBuffer> _gBufferChanged = new();
    private readonly Own<PipelineLayout> _shadowPipelineLayout;
    private readonly Own<BindGroupLayout> _bindGroupLayout0;
    private readonly BindGroupLayout _bindGroupLayout1;

    private readonly Own<BindGroupLayout> _shadowBindGroupLayout0;

    public BindGroupLayout BindGroupLayout0 => _bindGroupLayout0.AsValue();

    public BindGroupLayout BindGroupLayout1 => _bindGroupLayout1;

    public BindGroupLayout ShadowBindGroupLayout0 => _shadowBindGroupLayout0.AsValue();

    public PipelineLayout ShadowPipelineLayout => _shadowPipelineLayout.AsValue();


    public GBuffer CurrentGBuffer => _gBuffer.AsValue();

    public Event<GBuffer> GBufferChanged => _gBufferChanged.Event;

    internal PbrLayer(Screen screen)
        : base(
            screen,
            BuildPipelineLayoutDescriptor(screen, out var bindGroupLayout0, out var bindGroupLayout1))
    {
        _bindGroupLayout0 = bindGroupLayout0;
        _bindGroupLayout1 = bindGroupLayout1;
        _shadowPipelineLayout = BuildShadowPipeline(screen, out var shadowBgl0);
        _shadowBindGroupLayout0 = shadowBgl0;
        _bindGroupLayout1 = bindGroupLayout1;

        RecreateGBuffer(screen, screen.ClientSize);

        screen.Resized.Subscribe(x =>
        {
            RecreateGBuffer(x.Screen, x.Size);
        }).AddTo(Subscriptions);
        Dead.Subscribe(static self =>
        {
            self._gBuffer.Dispose();
            self._bindGroupLayout0.Dispose();
            self._shadowBindGroupLayout0.Dispose();
            self._shadowPipelineLayout.Dispose();
        }).AddTo(Subscriptions);
    }

    private static Own<PipelineLayout> BuildShadowPipeline(
        Screen screen,
        out Own<BindGroupLayout> bgl0)
    {
        return PipelineLayout.Create(screen, new()
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new() { Type = BufferBindingType.Uniform }),
                        BindGroupLayoutEntry.Buffer(1, ShaderStages.Vertex, new() { Type = BufferBindingType.StorateReadOnly }),
                    },
                }).AsValue(out bgl0),
            },
        });
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

    protected override OwnRenderPass CreateRenderPass(in OperationContext context)
    {
        return _gBuffer.AsValue().CreateRenderPass();
    }

    private static Own<PipelineLayout> BuildPipelineLayoutDescriptor(
        Screen screen,
        out Own<BindGroupLayout> bindGroupLayout0,
        out BindGroupLayout bindGroupLayout1)
    {
        bindGroupLayout1 = screen.Camera.CameraDataBindGroupLayout;
        return PipelineLayout.Create(screen, new PipelineLayoutDescriptor
        {
            BindGroupLayouts = new[]
            {
                BindGroupLayout.Create(screen, new()
                {
                    Entries = new[]
                    {
                        BindGroupLayoutEntry.Buffer(0, ShaderStages.Vertex, new BufferBindingData
                        {
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
                }).AsValue(out bindGroupLayout0),
                bindGroupLayout1,
            },
        });
    }

    protected override void RenderShadowMap(in RenderShadowMapContext context, ReadOnlySpan<PbrModel> objects)
    {
        using var pass = context.CreateRenderPass();
        var p = pass.AsValue();
        var directionalLight = context.Lights.DirectionalLight;

        for(uint i = 0; i < directionalLight.CascadeCount; i++) {
            foreach(var obj in objects) {
                obj.RenderShadowMap(in p, i, context.Lights, obj.Material, obj.Mesh);
            }
        }
    }
}
