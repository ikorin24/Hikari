#nullable enable
using Hikari.NativeBind;
using System;

namespace Hikari;

internal static class TextureHelper
{
    public static void CalcMipDataSize<T>(in T desc, Span<(Vector3u MipSize, u32 ByteLength)> mipData, out usize totalByteSize)
        where T : struct, ITextureDescriptor
    {
        totalByteSize = 0;
        var arrayLayerCount = desc.ArrayLayerCount();
        for(uint layer = 0; layer < arrayLayerCount; layer++) {
            for(uint mip = 0; mip < desc.MipLevelCount; mip++) {
                var mipSize = desc.MipLevelSizeRaw(mip).GetOrThrow();
                var info = desc.Format.MipInfo(mipSize);
                u32 dataSize = info.BytesPerRow * info.RowCount;
                mipData[(int)mip] = (MipSize: mipSize, ByteLength: dataSize);
                totalByteSize += dataSize;
            }
        }
    }

    public unsafe static TOut CreateFromRawData<TDesc, TOut>(
        Screen screen,
        in TDesc desc,
        ReadOnlySpan<u8> data,
        delegate*<Screen, Rust.Box<Wgpu.Texture>, in TDesc, TOut> callback)
        where TDesc : struct, ITextureDescriptor
    {
        // data is a raw data of texture in the current format.
        // data = [ mipmap0, mipmap1, mipmap2, ... ]
        //
        // If texture is texture array,
        // data =
        // [
        //     [ mipmap0, mipmap1, mipmap2, ... ],  // layer 0
        //     [ mipmap0, mipmap1, mipmap2, ... ],  // layer 1
        //     ...
        // ]

        Span<(Vector3u MipSize, u32 ByteLength)> mipData = stackalloc (Vector3u, u32)[(int)desc.MipLevelCount];
        CalcMipDataSize(desc, mipData, out var totalByteSize);
        if((uint)data.Length != totalByteSize) {
            throw new ArgumentException($"length of {nameof(data)} should be {totalByteSize}, but actual {data.Length}");
        }

        ArgumentNullException.ThrowIfNull(screen);
        var descNative = desc.ToNative();
        Rust.Box<Wgpu.Texture> textureNative;
        fixed(u8* p = data) {
            textureNative = screen
                .AsRefChecked()
                .CreateTextureWithData(
                    descNative,
                    new CH.Slice<byte>(p, data.Length)
                );
        }
        return callback(screen, textureNative, in desc);
    }
}
