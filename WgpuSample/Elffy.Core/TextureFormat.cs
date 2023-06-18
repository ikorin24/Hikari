#nullable enable

namespace Elffy;

public enum TextureFormat : u32
{
    [EnumMapTo(CE.TextureFormat.R8Unorm)] R8Unorm,
    [EnumMapTo(CE.TextureFormat.R8Snorm)] R8Snorm,
    [EnumMapTo(CE.TextureFormat.R8Uint)] R8Uint,
    [EnumMapTo(CE.TextureFormat.R8Sint)] R8Sint,
    [EnumMapTo(CE.TextureFormat.R16Uint)] R16Uint,
    [EnumMapTo(CE.TextureFormat.R16Sint)] R16Sint,
    [EnumMapTo(CE.TextureFormat.R16Unorm)] R16Unorm,
    [EnumMapTo(CE.TextureFormat.R16Snorm)] R16Snorm,
    [EnumMapTo(CE.TextureFormat.R16Float)] R16Float,
    [EnumMapTo(CE.TextureFormat.Rg8Unorm)] Rg8Unorm,
    [EnumMapTo(CE.TextureFormat.Rg8Snorm)] Rg8Snorm,
    [EnumMapTo(CE.TextureFormat.Rg8Uint)] Rg8Uint,
    [EnumMapTo(CE.TextureFormat.Rg8Sint)] Rg8Sint,
    [EnumMapTo(CE.TextureFormat.R32Uint)] R32Uint,
    [EnumMapTo(CE.TextureFormat.R32Sint)] R32Sint,
    [EnumMapTo(CE.TextureFormat.R32Float)] R32Float,
    [EnumMapTo(CE.TextureFormat.Rg16Uint)] Rg16Uint,
    [EnumMapTo(CE.TextureFormat.Rg16Sint)] Rg16Sint,
    [EnumMapTo(CE.TextureFormat.Rg16Unorm)] Rg16Unorm,
    [EnumMapTo(CE.TextureFormat.Rg16Snorm)] Rg16Snorm,
    [EnumMapTo(CE.TextureFormat.Rg16Float)] Rg16Float,
    [EnumMapTo(CE.TextureFormat.Rgba8Unorm)] Rgba8Unorm,
    [EnumMapTo(CE.TextureFormat.Rgba8UnormSrgb)] Rgba8UnormSrgb,
    [EnumMapTo(CE.TextureFormat.Rgba8Snorm)] Rgba8Snorm,
    [EnumMapTo(CE.TextureFormat.Rgba8Uint)] Rgba8Uint,
    [EnumMapTo(CE.TextureFormat.Rgba8Sint)] Rgba8Sint,
    [EnumMapTo(CE.TextureFormat.Bgra8Unorm)] Bgra8Unorm,
    [EnumMapTo(CE.TextureFormat.Bgra8UnormSrgb)] Bgra8UnormSrgb,
    [EnumMapTo(CE.TextureFormat.Rgb10a2Unorm)] Rgb10a2Unorm,
    [EnumMapTo(CE.TextureFormat.Rg11b10Float)] Rg11b10Float,
    [EnumMapTo(CE.TextureFormat.Rg32Uint)] Rg32Uint,
    [EnumMapTo(CE.TextureFormat.Rg32Sint)] Rg32Sint,
    [EnumMapTo(CE.TextureFormat.Rg32Float)] Rg32Float,
    [EnumMapTo(CE.TextureFormat.Rgba16Uint)] Rgba16Uint,
    [EnumMapTo(CE.TextureFormat.Rgba16Sint)] Rgba16Sint,
    [EnumMapTo(CE.TextureFormat.Rgba16Unorm)] Rgba16Unorm,
    [EnumMapTo(CE.TextureFormat.Rgba16Snorm)] Rgba16Snorm,
    [EnumMapTo(CE.TextureFormat.Rgba16Float)] Rgba16Float,
    [EnumMapTo(CE.TextureFormat.Rgba32Uint)] Rgba32Uint,
    [EnumMapTo(CE.TextureFormat.Rgba32Sint)] Rgba32Sint,
    [EnumMapTo(CE.TextureFormat.Rgba32Float)] Rgba32Float,
    [EnumMapTo(CE.TextureFormat.Depth32Float)] Depth32Float,
    [EnumMapTo(CE.TextureFormat.Depth32FloatStencil8)] Depth32FloatStencil8,
    [EnumMapTo(CE.TextureFormat.Depth24Plus)] Depth24Plus,
    [EnumMapTo(CE.TextureFormat.Depth24PlusStencil8)] Depth24PlusStencil8,
    [EnumMapTo(CE.TextureFormat.Rgb9e5Ufloat)] Rgb9e5Ufloat,
    [EnumMapTo(CE.TextureFormat.Bc1RgbaUnorm)] Bc1RgbaUnorm,
    [EnumMapTo(CE.TextureFormat.Bc1RgbaUnormSrgb)] Bc1RgbaUnormSrgb,
    [EnumMapTo(CE.TextureFormat.Bc2RgbaUnorm)] Bc2RgbaUnorm,
    [EnumMapTo(CE.TextureFormat.Bc2RgbaUnormSrgb)] Bc2RgbaUnormSrgb,
    [EnumMapTo(CE.TextureFormat.Bc3RgbaUnorm)] Bc3RgbaUnorm,
    [EnumMapTo(CE.TextureFormat.Bc3RgbaUnormSrgb)] Bc3RgbaUnormSrgb,
    [EnumMapTo(CE.TextureFormat.Bc4RUnorm)] Bc4RUnorm,
    [EnumMapTo(CE.TextureFormat.Bc4RSnorm)] Bc4RSnorm,
    [EnumMapTo(CE.TextureFormat.Bc5RgUnorm)] Bc5RgUnorm,
    [EnumMapTo(CE.TextureFormat.Bc5RgSnorm)] Bc5RgSnorm,
    [EnumMapTo(CE.TextureFormat.Bc6hRgbUfloat)] Bc6hRgbUfloat,
    [EnumMapTo(CE.TextureFormat.Bc6hRgbSfloat)] Bc6hRgbSfloat,
    [EnumMapTo(CE.TextureFormat.Bc7RgbaUnorm)] Bc7RgbaUnorm,
    [EnumMapTo(CE.TextureFormat.Bc7RgbaUnormSrgb)] Bc7RgbaUnormSrgb,
    [EnumMapTo(CE.TextureFormat.Etc2Rgb8Unorm)] Etc2Rgb8Unorm,
    [EnumMapTo(CE.TextureFormat.Etc2Rgb8UnormSrgb)] Etc2Rgb8UnormSrgb,
    [EnumMapTo(CE.TextureFormat.Etc2Rgb8A1Unorm)] Etc2Rgb8A1Unorm,
    [EnumMapTo(CE.TextureFormat.Etc2Rgb8A1UnormSrgb)] Etc2Rgb8A1UnormSrgb,
    [EnumMapTo(CE.TextureFormat.Etc2Rgba8Unorm)] Etc2Rgba8Unorm,
    [EnumMapTo(CE.TextureFormat.Etc2Rgba8UnormSrgb)] Etc2Rgba8UnormSrgb,
    [EnumMapTo(CE.TextureFormat.EacR11Unorm)] EacR11Unorm,
    [EnumMapTo(CE.TextureFormat.EacR11Snorm)] EacR11Snorm,
    [EnumMapTo(CE.TextureFormat.EacRg11Unorm)] EacRg11Unorm,
    [EnumMapTo(CE.TextureFormat.EacRg11Snorm)] EacRg11Snorm,
}

public static class TextureFormatHelper
{
    public static TextureFormatInfo TextureFormatInfo(this TextureFormat format)
    {
        var info = format.MapOrThrow().TextureFormatInfo();
        return new TextureFormatInfo(info);
    }
}

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

    internal TextureFormatInfo(in CE.TextureFormatInfo info)
    {
        SampleType = info.sample_type.MapOrThrow();
        BlockDimensions = new(info.block_dimensions.Value1, info.block_dimensions.Value2);
        BlockSize = info.block_size;
        ComponentCount = info.components;
        IsSrgb = info.srgb;
    }

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
