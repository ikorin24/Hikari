#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public readonly record struct BindGroupData
{
    public required uint Index { get; init; }
    public required BindGroup BindGroup { get; init; }

    [SetsRequiredMembers]
    public BindGroupData(uint index, BindGroup bindGroup)
    {
        Index = index;
        BindGroup = bindGroup;
    }

    public void Deconstruct(out uint index, out BindGroup bindGroup)
    {
        index = Index;
        bindGroup = BindGroup;
    }
}
