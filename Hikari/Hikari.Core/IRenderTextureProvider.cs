#nullable enable

namespace Hikari;

public interface IRenderTextureProvider
{
    Texture2D GetCurrent();
    Event<Texture2D> Changed { get; }
    uint MipLevelCount { get; }
    uint SampleCount { get; }
    TextureFormat Format { get; }
    TextureUsages Usage { get; }
    TextureDimension Dimension { get; }
}
