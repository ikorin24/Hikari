#nullable enable
using System.Collections.Immutable;

namespace Hikari;

public interface ITypedShader
{
    Screen Screen { get; }
    ImmutableArray<ShaderPassData> ShaderPasses { get; }
}
