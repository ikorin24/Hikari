#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

public unsafe readonly ref struct RenderShadowMapContext
{
    private readonly CommandEncoder _encoder;
    private readonly Lights _lights;
    private readonly TextureView _shadowMap;

    [UnscopedRef]
    public ref readonly CommandEncoder CommandEncoder => ref _encoder;

    public Lights Lights => _lights;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RenderShadowMapContext() => throw new NotSupportedException("Don't use default constructor.");

    internal RenderShadowMapContext(CommandEncoder encoder, Lights lights)
    {
        _encoder = encoder;
        _lights = lights;
        _shadowMap = lights.DirectionalLight.ShadowMap.View;
    }

    public Own<RenderPass> CreateRenderPass()
    {
        return RenderPass.Create(_encoder.NativeMut, new()
        {
            color_attachments_clear = new() { data = null, len = 0 },
            depth_stencil_attachment_clear = new(new()
            {
                depth_clear = CE.Opt<float>.Some(1f),
                stencil_clear = CE.Opt<u32>.None,
                view = _shadowMap.NativeRef,
            }),
        });
    }
}
