#nullable enable
using System;

namespace Hikari;

public readonly record struct MaterialPassData
{
    private readonly int _index;
    private readonly BindGroupData[] _bindGroups;

    public int Index => _index;
    public ReadOnlySpan<BindGroupData> BindGroups => _bindGroups;

    public MaterialPassData(int index, BindGroupData[] bindGroups)
    {
        _index = index;
        _bindGroups = bindGroups;
    }

    public void Deconstruct(out int index, out ReadOnlySpan<BindGroupData> bindGroups)
    {
        index = _index;
        bindGroups = _bindGroups;
    }
}
