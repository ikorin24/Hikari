#nullable enable
using Hikari.NativeBind;

namespace Hikari;

public interface ITexture : ITextureView
{
    internal Rust.Ref<Wgpu.Texture> NativeRef { get; }
}

public interface ITexture<TSize> : ITexture
    where TSize : struct
{
    TSize Size { get; }
    uint MipLevelCount { get; }
    uint SampleCount { get; }
    TextureFormat Format { get; }
    TextureUsages Usage { get; }
    TextureDimension Dimension { get; }
}

public interface ITextureView
{
    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef { get; }
}
