#nullable enable

namespace Hikari;

public readonly record struct MaterialPassData
{
    public required int Index { get; init; }
    public required PassKind PassKind { get; init; }
    public required int SortOrder { get; init; }
    public required RenderPipeline Pipeline { get; init; }
    public required PipelineLayout PipelineLayout { get; init; }

    //public required ImmutableArray<BindGroupData> BindGroups { get; init; }

    //public int Index => _index;
    //public ReadOnlySpan<BindGroupData> BindGroups => _bindGroups.AsSpan();

    //public MaterialPassData(int index, ImmutableArray<BindGroupData> bindGroups)
    //{
    //    _index = index;
    //    _bindGroups = bindGroups;
    //}

    //public void Deconstruct(out int index, out ReadOnlySpan<BindGroupData> bindGroups)
    //{
    //    index = _index;
    //    bindGroups = _bindGroups.AsSpan();
    //}
}
