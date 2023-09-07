#nullable enable
using Hikari.NativeBind;

namespace Hikari;

public interface ITextureProvider : ITextureViewProvider
{
    Event<ITexture2DProvider> TextureChanged { get; }
    internal Rust.Ref<Wgpu.Texture> GetCurrentTexture();
}

public interface ITextureProvider<TSize> : ITextureProvider
    where TSize : struct
{
    TSize GetCurrentSize();
    uint GetCurrentMipLevelCount();
    uint GetCurrentSampleCount();
    TextureFormat GetCurrentFormat();
    TextureUsages GetCurrentUsage();
    TextureDimension GetCurrentDimension();
}

public interface ITextureViewProvider
{
    Event<ITextureViewProvider> TextureViewChanged { get; }
    internal Rust.Ref<Wgpu.TextureView> GetCurrentTextureView();
}
