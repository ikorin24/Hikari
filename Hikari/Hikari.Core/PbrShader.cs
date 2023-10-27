#nullable enable
using System;

namespace Hikari;

public abstract class PbrShader : Shader<PbrShader, PbrMaterial, PbrLayer>
{
    protected PbrShader(
        ReadOnlySpan<byte> source,
        PbrLayer operation,
        Func<PbrLayer, ShaderModule, RenderPipelineDescriptor> getPipelineDesc)
        : base(source, operation, getPipelineDesc)
    {
    }

    public abstract RenderPipeline ShadowPipeline(uint cascade);
}
