#nullable enable
using System;

namespace Hikari;

public readonly ref struct ShaderPassDescriptor
{
    public required ReadOnlySpan<byte> Source { get; init; }
    public required PipelineLayoutDescriptor LayoutDescriptor { get; init; }
    public required Func<ShaderModule, PipelineLayout, RenderPipelineDescriptor> PipelineDescriptorFactory { get; init; }
    public required int SortOrder { get; init; }
    public required PassKind PassKind { get; init; }

    internal MaterialPassData CreateMaterialPassData<_>(Shader shader, int passIndex, Event<_> lifetimeLimit)
    {
        var screen = shader.Screen;
        var module = ShaderModule.Create(screen, Source).DisposeOn(lifetimeLimit);
        var layout = PipelineLayout.Create(screen, LayoutDescriptor).DisposeOn(lifetimeLimit);
        return new MaterialPassData
        {
            Index = passIndex,
            PassKind = PassKind,
            SortOrder = SortOrder,
            Pipeline = RenderPipeline.Create(screen, PipelineDescriptorFactory(module, layout)).DisposeOn(lifetimeLimit),
            PipelineLayout = layout,
        };
    }
}

public readonly ref struct ShaderPassDescriptorArray1
{
    public required ShaderPassDescriptor Pass0 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray2
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray3
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
    public required ShaderPassDescriptor Pass2 { get; init; }
}

public readonly ref struct ShaderPassDescriptorArray4
{
    public required ShaderPassDescriptor Pass0 { get; init; }
    public required ShaderPassDescriptor Pass1 { get; init; }
    public required ShaderPassDescriptor Pass2 { get; init; }
    public required ShaderPassDescriptor Pass3 { get; init; }
}
