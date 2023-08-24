#nullable enable
using Hikari.NativeBind;
using System;
using System.ComponentModel;

namespace Hikari;

public interface ITextureView
{
    TextureViewHandle Handle { get; }
}

public readonly ref struct TextureViewHandle
{
    private readonly Rust.Ref<Wgpu.TextureView> _native;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TextureViewHandle() => throw new NotSupportedException("Don't use default constructor.");

    internal TextureViewHandle(Rust.Ref<Wgpu.TextureView> native)
    {
        _native = native;
    }

    internal Rust.Ref<Wgpu.TextureView> AsRef() => _native;
}
