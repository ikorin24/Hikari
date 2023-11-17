#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public readonly record struct MaterialPassData
{
    private readonly int _index;
    private readonly ImmutableArray<BindGroupData> _bindGroups;

    public int Index => _index;
    public ReadOnlySpan<BindGroupData> BindGroups => _bindGroups.AsSpan();

    public MaterialPassData(int index, ImmutableArray<BindGroupData> bindGroups)
    {
        _index = index;
        _bindGroups = bindGroups;
    }

    public void Deconstruct(out int index, out ReadOnlySpan<BindGroupData> bindGroups)
    {
        index = _index;
        bindGroups = _bindGroups.AsSpan();
    }
}
