#nullable enable
using System;

namespace Hikari;

public interface IMaterial
{
    Screen Screen { get; }
    Shader Shader { get; }
    ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex);
}
