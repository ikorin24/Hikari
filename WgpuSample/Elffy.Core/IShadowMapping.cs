#nullable enable

namespace Elffy;

public interface IShadowMapping
{
    void RenderShadowMap(in ComputePass pass, RenderPipeline pipeline);
}
