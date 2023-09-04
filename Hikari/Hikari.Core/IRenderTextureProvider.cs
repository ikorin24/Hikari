#nullable enable

namespace Hikari;

public interface IRenderTextureProvider
{
    Texture GetCurrent();
    Event<Texture> Changed { get; }
    uint MipLevelCount { get; }
    uint SampleCount { get; }
    TextureFormat Format { get; }
    TextureUsages Usage { get; }
    TextureDimension Dimension { get; }
}
