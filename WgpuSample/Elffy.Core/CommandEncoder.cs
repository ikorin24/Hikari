#nullable enable
using Elffy.NativeBind;
using System;
using System.ComponentModel;

namespace Elffy;

public readonly ref struct CommandEncoder
{
    private readonly Rust.OptionBox<Wgpu.CommandEncoder> _native;

    internal Rust.MutRef<Wgpu.CommandEncoder> NativeMut => _native.Unwrap();

    public static CommandEncoder None => default;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CommandEncoder() => throw new NotSupportedException("Don't use default constructor.");

    internal CommandEncoder(Rust.Box<Wgpu.CommandEncoder> native)
    {
        _native = native;
    }

    public unsafe Own<RenderPass> CreateSurfaceRenderPass(SurfaceTextureView surface, TextureView? depth)
    {
        return RenderPass.CreateSurfaceRenderPass(this, surface, depth);
    }
}
