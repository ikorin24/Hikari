#nullable enable
using Hikari.NativeBind;

namespace Hikari;

public interface ITexture
{
    internal Rust.Ref<Wgpu.Texture> NativeRef { get; }
    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef { get; }
}

public interface ITexture<TSize> : ITexture
    where TSize : struct
{
    TSize Size { get; }
}
