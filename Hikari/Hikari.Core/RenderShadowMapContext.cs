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

    public OwnRenderPass CreateRenderPass(bool clear = true)
    {
        var screen = _lights.Screen;
        var depthClear = clear ? CE.Opt<float>.Some(1f) : CE.Opt<float>.None;
        return RenderPass.Create(screen, new CE.RenderPassDescriptor()
        {
            color_attachments_clear = new() { data = null, len = 0 },
            depth_stencil_attachment_clear = new(new()
            {
                depth_clear = depthClear,
                stencil_clear = CE.Opt<u32>.None,
                view = _shadowMap.NativeRef,
            }),
        });
    }
}
