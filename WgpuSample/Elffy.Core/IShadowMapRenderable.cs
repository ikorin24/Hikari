#nullable enable

namespace Elffy;

public interface IShadowMapRenderable
{
    void RenderShadowMap(in RenderShadowMapContext context, in ComputePass pass);
}
