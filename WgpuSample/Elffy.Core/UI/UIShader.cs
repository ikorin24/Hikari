#nullable enable
using System;

namespace Elffy.UI;

public abstract class UIShader : Shader<UIShader, UIMaterial, UILayer>
{
    protected UIShader(
        ReadOnlySpan<byte> shaderSource,
        UILayer operation,
        Func<PipelineLayout, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
        : base(shaderSource, operation, getPipelineDesc)
    {
    }

    public abstract Own<UIMaterial> CreateMaterial();
}
