#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

public unsafe readonly ref struct RenderShadowMapContext
{
    private readonly CommandEncoder _encoder;
    private readonly Lights _lights;

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
    }
}
