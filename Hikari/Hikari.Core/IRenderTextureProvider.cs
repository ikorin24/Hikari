#nullable enable

namespace Hikari;

public interface IRenderTextureProvider
{
    ITexture2D GetCurrent();
    Event<ITexture2D> Changed { get; }
    uint MipLevelCount { get; }
    uint SampleCount { get; }
    TextureFormat Format { get; }
    TextureUsages Usage { get; }
    TextureDimension Dimension { get; }
}
