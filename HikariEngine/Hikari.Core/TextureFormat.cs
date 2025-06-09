#nullable enable
using Hikari.NativeBind;
using System;

namespace Hikari;

public enum TextureFormat : u32
{
    [EnumMapTo(CH.TextureFormat.R8Unorm)] R8Unorm,
    [EnumMapTo(CH.TextureFormat.R8Snorm)] R8Snorm,
    [EnumMapTo(CH.TextureFormat.R8Uint)] R8Uint,
    [EnumMapTo(CH.TextureFormat.R8Sint)] R8Sint,
    [EnumMapTo(CH.TextureFormat.R16Uint)] R16Uint,
    [EnumMapTo(CH.TextureFormat.R16Sint)] R16Sint,
    [EnumMapTo(CH.TextureFormat.R16Unorm)] R16Unorm,
    [EnumMapTo(CH.TextureFormat.R16Snorm)] R16Snorm,
    [EnumMapTo(CH.TextureFormat.R16Float)] R16Float,
    [EnumMapTo(CH.TextureFormat.Rg8Unorm)] Rg8Unorm,
    [EnumMapTo(CH.TextureFormat.Rg8Snorm)] Rg8Snorm,
    [EnumMapTo(CH.TextureFormat.Rg8Uint)] Rg8Uint,
    [EnumMapTo(CH.TextureFormat.Rg8Sint)] Rg8Sint,
    [EnumMapTo(CH.TextureFormat.R32Uint)] R32Uint,
    [EnumMapTo(CH.TextureFormat.R32Sint)] R32Sint,
    [EnumMapTo(CH.TextureFormat.R32Float)] R32Float,
    [EnumMapTo(CH.TextureFormat.Rg16Uint)] Rg16Uint,
    [EnumMapTo(CH.TextureFormat.Rg16Sint)] Rg16Sint,
    [EnumMapTo(CH.TextureFormat.Rg16Unorm)] Rg16Unorm,
    [EnumMapTo(CH.TextureFormat.Rg16Snorm)] Rg16Snorm,
    [EnumMapTo(CH.TextureFormat.Rg16Float)] Rg16Float,
    [EnumMapTo(CH.TextureFormat.Rgba8Unorm)] Rgba8Unorm,
    [EnumMapTo(CH.TextureFormat.Rgba8UnormSrgb)] Rgba8UnormSrgb,
    [EnumMapTo(CH.TextureFormat.Rgba8Snorm)] Rgba8Snorm,
    [EnumMapTo(CH.TextureFormat.Rgba8Uint)] Rgba8Uint,
    [EnumMapTo(CH.TextureFormat.Rgba8Sint)] Rgba8Sint,
    [EnumMapTo(CH.TextureFormat.Bgra8Unorm)] Bgra8Unorm,
    [EnumMapTo(CH.TextureFormat.Bgra8UnormSrgb)] Bgra8UnormSrgb,
    [EnumMapTo(CH.TextureFormat.Rgb10a2Unorm)] Rgb10a2Unorm,
    [EnumMapTo(CH.TextureFormat.Rg11b10Float)] Rg11b10Float,
    [EnumMapTo(CH.TextureFormat.Rg32Uint)] Rg32Uint,
    [EnumMapTo(CH.TextureFormat.Rg32Sint)] Rg32Sint,
    [EnumMapTo(CH.TextureFormat.Rg32Float)] Rg32Float,
    [EnumMapTo(CH.TextureFormat.Rgba16Uint)] Rgba16Uint,
    [EnumMapTo(CH.TextureFormat.Rgba16Sint)] Rgba16Sint,
    [EnumMapTo(CH.TextureFormat.Rgba16Unorm)] Rgba16Unorm,
    [EnumMapTo(CH.TextureFormat.Rgba16Snorm)] Rgba16Snorm,
    [EnumMapTo(CH.TextureFormat.Rgba16Float)] Rgba16Float,
    [EnumMapTo(CH.TextureFormat.Rgba32Uint)] Rgba32Uint,
    [EnumMapTo(CH.TextureFormat.Rgba32Sint)] Rgba32Sint,
    [EnumMapTo(CH.TextureFormat.Rgba32Float)] Rgba32Float,
    [EnumMapTo(CH.TextureFormat.Depth32Float)] Depth32Float,
    [EnumMapTo(CH.TextureFormat.Depth32FloatStencil8)] Depth32FloatStencil8,
    [EnumMapTo(CH.TextureFormat.Depth24Plus)] Depth24Plus,
    [EnumMapTo(CH.TextureFormat.Depth24PlusStencil8)] Depth24PlusStencil8,
    [EnumMapTo(CH.TextureFormat.Rgb9e5Ufloat)] Rgb9e5Ufloat,
    [EnumMapTo(CH.TextureFormat.Bc1RgbaUnorm)] Bc1RgbaUnorm,
    [EnumMapTo(CH.TextureFormat.Bc1RgbaUnormSrgb)] Bc1RgbaUnormSrgb,
    [EnumMapTo(CH.TextureFormat.Bc2RgbaUnorm)] Bc2RgbaUnorm,
    [EnumMapTo(CH.TextureFormat.Bc2RgbaUnormSrgb)] Bc2RgbaUnormSrgb,
    [EnumMapTo(CH.TextureFormat.Bc3RgbaUnorm)] Bc3RgbaUnorm,
    [EnumMapTo(CH.TextureFormat.Bc3RgbaUnormSrgb)] Bc3RgbaUnormSrgb,
    [EnumMapTo(CH.TextureFormat.Bc4RUnorm)] Bc4RUnorm,
    [EnumMapTo(CH.TextureFormat.Bc4RSnorm)] Bc4RSnorm,
    [EnumMapTo(CH.TextureFormat.Bc5RgUnorm)] Bc5RgUnorm,
    [EnumMapTo(CH.TextureFormat.Bc5RgSnorm)] Bc5RgSnorm,
    [EnumMapTo(CH.TextureFormat.Bc6hRgbUfloat)] Bc6hRgbUfloat,
    [EnumMapTo(CH.TextureFormat.Bc6hRgbFloat)] Bc6hRgbFloat,
    [EnumMapTo(CH.TextureFormat.Bc7RgbaUnorm)] Bc7RgbaUnorm,
    [EnumMapTo(CH.TextureFormat.Bc7RgbaUnormSrgb)] Bc7RgbaUnormSrgb,
    [EnumMapTo(CH.TextureFormat.Etc2Rgb8Unorm)] Etc2Rgb8Unorm,
    [EnumMapTo(CH.TextureFormat.Etc2Rgb8UnormSrgb)] Etc2Rgb8UnormSrgb,
    [EnumMapTo(CH.TextureFormat.Etc2Rgb8A1Unorm)] Etc2Rgb8A1Unorm,
    [EnumMapTo(CH.TextureFormat.Etc2Rgb8A1UnormSrgb)] Etc2Rgb8A1UnormSrgb,
    [EnumMapTo(CH.TextureFormat.Etc2Rgba8Unorm)] Etc2Rgba8Unorm,
    [EnumMapTo(CH.TextureFormat.Etc2Rgba8UnormSrgb)] Etc2Rgba8UnormSrgb,
    [EnumMapTo(CH.TextureFormat.EacR11Unorm)] EacR11Unorm,
    [EnumMapTo(CH.TextureFormat.EacR11Snorm)] EacR11Snorm,
    [EnumMapTo(CH.TextureFormat.EacRg11Unorm)] EacRg11Unorm,
    [EnumMapTo(CH.TextureFormat.EacRg11Snorm)] EacRg11Snorm,
}

public static class TextureFormatHelper
{
    internal static Wgpu.Features RequiredFeatures(this TextureFormat format)
    {
        return format.MapOrThrow().TextureFormatRequiredFeatures();
    }

    public static TextureSampleType? SampleType(this TextureFormat format, TextureAspect? aspect = null)
    {
        var aspectNative = CH.Opt.From(aspect?.MapOrThrow());
        var sampleType = format.MapOrThrow().TextureFormatSampleType(aspectNative);
        return sampleType.TryGetValue(out var value) ? value.MapOrThrow() : null;
    }

    public static Vector2u BlockDimensions(this TextureFormat format)
    {
        var (x, y) = format.MapOrThrow().TextureFormatBlockDimensions();
        return new Vector2u(x, y);
    }

    public static bool IsCompressed(this TextureFormat format) => format.BlockDimensions() != new Vector2u(1, 1);

    public static u32? BlockSize(this TextureFormat format, TextureAspect? aspect = null)
    {
        var aspectNative = CH.Opt.From(aspect?.MapOrThrow());
        return format.MapOrThrow().TextureFormatBlockSize(aspectNative).GetOrNull();
    }

    public static u8 ComponentCount(this TextureFormat format, TextureAspect aspect)
    {
        return format.MapOrThrow().TextureFormatComponents(aspect.MapOrThrow());
    }

    public static bool IsSrgb(this TextureFormat format)
    {
        return format.MapOrThrow().TextureFormatIsSrgb();
    }

    public static TextureFormatFeatures GuaranteedFormatFeatures(this TextureFormat format, Screen screen)
    {
        var value = format.MapOrThrow().TextureFormatGuaranteedFormatFeatures(screen.AsRefChecked());
        return new TextureFormatFeatures
        {
            AllowedUsages = value.allowed_usages.FlagsMap(),
            Flags = value.flags.FlagsMap(),
        };
    }

    internal static Vector3u PhysicalSize(this TextureFormat format, Vector3u mipSize)
    {
        var (blockDimWidth, blockDimHHeight) = format.BlockDimensions();
        return new Vector3u
        {
            X = ((mipSize.X + blockDimWidth - 1) / blockDimWidth) * blockDimWidth,
            Y = ((mipSize.Y + blockDimHHeight - 1) / blockDimHHeight) * blockDimHHeight,
            Z = mipSize.Z,
        };
    }

    internal static (Vector3u PhysicalSize, u32 BytesPerRow, u32 RowCount) MipInfo(this TextureFormat format, Vector3u mipSize)
    {
        if(format.BlockSize() is not u32 blockSize) {
            throw new ArgumentException($"TextureFormat '{format}' is not available");
        }
        var (blockDimWidth, blockDimHHeight) = format.BlockDimensions();
        var mipPhysicalSize = new Vector3u
        {
            X = ((mipSize.X + blockDimWidth - 1) / blockDimWidth) * blockDimWidth,
            Y = ((mipSize.Y + blockDimHHeight - 1) / blockDimHHeight) * blockDimHHeight,
            Z = mipSize.Z,
        };

        u32 widthBlocks = mipPhysicalSize.X / blockDimWidth;
        u32 bytesPerRow = widthBlocks * blockSize;
        u32 heightBlocks = mipPhysicalSize.Y / blockDimHHeight;
        return (
            PhysicalSize: mipPhysicalSize,
            BytesPerRow: bytesPerRow,
            RowCount: heightBlocks * mipSize.Z);
    }
}

[System.Obsolete("", true)]
public readonly struct TextureFormatInfo
{
    public readonly TextureSampleType SampleType { get; init; }
    /// <summary>Dimension of a "block" of texels. This is always (1, 1) on uncompressed textures.</summary>
    public readonly Vector2u BlockDimensions { get; init; }
    /// <summary>
    /// Size in bytes of a "block" of texels. This is the size per pixel on uncompressed textures.
    /// (For example, 4 if format is 'Rgba8Unorm')
    /// </summary>
    public readonly u8 BlockSize { get; init; }
    /// <summary>
    /// Number of components in the format. 
    /// (For example, 4 if format is 'Rgba8Unorm', which has 4 components (R, G, B, A))
    /// </summary>
    public readonly u8 ComponentCount { get; init; }
    public readonly bool IsSrgb { get; init; }

    public bool IsCompressed => BlockDimensions != new Vector2u(1, 1);

    //internal TextureFormatInfo(in CH.TextureFormatInfo info)
    //{
    //    SampleType = info.sample_type.MapOrThrow();
    //    BlockDimensions = new(info.block_dimensions.Value1, info.block_dimensions.Value2);
    //    BlockSize = info.block_size;
    //    ComponentCount = info.components;
    //    IsSrgb = info.srgb;
    //}

    private Vector3u PhysicalSize(Vector3u mipSize)
    {
        var (w, h) = BlockDimensions;
        var block_width = (u32)w;
        var block_height = (u32)h;

        var width = ((mipSize.X + block_width - 1) / block_width) * block_width;
        var height = ((mipSize.Y + block_height - 1) / block_height) * block_height;

        return new Vector3u
        {
            X = width,
            Y = height,
            Z = mipSize.Z,
        };
    }

    public (Vector3u PhysicalSize, u32 BytesPerRow, u32 RowCount) MipInfo(Vector3u mipSize)
    {
        var mipPhysicalSize = PhysicalSize(mipSize);
        u32 widthBlocks = mipPhysicalSize.X / BlockDimensions.X;
        u32 bytesPerRow = widthBlocks * BlockSize;
        u32 heightBlocks = mipPhysicalSize.Y / BlockDimensions.Y;
        return (
            PhysicalSize: mipPhysicalSize,
            BytesPerRow: bytesPerRow,
            RowCount: heightBlocks * mipSize.Z);
    }
}
