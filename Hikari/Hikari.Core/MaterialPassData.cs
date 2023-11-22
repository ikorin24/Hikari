#nullable enable

namespace Hikari;

public readonly record struct MaterialPassData
{
    public required int Index { get; init; }
    public required PassKind PassKind { get; init; }
    public required int SortOrder { get; init; }
    public required RenderPipeline Pipeline { get; init; }
    public required PipelineLayout PipelineLayout { get; init; }
}
