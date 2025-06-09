#nullable enable

using Hikari.NativeBind;

namespace Hikari;

internal interface ITextureDescriptor
{
    u32 MipLevelCount { get; }
    u32 SampleCount { get; }
    TextureDimension Dimension { get; }
    TextureFormat Format { get; }
    TextureUsages Usage { get; }
    Vector3u SizeRaw { get; }
    Vector3u? MipLevelSizeRaw(u32 level);

    u32 ArrayLayerCount()
    {
        return Dimension switch
        {
            TextureDimension.D1 or TextureDimension.D3 => 1,
            TextureDimension.D2 => SizeRaw.Z,
            _ => 0,
        };
    }
    CH.TextureDescriptor ToNative();
}

internal interface ITextureDescriptor<TSize>
    : ITextureDescriptor
    where TSize : struct
{
    TSize Size { get; }
    TSize? MipLevelSize(u32 level);
}
