#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public readonly record struct ShaderPassDescriptor
{
    public required ImmutableArray<byte> Source { get; init; }
    public required PipelineLayoutDescriptor LayoutDescriptor { get; init; }
    public required Func<ShaderModule, PipelineLayout, RenderPipelineDescriptor> PipelineDescriptorFactory { get; init; }
    public required int SortOrder { get; init; }
    public required PassKind PassKind { get; init; }
    public required RenderPassAction OnRenderPass { get; init; }

    internal ShaderPassData CreateMaterialPassData<_>(Shader shader, int passIndex, Event<_> lifetimeLimit)
    {
        var screen = shader.Screen;
        var module = ShaderModule.Create(screen, Source.AsSpan()).DisposeOn(lifetimeLimit);
        var layout = PipelineLayout.Create(screen, LayoutDescriptor).DisposeOn(lifetimeLimit);
        return new ShaderPassData
        {
            Index = passIndex,
            PassKind = PassKind,
            SortOrder = SortOrder,
            Pipeline = RenderPipeline.Create(screen, PipelineDescriptorFactory(module, layout)).DisposeOn(lifetimeLimit),
            OnRenderPass = OnRenderPass,
        };
    }
}

public delegate void RenderPassAction(in RenderPass renderPass, RenderPipeline pipeline, IMaterial material, Mesh mesh, in SubmeshData submesh, int passIndex);
