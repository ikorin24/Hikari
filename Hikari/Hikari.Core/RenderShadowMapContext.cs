#nullable enable
using System;
using System.ComponentModel;

namespace Hikari;

public readonly ref struct RenderShadowMapContext
{
    private readonly Lights _lights;
    private readonly TextureView _shadowMap;

    public Lights Lights => _lights;
    public TextureView ShadowMap => _shadowMap;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RenderShadowMapContext() => throw new NotSupportedException("Don't use default constructor.");

    internal RenderShadowMapContext(Lights lights)
    {
        _lights = lights;
        _shadowMap = lights.DirectionalLight.ShadowMap.View;
    }

    public OwnRenderPass CreateRenderPass()
    {
        var screen = _lights.Screen;
        return RenderPass.Create(
            screen,
            null,
            new DepthStencilAttachment
            {
                Target = _shadowMap,
                LoadOp = new DepthStencilBufferInit
                {
                    Depth = DepthBufferInit.Clear(1f),
                    Stencil = null,
                }
            });
    }
}
