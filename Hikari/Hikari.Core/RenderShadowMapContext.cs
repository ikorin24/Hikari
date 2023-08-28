#nullable enable
using System;
using System.ComponentModel;
using Hikari.NativeBind;

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
        return RenderPass.Create(screen, new CH.RenderPassDescriptor()
        {
            color_attachments = new() { data = null, len = 0 },
            depth_stencil_attachment = new(new()
            {
                view = _shadowMap.NativeRef,
                depth = new(clear switch
                {
                    true => new()
                    {
                        mode = CH.RenderPassBufferInitMode.Clear,
                        value = 1f,
                    },
                    false => new()
                    {
                        mode = CH.RenderPassBufferInitMode.Load,
                        value = default,
                    },
                }),
                stencil = CH.Opt<CH.RenderPassStencilBufferInit>.None,
            }),
        });
    }
}
