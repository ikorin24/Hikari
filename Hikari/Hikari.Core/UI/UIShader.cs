#nullable enable
using System;

namespace Hikari.UI;

internal abstract class UIShader : Shader<UIShader, UIMaterial, UILayer>
{
    protected UIShader(
        ReadOnlySpan<byte> shaderSource,
        UILayer operation,
        Func<UILayer, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
        : base(shaderSource, operation, getPipelineDesc)
    {
    }

    public abstract Own<UIMaterial> CreateMaterial();
}
