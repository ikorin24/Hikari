#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Elffy;

public unsafe readonly ref struct RenderShadowMapContext
{
    private readonly CommandEncoder _encoder;
    private readonly BindGroup _lightDepthBindGroup;

    [UnscopedRef]
    public ref readonly CommandEncoder CommandEncoder => ref _encoder;

    public BindGroup LightDepthBindGroup => _lightDepthBindGroup;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RenderShadowMapContext() => throw new NotSupportedException("Don't use default constructor.");

    internal RenderShadowMapContext(CommandEncoder encoder, BindGroup lightDepthBindGroup)
    {
        _encoder = encoder;
        _lightDepthBindGroup = lightDepthBindGroup;
    }
}
